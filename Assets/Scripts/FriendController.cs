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
    public GameObject SubjectInput;
    public GameObject BodyInput;
    public Text ToInput;
    public Dropdown ToDropdown;
    public Text SubjectText;
    public Text BodyText;
    public Text FromText;
    public GameObject ViewMessagePanel;
    public GameObject MessageButtonContent;
    public GameObject RemoveChoice;
    public Button MessagePrefabObj;
    private List<Button> MessageButtonLog = new List<Button>();

    [Header("FriendsList")]
    public GameObject friendslist;
    public GameObject FriendButtonContent;
    public GameObject FriendText;
    private List<Button> FriendButtonLog = new List<Button>();

    private int toDeleteId = 0;
    private string toDeleteName = "";
    private Message CurrentMessage;
    public GameObject alert;
    public Text alertText;


    void Awake()
    {
        sql = new SqlController();
    }
    private void OnEnable()
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Loading";
        alert.transform.Find("AlertText").GetComponent<Text>().text = "Finding all of your friends...";
        alert.SetActive(true);
        StartCoroutine(sql.RequestRoutine($"player/GetFriends?userId={Global.LoggedInPlayer.UserId}", GetFriendCallback));
        StartCoroutine(sql.RequestRoutine($"player/GetMessages?userId={Global.LoggedInPlayer.UserId}", GetMessageCallback));
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


    public void AddFriend()
    {
        StartCoroutine(sql.RequestRoutine("player/GetUserByName?username=" + FriendText.GetComponent<InputField>().text, this.GetFriendByUsernameCallback));
    }  
    
    private void RemoveFriend(string FriendName)
    {
        toDeleteId = 0;
        toDeleteName = FriendName;
        RemoveChoice.transform.Find("Question").GetComponent<Text>().text = "Do you want to remove this friend?";
        RemoveChoice.SetActive(true);
    }

    public void ShowFriendsList(bool open)
    {
        MainMenuController.i.profilePanel.SetActive(!open);
        friendslist.SetActive(open);
    }

    public void ShowSendMessage(bool open)
    {
        if (ToDropdown.options.Count > 0)
            sendMessagePanel.SetActive(open);
        else
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Failure";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "You must have friends that are friends with you to be able to send a message! Why did you remove poor Feca :(";
            alert.SetActive(true);
        }
    }

    public void SendMessage()
    {
        if (!String.IsNullOrEmpty(ToInput.text))
        {
            StartCoroutine(sql.RequestRoutine($"player/SendMessage?userId={Global.LoggedInPlayer.UserId}&toName={ToInput.text}&subject={SubjectInput.GetComponent<InputField>().text}&body={BodyInput.GetComponent<InputField>().text}"));
            sendMessagePanel.SetActive(false);
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Success";
            alert.transform.Find("AlertText").GetComponent<Text>().text = $"Message sent to {ToInput.text}";
        }
        else
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Failure";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "Can not send a message without a friend selected!";
        }
        alert.SetActive(true);
    }
    public void viewMessage(Message message)
    {
        CurrentMessage = message;
        SubjectText.text = CurrentMessage.Subject;
        BodyText.text = CurrentMessage.Body;
        FromText.text = $"From: " + CurrentMessage.FromName;
        ViewMessagePanel.SetActive(true);
        StartCoroutine(sql.RequestRoutine($"player/ReadMessage?MessageId={CurrentMessage.MessageId}", GetMessageCallback));
    }
    public void deleteMessage(int messageId)
    {
        toDeleteName = "";
        toDeleteId = messageId;
        RemoveChoice.transform.Find("Question").GetComponent<Text>().text = "Do you want to delete this message?";
        RemoveChoice.SetActive(true);
    }

    public void Delete()
    {
        RemoveChoice.SetActive(false);
        if (toDeleteId != 0)
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Success";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "Message Deleted!";
            alert.SetActive(true);
            StartCoroutine(sql.RequestRoutine($"player/DeleteMessage?MessageId={toDeleteId}", GetMessageCallback));
            toDeleteId = 0;
        }
        else if (toDeleteName != "")
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Success";
            alert.transform.Find("AlertText").GetComponent<Text>().text = $"You have removed {toDeleteName} as a friend :(";
            alert.SetActive(true);
            MainMenuController.i.ShowProfile(false);
            StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Global.LoggedInPlayer.UserId}&username={toDeleteName}&add={false}", GetFriendCallback));
            toDeleteName = "";
        }
    }
    public void HideMessage()
    {
        ViewMessagePanel.SetActive(false);
    }

    private void CreateFriend(FriendDTO user, bool realFriend)
    {
        if (realFriend)
        {
            ToDropdown.options.Add(new Dropdown.OptionData()
            {
                text = user.Username
            });
        }
        MessagePrefabObj.transform.Find("Image").gameObject.SetActive(!realFriend);
        MessagePrefabObj.transform.Find("Delete").gameObject.SetActive(realFriend);
        MessagePrefabObj.GetComponentInChildren<Text>().text = user.Username;
        Button newButton = Instantiate(MessagePrefabObj, FriendButtonContent.transform);
        newButton.onClick.AddListener(() => StartCoroutine(sql.RequestRoutine($"player/GetProfile?UserId={user.UserId}", MainMenuController.i.GetFriendProfileCallback)));
        newButton.transform.Find("Image").GetComponent<Button>().onClick.AddListener(()=> NotRealFriendPopup(user.Username));
        newButton.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(()=> RemoveFriend(user.Username));
        FriendButtonLog.Add(newButton);
    }

    private void NotRealFriendPopup(string Username)
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Not Your Friend";
        alert.transform.Find("AlertText").GetComponent<Text>().text = $"{Username} is your friend, but its not mutual. Waiting on them to add you back so you can do cool friend stuff!";
        alert.SetActive(true);
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
        MessagePrefabObj.transform.Find("Delete").gameObject.SetActive(message.IsRead);
        MessagePrefabObj.GetComponentInChildren<Text>().text = message.Subject;
        Button newButton = Instantiate(MessagePrefabObj, MessageButtonContent.transform);
        newButton.onClick.AddListener(() => viewMessage(message));
        newButton.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(() => deleteMessage(message.MessageId));
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
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Failure";
            alert.transform.Find("AlertText").GetComponent<Text>().text = "Player not found.";
            alert.SetActive(true);
        }
        else
        {
            alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Success";
            alert.transform.Find("AlertText").GetComponent<Text>().text = $"You have added {player.Username} as a friend :)";
            alert.SetActive(true);
            FriendText.GetComponent<InputField>().text = "";
            StartCoroutine(sql.RequestRoutine($"player/EditFriend?userId={Global.LoggedInPlayer.UserId}&username={player.Username}&add={true}", GetFriendCallback));
        }
    }

    private void GetFriendCallback(string data)
    {
        ClearFriends();
        var friends = sql.jsonConvert<List<FriendDTO>>(data);
        foreach (var d in friends.OrderByDescending(x => x.Level))
        {
            CreateFriend(d, d.RealFriend);
        }
        alert.SetActive(false);
    }
    private class FriendDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public bool RealFriend { get; set; }
        public int Level { get; set; }
    }
}
