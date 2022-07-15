using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChestController : MonoBehaviour
{
    public GameObject DiePrefab;
    public GameObject DieContent;
    public List<GameObject> Slots;
    public List<Image> ChestImages;
    public List<Image> UnlockImages;
    private List<Chest> PlayerChests = new List<Chest>();
    public Image ChestToOpenSlot;
    public Text HelpText;
    public GameObject UnlockPanel;
    private List<GameObject> DieLog = new List<GameObject>();
    public Sprite EmptySlot;
    public Sprite UnselectedChest;
    public Sprite SelectedChest;
    public Sprite SmallPack;
    public Sprite MediumPack;
    public Sprite LargePack;
    public Sprite EpicBackground;
    public Sprite RareBackground;
    public class Chest
    {
        public int ChestId { get; set; }
        public int ChestSize { get; set; }
    }  
    private SqlController sql;
    private int SelectedChestId = 0;
    private int? SlotSelected = null;
    private string defualtText;
    private void Awake()
    {
        sql = new SqlController();
        defualtText = HelpText.text;
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
        var j = 0;
        PlayerChests.ForEach(x => { CreateChests(x,j); j++; });
        unlocks.ToList().ForEach(x => { ShowUnlocks(x); });
    }

    private void ShowUnlocks(Skin x)
    {
        DiePrefab.transform.Find("DieImage").GetComponent<Image>().sprite = MainMenuController.i.diceSprites[x.SkinId-1];
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

        var die = Instantiate(DiePrefab, DieContent.transform);
        DieLog.Add(die);
    }
    public void HideUnlocks()
    {
        UnlockPanel.SetActive(false);
        if (DieLog.Count > 0)
        {
            for (int i = DieLog.Count - 1; i >= 0; i--)
            {
                Destroy(DieLog[i].gameObject);
                DieLog.Remove(DieLog[i]);
            }
        }
    }
    private void CreateChests(Chest chest, int chestSlot)
    {
        ChestImages[chestSlot].sprite = chest.ChestSize == 3 ? LargePack : chest.ChestSize == 2 ? MediumPack : SmallPack;
        Slots[chestSlot].GetComponent<Button>().interactable = true;
    }  
    
    private void ClearChests()
    {
        ChestImages.ForEach(x => x.sprite = EmptySlot);
        Slots.ForEach(x => x.GetComponent<Button>().interactable = false);
    }

    public void PressChest(int slotSelected)
    {
        HideUnlocks();
        HelpText.text = $"Press to open your {(PlayerChests[slotSelected].ChestSize == 3 ? "large" : PlayerChests[slotSelected].ChestSize == 2 ? "medium" : "small")} dice pack!";
        SlotSelected = slotSelected;
        SelectedChestId = PlayerChests[slotSelected].ChestId;
        ChestToOpenSlot.sprite = ChestImages[slotSelected].sprite;
        Slots.ForEach(x => x.GetComponent<Image>().sprite = UnselectedChest);
        Slots[slotSelected].GetComponent<Image>().sprite = SelectedChest;
    }

    public void PressOpen()
    {
        if (SlotSelected != null)
        {
            Slots.ForEach(x => x.GetComponent<Button>().interactable = false);
            StartCoroutine(OpenChest());
            PlayerChests.RemoveAt((int)SlotSelected);
            SelectedChestId = 0;
            Slots.ForEach(x => x.GetComponent<Image>().sprite = UnselectedChest);
            SlotSelected = null;
        }
    }

    private IEnumerator OpenChest()
    {
        HelpText.text = "";
        ChestToOpenSlot.gameObject.GetComponent<Animation>().Play("DiceShaker");
        yield return StartCoroutine(sql.RequestRoutine($"skin/OpenMyChest?UserId={Settings.LoggedInPlayer.UserId}&ChestId={SelectedChestId}", OpenChestsCallback));
        yield return new WaitForSeconds(1.25f);
        ChestToOpenSlot.sprite = EmptySlot;
        UnlockPanel.SetActive(true);
        HelpText.text = defualtText;
    }
}
