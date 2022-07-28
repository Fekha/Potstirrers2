using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public static TabController i;
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
    public GameObject LeaderboardPanel;
    public Sprite SelectedTabSprite;
    public Sprite UnselectedTabSprite; 
    private static int lastSelected = 0;
    private void Awake()
    {
        i = this;
    }
    public void TabClicked(int Selected)
    {
        if (Global.LoggedInPlayer.IsGuest && Selected != 3 && Selected != 6)
        {
            MainMenuController.i.DisplayAlert("Restricted", "Create an account to access the tabs!");
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
                Global.OnlyGetOnlineFriends = false;
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
                StartCoroutine(MainMenuController.i.SetPlayer());
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

            if (Selected != 6)
            {
                LeaderboardPanel.SetActive(false);
            }
            else
            {
                LeaderboardPanel.SetActive(true);
            }
        }
    }
}
