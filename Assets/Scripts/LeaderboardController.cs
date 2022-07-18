using Assets.Models;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardController : MonoBehaviour
{
    [System.Serializable]
    public class LeaderboardMessage
    {
        public string text;
        public Text textObject;
    }
    public Text headerText;
    public GameObject eventLogTextContent;
    public GameObject alert;
    public Text eventLogTextObject;
    private List<LeaderboardMessage> eventLogList = new List<LeaderboardMessage>();
    public List<Profile> leaderData;
    private SqlController sql;
    private int numLeaderboards = 2;
    private int currentShowing = 1; 
    private float elapsed;
    private void Start()
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Loading";
        alert.transform.Find("AlertText").GetComponent<Text>().text = "Getting all the good players...";
        alert.SetActive(true);
        sql = new SqlController();
        StartCoroutine(sql.RequestRoutine("player/GetLeaderboard", leaderboardCallback, true));
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= 5f)
        {
            elapsed = elapsed % 5f;
            try
            {
                StartCoroutine(sql.RequestRoutine($"player/GetAppVersion", GetAppVersionCallback, true));
            }
            catch (Exception ex)
            {
                alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Network Failure";
                alert.transform.Find("AlertText").GetComponent<Text>().text = "Can't connect to the server.";
                alert.SetActive(true);
            }
        }
    }

    private void GetAppVersionCallback(string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            var version = sql.jsonConvert<double>(data);
            if (Settings.AppVersion < version)
            {
                alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Version Mismatch";
                alert.transform.Find("AlertText").GetComponent<Text>().text = "Your version of the game is out of sync, please refresh your browser to get the latest update.";
                alert.SetActive(true);
            }
        }
    }
    private void leaderboardCallback(string jdata)
    {
        leaderData = sql.jsonConvert<List<Profile>>(jdata);
        currentShowing = 1;// Random.Range(0, numLeaderboards);
        ShowLeaderboard();
        alert.SetActive(false);
    }

    private void ShowLeaderboard()
    {
        //if (currentShowing == 0)
        //{
        //    ShowDailyWins();
        //}
        //else if (currentShowing == 1)
        //{
        //    ShowWeeklyWins();
        //} 
        if (currentShowing == 0)
        {
            ShowWins();
        }
        else
        {
            ShowLocalWins();
        }
    }

    public void SceneChange(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    private void ShowWins()
    {
        headerText.text = "All-Time Wins vs CPU";
        ClearMessages();
        var i = 0;
        foreach (var d in leaderData.Where(x => x.AllWins > 0).OrderByDescending(x => x.AllWins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.AllWins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }  
    private void ShowLocalWins()
    {
        headerText.text = "All-Time Wins vs Players";
        ClearMessages();
        var i = 0;
        foreach (var d in leaderData.Where(x => x.AllPVPWins > 0).OrderByDescending(x => x.AllPVPWins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.AllPVPWins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }

    private void ShowDailyWins()
    {
        headerText.text = "Daily Wins vs CPU";
        ClearMessages();
        var i = 0;
        foreach (var d in leaderData.Where(x => x.DailyWins > 0).OrderByDescending(x => x.DailyWins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.DailyWins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }
    
    private void ShowWeeklyWins()
    {
        headerText.text = "Weekly Wins vs CPU";
        ClearMessages();
        var i = 0;
        foreach (var d in leaderData.Where(x => x.WeeklyWins > 0).OrderByDescending(x => x.WeeklyWins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.WeeklyWins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }
    public void next(bool left = false)
    {
        if (left)
        {
            currentShowing--;
            if(currentShowing < 0)
                currentShowing = numLeaderboards-1;
        }
        else
        {
            currentShowing++;
            currentShowing = currentShowing % numLeaderboards;
            
        }
        ShowLeaderboard();
    }
    private void ClearMessages()
    {
        if (eventLogList.Count() > 0)
        {
            for (int i = eventLogList.Count() - 1; i >= 0; i--)
            {
                Destroy(eventLogList[i].textObject.gameObject);
                eventLogList.Remove(eventLogList[i]);
            }
        }
    }
    private void SendEventToLog(string text)
    {
        LeaderboardMessage newMessage = new LeaderboardMessage();
        newMessage.text = text;
        Text newText = Instantiate(eventLogTextObject, eventLogTextContent.transform);
        newMessage.textObject = newText.GetComponent<Text>();
        newMessage.textObject.text = newMessage.text;
        eventLogList.Add(newMessage);
    }
}
