using Assets.Models;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("MainMenu")]
    #region MainMenu
    public Slider xpSlider;
    public Text lvlText;
    public Text xpText;
    public GameObject VersionPanel;
    public Text Seachingtext;
    #endregion

    [Header("Settings")]
    #region Settings
    public GameObject settings;
    public Toggle wineToggle;
    public Toggle d8Toggle;
    public Toggle ExperimentalToggle;
    public Toggle doubleToggle;
    public Toggle playAsPurple;
    public GameObject CodeText;
    public Slider VolumeSlider;
    public Slider TurnVolumeSlider;

    #endregion

    [Header("Profile")]
    #region Profile
    public GameObject profilePanel;
    public GameObject viewFriends;
    public GameObject removeFriend;
    public Text ProfileText;
    public Text MainProfileText;
    public Text CurrentLevelText;
    public Text DailyWinsText;
    public Text WeeklyWinsText;
    public Text AllCPUWinsText;
    public Text AllPVPWinsText;
    public Text CookedIngredientsText;
    public Text CaloriesText;
    public Text LastLoginText;
    #endregion

    [Header("ConfirmationPopups")]
    #region ConfirmationPopups
    public GameObject alert;
    public GameObject rewardAlert;
    public GameObject rewardAlert2;
    public GameObject rewardAlert3;
    public GameObject rewardAlert4;
    public GameObject LoadingPanel;
    public Text loadingTimer;
    public GameObject ExitPrompt;
    public GameObject NoMatchesFound;
    private bool keepWaiting = false;
    private int wager = 100;
    #endregion
    public static MainMenuController i;
    #region Internal Varibales
    private Profile CurrentPlayer;

    private bool toggleActivated;
    private bool loadingToggle = true; 
    private SqlController sql;

    private float totalElapsed = 0f;
    private float elapsed = 0f;
    private float elapsed2 = 0f;
    private bool LookingForGame = false;
    public List<Sprite> DieSprites;
    public List<Sprite> IngSprites;
    private int tick = 10;
    public Profile YourFriend;
    public GameObject HasMessage;
    public GameObject HasChest;
    public GameObject HasUnlock;
    private AudioSource audioSourceGlobal;


    #endregion
    void Awake()
    {
        i = this;
        sql = new SqlController();
        Global.OnlineGameId = 0;
        Global.FakeOnlineGame = false;
        Global.HardMode = true;
        Global.IsDebug = false;
        if (GameObject.FindGameObjectsWithTag("GameMusic").Length > 0)
        {
            audioSourceGlobal = GameObject.FindGameObjectWithTag("GameMusic").GetComponent<AudioSource>();
            audioSourceGlobal.volume = Global.LoggedInPlayer.MusicVolume;
        }
#if UNITY_EDITOR
        //Settings.IsDebug = true;
#endif
        wineToggle.isOn = Global.LoggedInPlayer.WineMenu;
        d8Toggle.isOn = Global.LoggedInPlayer.UseD8s;
        doubleToggle.isOn = Global.LoggedInPlayer.DisableDoubles;
        playAsPurple.isOn = Global.LoggedInPlayer.PlayAsPurple;
        VolumeSlider.value = Global.LoggedInPlayer.MusicVolume;
        TurnVolumeSlider.value = Global.LoggedInPlayer.TurnVolume;
    }
    private void Start()
    {
        //loading starts as true but is turned false after elements render and toggles are set correctly
        loadingToggle = false;
        toggleActivated = false;
        Global.SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
        StartCoroutine(SetPlayer());
        if (Global.LoggedInPlayer.IsGuest && Global.EnteredGame)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Still a guest?";
            alert.transform.Find("AlertText").GetComponent<Text>().text = $"If you make an account you will get xp, calories, and unlock new rewards!";
            alert.SetActive(true);
            Global.EnteredGame = false;
        } 
        else if (Global.JustWonOnline) 
        {
            Global.JustWonOnline = false;
            rewardAlert2.transform.Find("RewardText").GetComponent<Text>().text = $"You earned a reward for winning your match. Check the reward tab at the bottom to claim it!";
            HasChest.SetActive(true);
            rewardAlert2.SetActive(true);
        }
    }
    
    private void Update()
    {
        elapsed += Time.deltaTime;
        
        if (LoadingPanel.activeInHierarchy) {
            elapsed2 += Time.deltaTime;
            if (elapsed2 >= .5f)
            {
                elapsed2 = elapsed2 % .5f;
                if (Seachingtext.text == "Searching for match.")
                    Seachingtext.text = "Searching for match..";
                else if (Seachingtext.text == "Searching for match..")
                    Seachingtext.text = "Searching for match...";
                else if (Seachingtext.text == "Searching for match...")
                    Seachingtext.text = "Searching for match....";
                else
                    Seachingtext.text = "Searching for match.";
            }
        }
        if (elapsed >= 1f)
        {
            
            elapsed = elapsed % 1f;
            tick++;
            if (LookingForGame)
            {
                totalElapsed++;
                TimeSpan time = TimeSpan.FromSeconds(totalElapsed);
                loadingTimer.text = "Time in queue: " + time.ToString(@"m\:ss");
                StartCoroutine(sql.RequestRoutine($"multiplayer/LookforGame?UserId={Global.LoggedInPlayer.UserId}&Wager={wager}", GetGameUpdate));
                if (!keepWaiting && totalElapsed > 30)
                {
                    NoMatchesFound.SetActive(true);
                }
            }
            else
            {
                totalElapsed = 0;
                loadingTimer.text = "Time in queue: 0:00";
            }

            if (tick > 5)
            {
                tick = 0;
                try
                {
                    StartCoroutine(sql.RequestRoutine($"player/GetAppVersion?UserId={Global.LoggedInPlayer.UserId}", GetAppVersionCallback, true));
                }
                catch (Exception ex)
                {
                    alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Network Failure";
                    alert.transform.Find("AlertText").GetComponent<Text>().text = "Can not connect to server.";
                    alert.SetActive(true);
                }
            }
        }
    }
    public void OnVolumeChanged(bool turn)
    {
        toggleActivated = true;
        if (turn)
        {
            Global.LoggedInPlayer.TurnVolume = TurnVolumeSlider.value;
        }
        else
        {
            Global.LoggedInPlayer.MusicVolume = VolumeSlider.value;
            audioSourceGlobal.volume = VolumeSlider.value;
        }
    }  
    private void GetGameUpdate(string data)
    {
        Global.OnlineGameId = sql.jsonConvert<int>(data);
        if (Global.OnlineGameId != 0)
        {
            LookingForGame = false;
            SceneManager.LoadScene("PlayScene");
        }
    }

    private void StartFakeOnlineGame()
    {
        StopMatchmaking();
        Global.SecondPlayer = new Player() { Username = "Ethan", IsCPU = true, UserId = 42 };
        Global.FakeOnlineGame = true;
        SceneManager.LoadScene("PlayScene");
    }

    private void GetAppVersionCallback(string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            var version = sql.jsonConvert<double>(data);
            if (Global.AppVersion < version)
            {
                StopMatchmaking();
                VersionPanel.SetActive(true);
            }
        }
    }
    public void Matchmaking(bool start)
    {
        if (Global.LoggedInPlayer.IsGuest) 
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Restricted";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "Guests can only play vs the computer, try that or create an account!";
            alert.SetActive(true);
        }
        else if (Global.LoggedInPlayer.Calories < 100)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Insufficent Funds";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "You need 100 calories to play online!";
            alert.SetActive(true);
        }
        else
        {
            if (start)
            {
                LoadingPanel.SetActive(start);
                Seachingtext.gameObject.SetActive(false);
                LookingForGame = false;
                LoadingPanel.transform.Find("StartSearching").gameObject.SetActive(true);
            }
            else
            {
                StopMatchmaking();
            }
        }
    }

    public void LookForGame()
    {
        LoadingPanel.transform.Find("StartSearching").gameObject.SetActive(false);
        Seachingtext.gameObject.SetActive(true);
        LookingForGame = true;
    }
    private void DisplayAlert(string skinDesc, string bannerText)
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = bannerText;
        alert.transform.Find("AlertText").GetComponent<Text>().text = skinDesc;
        alert.SetActive(true);
    }
    public void EditWager(bool more)
    {
        if (!LookingForGame)
        {
            if (more)
            {
                if (Global.LoggedInPlayer.Calories >= wager + 100)
                {
                    wager = wager + 100;
                }
                else
                {
                    DisplayAlert("You don't have that much money, you addict!", "Insufficent Funds");
                }
            }
            else
            {
                if (wager > 100)
                {
                    wager -= 100;
                }
            }
            LoadingPanel.transform.Find("WagerText").GetComponent<Text>().text = wager.ToString();
        }
    }
    private void StopMatchmaking()
    {
        LookingForGame = false;
        keepWaiting = false;
        LoadingPanel.SetActive(false);
        StartCoroutine(sql.RequestRoutine($"multiplayer/StopLookingforGame?UserId={Global.LoggedInPlayer.UserId}"));
    }

    public void showSettings()
    {
        if (!Global.IsConnected)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unable to Connect";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            if (!settings.activeInHierarchy)
            {
                settings.SetActive(true);
            }
        }
    }

    public void ShowProfile(bool open)
    {
        if (!Global.IsConnected)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unable to Connect";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            if (Global.LoggedInPlayer.IsGuest)
            {
                alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Restricted";
                alert.transform.Find("AlertText").GetComponent<Text>().text = "Log in to create a profile!";
                alert.SetActive(true);
            }
            else
            {
                if (!open)
                {
                    SetProfileData(); //reset
                }

                profilePanel.SetActive(open);
            }
        }
    }

    public void ExitMenu(bool open)
    {
        ExitPrompt.SetActive(open);
    } 
    internal IEnumerator SetPlayer()
    {
        yield return StartCoroutine(sql.RequestRoutine($"player/UpdateLevel?UserId={Global.LoggedInPlayer.UserId}", GetLevelUpdateCallback));
        StartCoroutine(sql.RequestRoutine($"player/CheckForReward?UserId={Global.LoggedInPlayer.UserId}", GetRewardCallback));
        StartCoroutine(sql.RequestRoutine($"skin/CheckForUnlocks?UserId={Global.LoggedInPlayer.UserId}", GetTitleUnlockCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Global.LoggedInPlayer.Username}", GetPlayerCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetProfile?UserId={Global.LoggedInPlayer.UserId}", GetProfileCallback));
    }

    public void UpdateLvlText() {
        float xpNeeded = (100 + (Global.LoggedInPlayer.Level * 50));
        lvlText.text = $"Current Level: {Global.LoggedInPlayer.Level}";
        xpText.text = $"XP To Next Level: {Global.LoggedInPlayer.Xp}/{xpNeeded}";
        xpSlider.value = ((float)Global.LoggedInPlayer.Xp / xpNeeded);
    }
    private void GetPlayerCallback(string data)
    {
        var player = sql.jsonConvert<Player>(data);

        if (player.HasNewMessage)
            HasMessage.SetActive(true);
        
        if (player.HasNewChest)
            HasChest.SetActive(true);

        Global.LoggedInPlayer.Calories = player.Calories;
        Global.LoggedInPlayer.Level = player.Level;
        Global.LoggedInPlayer.Xp = player.Xp;
        UpdateLvlText();
    } 
    private void GetProfileCallback(string data)
    {
        CurrentPlayer = sql.jsonConvert<Profile>(data);
        Global.LoggedInPlayer.Wins = CurrentPlayer.AllWins ?? 0;
        SetProfileData();
    }

    private void SetProfileData()
    {
        CurrentLevelText.color = Color.white;
        DailyWinsText.color = Color.white;
        WeeklyWinsText.color = Color.white;
        AllCPUWinsText.color = Color.white;
        AllPVPWinsText.color = Color.white;
        CookedIngredientsText.color = Color.white;
        CaloriesText.color = Color.white;
        LastLoginText.color = Color.white;

        ProfileText.text = $"{CurrentPlayer.Username}'s Profile";
        MainProfileText.text = $"{CurrentPlayer.Username}";
        CurrentLevelText.text = $"";
        DailyWinsText.text = $"Daily CPU Wins: {CurrentPlayer.DailyWins}";
        WeeklyWinsText.text = $"Weekly CPU Wins: {CurrentPlayer.WeeklyWins}";
        AllCPUWinsText.text = $"All CPU Wins: {CurrentPlayer.AllWins}";
        AllPVPWinsText.text = $"All PVP Wins: {CurrentPlayer.AllPVPWins}";
        CookedIngredientsText.text = $"Cooked Ingredients: {CurrentPlayer.Cooked}";
        CaloriesText.text = $"Calories: {CurrentPlayer.Calories}";
        LastLoginText.text = $"Online Status: Online";
        //removeFriend.SetActive(false);
    }

    public void GetFriendProfileCallback(string data)
    {
        YourFriend = sql.jsonConvert<Profile>(data);

        CurrentLevelText.color = Color.white;
        DailyWinsText.color = Color.white;
        WeeklyWinsText.color = Color.white;
        AllCPUWinsText.color = Color.white;
        AllPVPWinsText.color = Color.white;
        CookedIngredientsText.color = Color.white;
        CaloriesText.color = Color.white;
        LastLoginText.color = Color.white;

        ProfileText.text = $"{YourFriend.Username}'s Profile";
        if (YourFriend.Level > CurrentPlayer.Level)
            CurrentLevelText.color = Color.red;
        else if(YourFriend.Level < CurrentPlayer.Level)
            CurrentLevelText.color = Color.green;
        CurrentLevelText.text = $"Level: {YourFriend.Level}";
        if (YourFriend.DailyWins > CurrentPlayer.DailyWins)
            DailyWinsText.color = Color.red;
        else if (YourFriend.DailyWins < CurrentPlayer.DailyWins)
            DailyWinsText.color = Color.green;
        DailyWinsText.text = $"Daily CPU Wins: {YourFriend.DailyWins}";
        if (YourFriend.WeeklyWins > CurrentPlayer.WeeklyWins)
            WeeklyWinsText.color = Color.red;
        else if (YourFriend.WeeklyWins < CurrentPlayer.WeeklyWins)
            WeeklyWinsText.color = Color.green;
        WeeklyWinsText.text = $"Weekly CPU Wins: {YourFriend.WeeklyWins}";
        if (YourFriend.AllWins > CurrentPlayer.AllWins)
            AllCPUWinsText.color = Color.red;
        else if (YourFriend.AllWins < CurrentPlayer.AllWins)
            AllCPUWinsText.color = Color.green;
        AllCPUWinsText.text = $"All CPU Wins: {YourFriend.AllWins}";
        if (YourFriend.AllPVPWins > CurrentPlayer.AllPVPWins)
            AllPVPWinsText.color = Color.red;
        else if (YourFriend.AllPVPWins < CurrentPlayer.AllPVPWins)
            AllPVPWinsText.color = Color.green;
        AllPVPWinsText.text = $"All PVP Wins: {YourFriend.AllPVPWins}";
        if (YourFriend.Cooked > CurrentPlayer.Cooked)
            CookedIngredientsText.color = Color.red;
        else if (YourFriend.Cooked < CurrentPlayer.Cooked)
            CookedIngredientsText.color = Color.green;
        CookedIngredientsText.text = $"Cooked Ingredients: {YourFriend.Cooked}";
        if (YourFriend.Calories > CurrentPlayer.Calories)
            CaloriesText.color = Color.red;
        else if (YourFriend.Calories < CurrentPlayer.Calories)
            CaloriesText.color = Color.green;
        CaloriesText.text = $"Calories: {YourFriend.Calories}";
        if (!YourFriend.IsOnline)
            LastLoginText.color = Color.red;
        else if (YourFriend.IsOnline)
            LastLoginText.color = Color.green;
        LastLoginText.text = $"Online Status: {(YourFriend.IsOnline ? "Online" : "Offline")}";

        //removeFriend.SetActive(true);
        profilePanel.SetActive(true);
    }  

    private void GetRewardCallback(string data)
    {
        var rewardText = sql.jsonConvert<string>(data);      
        if (!String.IsNullOrEmpty(rewardText))
        {
            rewardAlert3.transform.Find("Banner").GetComponentInChildren<Text>().text = "Congrats!";
            rewardAlert3.transform.Find("RewardText").GetComponent<Text>().text = rewardText;
            rewardAlert3.SetActive(true);
        }
    } 
    private void GetLevelUpdateCallback(string data)
    {
        var rewardText = sql.jsonConvert<string>(data);      
        if (!String.IsNullOrEmpty(rewardText))
        {
            rewardAlert4.transform.Find("Banner").GetComponentInChildren<Text>().text = "Congrats!";
            rewardAlert4.transform.Find("RewardText").GetComponent<Text>().text = rewardText;
            rewardAlert4.SetActive(true);
        }
    }
    
    private void GetTitleUnlockCallback(string data)
    {
        var rewardText = sql.jsonConvert<string>(data);      
        if (!String.IsNullOrEmpty(rewardText))
        {
            HasUnlock.SetActive(true);
            Global.hasNewTitle = true;
            rewardAlert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Congrats!";
            rewardAlert.transform.Find("RewardText").GetComponent<Text>().text = rewardText;
            rewardAlert.SetActive(true);
        }
    }

    public void ExitSettings()
    {
        settings.SetActive(false);
        if (toggleActivated)
        {
            StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(Global.LoggedInPlayer.UserId)}&GameVolume={(Global.LoggedInPlayer.MusicVolume)}&TurnVolume={(Global.LoggedInPlayer.TurnVolume)}&WineMenu={(Global.LoggedInPlayer.WineMenu)}&PlayAsPurple={(Global.LoggedInPlayer.PlayAsPurple)}"));
        }
        toggleActivated = false;
    }

    public void TryCode()
    {
       StartCoroutine(sql.RequestRoutine($"skin/UseKey?key={CodeText.GetComponent<InputField>().text}&userId={Global.LoggedInPlayer.UserId}", this.GetCodeCallback));
    }

    private void GetCodeCallback(string data)
    {
        var reward = sql.jsonConvert<int>(data);
        if (reward == 0)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Error";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "This code is invalid or already used!";
            alert.SetActive(true);
        }
        else
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Success";
            alert.transform.Find("AlertText").GetComponent<Text>().text = $"Your code was valid! \n \n you have recieved {reward} Calories!";
            alert.SetActive(true);
            StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Global.LoggedInPlayer.Username}", GetPlayerCallback));
        }
    }
   
    public void toggleSetting(string toggle)
    {
        if (!loadingToggle)
        {
            toggleActivated = true;
            if (toggle == "wine")
            {
                Global.LoggedInPlayer.WineMenu = !Global.LoggedInPlayer.WineMenu;
            }
            else if(toggle == "double")
            {
                Global.LoggedInPlayer.DisableDoubles = !Global.LoggedInPlayer.DisableDoubles;
            }
            else if(toggle == "purple")
            {
                Global.LoggedInPlayer.PlayAsPurple = !Global.LoggedInPlayer.PlayAsPurple;
            } 
        }
    }
    public void SceneChange(string sceneName)
    {
        if (!Global.IsConnected && (sceneName == "Skins" || sceneName == "LeaderboardScene"))
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unable to Connect";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    public void StartCPUGame(bool hardMode)
    {
        Global.HardMode = true;
        SceneManager.LoadScene("PlayScene");
    }
    
    public void OnlineWaitingChoice(bool wait)
    {
        if (wait)
        {
            keepWaiting = true;
        }
        else
        {
            StartFakeOnlineGame();
        }
        NoMatchesFound.SetActive(false);
    }
}
