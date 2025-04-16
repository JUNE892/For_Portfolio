using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// GameManager�� DataController�� �ٸ��� ���ξ����� ó�� �ҷ������Ƿ� �̸� �̿��� ������ ����ȭ�� �����Ͽ� ����ϸ� ���̵�
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

    [Header("# ���ξ� ���� UI ����")]
    public Slider heroicSpiritSlider;
    public float heroicSpirit_DRate;
    public float maxHeroicSpiritTime;
    public float heroicSpiritTime;
    public bool isBuffActivate;
    public GameObject blessing_Start_Ani, blessing_Start_Lower_Ani, blessingAurora_Ani, blessingAurora_Ani_Front;
    public GameObject[] buffIcons;
    public float goddessBlessing_GoldBuffFactor = 0.5f, goddessBlessing_ExpBuffFactor = 0.5f;

    [Header("# �������")]
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
        //��� ������� �ػ� 9:16�� ����
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
        // ���� ���� �ʱ�ȭ
        BuffInit();

        // �����ʱ�ȭ
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

        // ��ġ��� Ÿ�̸� �۵�
        StartCoroutine(IE_SleepModeActivation());

        // ���� �ʱ�ȭ
        //SoundManager.Instance.SoundInit();

        //����� ���
        SoundManager.Instance.PlaySceneBGM();

        // ù��° �α��� ����
        if (PlayerPrefs.GetInt("FirstLogin") == 0)
            PlayerPrefs.SetInt("FirstLogin", 1);

        // �н������� �ð����ӱ� UI ������Ʈ
        UpdateTimeAcc();

        // ���ӱ��
        SendLoginHistory();
    }
    private void Update()
    {
        // �⺻ ��ġ �ִϸ��̼� ����
        TouchNormalEffect();
    }

    #region About Equipment

    // ��� ��Ȱ��ȭ�� ����г��� �θ������Ʈ�� 1ȸ Ȱ��ȭ�Ͽ� �ν��Ͻ� �ʱ�ȭ
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
    // �н������� �ð����ӱ� UI ������Ʈ
    private void UpdateTimeAcc()
    {
        // ��Ʋ�н� ������
        if (DataController.instance.playerSaveData.buyBattlePass)
            PassReward_TimeACC("BattlePass");

        // �����̾� �н� ������
        if (DataController.instance.playerSaveData.buyPremiumPass)
            PassReward_TimeACC("PremiumPass");
    }

    // �ð� ����� ��� �޼���
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

    // ����ȭ�� ��ư3���� ������
    public void ClickTimeButton(float multiple)
    {
        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);
        oneTime_TimeACC = false;
        twice_TimeACC = false;
        threeTimes_TimeACC = false;

        // 1����� ��� �׻� �⺻ ����
        if (multiple == 1)
        {
            oneTime_TimeACC = true;
            gameSpeed1Ani.SetActive(true);
            TimeAccelerator(1f);
            SoundManager.Instance.PlayTimeSpeedSEF();
        }
        // 2����� ��� AD �Ǵ� ���̾Ƹ� ���� ���� ������ �ٷ� ����, �ƴϸ� ������ ����
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
        // 3����� ��� ���̾Ƹ� ���� ���� ������ �ٷ� ����, �ƴϸ� ������ ����
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

        // ���������г� ������Ʈ
        StageManager.instance.RefreshStageInfo();
    }

    // �ð� ���ӱ� ���� ��ư 3���� ������
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
                    // ���̾� ���� ����
                    DataController.instance.playerSaveData.gem -= 500;

                    // ���� �ݿ�
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

                    // ���� �ݿ�
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

    // ���� ���۽� �ӵ� �ʱ�ȭ
    public void InitGameSpeed()
    {
        gameSpeed1Ani.SetActive(false);
        gameSpeed2Ani.SetActive(false);
        gameSpeed3Ani.SetActive(false);

        gameSpeed1Ani.SetActive(true);
        TimeAccelerator(1f);
    }

    // ���Ӽӵ��� �簳�Ҷ� ���
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

    // �ð� ���ӱ� ���� �ݱ� ��ư
    public void TimeACC_CloseBtn()
    {
        timeAcceleratorPanel.SetActive(false);
        SoundManager.Instance.PlayBackButtonSEF();
    }

    // �ֱ��� �ð����� �ڷ�ƾ
    IEnumerator TimeAcceleratorReduce()
    {
        while (true)
        {
            // �ֱ��� �ð�����
            TimeACCReduce();

            // �����̴� �� UI �ݿ�
            TimeACC_UI_Refresh();
            yield return waitTimeACC;
        }
    }

    // �ֱ��� �ð�����
    private void TimeACCReduce()
    {
        // ��Ʋ�н� �Ǵ� �����̾��н� ���Ž� �ð����� ����
        if (twice_TimeACC && !DataController.instance.playerSaveData.buyBattlePass)
        {
            DataController.instance.playerSaveData.twiceRemainTime--;
            if (DataController.instance.playerSaveData.twiceRemainTime <= 0f)
            {
                DataController.instance.playerSaveData.twiceRemainTime = 0f;

                // 1������� ������ �ٲٰ� ���� �籸�� �����ϰ� ����
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

                // 1������� ������ �ٲٰ� ���� �籸�� �����ϰ� ����
                threeTimesSoldOut.SetActive(false);
                TimeAccelerator(1f);
            }
        }
    }

    // �н����� : �ð�����
    public void PassReward_TimeACC(string passType)
    {
        switch (passType)
        {
            case "BattlePass":
                // �ð��ο� / �����̴� �ݿ� / �ð����ӱ� ���� Sold Out / �ð� �̰���
                DataController.instance.playerSaveData.twiceRemainTime = basicTime;
                break;

            case "PremiumPass":
                DataController.instance.playerSaveData.threeTimesRemainTime = basicTime;
                break;
        }

        // UI ������Ʈ
        TimeACC_UI_Refresh();
    }

    // �����̴� �� UI �ݿ�
    public void TimeACC_UI_Refresh()
    {
        twiceTimeSlider.maxValue = basicTime;
        threeTimesTimeSlider.maxValue = basicTime;

        twiceTimeSlider.value = DataController.instance.playerSaveData.twiceRemainTime;
        threeTimesTimeSlider.value = DataController.instance.playerSaveData.threeTimesRemainTime;

        // �ð� ���� ������ �ؽ�Ʈ UI ����
        float remainTime_Twice = DataController.instance.playerSaveData.twiceRemainTime;
        float remainTime_ThreeTimes = DataController.instance.playerSaveData.threeTimesRemainTime;
        int min_Twice = Mathf.FloorToInt(remainTime_Twice / 60);
        int sec_Twice = Mathf.FloorToInt(remainTime_Twice % 60);
        int min_ThreeTimes = Mathf.FloorToInt(remainTime_ThreeTimes / 60);
        int sec_ThreeTimes = Mathf.FloorToInt(remainTime_ThreeTimes % 60);
        adSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_Twice, sec_Twice);
        twiceSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_Twice, sec_Twice);
        threeTimesSoldOut_Text.text = string.Format("{0:D2}:{1:D2}", min_ThreeTimes, sec_ThreeTimes);

        // ���� �ð��� ���� �������
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
        Debug.Log("������ ���� �� �帥�ð��� " + TimeManager.instance.timeAfterLastPlay + "�� �Դϴ�.");
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

    // �⺻ ��ġ �ִϸ��̼�
    private void TouchNormalEffect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // �⺻ ��ġ �ִϸ��̼� ����
            GameObject touchEffect = M_PoolManager.instance.EffectGet(3, M_PoolManager.instance.prefabPoolParent);
            RectTransform rectTouchEffect = touchEffect.GetComponent<RectTransform>();
            Animator animTouchEffect = touchEffect.GetComponent<Animator>();

            // �ӵ��� ������ ���� �ʰ� ����
            animTouchEffect.updateMode = AnimatorUpdateMode.UnscaledTime;

            // ���� ��ġ ����
            rectTouchEffect.position = new Vector3(Input.mousePosition.x - 58f, Input.mousePosition.y - 58f, 0f);

            // ��ġ�ð� �ʱ�ȭ
            idletime = 0f;
        }
    }

    // ���� ��ġ �ִϸ��̼�
    public void TouchBrokenEffect()
    {
        // �⺻ ��ġ �ִϸ��̼� ����
        GameObject touchEffect = M_PoolManager.instance.EffectGet(2, M_PoolManager.instance.prefabPoolParent);
        RectTransform rectTouchEffect = touchEffect.GetComponent<RectTransform>();
        Animator animTouchEffect = touchEffect.GetComponent<Animator>();

        // �ӵ��� ������ ���� �ʰ� ����
        animTouchEffect.updateMode = AnimatorUpdateMode.UnscaledTime;

        // ���� ��ġ ����
        rectTouchEffect.position = new Vector3(Input.mousePosition.x - 120f, Input.mousePosition.y - 120f, 0f);

        // �������� ����
        UIManager.instance.GoddessBlessPanelOFF(false);
    }

    // ���� ������ �� ȿ�� �ʱ�ȭ
    private void BuffInit()
    {
        for (int i = 0; i < buffIcons.Length; i++)
        {
            buffIcons[i].SetActive(false);
        }

        DataController.instance.playerSaveData.goddessBlessing_GoldBuff = 0;
        DataController.instance.playerSaveData.goddessBlessing_ExpBuff = 0;

        // ���� �г�
        UIManager.instance.buffPanel_Gold.SetActive(false);
        UIManager.instance.buffPanel_EXP.SetActive(false);
    }

    // ���� Ȱ��ȭ
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
                // ������ ��ȣ ��� ����
                DataController.instance.playerSaveData.goddessBlessing_GoldBuff = goddessBlessing_GoldBuffFactor;

                // ������ ����
                buffIcons[6].SetActive(true);

                // ���� �г� ������Ʈ
                UIManager.instance.buffPanel_Gold.SetActive(true);

                // �������� ���� �г� ����
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;

            case "Buff_EXP":
                // ������ ��ȣ ����ġ ����
                DataController.instance.playerSaveData.goddessBlessing_ExpBuff = goddessBlessing_ExpBuffFactor;
                buffIcons[7].SetActive(true);
                UIManager.instance.buffPanel_EXP.SetActive(true);
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;
        }
    }

    // ���� ��Ȱ��ȭ
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
                // ������ ��ȣ ��� ����
                DataController.instance.playerSaveData.goddessBlessing_GoldBuff = 0;

                // ������ ����
                buffIcons[6].SetActive(false);

                // ���� �г� ������Ʈ
                UIManager.instance.buffPanel_Gold.SetActive(false);

                // �������� ���� �г� ����
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;

            case "Buff_EXP":
                // ������ ��ȣ ����ġ ����
                DataController.instance.playerSaveData.goddessBlessing_ExpBuff = 0;
                buffIcons[7].SetActive(false);
                UIManager.instance.buffPanel_EXP.SetActive(false);
                DataController.instance.HeroStatsFormula();
                StageManager.instance.RefreshStageInfo();
                break;
        }
    }

    // ���콺 ��ư Ŭ���� ������ ��ȣ �ð� ����
    public void HeroicSpirit()
    {
        // ��Ű���� �׾��ٸ� ����
        if (!Valkyrie.instance.gameObject.activeSelf)
            return;

        // ���� �ִϸ��̼� ����
        GameObject levelUpEffect = M_PoolManager.instance.EffectGet(1, M_PoolManager.instance.prefabPoolParent);
        levelUpEffect.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        heroicSpiritTime += 10;

        if (heroicSpiritTime >= maxHeroicSpiritTime / 3f && !isBuffActivate)
        {
            // ���� �ɷ�ġ�� �Ʒ��� ������ �ȴ�.
            ActiveBuff("Buff_GOLD");
            ActiveBuff("Buff_EXP");

            // �ִϸ��̼� �۵�
            blessing_Start_Ani.SetActive(true);
            isBuffActivate = true;

            // ȿ����
            SoundManager.Instance.PlayGodBlessSEF();
        }

        if (heroicSpiritTime >= maxHeroicSpiritTime)
        {
            heroicSpiritTime = maxHeroicSpiritTime;
        }

        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // ������ ��ȣ �����̴� �ʱ�ȭ
    private void HeroicSpiritSliderInit()
    {
        heroicSpiritTime = 0f;
        heroicSpiritSlider.maxValue = maxHeroicSpiritTime;
        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.maxValue = maxHeroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // ������ ��ȣ �ð� ����
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

            // �����г� �ݱ�
            UIManager.instance.GoddessBlessPanelOFF(false);
        }

        heroicSpiritSlider.value = heroicSpiritTime;
        UIManager.instance.profile_HeroicSpiritSlider.value = heroicSpiritTime;
    }

    // ������ ��ȣ �ð� ���� �ڷ�ƾ
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

    // ���� UTC0��(�ѱ��ð� ���� 9��) ���̾�̽� Functions�� ���� ��� �������� dailyInit = true�� �ʱ�ȭ
    // 9�� ���Ŀ� �����ϴ� �������� �翬�� DailyInit() �޼��带 ���� ���� �ʱ�ȭ �۾� ����
    // ���� Ŭ���̾�Ʈ�� ������ ���·� �ð��� ������ �ð��������� secondsUntilMidnightUTC ���� 0�̵Ǹ�
    // ���� �ʱ�ȭ ������ ���� ���ָ� �ȴ�.

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
            Debug.Log("dailyInit = false �̹Ƿ� �����ʱ�ȭ�� �ʿ� ����");
        }
    }

    // UTC0�ÿ� �ʱ�ȭ�� ���͵��� �޼��忡 �ִ´�
    void DailyInitList()
    {
        // �귿 �ʱ�ȭ
        DataController.instance.playerSaveData.rouletteCount = 5;
        DataController.instance.playerSaveData.rouletteCount_PlusAD = 5;

        // ���� �ӹ� �ʱ�ȭ
        DailyMissionManager.instance.InitDailyReward();

        // ���� �⼮ �ʱ�ȭ
        DailyLoginRewardManager.instance.dailyFirstLogin = false;
        DataController.instance.playerSaveData.dailyLoginRewardClaim = false;
        if (DataController.instance.playerSaveData.dailyLoginProgress >= 7)
            DataController.instance.playerSaveData.dailyLoginProgress = 0;
        DailyLoginRewardManager.instance.InitDailyLoginContents();
        DailyLoginRewardManager.instance.RefreshDailyLoginRewardState();

        // ���� ���� ���� ���̾� �ʱ�ȭ
        DataController.instance.playerSaveData.freeDia_ADShop = 0;
        DataController.instance.playerSaveData.freeDia_KeyShop = 0;

        // AD SHOP ���� ���� Ƚ�� �ʱ�ȭ
        DataController.instance.playerSaveData.adShop_Dia_AdCount = 0;
        DataController.instance.playerSaveData.adShop_GoldKey_AdCount = 0;
        DataController.instance.playerSaveData.adShop_Gold_AdCount = 0;
        DataController.instance.playerSaveData.adShop_SoulStone_AdCount = 0;
        DataController.instance.playerSaveData.adShop_SupStone_AdCount = 0;

        // ���� �ʱ�ȭ ������ ���� ��¥ ����
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

    // ���ξ��� ���� ��� ���� �޼���
    private void VillageBackGroundChange(int villageNumber)
    {
        bgRender.sprite = village_BG[villageNumber];
        fgRender.sprite = village_FG[villageNumber];
    }

    #endregion
    #region Admob AD Buttons

    // ���� ���� ��ǰ
    public void AD_SHOP_REWARDADS_Button(string type)
    {
        switch (type)
        {
            case "Dia":
                if (DataController.instance.playerSaveData.adShop_Dia_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // ���� Ƚ�� �ʰ�
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // �н� �����ڴ� ������� ��� ��������
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gem, 500);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // �����ӹ� ������ Ƚ�� �߰�
                        DailyMissionManager.instance.AddProgressDailyMission(11);

                        // AD SHOP ����Ƚ�� �߰� �� ���� UI ������Ʈ
                        DataController.instance.playerSaveData.adShop_Dia_AdCount++;
                        ShopPanel.Instance.AllShopUIRefresh();
                    }
                    else
                    {
                        // ���� �̿��ڴ� ���� ���� ��������
                        StartCoroutine(AdmobManager.instance.StartRewardAds_AdShopDia());
                    }
                }
                break;

            case "SupStone":
                if (DataController.instance.playerSaveData.adShop_SupStone_AdCount >= ShopPanel.Instance.maxAdShop_Count)
                {
                    // ���� Ƚ�� �ʰ�
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // �н� �����ڴ� ������� ��� ��������
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SuppressionStone, 10);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP ����Ƚ�� �߰�
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
                    // ���� Ƚ�� �ʰ�
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // �н� �����ڴ� ������� ��� ��������
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.GoldKey, 1);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP ����Ƚ�� �߰�
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
                    // ���� Ƚ�� �ʰ�
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // �н� �����ڴ� ������� ��� ��������
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.Gold, ShopPanel.Instance.goldValue_1H);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP ����Ƚ�� �߰�
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
                    // ���� Ƚ�� �ʰ�
                    SoundManager.Instance.PlayCancelSEF();
                }
                else
                {
                    // �н� �����ڴ� ������� ��� ��������
                    if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
                    {
                        RewardManager.instance.AddRewardItem(RewardManager.RewardType.SoulStone, ShopPanel.Instance.soulStoneValue_1H);
                        RewardManager.instance.DisplayAndReceiveAllRewards(RewardManager.instance.rewardItems);

                        // AD SHOP ����Ƚ�� �߰�
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

    // �������� 2�� ���� ��ư
    public void RewardedAdsButton()
    {
        // �н� �����ڴ� �����û ���� 2�� ����
        if (DataController.instance.playerSaveData.buyBattlePass || DataController.instance.playerSaveData.buyPremiumPass)
        {
            StartCoroutine(GiveOfflineRewardToPlayer());
        }
        else
        {
            // �����̿��ڴ� ���� ��û
            StartCoroutine(AdmobManager.instance.StartRewardAds_OfflineReward());
        }
    }

    // ���� ���� �⺻ ���� ��ư
    public void NoAdRewardButton()
    {
        offlineRewardPanel.SetActive(false);
        // �⺻����
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

    // �н� �̿��� �������� 2�� ���� ���� ��ŵ �ڷ�ƾ
    IEnumerator GiveOfflineRewardToPlayer()
    {
        loadingPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        loadingPanel.SetActive(false);
        offlineRewardPanel.SetActive(false);

        // �����Ұ� ����
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

    // �н� �ð� ���� �ο�
    private void PassRemainTimeInit()
    {
        // ������ �ѹ� �̻� �� �ڵ尡 ���ӿ��� ������� �ʵ��� ��(���� ���� �� 1ȸ)
        if (TimeManager.instance.passTimerON)
            return;

        // �н� �ð��� ���� �ο� �ȵǾ��ִٸ� �ο��Ѵ�.(���� ���ӽ� 1ȸ) 
        if (DataController.instance.playerSaveData.passPeriodTime == 0)
        {
            // 28���� �и��ʷ� ���
            long millisecondsIn28Days = 28L * 24L * 60L * 60L * 1000L;

            DataController.instance.playerSaveData.passPeriodTime = TimeManager.instance.currentServerTime + millisecondsIn28Days;
        }

        // �н� �ð��� �����ϸ� �����ð��� TimeManager�� �����Ѵ�.
        TimeManager.instance.passPeriodRemainTime = DataController.instance.playerSaveData.passPeriodTime - TimeManager.instance.currentServerTime;

        // ���� �н� �����ð��� 0���� ������ �н� �ʱ�ȭ �۾��� ����
        if (TimeManager.instance.passPeriodRemainTime <= 0)
        {
            // �н��ʱ�ȭ �۾� �޼���
            TimeManager.instance.Init_All_PassData();

            // �ð� �ʱ�ȭ
            // 28���� �и��ʷ� ���
            long millisecondsIn28Days = 28L * 24L * 60L * 60L * 1000L;

            // ������ ���� �ð��������� �ʱ�ȭ
            DataController.instance.playerSaveData.passPeriodTime = TimeManager.instance.currentServerTime + millisecondsIn28Days;
            TimeManager.instance.passPeriodRemainTime = DataController.instance.playerSaveData.passPeriodTime - TimeManager.instance.currentServerTime;
        }

        // �ð��������� �н��ð� ���� �ڷ�ƾ�� �۵��Ѵ�.
        TimeManager.instance.StartCoroutine(TimeManager.instance.PassTimer());

        TimeManager.instance.passTimerON = true;
    }

    // 300�� �ֱ�� ���̾�̽� ���̺�
    IEnumerator IE_SaveToFirebaseDB()
    {
        yield return new WaitForSecondsRealtime(60f);

        while (true)
        {
            DataController.instance.SaveToFirebaseDB();
            yield return wait_SaveToFirebaseDB;
        }
    }

    // 300�� �̻� �ƹ� �ൿ ������ ��ġ��� �۵�
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

    // ���ӱ���� ������ ����
    private void SendLoginHistory()
    {
        // ���ӽ��� �� 1ȸ�� ���� || �г����� ����ִٸ� �������
        if (GlobalManager.instance.loginHistory || string.IsNullOrEmpty(DataController.instance.playerSaveData.userNickName))
            return;

        // �����ڵ� ����
        FirebaseDatamanager.instance.SendLogInHistory();

        // bool�� true�� ����
        GlobalManager.instance.loginHistory = true;
    }
}
