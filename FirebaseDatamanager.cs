using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Net;

public class FirebaseDatamanager : MonoBehaviour
{
    public static FirebaseDatamanager instance;
    DatabaseReference dbRef;
    DatabaseReference mailRef;
    DatabaseReference rankingRef;
    DatabaseReference iapRef;
    DatabaseReference loginRef;
    DatabaseReference blackListRef;
    public DatabaseReference chatRef;
    private bool isListenerRegistered = false;
    private Query recentChildQuery;
    private string storeType;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        FirebaseDatabase firebaseDB = FirebaseDatabase.DefaultInstance;
        firebaseDB.SetPersistenceEnabled(false);  // 오프라인 동기화 비활성화
        dbRef = firebaseDB.RootReference;
        mailRef = firebaseDB.GetReference("GlobalMailBox");
        rankingRef = firebaseDB.GetReference("Rankings");
        chatRef = firebaseDB.GetReference("ChatSystem");
        iapRef = firebaseDB.GetReference("IAP_History");
        loginRef = firebaseDB.GetReference("LogIn_History");
        blackListRef = firebaseDB.GetReference("BlackList");
    }

    private void Start()
    {
        recentChildQuery = chatRef.OrderByKey().LimitToLast(20);

#if UNITY_IOS
        storeType = "App Store";
#else
        storeType = "Google Play Store";
#endif
    }

    #region Chat System

    // 입력한 채팅을 파이어베이스 서버로 전송
    public void SaveChatToFirebaseServer(int ranking, string nickName, string message)
    {
        ChatMessage chatMessage = new ChatMessage(ranking, nickName, message);

        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
        {
            string json = JsonUtility.ToJson(chatMessage);

            chatRef.Push().SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Firebase Database Chat Save Complete");

                    // 인풋필드 초기화
                    ChatSystemManager.instance.chatInputField.text = "";
                }
                else
                {
                    Debug.LogError("Failed to save chat to Firebase: " + task.Exception);
                }
            });
        }
        else
        {
            Debug.Log("No CurrentUser found");
        }
    }

    // 리스너를 등록하고 RTDB에 저장된 채팅중 최신 20개를 받아옴
    public void LoadRecentChats()
    {
        // 리스너가 이미 등록되었다면 실행안함
        if (isListenerRegistered)
            return;

        // ChildAdded 이벤트 등록
        recentChildQuery.ChildAdded += HandleChildAdded;

        // bool
        isListenerRegistered = true;
    }

    // 채팅 리스너 등록 해제
    public void UnregisterListener()
    {
        if (!isListenerRegistered)
            return;

        // ChildAdded 이벤트 등록해제
        recentChildQuery.ChildAdded -= HandleChildAdded;

        // bool
        isListenerRegistered = false;
    }

    // 리스너와 함께 실행되는 이벤트
    public void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Database Error: " + args.DatabaseError.Message);
            return;
        }

        // 새로운 메시지를 ChatMessage 객체로 변환
        string chatJson = args.Snapshot.GetRawJsonValue();
        ChatMessage newMessage = JsonUtility.FromJson<ChatMessage>(chatJson);

        // 채팅 리스트에 새 메시지 추가
        ChatSystemManager.instance.chatMessages.Add(newMessage);

        // UI에 새 메시지 반영
        ChatSystemManager.instance.AddChatToUI(newMessage);
        ChatSystemManager.instance.AddChatToSmallUI();
    }

    #endregion
    #region MailBox

    public void LoadGlobalMailBox()
    {
        StartCoroutine(LoadGlobalMailBoxEnum());
    }
    public void SaveMailsToFirebase()
    {
        MailBoxManager.instance.AddSendTimeAndGuidKeyCode();
        string json = JsonUtility.ToJson(MailBoxManager.instance.mailBox);

        mailRef.SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Mail data saved successfully.");
            }
            else
            {
                Debug.LogError("Failed to save mail data: " + task.Exception.ToString());
            }
        });
    }
    IEnumerator LoadGlobalMailBoxEnum()
    {
        DataSnapshot snapshot = null;
        var serverData = mailRef.GetValueAsync();
        yield return new WaitUntil(predicate: () => serverData.IsCompleted);

        print("process is complete");

        snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (jsonData != null)
        {
            print("mail data found");
            MailBoxManager.instance.mailBox = JsonUtility.FromJson<MailBox>(jsonData);
            MailBoxManager.instance.CompareAndSaveMail();
            MailBoxManager.instance.CompareAndDeleteUserSavedMail();
        }
        else
        {
            print("no mail data found");
        }
    }
    #endregion
    #region Time

    // 시간계산 메서드 : timeAfterLastPlay까지 계산 : 로그인씬 시작시 1회 작동
    public IEnumerator TimeCalculator()
    {
        // 서버 시간 저장 비동기 작업 시작
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // 서버 시간 로드 비동기 작업 시작
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Error load server time.");
            yield break;
        }
        TimeManager.instance.currentServerTime = (long)loadTask.Result.Value;
        Debug.Log("ReadServerTime Complete");

        // 일일임무 UI에 보여질 다음날 초기화까지의 시간(초)을 계산하여 TimeManager에 기록;
        CalculateTimeUntilMidnightUTC();

        // 첫 접속시 날짜를 "yyyy-MM-dd" 양식으로 저장
        CalculateUTCDateTime();

        // 마지막 플레이시간 로드 비동기 작업 시작
        var lastPlayTimeLoadTask = dbRef.Child("TimeData").Child("User Access Log").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).Child("lastPlayTime").GetValueAsync();
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => lastPlayTimeLoadTask.IsCompleted);

        if (lastPlayTimeLoadTask.IsFaulted)
        {
            Debug.LogError("Error load lastPlayTime.");
            yield break;
        }
        if (lastPlayTimeLoadTask.Result.Value == null)
        {
            Debug.Log("lastPlayTime is null.");
            yield break;
        }
        TimeManager.instance.lastPlayTime = (long)lastPlayTimeLoadTask.Result.Value;
        //Debug.Log("lastPlayTime Load Complete" + lastPlayTimeLoadTask.Result.Value);


        // 시간차이를 계산하고 그 값을 밀리초에서 초단위로 변경
        if (TimeManager.instance.lastPlayTime != 0 && TimeManager.instance.currentServerTime != 0)
        {
            long timeDifference = TimeManager.instance.currentServerTime - TimeManager.instance.lastPlayTime;
            TimeManager.instance.timeAfterLastPlay = timeDifference / 1000;
            if (TimeManager.instance.timeAfterLastPlay >= DataController.instance.playerSaveData.offlineRewardMaxTime)
            {
                TimeManager.instance.timeAfterLastPlay = (long)DataController.instance.playerSaveData.offlineRewardMaxTime;
            }
            Debug.Log("timeAfterLastPlay 계산완료");
        }
        else
        {
            Debug.Log("현재 서버시간 또는 마지막 플레이 시간이 발견되지 않습니다.");
            TimeManager.instance.timeAfterLastPlay = 0;
        }
    }

    // 일시정지 후 재개시 UTC0시까지의 시간을 다시 계산
    public IEnumerator ApplicationResumeTimeCalculator()
    {
        TimeManager.instance.isResumeTimeCalculating = true;

        // 서버 시간 저장 비동기 작업 시작
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // 서버 시간 로드 비동기 작업 시작
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Error load server time.");
            yield break;
        }
        TimeManager.instance.currentServerTime = (long)loadTask.Result.Value;
        Debug.Log("ReadServerTime Complete");

        // 1. UTC 0시까지 남은시간을 계산한다. TimeManager.instance.secondsUntilMidnightUTC 에 저장
        // 2. 현재날짜를 계산하여 저장된 날짜와 비교, 다르면 DailyInit = true로 만들고 저장된 날짜를 현재날짜로 교체
        TimeManager.instance.ARTC_Support();
    }

    // 일일초기화 이후 날짜갱신(이중 초기화 방지)
    public IEnumerator DailyInitDateUpdate()
    {
        // 서버 시간 저장 비동기 작업 시작
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // 서버 시간 로드 비동기 작업 시작
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // 비동기 작업이 완료될 때까지 대기
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Error load server time.");
            yield break;
        }
        TimeManager.instance.currentServerTime = (long)loadTask.Result.Value;
        Debug.Log("ReadServerTime Complete");

        CalculateUTCDateTime();
    }

    // UTC0시까지 남은 시간을 구하고 일일 초기화까지 시간감소 코루틴 작동
    private void CalculateTimeUntilMidnightUTC()
    {
        // currentServerTime을 초단위로 변환
        long currentSeconds = TimeManager.instance.currentServerTime / 1000;

        // 하루의 초 수
        long secondsInADay = 86400;

        // 현재 UTC 시간에서 날짜를 계산하여 다음 날 0시까지 남은 시간 계산
        long secondsUntilMidnightUTC = secondsInADay - (currentSeconds % secondsInADay);

        // 결과 로그 출력
        Debug.Log("Seconds until next midnight UTC: " + secondsUntilMidnightUTC);

        // TimeManager에 저장 또는 추가 처리
        TimeManager.instance.secondsUntilMidnightUTC = secondsUntilMidnightUTC;

        // 시간감소 코루틴 작동
        StartCoroutine(TimeManager.instance.DailyInitTimeCalculator());
    }

    // UTC시간을 yyyy-MM-dd 양식으로 저장
    private void CalculateUTCDateTime()
    {
        // currentServerTime을 초단위로 변환
        long currentSeconds = TimeManager.instance.currentServerTime / 1000;

        DateTime currentDateUTC = DateTimeOffset.FromUnixTimeSeconds(currentSeconds).UtcDateTime;
        string currentDateString = currentDateUTC.ToString("yyyy-MM-dd");

        TimeManager.instance.lastRecordedDate = currentDateString;
    }

    // 일시정지 또는 종료시 유저의 마지막 접속시간 서버에 저장
    public void RecordServerTimeOnPauseOrExit()
    {
        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
            dbRef.Child("TimeData").Child("User Access Log").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).Child("lastPlayTime").SetValueAsync(ServerValue.Timestamp);
    }

    #endregion
    #region IAP DATA SEND & SEND LOGIN HISTORY & BLACK LIST

    // 유저의 접속기록을 파이어베이스로 전송
    public void SendLogInHistory()
    {
        LoginHistory tmp_LoginHistory = new LoginHistory
        {
            userNickName = DataController.instance.playerSaveData.userNickName,
            userUID = FireBaseAuthManager.instance.user.UserId,
            clientIPAddress = GetLocalIPAddress(),
            loginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            timeZone = TimeZoneInfo.Local.DisplayName,
            usedDia_haveDia = DataController.instance.NumberUnitsExchanger(DataController.instance.playerSaveData.diamondUsageProgress) + " / " +
                              DataController.instance.NumberUnitsExchanger(DataController.instance.playerSaveData.gem),
            usedLegendKey_haveLegendKey = DataController.instance.NumberUnitsExchanger(DataController.instance.playerSaveData.usedLegendKey) + " / " +
                              DataController.instance.NumberUnitsExchanger(DataController.instance.playerSaveData.legendKey),
            isBlackList = GetBlackList(),
            store_type = storeType
        };

        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
        {
            string json = JsonUtility.ToJson(tmp_LoginHistory);

            loginRef.Push().SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Firebase SendLogInHistory Complete");
                }
                else
                {
                    Debug.LogError("Failed to SendLogInHistory to Firebase: " + task.Exception);
                }
            });
        }
        else
        {
            Debug.Log("No CurrentUser found");
        }
    }

    // 유저의 구매기록을 파이어베이스로 전송
    public void SavePurchaseHistory(string productType)
    {
        PurchaseHistory tmp_PurchaseHistory = new PurchaseHistory
        {
            userNickName = DataController.instance.playerSaveData.userNickName,
            userUID = FireBaseAuthManager.instance.user.UserId,
            clientIPAddress = GetLocalIPAddress(),
            productName = productType
        };

        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
        {
            string json = JsonUtility.ToJson(tmp_PurchaseHistory);

            iapRef.Push().SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Firebase SavePurchaseHistory Complete");
                }
                else
                {
                    Debug.LogError("Failed to SavePurchaseHistory to Firebase: " + task.Exception);
                }
            });
        }
        else
        {
            Debug.Log("No CurrentUser found");
        }
    }

    // 유저의 IP 주소를 String 값으로 반환
    string GetLocalIPAddress()
    {
        try
        {
            string hostName = Dns.GetHostName(); // 호스트 이름 가져오기
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4 주소 필터링
                {
                    return address.ToString();
                }
            }

            throw new Exception("Local IP Address Not Found!");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error retrieving local IP: " + ex.Message);
            return "127.0.0.1"; // 기본값 (localhost)
        }
    }

    // 로그인 기록(블랙리스트)
    string GetBlackList()
    {
        string tmp;

        if (DataController.instance.playerSaveData.isBlackList)
        {
            tmp = "YES";
        }
        else
        {
            tmp = "NO";
        }

        return tmp;
    }

    // 블랙리스트 검증
    public void CheckIfUserIsBlacklisted(string userUID, Action<bool> callback)
    {
        blackListRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // 블랙리스트 키 값만 비교
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    // 우선 Max_Dia_Use 값을 가져옴
                    if (childSnapshot.Key == "Max_Dia_Use")
                    {
                        // 최대 다이아값을 가져옴
                        int maxDiaUse = int.Parse(childSnapshot.Value.ToString());
                        Debug.Log("블랙기준 다이아 수량 : " + maxDiaUse);

                        // 최대 다이아값으로 전설키 사용량 계산
                        int maxLegendKey = maxDiaUse / 12500;
                        Debug.Log("블랙기준 전설키 수량 : " + maxLegendKey);

                        // 다이아 사용량이 최대 다이아값 보다 크면 블랙
                        if (DataController.instance.playerSaveData.diamondUsageProgress >= maxDiaUse)
                        {
                            // UID is in BlackList
                            Debug.Log("다이아 사용값이 비정상적으로 높습니다.");
                            callback(true);
                            return;
                        }
                        // 다이아 사용량 + 보유 다이아가 최대 다이아의 2배보다 클 경우 블랙
                        else if ((DataController.instance.playerSaveData.diamondUsageProgress + DataController.instance.playerSaveData.gem) >= (2 * maxDiaUse))
                        {
                            Debug.Log("다이아 사용값 및 보유 다이아 수량이 비정상적으로 높습니다.");
                            callback(true);
                            return;
                        }
                        // 보유하거나 사용한 전설키 수량이 maxLegendKey 보다 클 경우 블랙
                        else if (DataController.instance.playerSaveData.legendKey >= maxLegendKey || DataController.instance.playerSaveData.usedLegendKey >= maxLegendKey)
                        {
                            Debug.Log("사용하거나 보유중인 전설키 수량이 비정상적으로 높습니다.");
                            callback(true);
                            return;
                        }
                    }

                    // 두번째로 UID에 블랙리스트가 있는지 검증
                    if (childSnapshot.Value != null && childSnapshot.Value.ToString() == userUID)
                    {
                        // UID is in BlackList
                        callback(true);
                        return;
                    }
                }

                // UID not found in BlackList
                callback(false);
            }
            else
            {
                Debug.LogError("Failed to retrieve BlackList data: " + task.Exception);
                callback(false);
            }
        });
    }

    #endregion
    #region Coupon System

    // 쿠폰 코드 검증 및 보상 지급
    public IEnumerator RedeemCouponCoroutine(string couponCode, string userUID, string userNickname)
    {
        GameManager.instance.loadingPanel.SetActive(true);

        // 1. 쿠폰이 존재하는지 확인
        var couponTask = dbRef.Child("Coupon").Child(couponCode).GetValueAsync();
        yield return new WaitUntil(() => couponTask.IsCompleted);

        // 쿠폰이 존재하지 않으면 로딩창 없애고 오류 메시지 보여주기
        if (!couponTask.Result.Exists)
        {
            StartCoroutine(CouponManager.instance.ErrorMessageShow(LocaleManager.instance.GetLocaleString("Invalid coupon code")));
            GameManager.instance.loadingPanel.SetActive(false);
            yield break;
        }

        // 2. 해당 유저가 이미 쿠폰을 사용했는지 확인
        var receivedUsersTask = dbRef.Child("Coupon").Child(couponCode).Child("Received Users").Child(userUID).GetValueAsync();
        yield return new WaitUntil(() => receivedUsersTask.IsCompleted);

        // 유저가 쿠폰을 이미 사용했다면 로딩창 없애고 오류 메시지 보여주기
        if (receivedUsersTask.Result.Exists)
        {
            StartCoroutine(CouponManager.instance.ErrorMessageShow(LocaleManager.instance.GetLocaleString("Used Coupon")));
            GameManager.instance.loadingPanel.SetActive(false);
            yield break;
        }

        // 3. 이용가능 쿠폰이므로 미리 'Received Users' 목록에 사용자 추가
        var addUserTask = dbRef.Child("Coupon").Child(couponCode).Child("Received Users").Child(userUID).SetValueAsync(userNickname);
        yield return new WaitUntil(() => addUserTask.IsCompleted);

        // 로딩패널 비활성화
        GameManager.instance.loadingPanel.SetActive(false);

        // 4. 보상 지급
        int rewardTypeInt = Convert.ToInt32(couponTask.Result.Child("rewardType").Value);
        float rewardValue = Convert.ToSingle(couponTask.Result.Child("rewardValue").Value);

        // Enum형 변환
        RewardManager.RewardType rewardType = (RewardManager.RewardType)rewardTypeInt;

        // 보상 지급
        RewardManager.instance.AddRewardItem(rewardType, rewardValue);
        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

        // InputField 텍스트 초기화
        CouponManager.instance.inputField.text = "";
    }

    #endregion

    // 파이어베이스에 DC의 유저 저장클래스를 저장
    public void SaveDataToFirebaseServer()
    {
        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
        {
            string json = JsonUtility.ToJson(DataController.instance.playerSaveData);
            dbRef.Child("UserSaveData").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).SetRawJsonValueAsync(json);
            Debug.Log("Firebase Database Save Complete");
        }
        else
        {
            Debug.Log("no found CurrentUser");
        }
    }

    // 파이어베이로부터 유저데이터 로드
    public void LoadDataFromFirebaseServer()
    {
        StartCoroutine(LoadDataEnum());
    }
    public void VersionCheck()
    {
        StartCoroutine(LoadVersionDataEnum());
    }
    public void SetNickname(string nickname)
    {
        // 사용자가 입력한 닉네임이 이미 존재하는지 확인합니다.
        dbRef.Child("UserNickName").Child(nickname).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error fetching data: " + task.Exception);
                    return;
                }

                // 닉네임이 이미 존재하면 사용자에게 알립니다.

                /*
                유니티는 메인 스레드에서 UI 요소를 조작해야 합니다. 
                그러나 Firebase의 비동기 작업은 메인 스레드가 아닌 백그라운드 스레드에서 수행될 수 있습니다. 
                따라서 UI를 조작하는 코드는 메인 스레드에서 실행되지 않을 수 있습니다. 
                */

                if (task.Result.Exists)
                {
                    Debug.Log("This nickname is already in use. Please choose another one.");
                    NickNameSetting.instance.error = true;
                }
                else
                {
                    // 닉네임을 데이터베이스에 저장합니다.
                    SaveNickname(nickname);
                }
            }
        });
    }

    #region All Rankings

    // 스테이지 랭킹
    public void UpdateUserStageLevel(string userNickName, int stageLevel)
    {
        // 랭킹 정보를 업데이트하는 부분
        rankingRef.Child("Stage Ranking").Child(userNickName).SetValueAsync(stageLevel).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetUserStageRanking(userNickName, stageLevel);
            }
            else
            {
                // 실패해도 표지판은 떨어지도록 강제설정
                StageRanking.instance.isGetUserRankingEnd = true;
                StageRanking.instance.RankUpdater();

                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetUserStageRanking(string userNickName, int stageLevel)
    {
        // 사용자의 레벨보다 높은 모든 유저의 수를 찾음
        rankingRef.Child("Stage Ranking").OrderByValue().StartAt(stageLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // 찾은 유저 수에 1을 더해 해당 유저의 랭킹을 얻음
                int diffRank = DataController.instance.playerSaveData.userStageRanking - rankingPosition;
                if (diffRank <= 0)
                {
                    diffRank = 0;
                }
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");

                StageRanking.instance.diffRank = diffRank;
                DataController.instance.playerSaveData.userStageRanking = rankingPosition;

                StageRanking.instance.isGetUserRankingEnd = true;
                StageRanking.instance.RankUpdater();
            }
            else
            {
                // 실패해도 표지판은 떨어지도록 강제설정
                StageRanking.instance.isGetUserRankingEnd = true;
                StageRanking.instance.RankUpdater();

                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
        });
    }
    public void UploadUserStageRankData(string userNickName, int stageLevel)
    {
        // "Stage Ranking" 하위에 랜덤 생성된 유저 ID와 레벨로 데이터를 설정합니다.
        rankingRef.Child("Stage Ranking").Child(userNickName).SetValueAsync(stageLevel).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"User {userNickName} with level {stageLevel} uploaded successfully.");
            }
            else
            {
                Debug.LogError($"Failed to upload user {userNickName}: {task.Exception}");
            }
        });
    } // 유저랭킹 데이터 벌그업할때 사용
    public IEnumerator FetchTop100StageRank()
    {
        var task = rankingRef.Child("Stage Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task가 완료될 때까지 대기
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("상위 100명의 유저 데이터 가져오기 실패: " + task.Exception);
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            RankingManager.instance.stageRank_Top100.Clear();
            List<RankData> tempRankDataList = new List<RankData>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                RankData rankData = new RankData
                {
                    userNickName = child.Key,  // 파이어베이스 키를 닉네임으로 사용
                    userPoint = (long)child.Value  // 파이어베이스 값으로 포인트를 설정
                };
                tempRankDataList.Add(rankData);
            }

            // 파이어베이스는 데이터를 오름차순으로 반환하므로 순서를 뒤집어 최상위 랭킹부터 저장
            tempRankDataList.Reverse();

            RankingManager.instance.stageRank_Top100.AddRange(tempRankDataList);
            Debug.Log("상위 100명의 유저 데이터가 stageRank_Top100에 로드됨.");
            RankingManager.instance.StageRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // 월드버프 랭킹
    public void UpdateUserWorldBuffCount(string userNickName, int worldBuffCount)
    {
        // 랭킹 정보를 업데이트하는 부분
        rankingRef.Child("World Buff Ranking").Child(userNickName).SetValueAsync(worldBuffCount).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetUserWorldBuffRanking(userNickName, worldBuffCount);
            }
            else
            {
                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetUserWorldBuffRanking(string userNickName, int worldBuffCount)
    {
        // 사용자의 레벨보다 높은 모든 유저의 수를 찾음
        rankingRef.Child("World Buff Ranking").OrderByValue().StartAt(worldBuffCount + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // 찾은 유저 수에 1을 더해 해당 유저의 랭킹을 얻음
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userWorldBuffRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100WorldBuffRank()
    {
        var task = rankingRef.Child("World Buff Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task가 완료될 때까지 대기
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("상위 100명의 유저 데이터 가져오기 실패: " + task.Exception);
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            RankingManager.instance.worldBuffRank_Top100.Clear();
            List<RankData> tempRankDataList = new List<RankData>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                RankData rankData = new RankData
                {
                    userNickName = child.Key,  // 파이어베이스 키를 닉네임으로 사용
                    userPoint = (long)child.Value  // 파이어베이스 값으로 포인트를 설정
                };
                tempRankDataList.Add(rankData);
            }

            // 파이어베이스는 데이터를 오름차순으로 반환하므로 순서를 뒤집어 최상위 랭킹부터 저장
            tempRankDataList.Reverse();

            RankingManager.instance.worldBuffRank_Top100.AddRange(tempRankDataList);
            Debug.Log("상위 100명의 유저 데이터가 worldBuffRank_Top100에 로드됨.");
            RankingManager.instance.WorldBuffRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // 토벌 랭킹
    public void UpdateUserSuppressionRank(string userNickName, int suppressionMonsterCount)
    {
        // 랭킹 정보를 업데이트하는 부분
        rankingRef.Child("Suppression Ranking").Child(userNickName).SetValueAsync(suppressionMonsterCount).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetUserSuppressionRank(userNickName, suppressionMonsterCount);
            }
            else
            {
                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetUserSuppressionRank(string userNickName, int suppressionLevel)
    {
        // 사용자의 레벨보다 높은 모든 유저의 수를 찾음
        rankingRef.Child("Suppression Ranking").OrderByValue().StartAt(suppressionLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // 찾은 유저 수에 1을 더해 해당 유저의 랭킹을 얻음
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userSuppressionRanking = rankingPosition;
            }
        });
    }

    public IEnumerator FetchTop100SuppressionRank()
    {
        var task = rankingRef.Child("Suppression Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task가 완료될 때까지 대기
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("상위 100명의 유저 데이터 가져오기 실패: " + task.Exception);
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            RankingManager.instance.suppressionRank_Top100.Clear();
            List<RankData> tempRankDataList = new List<RankData>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                RankData rankData = new RankData
                {
                    userNickName = child.Key,  // 파이어베이스 키를 닉네임으로 사용
                    userPoint = (long)child.Value  // 파이어베이스 값으로 포인트를 설정
                };
                tempRankDataList.Add(rankData);
            }

            // 파이어베이스는 데이터를 오름차순으로 반환하므로 순서를 뒤집어 최상위 랭킹부터 저장
            tempRankDataList.Reverse();

            RankingManager.instance.suppressionRank_Top100.AddRange(tempRankDataList);
            Debug.Log("상위 100명의 유저 데이터가 suppressionRank_Top100에 로드됨.");
            RankingManager.instance.SuppressionRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // 레벨 랭킹
    public void UpdateHeroLevelRank(string userNickName, int heroLevel)
    {
        // 랭킹 정보를 업데이트하는 부분
        rankingRef.Child("Hero Level Ranking").Child(userNickName).SetValueAsync(heroLevel).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetHeroLevelRank(userNickName, heroLevel);
            }
            else
            {
                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetHeroLevelRank(string userNickName, int heroLevel)
    {
        // 사용자의 레벨보다 높은 모든 유저의 수를 찾음
        rankingRef.Child("Hero Level Ranking").OrderByValue().StartAt(heroLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // 찾은 유저 수에 1을 더해 해당 유저의 랭킹을 얻음
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userHeroLevelRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100LevelRank()
    {
        var task = rankingRef.Child("Hero Level Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task가 완료될 때까지 대기
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("상위 100명의 유저 데이터 가져오기 실패: " + task.Exception);
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            RankingManager.instance.levelRank_Top100.Clear();
            List<RankData> tempRankDataList = new List<RankData>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                RankData rankData = new RankData
                {
                    userNickName = child.Key,  // 파이어베이스 키를 닉네임으로 사용
                    userPoint = (long)child.Value  // 파이어베이스 값으로 포인트를 설정
                };
                tempRankDataList.Add(rankData);
            }

            // 파이어베이스는 데이터를 오름차순으로 반환하므로 순서를 뒤집어 최상위 랭킹부터 저장
            tempRankDataList.Reverse();

            RankingManager.instance.levelRank_Top100.AddRange(tempRankDataList);
            Debug.Log("상위 100명의 유저 데이터가 levelRank_Top100에 로드됨.");
            RankingManager.instance.HeroLevelRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // 주민수 랭킹
    public void UpdateUserVillagerRank(string userNickName, int villagerCount)
    {
        // 랭킹 정보를 업데이트하는 부분
        rankingRef.Child("Villager Ranking").Child(userNickName).SetValueAsync(villagerCount).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetUserVillagerRank(userNickName, villagerCount);
            }
            else
            {
                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetUserVillagerRank(string userNickName, int villagerCount)
    {
        // 사용자의 레벨보다 높은 모든 유저의 수를 찾음
        rankingRef.Child("Villager Ranking").OrderByValue().StartAt(villagerCount + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // 찾은 유저 수에 1을 더해 해당 유저의 랭킹을 얻음
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userVillagerRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100VillagerRank()
    {
        var task = rankingRef.Child("Villager Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task가 완료될 때까지 대기
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("상위 100명의 유저 데이터 가져오기 실패: " + task.Exception);
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            RankingManager.instance.villagerRank_Top100.Clear();
            List<RankData> tempRankDataList = new List<RankData>();

            foreach (DataSnapshot child in snapshot.Children)
            {
                RankData rankData = new RankData
                {
                    userNickName = child.Key,  // 파이어베이스 키를 닉네임으로 사용
                    userPoint = (long)child.Value  // 파이어베이스 값으로 포인트를 설정
                };
                tempRankDataList.Add(rankData);
            }

            // 파이어베이스는 데이터를 오름차순으로 반환하므로 순서를 뒤집어 최상위 랭킹부터 저장
            tempRankDataList.Reverse();

            RankingManager.instance.villagerRank_Top100.AddRange(tempRankDataList);
            Debug.Log("상위 100명의 유저 데이터가 villagerRank_Top100에 로드됨.");
            RankingManager.instance.VillagerRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }
    #endregion

    void DeleteNickName(string nickname)
    {
        // UserNickName 항목에서 데이터 삭제
        dbRef.Child("UserNickName").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });

        // Hero Level Ranking 항목에서 데이터 삭제
        rankingRef.Child("Hero Level Ranking").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });

        // Stage Ranking 항목에서 데이터 삭제
        rankingRef.Child("Stage Ranking").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });

        // Suppression Ranking 항목에서 데이터 삭제
        rankingRef.Child("Suppression Ranking").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });

        // Villager Ranking 항목에서 데이터 삭제
        rankingRef.Child("Villager Ranking").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });

        // World Buff Ranking 항목에서 데이터 삭제
        rankingRef.Child("World Buff Ranking").Child(nickname).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error deleting data: " + task.Exception);
                }
                else
                {
                    Debug.Log("Nickname deleted successfully.");
                }
            }
        });
    }
    void SaveNickname(string nickname)
    {
        // 데이터베이스에 새로운 닉네임을 저장합니다.
        dbRef.Child("UserNickName").Child(nickname).SetValueAsync(FireBaseAuthManager.instance.auth.CurrentUser.UserId).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error saving data: " + task.Exception);
                    return;
                }

                // 사용자에게 성공적으로 설정되었음을 알립니다.
                // 닉네임 변경의 경우 기존의 닉네임에 해당하는 랭킹 데이터 삭제 및 다이아 1만개 감소
                if (!string.IsNullOrEmpty(NickNameSetting.instance.archivedNickName))
                {
                    Debug.Log("기존의 랭킹 데이터를 삭제합니다.");
                    DeleteNickName(NickNameSetting.instance.archivedNickName);
                    DataController.instance.playerSaveData.gem -= 10000;
                }
                DataController.instance.playerSaveData.userNickName = nickname;
                Debug.Log("Nickname set successfully!");
                NickNameSetting.instance.complete = true;
                Debug.Log("TimeAccelerator 시작");
                GameManager.instance.TimeAccelerator(1f);

                // 파이어베이스의 비동기작업은 UI뿐 아니라 Time.timescale 이외에
                // 유니티 메인스레드 관련된 여러작업과 같이 실행이 안될 수 있음
                // 이 아래 코드가 실행 안되는것도 비슷한 이유임
                // 그래서 파이어베이스 비동기작업을 할땐 이런 속성을 최대한 유의해야함
                Debug.Log("TimeAccelerator 완료"); // 실행 안됨
            }
        });
    }

    // 파이어베이스로부터 유저 데이터 로드 코루틴
    IEnumerator LoadDataEnum()
    {
        DataSnapshot snapshot = null;
        var serverData = dbRef.Child("UserSaveData").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => serverData.IsCompleted);

        print("데이터 불러오기 완료");
        LogInScene.instacne.loadingPanel.SetActive(false);

        snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (jsonData != null)
        {
            // DB에 데이터가 발견되면 데이터를 불러와서 씬 이동
            print("server data found");
            DataController.instance.playerSaveData = JsonUtility.FromJson<PlayerSaveData>(jsonData);

            // 로딩 패널을 닫음
            LogInScene.instacne.waitLoading = false;

            // 첫번째 튜토리얼을 완료했으면 메인씬을 불러오고 그 이외의 경우에는 배틀씬을 불러온다.
            if (DataController.instance.playerSaveData.firstTutorial)
            {
                GameSceneManager.instance.LoadingAndLoadScene("2_Main");
            }
            else
            {
                GameSceneManager.instance.LoadingAndLoadScene("3_Battle");
            }

            Debug.Log("DT에 Json데이터 덮어쓰기 완료");
        }
        else
        {
            // DB에 데이터가 없다면 씬 이동후 해당 UID로 새로운 DB 생성
            print("no data found");
            FireBaseAuthManager.instance.noDataFound = true;
        }
    }

    // 버전체크 코루틴 : 로그인씬 버전
    IEnumerator LoadVersionDataEnum()
    {
        DataSnapshot versionSnapshot = null;
        var versionData = dbRef.Child("GameVersionCheck").Child("LatestVersion").GetValueAsync();

        yield return new WaitUntil(predicate: () => versionData.IsCompleted);
        versionSnapshot = versionData.Result;
        string versionJsonData = versionSnapshot.GetRawJsonValue().Trim('"');
        string appVersion = Application.version;

        // 문자열을 float으로 변환
        float firebaseVersion = float.Parse(versionJsonData);
        float currentAppVersion = float.Parse(appVersion);

        Debug.Log("애플리케이션 버전 : " + currentAppVersion);
        Debug.Log("파이어베이스 DB 버전 : " + firebaseVersion);
        if (currentAppVersion >= firebaseVersion)
        {
            Debug.Log("버전체크 통과 => 로그인을 시작합니다.");
            FireBaseAuthManager.instance.versionCheck = true;
            FireBaseAuthManager.instance.isVersionCheckEnd = true;
        }
        else
        {
            Debug.Log("버전체크 실패 => 버전패널 온");
            FireBaseAuthManager.instance.versionCheck = false;
            FireBaseAuthManager.instance.isVersionCheckEnd = true;
        }
    }

    // 버전체크 코루틴 : 메인씬 버전
    public IEnumerator LoadVersionDataEnum_MainScene()
    {
        DataSnapshot versionSnapshot = null;
        var versionData = dbRef.Child("GameVersionCheck").Child("LatestVersion").GetValueAsync();

        yield return new WaitUntil(predicate: () => versionData.IsCompleted);
        versionSnapshot = versionData.Result;
        string versionJsonData = versionSnapshot.GetRawJsonValue().Trim('"');
        string appVersion = Application.version;

        // 문자열을 float으로 변환
        float firebaseVersion = float.Parse(versionJsonData);
        float currentAppVersion = float.Parse(appVersion);

        Debug.Log("애플리케이션 버전 : " + currentAppVersion);
        Debug.Log("파이어베이스 DB 버전 : " + firebaseVersion);
        if (currentAppVersion >= firebaseVersion)
        {
            Debug.Log("버전체크 통과 => 게임진행 계속");
        }
        else
        {
            Debug.Log("버전체크 실패 => 게임진행을 멈추고 버전패널을 보여줌");
            NoticeManager.instance.versionCheckPanel.SetActive(true);
        }
    }
}

// 채팅용 클래스
[System.Serializable]
public class ChatMessage
{
    public int UserRanking;
    public string UserNickName;
    public string Message;

    // 기본 생성자
    public ChatMessage()
    {

    }

    // 정의 생성자
    public ChatMessage(int userRanking, string userNickName, string message)
    {
        UserRanking = userRanking;
        UserNickName = userNickName;
        Message = message;
    }

}

// 구매내역 기록관리용 클래스
[Serializable]
public class PurchaseHistory
{
    public string userNickName;
    public string userUID;
    public string clientIPAddress;
    public string productName;
}

// 로그인 기록용 클래스
[Serializable]
public class LoginHistory
{
    public string userNickName;
    public string userUID;
    public string clientIPAddress;
    public string loginTime;
    public string timeZone;
    public string usedDia_haveDia;
    public string usedLegendKey_haveLegendKey;
    public string isBlackList;
    public string store_type;
}