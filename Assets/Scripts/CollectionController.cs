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
    public GameObject AreYouSurePanel;
    private SqlController sql;
    private Skin DieToPurchase;
    private Skin IngToPurchase;
    public static CollectionController i;
    public GameObject hasNewIng;
    public GameObject hasNewDie;
    public GameObject hasNewTitle;
    private int lastSelected = 1;
    private int craftCost = 1000;
    private int rarityCost = 2000;
    

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
        hasNewIng.SetActive(Global.hasNewIng);
        hasNewDie.SetActive(Global.hasNewDie);
        hasNewTitle.SetActive(Global.hasNewTitle);
        TabClicked(lastSelected);
    }
    public void UnlockItem()
    {
        UnlockPanel.SetActive(false);
        if (DieToPurchase != null)
        {
            if (DieToPurchase.UnlockedQty > 9)
            {
                StartCoroutine(sql.RequestRoutine($"skin/UnlockDiceSkin?UserId={Global.LoggedInPlayer.UserId}&SkinId={DieToPurchase.SkinId}", RefreshSkinsCallback));
                DieToPurchase = null;
                Global.LoggedInPlayer.Calories -= rarityCost;
            }
            else
            {
                if (MyDiceSkins.Count > 1)
                {
                    currentCraftItem = DieToPurchase;
                    isCraftingDie = true;
                    SetDestroyableDie();
                    if (canDestroy.Count != 0)
                    {
                        OpenCraftPanel();
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Unable to craft", "You need to collect more skins before being able to craft this!");
                    }
                }
                else
                {
                    MainMenuController.i.DisplayAlert("Unable to craft", "You need to collect more skins before being able to craft this!");
                }
            }
        }
        else if (IngToPurchase != null)
        {
            if (IngToPurchase.UnlockedQty > 3)
            {
                StartCoroutine(sql.RequestRoutine($"skin/UnlockIngSkin?UserId={Global.LoggedInPlayer.UserId}&SkinId={IngToPurchase.SkinId}", RefreshSkinsCallback));
                IngToPurchase = null;
                Global.LoggedInPlayer.Calories -= rarityCost;
            }
            else
            {
                if (MyIngSkins.Count > 1)
                {
                    currentCraftItem = IngToPurchase;
                    isCraftingDie = false;
                    SetDestroyableDie();
                   
                    if (canDestroy.Count != 0)
                    {
                        OpenCraftPanel();
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Unable to craft", "You need to collect more skins before being able to craft this!");
                    }
                }
                else
                {
                    MainMenuController.i.DisplayAlert("Unable to craft", "You need to collect more skins before being able to craft this!");
                }
            }
        }
    }

    private void SetDestroyableDie()
    {
        canDestroy.Clear();
        var list = isCraftingDie ? MyDiceSkins : MyIngSkins;
        list.ForEach(x => {
            if (x.SkinId != currentCraftItem.SkinId && x.UnlockedQty >= SetCraftCosts(x.Rarity))
                canDestroy.Add(x);
        });
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
        //if (isCraftingDie)
        //{
        //    (CurrentCraftImage.transform as RectTransform).sizeDelta = new Vector2(225, 225);
        //    (CurrentDestroyImage.transform as RectTransform).sizeDelta = new Vector2(225, 225);
        //}else{
        //    (CurrentCraftImage.transform as RectTransform).sizeDelta = new Vector2(250, 250);
        //    (CurrentDestroyImage.transform as RectTransform).sizeDelta = new Vector2(250, 250);
        //}
        
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
        craftCost = (currentCraftItem.Rarity == 3 ? 450 : currentCraftItem.Rarity == 2 ? 150 : 50) * (isCraftingDie ? 1 : 2);
        CostText.text = $"Cost to Craft: " + craftCost.ToString() + " Calories";
    }

    public void TryToCraftDie()
    {
        if (Global.LoggedInPlayer.Calories >= craftCost)
        {
            AreYouSurePanel.transform.Find("Question").GetComponent<Text>().text = $"Are you sure you want to spend {craftCost} of your {Global.LoggedInPlayer.Calories} Calories to craft this skin?";
            AreYouSurePanel.SetActive(true);
        }
        else
        {
            MainMenuController.i.DisplayAlert("Insufficent Funds", $"To craft this it costs {craftCost} Calories, but you only have {Global.LoggedInPlayer.Calories} :(");
        }
    }

    public void CraftDie()
    {
        MainMenuController.i.DisplayLoading("Loading","Crafting your item...");
        AreYouSurePanel.SetActive(false);
        Global.LoggedInPlayer.Calories -= craftCost;
        StartCoroutine(sql.RequestRoutine($"skin/CraftSkins?UserId={Global.LoggedInPlayer.UserId}&ToDeleteSkinId={currentDestroyItem.SkinId}&ToCraftSkinId={currentCraftItem.SkinId}&isDie={isCraftingDie}", RefreshCraftedSkinsCallback));
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
        StartCoroutine(sql.RequestRoutine($"skin/GetMyIngredientSkins?UserId={Global.LoggedInPlayer.UserId}", GetMyIngredientCallback));
        StartCoroutine(sql.RequestRoutine($"skin/GetMyDiceSkins?UserId={Global.LoggedInPlayer.UserId}", GetMyDiceCallback));
        StartCoroutine(sql.RequestRoutine($"skin/GetMyTitles?UserId={Global.LoggedInPlayer.UserId}", GetMyTitlesCallback));
    }
    private void RefreshCraftedSkinsCallback(string obj = null)
    {
        RefreshSkinsCallback();
        currentDestroyItem.UnlockedQty -= AmountToDestroy;
        currentCraftItem.UnlockedQty += AmountToCraft;
        if (isCraftingDie)
        {
            MyDiceSkins.FirstOrDefault(x=>x.SkinId == currentDestroyItem.SkinId).UnlockedQty = currentDestroyItem.UnlockedQty;
        }
        else
        {
            MyIngSkins.FirstOrDefault(x => x.SkinId == currentDestroyItem.SkinId).UnlockedQty = currentDestroyItem.UnlockedQty;
        }
        SetDestroyableDie();
        if (canDestroy.Count == 0)
        {
            CraftPanel.SetActive(false);
        }
        else
        {
            if (canDestroyIndex > canDestroy.Count - 1 || currentDestroyItem.SkinId != canDestroy[canDestroyIndex].SkinId)
            {
                canDestroyIndex = 0;
            }
            SetSkinData();
        }
        MainMenuController.i.HideLoading();
    }
    #region Ingredients
    private void CreateIng(Sprite item, int skinId)
    {
        bool isSelected = false;
        if (Global.LoggedInPlayer.SelectedIngs.Contains(skinId))
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        //(ItemPrefabObj.transform.Find("Item").transform as RectTransform).sizeDelta = new Vector2(230, 230);
        ItemPrefabObj.transform.Find("Item").gameObject.GetComponent<Image>().sprite = item;
        
        SkinData skin = new SkinData();
        if (MyIngSkins.Any(x => x.SkinId == skinId))
        {
            skin = MyIngSkins.FirstOrDefault(x => x.SkinId == skinId);
            ItemPrefabObj.GetComponentInChildren<Text>().text = (skin.IsUnlocked ? "" : skin.UnlockedQty.ToString() + "/4");
        }
        else if (skinId >= 0 && skinId <= 4)
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = true,
                Rarity = skinId < 15 ? 1 : skinId < 22 ? 2 : 3
            };
            ItemPrefabObj.GetComponentInChildren<Text>().text = "";
        }
        else
        {
            skin = new SkinData()
            {
                SkinId = skinId,
                IsUnlocked = false,
                Rarity = skinId < 15 ? 1 : skinId < 22 ? 2 : 3
            };
            ItemPrefabObj.GetComponentInChildren<Text>().text = "0/4";
        }
        if (Global.IsDebug)
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
            Global.LoggedInPlayer.SelectedIngs.Remove(item.SkinId);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateIngredientSkins?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Global.LoggedInPlayer.SelectedIngs.Add(item.SkinId);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateIngredientSkins?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                IngToPurchase = item;
                DieToPurchase = null;
                rarityCost = (item.Rarity == 3 ? 1500 : item.Rarity == 2 ? 1000 : 500);
                if (item.UnlockedQty > 3)
                {
                    if (Global.LoggedInPlayer.Calories >= rarityCost)
                    {
                        DisplayUnlock("Unlock Ingredient?", $"Do you want to spend 4 of these skins and {rarityCost} of your {Global.LoggedInPlayer.Calories} Calories to unlock this?");
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Insufficent Funds", $"You need {rarityCost} calories to unlock this, but you only have {Global.LoggedInPlayer.Calories}");
                    }
                }
                else
                {
                    if (item.UnlockedQty > 0)
                    {
                        UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = "Craft Ingredient?";
                        UnlockPanel.transform.Find("Question").GetComponent<Text>().text = $"You need 4 of these Ingredient skins to unlock this skin in game, would you like to try and craft some?";
                        UnlockPanel.SetActive(true);
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Locked", "Open more packs to unlock this skin in game!");
                    }
                }
            }
        }

    }

    private void DisplayUnlock(string title, string body)
    {
        UnlockPanel.transform.Find("Banner").GetComponentInChildren<Text>().text = title;
        UnlockPanel.transform.Find("Question").GetComponent<Text>().text = body;
        UnlockPanel.SetActive(true);
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
        if (Global.LoggedInPlayer.SelectedDice.Contains(skinId))
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(true);
            isSelected = true;
        }
        else
        {
            ItemPrefabObj.transform.Find("Selected").gameObject.SetActive(false);
        }
        //(ItemPrefabObj.transform.Find("Item").transform as RectTransform).sizeDelta = new Vector2(205, 205);
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
        if(Global.IsDebug)
            skin.IsUnlocked = true;

        ItemPrefabObj.transform.Find("Rarity").gameObject.GetComponent<Image>().sprite = skin.Rarity == 3 ? EpicSprite : skin.Rarity == 2 ? RareSprite : CommonSprite;
        ItemPrefabObj.GetComponentInChildren<Text>().text =  (skin.IsUnlocked ? "" : skin.UnlockedQty.ToString() + "/10"); ;
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
            Global.LoggedInPlayer.SelectedDice.Remove(item.SkinId);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateDiceSkins?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Global.LoggedInPlayer.SelectedDice.Add(item.SkinId);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateDiceSkins?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                IngToPurchase = null;
                DieToPurchase = item;
                rarityCost = (item.Rarity == 3 ? 1500 : item.Rarity == 2 ? 1000 : 500);
                if (item.UnlockedQty > 9)
                {
                    if (Global.LoggedInPlayer.Calories >= rarityCost)
                    {
                        DisplayUnlock("Unlock Die?", $"Do you want to spend 10 of these skins and {rarityCost} of your {Global.LoggedInPlayer.Calories} Calories to unlock this?");
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Insufficent Funds", $"You need {rarityCost} calories to unlock this, but you only have {Global.LoggedInPlayer.Calories}");
                    }
                }
                else
                {
                    if (item.UnlockedQty > 1)
                    {
                        DisplayUnlock("Craft Die?", $"You need 10 of these die skins to unlock this skin in game, would you like to try and craft some?");
                    }
                    else
                    {
                        MainMenuController.i.DisplayAlert("Locked", "Open more packs to unlock this skin in game!");
                    }
                }
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
        if (Global.LoggedInPlayer.SelectedTitles.Contains(item.SkinName))
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
        if(Global.IsDebug || item.SkinId == 1 || item.SkinId == 2)
            skin.IsUnlocked = true;
        TitlePrefabObj.transform.Find("Lock").gameObject.SetActive(!skin.IsUnlocked);
        TitlePrefabObj.transform.Find("Info").gameObject.SetActive(skin.IsUnlocked);
        Button newButton = Instantiate(TitlePrefabObj, TitleButtonContent.transform);
        newButton.onClick.AddListener(() => SelectTitle(skin));
        newButton.transform.Find("Info").gameObject.GetComponent<Button>().onClick.AddListener(() => MainMenuController.i.DisplayAlert("Info",item.SkinDesc));
        skin.SkinButton = newButton;
        skin.IsSelected = isSelected;
        AllTitles.Add(skin);
        TitleButtonLog.Add(newButton);
    }

    private void SelectTitle(SkinData item)
    {
        if (item.IsSelected)
        {
            item.SkinButton.transform.Find("Selected").gameObject.SetActive(false);
            item.IsSelected = false;
            Global.LoggedInPlayer.SelectedTitles.Remove(item.SkinName);
            StartCoroutine(sql.RequestRoutine($"skin/UpdateTitle?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=false"));
        }
        else
        {
            if (item.IsUnlocked)
            {
                item.SkinButton.transform.Find("Selected").gameObject.SetActive(true);
                item.IsSelected = true;
                Global.LoggedInPlayer.SelectedTitles.Add(item.SkinName);
                StartCoroutine(sql.RequestRoutine($"skin/UpdateTitle?UserId={Global.LoggedInPlayer.UserId}&skinId={item.SkinId}&add=true"));
            }
            else
            {
                MainMenuController.i.DisplayAlert("Locked", item.SkinDesc);
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
            Global.hasNewIng = false;
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
            Global.hasNewDie = false;
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
            Global.hasNewTitle = false;
            TitlePanel.SetActive(true);
            TitleButtonImage.sprite = SelectedTabSprite;
        }
        hasNewIng.SetActive(Global.hasNewIng);
        hasNewDie.SetActive(Global.hasNewDie);
        hasNewTitle.SetActive(Global.hasNewTitle);
    }

    #endregion region
}
