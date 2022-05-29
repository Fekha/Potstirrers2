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
    #endregion

    [Header("FriendsList")]
    #region FriendsList
    public GameObject friendslist;
    public GameObject FriendButtonContent;
    public GameObject FriendText;
    private List<Button> FriendButtonLog = new List<Button>();
    #endregion

    [Header("Profile")]
    #region Profile
    public GameObject profilePanel;
    public GameObject viewFriends;
    public GameObject removeFriend;
    public Text ProfileText;
    public Text CurrentLevelText;
    public Text DailyWinsText;
    public Text WeeklyWinsText;
    public Text AllCPUWinsText;
    public Text AllPVPWinsText;
    public Text CookedIngredientsText;
    public Text CaloriesText;
    public Text LastLoginText;
    #endregion

    [Header("Messages")]
    #region Messages
    public GameObject hasMessage;
    public GameObject sendMessagePanel;
    public GameObject messageAlert;
    public GameObject messageChoice;
    public GameObject SubjectInput;
    public GameObject BodyInput;
    public Text ToInput;
    public Dropdown ToDropdown;
    public Text SubjectText;
    public Text BodyText;
    public Text FromText;
    public GameObject messagePanel;
    public GameObject MessageButtonContent;
    public Button ButtonObject;
    private List<Button> MessageButtonLog = new List<Button>();
    #endregion

    [Header("ConfirmationPopups")]
    #region ConfirmationPopups
    public GameObject alert;
    public Text alertText;
    public GameObject ExitPrompt;
    public GameObject DifficultyPrompt;
    public GameObject LoginSecondPlayer;
    public GameObject usernameText;
    #endregion

    #region Internal Varibale
    private Profile CurrentPlayer;
    private Profile YourFriend;
    private bool toggleActivated;
    private bool showFriendList = false;
    private bool loadingToggle = true; 
    private SqlController sql;
    private int debugClicks;
    private Message CurrentMessage;
    #endregion
    void Awake()
    {
        wineToggle.isOn = Settings.LoggedInPlayer.WineMenu;
        d8Toggle.isOn = Settings.LoggedInPlayer.UseD8s;
        doubleToggle.isOn = Settings.LoggedInPlayer.DisableDoubles;
        playAsPurple.isOn = Settings.LoggedInPlayer.PlayAsPurple;
    }
    private void Start()
    {
        sql = new SqlController();
        //loading starts as true but is turned false after elements render and toggles are set correctly
        loadingToggle = false;
        toggleActivated = false;
        debugClicks = 0;
        Settings.SecondPlayer = new Player();
        SetPlayer();
        if (Settings.LoggedInPlayer.IsGuest && Settings.EnteredGame)
        {
            alertText.text = $"If you enjoyed the game make an account! You can unlock new pieces and dice to play with and compete on the leaderboard.";
            alert.SetActive(true);
            Settings.EnteredGame = false;
        }
    }
    public void showSettings()
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

    public void ShowProfile(bool open)
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
                friendslist.SetActive(showFriendList);
                SetProfileData(); //reset
            }

            profilePanel.SetActive(open);
        }
    }

    public void ShowFriendsList(bool open)
    {
        showFriendList = open;
        profilePanel.SetActive(!open);
        friendslist.SetActive(open);
    }
    
    public void ShowMessages(bool open)
    {
        if (Settings.LoggedInPlayer.IsGuest)
        {
            alertText.text = "Log in to send and receive messages!";
            alert.SetActive(true);
        }
        else
        {
            messagePanel.SetActive(open);
        }
    }

    public void ExitMenu(bool open)
    {
        ExitPrompt.SetActive(open);
    } 
    public void ShowSendMessage(bool open)
    {
        if (ToDropdown.options.Count > 0)
            sendMessagePanel.SetActive(open);
        else
        {
            alertText.text = "You must have friends that are friends with you to be able to send a message! Why did you remove poor feca :(";
            alert.SetActive(true);
        }
    }
    public void exitLogin()
    {
        LoginSecondPlayer.SetActive(false);
    }
    public void hideAlert()
    {
        alert.SetActive(false);
    } 
    public void hideMessageAlert()
    {
        messageAlert.SetActive(false);
    } 
    private void showMessageChoice(Message message)
    {
        CurrentMessage = message;
        if (message.IsRead)
        {
            messageChoice.SetActive(true);
        }
        else
        {
            viewMessage();
        }
    }
    public void viewMessage()
    {
        SubjectText.text = CurrentMessage.Subject;
        BodyText.text = CurrentMessage.Body;
        FromText.text = $"From: " + CurrentMessage.FromName;
        messageAlert.SetActive(true);
        messageChoice.SetActive(false);
        StartCoroutine(sql.RequestRoutine($"player/ReadMessage?MessageId={CurrentMessage.MessageId}", GetMessageCallback));
    } 
    public void deleteMessage()
    {
        messageChoice.SetActive(false);
        alertText.text = "Message Deleted!";
        alert.SetActive(true);
        StartCoroutine(sql.RequestRoutine($"player/DeleteMessage?MessageId={CurrentMessage.MessageId}", GetMessageCallback));
    }
    private void SetPlayer()
    {
        StartCoroutine(sql.RequestRoutine($"player/UpdateLevel?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        StartCoroutine(sql.RequestRoutine($"player/CheckForReward?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Settings.LoggedInPlayer.Username}", GetPlayerCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetProfile?username={Settings.LoggedInPlayer.Username}", GetProfileCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetMessages?userId={Settings.LoggedInPlayer.UserId}", GetMessageCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetFriends?userId={Settings.LoggedInPlayer.UserId}", GetFriendCallback));
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
        CurrentLevelText.text = $"";
        DailyWinsText.text = $"Daily CPU Wins: {CurrentPlayer.DailyWins}";
        WeeklyWinsText.text = $"Weekly CPU Wins: {CurrentPlayer.WeeklyWins}";
        AllCPUWinsText.text = $"All CPU Wins: {CurrentPlayer.AllWins}";
        AllPVPWinsText.text = $"All PVP Wins: {CurrentPlayer.AllPVPWins}";
        CookedIngredientsText.text = $"Cooked Ingredients: {CurrentPlayer.Cooked}";
        CaloriesText.text = $"Calories: {CurrentPlayer.Stars}";
        LastLoginText.text = $"";

        viewFriends.SetActive(true);
        removeFriend.SetActive(false);
    }

    private void GetFriendProfileCallback(string data)
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

        viewFriends.SetActive(false);
        removeFriend.SetActive(true);
        profilePanel.SetActive(true);
        friendslist.SetActive(false);
    }  
    private void GetFriendCallback(string data)
    {
        ClearFriends();
        var friends = sql.jsonConvert<List<FriendDTO>>(data);
        foreach (var d in friends.OrderByDescending(x => x.Level))
        {
            CreateFriend(d.Username, d.RealFriend);
        }
    }
    private void CreateFriend(string username, bool realFriend)
    {
        if (realFriend)
        {
            ToDropdown.options.Add(new Dropdown.OptionData()
            {
                text = username
            });
        }
        ButtonObject.transform.Find("Image").gameObject.SetActive(!realFriend);
        ButtonObject.GetComponentInChildren<Text>().text = username;
        Button newButton = Instantiate(ButtonObject, FriendButtonContent.transform);
        newButton.onClick.AddListener(() => StartCoroutine(sql.RequestRoutine($"player/GetProfile?username={username}", GetFriendProfileCallback)));
        FriendButtonLog.Add(newButton);
    }
    private void ClearFriends()
    {
        if (FriendButtonLog.Count() > 0)
        {
            for (int i = FriendButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(FriendButtonLog[i].gameObject);
                FriendButtonLog.Remove(FriendButtonLog[i]);
            }
        }
    }
    private void GetMessageCallback(string data)
    {
        ClearMessages();
        var messages = sql.jsonConvert<List<Message>>(data);
        foreach (var d in messages.OrderByDescending(x => x.CreatedDate))
        {
            CreateMessage(d);
        }
        hasMessage.SetActive(messages.Any(x => !x.IsRead));
    }
    private void CreateMessage(Message message)
    {
        ButtonObject.transform.Find("Image").gameObject.SetActive(!message.IsRead);
        ButtonObject.GetComponentInChildren<Text>().text = message.Subject;
        Button newButton = Instantiate(ButtonObject, MessageButtonContent.transform);
        newButton.onClick.AddListener(() => showMessageChoice(message));
        MessageButtonLog.Add(newButton);
    }
    private void ClearMessages()
    {
        if (MessageButtonLog.Count() > 0)
        {
            for (int i = MessageButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(MessageButtonLog[i].gameObject);
                MessageButtonLog.Remove(MessageButtonLog[i]);
            }
        }
    }

    public void SendMessage()
    {
        if (!String.IsNullOrEmpty(ToInput.text))
        {
            StartCoroutine(sql.RequestRoutine($"player/SendMessage?userId={Settings.LoggedInPlayer.UserId}&toName={ToInput.text}&subject={SubjectInput.GetComponent<InputField>().text}&body={BodyInput.GetComponent<InputField>().text}"));
            sendMessagePanel.SetActive(false);
            alertText.text = "Message sent to " + ToInput.text;
        }
        else
        {
            alertText.text = "Can not send a message without a friend selected!";
        }
        alert.SetActive(true);
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

    public void DifficultyMenu(bool open)
    {
        if (Settings.LoggedInPlayer.Wins > 0 || Settings.LoggedInPlayer.IsGuest)
        {
            DifficultyPrompt.SetActive(open);
        }
        else
        {
            Settings.HardMode = false;
            StartTheGame(true);
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
    public void EditFriend(bool add)
    {
        if (add)
        {
            StartCoroutine(sql.RequestRoutine("player/GetUserByName?username=" + FriendText.GetComponent<InputField>().text, this.GetFriendByUsernameCallback));
        }
        else
        {
            alertText.text = $"You have removed {YourFriend.Username} as a friend :(";
            alert.SetActive(true);
            ShowProfile(false);
            StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Settings.LoggedInPlayer.UserId}&username={YourFriend.Username}&add={add}", GetFriendCallback));
        }
    }

    public void PlayerVsPlayer()
    {
        Settings.HardMode = false;
        if (Settings.LoggedInPlayer.IsGuest)
        {
            StartTheGame(false);
        }
        else
        {
            LoginSecondPlayer.SetActive(true);
        }
    }

    private void GetByUsernameCallback(string data)
    {
        var player = sql.jsonConvert<Player>(data);
        Settings.SecondPlayer = player;
        if (player == null)
        {
            alertText.text = "Username not found.";
            alert.SetActive(true);
        }
        else
        {
            Settings.SecondPlayer.IsGuest = false;
            StartTheGame(false);
        }
    } 
    private void GetFriendByUsernameCallback(string data)
    {
        var player = sql.jsonConvert<Player>(data);
        if (player == null)
        {
            alertText.text = "Player not found.";
            alert.SetActive(true);
        }
        else
        {
            alertText.text = $"You have added {player.Username} as a friend :)";
            alert.SetActive(true);
            FriendText.GetComponent<InputField>().text = "";
            StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Settings.LoggedInPlayer.UserId}&username={player.Username}&add={true}", GetFriendCallback));
        }
    }
    public void Login()
    {

        if (String.IsNullOrEmpty(usernameText.GetComponent<InputField>().text))
        {
            if (String.IsNullOrEmpty(usernameText.GetComponent<InputField>().text))
            {
                alertText.text = "Username may not be blank.";
            }
            alert.SetActive(true);
        }
        else if (usernameText.GetComponent<InputField>().text == Settings.LoggedInPlayer.Username)
        {
            if (string.IsNullOrEmpty(usernameText.GetComponent<InputField>().text))
            {
                alertText.text = "User already in use!";
            }
            alert.SetActive(true);
        }
        else
        {
            StartCoroutine(sql.RequestRoutine("player/GetUserByName?username=" + usernameText.GetComponent<InputField>().text, this.GetByUsernameCallback, true));
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
        SceneManager.LoadScene(sceneName);
    }
    public void StartDebug()
    {
        debugClicks++;
   
        if (debugClicks > 2)
        {
            Settings.IsDebug = true;
            SceneManager.LoadScene("PlayScene");
        }
    }
    public void StartTheGame(bool cpu)
    {
        Settings.EnteredGame = true;
        if (cpu)
        {
            if(Settings.HardMode)
                Settings.SecondPlayer = Settings.CPUPlayers[Settings.CPUPlayers.Count-1];
            else
                Settings.SecondPlayer = Settings.CPUPlayers[UnityEngine.Random.Range(0, Settings.CPUPlayers.Count - 1)];

        }
        else
        {
            Settings.HardMode = false;
            Settings.SecondPlayer = !Settings.SecondPlayer.IsGuest ? Settings.SecondPlayer : new Player() { Username = Settings.LoggedInPlayer.Username+"(2)", playerType = PlayerTypes.HUMAN};
        }

        SceneManager.LoadScene("PlayScene");
    }

    public void StartCPUGame(bool hardMode)
    {
        Settings.HardMode = hardMode;
        StartTheGame(true);
    }

    private class FriendDTO
    {
        public string Username { get; set; }
        public bool RealFriend { get; set; }
        public int Level { get; set; }
    }
}
