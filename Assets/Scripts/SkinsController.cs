using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkinsController : MonoBehaviour
{

    public Text meatButtonText;
    public Text veggieButtonText;
    public Text fruitButtonText;
    public Text dieButtonText;
    public GameObject loading;
    public GameObject meatPurchaseButton;
    public GameObject veggiePurchaseButton;
    public GameObject fruitPurchaseButton;
    public GameObject diePurchaseButton;
    public Text playerWins;
    public Image currentMeatImage;
    public Image currentVeggieImage;
    public Image currentFruitImage;
    public Image currentDieImage;
    public Sprite purpleDie;
    public Sprite yellowDie;
    public GameObject lockedMeat;
    public GameObject lockedVeggie;
    public GameObject lockedFruit;
    public GameObject lockedDie;
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
    private class ItemCost
    {
        public int PurchaseId;
        public int PurchaseCost;

    }

    private List<int> PlayerOwns;
    private List<ItemCost> ingredientCosts;
    private SqlController sql;
    private int currentMeat = 0;
    private int currentVeggie = 0;
    private int currentFruit = 0;
    private int currentDie = 0;
    private string PurchaseType = "";
  
    private void Start()
    {
        sql = new SqlController();
        PlayerOwns = new List<int>();
        ingredientCosts = new List<ItemCost>();
        //loading.SetActive(true);
        currentMeat = Settings.LoggedInPlayer.SelectedMeat;
        currentVeggie = Settings.LoggedInPlayer.SelectedVeggie;
        currentFruit = Settings.LoggedInPlayer.SelectedFruit;
        currentDie = Settings.LoggedInPlayer.SelectedDie;
        currentMeatImage.sprite = allMeatIngredients[currentMeat].image;
        currentVeggieImage.sprite = allVeggieIngredients[currentVeggie].image;
        currentFruitImage.sprite = allFruitIngredients[currentFruit].image;
        currentDieImage.sprite = currentDie == 0 ? (Settings.LoggedInPlayer.PlayAsPurple ? purpleDie : yellowDie) : allD10s[currentDie].image;
        SetStarsTotalText();
        StartCoroutine(sql.RequestRoutine($"purchase/GetAllPurchasables", GetAllPurchaseCallback, true));
        StartCoroutine(sql.RequestRoutine($"purchase/GetPlayerPurchasables?UserId={Settings.LoggedInPlayer.UserId}", GetPlayerPurchaseCallback));
    }
    public void SetStarsTotalText()
    {
        if (Settings.LoggedInPlayer.IsGuest)
        {
            playerWins.text = @"Guests do not earn calories.";
        }
        else
        {
            if (Settings.LoggedInPlayer.Stars == 0)
            {
                playerWins.text = @"Wow, how embarassing, you're out of Calories!
Earn 50 Calories per ingredient you cook!";
            }
            else
            {
                playerWins.text = "You have " + Settings.LoggedInPlayer.Stars.ToString() + @" Calories!
Earn 50 Calories per ingredient you cook!";
            }
        }
    }
    private void GetPlayerPurchaseCallback(string data)
    {
        PlayerOwns = sql.jsonConvert<List<int>>(data);
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
                cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId).PurchaseCost;
                PlayerOwns.Add(allMeatIngredients[currentMeat].purchaseId);
            }
            else if (PurchaseType == "veggie")
            {
                lockedVeggie.SetActive(false);
                veggiePurchaseButton.SetActive(false);
                veggieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedVeggie = currentVeggie;
                cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId).PurchaseCost;
                PlayerOwns.Add(allVeggieIngredients[currentVeggie].purchaseId);
            }
            else if (PurchaseType == "fruit")
            {
                lockedFruit.SetActive(false);
                fruitPurchaseButton.SetActive(false);
                fruitButtonText.text = "";
                Settings.LoggedInPlayer.SelectedFruit = currentFruit;
                cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId).PurchaseCost;
                PlayerOwns.Add(allFruitIngredients[currentFruit].purchaseId);
            }
            else if (PurchaseType == "die")
            {
                lockedDie.SetActive(false);
                diePurchaseButton.SetActive(false);
                dieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedDie = currentDie;
                cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie].purchaseId).PurchaseCost;
                PlayerOwns.Add(allD10s[currentDie].purchaseId);
            }
            Settings.LoggedInPlayer.Stars -= cost;
            SetStarsTotalText();
        }
    } 
    private void GetAllPurchaseCallback(string data)
    {
        ingredientCosts = sql.jsonConvert<List<ItemCost>>(data);
    }
    public void SaveAndExit()
    {
        StartCoroutine(sql.RequestRoutine($"player/UpdateSkins?UserId={Settings.LoggedInPlayer.UserId}&SelectedMeat={Settings.LoggedInPlayer.SelectedMeat}&SelectedVeggie={Settings.LoggedInPlayer.SelectedVeggie}&SelectedFruit={Settings.LoggedInPlayer.SelectedFruit}&SelectedD10={Settings.LoggedInPlayer.SelectedDie}"));
        SceneManager.LoadScene("MainMenu");
    }

    public void Purchase(string type)
    {
        PurchaseType = type;
        var PurchaseId = (type == "meat" ? allMeatIngredients[currentMeat].purchaseId : type == "veggie" ? allVeggieIngredients[currentVeggie].purchaseId : type == "fruit" ? allFruitIngredients[currentFruit].purchaseId : allD10s[currentDie].purchaseId);
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
            currentMeatImage.sprite = allMeatIngredients[currentMeat].image;
            if (allMeatIngredients[currentMeat].purchaseId == 0 || PlayerOwns.Contains(allMeatIngredients[currentMeat].purchaseId))
            {
                meatPurchaseButton.SetActive(false);
                lockedMeat.SetActive(false);
                meatButtonText.text = "";
                Settings.LoggedInPlayer.SelectedMeat = currentMeat;
            }
            else
            {
                //meatLockedText.text = "This Ingredient is locked until you reach " + currentMeat * 5 + " wins vs the computer!";
                var cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allMeatIngredients[currentMeat].purchaseId).PurchaseCost;
                meatButtonText.text = "Burn " + cost + " Calories to unlock";
                meatPurchaseButton.SetActive(true);
                lockedMeat.SetActive(true);
            }
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
            
            currentVeggieImage.sprite = allVeggieIngredients[currentVeggie].image;
            if (allVeggieIngredients[currentVeggie].purchaseId == 0 || PlayerOwns.Contains(allVeggieIngredients[currentVeggie].purchaseId))
            {
                lockedVeggie.SetActive(false);
                veggiePurchaseButton.SetActive(false);
                veggieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedVeggie = currentVeggie;
            }
            else
            {
                var cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allVeggieIngredients[currentVeggie].purchaseId).PurchaseCost;
                veggieButtonText.text = "Burn " + cost + " Calories to unlock";
                veggiePurchaseButton.SetActive(true);
                lockedVeggie.SetActive(true);
            }
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
            
            currentFruitImage.sprite = allFruitIngredients[currentFruit].image;
            if (allFruitIngredients[currentFruit].purchaseId == 0 || PlayerOwns.Contains(allFruitIngredients[currentFruit].purchaseId))
            {
                lockedFruit.SetActive(false);
                fruitPurchaseButton.SetActive(false);
                fruitButtonText.text = "";
                Settings.LoggedInPlayer.SelectedFruit = currentFruit;
            }
            else
            {
                var cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allFruitIngredients[currentFruit].purchaseId).PurchaseCost;
                fruitButtonText.text = "Burn " + cost + " Calories to unlock";
                fruitPurchaseButton.SetActive(true);
                lockedFruit.SetActive(true);
            }
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
            
            currentDieImage.sprite = allD10s[currentDie].image;
            if (allD10s[currentDie].purchaseId == 0 || PlayerOwns.Contains(allD10s[currentDie].purchaseId))
            {
                lockedDie.SetActive(false);
                diePurchaseButton.SetActive(false);
                dieButtonText.text = "";
                Settings.LoggedInPlayer.SelectedDie = currentDie;
            }
            else
            {
                var cost = ingredientCosts.FirstOrDefault(x => x.PurchaseId == allD10s[currentDie].purchaseId).PurchaseCost;
                dieButtonText.text = "Burn " + cost + " Calories to unlock";
                diePurchaseButton.SetActive(true);
                lockedDie.SetActive(true);
            }
        }
    }
}
