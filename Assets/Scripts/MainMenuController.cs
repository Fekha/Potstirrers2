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
    public Text alertText;
    public GameObject LoadingPanel;
    public Text loadingTimer;
    public GameObject ExitPrompt;
    public GameObject DifficultyPrompt;
    public GameObject LoginSecondPlayer;
    public GameObject usernameText;
    #endregion
    public static MainMenuController i;
    #region Internal Varibales
    private Profile CurrentPlayer;

    private bool toggleActivated;
    private bool loadingToggle = true; 
    private SqlController sql;

    private float totalElapsed = 0f;
    private float elapsed = 0f;
    private bool LookingForGame = false;
    #endregion
    void Awake()
    {
        i = this;
        sql = new SqlController();
        Settings.OnlineGameId = 0;
        wineToggle.isOn = Settings.LoggedInPlayer.WineMenu;
        d8Toggle.isOn = Settings.LoggedInPlayer.UseD8s;
        doubleToggle.isOn = Settings.LoggedInPlayer.DisableDoubles;
        playAsPurple.isOn = Settings.LoggedInPlayer.PlayAsPurple;
    }
    private void Start()
    {
        //loading starts as true but is turned false after elements render and toggles are set correctly
        loadingToggle = false;
        toggleActivated = false;
        Settings.SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
        StartCoroutine(SetPlayer());
        if (Settings.LoggedInPlayer.IsGuest && Settings.EnteredGame)
        {
            alertText.text = $"If you enjoyed the game make an account! You can unlock new pieces and dice to play with and compete on the leaderboard.";
            alert.SetActive(true);
            Settings.EnteredGame = false;
        }
    }
    private void GetGameUpdate(string data)
    {
        Settings.OnlineGameId = sql.jsonConvert<int>(data);
        if (Settings.OnlineGameId != 0)
        {
            LookingForGame = false;
            Settings.HardMode = false;
            SceneManager.LoadScene("PlayScene");
        }
    }
    private void Update()
    {
        if (LookingForGame)
        {
            elapsed += Time.deltaTime;
            totalElapsed += Time.deltaTime;
            if (elapsed >= 1f)
            {
                TimeSpan time = TimeSpan.FromSeconds(totalElapsed);
                loadingTimer.text = "Time in queue: " + time.ToString(@"mm\:ss");
                elapsed = elapsed % 1f;
                if(sql == null)
                    sql = new SqlController();
                StartCoroutine(sql.RequestRoutine($"analytic/LookforGame?UserId={Settings.LoggedInPlayer.UserId}", GetGameUpdate));
            }
        }
        else
        {
            totalElapsed = 0;
        }
       
    }
    public void Matchmaking(bool start)
    {
        LookingForGame = start;
        LoadingPanel.SetActive(start);
        if (!start)
        {
            StartCoroutine(sql.RequestRoutine($"analytic/StopLookingforGame?UserId={Settings.LoggedInPlayer.UserId}"));
        }
    }
    public void showSettings()
    {
        if (!Settings.IsConnected)
        {
            alertText.text = "Unable to connect! \n \n This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            if (Settings.LoggedInPlayer.IsGuest)
            {
                alertText.text = "Log in to edit settings!";
                alert.SetActive(true);
            }
            else if (Settings.LoggedInPlayer.Wins == 0)
            {
                alertText.text = "Win a game to access additional settings!";
                alert.SetActive(true);
            }
            else if (!settings.activeInHierarchy)
            {
                settings.SetActive(true);
            }
        }
    }

    public void ShowProfile(bool open)
    {
        if (!Settings.IsConnected)
        {
            alertText.text = "Unable to connect! \n \n This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            if (Settings.LoggedInPlayer.IsGuest)
            {
                alertText.text = "Log in to create a profile!";
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

    public void exitLogin()
    {
        LoginSecondPlayer.SetActive(false);
    }
    public void hideAlert()
    {
        alert.SetActive(false);
    } 

    private IEnumerator SetPlayer()
    {
        yield return StartCoroutine(sql.RequestRoutine($"player/UpdateLevel?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        yield return StartCoroutine(sql.RequestRoutine($"player/CheckForReward?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Settings.LoggedInPlayer.Username}", GetPlayerCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetProfile?username={Settings.LoggedInPlayer.Username}", GetProfileCallback));

    }

    public void UpdateLvlText() {
        float xpNeeded = (300 + (Settings.LoggedInPlayer.Level * 25));
        lvlText.text = $"Current Level: {Settings.LoggedInPlayer.Level}";
        xpText.text = $"XP To Next Level: {Settings.LoggedInPlayer.Xp}/{xpNeeded}";
        xpSlider.value = ((float)Settings.LoggedInPlayer.Xp / xpNeeded);
    }
    private void GetPlayerCallback(string data)
    {
        var player = sql.jsonConvert<Player>(data);
        Settings.LoggedInPlayer.Stars = player.Stars;
        Settings.LoggedInPlayer.Level = player.Level;
        Settings.LoggedInPlayer.Xp = player.Xp;
        UpdateLvlText();
    } 
    private void GetProfileCallback(string data)
    {
        CurrentPlayer = sql.jsonConvert<Profile>(data);
        Settings.LoggedInPlayer.Wins = CurrentPlayer.AllWins ?? 0;
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
        CaloriesText.text = $"Calories: {CurrentPlayer.Stars}";
        LastLoginText.text = $"";
    }

    public void GetFriendProfileCallback(string data)
    {
        var YourFriend = sql.jsonConvert<Profile>(data);

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
        if (YourFriend.Stars > CurrentPlayer.Stars)
            CaloriesText.color = Color.red;
        else if (YourFriend.Stars < CurrentPlayer.Stars)
            CaloriesText.color = Color.green;
        CaloriesText.text = $"Calories: {YourFriend.Stars}";
        if (YourFriend.LastLogin < CurrentPlayer.LastLogin)
            LastLoginText.color = Color.red;
        else if (YourFriend.LastLogin > CurrentPlayer.LastLogin)
            LastLoginText.color = Color.green;
        LastLoginText.text = YourFriend.LastLogin.HasValue ? $"Last Login: {YourFriend.LastLogin.Value.ToShortDateString()} {YourFriend.LastLogin.Value.ToShortTimeString()}" : "";

        profilePanel.SetActive(true);
    }  

    private void GetRewardCallback(string data)
    {
        var rewardText = sql.jsonConvert<string>(data);      
        if (!String.IsNullOrEmpty(rewardText))
        {
            alertText.text = rewardText;
            alert.SetActive(true);
        }
    }

    public void ExitSettings()
    {
        settings.SetActive(false);
        if (!Settings.LoggedInPlayer.IsGuest && toggleActivated)
        {
            StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(Settings.LoggedInPlayer.UserId)}&WineMenu={(Settings.LoggedInPlayer.WineMenu)}&UseD8s={(Settings.LoggedInPlayer.UseD8s)}&DisableDoubles={(Settings.LoggedInPlayer.DisableDoubles)}&PlayAsPurple={(Settings.LoggedInPlayer.PlayAsPurple)}"));
        }
        toggleActivated = false;
    }

    public void TryCode()
    {
       StartCoroutine(sql.RequestRoutine($"purchase/UseKey?key={CodeText.GetComponent<InputField>().text}&userId={Settings.LoggedInPlayer.UserId}", this.GetCodeCallback));
    }

    private void GetCodeCallback(string data)
    {
        var reward = sql.jsonConvert<int>(data);
        if (reward == 0)
        {
            alertText.text = "This code is invalid or already used!";
            alert.SetActive(true);
        }
        else
        {
            alertText.text = $"Your code was valid! \n \n you have recieved {reward} Calories!";
            alert.SetActive(true);
            StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Settings.LoggedInPlayer.Username}", GetPlayerCallback));
        }
    }
   
    public void toggleSetting(string toggle)
    {
        if (!loadingToggle)
        {
            toggleActivated = true;
            if (toggle == "wine")
            {
                Settings.LoggedInPlayer.WineMenu = !Settings.LoggedInPlayer.WineMenu;
            }
            else if (toggle == "d8")
            {
                Settings.LoggedInPlayer.UseD8s = !Settings.LoggedInPlayer.UseD8s;
            }
            else if(toggle == "double")
            {
                Settings.LoggedInPlayer.DisableDoubles = !Settings.LoggedInPlayer.DisableDoubles;
            }
            else if(toggle == "purple")
            {
                Settings.LoggedInPlayer.PlayAsPurple = !Settings.LoggedInPlayer.PlayAsPurple;
            } 
        }
    }
    public void SceneChange(string sceneName)
    {
        if (!Settings.IsConnected && (sceneName == "Skins" || sceneName == "LeaderboardScene"))
        {
            alertText.text = "Unable to connect! \n \n This feature requires an active connection to the game server.";
            alert.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    public void StartCPUGame(bool hardMode)
    {
        Settings.HardMode = true;
        Settings.EnteredGame = true;
        SceneManager.LoadScene("PlayScene");
    }


}
