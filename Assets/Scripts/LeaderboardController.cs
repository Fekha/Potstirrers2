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
    public GameObject eventLogTextObject;
    private List<GameObject> eventLogList = new List<GameObject>();
    public List<Player> leaderData;
    private SqlController sql;
    private int numLeaderboards = 3;
    private int currentShowing = 0; 
    private float elapsed;
    private void Awake()
    {
        sql = new SqlController();
        MainMenuController.i.DisplayLoading("Loading", "Getting all the good players...");
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
                MainMenuController.i.DisplayAlert("Network Failure", ex.Message);
            }
        }
    }

    private void GetAppVersionCallback(string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            var version = sql.jsonConvert<double>(data);
            if (Global.AppVersion < version)
            {
                MainMenuController.i.DisplayAlert("Version Mismatch", "Your version of the game is out of sync, please refresh your browser to get the latest update.");
            }
        }
    }
    private void leaderboardCallback(string jdata)
    {
        leaderData = sql.jsonConvert<List<Player>>(jdata);
        currentShowing = 0;// Random.Range(0, numLeaderboards);
        ShowLeaderboard();
        MainMenuController.i.HideLoading();
    }

    private void ShowLeaderboard()
    {
        try
        {
            //if (currentShowing == 0)
            //{
            //    ShowDailyWins();
            //}
            if (currentShowing == 0)
            {
                ShowSeasonScore();
            }
            else if (currentShowing == 1)
            {
                ShowWins();
            }
            else
            {
                ShowLocalWins();
            }
        }catch(Exception ex)
        {
            MainMenuController.i.HideLoading();
            MainMenuController.i.DisplayAlert("Network Failure", "No internet connection");
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
        var i = 1;
        //foreach (var player in leaderData.Where(x => x.AllWins > 0).OrderByDescending(x => x.AllWins))
        //{
        //    SendEventToLog(player,i,player.AllWins);
        //    i++;
        //}
    }  
    private void ShowLocalWins()
    {
        headerText.text = "All-Time Wins vs Players";
        ClearMessages();
        var i = 1;
        //foreach (var player in leaderData.Where(x => x.AllPVPWins > 0).OrderByDescending(x => x.AllPVPWins))
        //{
        //    SendEventToLog(player, i, player.AllPVPWins);
        //    i++;
        //}
    }

    private void ShowDailyWins()
    {
        headerText.text = "Daily Wins vs CPU";
        ClearMessages();
        var i = 1;
        //foreach (var player in leaderData.Where(x => x.DailyWins > 0).OrderByDescending(x => x.DailyWins))
        //{
        //    SendEventToLog(player, i, player.DailyWins);
        //    i++;
        //}
    }
    
    private void ShowSeasonScore()
    {
        headerText.text = "Calories Earned Online";
        ClearMessages();
        var i = 1;
        foreach (var player in leaderData.Where(x => x.SeasonScore > 0).OrderByDescending(x => x.SeasonScore))
        {
            SendEventToLog(player, i, player.SeasonScore);
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
                Destroy(eventLogList[i]);
                eventLogList.Remove(eventLogList[i]);
            }
        }
    }
    private void SendEventToLog(Player player, int rank, int wins)
    {
        GameObject newMessage = Instantiate(eventLogTextObject, eventLogTextContent.transform);
        newMessage.transform.Find("RankText").gameObject.GetComponent<Text>().text = rank + ") ";
        newMessage.transform.Find("PlayerText").gameObject.GetComponent<Text>().text = player.Username + " - " + wins;
        newMessage.GetComponent<Button>().onClick.AddListener(() => MainMenuController.i.OpenFriendProfile(player.UserId));
        eventLogList.Add(newMessage);
    }
}
