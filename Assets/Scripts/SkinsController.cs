using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkinsController : MonoBehaviour
{

    public Text meatButtonText;
    public Text meatNameText;
    public Text meatRequiredText;
    public Text veggieButtonText;
    public Text veggieNameText;
    public Text veggieRequiredText;
    public Text fruitButtonText;
    public Text fruitNameText;
    public Text fruitRequiredText;
    public Text dieButtonText;
    public Text dieNameText;
    public Text dieRequiredText;
    public Text die2ButtonText;
    public Text die2NameText;
    public Text die2RequiredText;
    public GameObject DiePanel;
    public GameObject IngPanel;
    public GameObject loading;
    public GameObject meatPurchaseButton;
    public GameObject veggiePurchaseButton;
    public GameObject fruitPurchaseButton;
    public GameObject diePurchaseButton;
    public GameObject die2PurchaseButton;
    public Text playerWins;
    public Image currentMeatImage;
    public Image currentVeggieImage;
    public Image currentFruitImage;
    public Image currentDieImage;
    public Image currentDie2Image;
    public Sprite purpleDie;
    public Sprite yellowDie;
    public GameObject lockedMeat;
    public GameObject lockedVeggie;
    public GameObject lockedFruit;
    public GameObject lockedDie;
    public GameObject lockedDie2;
    public List<ItemImage> allMeatIngredients;
    public List<ItemImage> allVeggieIngredients;
    public List<ItemImage> allFruitIngredients;
    public List<ItemImage> allD10s;

    [System.Serializable]
    public class ItemImage
    {
        public int purchaseId;
        public Sprite image;
    }
    [System.Serializable]
    private class ItemData
    {
        public int PurchaseId = 0;
        public int PurchaseCost = 0;
        public string PurchaseName = "";
        public int LevelMinimum = 0;
        public int RequiredPurchase = 0;
    }

    private List<int> PlayerOwns;
    private List<ItemData> IngredientData;
    private SqlController sql;
    private int currentMeat = 0;
    private int currentVeggie = 0;
    private int currentFruit = 0;
    private int currentDie = 0;
    private int currentDie2 = 0;
    private string PurchaseType = "";
  
    private void Start()
    {
        sql = new SqlController();
        PlayerOwns = new List<int>();
        IngredientData = new List<ItemData>();
        //loading.SetActive(true);
        currentMeat = Settings.LoggedInPlayer.SelectedMeat;
        currentVeggie = Settings.LoggedInPlayer.SelectedVeggie;
        currentFruit = Settings.LoggedInPlayer.SelectedFruit;
        currentDie = Settings.LoggedInPlayer.SelectedDie;
        currentDie2 = Settings.LoggedInPlayer.SelectedDie2;
 
        currentMeatImage.sprite = allMeatIngredients[currentMeat].image;
        currentVeggieImage.sprite = allVeggieIngredients[currentVeggie].image;
        currentFruitImage.sprite = allFruitIngredients[currentFruit].image;
        currentDieImage.sprite = currentDie == 0 ? (Settings.LoggedInPlayer.PlayAsPurple ? purpleDie : yellowDie) : allD10s[currentDie].image;
        currentDie2Image.sprite = currentDie2 == 0 ? (Settings.LoggedInPlayer.PlayAsPurple ? purpleDie : yellowDie) : allD10s[currentDie2].image;
        SetStarsTotalText();
        StartCoroutine(sql.RequestRoutine($"purchase/GetAllPurchasables", GetAllPurchaseCallback, true));
        
    }

    public void ChoosePanel(string panel)
    {
        if (panel == "ing")
        {
            IngPanel.SetActive(true);
            DiePanel.SetActive(false);
          
        }
        else if (panel == "die")
        {
            DiePanel.SetActive(true);
            IngPanel.SetActive(false);
        }
        else
        {
            DiePanel.SetActive(false);
            IngPanel.SetActive(false);
        }
    }
    public void SetStarsTotalText()
    {
        if (Settings.LoggedInPlayer.IsGuest)
        {
            playerWins.text = @"Guests do not earn Stars.";
        }
        else
        {
            if (Settings.LoggedInPlayer.Stars == 0)
            {
                playerWins.text = @"Wow, how embarassing, you're out of Stars!";
            }
            else
            {
                playerWins.text = "You have " + Settings.LoggedInPlayer.Stars.ToString() + @" Stars!";
            }
        }
    }
    private void GetPlayerPurchaseCallback(string data)
    {
        PlayerOwns = sql.jsonConvert<List<int>>(data);
        checkMeatUnlock();
        checkVeggieUnlock();
        checkFruitUnlock();
        checkDieUnlock();
        checkDie2Unlock();
    }  
    private void GetPurchaseCallback(string data)
    {
        var newData = sql.jsonConvert<bool>(data);
        if (newData)
        {
            int cost = 0;
            if (PurchaseType == "meat")
            {
                lockedMeat.SetActive(false);
                meatPurchaseButton.SetActive(false);
                meatButtonText.text = "";
                Settings.LoggedInPlayer.SelectedMeat = currentMeat;
                cost = IngredientData.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId).PurchaseCost;
                PlayerOwns.Add(allMeatIngredients[currentMeat].purchaseId);
            }
            else if (PurchaseType == "veggie")
            {
                lockedVeggie.SetActive(false);
                veggiePurchaseButton.SetActive(false);
                veggieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedVeggie = currentVeggie;
                cost = IngredientData.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId).PurchaseCost;
                PlayerOwns.Add(allVeggieIngredients[currentVeggie].purchaseId);
            }
            else if (PurchaseType == "fruit")
            {
                lockedFruit.SetActive(false);
                fruitPurchaseButton.SetActive(false);
                fruitButtonText.text = "";
                Settings.LoggedInPlayer.SelectedFruit = currentFruit;
                cost = IngredientData.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId).PurchaseCost;
                PlayerOwns.Add(allFruitIngredients[currentFruit].purchaseId);
            }
            else if (PurchaseType == "die")
            {
                lockedDie.SetActive(false);
                diePurchaseButton.SetActive(false);
                dieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedDie = currentDie;
                cost = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie].purchaseId).PurchaseCost;
                PlayerOwns.Add(allD10s[currentDie].purchaseId);
            }
            else if (PurchaseType == "die2")
            {
                lockedDie2.SetActive(false);
                die2PurchaseButton.SetActive(false);
                die2ButtonText.text = "";
                Settings.LoggedInPlayer.SelectedDie2 = currentDie2;
                cost = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie2].purchaseId).PurchaseCost;
                PlayerOwns.Add(allD10s[currentDie2].purchaseId);
            }
            Settings.LoggedInPlayer.Stars -= cost;
            SetStarsTotalText();
            checkMeatUnlock();
            checkVeggieUnlock();
            checkFruitUnlock();
            checkDieUnlock();
            checkDie2Unlock();
        }
    } 
    private void GetAllPurchaseCallback(string data)
    {
        var ingData = sql.jsonConvert<IEnumerable<ItemData>>(data);
        IngredientData = ingData.ToList();
        StartCoroutine(sql.RequestRoutine($"purchase/GetPlayerPurchasables?UserId={Settings.LoggedInPlayer.UserId}", GetPlayerPurchaseCallback));
        //meatNameText.text = currentMeat == 0 ? "Chiken" : IngredientData.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId).PurchaseName;
        //veggieNameText.text = currentVeggie == 0 ? "Potato" : IngredientData.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId).PurchaseName;
        //fruitNameText.text = currentFruit == 0 ? "Eggplant" : IngredientData.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId).PurchaseName;
    }
    public void SaveAndExit()
    {
        StartCoroutine(sql.RequestRoutine($"player/UpdateSkins?UserId={Settings.LoggedInPlayer.UserId}&SelectedMeat={Settings.LoggedInPlayer.SelectedMeat}&SelectedVeggie={Settings.LoggedInPlayer.SelectedVeggie}&SelectedFruit={Settings.LoggedInPlayer.SelectedFruit}&SelectedD10={Settings.LoggedInPlayer.SelectedDie}&SelectedD102={Settings.LoggedInPlayer.SelectedDie2}"));
        SceneManager.LoadScene("MainMenu");
    }

    public void Purchase(string type)
    {
        PurchaseType = type;
        var PurchaseId = (type == "meat" ? allMeatIngredients[currentMeat].purchaseId : type == "veggie" ? allVeggieIngredients[currentVeggie].purchaseId : type == "fruit" ? allFruitIngredients[currentFruit].purchaseId : type == "die"? allD10s[currentDie].purchaseId: allD10s[currentDie2].purchaseId);
        StartCoroutine(sql.RequestRoutine($"purchase/Purchase?UserId={Settings.LoggedInPlayer.UserId}&PurchaseId={PurchaseId}", GetPurchaseCallback));
    }

    public void nextImageForward(string directionAndType)
    {
        var direction = directionAndType.Split(',')[0];
        var type = directionAndType.Split(',')[1];
        if (type == "meat")
        {
            if (direction == "forward")
                if (currentMeat < allMeatIngredients.Count - 1)
                {
                    currentMeat++;
                }
                else
                {
                    currentMeat = 0;
                }
            else
            {
                if (currentMeat > 0)
                {
                    currentMeat--;
                }
                else
                {
                    currentMeat = allMeatIngredients.Count - 1;
                }
            }
            checkMeatUnlock();
        }
        else if (type == "veggie")
        {
            if (direction == "forward")
                if (currentVeggie < allVeggieIngredients.Count - 1)
                {
                    currentVeggie++;
                }
                else
                {
                    currentVeggie = 0;
                }
            else
            {
                if (currentVeggie > 0)
                {
                    currentVeggie--;
                }
                else
                {
                    currentVeggie = allVeggieIngredients.Count - 1;
                }
            }
            checkVeggieUnlock();
        }
        else if(type == "fruit")
        {
            if (direction == "forward")
                if (currentFruit < allFruitIngredients.Count - 1)
                {
                    currentFruit++;
                }
                else
                {
                    currentFruit = 0;
                }
            else
            {
                if (currentFruit > 0)
                {
                    currentFruit--;
                }
                else
                {
                    currentFruit = allFruitIngredients.Count - 1;
                }
            }
            checkFruitUnlock();
        } else if(type == "die")
        {
            if (direction == "forward")
                if (currentDie < allD10s.Count - 1)
                {
                    currentDie++;
                }
                else
                {
                    currentDie = 0;
                }
            else
            {
                if (currentDie > 0)
                {
                    currentDie--;
                }
                else
                {
                    currentDie = allD10s.Count - 1;
                }
            }
            checkDieUnlock();
        }else if(type == "die2")
        {
            if (direction == "forward")
                if (currentDie2 < allD10s.Count - 1)
                {
                    currentDie2++;
                }
                else
                {
                    currentDie2 = 0;
                }
            else
            {
                if (currentDie2 > 0)
                {
                    currentDie2--;
                }
                else
                {
                    currentDie2 = allD10s.Count - 1;
                }
            }
            checkDie2Unlock();
        }
    }

    private void checkMeatUnlock()
    {
        var selectedMeat = allMeatIngredients[currentMeat];
        currentMeatImage.sprite = selectedMeat.image;
        meatPurchaseButton.SetActive(false);
        meatRequiredText.text = "";
        meatButtonText.text = "";
        if (allMeatIngredients[currentMeat].purchaseId == 0 || PlayerOwns.Contains(allMeatIngredients[currentMeat].purchaseId))
        {
            if (allMeatIngredients[currentMeat].purchaseId == 0)
            {
                meatRequiredText.text = "You got this for free!";
                meatNameText.text = "Chicken";
            }
            else
            {
                var SelectedMeatData = IngredientData.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId);
                meatNameText.text = SelectedMeatData.PurchaseName;
                if (SelectedMeatData.RequiredPurchase != 0)
                {
                    var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedMeatData.RequiredPurchase);
                    meatRequiredText.text = $"You unlocked {requiredIng.PurchaseName} \n";
                }
                meatRequiredText.text += $"You are above level {SelectedMeatData.LevelMinimum} \n";
                meatRequiredText.text += $"You paid {SelectedMeatData.PurchaseCost} Stars";
            }
            lockedMeat.SetActive(false);
            Settings.LoggedInPlayer.SelectedMeat = currentMeat;
        }
        else
        {
            var SelectedMeatData = IngredientData.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId);
            meatNameText.text = SelectedMeatData.PurchaseName;
            lockedMeat.SetActive(true);
            if (SelectedMeatData.RequiredPurchase != 0 && !PlayerOwns.Contains(SelectedMeatData.RequiredPurchase))
            {
                var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedMeatData.RequiredPurchase);
                meatRequiredText.text = $"{requiredIng.PurchaseName} Unlock Required \n";
            }
            if (Settings.LoggedInPlayer.Level < SelectedMeatData.LevelMinimum)
            {
                meatRequiredText.text += $"Level {SelectedMeatData.LevelMinimum} Required \n";
            }

            if (Settings.LoggedInPlayer.Stars < SelectedMeatData.PurchaseCost)
            {
                meatRequiredText.text += $"{SelectedMeatData.PurchaseCost} Stars Required";
            }

            if (meatRequiredText.text == "")
            {
                meatButtonText.text = "Spend " + SelectedMeatData.PurchaseCost + " Stars to Unlock";
                meatPurchaseButton.SetActive(true);
            }
        }
    }
    private void checkVeggieUnlock()
    {
        var selectedVeggie = allVeggieIngredients[currentVeggie];
        currentVeggieImage.sprite = selectedVeggie.image;
        veggiePurchaseButton.SetActive(false);
        veggieRequiredText.text = "";
        veggieButtonText.text = "";
        if (allVeggieIngredients[currentVeggie].purchaseId == 0 || PlayerOwns.Contains(allVeggieIngredients[currentVeggie].purchaseId))
        {
            if (allVeggieIngredients[currentVeggie].purchaseId == 0)
            {
                veggieRequiredText.text = "You got this for free!";
                veggieNameText.text = "Potato1";
            }
            else
            {
                var SelectedVeggieData = IngredientData.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId);
                veggieNameText.text = SelectedVeggieData.PurchaseName;
                if (SelectedVeggieData.RequiredPurchase != 0)
                {
                    var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedVeggieData.RequiredPurchase);
                    veggieRequiredText.text = $"You unlocked {requiredIng.PurchaseName} \n";
                }
                veggieRequiredText.text += $"You are above level {SelectedVeggieData.LevelMinimum} \n";
                veggieRequiredText.text += $"You paid {SelectedVeggieData.PurchaseCost} Stars";
            }
            lockedVeggie.SetActive(false);
            Settings.LoggedInPlayer.SelectedVeggie = currentVeggie;
        }
        else
        {
            var SelectedVeggieData = IngredientData.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId);
            veggieNameText.text = SelectedVeggieData.PurchaseName;
            lockedVeggie.SetActive(true);
            if (SelectedVeggieData.RequiredPurchase != 0 && !PlayerOwns.Contains(SelectedVeggieData.RequiredPurchase))
            {
                var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedVeggieData.RequiredPurchase);
                veggieRequiredText.text = $"{requiredIng.PurchaseName} Unlock Required \n";
            }
            if (Settings.LoggedInPlayer.Level < SelectedVeggieData.LevelMinimum)
            {
                veggieRequiredText.text += $"Level {SelectedVeggieData.LevelMinimum} Required \n";
            }

            if (Settings.LoggedInPlayer.Stars < SelectedVeggieData.PurchaseCost)
            {
                veggieRequiredText.text += $"{SelectedVeggieData.PurchaseCost} Stars Required";
            }

            if (veggieRequiredText.text == "")
            {
                veggieButtonText.text = "Spend " + SelectedVeggieData.PurchaseCost + " Stars to Unlock";
                veggiePurchaseButton.SetActive(true);
            }
        }
    }

    private void checkFruitUnlock()
    {
        var selectedFruit = allFruitIngredients[currentFruit];
        currentFruitImage.sprite = selectedFruit.image;
        fruitPurchaseButton.SetActive(false);
        fruitRequiredText.text = "";
        fruitButtonText.text = "";
        if (allFruitIngredients[currentFruit].purchaseId == 0 || PlayerOwns.Contains(allFruitIngredients[currentFruit].purchaseId))
        {
            if (allFruitIngredients[currentFruit].purchaseId == 0)
            {
                fruitNameText.text = "Eggplant1";
                fruitRequiredText.text = "You got this for free!";
            }
            else
            {
                var SelectedFruitData = IngredientData.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId);
                fruitNameText.text = SelectedFruitData.PurchaseName;
                if (SelectedFruitData.RequiredPurchase != 0)
                {
                    var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedFruitData.RequiredPurchase);
                    fruitRequiredText.text = $"You unlocked {requiredIng.PurchaseName} \n";
                }
                fruitRequiredText.text += $"You are above level {SelectedFruitData.LevelMinimum} \n";
                fruitRequiredText.text += $"You paid {SelectedFruitData.PurchaseCost} Stars";
            }
            lockedFruit.SetActive(false);
            Settings.LoggedInPlayer.SelectedFruit = currentFruit;
        }
        else
        {
            var SelectedFruitData = IngredientData.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId);
            fruitNameText.text = SelectedFruitData.PurchaseName;
            lockedFruit.SetActive(true);
            if (SelectedFruitData.RequiredPurchase != 0 && !PlayerOwns.Contains(SelectedFruitData.RequiredPurchase))
            {
                var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedFruitData.RequiredPurchase);
                fruitRequiredText.text = $"{requiredIng.PurchaseName} Unlock Required \n";
            }
            if (Settings.LoggedInPlayer.Level < SelectedFruitData.LevelMinimum)
            {
                fruitRequiredText.text += $"Level {SelectedFruitData.LevelMinimum} Required \n";
            }

            if (Settings.LoggedInPlayer.Stars < SelectedFruitData.PurchaseCost)
            {
                fruitRequiredText.text += $"{SelectedFruitData.PurchaseCost} Stars Required";
            }

            if (fruitRequiredText.text == "")
            {
                fruitButtonText.text = "Spend " + SelectedFruitData.PurchaseCost + " Stars to Unlock";
                fruitPurchaseButton.SetActive(true);
            }
        }
    }

    private void checkDieUnlock()
    {
        currentDieImage.sprite = currentDie == 0 ? (Settings.LoggedInPlayer.PlayAsPurple ? purpleDie : yellowDie) : allD10s[currentDie].image;
        diePurchaseButton.SetActive(false);
        dieRequiredText.text = "";
        dieButtonText.text = "";
        if (allD10s[currentDie].purchaseId == 0 || PlayerOwns.Contains(allD10s[currentDie].purchaseId))
        {
            if (allD10s[currentDie].purchaseId == 0)
            {
                dieNameText.text = Settings.LoggedInPlayer.PlayAsPurple ? "Purple Die" : "Yellow Die";
                dieRequiredText.text = "You got this for free!";
            }
            else
            {
                var SelectedDieData = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie].purchaseId);
                dieNameText.text = SelectedDieData.PurchaseName;
                if (SelectedDieData.RequiredPurchase != 0)
                {
                    var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedDieData.RequiredPurchase);
                    dieRequiredText.text = $"You unlocked {requiredIng.PurchaseName} \n";
                }
                dieRequiredText.text += $"You are above level {SelectedDieData.LevelMinimum} \n";
                dieRequiredText.text += $"You paid {SelectedDieData.PurchaseCost} Stars";
            }
            lockedDie.SetActive(false);
            Settings.LoggedInPlayer.SelectedDie = currentDie;
        }
        else
        {
            var SelectedDieData = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie].purchaseId);
            dieNameText.text = SelectedDieData.PurchaseName;
            lockedDie.SetActive(true);
            if (SelectedDieData.RequiredPurchase != 0 && !PlayerOwns.Contains(SelectedDieData.RequiredPurchase))
            {
                var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedDieData.RequiredPurchase);
                dieRequiredText.text = $"{requiredIng.PurchaseName} Unlock Required \n";
            }
            if (Settings.LoggedInPlayer.Level < SelectedDieData.LevelMinimum)
            {
                dieRequiredText.text += $"Level {SelectedDieData.LevelMinimum} Required \n";
            }

            if (Settings.LoggedInPlayer.Stars < SelectedDieData.PurchaseCost)
            {
                dieRequiredText.text += $"{SelectedDieData.PurchaseCost} Stars Required";
            }

            if (dieRequiredText.text == "")
            {
                dieButtonText.text = "Spend " + SelectedDieData.PurchaseCost + " Stars to Unlock";
                diePurchaseButton.SetActive(true);
            }
        }
    }
    private void checkDie2Unlock()
    {
        currentDie2Image.sprite = currentDie2 == 0 ? (Settings.LoggedInPlayer.PlayAsPurple ? purpleDie : yellowDie) : allD10s[currentDie2].image;
        die2PurchaseButton.SetActive(false);
        die2RequiredText.text = "";
        die2ButtonText.text = "";
        if (allD10s[currentDie2].purchaseId == 0 || PlayerOwns.Contains(allD10s[currentDie2].purchaseId))
        {
            if (allD10s[currentDie2].purchaseId == 0)
            {
                die2NameText.text = Settings.LoggedInPlayer.PlayAsPurple ? "Purple die" : "Yellow die";
                die2RequiredText.text = "You got this for free!";
            }
            else
            {
                var SelectedDie2Data = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie2].purchaseId);
                die2NameText.text = SelectedDie2Data.PurchaseName;
                if (SelectedDie2Data.RequiredPurchase != 0)
                {
                    var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedDie2Data.RequiredPurchase);
                    die2RequiredText.text = $"You unlocked {requiredIng.PurchaseName} \n";
                }
                die2RequiredText.text += $"You are above level {SelectedDie2Data.LevelMinimum} \n";
                die2RequiredText.text += $"You paid {SelectedDie2Data.PurchaseCost} Stars";
            }
            lockedDie2.SetActive(false);
            Settings.LoggedInPlayer.SelectedDie2 = currentDie2;
        }
        else
        {
            var SelectedDie2Data = IngredientData.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie2].purchaseId);
            die2NameText.text = SelectedDie2Data.PurchaseName;
            lockedDie2.SetActive(true);
            if (SelectedDie2Data.RequiredPurchase != 0 && !PlayerOwns.Contains(SelectedDie2Data.RequiredPurchase))
            {
                var requiredIng = IngredientData.FirstOrDefault(x => x.PurchaseId == SelectedDie2Data.RequiredPurchase);
                die2RequiredText.text = $"{requiredIng.PurchaseName} Unlock Required \n";
            }
            if (Settings.LoggedInPlayer.Level < SelectedDie2Data.LevelMinimum)
            {
                die2RequiredText.text += $"Level {SelectedDie2Data.LevelMinimum} Required \n";
            }

            if (Settings.LoggedInPlayer.Stars < SelectedDie2Data.PurchaseCost)
            {
                die2RequiredText.text += $"{SelectedDie2Data.PurchaseCost} Stars Required";
            }

            if (die2RequiredText.text == "")
            {
                die2ButtonText.text = "Spend " + SelectedDie2Data.PurchaseCost + " Stars to Unlock";
                die2PurchaseButton.SetActive(true);
            }
        }
    }
}
