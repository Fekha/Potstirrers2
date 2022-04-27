using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject LoginSecondPlayer;
    public GameObject ExitPrompt;
    public GameObject alert;
    public Slider xpSlider;
    public Text lvlText;
    public Text xpText;
    public Text alertText;
    public Text usernameText;
    public GameObject settings;
    public GameObject button;
    public Toggle wineToggle;
    public Toggle d8Toggle;
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
        StartCoroutine(sql.RequestRoutine($"player/UpdateLevel?UserId={Settings.LoggedInPlayer.UserId}", GetPlayerCallback));

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
        if (player.Xp != Settings.LoggedInPlayer.Xp && player.Level != Settings.LoggedInPlayer.Level)
        {
            var starsGained = player.Stars - Settings.LoggedInPlayer.Stars;
            alertText.text = $"Congrats you hit level {Settings.LoggedInPlayer.Level}! You gained {starsGained} Calories!";
            alert.SetActive(true);
        }
        Settings.LoggedInPlayer.Stars = player.Stars;
        Settings.LoggedInPlayer.Level = player.Level;
        Settings.LoggedInPlayer.Xp = player.Xp;
        UpdateLvlText();
    }
    void Awake()
    {
        wineToggle.isOn = global::Settings.LoggedInPlayer.WineMenu;
        d8Toggle.isOn = global::Settings.LoggedInPlayer.UseD8s;
        doubleToggle.isOn = global::Settings.LoggedInPlayer.DisableDoubles;
        playAsPurple.isOn = global::Settings.LoggedInPlayer.PlayAsPurple;
    }
    public void ExitMenu(bool open)
    {
        ExitPrompt.SetActive(open);
    }

    public void ExitSettings()
    {
        settings.SetActive(false);
        if (!Settings.LoggedInPlayer.IsGuest && toggleActivated)
        {
            StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(global::Settings.LoggedInPlayer.UserId)}&WineMenu={(global::Settings.LoggedInPlayer.WineMenu)}&UseD8s={(global::Settings.LoggedInPlayer.UseD8s)}&DisableDoubles={(global::Settings.LoggedInPlayer.DisableDoubles)}&PlayAsPurple={(global::Settings.LoggedInPlayer.PlayAsPurple)}"));
        }
        toggleActivated = false;
    }
    public void PlayerVsPlayer()
    {
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
        global::Settings.SecondPlayer = player;
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

        if (String.IsNullOrEmpty(usernameText.text))
        {
            if (String.IsNullOrEmpty(usernameText.text))
            {
                alertText.text = "Username may not be blank.";
            }
            alert.SetActive(true);
        }
        else if (usernameText.text == global::Settings.LoggedInPlayer.Username)
        {
            if (string.IsNullOrEmpty(usernameText.text))
            {
                alertText.text = "User already in use!";
            }
            alert.SetActive(true);
        }
        else
        {
            StartCoroutine(sql.RequestRoutine("player/GetUserByName?username=" + usernameText.text, this.GetByUsernameCallback, true));
        }
    }
    public void showSettings()
    {
        if (global::Settings.LoggedInPlayer.IsGuest)
        {
            alertText.text = "Log in to edit settings!";
            alert.SetActive(true);
        }
        else if (global::Settings.LoggedInPlayer.Wins == 0)
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
                global::Settings.LoggedInPlayer.WineMenu = !global::Settings.LoggedInPlayer.WineMenu;
            }
            else if (toggle == "d8")
            {
                global::Settings.LoggedInPlayer.UseD8s = !global::Settings.LoggedInPlayer.UseD8s;
            }
            else if(toggle == "double")
            {
                global::Settings.LoggedInPlayer.DisableDoubles = !global::Settings.LoggedInPlayer.DisableDoubles;
            }
            else if(toggle == "purple")
            {
                global::Settings.LoggedInPlayer.PlayAsPurple = !global::Settings.LoggedInPlayer.PlayAsPurple;
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
        global::Settings.PlayingPlayers[0] = global::Settings.CPUPlayers[0];
        global::Settings.PlayingPlayers[1] = global::Settings.CPUPlayers[1];

        if (debugClicks > 2)
        {
            global::Settings.IsDebug = true;
            SceneManager.LoadScene("PlayScene");
        }
    }
    public void StartTheGame(bool cpu)
    {
        if (cpu)
        {
            global::Settings.PlayingPlayers[0] = global::Settings.LoggedInPlayer;
            global::Settings.PlayingPlayers[1] = global::Settings.CPUPlayers[UnityEngine.Random.Range(0, global::Settings.CPUPlayers.Count)];
        }
        else
        {
            global::Settings.PlayingPlayers[0] = global::Settings.LoggedInPlayer;
            global::Settings.PlayingPlayers[1] = global::Settings.SecondPlayer.UserId != 0 ? global::Settings.SecondPlayer : new Player() { Username = global::Settings.LoggedInPlayer.Username+"(2)", playerType = PlayerTypes.HUMAN};
        }

        SceneManager.LoadScene("PlayScene");
    }
}
