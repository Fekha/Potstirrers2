using Assets.Models;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
    private GameObject challengeFriendButton;
    private GameObject addFriendButton;
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
    private GameObject alertPanel;
    private GameObject loadingPanel;
    public GameObject FriendChallengePanel;
    public GameObject rewardAlert;
    public GameObject rewardAlert2;
    public GameObject rewardAlert3;
    public GameObject rewardAlert4;
    public GameObject MatchmakingPanel;
    public Text loadingTimer;
    public GameObject ExitPrompt;
    public GameObject NoMatchesFound;
    private bool keepWaiting = false;
    private int wager = 100;
    #endregion
    public static MainMenuController i;
    #region Internal Varibales
    private Player CurrentProfile;

    private bool toggleActivated;
    private bool loadingToggle = true; 
    private SqlController sql;

    private float totalElapsed = 0f;
    private float elapsed = 0f;
    private float elapsed2 = 0f;
    private bool LookingForGame = false;
    private bool LookingForFriendGame = false;
    public List<Sprite> DieSprites;
    public List<Sprite> IngSprites;
    private int tick = 10;
    public Player YourFriend;
    public GameObject HasMessage;
    public GameObject HasChest;
    public GameObject HasUnlock;
    private AudioSource audioSourceGlobal;
    private List<GameObject> objectsInScene;
    #endregion
    void Awake()
    {
        i = this;
        sql = new SqlController();
        GetAllObjectsOnlyInScene();
        alertPanel = GetObject("AlertPanel");
        loadingPanel = GetObject("LoadingPanel");
        addFriendButton = GetObject("AddFriendButton");
        challengeFriendButton = GetObject("ChallengeFriendButton");
        Global.Reset();
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
        MainProfileText.text = $"{Global.LoggedInPlayer.Username}";
        UpdateLvlText();
        StartCoroutine(SetPlayer());
        if (Global.LoggedInPlayer.IsGuest && Global.EnteredGame)
        {
            DisplayAlert("Still a guest?", $"If you make an account you will get xp, calories, and unlock new rewards!");
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
        
        if (MatchmakingPanel.activeInHierarchy) {
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
#if UNITY_EDITOR
                totalElapsed = 30f;
#endif
                totalElapsed++;
                TimeSpan time = TimeSpan.FromSeconds(totalElapsed);
                loadingTimer.text = "Time in queue: " + time.ToString(@"m\:ss");
                StartCoroutine(sql.RequestRoutine($"multiplayer/LookforGame?UserId={Global.LoggedInPlayer.UserId}&Wager={wager}", GetGameUpdate));
                if (!keepWaiting && totalElapsed > 30)
                {
                    NoMatchesFound.SetActive(true);
                }
            }
            else if (LookingForFriendGame)
            {
                StartCoroutine(sql.RequestRoutine($"multiplayer/FriendGameStarted?UserId={Global.LoggedInPlayer.UserId}", GetFriendGameUpdate));
            }
            else
            {
                
                totalElapsed = 0;
                loadingTimer.text = "Time in queue: 0:00";
            }

            //try
            //{
            //    StartCoroutine(sql.RequestRoutine($"player/GetAppVersion?UserId={Global.LoggedInPlayer.UserId}", GetAppVersionCallback, true));
            //}
            //catch (Exception ex)
            //{
            //    DisplayAlert("Network Failure", ex.Message);
            //}

            //if (!LookingForFriendGame && !FriendChallengePanel.activeInHierarchy)
            //{
            //    StartCoroutine(sql.RequestRoutine($"multiplayer/CheckForFriendGameInvite?UserId={Global.LoggedInPlayer.UserId}", GetInviteUpdate));
            //}
        }
    }

    internal void OpenFriendProfile(int userId)
    {
        StartCoroutine(sql.RequestRoutine($"player/GetProfile?UserId={userId}", GetFriendProfileCallback, true));
    }

    private GameObject GetObject(string v)
    {
        return objectsInScene.FirstOrDefault(x => x.name == v);
    }
    private void GetAllObjectsOnlyInScene()
    {
        objectsInScene = new List<GameObject>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (!(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                objectsInScene.Add(go);
        }
    }
    public void TutorialButton()
    {
        Global.IsTutorial = true;
        Global.CPUGame = true;
        Global.SecondPlayer = new Player() { Username = "Mike", IsCPU = true, UserId = 43 };
        SceneManager.LoadScene("PlayScene");
    }
    public void OnVolumeChanged(string type)
    {
        toggleActivated = true;
        if (type == "turn")
        {
            Global.LoggedInPlayer.TurnVolume = TurnVolumeSlider.value;
        }
        else if (type == "music")
        {
            Global.LoggedInPlayer.MusicVolume = VolumeSlider.value;
            if (audioSourceGlobal != null)
                audioSourceGlobal.volume = Global.LoggedInPlayer.MusicVolume;
        }
        else if (type == "master")
        {
            Global.LoggedInPlayer.MasterVolume = settings.transform.Find("MasterVolumeSlider").GetComponent<Slider>().value;
            AudioListener.volume = Global.LoggedInPlayer.MasterVolume;
        }
        else if (type == "voice")
        {
            Global.LoggedInPlayer.VoiceVolume = settings.transform.Find("VoiceVolumeSlider").GetComponent<Slider>().value;
        }
        else if (type == "effect")
        {
            Global.LoggedInPlayer.EffectsVolume = settings.transform.Find("EffectsVolumeSlider").GetComponent<Slider>().value;
        }
    }  
    private void GetGameUpdate(string data)
    {
        var gameId = sql.jsonConvert<int>(data);
        if (gameId != 0)
        {
            Global.GameId = gameId;
            LookingForGame = false;
            LookingForFriendGame = false;
            SceneManager.LoadScene("PlayScene");
        }
    }  
    
    private void GetFriendGameUpdate(string data)
    {
        var friendGame = sql.jsonConvert<int?>(data);
        if (friendGame != null)
        {
            if ((int)friendGame != 0)
            {
                Global.GameId = (int)friendGame;
                Global.FriendlyGame = true;
                LookingForGame = false;
                LookingForFriendGame = false;
                SceneManager.LoadScene("PlayScene");
            }
        }
        else
        {
            LookingForGame = false;
            LookingForFriendGame = false;
            HideLoading();
            DisplayAlert("Declined", "Your invite to play was declined.");
        }
    }
    private void GetInviteUpdate(string data)
    {
        var gameId = sql.jsonConvert<int>(data);
        if (gameId != 0)
        {
            Global.GameId = gameId;
            FriendChallengePanel.transform.Find("Body").GetComponent<Text>().text = $"You have been invited to play a friendly game, would you like to play vs them?";
            FriendChallengePanel.SetActive(true);
        }
    }
    public void StartFriendGame()
    {
        LookingForFriendGame = false;
        FriendChallengePanel.SetActive(false);
        StartCoroutine(sql.RequestRoutine($"multiplayer/StartFriendGame?UserId={Global.LoggedInPlayer.UserId}"));
        Global.FriendlyGame = true;
        SceneManager.LoadScene("PlayScene");

    } 
    public void EndFriendGame()
    {
        Global.FriendlyGame = false;
        LookingForFriendGame = false;
        FriendChallengePanel.SetActive(false);
        StartCoroutine(sql.RequestRoutine($"multiplayer/DeclineFriendGame?GameId={Global.GameId}"));
        Global.GameId = 0;
    }
    private void StartFakeOnlineGame()
    {
        StopMatchmaking();
        Global.SecondPlayer = new Player() { Username = "Ethan", IsCPU = true, UserId = 42 };
        Global.FakeOnlineGame = true;
        Global.CPUGame = true;
        SceneManager.LoadScene("PlayScene");
    }

    public void FindOnlineFriends()
    {
        if (Global.LoggedInPlayer.IsGuest)
        {
            DisplayAlert("No Friends","Guests can't make friends to play against, go make an account!");
        }
        else
        {
            Global.OnlyGetOnlineFriends = true;
            TabController.i.TabClicked(2);
            FriendController.i.TabClicked(1);
        }
    } 
    
    public void ChallengeFriend()
    {
        profilePanel.SetActive(false);
        DisplayLoading("Asking Friend", "Waiting for a game response from the other player");
        StartCoroutine(sql.RequestRoutine($"multiplayer/FriendGameInvite?UserId={Global.LoggedInPlayer.UserId}&OtherUserId={YourFriend.UserId}", SentChallengeCallback));
    }

    private void SentChallengeCallback(string data)
    {
        Global.GameId = sql.jsonConvert<int>(data);
        if (Global.GameId != 0)
        {
            LookingForFriendGame = true;
        }
        else
        {
            DisplayAlert("Unavailable", "Your friend is likely already in a game, send them a message!");
            LookingForFriendGame = false;
            HideLoading();
        }
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
            StartFakeOnlineGame();
        }
        else if (Global.LoggedInPlayer.Calories < 100)
        {
            DisplayAlert("Insufficent Funds", $"You need 100 calories to play online but you only have {Global.LoggedInPlayer.Calories}!");
        }
        else
        {
            if (start)
            {
                MatchmakingPanel.SetActive(start);
                Seachingtext.gameObject.SetActive(false);
                LookingForGame = false;
                MatchmakingPanel.transform.Find("StartSearching").gameObject.SetActive(true);
            }
            else
            {
                StopMatchmaking();
            }
        }
    }

    public void LookForGame()
    {
        MatchmakingPanel.transform.Find("StartSearching").gameObject.SetActive(false);
        Seachingtext.gameObject.SetActive(true);
        LookingForGame = true;
    }
    public void DisplayAlert(string title, string body)
    {
        alertPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = title;
        alertPanel.transform.Find("AlertText").GetComponent<Text>().text = body;
        alertPanel.SetActive(true);
    } 
    public void DisplayLoading(string title, string body)
    {
        loadingPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = title;
        loadingPanel.transform.Find("LoadingText").GetComponent<Text>().text = body;
        loadingPanel.SetActive(true);
    }

    public void HideLoading()
    {
        loadingPanel.SetActive(false);
    }

    public void AddFriend()
    {
        StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Global.LoggedInPlayer.UserId}&username={YourFriend.Username}&add={true}", AddFriendCallback));
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
                    DisplayAlert("Insufficent Funds", "You don't have that much money, you addict!");
                }
            }
            else
            {
                if (wager > 100)
                {
                    wager -= 100;
                }
            }
            MatchmakingPanel.transform.Find("WagerText").GetComponent<Text>().text = wager.ToString();
        }
    }
    private void StopMatchmaking()
    {
        LookingForGame = false;
        keepWaiting = false;
        MatchmakingPanel.SetActive(false);
        StartCoroutine(sql.RequestRoutine($"multiplayer/StopLookingforGame?UserId={Global.LoggedInPlayer.UserId}"));
    }

    public void showSettings()
    {
        if (!Global.IsConnected)
        {
            DisplayAlert("Unable to Connect", "This feature requires an active connection to the game server.");
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
            DisplayAlert("Unable to Connect", "This feature requires an active connection to the game server.");
        }
        else
        {
            if (open)
            {
                if (Global.LoggedInPlayer.IsGuest)
                {
                    DisplayAlert("Restricted", "Log in to create a profile!");
                }
                else
                {
                    profilePanel.SetActive(open);
                }
            }
            else
            {
                if (!Global.LoggedInPlayer.IsGuest)
                    SetProfileData(Global.LoggedInPlayer); //reset
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
        StartCoroutine(sql.RequestRoutine($"player/GetProfile?UserId={Global.LoggedInPlayer.UserId}", GetProfileCallback));
    }

    public void UpdateLvlText() {
        float xpNeeded = (100 + (Global.LoggedInPlayer.Level * 50));
        lvlText.text = $"Current Level: {Global.LoggedInPlayer.Level}";
        xpText.text = $"XP To Next Level: {Global.LoggedInPlayer.Xp}/{xpNeeded}";
        xpSlider.value = ((float)Global.LoggedInPlayer.Xp / xpNeeded);
    }
    private void GetProfileCallback(string data)
    {
        CurrentProfile = sql.jsonConvert<Player>(data);
        Global.LoggedInPlayer.Wins = CurrentProfile.AllWins;
        SetProfileData(Global.LoggedInPlayer);

        if (CurrentProfile.HasNewMessage)
            HasMessage.SetActive(true);

        if (CurrentProfile.HasNewChest)
            HasChest.SetActive(true);

        Global.LoggedInPlayer.Calories = CurrentProfile.Calories;
        Global.LoggedInPlayer.Level = CurrentProfile.Level;
        Global.LoggedInPlayer.Xp = CurrentProfile.Xp;
        UpdateLvlText();
        HideLoading();
    }

    private void SetProfileData(Player CurrentProfile)
    {
        CurrentLevelText.color = Color.white;
        DailyWinsText.color = Color.white;
        WeeklyWinsText.color = Color.white;
        AllCPUWinsText.color = Color.white;
        AllPVPWinsText.color = Color.white;
        CookedIngredientsText.color = Color.white;
        CaloriesText.color = Color.white;

        ProfileText.text = $"{CurrentProfile.Username}'s Profile";
        
        CurrentLevelText.text = $"Current Level: {CurrentProfile.Level}"; ;
        DailyWinsText.text = $"Daily CPU Wins: {CurrentProfile.DailyWins}";
        WeeklyWinsText.text = $"Weekly CPU Wins: {CurrentProfile.WeeklyWins}";
        AllCPUWinsText.text = $"All CPU Wins: {CurrentProfile.AllWins}";
        AllPVPWinsText.text = $"All PVP Wins: {CurrentProfile.AllPVPWins}";
        CookedIngredientsText.text = $"Cooked Ingredients: {CurrentProfile.Cooked}";
        CaloriesText.text = $"Calories: {CurrentProfile.Calories}";
        LastLoginText.text = "";

        if (Global.LoggedInPlayer.UserId != CurrentProfile.UserId)
        {
            if (!CurrentProfile.IsOnline)
                LastLoginText.color = Color.red;
            else if (CurrentProfile.IsOnline)
                LastLoginText.color = Color.green;
            LastLoginText.text = $"Online Status: {(CurrentProfile.IsOnline ? "Online" : "Offline")}";

            if (!Global.LoggedInPlayer.IsGuest)
            {
                if (Global.LoggedInPlayer.Friends.Contains(CurrentProfile.UserId))
                {
                    challengeFriendButton.SetActive(true);
                    addFriendButton.SetActive(false);

                    if (CurrentProfile.IsOnline)
                    {
                        challengeFriendButton.GetComponent<Button>().interactable = true;
                        challengeFriendButton.GetComponentInChildren<Text>().color = Color.white;
                    }
                    else
                    {
                        challengeFriendButton.GetComponent<Button>().interactable = false;
                        challengeFriendButton.GetComponentInChildren<Text>().color = Color.grey;
                    }
                }
                else
                {
                    addFriendButton.SetActive(true);
                    challengeFriendButton.SetActive(false);
                }
            }
            else
            {
                addFriendButton.SetActive(false);
                challengeFriendButton.SetActive(false);
            }
        }
       
    }
    private void AddFriendCallback(string data)
    {
        var player = sql.jsonConvert<Friend>(data);
        Global.LoggedInPlayer.Friends.Add(player.UserId);
        OpenFriendProfile(player.UserId);
    }
    internal void GetFriendProfileCallback(string data)
    {
        YourFriend = sql.jsonConvert<Player>(data);
        SetProfileData(YourFriend);
        profilePanel.SetActive(true);
    }  

    private void GetRewardCallback(string rewardText)
    {
        if (!String.IsNullOrEmpty(rewardText))
        {
            rewardAlert3.transform.Find("Banner").GetComponentInChildren<Text>().text = "Congrats!";
            rewardAlert3.transform.Find("RewardText").GetComponent<Text>().text = rewardText;
            rewardAlert3.SetActive(true);
        }
    } 
    private void GetLevelUpdateCallback(string rewardText)
    {    
        if (!String.IsNullOrEmpty(rewardText))
        {
            rewardAlert4.transform.Find("Banner").GetComponentInChildren<Text>().text = "Congrats!";
            rewardAlert4.transform.Find("RewardText").GetComponent<Text>().text = rewardText;
            rewardAlert4.SetActive(true);
        }
    }
    
    private void GetTitleUnlockCallback(string rewardText)
    {    
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
            StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(Global.LoggedInPlayer.UserId)}" +
                $"&MasterVolume={(Global.LoggedInPlayer.MasterVolume)}" +
                $"&MusicVolme={(Global.LoggedInPlayer.MusicVolume)}" +
                $"&TurnVolume={(Global.LoggedInPlayer.TurnVolume)}" +
                $"&VoiceVolume={(Global.LoggedInPlayer.VoiceVolume)}" +
                $"&EffectVolume={(Global.LoggedInPlayer.EffectsVolume)}" +
                $"&WineMenu={(Global.LoggedInPlayer.WineMenu)}" +
                $"&PlayAsPurple={(Global.LoggedInPlayer.PlayAsPurple)}"));
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
            DisplayAlert("Error", "This code is invalid or already used!");
        }
        else
        {
            DisplayAlert("Success", $"Your code was valid! \n \n you have recieved {reward} Calories!");
            StartCoroutine(sql.RequestRoutine($"player/GetUserById?UserId={Global.LoggedInPlayer.UserId}", GetProfileCallback));
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
            alertPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unable to Connect";
            alertPanel.transform.Find("AlertText").GetComponent<Text>().text = "This feature requires an active connection to the game server.";
            alertPanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    public void StartCPUGame()
    {
        Global.CPUGame = true;
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
