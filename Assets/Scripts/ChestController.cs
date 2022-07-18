using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChestController : MonoBehaviour
{
    public GameObject RewardContent;
    public List<GameObject> Slots;
    public List<Image> ChestImages;
    public List<Image> UnlockImages;
    private List<Chest> PlayerChests = new List<Chest>();
    public Image ChestToOpenSlot;
    public Text HelpText;
    public GameObject UnlockPanel;
    public Text TimerText;
    private List<GameObject> RewardLog = new List<GameObject>();
    public Sprite EmptySlot;
    public Sprite UnselectedChestSprite;
    public Sprite SelectedChestSprite;
    #region Dice
    public GameObject DiePrefab;
    public Sprite SmallPack;
    public Sprite MediumPack;
    public Sprite LargePack;
    #endregion
    #region Ings
    public GameObject IngPrefab;
    public Sprite IngSmallPack;
    public Sprite IngMediumPack;
    public Sprite IngLargePack;
    #endregion
    public Sprite EpicBackground;
    public Sprite RareBackground;
    private DateTime TimeNow;
    private float elapsed = 1;
    public GameObject PurchaseSpeedPanel;
    public class Chest
    {
        public int ChestId { get; set; }
        public int ChestSize { get; set; }
        public int ChestTypeId { get; set; }
        public DateTime? FinishUnlock { get; set; }
    }  
    private SqlController sql;
    private Chest SelectedChest;
    private int? SlotSelected = null;
    private string defualtText;
    private bool isOpening = false;
    private void Awake()
    {
        sql = new SqlController();
        defualtText = HelpText.text;
    }
    private void FixedUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= 1f)
        {
            elapsed = elapsed % 1f;
            UpdateTime();
        }
    }
    private void OnEnable()
    {
        StartCoroutine(sql.RequestRoutine($"skin/GetMyChests?UserId={Settings.LoggedInPlayer.UserId}", GetMyChestsCallback));
    }
    private void GetMyChestsCallback(string data)
    {
        PlayerChests = sql.jsonConvert<List<Chest>>(data);
        var j = 0;
        PlayerChests.ForEach(x => { CreateChests(x,j); j++; });
    }
    
    private void OpenChestsCallback(string data)
    {
        var unlocks = sql.jsonConvert<List<Skin>>(data);
        ClearChests();
        MainMenuController.i.HasUnlock.SetActive(true);
        var j = 0;
        PlayerChests.ForEach(x => { CreateChests(x,j); j++; });
        unlocks.ToList().ForEach(x => { ShowUnlocks(x); });
    }

    private void ShowUnlocks(Skin x)
    {
        if (x.SkinType == 1)
        {
            IngPrefab.transform.Find("IngImage").GetComponent<Image>().sprite = MainMenuController.i.IngSprites[x.SkinId - 1];
            if (x.Rarity == 3)
            {
                IngPrefab.transform.Find("Rarity").GetComponent<Image>().sprite = EpicBackground;
                IngPrefab.transform.Find("RarityText").GetComponent<Text>().text = "Epic!";
            }
            else if (x.Rarity == 2)
            {
                IngPrefab.transform.Find("Rarity").GetComponent<Image>().sprite = RareBackground;
                IngPrefab.transform.Find("RarityText").GetComponent<Text>().text = "Rare!";
            }
            else
            {
                IngPrefab.transform.Find("Rarity").GetComponent<Image>().sprite = EmptySlot;
                IngPrefab.transform.Find("RarityText").GetComponent<Text>().text = "";
            }

            var Ing = Instantiate(IngPrefab, RewardContent.transform);
            RewardLog.Add(Ing);
        }
        if (x.SkinType == 2)
        {
            DiePrefab.transform.Find("DieImage").GetComponent<Image>().sprite = MainMenuController.i.DieSprites[x.SkinId - 1];
            DiePrefab.transform.Find("DieImage").transform.Find("DieNumber").GetComponent<Text>().text = x.UnlockedQty.ToString();
            if (x.Rarity == 3)
            {
                DiePrefab.transform.Find("Rarity").GetComponent<Image>().sprite = EpicBackground;
                DiePrefab.transform.Find("RarityText").GetComponent<Text>().text = "Epic!";
            }
            else if (x.Rarity == 2)
            {
                DiePrefab.transform.Find("Rarity").GetComponent<Image>().sprite = RareBackground;
                DiePrefab.transform.Find("RarityText").GetComponent<Text>().text = "Rare!";
            }
            else
            {
                DiePrefab.transform.Find("Rarity").GetComponent<Image>().sprite = EmptySlot;
                DiePrefab.transform.Find("RarityText").GetComponent<Text>().text = "";
            }

            var die = Instantiate(DiePrefab, RewardContent.transform);
            RewardLog.Add(die);
        }
    }
    public void HideUnlocks()
    {
        UnlockPanel.SetActive(false);
        if (RewardLog.Count > 0)
        {
            for (int i = RewardLog.Count - 1; i >= 0; i--)
            {
                Destroy(RewardLog[i].gameObject);
                RewardLog.Remove(RewardLog[i]);
            }
        }
    }
    private void CreateChests(Chest chest, int chestSlot)
    {
        if (chest.ChestTypeId == 1)
            ChestImages[chestSlot].sprite = chest.ChestSize == 3 ? IngLargePack : chest.ChestSize == 2 ? IngMediumPack : IngSmallPack;
        else if (chest.ChestTypeId == 2)
            ChestImages[chestSlot].sprite = chest.ChestSize == 3 ? LargePack : chest.ChestSize == 2 ? MediumPack : SmallPack;

        if (chest.FinishUnlock != null)
        {
            SetSelected(chestSlot);
        }

        Slots[chestSlot].GetComponent<Button>().interactable = true;
    }  
    
    private void ClearChests()
    {
        ChestImages.ForEach(x => x.sprite = EmptySlot);
        Slots.ForEach(x => x.GetComponent<Button>().interactable = false);
    }

    public void PressChest(int slotSelected)
    {
        if (!isOpening)
        {
            if (!PlayerChests.Any(x => x.FinishUnlock != null))
            {
                SetSelected(slotSelected);
            }
            else
            {
                MainMenuController.i.alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Unlock In Progress";
                MainMenuController.i.alert.transform.Find("AlertText").GetComponent<Text>().text = $"You can't select another pack to unlock until the current one is done!";
                MainMenuController.i.alert.SetActive(true);
            }
        }
    }

    private void SetSelected(int slotSelected)
    {
        HideUnlocks();
        SlotSelected = slotSelected;
        SelectedChest = PlayerChests[slotSelected];
        ChestToOpenSlot.sprite = ChestImages[slotSelected].sprite;
        Slots.ForEach(x => x.GetComponent<Image>().sprite = UnselectedChestSprite);
        Slots[slotSelected].GetComponent<Image>().sprite = SelectedChestSprite;
        if (SelectedChest != null && SelectedChest.FinishUnlock != null)
        {
            UpdateTime();
            HelpText.text = "";
        }
        else {
            HelpText.text = $"Press to start unlocking your {(PlayerChests[slotSelected].ChestSize == 3 ? "large" : PlayerChests[slotSelected].ChestSize == 2 ? "medium" : "small")} {(PlayerChests[slotSelected].ChestTypeId == 2 ? "dice pack" : "ingredient crate")}!";
        }
    }

    public void PressOpen()
    {
        if (SlotSelected != null && !isOpening)
        {
            if (SelectedChest.FinishUnlock == null)
            {
                StartCoroutine(sql.RequestRoutine($"skin/StartChestUnlock?ChestId={SelectedChest.ChestId}", UpdateChestTimerCallback));
            }
            else if (SelectedChest.FinishUnlock < TimeNow)
            {
                StartCoroutine(OpenChest());
            }
            else
            {
                if (Settings.LoggedInPlayer.Calories >= getSpeedUpCost() || Settings.LoggedInPlayer.Level < 5)
                {
                    var message = $"Want to speed up this pack up? \n";
                    if (Settings.LoggedInPlayer.Level < 5)
                    {
                        message += $"It's free for players under level 5!";
                    }
                    else
                    {
                        message += $"It'll cost you {getSpeedUpCost()} Calories.";
                    }
                    PurchaseSpeedPanel.transform.Find("PurchaseCost").GetComponent<Text>().text = message;
                    PurchaseSpeedPanel.SetActive(true);
                    UpdateTime();
                }
                else
                {
                    MainMenuController.i.alert.transform.Find("Banner").GetComponentInChildren<Text>().text = "Insufficent Funds";
                    MainMenuController.i.alert.transform.Find("AlertText").GetComponent<Text>().text = $"It costs {getSpeedUpCost()} Calories to speed up this unlock but you only have {Settings.LoggedInPlayer.Calories} Calories :(";
                    MainMenuController.i.alert.SetActive(true);
                }
            }
        }
    }

    private double getSpeedUpCost()
    {
        var time = (DateTime)SelectedChest.FinishUnlock - TimeNow;
        return (int)time.TotalMinutes;
    }

    public void PurchaseTime()
    {
        if (Settings.LoggedInPlayer.Calories >= getSpeedUpCost() || Settings.LoggedInPlayer.Level < 5)
        {
            StartCoroutine(sql.RequestRoutine($"skin/PurchaseChestUnlock?ChestId={SelectedChest.ChestId}", UpdateChestTimerCallback));
            PurchaseSpeedPanel.SetActive(false);
            StartCoroutine(OpenChest());
        }
    }
    private void UpdateTime()
    {
        TimeNow = DateTime.UtcNow.AddHours(-4);
        if (SelectedChest != null && SelectedChest.FinishUnlock != null)
        {
            HelpText.text = "";
            TimeSpan time = (DateTime)SelectedChest.FinishUnlock - TimeNow;
            if (time.Hours > 0)
            {
                TimerText.text = "Time until unlock: " + time.ToString(@"h\:mm\:ss");
            }
            else if (time.Seconds > 0)
            {
                TimerText.text = "Time until unlock: " + time.ToString(@"m\:ss");
            }
            else
            {
                if(!isOpening)
                    TimerText.text = "Press to open your pack!!!";
                else
                    TimerText.text = "";
            }
        }
        else
        {
            TimerText.text = "";
        }
    }

    private void UpdateChestTimerCallback(string data)
    {
        SelectedChest.FinishUnlock = sql.jsonConvert<DateTime>(data);
        UpdateTime();
    }

    private IEnumerator OpenChest()
    {
        if (!isOpening)
        {
            isOpening = true;
            TimerText.text = "";
            if (SelectedChest.ChestTypeId == 1)
            {
                Settings.hasNewIng = true;
            }
            else if (SelectedChest.ChestTypeId == 2)
            {
                Settings.hasNewDie = true;
            }
            Slots.ForEach(x =>
            {
                x.GetComponent<Image>().sprite = UnselectedChestSprite;
                x.GetComponent<Button>().interactable = false;
            });
            PlayerChests.RemoveAt((int)SlotSelected);
            if (SelectedChest.ChestTypeId == 1)
            {
                RewardContent.GetComponent<GridLayoutGroup>().cellSize = new Vector2(300, 300);
            }
            else if (SelectedChest.ChestTypeId == 2)
            {
                RewardContent.GetComponent<GridLayoutGroup>().cellSize = new Vector2(200, 200);
            }
            HelpText.text = "";
            ChestToOpenSlot.gameObject.GetComponent<Animation>().Play("DiceShaker");
            yield return StartCoroutine(sql.RequestRoutine($"skin/OpenMyChest?UserId={Settings.LoggedInPlayer.UserId}&ChestId={SelectedChest.ChestId}", OpenChestsCallback));
            yield return new WaitForSeconds(1.25f);
            ChestToOpenSlot.sprite = EmptySlot;
            UnlockPanel.SetActive(true);
            HelpText.text = defualtText;
            SelectedChest = null;
            SlotSelected = null;
            isOpening = false;
        }
    }
}
