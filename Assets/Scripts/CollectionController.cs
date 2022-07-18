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
    private List<SkinData> AllIngSkins = new List<SkinData>();
    private List<SkinData> MyIngSkins = new List<SkinData>();
    private List<Button> IngButtonLog = new List<Button>();

    [Header("Dice")]
    public GameObject DiceButtonContent;
    private List<SkinData> AllDiceSkins = new List<SkinData>();
    private List<SkinData> MyDiceSkins = new List<SkinData>();
    private List<Button> DiceButtonLog = new List<Button>();
    
    [Header("Titles")]
    public GameObject TitleButtonContent;
    public Button TitlePrefabObj;
    private List<SkinData> AllTitles = new List<SkinData>();
    private List<SkinData> MyTitles = new List<SkinData>();
    private List<Button> TitleButtonLog = new List<Button>();

    [Header("Crafting")]
    public GameObject CraftPanel;
    private int AmountToDestroy;
    private int AmountToCraft;
    private bool isCraftingDie = false;
    private Skin currentDestroyItem;
    private Skin currentCraftItem;
    private int canDestroyIndex = 0;
    private List<Skin> canCraft = new List<Skin>();
    private List<Skin> canDestroy = new List<Skin>();
    public Sprite CommonSprite;
    public Sprite EpicSprite;
    public Sprite RareSprite;

    private Image CurrentCraftImage;
    private Image CurrentDestroyImage;
    private Image BackgroundCraftImage;
    private Image BackgroundDestroyImage;
    private Text AmountToCraftText;
    private Text AmountToDestroyText;
    private Text AmountOwnedCraftText;
    private Text AmountOwnedDestroyText;
    private Text CostText;

    [Header("Global")]
    public Button ItemPrefabObj;
    public GameObject alert;
    public GameObject UnlockPanel;
    private SqlController sql;
    private Skin DieToPurchase;
    private Skin IngToPurchase;
    public static CollectionController i;
    public GameObject hasNewIng;
    public GameObject hasNewDie;
    public GameObject hasNewTitle;
    private int lastSelected = 1;
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
        RefreshSkinsCallback();
        hasNewIng.SetActive(Settings.hasNewIng);
        hasNewDie.SetActive(Settings.hasNewDie);
        hasNewTitle.SetActive(Settings.hasNewTitle);
        TabClicked(lastSelected);
    }
    public void UnlockItem()
    {
        UnlockPanel.SetActive(false);
        if (DieToPurchase != null)
        {
            if (DieToPurchase.UnlockedQty > 9)
            {
                StartCoroutine(sql.RequestRoutine($"skin/UnlockDiceSkin?UserId={Settings.LoggedInPlayer.UserId}&SkinId={DieToPurchase.SkinId}", RefreshSkinsCallback));
                DieToPurchase = null;
            }
            else
            {
                if (MyDiceSkins.Count > 1)
                {
                    canDestroy.Clear();
                    currentCraftItem = DieToPurchase;
                    isCraftingDie = true;
                    MyDiceSkins.ForEach(x =>
                    {
                        if (x.SkinId != DieToPurchase.SkinId && x.UnlockedQty >= SetCraftCosts(x.Rarity))
                            canDestroy.Add(x);
                    });
                    if (canDestroy.Count != 0)
                    {
                        OpenCraftPanel();
                    }
                    else
                    {
                        DisplayAlert("You need to collect more skins before being able to craft this!", "Unable to craft");
                    }
                }
                else
                {
                    DisplayAlert("You need to collect more skins before being able to craft this!", "Unable to craft");
                }
            }
        }
        else if (IngToPurchase != null)
        {
            if (IngToPurchase.UnlockedQty > 3)
            {
                StartCoroutine(sql.RequestRoutine($"skin/UnlockIngSkin?UserId={Settings.LoggedInPlayer.UserId}&SkinId={IngToPurchase.SkinId}", RefreshSkinsCallback));
                IngToPurchase = null;
            }
            else
            {
                if (MyIngSkins.Count > 1)
                {
                    canDestroy.Clear();
                    currentCraftItem = IngToPurchase;
                    isCraftingDie = false;
                    MyIngSkins.ForEach(x => {
                        if (x.SkinId != IngToPurchase.SkinId && x.UnlockedQty >= SetCraftCosts(x.Rarity))
                            canDestroy.Add(x);
                    });
                    if (canDestroy.Count != 0)
                    {
                        OpenCraftPanel();
                    }
                    else
                    {
                        DisplayAlert("You need to collect more skins before being able to craft this!", "Unable to craft");
                    }
                }
                else
                {
                    DisplayAlert("You need to collect more skins before being able to craft this!", "Unable to craft");
                }
            }
        }
    }

    private void OpenCraftPanel()
    {
        canDestroyIndex = 0;

        CraftPanel.SetActive(true);

        var itemToCraftPanel = CraftPanel.transform.Find("ItemToCraft");
        CurrentCraftImage = itemToCraftPanel.transform.Find("CurrentCraft").GetComponent<Image>();
        AmountToCraftText = itemToCraftPanel.transform.Find("AmountToCraftText").GetComponent<Text>();
        BackgroundCraftImage = itemToCraftPanel.transform.Find("BackgroundRarity").GetComponent<Image>();
        AmountOwnedCraftText = itemToCraftPanel.transform.Find("AmountOwnedText").GetComponent<Text>();

        var itemToDestroyPanel = CraftPanel.transform.Find("ItemToDestroy");
        CurrentDestroyImage = itemToDestroyPanel.transform.Find("CurrentDestroy").GetComponent<Image>();
        AmountToDestroyText = itemToDestroyPanel.transform.Find("AmountToDestroyText").GetComponent<Text>();
        BackgroundDestroyImage = itemToDestroyPanel.transform.Find("BackgroundRarity").GetComponent<Image>();
        AmountOwnedDestroyText = itemToDestroyPanel.transform.Find("AmountOwnedText").GetComponent<Text>();

        CostText = CraftPanel.transform.Find("CostText").GetComponent<Text>();
        if (isCraftingDie)
        {
            (CurrentCraftImage.transform as RectTransform).sizeDelta = new Vector2(225, 225);
            (CurrentDestroyImage.transform as RectTransform).sizeDelta = new Vector2(225, 225);
        }else{
            (CurrentCraftImage.transform as RectTransform).sizeDelta = new Vector2(250, 250);
            (CurrentDestroyImage.transform as RectTransform).sizeDelta = new Vector2(250, 250);
        }
        
        SetSkinData();
    }

    public void NextDestroyOption(bool forward)
    {
        if (forward)
        {
            if (canDestroyIndex < canDestroy.Count-1)
                canDestroyIndex++;
            else
                canDestroyIndex = 0;

        }
        else
        {
            if (canDestroyIndex > 0)
                canDestroyIndex--;
            else
                canDestroyIndex = canDestroy.Count - 1;

        }

        SetSkinData();

        //if (currentDestroyItem.Rarity == 1)
        //{
        //    AmountToCraft = 1;
        //    if (currentCraftItem.Rarity == 1)
        //    {
        //        AmountToDestroy = 1;
        //    }
        //    else if (currentCraftItem.Rarity == 2)
        //    {
        //        AmountToDestroy = 5;
        //    }
        //    else if (currentCraftItem.Rarity == 3)
        //    {
        //        AmountToDestroy = 25;
        //    }
        //}
        //else if (currentDestroyItem.Rarity == 2)
        //{
        //    if (currentCraftItem.Rarity == 1)
        //    {
        //        AmountToCraft = 3;
        //        AmountToDestroy = 1;
        //    }
        //    else if (currentCraftItem.Rarity == 2)
        //    {
        //        AmountToCraft = 1;
        //        AmountToDestroy = 1;
        //    }
        //    else if (currentCraftItem.Rarity == 3)
        //    {
        //        AmountToCraft = 1;
        //        AmountToDestroy = 5;
        //    }
        //} 
        //else if (currentDestroyItem.Rarity == 3)
        //{
        //    AmountToDestroy = 1;
        //    if (currentCraftItem.Rarity == 1)
        //    {
        //        AmountToCraft = 9;
        //    }
        //    else if (currentCraftItem.Rarity == 2)
        //    {
        //        AmountToCraft = 3;
        //    }
        //    else if (currentCraftItem.Rarity == 3)
        //    {
        //        AmountToCraft = 1;
        //    }
        //}
    }

    private void SetSkinData()
    {
        currentDestroyItem = canDestroy[canDestroyIndex];
        BackgroundCraftImage.sprite = currentCraftItem.Rarity == 3 ? EpicSprite : currentCraftItem.Rarity == 2 ? RareSprite : CommonSprite;
        BackgroundDestroyImage.sprite = currentDestroyItem.Rarity == 3 ? EpicSprite : currentDestroyItem.Rarity == 2 ? RareSprite : CommonSprite;
        AmountOwnedCraftText.text = currentCraftItem.UnlockedQty.ToString();
        AmountOwnedDestroyText.text = currentDestroyItem.UnlockedQty.ToString();
        
        //diff positive then destroying a better item
        if (isCraftingDie)
        {
            CurrentCraftImage.sprite = MainMenuController.i.DieSprites[currentCraftItem.SkinId - 1];
            CurrentDestroyImage.sprite = MainMenuController.i.DieSprites[currentDestroyItem.SkinId - 1];
        }
        else
        {
            CurrentCraftImage.sprite = MainMenuController.i.IngSprites[currentCraftItem.SkinId - 1];
            CurrentDestroyImage.sprite = MainMenuController.i.IngSprites[currentDestroyItem.SkinId - 1];
        }
        SetCraftCosts(currentDestroyItem.Rarity);
        AmountToCraftText.text = AmountToCraft.ToString();
        AmountToDestroyText.text = AmountToDestroy.ToString();
        CostText.text = $"Cost to Craft: " + ((AmountToCraft + AmountToDestroy) * (isCraftingDie ? 50 : 100)).ToString() + " Calories";
    }

    private int SetCraftCosts(int deleteRarity)
    {
        var difference = deleteRarity - currentCraftItem.Rarity;
        AmountToCraft = (int)(difference > 0 ? Math.Pow(isCraftingDie ? 3: 2, difference) : 1);
        AmountToDestroy = (int)(difference > 0 ? 1 : Math.Pow(isCraftingDie ? 5 : 3, Math.Abs(difference)));
        return AmountToDestroy;
    }

    private void RefreshSkinsCallback(string obj = null)
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
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        (ItemPrefabObj.transform.Find("Item").transform as RectTransform).sizeDelta = new Vector2(230, 230);
        ItemPrefabObj.transform.Find("Item").gameObject.GetComponent<Image>().sprite = item;
        
        SkinData skin = new SkinData();
        if (MyIngSkins.Any(x => x.SkinId == skinId))
        {
            skin = MyIngSkins.FirstOrDefault(x => x.SkinId == skinId);
            ItemPrefabObj.GetComponentInChildren<Text>().text = skin.UnlockedQty.ToString();
        }
        else if (skinId >= 0 && skinId <= 3)
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = true,
                Rarity = skinId < 13 ? 1 : skinId < 17 ? 2 : 3
            };
            ItemPrefabObj.GetComponentInChildren<Text>().text = "";
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = false,
                Rarity = skinId < 13 ? 1 : skinId < 19 ? 2 : 3
            };
            ItemPrefabObj.GetComponentInChildren<Text>().text = "0";
        }
        if (Settings.IsDebug)
            skin.IsUnlocked = true;
        
        ItemPrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        ItemPrefabObj.transform.Find("Rarity").gameObject.GetComponent<Image>().sprite = skin.Rarity == 3 ? EpicSprite : skin.Rarity == 2 ? RareSprite : CommonSprite;
        Button newButton = Instantiate(ItemPrefabObj, IngButtonContent.transform);
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
                IngToPurchase = item;
                DieToPurchase = null;
                if (item.UnlockedQty > 9)
                {
                    UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unlock Ingredient?";
                    UnlockPanel.transform.Find("Question").GetComponent<Text>().text = $"Do you want to spend 4 of these Ingredient skins, representing the 4 playing pieces on your team, to unlock this?";
                }
                else
                {
                    UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Craft Ingredient?";
                    UnlockPanel.transform.Find("Question").GetComponent<Text>().text = $"You need 4 of these Ingredient skins to unlock this, would you like to try and craft some?";
                }
                UnlockPanel.SetActive(true);
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
        MainMenuController.i.IngSprites.ForEach(x => { j++; CreateIng(x, j); });
    }
    #endregion

    #region Dice
    private void CreateDice(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Settings.LoggedInPlayer.SelectedDice.Contains(skinId))
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        (ItemPrefabObj.transform.Find("Item").transform as RectTransform).sizeDelta = new Vector2(205, 205);
        ItemPrefabObj.transform.Find("Item").gameObject.GetComponent<Image>().sprite = item;
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
                UnlockedQty = 0,
                Rarity = skinId < 10 ? 1 : skinId < 16 ? 2 : 3
            };
        }
        if(Settings.IsDebug)
            skin.IsUnlocked = true;

        ItemPrefabObj.transform.Find("Rarity").gameObject.GetComponent<Image>().sprite = skin.Rarity == 3 ? EpicSprite : skin.Rarity == 2 ? RareSprite : CommonSprite;
        ItemPrefabObj.GetComponentInChildren<Text>().text = skin.UnlockedQty.ToString();
        ItemPrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        Button newButton = Instantiate(ItemPrefabObj, DiceButtonContent.transform);
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
                IngToPurchase = null;
                DieToPurchase = item;
                if (item.UnlockedQty > 9)
                {
                    UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unlock Die?";
                    UnlockPanel.transform.Find("Question").GetComponent<Text>().text = $"Do you want to spend 10 of these Die skins, representing the 10 sides of the die, to unlock this?";
                }
                else
                {
                    UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Craft Die?";
                    UnlockPanel.transform.Find("Question").GetComponent<Text>().text = $"You need 10 of these die skins to unlock this, would you like to try and craft some?";
                }
                UnlockPanel.SetActive(true);
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
        MainMenuController.i.DieSprites.ForEach(x => { j++; CreateDice(x, j); });
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
        newButton.onClick.AddListener(() => SelectTitle(skin));
        newButton.transform.Find("Info").gameObject.GetComponent<Button>().onClick.AddListener(() => DisplayAlert(item.SkinDesc, "Info"));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllTitles.Add(skin);
        TitleButtonLog.Add(newButton);
    }

    private void DisplayAlert(string skinDesc, string bannerText)
    {
        alert.transform.Find("Banner").GetComponentInChildren<Text>().text = bannerText;
        alert.transform.Find("AlertText").GetComponent<Text>().text = skinDesc;
        alert.SetActive(true);
    }

    private void SelectTitle(SkinData item)
    {
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
                DisplayAlert(item.SkinDesc,"Locked");
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
        if (lastSelected == 1)
        {

        }
        else if (lastSelected == 2)
        {

        }
        else if (lastSelected == 3)
        {

        }
        lastSelected = Selected;
        if (Selected != 1)
        {
            IngPanel.SetActive(false);
            IngButtonImage.sprite = UnselectedTabSprite;
        }
        else
        {
            Settings.hasNewIng = false;
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
            Settings.hasNewDie = false;
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
            Settings.hasNewTitle = false;
            TitlePanel.SetActive(true);
            TitleButtonImage.sprite = SelectedTabSprite;
        }
        hasNewIng.SetActive(Settings.hasNewIng);
        hasNewDie.SetActive(Settings.hasNewDie);
        hasNewTitle.SetActive(Settings.hasNewTitle);
    }

    #endregion region
}
