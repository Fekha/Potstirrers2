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

public class LoginController : MonoBehaviour
{
    public GameObject alert;
    public GameObject password;
    public GameObject username;
    public Text alertText;
    private Guid deviceId;
    private bool rememberMe = false; 
    public Toggle rememberToggle;
    private EventSystem system;
    Player Player;
    private SqlController sql;

    private void Start()
    {
        sql = new SqlController();
        system = EventSystem.current;
        alertText.text = "Sorry, I messed up.. \n \n Until July 1st only playing as guest will work..";
        alert.SetActive(true);
        try {
            FileStream stream = File.Open("idbfs/PotstirrersDevice.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (StreamReader sr = new StreamReader(stream))
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    Guid.TryParse(line, out deviceId);
                }
            }
            if (deviceId == new Guid()) {
                stream = File.Open("idbfs/PotstirrersDevice.json", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                deviceId = Guid.NewGuid();
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(deviceId.ToString());
                }
            }
        
            var url = "player/GetDevice?deviceId=" + deviceId;
            StartCoroutine(sql.RequestRoutine(url, GetDeviceCallback, true));
        }
        catch(Exception ex){}

#if UNITY_EDITOR
        username.GetComponent<InputField>().text = "feca";
        password.GetComponent<InputField>().text = "1234";
#endif

    }
    public void RememberMe()
    {
        rememberMe = !rememberMe;
    }
    private void GetDeviceCallback(string data)
    {
        Settings.IsConnected = true;
        Player = sql.jsonConvert<Player>(data);
        Settings.LoggedInPlayer = Player;
        if (Player != null)
        {
            username.GetComponent<InputField>().text = Player.Username;
            password.GetComponent<InputField>().text = Decrypt(Player.Password);
            rememberToggle.isOn = true;
            rememberMe = true;
        }
    } 
    
    private void GetPlayerCallback(string data)
    {
        Player = sql.jsonConvert<Player>(data);
        Settings.LoggedInPlayer = Player;
        if (Player == null)
        {
            alertText.text = "Username and/or password do not match.";
            alert.SetActive(true);
        }
        else
        {
            alert.SetActive(false);
            Settings.LoggedInPlayer.IsGuest = false;
            SceneManager.LoadScene("MainMenu");
        }
    }
    public void LoginButton()
    {
        if (!String.IsNullOrEmpty(username.GetComponent<InputField>().text) && !String.IsNullOrEmpty(password.GetComponent<InputField>().text))
        {
            alertText.text = "Loading...";
            alert.SetActive(true);
            StartCoroutine(Login());
        }
        else
        {
            if (String.IsNullOrEmpty(username.GetComponent<InputField>().text))
            {
                alertText.text = "Username may not be blank.";
            }
            else
            {
                alertText.text = "Password may not be blank.";
            }
            alert.SetActive(true);
        }

    }
    public void GuestButton()
    {
        Settings.LoggedInPlayer = new Player() { Username = "Guest" };
        Settings.LoggedInPlayer.IsGuest = true;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator Login()
    {
        var url = $"player/LoginUser?username={username.GetComponent<InputField>().text}&rememberMe={rememberMe}&deviceId={deviceId}&password={Encrypt(password.GetComponent<InputField>().text)}";
        yield return StartCoroutine(sql.RequestRoutine(url, GetPlayerCallback, true));  
    }

    private void RegisterButton()
    {
        if (!String.IsNullOrEmpty(username.GetComponent<InputField>().text) && !String.IsNullOrEmpty(password.GetComponent<InputField>().text))
        {
            StartCoroutine(sql.RequestRoutine($"player/RegisterUser?username={username.GetComponent<InputField>().text.Trim()}&password={Encrypt(password.GetComponent<InputField>().text)}&rememberMe={rememberMe}&deviceId={deviceId}", RegisterCallback, true));
        }
        else
        {
            if (String.IsNullOrEmpty(username.GetComponent<InputField>().text))
            {
                alertText.text = "Username may not be blank.";
            }
            else
            {
                alertText.text = "Password may not be blank.";
            }
            alert.SetActive(true);
        }
    }

    private void RegisterCallback(string data)
    {
        Player = sql.jsonConvert<Player>(data);
        if (Player != null)
        {
            Settings.LoggedInPlayer = Player;
            Settings.LoggedInPlayer.IsGuest = false;
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            alertText.text = "Username already taken. Be unique, geez.";
            alert.SetActive(true);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && alert.activeInHierarchy)
        {
            alert.SetActive(false);
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
