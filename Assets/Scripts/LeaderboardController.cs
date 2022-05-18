using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardController : MonoBehaviour
{
    [System.Serializable]
    public class Message
    {
        public string text;
        public Text textObject;
    }
    public Text headerText;
    public GameObject eventLogTextContent;
    public GameObject loading;
    public Text eventLogTextObject;
    public List<Message> eventLogList;
    public List<Player> playerList;
    private SqlController sql;
    private int numLeaderboards = 4;
    private int currentShowing = 0;
    private void Start()
    {
        loading.SetActive(true);
        sql = new SqlController();
        StartCoroutine(sql.RequestRoutine("player/GetLeaderboard", leaderboardCallback, true));
    }
    private void leaderboardCallback(string jdata)
    {
        playerList = sql.jsonConvert<List<Player>>(jdata);
        currentShowing = 1;// Random.Range(0, numLeaderboards);
        ShowLeaderboard();
        loading.SetActive(false);
    }

    private void ShowLeaderboard()
    {
        if (currentShowing == 0)
        {
            ShowStars();
        }
        else if (currentShowing == 1)
        {
            ShowWins();
        } 
        else if (currentShowing == 2)
        {
            ShowLevels();
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
    private void ShowLevels()
    {
        headerText.text = "Highest Level Player";
        ClearMessages();
        var i = 0;
        foreach (var d in playerList.Where(x => x.Level > 1).OrderByDescending(x => x.Level).ThenByDescending(x=>x.Xp))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - Level: " + d.Level;
            SendEventToLog(evenetDesc);
            i++;
        }
    }
    private void ShowWins()
    {
        headerText.text = "Most Wins vs CPU";
        ClearMessages();
        var i = 0;
        foreach (var d in playerList.Where(x => x.Wins > 0).OrderByDescending(x => x.Wins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.Wins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }  
    private void ShowLocalWins()
    {
        headerText.text = "Most Wins vs Players";
        ClearMessages();
        var i = 0;
        foreach (var d in playerList.Where(x => x.LocalWins > 0).OrderByDescending(x => x.LocalWins))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.LocalWins;
            SendEventToLog(evenetDesc);
            i++;
        }
    }

    private void ShowStars()
    {
        headerText.text = "Most Cooked Ingredients";
        ClearMessages();
        var i = 0;
        foreach (var d in playerList.Where(x => x.Cooked > 0).OrderByDescending(x => x.Cooked))
        {
            var evenetDesc = (i + 1) + ") " + d.Username + " - " + d.Cooked;
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
        Message newMessage = new Message();
        newMessage.text = text;
        Text newText = Instantiate(eventLogTextObject, eventLogTextContent.transform);
        newMessage.textObject = newText.GetComponent<Text>();
        newMessage.textObject.text = newMessage.text;
        eventLogList.Add(newMessage);
    }
}
