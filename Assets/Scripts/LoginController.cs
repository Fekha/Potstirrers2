using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using Assets.Models;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class LoginController : MonoBehaviour
{
    public GameObject alertPanel;
    public GameObject VersionPanel;
    public GameObject password;
    public GameObject username;
    public Text AppVersion;
    private Guid deviceId;
    private bool rememberMe = false; 
    public Toggle rememberToggle;
    private EventSystem system;
    private Assets.Models.Player Player;
    private SqlController sql;
    private float elapsed;
    private float elapsed2;
    private int tick = 10;
    private bool isLoading = false;
    private string isLoadingPhrase = "";
    private bool isGooglePlay = false;

    private void Awake()
    {
        Global.Reset();
        Global.EnteredGame = false;
        AppVersion.text = "Ver. " + Global.AppVersion.ToString();
        sql = new SqlController();
        system = EventSystem.current;
    }

    private void Start()
    {
#if UNITY_ANDROID
            DisplayAlert("Loading", "Logging in with Google Play", true);
            PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#else
        CheckRememberMe();
#endif

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && alertPanel.activeInHierarchy && !isLoading)
        {
            HideAlert();
        }
        if (!String.IsNullOrEmpty(isLoadingPhrase))
        {
            elapsed2 += Time.deltaTime;
            if (elapsed2 >= .5f)
            {
                elapsed2 = elapsed2 % .5f;
                var text = alertPanel.transform.Find("AlertText").GetComponent<Text>().text;
                if (text == $"{isLoadingPhrase}")
                    alertPanel.transform.Find("AlertText").GetComponent<Text>().text = $"{isLoadingPhrase}.";
                else if (text == $"{isLoadingPhrase}.")
                    alertPanel.transform.Find("AlertText").GetComponent<Text>().text = $"{isLoadingPhrase}..";
                else if (text == $"{isLoadingPhrase}..")
                    alertPanel.transform.Find("AlertText").GetComponent<Text>().text = $"{isLoadingPhrase}...";
                else
                    alertPanel.transform.Find("AlertText").GetComponent<Text>().text = $"{isLoadingPhrase}";
            }
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                    inputfield.OnPointerClick(new PointerEventData(system));

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
            }
        }
        elapsed += Time.deltaTime;
        if (elapsed >= 1f)
        {
            elapsed = elapsed % 1f;
            tick++;
            if (tick > 5)
            {
                tick = 0;
                try{
                    StartCoroutine(sql.RequestRoutine($"player/GetAppVersion", GetAppVersionCallback, true));
                }
                catch (Exception ex)
                {
                    DisplayAlert("Network Failure", ex.Message);
                }
            }
        }
    }
#if UNITY_ANDROID
    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            isGooglePlay = true;
            //DisplayAlert("Success", $"Logged in {PlayGamesPlatform.Instance.GetUserDisplayName()} with Google Play");
            username.GetComponent<InputField>().text = PlayGamesPlatform.Instance.GetUserDisplayName();
            StartCoroutine(sql.RequestRoutine($"player/LoginUser?username={username.GetComponent<InputField>().text}&rememberMe={rememberMe}&deviceId={deviceId}&password={Encrypt(password.GetComponent<InputField>().text)}&isGooglePlay={isGooglePlay}", GetPlayerCallback, true));
        }
        else
        {
            DisplayAlert("Failure", "Couldn't connect to google play, try manual login.");
        }
    }
#endif

    private void CheckRememberMe()
    {
        //alert.transform.Find("AlertText").GetComponent<Text>().text = "Sorry, I messed up.. \n \n Until July 1st only playing as guest will work..";
        //alert.SetActive(true);
        DisplayAlert("Loading", "Checking if we recognize you", true);
        try
        {
            FileStream stream = File.Open("idbfs/PotstirrersDevice.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (StreamReader sr = new StreamReader(stream))
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    Guid.TryParse(line, out deviceId);
                }
            }
            if (deviceId == new Guid())
            {
                stream = File.Open("idbfs/PotstirrersDevice.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                deviceId = Guid.NewGuid();
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(deviceId.ToString());
                }
            }

            StartCoroutine(sql.RequestRoutine($"player/GetDevice?deviceId={deviceId}", GetDeviceCallback, true));
        }
        catch (Exception)
        {
            HideAlert();
        }

#if UNITY_EDITOR
        username.GetComponent<InputField>().text = "Feca";
        password.GetComponent<InputField>().text = "1234";
        HideAlert();
#endif
    }

    public void DisplayAlert(string title, string body, bool isLoadingPanel = false)
    {
        isLoading = isLoadingPanel;
        if (isLoadingPanel)
        {
            isLoadingPhrase = body;
        }
        else
        {
            isLoadingPhrase = "";
        }
        alertPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = title;
        alertPanel.transform.Find("AlertText").GetComponent<Text>().text = body;
        alertPanel.SetActive(true);
    }
    public void HideAlert()
    {
        isLoading = false;
        isLoadingPhrase = "";
        alertPanel.SetActive(false);
    }
    public void RememberMe()
    {
        rememberMe = !rememberMe;
    }
    private void GetDeviceCallback(string data)
    {
        Player = sql.jsonConvert<Assets.Models.Player>(data);
        Global.LoggedInPlayer = Player;
        HideAlert();
        if (Player != null)
        {
            username.GetComponent<InputField>().text = Player.Username;
            password.GetComponent<InputField>().text = Decrypt(Player.Password);
            rememberToggle.isOn = true;
            rememberMe = true;
        }
    } 
    private void GetAppVersionCallback(string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            Global.IsConnected = true;
            var version = sql.jsonConvert<double>(data);
            if (Global.AppVersion < version)
            {
                VersionPanel.SetActive(true);
            }
        }
        else
        {
            Global.IsConnected = false;
        }
    } 
  
    private bool CheckForValidFields()
    {
        if (Global.IsConnected)
        {
            if (!String.IsNullOrEmpty(username.GetComponent<InputField>().text) && !String.IsNullOrEmpty(password.GetComponent<InputField>().text))
            {
                DisplayAlert("Loading", "Any second now", true);
                return true;
            }
            else
            {
                DisplayAlert("Invalid", $"{(String.IsNullOrEmpty(username.GetComponent<InputField>().text) ? "Username" : "Password")} may not be blank.");
                return false;
            }
        }
        else
        {
            DisplayAlert("Not Connected", "Could not connect to server");
            return false;
        }
    }
    public void LoginButton()
    {
        if (CheckForValidFields())
        {
            StartCoroutine(sql.RequestRoutine($"player/LoginUser?username={username.GetComponent<InputField>().text}&rememberMe={rememberMe}&deviceId={deviceId}&password={Encrypt(password.GetComponent<InputField>().text)}", GetPlayerCallback, true));
        }
    }

    private void GetPlayerCallback(string data)
    {
        Player = sql.jsonConvert<Player>(data);
        if (Player != null)
        {
            Global.LoggedInPlayer = Player;
            Global.LoggedInPlayer.IsGuest = false;
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            if (isGooglePlay)
            {
                StartCoroutine(sql.RequestRoutine($"player/RegisterUser?username={username.GetComponent<InputField>().text.Trim()}&password={Encrypt("G00GL3PlAyLooogin123")}&rememberMe={rememberMe}&deviceId={deviceId}", RegisterCallback, true));
            }
            else
            {
                DisplayAlert("Failure", "Username and/or password do not match.");
            }
        }
    }

    public void RegisterButton()
    {
        if (CheckForValidFields())
        {
            StartCoroutine(sql.RequestRoutine($"player/RegisterUser?username={username.GetComponent<InputField>().text.Trim()}&password={Encrypt(password.GetComponent<InputField>().text)}&rememberMe={rememberMe}&deviceId={deviceId}", RegisterCallback, true));
        }
    }

    private void RegisterCallback(string data)
    {
        Player = sql.jsonConvert<Assets.Models.Player>(data);
        if (Player != null)
        {
            Global.LoggedInPlayer = Player;
            Global.LoggedInPlayer.IsGuest = false;
            Global.IsTutorial = true;
            Global.CPUGame = true;
            Global.SecondPlayer = new Assets.Models.Player() { Username = "Mike", IsCPU = true, UserId = 43 };
            SceneManager.LoadScene("PlayScene");
        }
        else
        {
            DisplayAlert("Failure", $"Username already taken. Be unique, geez.");
        }
    }
    public void GuestButton()
    {
        Global.LoggedInPlayer = new Assets.Models.Player() { Username = "Guest" + UnityEngine.Random.Range(1000, 10000) };
        Global.LoggedInPlayer.IsGuest = true;
        Global.IsTutorial = true;
        Global.CPUGame = true;
        Global.SecondPlayer = new Assets.Models.Player() { Username = "Mike", IsCPU = true, UserId = 43 };
        SceneManager.LoadScene("PlayScene");
    }

    public static string Encrypt(string encryptString)
    {
        string EncryptionKey = Secrets.secretKey;
        byte[] clearBytes = Encoding.Unicode.GetBytes(encryptString);
        using (Aes encryptor = Aes.Create())
        {
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearBytes, 0, clearBytes.Length);
                    cs.Close();
                }
                encryptString = Convert.ToBase64String(ms.ToArray());
            }
        }
        return encryptString;
    }

    public static string Decrypt(string cipherText)
    {
        string EncryptionKey = Secrets.secretKey;
        cipherText = cipherText.Replace(" ", "+");
        byte[] cipherBytes = Convert.FromBase64String(cipherText);
        using (Aes encryptor = Aes.Create())
        {
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.Close();
                }
                cipherText = Encoding.Unicode.GetString(ms.ToArray());
            }
        }
        return cipherText;
    }
}

