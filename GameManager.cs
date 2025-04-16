using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// GameManager는 DataController와 다르게 메인씬에서 처음 불러와지므로 이를 이용해 데이터 동기화에 구분하여 사용하면 개이득
public class GameManager : MonoBehaviour
{
    [Header("# TIME ACCELERATOR")]
    public float basicTime = 300f;
    public GameObject gameSpeed1Ani;
    public GameObject gameSpeed2Ani, gameSpeed3Ani;
    public GameObject timeAcceleratorPanel;
    public GameObject adSoldOut, twiceSoldOut, threeTimesSoldOut;
    public bool oneTime_TimeACC, twice_TimeACC, threeTimes_TimeACC;
    public Text adSoldOut_Text, twiceSoldOut_Text, threeTimesSoldOut_Text;
    public Slider twiceTimeSlider, threeTimesTimeSlider;
    private WaitForSecondsRealtime waitTimeACC;

    [Header("# OFFLINE REWARD")]
    public GameObject offlineRewardPanel;
    public Text rewardTimeText;
    public Text goldRewardText;
    public Text soulStoneRewardText;
    public Text maxOfflineTimeText;
    public float goldOffReward;
    public float soulStoneOffReward;
    public float basicOfflineRewardMaxTime = 10800;
    public Slider offlineRewardSlider;

    [Header("# 메인씬 버프 UI 관련")]
    public Slider heroicSpiritSlider;
    public float heroicSpirit_DRate;
    public float maxHeroicSpiritTime;
    public float heroicSpiritTime;
    public bool isBuffActivate;
    public GameObject blessing_Start_Ani, blessing_Start_Lower_Ani, blessingAurora_Ani, blessingAurora_Ani_Front;
    public GameObject[] buffIcons;
    public float goddessBlessing_GoldBuffFactor = 0.5f, goddessBlessing_ExpBuffFactor = 0.5f;

    [Header("# 절전모드")]
    public GameObject sleepModePanel;
    public float sleepModeTime = 300f;
    public float idletime = 0f;
    WaitForSecondsRealtime sleepWait;

    [Header("# ETC")]
    public GameObject loadingPanel;
    public SpriteRenderer bgRender, fgRender;
    public Sprite[] village_BG, village_FG;
    public GameObject levelUpEffect;
    WaitForSecondsRealtime wait_SaveToFirebaseDB;

    public static GameManager instance;
    private void Awake()
    {
        instance = this;

//#if UNITY_ANDROID
        //기기 상관없이 해상도 9:16로 맞춤
        //Screen.SetResolution(Screen.width, (Screen.width / 9) * 16, true);
        if (Screen.width != 1080 || Screen.height != 1920)
            Screen.SetResolution(1080, 1920, true);
//#endif

        waitTimeACC = new WaitForSecondsRealtime(1f);
        sleepWait = new WaitForSecondsRealtime(1f);
        wait_SaveToFirebaseDB = new WaitForSecondsRealtime(900f);
    }

    private void Start()
    {
        // 각종 버프 초기화
        BuffInit();

        // 일일초기화
        DailyInit();

        StageManager.instance.stageProgressTime = 0f;
        InstanceMemorizing();
        DataController.instance.WeaponItemAmountLoad();
        DataController.instance.WeaponItemUnlockLoad();
        DataController.instance.Stage_HeroEXP_Init();
        OfflineReward();
        HeroicSpiritSliderInit();
        All_EquipmentItem_Unlock_Sync();
        InitGameSpeed();
        VillageBackGroundChange(DataController.instance.playerSaveData.villageBG_Number);

        StartCoroutine(IE_SaveToFirebaseDB());
        StartCoroutine(IE_HeroicSpiritDecrease());
        StartCoroutine(TimeAcceleratorReduce());
        PassRemainTimeInit();

        // 방치모드 타이머 작동
        StartCoroutine(IE_SleepModeActivation());

        // 사운드 초기화
        //SoundManager.Instance.SoundInit();

        //배경음 재생
        SoundManager.Instance.PlaySceneBGM();

        // 첫번째 로그인 갱신
        if (PlayerPrefs.GetInt("FirstLogin") == 0)
            PlayerPrefs.SetInt("FirstLogin", 1);

        // 패스구매자 시간가속기 UI 업데이트
        UpdateTimeAcc();

        // 접속기록
        SendLoginHistory();
    }
    private void Update()
    {
        // 기본 터치 애니메이션 생성
        TouchNormalEffect();
    }

    #region About Equipment

    // 모든 비활성화된 장비패널의 부모오브젝트를 1회 활성화하여 인스턴스 초기화
    public void InstanceMemorizing()
    {
        for (int i = 1; i < EquipmentTypeSelect.instance.equipmentTypePanels.Count; i++)
        {
            EquipmentTypeSelect.instance.equipmentTypePanels[i].SetActive(true);
            EquipmentTypeSelect.instance.equipmentTypePanels[i].SetActive(false);
        }
    }
    public void ItemSynchronize()
    {
        DataController.instance.WeaponItemAmountLoad();
        WeaponItemParent.instance.WeaponItemUnlockSave();
        DataController.instance.WeaponItemUnlockLoad();

        DataController.instance.ArmorItemAmountLoad();
        ArmorItemParent.instance.ArmorItemUnlockSave();
        DataController.instance.ArmorItemUnlockLoad();

        DataController.instance.HelmetItemAmountLoad();
        HelmetItemParent.instance.HelmetItemUnlockSave();
        DataController.instance.HelmetItemUnlockLoad();

        DataController.instance.PantsItemAmountLoad();
        PantsItemParent.instance.PantsItemUnlockSave();
        DataController.instance.PantsItemUnlockLoad();

        DataController.instance.GlovesItemAmountLoad();
        GlovesItemParent.instance.GlovesItemUnlockSave();
        DataController.instance.GlovesItemUnlockLoad();

        DataController.instance.ShoesItemAmountLoad();
        ShoesItemParent.instance.ShoesItemUnlockSave();
        DataController.instance.ShoesItemUnlockLoad();

        DataController.instance.RingItemAmountLoad();
        RingItemParent.instance.RingItemUnlockSave();
        DataController.instance.RingItemUnlockLoad();

        DataController.instance.AmuletItemAmountLoad();
        AmuletItemParent.instance.AmuletItemUnlockSave();
        DataController.instance.AmuletItemUnlockLoad();
    }
    public void WeaponItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.weaponQuantity[itemNumber] += itemCount;
    }
    public void ArmorItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.armorQuantity[itemNumber] += itemCount;
    }
    public void HelmetItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.helmetQuantity[itemNumber] += itemCount;
    }
    public void PantsItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.pantsQuantity[itemNumber] += itemCount;
    }
    public void GlovesItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.glovesQuantity[itemNumber] += itemCount;
    }
    public void ShoesItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.shoesQuantity[itemNumber] += itemCount;
    }
    public void RingItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.ringQuantity[itemNumber] += itemCount;
    }
    public void AmuletItemAdd(int itemNumber, int itemCount)
    {
        DataController.instance.playerSaveData.amuletQuantity[itemNumber] += itemCount;
    }
    private void All_EquipmentItem_Unlock_Sync()
    {
        for (int i = 0; i < DataController.instance.playerSaveData.weaponQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.weaponQuantity[i] >= 1)
            {
                DataController.instance.dtWeaponUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.armorQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.armorQuantity[i] >= 1)
            {
                DataController.instance.dtArmorUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.helmetQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.helmetQuantity[i] >= 1)
            {
                DataController.instance.dtHelmetUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.pantsQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.pantsQuantity[i] >= 1)
            {
                DataController.instance.dtPantsUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.glovesQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.glovesQuantity[i] >= 1)
            {
                DataController.instance.dtGlovesUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.shoesQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.shoesQuantity[i] >= 1)
            {
                DataController.instance.dtShoesUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.ringQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.ringQuantity[i] >= 1)
            {
                DataController.instance.dtRingUnlock[i] = 1;
            }
        }

        for (int i = 0; i < DataController.instance.playerSaveData.amuletQuantity.Count; i++)
        {
            if (DataController.instance.playerSaveData.amuletQuantity[i] >= 1)
            {
                DataController.instance.dtAmuletUnlock[i] = 1;
            }
        }
    }

    #endregion
    #region About TimeAccelerator
    // 패스구매자 시간가속기 UI 업데이트
    private void UpdateTimeAcc()
    {
        // 배틀패스 구매자
        if (DataController.instance.playerSaveData.buyBattlePass)
            PassReward_TimeACC("BattlePass");

        // 프리미엄 패스 구매자
        if (DataController.instance.playerSaveData.buyPremiumPass)
            PassReward_TimeACC("PremiumPass");
    }

    // 시간 변경시 사용 메서드
    public void TimeAccelerator(float multiple)
    {
        Time.timeScale = multiple;
        DataController.instance.gameSpeed = multiple;

        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);
        oneTime_TimeACC = false;
        twice_TimeACC = false;
        threeTimes_TimeACC = false;

        if (multiple == 1)
        {
            oneTime_TimeACC = true;
            gameSpeed1Ani.SetActive(true);
        }
        else if (multiple == 2)
        {
            twice_TimeACC = true;
            gameSpeed2Ani.SetActive(true);
        }
        else if (multiple == 3)
        {
            threeTimes_TimeACC = true;
            gameSpeed3Ani.SetActive(true);
        }
    }

    // 메인화면 버튼3개에 부착됨
    public void ClickTimeButton(float multiple)
    {
        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);
        oneTime_TimeACC = false;
        twice_TimeACC = false;
        threeTimes_TimeACC = false;

        // 1배속의 경우 항상 기본 실행
        if (multiple == 1)
        {
            oneTime_TimeACC = true;
            gameSpeed1Ani.SetActive(true);
            TimeAccelerator(1f);
            SoundManager.Instance.PlayTimeSpeedSEF();
        }
        // 2배속의 경우 AD 또는 다이아를 통해 구매 했으면 바로 실행, 아니면 상점을 연다
        else if (multiple == 2)
        {
            if (DataController.instance.playerSaveData.twiceRemainTime > 0f)
            {
                twice_TimeACC = true;
                gameSpeed2Ani.SetActive(true);
                TimeAccelerator(2f);
                SoundManager.Instance.PlayTimeSpeedSEF();
            }
            else
            {
                RefreshGameSpeed();
                timeAcceleratorPanel.SetActive(true);
                SoundManager.Instance.PlaySmallClickButtonSEF();
            }
        }
        // 3배속의 경우 다이아를 통해 구매 했으면 바로 실행, 아니면 상점을 연다
        else if (multiple == 3)
        {
            if (DataController.instance.playerSaveData.threeTimesRemainTime > 0f)
            {
                threeTimes_TimeACC = true;
                gameSpeed3Ani.SetActive(true);
                TimeAccelerator(3f);
                SoundManager.Instance.PlayTimeSpeedSEF();
            }
            else
            {
                RefreshGameSpeed();
                timeAcceleratorPanel.SetActive(true);
                SoundManager.Instance.PlaySmallClickButtonSEF();
            }
        }

        // 스테이지패널 업데이트
        StageManager.instance.RefreshStageInfo();
    }

    // 시간 가속기 상점 버튼 3개에 부착됨
    public void BuyTimeACC_Btn(string type)
    {
        switch (type)
        {
            case "twiceAD":
                AdmobManager.instance.StartCoroutine(AdmobManager.instance.StartRewardAds_TimeACC());
                break;

            case "twiceDIA":
                if (DataController.instance.playerSaveData.gem >= 500)
                {
                    // 다이아 수량 감소
                    DataController.instance.playerSaveData.gem -= 500;

                    // 업적 반영
                    AchievementManager.instance.Add_Achievement_Progress(9, 500);

                    DataController.instance.playerSaveData.twiceRemainTime = basicTime;
                    TimeACC_UI_Refresh();
                    adSoldOut.SetActive(true);
                    twiceSoldOut.SetActive(true);
                    SoundManager.Instance.PlaySuccessSEF();
                }
                else
                {
                    SoundManager.Instance.PlayCancelSEF();
                }
                break;

            case "threeTimesDIA":
                if (DataController.instance.playerSaveData.gem >= 1000)
                {
                    DataController.instance.playerSaveData.gem -= 1000;

                    // 업적 반영
                    AchievementManager.instance.Add_Achievement_Progress(9, 1000);

                    DataController.instance.playerSaveData.threeTimesRemainTime = basicTime;
                    TimeACC_UI_Refresh();
                    threeTimesSoldOut.SetActive(true);
                    SoundManager.Instance.PlaySuccessSEF();
                }
                else
                {
                    SoundManager.Instance.PlayCancelSEF();
                }
                break;
        }
    }

    // 게임 시작시 속도 초기화
    public void InitGameSpeed()
    {
        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);

        gameSpeed1Ani.SetActive(true);
        TimeAccelerator(1f);
    }

    // 게임속도를 재개할때 사용
    public void RefreshGameSpeed()
    {
        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);

        if (DataController.instance.gameSpeed == 1)
        {
            gameSpeed1Ani.SetActive(true);
            TimeAccelerator(1f);
        }
        else if (DataController.instance.gameSpeed == 2)
        {
            gameSpeed2Ani.SetActive(true);
            TimeAccelerator(2f);
        }
        else if (DataController.instance.gameSpeed == 3)
        {
            gameSpeed3Ani.SetActive(true);
            TimeAccelerator(3f);
        }
    }

    // 시간 가속기 상점 닫기 버튼
    public void TimeACC_CloseBtn()
    {
        timeAcceleratorPanel.SetActive(false);
        SoundManager.Instance.PlayBackButtonSEF();
    }

    // 주기적 시간감소 코루틴
    IEnumerator TimeAcceleratorReduce()
    {
        while (true)
        {
            // 주기적 시간감소
            TimeACCReduce();

            // 슬라이더 및 UI 반영
            TimeACC_UI_Refresh();
            yield return waitTimeACC;
        }
    }

    // 주기적 시간감소
    private void TimeACCReduce()
    {
        // 배틀패스 또는 프리미엄패스 구매시 시간감소 안함
        if (twice_TimeACC && !DataController.instance.playerSaveData.buyBattlePass)
        {
            DataController.instance.playerSaveData.twiceRemainTime--;
            if (DataController.instance.playerSaveData.twiceRemainTime <= 0f)
            {
                DataController.instance.playerSaveData.twiceRemainTime = 0f;

                // 1배속으로 강제로 바꾸고 상점 재구매 가능하게 변경
                adSoldOut.SetActive(false);
                twiceSoldOut.SetActive(false);
                TimeAccelerator(1f);
            }
        }
        else if (threeTimes_TimeACC && !DataController.instance.playerSaveData.buyPremiumPass)
        {
            DataController.instance.playerSaveData.threeTimesRemainTime--;
            if (DataController.instance.playerSaveData.threeTimesRemainTime <= 0f)
            {
                DataController.instance.playerSaveData.threeTimesRemainTime = 0f;

                // 1배속으로 강제로 바꾸고 상점 재구매 가능하게 변경
                threeTimesSoldOut.SetActive(false);
                TimeAccelerator(1f);
            }
        }
    }

    // 패스보상 : 시간가속
    public void PassReward_TimeACC(string passType)
    {
        switch (passType)
        {
            case "BattlePass":
                // 시간부여 / 슬라이더 반영 / 시간가속기 상점 Sold Out / 시간 미감소
                DataController.instance.playerSaveData.twiceRemainTime = basicTime;
                break;

            case "PremiumPass":
                DataController.instance.playerSaveData.threeTimesRemainTime = basicTime;
                break;
        }

        // UI 업데이트
        TimeACC_UI_Refresh();
    }

    // 슬라이더 및 UI 반영
    public void TimeACC_UI_Refresh()
    {
        twiceTimeSlider.maxValue = basicTime;
        threeTimesTimeSlider.maxValue = basicTime;

        twiceTimeSlider.value = DataController.instance.playerSaveData.twiceRemainTime;
        threeTimesTimeSlider.value = DataController.instance.playerSaveData.threeTimesRemainTime;

        // 시간 가속 상점의 텍스트 UI 변경
        float remainTime_Twice = DataController.instance.playerSaveData.twiceRemainTime;
        float remainTime_ThreeTimes = DataController.instance.playerSaveData.threeTimesRemainTime;
        int min_Twice = Mathf.FloorToInt(remainTime_Twice / 60);
        int sec_Twice = Mathf.FloorToInt(remainTime_Twice % 60);
        int min_ThreeTimes = Mathf.FloorToInt(remainTime_ThreeTimes / 60);
        int sec_ThreeTimes = Mathf.FloorToInt(remainTime_ThreeTimes % 60);
        adSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_Twice, sec_Twice);
        twiceSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_Twice, sec_Twice);
        threeTimesSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_ThreeTimes, sec_ThreeTimes);

        // 남은 시간에 따른 변경사항
        if (DataController.instance.playerSaveData.twiceRemainTime <= 0f && !DataController.instance.playerSaveData.buyBattlePass)
        {
            adSoldOut.SetActive(false);
            twiceSoldOut.SetActive(false);
        }
        else
        {
            adSoldOut.SetActive(true);
            twiceSoldOut.SetActive(true);
        }

        if (DataController.instance.playerSaveData.threeTimesRemainTime <= 0f && !DataController.instance.playerSaveData.buyPremiumPass)
        {
            threeTimesSoldOut.SetActive(false);
        }
        else
        {
            threeTimesSoldOut.SetActive(true);
        }
    }

    #endregion
    #region About OfflineReward

    private void OfflineReward()
    {
        if (TimeManager.instance.timeAfterLastPlay >= 60 && (DataController.instance.playerSaveData.goldOffRewardPerSecond >= 1 || DataController.instance.playerSaveData.soulStoneOffRewardPerSecond >= 1))
        {
            offlineRewardPanel.SetActive(true);
        }
        Debug.Log("마지막 저장 후 흐른시간은 " + TimeManager.instance.timeAfterLastPlay + "초 입니다.");
        long hour, minute, second, remainder;
        string sHour, sMinute, sSecond;
        int maxMinute;
        hour = TimeManager.instance.timeAfterLastPlay / 3600;
        remainder = TimeManager.instance.timeAfterLastPlay % 3600;
        minute = remainder / 60;
        second = remainder % 60;
        sHour = LocaleManager.instance.GetLocaleString("Hour");
        sMinute = LocaleManager.instance.GetLocaleString("Minute");
        sSecond = LocaleManager.instance.GetLocaleString("Second");
        maxMinute = (int)DataController.instance.playerSaveData.offlineRewardMaxTime / 60;

        maxOfflineTimeText.text = "" + maxMinute + "M";
        offlineRewardSlider.maxValue = DataController.instance.playerSaveData.offlineRewardMaxTime;
        offlineRewardSlider.value = TimeManager.instance.timeAfterLastPlay;

        if (hour >= 1)
        {
            rewardTimeText.text = string.Format("Offline Time : {0:0}{3} {1:00}{4} {2:00}{5}", hour, minute, second, sHour, sMinute, sSecond);
        }
        else
        {
            rewardTimeText.text = string.Format("Offline Time : {0:00}{2} {1:00}{3}", minute, second, sMinute, sSecond);
        }

        goldOffReward = DataController.instance.playerSaveData.goldOffRewardPerSecond * TimeManager.instance.timeAfterLastPlay / 60f;
        soulStoneOffReward = DataController.instance.playerSaveData.soulStoneOffRewardPerSecond * TimeManager.instance.timeAfterLastPlay / 60f;

        goldRewardText.text = DataController.instance.NumberUnitsExchanger(goldOffReward);
        soulStoneRewardText.text = DataController.instance.NumberUnitsExchanger(soulStoneOffReward);
    }

    #endregion
    #region About HeroicSpirit & Touch

    // 기본 터치 애니메이션
    private void TouchNormalEffect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 기본 터치 애니메이션 생성
            GameObject touchEffect = M_PoolManager.instance.EffectGet(3, M_PoolManager.instance.prefabPoolParent);
            RectTransform rectTouchEffect = touchEffect.GetComponent<RectTransform>();
            Animator animTouchEffect = touchEffect.GetComponent<Animator>();

            // 속도에 영향을 받지 않게 변경
            animTouchEffect.updateMode = AnimatorUpdateMode.UnscaledTime;

            // 최종 위치 설정
            rectTouchEffect.position = new Vector3(Input.mousePosition.x - 58f, Input.mousePosition.y - 58f, 0f);

            // 방치시간 초기화
            idletime = 0f;
        }
    }

    // 깨짐 터치 애니메이션
    public void TouchBrokenEffect()
    {
        // 기본 터치 애니메이션 생성
        GameObject touchEffect = M_PoolManager.instance.EffectGet(2, M_PoolManager.instance.prefabPoolParent);
        RectTransform rectTouchEffect = touchEffect.GetComponent<RectTransform>();
        Animator animTouchEffect = touchEffect.GetComponent<Animator>();

        // 속도에 영향을 받지 않게 변경
        animTouchEffect.updateMode = AnimatorUpdateMode.UnscaledTime;

        // 최종 위치 설정
        rectTouchEffect.position = new Vector3(Input.mousePosition.x - 120f, Input.mousePosition.y - 120f, 0f);

        // 버프설명 오프
        UIManager.instance.GoddessBlessPanelOFF(false);
    }

    // 버프 아이콘 및 효과 초기화
    private void BuffInit()
    {
        for (int i = 0; i < buffIcons.Length; i++)
        {
            buffIcons[i].SetActive(false);
        }

        DataController.instance.playerSaveData.goddessBlessing_GoldBuff = 0;
        DataController.instance.playerSaveData.goddessBlessing_ExpBuff = 0;

        // 버프 패널
        UIManager.instance.buffPanel_Gold.SetActive(false);
        UIManager.instance.buffPanel_EXP.SetActive(false);
    }

    // 버프 활성화
    public void ActiveBuff(string typeOfBuff)
    {
        switch (typeOfBuff)
        {
            case "Buff_ATT":
                break;

            case "Buff_HP":
                break;

            case "Buff_DEF":
                break;

            case "Buff_CP":
                break;

            case "Buff_CD":
                break;

            case "Buff_ATS":
                break;

            case "Buff_GOLD":
                // 여신의 가호 골드 버프
                DataController.instance.playerSaveData.goddessBlessing_GoldBuff = goddessBlessing_GoldBuffFactor;

                // 아이콘 갱신
                buffIcons[6].SetActive(true);

                // 버프 패널 업데이트
                UIManager.instance.buffPanel_Gold.SetActive(true);

                // 스테이지 정보 패널 갱신
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;

            case "Buff_EXP":
                // 여신의 가호 경험치 버프
                DataController.instance.playerSaveData.goddessBlessing_ExpBuff = goddessBlessing_ExpBuffFactor;
                buffIcons[7].SetActive(true);
                UIManager.instance.buffPanel_EXP.SetActive(true);
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;
        }
    }

    // 버프 비활성화
    public void DeactiveBuff(string typeOfBuff)
    {
        switch (typeOfBuff)
        {
            case "Buff_ATT":
                break;

            case "Buff_HP":
                break;

            case "Buff_DEF":
                break;

            case "Buff_CP":
                break;

            case "Buff_CD":
                break;

            case "Buff_ATS":
                break;

            case "Buff_GOLD":
                // 여신의 가호 골드 버프
                DataController.instance.playerSaveData.goddessBlessing_GoldBuff = 0;

                // 아이콘 갱신
                buffIcons[6].SetActive(false);

                // 버프 패널 업데이트
                UIManager.instance.buffPanel_Gold.SetActive(false);

                // 스테이지 정보 패널 갱신
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;

            case "Buff_EXP":
                // 여신의 가호 경험치 버프
                DataController.instance.playerSaveData.goddessBlessing_ExpBuff = 0;
                buffIcons[7].SetActive(false);
                UIManager.instance.buffPanel_EXP.SetActive(false);
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;
        }
    }

    // 마우스 버튼 클릭시 여신의 가호 시간 증가
    public void HeroicSpirit()
    {
        // 발키리가 죽었다면 무시
        if (!Valkyrie.instance.gameObject.activeSelf)
            return;

        // 버프 애니메이션 생성
        GameObject levelUpEffect = M_PoolManager.instance.EffectGet(1, M_PoolManager.instance.prefabPoolParent);
        levelUpEffect.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        heroicSpiritTime += 10;

        if (heroicSpiritTime >= maxHeroicSpiritTime / 3f && !isBuffActivate)
        {
            // 버프 능력치를 아래에 넣으면 된다.
            ActiveBuff("Buff_GOLD");
            ActiveBuff("Buff_EXP");

            // 애니메이션 작동
            blessing_Start_Ani.SetActive(true);
            isBuffActivate = true;

            // 효과음
            SoundManager.Instance.PlayGodBlessSEF();
        }

        if (heroicSpiritTime >= maxHeroicSpiritTime)
        {
            heroicSpiritTime = maxHeroicSpiritTime;
        }

        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // 여신의 가호 슬라이더 초기화
    private void HeroicSpiritSliderInit()
    {
        heroicSpiritTime = 0f;
        heroicSpiritSlider.maxValue = maxHeroicSpiritTime;
        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.maxValue = maxHeroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // 여신의 가호 시간 감소
    private void HeroicSpiritDecrease()
    {
        heroicSpiritTime -= heroicSpirit_DRate / 10f;

        if (heroicSpiritTime <= 0)
        {
            if (isBuffActivate)
            {
                DeactiveBuff("Buff_GOLD");
                DeactiveBuff("Buff_EXP");
                isBuffActivate = false;
            }

            heroicSpiritTime = 0f;
            blessingAurora_Ani.SetActive(false);
            blessingAurora_Ani_Front.SetActive(false);

            // 버프패널 닫기
            UIManager.instance.GoddessBlessPanelOFF(false);
        }

        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // 여신의 가호 시간 감소 코루틴
    IEnumerator IE_HeroicSpiritDecrease()
    {
        yield return new WaitForSecondsRealtime(1f);

        while (true)
        {
            HeroicSpiritDecrease();
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    #endregion
    #region About Daily Initialize

    // 매일 UTC0시(한국시간 오전 9시) 파이어베이스 Functions를 통해 모든 유저들의 dailyInit = true로 초기화
    // 9시 이후에 접속하는 유저들은 당연히 DailyInit() 메서드를 통해 일일 초기화 작업 진행
    // 만약 클라이언트에 접속한 상태로 시간이 지나서 시간관리자의 secondsUntilMidnightUTC 값이 0이되면
    // 역시 초기화 진행을 같이 해주면 된다.

    public void DailyInit()
    {
        if (DataController.instance.playerSaveData.dailyInit)
        {
            DailyInitList();

            DataController.instance.playerSaveData.dailyInit = false;
            DataController.instance.SaveToFirebaseDB();
        }
        else
        {
            Debug.Log("dailyInit = false 이므로 일일초기화할 필요 없음");
        }
    }

    // UTC0시에 초기화할 모든것들을 메서드에 넣는다
    void DailyInitList()
    {
        // 룰렛 초기화
        DataController.instance.playerSaveData.rouletteCount = 5;
        DataController.instance.playerSaveData.rouletteCount_PlusAD = 5;

        // 일일 임무 초기화
        DailyMissionManager.instance.InitDailyReward();

        // 일일 출석 초기화
        DailyLoginRewardManager.instance.dailyFirstLogin = false;
        DataController.instance.playerSaveData.dailyLoginRewardClaim = false;
        if (DataController.instance.playerSaveData.dailyLoginProgress >= 7)
            DataController.instance.playerSaveData.dailyLoginProgress = 0;
        DailyLoginRewardManager.instance.InitDailyLoginContents();
        DailyLoginRewardManager.instance.RefreshDailyLoginRewardState();

        // 상점 일일 무료 다이아 초기화
        DataController.instance.playerSaveData.freeDia_ADShop = 0;
        DataController.instance.playerSaveData.freeDia_KeyShop = 0;

        // AD SHOP 광고 보기 횟수 초기화
        DataController.instance.playerSaveData.adShop_Dia_AdCount = 0;
        DataController.instance.playerSaveData.adShop_GoldKey_AdCount = 0;
        DataController.instance.playerSaveData.adShop_Gold_AdCount = 0;
        DataController.instance.playerSaveData.adShop_SoulStone_AdCount = 0;
        DataController.instance.playerSaveData.adShop_SupStone_AdCount = 0;

        // 이중 초기화 방지를 위해 날짜 갱신
        StartCoroutine(FirebaseDatamanager.instance.DailyInitDateUpdate());
    }

    #endregion
    #region About Settings

    public void ChangeLocale(int index)
    {
        if (LocaleManager.instance.isChanging)
            return;

        StartCoroutine(LocaleManager.instance.ChangeRoutine(index));
    }

    // 메인씬의 마을 배경 변경 메서드
    private void VillageBackGroundChange(int villageNumber)
    {
        bgRender.sprite = village_BG[villageNumber];
        fgRender.sprite = village_FG[villageNumber];
    }

    #endregion
    #region Admob AD Buttons

    // 상점 광고 상품
    public void AD_SHOP_REWARDADS_Button(string type)
    {
        switch (type)
        {
            case "Dia":
                if (DataController.instance.playerSaveData.adShop_Dia_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // 광고 횟수 초과
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // 패스 구매자는 광고없이 즉시 보상지급
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gem, 500);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // 일일임무 광고보기 횟수 추가
                        DailyMissionManager.instance.AddProgressDailyMission(11);

                        // AD SHOP 광고횟수 추가 및 상점 UI 업데이트
                        DataController.instance.playerSaveData.adShop_Dia_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        // 무료 이용자는 광고를 보고 보상지급
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopDia());
                    }
                }
                break;

            case "SupStone":
                if (DataController.instance.playerSaveData.adShop_SupStone_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // 광고 횟수 초과
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // 패스 구매자는 광고없이 즉시 보상지급
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SuppressionStone, 10);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP 광고횟수 추가
                        DataController.instance.playerSaveData.adShop_SupStone_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopSupStone());
                    }
                }
                break;

            case "GoldKey":
                if (DataController.instance.playerSaveData.adShop_GoldKey_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // 광고 횟수 초과
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // 패스 구매자는 광고없이 즉시 보상지급
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.GoldKey, 1);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP 광고횟수 추가
                        DataController.instance.playerSaveData.adShop_GoldKey_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopGoldKey());
                    }
                }
                break;

            case "1H_Gold":
                if (DataController.instance.playerSaveData.adShop_Gold_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // 광고 횟수 초과
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // 패스 구매자는 광고없이 즉시 보상지급
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gold, ShopPanel.Instance.goldValue_1H);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP 광고횟수 추가
                        DataController.instance.playerSaveData.adShop_Gold_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopGold());
                    }
                }
                break;

            case "1H_SoulStone":
                if (DataController.instance.playerSaveData.adShop_SoulStone_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // 광고 횟수 초과
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // 패스 구매자는 광고없이 즉시 보상지급
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SoulStone, ShopPanel.Instance.soulStoneValue_1H);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP 광고횟수 추가
                        DataController.instance.playerSaveData.adShop_SoulStone_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopSoulStone());
                    }
                }
                break;
        }
    }

    // 오프라인 2배 보상 버튼
    public void RewardedAdsButton()
    {
        // 패스 구매자는 광고시청 없이 2배 보상
        if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
        {
            StartCoroutine(GiveOfflineRewardToPlayer());
        }
        else
        {
            // 무료이용자는 광고를 시청
            StartCoroutine(AdmobManager.instance.StartRewardAds_OfflineReward());
        }
    }

    // 광고 없이 기본 보상 버튼
    public void NoAdRewardButton()
    {
        offlineRewardPanel.SetActive(false);
        // 기본보상
        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gold, goldOffReward);
        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SoulStone, soulStoneOffReward);
        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);
        UIManager.instance.DataReflesh();

        AchievementManager.instance.Add_Achievement_Progress(21, TimeManager.instance.timeAfterLastPlay);
        TimeManager.instance.timeAfterLastPlay = 0;

        if (!DailyLoginRewardManager.instance.dailyFirstLogin)
        {
            SideMenuPanelsParent.instance.dailyLoginRewardPanel.SetActive(true);
            DailyLoginRewardManager.instance.dailyFirstLogin = true;
            DailyLoginRewardManager.instance.RefreshDailyLoginRewardState();
        }
    }

    // 패스 이용자 오프라인 2배 보상 광고 스킵 코루틴
    IEnumerator GiveOfflineRewardToPlayer()
    {
        loadingPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        loadingPanel.SetActive(false);
        offlineRewardPanel.SetActive(false);

        // 보상할거 적기
        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gold, goldOffReward * 2f);
        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SoulStone, soulStoneOffReward * 2f);
        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);
        UIManager.instance.DataReflesh();
        DailyMissionManager.instance.AddProgressDailyMission(11);
        AchievementManager.instance.Add_Achievement_Progress(21, TimeManager.instance.timeAfterLastPlay);
        TimeManager.instance.timeAfterLastPlay = 0;

        if (!DailyLoginRewardManager.instance.dailyFirstLogin)
        {
            SideMenuPanelsParent.instance.dailyLoginRewardPanel.SetActive(true);
            DailyLoginRewardManager.instance.dailyFirstLogin = true;
            DailyLoginRewardManager.instance.RefreshDailyLoginRewardState();
        }
    }

    #endregion

    // 패스 시간 최초 부여
    private void PassRemainTimeInit()
    {
        // 실행후 한번 이상 이 코드가 게임에서 실행되지 않도록 함(게임 실행 후 1회)
        if (TimeManager.instance.passTimerON)
            return;

        // 패스 시간이 아직 부여 안되어있다면 부여한다.(최초 접속시 1회) 
        if (DataController.instance.playerSaveData.passPeriodTime == 0)
        {
            // 28일을 밀리초로 계산
            long millisecondsIn28Days = 28L * 24L * 60L * 60L * 1000L;

            DataController.instance.playerSaveData.passPeriodTime = TimeManager.instance.currentServerTime + millisecondsIn28Days;
        }

        // 패스 시간이 존재하면 남은시간을 TimeManager에 저장한다.
        TimeManager.instance.passPeriodRemainTime = DataController.instance.playerSaveData.passPeriodTime - TimeManager.instance.currentServerTime;

        // 만약 패스 남은시간이 0보다 작으면 패스 초기화 작업이 진행
        if (TimeManager.instance.passPeriodRemainTime <= 0)
        {
            // 패스초기화 작업 메서드
            TimeManager.instance.Init_All_PassData();

            // 시간 초기화
            // 28일을 밀리초로 계산
            long millisecondsIn28Days = 28L * 24L * 60L * 60L * 1000L;

            // 접속한 때의 시간기준으로 초기화
            DataController.instance.playerSaveData.passPeriodTime = TimeManager.instance.currentServerTime + millisecondsIn28Days;
            TimeManager.instance.passPeriodRemainTime = DataController.instance.playerSaveData.passPeriodTime - TimeManager.instance.currentServerTime;
        }

        // 시간관리자의 패스시간 감소 코루틴을 작동한다.
        TimeManager.instance.StartCoroutine(TimeManager.instance.PassTimer());

        TimeManager.instance.passTimerON = true;
    }

    // 300초 주기로 파이어베이스 세이브
    IEnumerator IE_SaveToFirebaseDB()
    {
        yield return new WaitForSecondsRealtime(60f);

        while (true)
        {
            DataController.instance.SaveToFirebaseDB();
            yield return wait_SaveToFirebaseDB;
        }
    }

    // 300초 이상 아무 행동 없을시 방치모드 작동
    IEnumerator IE_SleepModeActivation()
    {
        while (true)
        {
            idletime++;

            if (idletime >= sleepModeTime)
            {
                if (!sleepModePanel.activeSelf)
                    sleepModePanel.SetActive(true);

                idletime = 0f;
            }

            yield return sleepWait;
        }
    }

    // 접속기록을 서버로 전송
    private void SendLoginHistory()
    {
        // 게임실행 후 1회만 전송 || 닉네임이 비어있다면 실행안함
        if (GlobalManager.instance.loginHistory || string.IsNullOrEmpty(DataController.instance.playerSaveData.userNickName))
            return;

        // 전송코드 실행
        FirebaseDatamanager.instance.SendLogInHistory();

        // bool값 true로 변경
        GlobalManager.instance.loginHistory = true;
    }
}
