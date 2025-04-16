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
        firebaseDB.SetPersistenceEnabled(false);  // �������� ����ȭ ��Ȱ��ȭ
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

    // �Է��� ä���� ���̾�̽� ������ ����
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

                    // ��ǲ�ʵ� �ʱ�ȭ
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

    // �����ʸ� ����ϰ� RTDB�� ����� ä���� �ֽ� 20���� �޾ƿ�
    public void LoadRecentChats()
    {
        // �����ʰ� �̹� ��ϵǾ��ٸ� �������
        if (isListenerRegistered)
            return;

        // ChildAdded �̺�Ʈ ���
        recentChildQuery.ChildAdded += HandleChildAdded;

        // bool
        isListenerRegistered = true;
    }

    // ä�� ������ ��� ����
    public void UnregisterListener()
    {
        if (!isListenerRegistered)
            return;

        // ChildAdded �̺�Ʈ �������
        recentChildQuery.ChildAdded -= HandleChildAdded;

        // bool
        isListenerRegistered = false;
    }

    // �����ʿ� �Բ� ����Ǵ� �̺�Ʈ
    public void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Database Error: " + args.DatabaseError.Message);
            return;
        }

        // ���ο� �޽����� ChatMessage ��ü�� ��ȯ
        string chatJson = args.Snapshot.GetRawJsonValue();
        ChatMessage newMessage = JsonUtility.FromJson<ChatMessage>(chatJson);

        // ä�� ����Ʈ�� �� �޽��� �߰�
        ChatSystemManager.instance.chatMessages.Add(newMessage);

        // UI�� �� �޽��� �ݿ�
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

    // �ð���� �޼��� : timeAfterLastPlay���� ��� : �α��ξ� ���۽� 1ȸ �۵�
    public IEnumerator TimeCalculator()
    {
        // ���� �ð� ���� �񵿱� �۾� ����
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // �񵿱� �۾��� �Ϸ�� ������ ���
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // ���� �ð� �ε� �񵿱� �۾� ����
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // �񵿱� �۾��� �Ϸ�� ������ ���
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Error load server time.");
            yield break;
        }
        TimeManager.instance.currentServerTime = (long)loadTask.Result.Value;
        Debug.Log("ReadServerTime Complete");

        // �����ӹ� UI�� ������ ������ �ʱ�ȭ������ �ð�(��)�� ����Ͽ� TimeManager�� ���;
        CalculateTimeUntilMidnightUTC();

        // ù ���ӽ� ��¥�� "yyyy-MM-dd" ������� ����
        CalculateUTCDateTime();

        // ������ �÷��̽ð� �ε� �񵿱� �۾� ����
        var lastPlayTimeLoadTask = dbRef.Child("TimeData").Child("User Access Log").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).Child("lastPlayTime").GetValueAsync();
        // �񵿱� �۾��� �Ϸ�� ������ ���
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


        // �ð����̸� ����ϰ� �� ���� �и��ʿ��� �ʴ����� ����
        if (TimeManager.instance.lastPlayTime != 0 && TimeManager.instance.currentServerTime != 0)
        {
            long timeDifference = TimeManager.instance.currentServerTime - TimeManager.instance.lastPlayTime;
            TimeManager.instance.timeAfterLastPlay = timeDifference / 1000;
            if (TimeManager.instance.timeAfterLastPlay >= DataController.instance.playerSaveData.offlineRewardMaxTime)
            {
                TimeManager.instance.timeAfterLastPlay = (long)DataController.instance.playerSaveData.offlineRewardMaxTime;
            }
            Debug.Log("timeAfterLastPlay ���Ϸ�");
        }
        else
        {
            Debug.Log("���� �����ð� �Ǵ� ������ �÷��� �ð��� �߰ߵ��� �ʽ��ϴ�.");
            TimeManager.instance.timeAfterLastPlay = 0;
        }
    }

    // �Ͻ����� �� �簳�� UTC0�ñ����� �ð��� �ٽ� ���
    public IEnumerator ApplicationResumeTimeCalculator()
    {
        TimeManager.instance.isResumeTimeCalculating = true;

        // ���� �ð� ���� �񵿱� �۾� ����
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // �񵿱� �۾��� �Ϸ�� ������ ���
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // ���� �ð� �ε� �񵿱� �۾� ����
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // �񵿱� �۾��� �Ϸ�� ������ ���
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsFaulted)
        {
            Debug.LogError("Error load server time.");
            yield break;
        }
        TimeManager.instance.currentServerTime = (long)loadTask.Result.Value;
        Debug.Log("ReadServerTime Complete");

        // 1. UTC 0�ñ��� �����ð��� ����Ѵ�. TimeManager.instance.secondsUntilMidnightUTC �� ����
        // 2. ���糯¥�� ����Ͽ� ����� ��¥�� ��, �ٸ��� DailyInit = true�� ����� ����� ��¥�� ���糯¥�� ��ü
        TimeManager.instance.ARTC_Support();
    }

    // �����ʱ�ȭ ���� ��¥����(���� �ʱ�ȭ ����)
    public IEnumerator DailyInitDateUpdate()
    {
        // ���� �ð� ���� �񵿱� �۾� ����
        var saveTask = dbRef.Child("TimeData").Child("ServerTime").SetValueAsync(ServerValue.Timestamp);
        // �񵿱� �۾��� �Ϸ�� ������ ���
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.IsFaulted)
        {
            Debug.LogError("Error saving server time.");
            yield break;
        }
        Debug.Log("Server time saved successfully.");


        // ���� �ð� �ε� �񵿱� �۾� ����
        var loadTask = dbRef.Child("TimeData").Child("ServerTime").GetValueAsync();
        // �񵿱� �۾��� �Ϸ�� ������ ���
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

    // UTC0�ñ��� ���� �ð��� ���ϰ� ���� �ʱ�ȭ���� �ð����� �ڷ�ƾ �۵�
    private void CalculateTimeUntilMidnightUTC()
    {
        // currentServerTime�� �ʴ����� ��ȯ
        long currentSeconds = TimeManager.instance.currentServerTime / 1000;

        // �Ϸ��� �� ��
        long secondsInADay = 86400;

        // ���� UTC �ð����� ��¥�� ����Ͽ� ���� �� 0�ñ��� ���� �ð� ���
        long secondsUntilMidnightUTC = secondsInADay - (currentSeconds % secondsInADay);

        // ��� �α� ���
        Debug.Log("Seconds until next midnight UTC: " + secondsUntilMidnightUTC);

        // TimeManager�� ���� �Ǵ� �߰� ó��
        TimeManager.instance.secondsUntilMidnightUTC = secondsUntilMidnightUTC;

        // �ð����� �ڷ�ƾ �۵�
        StartCoroutine(TimeManager.instance.DailyInitTimeCalculator());
    }

    // UTC�ð��� yyyy-MM-dd ������� ����
    private void CalculateUTCDateTime()
    {
        // currentServerTime�� �ʴ����� ��ȯ
        long currentSeconds = TimeManager.instance.currentServerTime / 1000;

        DateTime currentDateUTC = DateTimeOffset.FromUnixTimeSeconds(currentSeconds).UtcDateTime;
        string currentDateString = currentDateUTC.ToString("yyyy-MM-dd");

        TimeManager.instance.lastRecordedDate = currentDateString;
    }

    // �Ͻ����� �Ǵ� ����� ������ ������ ���ӽð� ������ ����
    public void RecordServerTimeOnPauseOrExit()
    {
        if (FireBaseAuthManager.instance.auth.CurrentUser != null)
            dbRef.Child("TimeData").Child("User Access Log").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).Child("lastPlayTime").SetValueAsync(ServerValue.Timestamp);
    }

    #endregion
    #region IAP DATA SEND & SEND LOGIN HISTORY & BLACK LIST

    // ������ ���ӱ���� ���̾�̽��� ����
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

    // ������ ���ű���� ���̾�̽��� ����
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

    // ������ IP �ּҸ� String ������ ��ȯ
    string GetLocalIPAddress()
    {
        try
        {
            string hostName = Dns.GetHostName(); // ȣ��Ʈ �̸� ��������
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4 �ּ� ���͸�
                {
                    return address.ToString();
                }
            }

            throw new Exception("Local IP Address Not Found!");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error retrieving local IP: " + ex.Message);
            return "127.0.0.1"; // �⺻�� (localhost)
        }
    }

    // �α��� ���(������Ʈ)
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

    // ������Ʈ ����
    public void CheckIfUserIsBlacklisted(string userUID, Action<bool> callback)
    {
        blackListRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // ������Ʈ Ű ���� ��
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    // �켱 Max_Dia_Use ���� ������
                    if (childSnapshot.Key == "Max_Dia_Use")
                    {
                        // �ִ� ���̾ư��� ������
                        int maxDiaUse = int.Parse(childSnapshot.Value.ToString());
                        Debug.Log("������ ���̾� ���� : " + maxDiaUse);

                        // �ִ� ���̾ư����� ����Ű ��뷮 ���
                        int maxLegendKey = maxDiaUse / 12500;
                        Debug.Log("������ ����Ű ���� : " + maxLegendKey);

                        // ���̾� ��뷮�� �ִ� ���̾ư� ���� ũ�� ��
                        if (DataController.instance.playerSaveData.diamondUsageProgress >= maxDiaUse)
                        {
                            // UID is in BlackList
                            Debug.Log("���̾� ��밪�� ������������ �����ϴ�.");
                            callback(true);
                            return;
                        }
                        // ���̾� ��뷮 + ���� ���̾ư� �ִ� ���̾��� 2�躸�� Ŭ ��� ��
                        else if ((DataController.instance.playerSaveData.diamondUsageProgress + DataController.instance.playerSaveData.gem) >= (2 * maxDiaUse))
                        {
                            Debug.Log("���̾� ��밪 �� ���� ���̾� ������ ������������ �����ϴ�.");
                            callback(true);
                            return;
                        }
                        // �����ϰų� ����� ����Ű ������ maxLegendKey ���� Ŭ ��� ��
                        else if (DataController.instance.playerSaveData.legendKey >= maxLegendKey || DataController.instance.playerSaveData.usedLegendKey >= maxLegendKey)
                        {
                            Debug.Log("����ϰų� �������� ����Ű ������ ������������ �����ϴ�.");
                            callback(true);
                            return;
                        }
                    }

                    // �ι�°�� UID�� ������Ʈ�� �ִ��� ����
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

    // ���� �ڵ� ���� �� ���� ����
    public IEnumerator RedeemCouponCoroutine(string couponCode, string userUID, string userNickname)
    {
        GameManager.instance.loadingPanel.SetActive(true);

        // 1. ������ �����ϴ��� Ȯ��
        var couponTask = dbRef.Child("Coupon").Child(couponCode).GetValueAsync();
        yield return new WaitUntil(() => couponTask.IsCompleted);

        // ������ �������� ������ �ε�â ���ְ� ���� �޽��� �����ֱ�
        if (!couponTask.Result.Exists)
        {
            StartCoroutine(CouponManager.instance.ErrorMessageShow(LocaleManager.instance.GetLocaleString("Invalid coupon code")));
            GameManager.instance.loadingPanel.SetActive(false);
            yield break;
        }

        // 2. �ش� ������ �̹� ������ ����ߴ��� Ȯ��
        var receivedUsersTask = dbRef.Child("Coupon").Child(couponCode).Child("Received Users").Child(userUID).GetValueAsync();
        yield return new WaitUntil(() => receivedUsersTask.IsCompleted);

        // ������ ������ �̹� ����ߴٸ� �ε�â ���ְ� ���� �޽��� �����ֱ�
        if (receivedUsersTask.Result.Exists)
        {
            StartCoroutine(CouponManager.instance.ErrorMessageShow(LocaleManager.instance.GetLocaleString("Used Coupon")));
            GameManager.instance.loadingPanel.SetActive(false);
            yield break;
        }

        // 3. �̿밡�� �����̹Ƿ� �̸� 'Received Users' ��Ͽ� ����� �߰�
        var addUserTask = dbRef.Child("Coupon").Child(couponCode).Child("Received Users").Child(userUID).SetValueAsync(userNickname);
        yield return new WaitUntil(() => addUserTask.IsCompleted);

        // �ε��г� ��Ȱ��ȭ
        GameManager.instance.loadingPanel.SetActive(false);

        // 4. ���� ����
        int rewardTypeInt = Convert.ToInt32(couponTask.Result.Child("rewardType").Value);
        float rewardValue = Convert.ToSingle(couponTask.Result.Child("rewardValue").Value);

        // Enum�� ��ȯ
        RewardManager.RewardType rewardType = (RewardManager.RewardType)rewardTypeInt;

        // ���� ����
        RewardManager.instance.AddRewardItem(rewardType, rewardValue);
        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

        // InputField �ؽ�Ʈ �ʱ�ȭ
        CouponManager.instance.inputField.text = "";
    }

    #endregion

    // ���̾�̽��� DC�� ���� ����Ŭ������ ����
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

    // ���̾�̷κ��� ���������� �ε�
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
        // ����ڰ� �Է��� �г����� �̹� �����ϴ��� Ȯ���մϴ�.
        dbRef.Child("UserNickName").Child(nickname).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error fetching data: " + task.Exception);
                    return;
                }

                // �г����� �̹� �����ϸ� ����ڿ��� �˸��ϴ�.

                /*
                ����Ƽ�� ���� �����忡�� UI ��Ҹ� �����ؾ� �մϴ�. 
                �׷��� Firebase�� �񵿱� �۾��� ���� �����尡 �ƴ� ��׶��� �����忡�� ����� �� �ֽ��ϴ�. 
                ���� UI�� �����ϴ� �ڵ�� ���� �����忡�� ������� ���� �� �ֽ��ϴ�. 
                */

                if (task.Result.Exists)
                {
                    Debug.Log("This nickname is already in use. Please choose another one.");
                    NickNameSetting.instance.error = true;
                }
                else
                {
                    // �г����� �����ͺ��̽��� �����մϴ�.
                    SaveNickname(nickname);
                }
            }
        });
    }

    #region All Rankings

    // �������� ��ŷ
    public void UpdateUserStageLevel(string userNickName, int stageLevel)
    {
        // ��ŷ ������ ������Ʈ�ϴ� �κ�
        rankingRef.Child("Stage Ranking").Child(userNickName).SetValueAsync(stageLevel).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Ranking updated successfully.");
                GetUserStageRanking(userNickName, stageLevel);
            }
            else
            {
                // �����ص� ǥ������ ���������� ��������
                StageRanking.instance.isGetUserRankingEnd = true;
                StageRanking.instance.RankUpdater();

                Debug.LogError("Failed to update ranking: " + task.Exception);
            }
        });
    }
    public void GetUserStageRanking(string userNickName, int stageLevel)
    {
        // ������� �������� ���� ��� ������ ���� ã��
        rankingRef.Child("Stage Ranking").OrderByValue().StartAt(stageLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // ã�� ���� ���� 1�� ���� �ش� ������ ��ŷ�� ����
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
                // �����ص� ǥ������ ���������� ��������
                StageRanking.instance.isGetUserRankingEnd = true;
                StageRanking.instance.RankUpdater();

                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
        });
    }
    public void UploadUserStageRankData(string userNickName, int stageLevel)
    {
        // "Stage Ranking" ������ ���� ������ ���� ID�� ������ �����͸� �����մϴ�.
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
    } // ������ŷ ������ ���׾��Ҷ� ���
    public IEnumerator FetchTop100StageRank()
    {
        var task = rankingRef.Child("Stage Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task�� �Ϸ�� ������ ���
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("���� 100���� ���� ������ �������� ����: " + task.Exception);
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
                    userNickName = child.Key,  // ���̾�̽� Ű�� �г������� ���
                    userPoint = (long)child.Value  // ���̾�̽� ������ ����Ʈ�� ����
                };
                tempRankDataList.Add(rankData);
            }

            // ���̾�̽��� �����͸� ������������ ��ȯ�ϹǷ� ������ ������ �ֻ��� ��ŷ���� ����
            tempRankDataList.Reverse();

            RankingManager.instance.stageRank_Top100.AddRange(tempRankDataList);
            Debug.Log("���� 100���� ���� �����Ͱ� stageRank_Top100�� �ε��.");
            RankingManager.instance.StageRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // ������� ��ŷ
    public void UpdateUserWorldBuffCount(string userNickName, int worldBuffCount)
    {
        // ��ŷ ������ ������Ʈ�ϴ� �κ�
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
        // ������� �������� ���� ��� ������ ���� ã��
        rankingRef.Child("World Buff Ranking").OrderByValue().StartAt(worldBuffCount + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // ã�� ���� ���� 1�� ���� �ش� ������ ��ŷ�� ����
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userWorldBuffRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100WorldBuffRank()
    {
        var task = rankingRef.Child("World Buff Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task�� �Ϸ�� ������ ���
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("���� 100���� ���� ������ �������� ����: " + task.Exception);
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
                    userNickName = child.Key,  // ���̾�̽� Ű�� �г������� ���
                    userPoint = (long)child.Value  // ���̾�̽� ������ ����Ʈ�� ����
                };
                tempRankDataList.Add(rankData);
            }

            // ���̾�̽��� �����͸� ������������ ��ȯ�ϹǷ� ������ ������ �ֻ��� ��ŷ���� ����
            tempRankDataList.Reverse();

            RankingManager.instance.worldBuffRank_Top100.AddRange(tempRankDataList);
            Debug.Log("���� 100���� ���� �����Ͱ� worldBuffRank_Top100�� �ε��.");
            RankingManager.instance.WorldBuffRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // ��� ��ŷ
    public void UpdateUserSuppressionRank(string userNickName, int suppressionMonsterCount)
    {
        // ��ŷ ������ ������Ʈ�ϴ� �κ�
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
        // ������� �������� ���� ��� ������ ���� ã��
        rankingRef.Child("Suppression Ranking").OrderByValue().StartAt(suppressionLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // ã�� ���� ���� 1�� ���� �ش� ������ ��ŷ�� ����
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userSuppressionRanking = rankingPosition;
            }
        });
    }

    public IEnumerator FetchTop100SuppressionRank()
    {
        var task = rankingRef.Child("Suppression Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task�� �Ϸ�� ������ ���
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("���� 100���� ���� ������ �������� ����: " + task.Exception);
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
                    userNickName = child.Key,  // ���̾�̽� Ű�� �г������� ���
                    userPoint = (long)child.Value  // ���̾�̽� ������ ����Ʈ�� ����
                };
                tempRankDataList.Add(rankData);
            }

            // ���̾�̽��� �����͸� ������������ ��ȯ�ϹǷ� ������ ������ �ֻ��� ��ŷ���� ����
            tempRankDataList.Reverse();

            RankingManager.instance.suppressionRank_Top100.AddRange(tempRankDataList);
            Debug.Log("���� 100���� ���� �����Ͱ� suppressionRank_Top100�� �ε��.");
            RankingManager.instance.SuppressionRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // ���� ��ŷ
    public void UpdateHeroLevelRank(string userNickName, int heroLevel)
    {
        // ��ŷ ������ ������Ʈ�ϴ� �κ�
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
        // ������� �������� ���� ��� ������ ���� ã��
        rankingRef.Child("Hero Level Ranking").OrderByValue().StartAt(heroLevel + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // ã�� ���� ���� 1�� ���� �ش� ������ ��ŷ�� ����
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userHeroLevelRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100LevelRank()
    {
        var task = rankingRef.Child("Hero Level Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task�� �Ϸ�� ������ ���
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("���� 100���� ���� ������ �������� ����: " + task.Exception);
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
                    userNickName = child.Key,  // ���̾�̽� Ű�� �г������� ���
                    userPoint = (long)child.Value  // ���̾�̽� ������ ����Ʈ�� ����
                };
                tempRankDataList.Add(rankData);
            }

            // ���̾�̽��� �����͸� ������������ ��ȯ�ϹǷ� ������ ������ �ֻ��� ��ŷ���� ����
            tempRankDataList.Reverse();

            RankingManager.instance.levelRank_Top100.AddRange(tempRankDataList);
            Debug.Log("���� 100���� ���� �����Ͱ� levelRank_Top100�� �ε��.");
            RankingManager.instance.HeroLevelRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }

    // �ֹμ� ��ŷ
    public void UpdateUserVillagerRank(string userNickName, int villagerCount)
    {
        // ��ŷ ������ ������Ʈ�ϴ� �κ�
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
        // ������� �������� ���� ��� ������ ���� ã��
        rankingRef.Child("Villager Ranking").OrderByValue().StartAt(villagerCount + 1).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve rankings: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int rankingPosition = (int)snapshot.ChildrenCount + 1; // ã�� ���� ���� 1�� ���� �ش� ������ ��ŷ�� ����
                Debug.Log($"User {userNickName} is at ranking position: {rankingPosition}");
                DataController.instance.playerSaveData.userVillagerRanking = rankingPosition;
            }
        });
    }
    public IEnumerator FetchTop100VillagerRank()
    {
        var task = rankingRef.Child("Villager Ranking").OrderByValue().LimitToLast(100).GetValueAsync();

        // Task�� �Ϸ�� ������ ���
        GameManager.instance.loadingPanel.SetActive(true);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("���� 100���� ���� ������ �������� ����: " + task.Exception);
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
                    userNickName = child.Key,  // ���̾�̽� Ű�� �г������� ���
                    userPoint = (long)child.Value  // ���̾�̽� ������ ����Ʈ�� ����
                };
                tempRankDataList.Add(rankData);
            }

            // ���̾�̽��� �����͸� ������������ ��ȯ�ϹǷ� ������ ������ �ֻ��� ��ŷ���� ����
            tempRankDataList.Reverse();

            RankingManager.instance.villagerRank_Top100.AddRange(tempRankDataList);
            Debug.Log("���� 100���� ���� �����Ͱ� villagerRank_Top100�� �ε��.");
            RankingManager.instance.VillagerRank_DataSynchronization();
            GameManager.instance.loadingPanel.SetActive(false);
        }
    }
    #endregion

    void DeleteNickName(string nickname)
    {
        // UserNickName �׸񿡼� ������ ����
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

        // Hero Level Ranking �׸񿡼� ������ ����
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

        // Stage Ranking �׸񿡼� ������ ����
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

        // Suppression Ranking �׸񿡼� ������ ����
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

        // Villager Ranking �׸񿡼� ������ ����
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

        // World Buff Ranking �׸񿡼� ������ ����
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
        // �����ͺ��̽��� ���ο� �г����� �����մϴ�.
        dbRef.Child("UserNickName").Child(nickname).SetValueAsync(FireBaseAuthManager.instance.auth.CurrentUser.UserId).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Error saving data: " + task.Exception);
                    return;
                }

                // ����ڿ��� ���������� �����Ǿ����� �˸��ϴ�.
                // �г��� ������ ��� ������ �г��ӿ� �ش��ϴ� ��ŷ ������ ���� �� ���̾� 1���� ����
                if (!string.IsNullOrEmpty(NickNameSetting.instance.archivedNickName))
                {
                    Debug.Log("������ ��ŷ �����͸� �����մϴ�.");
                    DeleteNickName(NickNameSetting.instance.archivedNickName);
                    DataController.instance.playerSaveData.gem -= 10000;
                }
                DataController.instance.playerSaveData.userNickName = nickname;
                Debug.Log("Nickname set successfully!");
                NickNameSetting.instance.complete = true;
                Debug.Log("TimeAccelerator ����");
                GameManager.instance.TimeAccelerator(1f);

                // ���̾�̽��� �񵿱��۾��� UI�� �ƴ϶� Time.timescale �̿ܿ�
                // ����Ƽ ���ν����� ���õ� �����۾��� ���� ������ �ȵ� �� ����
                // �� �Ʒ� �ڵ尡 ���� �ȵǴ°͵� ����� ������
                // �׷��� ���̾�̽� �񵿱��۾��� �Ҷ� �̷� �Ӽ��� �ִ��� �����ؾ���
                Debug.Log("TimeAccelerator �Ϸ�"); // ���� �ȵ�
            }
        });
    }

    // ���̾�̽��κ��� ���� ������ �ε� �ڷ�ƾ
    IEnumerator LoadDataEnum()
    {
        DataSnapshot snapshot = null;
        var serverData = dbRef.Child("UserSaveData").Child(FireBaseAuthManager.instance.auth.CurrentUser.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => serverData.IsCompleted);

        print("������ �ҷ����� �Ϸ�");
        LogInScene.instacne.loadingPanel.SetActive(false);

        snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (jsonData != null)
        {
            // DB�� �����Ͱ� �߰ߵǸ� �����͸� �ҷ��ͼ� �� �̵�
            print("server data found");
            DataController.instance.playerSaveData = JsonUtility.FromJson<PlayerSaveData>(jsonData);

            // �ε� �г��� ����
            LogInScene.instacne.waitLoading = false;

            // ù��° Ʃ�丮���� �Ϸ������� ���ξ��� �ҷ����� �� �̿��� ��쿡�� ��Ʋ���� �ҷ��´�.
            if (DataController.instance.playerSaveData.firstTutorial)
            {
                GameSceneManager.instance.LoadingAndLoadScene("2_Main");
            }
            else
            {
                GameSceneManager.instance.LoadingAndLoadScene("3_Battle");
            }

            Debug.Log("DT�� Json������ ����� �Ϸ�");
        }
        else
        {
            // DB�� �����Ͱ� ���ٸ� �� �̵��� �ش� UID�� ���ο� DB ����
            print("no data found");
            FireBaseAuthManager.instance.noDataFound = true;
        }
    }

    // ����üũ �ڷ�ƾ : �α��ξ� ����
    IEnumerator LoadVersionDataEnum()
    {
        DataSnapshot versionSnapshot = null;
        var versionData = dbRef.Child("GameVersionCheck").Child("LatestVersion").GetValueAsync();

        yield return new WaitUntil(predicate: () => versionData.IsCompleted);
        versionSnapshot = versionData.Result;
        string versionJsonData = versionSnapshot.GetRawJsonValue().Trim('"');
        string appVersion = Application.version;

        // ���ڿ��� float���� ��ȯ
        float firebaseVersion = float.Parse(versionJsonData);
        float currentAppVersion = float.Parse(appVersion);

        Debug.Log("���ø����̼� ���� : " + currentAppVersion);
        Debug.Log("���̾�̽� DB ���� : " + firebaseVersion);
        if (currentAppVersion >= firebaseVersion)
        {
            Debug.Log("����üũ ��� => �α����� �����մϴ�.");
            FireBaseAuthManager.instance.versionCheck = true;
            FireBaseAuthManager.instance.isVersionCheckEnd = true;
        }
        else
        {
            Debug.Log("����üũ ���� => �����г� ��");
            FireBaseAuthManager.instance.versionCheck = false;
            FireBaseAuthManager.instance.isVersionCheckEnd = true;
        }
    }

    // ����üũ �ڷ�ƾ : ���ξ� ����
    public IEnumerator LoadVersionDataEnum_MainScene()
    {
        DataSnapshot versionSnapshot = null;
        var versionData = dbRef.Child("GameVersionCheck").Child("LatestVersion").GetValueAsync();

        yield return new WaitUntil(predicate: () => versionData.IsCompleted);
        versionSnapshot = versionData.Result;
        string versionJsonData = versionSnapshot.GetRawJsonValue().Trim('"');
        string appVersion = Application.version;

        // ���ڿ��� float���� ��ȯ
        float firebaseVersion = float.Parse(versionJsonData);
        float currentAppVersion = float.Parse(appVersion);

        Debug.Log("���ø����̼� ���� : " + currentAppVersion);
        Debug.Log("���̾�̽� DB ���� : " + firebaseVersion);
        if (currentAppVersion >= firebaseVersion)
        {
            Debug.Log("����üũ ��� => �������� ���");
        }
        else
        {
            Debug.Log("����üũ ���� => ���������� ���߰� �����г��� ������");
            NoticeManager.instance.versionCheckPanel.SetActive(true);
        }
    }
}

// ä�ÿ� Ŭ����
[System.Serializable]
public class ChatMessage
{
    public int UserRanking;
    public string UserNickName;
    public string Message;

    // �⺻ ������
    public ChatMessage()
    {

    }

    // ���� ������
    public ChatMessage(int userRanking, string userNickName, string message)
    {
        UserRanking = userRanking;
        UserNickName = userNickName;
        Message = message;
    }

}

// ���ų��� ��ϰ����� Ŭ����
[Serializable]
public class PurchaseHistory
{
    public string userNickName;
    public string userUID;
    public string clientIPAddress;
    public string productName;
}

// �α��� ��Ͽ� Ŭ����
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