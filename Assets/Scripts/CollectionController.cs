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
    public Sprite SelectedTabSprite;
    public Sprite UnselectedTabSprite;

    [Header("Ingredients")]
    public GameObject IngButtonContent;
    public Button IngPrefabObj;
    private List<SkinData> AllIngSkins = new List<SkinData>();
    private List<SkinData> MyIngSkins = new List<SkinData>();
    internal List<int> SelectedIngSkins = new List<int>();  

    [Header("Dice")]
    public GameObject DiceButtonContent;
    public Button DicePrefabObj;
    private List<SkinData> AllDiceSkins = new List<SkinData>();
    private List<SkinData> MyDiceSkins = new List<SkinData>();
    private List<int> SelectedDiceSkins = new List<int>();
    private List<Button> DiceButtonLog = new List<Button>();


    [Header("Global")]
    public GameObject alert;
    public Text alertText;
    private SqlController sql;
    private bool hasBeenIngChange = false;
    private bool hasBeenDiceChange = false;
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
        StartCoroutine(sql.RequestRoutine($"purchase/GetMyIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}", GetMyIngredientCallback));
        StartCoroutine(sql.RequestRoutine($"purchase/GetMyDiceSkins?UserId={Settings.LoggedInPlayer.UserId}", GetMyDiceCallback));
    }
    private void GetMyIngredientCallback(string data)
    {
        MyIngSkins = sql.jsonConvert<List<SkinData>>(data);
        var j = 0;
        MainMenuController.i.ingSprites.ForEach(x => { j++; CreateIng(x, j); });
    }
    
    private void GetMyDiceCallback(string data)
    {
        MyDiceSkins = sql.jsonConvert<List<SkinData>>(data);
        ClearDice();
        var j = 0;
        MainMenuController.i.diceSprites.ForEach(x => { j++; CreateDice(x, j); });
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
    #region Ingredients
    private void CreateIng(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedMeat == skinId || Settings.LoggedInPlayer.SelectedVeggie == skinId || Settings.LoggedInPlayer.SelectedFruit == skinId || Settings.LoggedInPlayer.SelectedFourth == skinId)
        {
            IngPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            SelectedIngSkins.Add(skinId);
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
        }
        else if (skinId >= 0 && skinId <= 4)
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = true,
            };
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = false,
            };
        }
        IngPrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        Button newButton = Instantiate(IngPrefabObj, IngButtonContent.transform);
        newButton.onClick.AddListener(() => SelectIng(skinId));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllIngSkins.Add(skin);
    }
    private void SelectIng(int skinId)
    {
        hasBeenIngChange = true;

        SkinData item = AllIngSkins.FirstOrDefault(x => x.SkinId == skinId);
        
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            SelectedIngSkins.Remove(item.SkinId);
        }
        else if (SelectedIngSkins.Count < 4)
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                SelectedIngSkins.Add(item.SkinId);
                var skin1 = SelectedIngSkins.Count > 0 ? SelectedIngSkins[0] : 0;
                var skin2 = SelectedIngSkins.Count > 1 ? SelectedIngSkins[1] : 0;
                var skin3 = SelectedIngSkins.Count > 2 ? SelectedIngSkins[2] : 0;
                var skin4 = SelectedIngSkins.Count > 3 ? SelectedIngSkins[3] : 0;
                Settings.LoggedInPlayer.SelectedMeat = skin1;
                Settings.LoggedInPlayer.SelectedVeggie = skin2;
                Settings.LoggedInPlayer.SelectedFruit = skin3;
                Settings.LoggedInPlayer.SelectedFourth = skin4;

                StartCoroutine(sql.RequestRoutine($"purchase/UpdateIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}&SelectedMeat={skin1}&SelectedVeggie={skin2}&SelectedFruit={skin3}&SelectedFourth={skin4}"));
            }
            else
            {
                alertText.text = "You have not unlocked this ingredient yet.";
                alert.SetActive(true);
            }
        }
        else
        {
            alertText.text = "Only 4 Ingredients may be active at a time.";
            alert.SetActive(true);
        }
    }
    #endregion

    #region Dice
    private void CreateDice(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedDie.Contains(skinId))
        {
            DicePrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            SelectedDiceSkins.Add(skinId);
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
        hasBeenDiceChange = true;
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            SelectedDiceSkins.Remove(item.SkinId);
            Settings.LoggedInPlayer.SelectedDie.Remove(item.SkinId);
            StartCoroutine(sql.RequestRoutine($"purchase/UpdateDiceSkins?UserId={Settings.LoggedInPlayer.UserId}&dieId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                SelectedDiceSkins.Add(item.SkinId);
                Settings.LoggedInPlayer.SelectedDie.Add(item.SkinId);
                StartCoroutine(sql.RequestRoutine($"purchase/UpdateDiceSkins?UserId={Settings.LoggedInPlayer.UserId}&dieId={item.SkinId}&add=true"));
            }
            else
            {
                alertText.text = $"You must unlock all sides before selecting this die.";
                alert.SetActive(true);
            }
        }
        
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
    }

    #endregion region
}
