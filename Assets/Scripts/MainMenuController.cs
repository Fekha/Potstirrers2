using Assets.Models;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject LoginSecondPlayer;
    public GameObject ExitPrompt;
    public GameObject DifficultyPrompt;
    public GameObject alert;
    public Slider xpSlider;
    public Text lvlText;
    public Text xpText;
    public Text alertText;
    public GameObject usernameText;
    public GameObject settings;
    public GameObject button;
    public Toggle wineToggle;
    public Toggle d8Toggle;
    public Toggle ExperimentalToggle;
    public Toggle doubleToggle;
    public Toggle playAsPurple;
    private bool toggleActivated;
    private bool loadingToggle = true; 
    private SqlController sql;
    private int debugClicks;

    private void Start()
    {
        sql = new SqlController();
        //loading starts as true but is turned false after elements render and toggles are set correctly
        loadingToggle = false;
        toggleActivated = false;
        debugClicks = 0;
        Settings.SecondPlayer = new Player();
        StartCoroutine(SetPlayer());
        if (Settings.LoggedInPlayer.IsGuest && Settings.EnteredGame)
        {
            alertText.text = $"If you enjoyed the game make an account! You can unlock new pieces and dice to play with and compete on the leaderboard.";
            alert.SetActive(true);
            Settings.EnteredGame = false;
        }
    }

    private IEnumerator SetPlayer()
    {
        yield return StartCoroutine(sql.RequestRoutine($"player/UpdateLevel?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        yield return StartCoroutine(sql.RequestRoutine($"player/CheckForReward?UserId={Settings.LoggedInPlayer.UserId}", GetRewardCallback));
        yield return StartCoroutine(sql.RequestRoutine($"player/GetUserByName?username={Settings.LoggedInPlayer.Username}", GetPlayerCallback));
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
    private void GetRewardCallback(string data)
    {
        var rewardText = sql.jsonConvert<string>(data);      
        if (!String.IsNullOrEmpty(rewardText))
        {
            alertText.text = rewardText;
            alert.SetActive(true);
        }
    }
    void Awake()
    {
        wineToggle.isOn = Settings.LoggedInPlayer.WineMenu;
        d8Toggle.isOn = Settings.LoggedInPlayer.UseD8s;
        ExperimentalToggle.isOn = Settings.LoggedInPlayer.Experimental;
        doubleToggle.isOn = Settings.LoggedInPlayer.DisableDoubles;
        playAsPurple.isOn = Settings.LoggedInPlayer.PlayAsPurple;
    }
    public void ExitMenu(bool open)
    {
        ExitPrompt.SetActive(open);
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
            StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(Settings.LoggedInPlayer.UserId)}&WineMenu={(Settings.LoggedInPlayer.WineMenu)}&UseD8s={(Settings.LoggedInPlayer.UseD8s)}&DisableDoubles={(Settings.LoggedInPlayer.DisableDoubles)}&PlayAsPurple={(Settings.LoggedInPlayer.PlayAsPurple)}&Experimental={(Settings.LoggedInPlayer.Experimental)}"));
        }
        toggleActivated = false;
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

    public void exitLogin()
    {
        LoginSecondPlayer.SetActive(false);
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
    public void hideAlert()
    {
        alert.SetActive(false);
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
            else if(toggle == "exp")
            {
                Settings.LoggedInPlayer.Experimental = !Settings.LoggedInPlayer.Experimental;
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
}
