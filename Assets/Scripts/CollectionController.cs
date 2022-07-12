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
    public List<Sprite> ingSprites;
    public GameObject IngButtonContent;
    public Button IngPrefabObj;
    private List<SkinData> AllIngSkins = new List<SkinData>();
    private List<SkinData> MyIngSkins = new List<SkinData>();
    internal List<int> SelectedIngSkins = new List<int>();  

    [Header("Dice")]
    public List<Sprite> diceSprites;
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
    public static CollectionController i;
    private bool hasBeenIngChange = false;
    private bool hasBeenDiceChange = false;
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
        ingSprites.ForEach(x => { j++; CreateIng(x, j); });
    }
    
    private void GetMyDiceCallback(string data)
    {
        MyDiceSkins = sql.jsonConvert<List<SkinData>>(data);
        ClearDice();
        var j = 0;
        diceSprites.ForEach(x => { j++; CreateDice(x, j); });
    }

    private void ClearDice()
    {
        if (DiceButtonLog.Count() > 0)
        {
            for (int i = DiceButtonLog.Count() - 1; i >= 0; i--)
            {
                Destroy(DiceButtonLog[i].gameObject);
                DiceButtonLog.Remove(DiceButtonLog[i]);
            }
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
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                SelectedDiceSkins.Add(item.SkinId);
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
        if (CheckValid())
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
    }

    public bool CheckValid()
    {
        if (SelectedIngSkins.Count < 4)
        {
            alertText.text = "You must select 4 Ingredients to be active!";
            alert.SetActive(true);
            return false;
        }
        else
        {
            if (hasBeenIngChange)
            {
                StartCoroutine(sql.RequestRoutine($"purchase/UpdateIngredientSkins?UserId={Settings.LoggedInPlayer.UserId}&SelectedMeat={SelectedIngSkins[0]}&SelectedVeggie={SelectedIngSkins[1]}&SelectedFruit={SelectedIngSkins[2]}&SelectedFourth={SelectedIngSkins[3]}"));
                hasBeenIngChange = false;
            }

            if (hasBeenDiceChange)
            {
                StartCoroutine(sql.PostRoutine($"purchase/UpdateDiceSkins?UserId={Settings.LoggedInPlayer.UserId}", SelectedDiceSkins));
                hasBeenDiceChange = false;
            }
            return true;
        }
    }
    #endregion region
}
