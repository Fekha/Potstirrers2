using Assets.Models;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
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
    public Dictionary<string,int> seasonData = new Dictionary<string, int>();
    public Dictionary<string,int> cpuData = new Dictionary<string, int>();
    public Dictionary<string,int> pvpData = new Dictionary<string, int>();
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
        var analytics = sql.jsonConvert<List<GameAnalytic>>(jdata);
        foreach(var anal in analytics)
        {
            if(anal.Quit && anal.TotalTurns < 20)
            {
                continue;
            }
            bool? player1Won = null;          
            if (anal.Player1CookedNum > anal.Player2CookedNum)
            {
                player1Won = true;
                if (anal.IsCpuGame)
                {
                    if (cpuData.ContainsKey(anal.Player1))
                    {
                        cpuData[anal.Player1] += 1;
                    }
                    else
                    {
                        cpuData.Add(anal.Player1, 1);
                    }
                }
                else
                {
                    if(pvpData.ContainsKey(anal.Player1))
                    {
                        pvpData[anal.Player1] += 1;
                    }
                    else
                    {
                        pvpData.Add(anal.Player1, 1);
                    }
                }
               
            }
            else if (anal.Player1CookedNum < anal.Player2CookedNum)
            {
                player1Won = false;
                if (!anal.IsCpuGame)
                {
                    if (pvpData.ContainsKey(anal.Player2))
                    {
                        pvpData[anal.Player2] += 1;
                    }
                    else
                    {
                        pvpData.Add(anal.Player2, 1);
                    }
                }
            }
            if (anal.IsCpuGame)
            {
                if (player1Won == true)
                {
                    if (seasonData.ContainsKey(anal.Player1))
                    {
                        seasonData[anal.Player1] += anal.Player2 == "Ethan" ? 100 : 50;
                    }
                    else
                    {
                        seasonData.Add(anal.Player1, anal.Player2 == "Ethan" ? 100 : 50);
                    }
                }
            }
            else
            {
                if (player1Won == true)
                {
                    if (seasonData.ContainsKey(anal.Player1))
                    {
                        seasonData[anal.Player1] += anal.Wager;
                    }
                    else
                    {
                        seasonData.Add(anal.Player1, anal.Wager);
                    }
                }
                else if(player1Won == false)
                {
                    if (seasonData.ContainsKey(anal.Player2))
                    {
                        seasonData[anal.Player2] += anal.Wager;
                    }
                    else
                    {
                        seasonData.Add(anal.Player2, anal.Wager);
                    }
                }
            }
        }
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
                ShowCPUWins();
            }
            else
            {
                ShowPVPWins();
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
    private void ShowCPUWins()
    {
        headerText.text = "Wins vs CPU";
        ClearMessages();
        var i = 1;
        foreach (var player in cpuData.OrderByDescending(x => x.Value))
        {
            SendEventToLog(player.Key, i, player.Value);
            i++;
        }
    }  
    private void ShowPVPWins()
    {
        headerText.text = "Wins vs Players";
        ClearMessages();
        var i = 1;
        foreach (var player in pvpData.OrderByDescending(x => x.Value))
        {
            SendEventToLog(player.Key, i, player.Value);
            i++;
        }
    }

    //private void ShowDailyWins()
    //{
    //    headerText.text = "Daily Wins vs CPU";
    //    ClearMessages();
    //    var i = 1;
    //    foreach (var player in leaderData.Where(x => x.DailyWins > 0).OrderByDescending(x => x.DailyWins))
    //    {
    //        SendEventToLog(player, i, player.DailyWins);
    //        i++;
    //    }
    //}
    
    private void ShowSeasonScore()
    {
        headerText.text = "Calories Earned";
        ClearMessages();
        var i = 1;
        foreach (var player in seasonData.OrderByDescending(x => x.Value))
        {
            SendEventToLog(player.Key, i, player.Value);
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
    private void SendEventToLog(string username, int rank, int wins)
    {
        GameObject newMessage = Instantiate(eventLogTextObject, eventLogTextContent.transform);
        newMessage.transform.Find("RankText").gameObject.GetComponent<Text>().text = rank + ") ";
        newMessage.transform.Find("PlayerText").gameObject.GetComponent<Text>().text = username + " - " + wins;
        //newMessage.GetComponent<Button>().onClick.AddListener(() => MainMenuController.i.OpenFriendProfile(player.UserId));
        eventLogList.Add(newMessage);
    }
}
