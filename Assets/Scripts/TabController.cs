using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public GameObject StorePanel;
    public Image StoreButtonImage;
    public GameObject FriendPanel;
    public Image FriendButtonImage;
    public GameObject HomePanel;
    public Image HomeButtonImage;
    public GameObject ChestsPanel;
    public Image ChestsButtonImage;
    public GameObject CollectionPanel;
    public Image CollectionButtonImage;
    public Sprite SelectedTabSprite;
    public Sprite UnselectedTabSprite; 
    private SqlController sql;
    private int lastSelected = 0;
    private void Start()
    {
        sql = new SqlController();
    }
    public void TabClicked(int Selected)
    {
        if (Selected == 3)
        {
            StartCoroutine(MainMenuController.i.SetPlayer());
        }
        if (Global.LoggedInPlayer.IsGuest && Selected != 3)
        {
            MainMenuController.i.alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Restricted";
            MainMenuController.i.alert.transform.Find("AlertText").GetComponent<Text>().text = "Create an account to access the tabs!";
            MainMenuController.i.alert.SetActive(true);
            return;
        }
        else
        {
            lastSelected = Selected;
            if (Selected != 1)
            {
                StorePanel.SetActive(false);
                StoreButtonImage.sprite = UnselectedTabSprite;
            }
            else
            {
                StorePanel.SetActive(true);
                StoreButtonImage.sprite = SelectedTabSprite;
            }
            if (Selected != 2)
            {
                FriendPanel.SetActive(false);
                FriendButtonImage.sprite = UnselectedTabSprite;
            }
            else
            {
                MainMenuController.i.HasMessage.SetActive(false);
                FriendPanel.SetActive(true);
                FriendButtonImage.sprite = SelectedTabSprite;
            }
            if (Selected != 3)
            {
                HomePanel.SetActive(false);
                HomeButtonImage.sprite = UnselectedTabSprite;
            }
            else
            {
                HomePanel.SetActive(true);
                HomeButtonImage.sprite = SelectedTabSprite;
            }
            if (Selected != 4)
            {
                ChestsPanel.SetActive(false);
                ChestsButtonImage.sprite = UnselectedTabSprite;
            }
            else
            {
                MainMenuController.i.HasChest.SetActive(false);
                ChestsPanel.SetActive(true);
                ChestsButtonImage.sprite = SelectedTabSprite;
            }
            if (Selected != 5)
            {
                CollectionPanel.SetActive(false);
                CollectionButtonImage.sprite = UnselectedTabSprite;
            }
            else
            {
                MainMenuController.i.HasUnlock.SetActive(false);
                CollectionPanel.SetActive(true);
                CollectionButtonImage.sprite = SelectedTabSprite;
            }
        }
    }
}
