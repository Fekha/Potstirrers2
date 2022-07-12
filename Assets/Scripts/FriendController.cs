using Assets.Models;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FriendController : MonoBehaviour
{
    private SqlController sql;

    [Header("Tabs")]
    public GameObject ListPanel;
    public Image ListButtonImage;
    public GameObject MessagePanel;
    public Image MessageButtonImage;
    public Sprite SelectedTabSprite;
    public Sprite UnselectedTabSprite;

    [Header("Messages")]
    public GameObject hasMessage;
    public GameObject sendMessagePanel;
    public GameObject messageChoice;
    public GameObject SubjectInput;
    public GameObject BodyInput;
    public Text ToInput;
    public Dropdown ToDropdown;
    public Text SubjectText;
    public Text BodyText;
    public Text FromText;
    public GameObject ViewMessagePanel;
    public GameObject MessageButtonContent;
    public Button MessagePrefabObj;
    private List<Button> MessageButtonLog = new List<Button>();

    [Header("FriendsList")]
    public GameObject friendslist;
    public GameObject FriendButtonContent;
    public GameObject FriendText;
    private List<Button> FriendButtonLog = new List<Button>();

    private Message CurrentMessage;
    private bool showFriendList = false;
    public GameObject alert;
    public Text alertText;
    private Profile YourFriend;

    void Awake()
    {
        sql = new SqlController();
    }
    private void Start()
    {
        StartCoroutine(sql.RequestRoutine($"player/GetFriends?userId={Settings.LoggedInPlayer.UserId}", GetFriendCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetMessages?userId={Settings.LoggedInPlayer.UserId}", GetMessageCallback));
    }
    public void TabClicked(int Selected)
    {
        if (Selected != 1)
        {
            ListPanel.SetActive(false);
            ListButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            ListPanel.SetActive(true);
            ListButtonImage.sprite = SelectedTabSprite;
        }
        if (Selected != 2)
        {
            MessagePanel.SetActive(false);
            MessageButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            MessagePanel.SetActive(true);
            MessageButtonImage.sprite = SelectedTabSprite;
        }
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
            MainMenuController.i.ShowProfile(false);
            StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Settings.LoggedInPlayer.UserId}&username={YourFriend.Username}&add={add}", GetFriendCallback));
        }
    }

    public void ShowFriendsList(bool open)
    {
        showFriendList = open;
        MainMenuController.i.profilePanel.SetActive(!open);
        friendslist.SetActive(open);
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
        messageChoice.SetActive(false);
        ViewMessagePanel.SetActive(true);
        StartCoroutine(sql.RequestRoutine($"player/ReadMessage?MessageId={CurrentMessage.MessageId}", GetMessageCallback));
    }
    public void deleteMessage()
    {
        messageChoice.SetActive(false);
        alertText.text = "Message Deleted!";
        alert.SetActive(true);
        StartCoroutine(sql.RequestRoutine($"player/DeleteMessage?MessageId={CurrentMessage.MessageId}", GetMessageCallback));
    }

    public void HideMessage()
    {
        ViewMessagePanel.SetActive(false);
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
        MessagePrefabObj.transform.Find("Image").gameObject.SetActive(!realFriend);
        MessagePrefabObj.GetComponentInChildren<Text>().text = username;
        Button newButton = Instantiate(MessagePrefabObj, FriendButtonContent.transform);
        newButton.onClick.AddListener(() => StartCoroutine(sql.RequestRoutine($"player/GetProfile?username={username}", MainMenuController.i.GetFriendProfileCallback)));
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
        MessagePrefabObj.transform.Find("Image").gameObject.SetActive(!message.IsRead);
        MessagePrefabObj.GetComponentInChildren<Text>().text = message.Subject;
        Button newButton = Instantiate(MessagePrefabObj, MessageButtonContent.transform);
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

    private void GetFriendCallback(string data)
    {
        ClearFriends();
        var friends = sql.jsonConvert<List<FriendDTO>>(data);
        foreach (var d in friends.OrderByDescending(x => x.Level))
        {
            CreateFriend(d.Username, d.RealFriend);
        }
    }
    private class FriendDTO
    {
        public string Username { get; set; }
        public bool RealFriend { get; set; }
        public int Level { get; set; }
    }
}
