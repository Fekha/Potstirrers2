using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CollectionController : MonoBehaviour
{
    [Header("Tabs")]
    public GameObject IngPanel;
    public Image IngButtonImage;
    public GameObject DicePanel;
    public Image DiceButtonImage;
    public GameObject TitlePanel;
    public Image TitleButtonImage;
    public Sprite SelectedTabSprite;
    public Sprite UnselectedTabSprite;

    [Header("Ingredients")]
    public GameObject IngButtonContent;
    public Button IngPrefabObj;
    private List<SkinData> AllIngSkins = new List<SkinData>();
    private List<SkinData> MyIngSkins = new List<SkinData>();
    private List<Button> IngButtonLog = new List<Button>();


    [Header("Dice")]
    public GameObject DiceButtonContent;
    public Button DicePrefabObj;
    private List<SkinData> AllDiceSkins = new List<SkinData>();
    private List<SkinData> MyDiceSkins = new List<SkinData>();
    private List<Button> DiceButtonLog = new List<Button>();
    
    [Header("Titles")]
    public GameObject TitleButtonContent;
    public Button TitlePrefabObj;
    private List<SkinData> AllTitles = new List<SkinData>();
    private List<SkinData> MyTitles = new List<SkinData>();
    private List<Button> TitleButtonLog = new List<Button>();


    [Header("Global")]
    public GameObject alert;
    public Text alertText;
    private SqlController sql;
    public static CollectionController i;
    private class SkinData : Skin
    {
        public Button SkinButton = null;
    }
    private void Awake()
    {
        i = this;
        sql = new SqlController();
    }

    private void OnEnable()
    {
        StartCoroutine(sql.RequestRoutine($"skin/GetMyIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}", GetMyIngredientCallback));
        StartCoroutine(sql.RequestRoutine($"skin/GetMyDiceSkins?UserId={Settings.LoggedInPlayer.UserId}", GetMyDiceCallback));
        StartCoroutine(sql.RequestRoutine($"skin/GetMyTitles?UserId={Settings.LoggedInPlayer.UserId}", GetMyTitlesCallback));
    }

    #region Ingredients
    private void CreateIng(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedIngs.Contains(skinId))
        {
            IngPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            IngPrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        IngPrefabObj.transform.Find("Item").gameObject.GetComponent<Image>().sprite = item;
     
        SkinData skin = new SkinData();
        if (MyIngSkins.Any(x => x.SkinId == skinId))
        {
            skin = MyIngSkins.FirstOrDefault(x => x.SkinId == skinId);
            IngPrefabObj.GetComponentInChildren<Text>().text = "0";
        }
        else if (skinId >= 0 && skinId <= 6)
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = true,
            };
            IngPrefabObj.GetComponentInChildren<Text>().text = "";
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = false,
            };
            IngPrefabObj.GetComponentInChildren<Text>().text = "0";
        }
        if (Settings.IsDebug)
            skin.IsUnlocked = true;
        
        IngPrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        Button newButton = Instantiate(IngPrefabObj, IngButtonContent.transform);
        newButton.onClick.AddListener(() => SelectIng(skinId));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllIngSkins.Add(skin);
        IngButtonLog.Add(newButton);

    }
    private void SelectIng(int skinId)
    {
        SkinData item = AllIngSkins.FirstOrDefault(x => x.SkinId == skinId);
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            Settings.LoggedInPlayer.SelectedIngs.Remove(item.SkinId);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Settings.LoggedInPlayer.SelectedIngs.Add(item.SkinId);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Locked";
                alert.transform.Find("AlertText").GetComponent<Text>().text = $"You have not unlocked this ingredient yet.";
                alert.SetActive(true);
            }
        }

    }

    private void ClearIngs()
    {
        if (IngButtonLog.Count() > 0)
        {
            AllIngSkins.Clear();
            for (int i = IngButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(IngButtonLog[i].gameObject);
                IngButtonLog.Remove(IngButtonLog[i]);
            }
            IngButtonLog.Clear();
        }
    }

    private void GetMyIngredientCallback(string data)
    {
        MyIngSkins = sql.jsonConvert<List<SkinData>>(data);
        ClearIngs();
        var j = 0;
        MainMenuController.i.ingSprites.ForEach(x => { j++; CreateIng(x, j); });
    }
    #endregion

    #region Dice
    private void CreateDice(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedDice.Contains(skinId))
        {
            DicePrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            DicePrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        DicePrefabObj.transform.Find("Item").gameObject.GetComponent<Image>().sprite = item;
        SkinData skin = new SkinData();
        if (MyDiceSkins.Any(x => x.SkinId == skinId))
        {
            skin = MyDiceSkins.FirstOrDefault(x => x.SkinId == skinId);
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = false,
                UnlockedQty = 0
            };
        }
        if(Settings.IsDebug)
            skin.IsUnlocked = true;
        DicePrefabObj.GetComponentInChildren<Text>().text = skin.UnlockedQty.ToString();
        DicePrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        Button newButton = Instantiate(DicePrefabObj, DiceButtonContent.transform);
        newButton.onClick.AddListener(() => SelectDice(skinId));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllDiceSkins.Add(skin);
        DiceButtonLog.Add(newButton);
    }
 
    private void SelectDice(int skinId)
    {
        SkinData item = AllDiceSkins.FirstOrDefault(x=>x.SkinId == skinId);
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            Settings.LoggedInPlayer.SelectedDice.Remove(item.SkinId);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateDiceSkins?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Settings.LoggedInPlayer.SelectedDice.Add(item.SkinId);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateDiceSkins?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Locked";
                alert.transform.Find("AlertText").GetComponent<Text>().text = $"You must unlock all sides before selecting this die.";
                alert.SetActive(true);
            }
        }
        
    }

    private void ClearDice()
    {
        if (DiceButtonLog.Count() > 0)
        {
            AllDiceSkins.Clear();
            for (int i = DiceButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(DiceButtonLog[i].gameObject);
                DiceButtonLog.Remove(DiceButtonLog[i]);
            }
            DiceButtonLog.Clear();
        }
    }

    private void GetMyDiceCallback(string data)
    {
        MyDiceSkins = sql.jsonConvert<List<SkinData>>(data);
        ClearDice();
        var j = 0;
        MainMenuController.i.diceSprites.ForEach(x => { j++; CreateDice(x, j); });
    }
    #endregion 
    #region Titles
    private void CreateTitle(Skin item)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedTitles.Contains(item.SkinName))
        {
            TitlePrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            TitlePrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        TitlePrefabObj.transform.Find("Title").gameObject.GetComponent<Text>().text = item.SkinName;
        SkinData skin = new SkinData();
        if (MyTitles.Any(x => x.SkinId == item.SkinId))
        {
            skin = MyTitles.FirstOrDefault(x => x.SkinId == item.SkinId);
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = item.SkinId,
                SkinName = item.SkinName,
                SkinDesc = item.SkinDesc,
                IsUnlocked = false,
                UnlockedQty = 0
            };
        }
        if(Settings.IsDebug || item.SkinId == 1 || item.SkinId == 2)
            skin.IsUnlocked = true;
        TitlePrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        TitlePrefabObj.transform.Find("Info").gameObject.SetActive(skin.IsUnlocked);
        Button newButton = Instantiate(TitlePrefabObj, TitleButtonContent.transform);
        newButton.onClick.AddListener(() => SelectTitle(item.SkinId));
        newButton.transform.Find("Info").gameObject.GetComponent<Button>().onClick.AddListener(() => GetTitleInfo(item.SkinDesc, "Info"));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllTitles.Add(skin);
        TitleButtonLog.Add(newButton);
    }

    private void GetTitleInfo(string skinDesc, string bannerText)
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = bannerText;
        alert.transform.Find("AlertText").GetComponent<Text>().text = skinDesc;
        alert.transform.Find("AlertText").GetComponent<Text>().text = skinDesc;
        alert.SetActive(true);
    }

    private void SelectTitle(int skinId)
    {
        SkinData item = AllTitles.FirstOrDefault(x=>x.SkinId == skinId);
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            Settings.LoggedInPlayer.SelectedTitles.Remove(item.SkinName);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateTitle?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Settings.LoggedInPlayer.SelectedTitles.Add(item.SkinName);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateTitle?UserId={Settings.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                GetTitleInfo(item.SkinDesc,"Locked");
            }
        }
    }

    private void ClearTitles()
    {
        if (TitleButtonLog.Count() > 0)
        {
            AllTitles.Clear();
            for (int i = TitleButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(TitleButtonLog[i].gameObject);
                TitleButtonLog.Remove(TitleButtonLog[i]);
            }
            TitleButtonLog.Clear();
        }
    }

    private void GetMyTitlesCallback(string data)
    {
        MyTitles = sql.jsonConvert<List<SkinData>>(data);
        StartCoroutine(sql.RequestRoutine($"skin/GetAllTitles", GetAllTitlesCallback));
    }
    private void GetAllTitlesCallback(string data)
    {
        var allTitles = sql.jsonConvert<List<Skin>>(data);
        ClearTitles();
        allTitles.ForEach(x => { CreateTitle(x); });
    }

    #endregion

    #region Tabs
    public void TabClicked(int Selected)
    {
        if (Selected != 1)
        {
            IngPanel.SetActive(false);
            IngButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            IngPanel.SetActive(true);
            IngButtonImage.sprite = SelectedTabSprite;
        }
        if (Selected != 2)
        {
            DicePanel.SetActive(false);
            DiceButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            DicePanel.SetActive(true);
            DiceButtonImage.sprite = SelectedTabSprite;
        } 
        if (Selected != 3)
        {
            TitlePanel.SetActive(false);
            TitleButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            TitlePanel.SetActive(true);
            TitleButtonImage.sprite = SelectedTabSprite;
        }
    }

    #endregion region
}
