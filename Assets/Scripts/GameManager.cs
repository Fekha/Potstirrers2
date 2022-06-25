using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region GameManger Variables
    private int TurnNumber = 0;
    private int higherMove = 0;
    private int lowerMove = 0;
    private bool higherMoveSelected;
    private List<Ingredient> AllIngredients;
    private List<Ingredient> UseableIngredients;
    private List<Ingredient> TeamIngredients;
    private List<Ingredient> UseableTeamIngredients;
    private List<Ingredient> EnemyIngredients;
    private List<Ingredient> UseableEnemyIngredients;
    private Ingredient IngredientMovedWithLower;
    private Ingredient IngredientMovedWithHigher;
    private SqlController sql;
    private GamePlayer playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    private float talkingTimeStart;
    private bool hasBeenDumb = false;
    private bool GameOver = false;
    private int GameId = 0;
    private int JennDie1;
    private int JennDie2;
    private TurnPosition[] TurnStartPositions = new TurnPosition[6];
    #endregion

    #region Ingredient Variables
    internal static GameManager i;
    internal bool IsReading = false;
    internal int Steps;
    internal bool? ShouldTrash = null;
    internal bool isMoving = false;
    internal bool firstMoveTaken = false;
    internal int activePlayer = 0;

    #endregion

    #region Unity Editor Variables
    [System.Serializable]
    public class GamePlayer
    {
        public Player player;
        public Ingredient[] myIngredients;
        public bool hasTurn;
        public bool TeamYellow;
    }
    public List<GamePlayer> playerList = new List<GamePlayer>();
    public List<Tile> prepTiles = new List<Tile>();
    public int? firstIngredientMoved;

    [Header("GameObject")]
    public GameObject EventCanvas;
    public GameObject ShouldTrashPopup;
    public GameObject HelpCanvas;
    public GameObject TrashCan2;
    public GameObject TrashCan3;
    public GameObject exitPanel;
    public GameObject undoPanel;
    public GameObject FullBoard;
    public GameObject TalkShitPanel;

    [Header("Player1Roll")]
    public GameObject RolledPanel1;
    public Button HigherRollImage1;
    public Button HigherRollButton1;
    public Text HigherRollText1;
    public Button LowerRollImage1;   
    public Button LowerRollButton1;
    public Text LowerRollText1;
    public Text ProfileText1;
    public GameObject RollButton1;

    [Header("Player2Roll")]
    public GameObject RolledPanel2;
    public Button HigherRollImage2;
    public Button HigherRollButton2;
    public Text HigherRollText2;
    public Button LowerRollImage2;
    public Button LowerRollButton2;
    public Text LowerRollText2;
    public Text ProfileText2;
    public GameObject RollButton2;

    [Header("Undo")]
    public Button undoButton1;
    public Button undoButton2;

    [Header("Text")]
    public Text eventText;
    public Text helpText;
    public Text actionText;
    public Text turnText;
    public Text talkShitText;

    [Header("Sprite")]
    public Sprite yellowDie;
    public Sprite purpleDie;
    public Sprite yellowD8;
    public Sprite purpleD8;
    public List<Sprite> allD10s;
    
    [Header("Material")]
    public Material AdvancedBoard;
    public List<Material> allMeatMaterials;
    public List<Material> allVeggieMaterials;
    public List<Material> allFruitMaterials;
    #endregion

    #region GameManager Only
    private void Awake()
    {
        i = this;
        Application.targetFrameRate = 30;
        sql = new SqlController();
        activePlayer = Random.Range(0, 2);
        AllIngredients = playerList.SelectMany(y => y.myIngredients).OrderBy(x => x.IngredientId).ToList();
#if UNITY_EDITOR
        Settings.IsDebug = true;
        //Settings.LoggedInPlayer.Experimental = true;
#endif

        if (Settings.IsDebug)
        {
            Settings.LoggedInPlayer = global::Settings.CPUPlayers[0];
            Settings.SecondPlayer = global::Settings.CPUPlayers[1];
            activePlayer = 0;
        }

        //if (Settings.LoggedInPlayer.Wins == 0 && !Settings.IsDebug)
        //{
        //    activePlayer = 0;
        //    getHelp();
        //}

        if (Settings.LoggedInPlayer.PlayAsPurple)
        {
            playerList[0].player = Settings.SecondPlayer;
            playerList[1].player = Settings.LoggedInPlayer;
        }
        else
        {
            playerList[0].player = Settings.LoggedInPlayer;
            playerList[1].player = Settings.SecondPlayer;
        }

        playerList[0].TeamYellow = true;
        playerList[1].TeamYellow = false;

        if (!Settings.IsDebug && !Settings.LoggedInPlayer.IsGuest)
        {
            var url = $"analytic/GameStart?Player1={playerList[0].player.UserId}&Player2={playerList[1].player.UserId}&WineMenu={Settings.LoggedInPlayer.WineMenu}";
            StartCoroutine(sql.RequestRoutine(url, GetNewGameCallback));
        }
        else
        {
            StartCoroutine(TakeTurn());
        }
    }

    private void Start()
    {
        //if (Settings.LoggedInPlayer.Experimental)
        //{
        //    var boardQuad = FullBoard.GetComponent<MeshRenderer>();
        //    var boardMats = boardQuad.materials;
        //    boardMats[0] = AdvancedBoard;
        //    boardQuad.materials = boardMats;
        //}
        
        //Set Starting Tiles
        prepTiles[0].ingredients.Push(AllIngredients[0]); 
        prepTiles[1].ingredients.Push(AllIngredients[1]); 
        prepTiles[2].ingredients.Push(AllIngredients[2]); 
        prepTiles[3].ingredients.Push(AllIngredients[3]); 
        prepTiles[4].ingredients.Push(AllIngredients[4]); 
        prepTiles[5].ingredients.Push(AllIngredients[5]); 
        if (!Settings.LoggedInPlayer.IsGuest)
        {
            //Change first player to there skins
            
            var HumanIngredients = AllIngredients.Where(x => x.TeamYellow != Settings.LoggedInPlayer.PlayAsPurple).ToList();

            var meatQuads = HumanIngredients.FirstOrDefault(x => x.type == "Meat").NormalQuad.GetComponent<MeshRenderer>();
            var meatMats = meatQuads.materials;
            meatMats[0] = allMeatMaterials[Settings.LoggedInPlayer.SelectedMeat];
            meatQuads.materials = meatMats;

            var veggieQuads = HumanIngredients.FirstOrDefault(x => x.type == "Veggie").NormalQuad.GetComponent<MeshRenderer>();
            var veggieMats = veggieQuads.materials;
            veggieMats[0] = allVeggieMaterials[Settings.LoggedInPlayer.SelectedVeggie];
            veggieQuads.materials = veggieMats;

            var fruitQuads = HumanIngredients.FirstOrDefault(x => x.type == "Fruit").NormalQuad.GetComponent<MeshRenderer>();
            var fruitMats = fruitQuads.materials;
            fruitMats[0] = allFruitMaterials[Settings.LoggedInPlayer.SelectedFruit];
            fruitQuads.materials = fruitMats; 
            
            meatQuads = HumanIngredients.FirstOrDefault(x => x.type == "Meat").BackNormalQuad.GetComponent<MeshRenderer>();
            meatMats = meatQuads.materials;
            meatMats[0] = allMeatMaterials[Settings.LoggedInPlayer.SelectedMeat];
            meatQuads.materials = meatMats;

            veggieQuads = HumanIngredients.FirstOrDefault(x => x.type == "Veggie").BackNormalQuad.GetComponent<MeshRenderer>();
            veggieMats = veggieQuads.materials;
            veggieMats[0] = allVeggieMaterials[Settings.LoggedInPlayer.SelectedVeggie];
            veggieQuads.materials = veggieMats;

            fruitQuads = HumanIngredients.FirstOrDefault(x => x.type == "Fruit").BackNormalQuad.GetComponent<MeshRenderer>();
            fruitMats = fruitQuads.materials;
            fruitMats[0] = allFruitMaterials[Settings.LoggedInPlayer.SelectedFruit];
            fruitQuads.materials = fruitMats;

            if (Settings.SecondPlayer.IsGuest)
            {
                //Change second player to random skins
                var CPUIngredients = AllIngredients.Where(x => x.TeamYellow == Settings.LoggedInPlayer.PlayAsPurple).ToList();

                allMeatMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedMeat);
                var randMeat = Random.Range(0, allMeatMaterials.Count());
                allVeggieMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedVeggie);
                var randVeggie = Random.Range(0, allVeggieMaterials.Count());
                allFruitMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedFruit);
                var randFruit = Random.Range(0, allFruitMaterials.Count());

                var CPUmeatQuads = CPUIngredients.FirstOrDefault(x=>x.type == "Meat").NormalQuad.GetComponent<MeshRenderer>();
                var CPUmeatMats = CPUmeatQuads.materials;
                CPUmeatMats[0] = allMeatMaterials[Settings.SecondPlayer.Username == "Jenn" ? allMeatMaterials.Count()-1 : randMeat];
                CPUmeatQuads.materials = CPUmeatMats;

                var CPUveggieQuads = CPUIngredients.FirstOrDefault(x => x.type == "Veggie").NormalQuad.GetComponent<MeshRenderer>();
                var CPUveggieMats = CPUveggieQuads.materials;
                CPUveggieMats[0] = allVeggieMaterials[Settings.SecondPlayer.Username == "Jenn" ? allVeggieMaterials.Count() - 1 : randVeggie];
                CPUveggieQuads.materials = CPUveggieMats;

                var CPUfruitQuads = CPUIngredients.FirstOrDefault(x => x.type == "Fruit").NormalQuad.GetComponent<MeshRenderer>();
                var CPUfruitMats = CPUfruitQuads.materials;
                CPUfruitMats[0] = allFruitMaterials[Settings.SecondPlayer.Username == "Jenn" ? allFruitMaterials.Count() - 1 : randFruit];
                CPUfruitQuads.materials = CPUfruitMats;
                
                CPUmeatQuads = CPUIngredients.FirstOrDefault(x=>x.type == "Meat").BackNormalQuad.GetComponent<MeshRenderer>();
                CPUmeatMats = CPUmeatQuads.materials;
                CPUmeatMats[0] = allMeatMaterials[Settings.SecondPlayer.Username == "Jenn" ? allMeatMaterials.Count()-1 : randMeat];
                CPUmeatQuads.materials = CPUmeatMats;

                CPUveggieQuads = CPUIngredients.FirstOrDefault(x => x.type == "Veggie").BackNormalQuad.GetComponent<MeshRenderer>();
                CPUveggieMats = CPUveggieQuads.materials;
                CPUveggieMats[0] = allVeggieMaterials[Settings.SecondPlayer.Username == "Jenn" ? allVeggieMaterials.Count() - 1 : randVeggie];
                CPUveggieQuads.materials = CPUveggieMats;

                CPUfruitQuads = CPUIngredients.FirstOrDefault(x => x.type == "Fruit").BackNormalQuad.GetComponent<MeshRenderer>();
                CPUfruitMats = CPUfruitQuads.materials;
                CPUfruitMats[0] = allFruitMaterials[Settings.SecondPlayer.Username == "Jenn" ? allFruitMaterials.Count() - 1 : randFruit];
                CPUfruitQuads.materials = CPUfruitMats;
            }
            else
            {
                //Change second player to there skins
                AllIngredients = playerList.SelectMany(y => y.myIngredients).ToList();
                var Human2Ingredients = AllIngredients.Where(x => x.TeamYellow == Settings.LoggedInPlayer.PlayAsPurple).ToList();

                var meat2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Meat").NormalQuad.GetComponent<MeshRenderer>();
                var meat2Mats = meat2Quads.materials;
                meat2Mats[0] = allMeatMaterials[Settings.SecondPlayer.SelectedMeat];
                meat2Quads.materials = meat2Mats;

                var veggie2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Veggie").NormalQuad.GetComponent<MeshRenderer>();
                var veggie2Mats = veggie2Quads.materials;
                veggie2Mats[0] = allVeggieMaterials[Settings.SecondPlayer.SelectedVeggie];
                veggie2Quads.materials = veggie2Mats;

                var fruit2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Fruit").NormalQuad.GetComponent<MeshRenderer>();
                var fruit2Mats = fruit2Quads.materials;
                fruit2Mats[0] = allFruitMaterials[Settings.SecondPlayer.SelectedFruit];
                fruit2Quads.materials = fruit2Mats;  
                
                meat2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Meat").BackNormalQuad.GetComponent<MeshRenderer>();
                meat2Mats = meat2Quads.materials;
                meat2Mats[0] = allMeatMaterials[Settings.SecondPlayer.SelectedMeat];
                meat2Quads.materials = meat2Mats;

                veggie2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Veggie").BackNormalQuad.GetComponent<MeshRenderer>();
                veggie2Mats = veggie2Quads.materials;
                veggie2Mats[0] = allVeggieMaterials[Settings.SecondPlayer.SelectedVeggie];
                veggie2Quads.materials = veggie2Mats;

                fruit2Quads = Human2Ingredients.FirstOrDefault(x => x.type == "Fruit").BackNormalQuad.GetComponent<MeshRenderer>();
                fruit2Mats = fruit2Quads.materials;
                fruit2Mats[0] = allFruitMaterials[Settings.SecondPlayer.SelectedFruit];
                fruit2Quads.materials = fruit2Mats;
            }
        }

        if (Settings.HardMode)
        {
            JennDie1 = Random.Range(allD10s.Count()-3, allD10s.Count());
            JennDie2 = Random.Range(allD10s.Count()-3, allD10s.Count());
            while (Settings.LoggedInPlayer.SelectedDie == JennDie1 || Settings.LoggedInPlayer.SelectedDie2 == JennDie2)
            {
                JennDie1 = Random.Range(allD10s.Count() - 3, allD10s.Count());
                JennDie2 = Random.Range(allD10s.Count() - 3, allD10s.Count());
            } 
        }
        if (playerList[0].player.Username == Settings.LoggedInPlayer.Username)
        {
            ProfileText1.text = Settings.LoggedInPlayer.Username + ":";
            ProfileText2.text = Settings.SecondPlayer.Username + ":";
            ProfileText1.color = new Color32(255, 202, 24, 255);
            ProfileText2.color = new Color32(171, 20, 157, 255);
        }
        else
        {
            ProfileText1.text = Settings.SecondPlayer.Username + ":";
            ProfileText2.text =  Settings.LoggedInPlayer.Username + ":";
            ProfileText1.color = new Color32(171, 20, 157, 255);
            ProfileText2.color =  new Color32(255, 202, 24, 255);
        }
        UpdateDiceSkin(HigherRollImage1, LowerRollImage1, Settings.LoggedInPlayer.Username, playerList[0].player.Username == Settings.LoggedInPlayer.Username);
        UpdateDiceSkin(HigherRollImage2, LowerRollImage2, Settings.SecondPlayer.Username, playerList[0].player.Username == Settings.SecondPlayer.Username);
    }
   
    private void GetNewGameCallback(string data)
    {
        GameId = sql.jsonConvert<int>(data);
        StartCoroutine(TakeTurn());
    }

    private void Update()
    {
        if (Settings.IsDebug && IsReading == true && IsCPUTurn())
        {
            var timeSince = Time.time - readingTimeStart;
            if (timeSince > 2.0) {
                HelpCanvas.SetActive(false);
                IsReading = false;
            }
        }
        if (TalkShitPanel.activeInHierarchy)
        {
            var timeSince = Time.time - talkingTimeStart;
            if (timeSince > 3.0)
            {
                talkShitText.text = "";
                TalkShitPanel.SetActive(false);
            }
        }
    }
    private void ResetMovementVariables()
    {
        undoButton1.gameObject.SetActive(false);
        undoButton2.gameObject.SetActive(false);
        firstIngredientMoved = null;
        firstMoveTaken = false;
        IngredientMovedWithLower = null;
        IngredientMovedWithHigher = null;
    }
    private void SwitchPlayer()
    {
        activePlayer = (activePlayer + 1) % playerList.Count();
        HigherRollImage1.interactable = false;
        LowerRollImage1.interactable = false;
        HigherRollButton1.interactable = false;
        LowerRollButton1.interactable = false;
        HigherRollImage2.interactable = false;
        LowerRollImage2.interactable = false;
        HigherRollButton2.interactable = false;
        LowerRollButton2.interactable = false;
        hasBeenDumb = false;
        ResetMovementVariables();
        StartCoroutine(TakeTurn());
    }
    private void StartReading()
    {
        IsReading = true;
        readingTimeStart = Time.time;
        HelpCanvas.SetActive(true);
    }
    private IEnumerator TakeTurn()
    {
        for (int i = 0; i < TurnStartPositions.Count(); i++) {
            TurnStartPositions[i] = new TurnPosition(AllIngredients[i].routePosition, AllIngredients[i].isCooked);
        }

        TurnNumber++;
        turnText.text = GetActivePlayer().player.Username.Trim() + $"'s Turn";
        if (GetActivePlayer().TeamYellow)
        {
            turnText.color = new Color32(255, 202, 24, 255);
            actionText.color = new Color32(255, 202, 24, 255);
        }
        else
        {
            turnText.color = new Color32(171, 20, 157, 255);
            actionText.color = new Color32(171, 20, 157, 255);
        }
        actionText.text = "Roll the dice!";
        yield return StartCoroutine(DeactivateAllSelectors());
        if (activePlayer == 0)
        {
            HigherRollText1.text = "";
            LowerRollText1.text = "";
            RollButton1.SetActive(true);
        }
        else
        {
            HigherRollText2.text = "";
            LowerRollText2.text = "";
            RollButton2.SetActive(true);
        }
        if (IsCPUTurn()) //Take turn for CPU
        {
            yield return StartCoroutine(CPUTurn());
        }
    }
    private IEnumerator RollSelected(bool isHigher, bool HUMAN)
    {
        if (isMoving || (IsCPUTurn() && HUMAN))
        {
            yield return new WaitForSeconds(0.5f);
        }
        else
        {

            higherMoveSelected = isHigher;
            Steps = higherMoveSelected ? higherMove : lowerMove;
            UpdateMoveText(Steps);
            SetDice();
            yield return StartCoroutine(SetSelectableIngredients());
        }
    }
    private void SetDice()
    {
        if (activePlayer == 0)
        {
            if (!firstMoveTaken)
            {
                if (higherMoveSelected)
                {
                    HigherRollImage1.interactable = false;
                    LowerRollImage1.interactable = true;
                    HigherRollButton1.interactable = false;
                    LowerRollButton1.interactable = true;
                }
                else
                {
                    LowerRollImage1.interactable = false;
                    HigherRollImage1.interactable = true;
                    LowerRollButton1.interactable = false;
                    HigherRollButton1.interactable = true;
                }
            }
            else
            {
                HigherRollImage1.interactable = false;
                LowerRollImage1.interactable = false;
                HigherRollButton1.interactable = false;
                LowerRollButton1.interactable = false;
            }
        }
        else
        {
            if (!firstMoveTaken)
            {
                if (higherMoveSelected)
                {
                    HigherRollImage2.interactable = false;
                    LowerRollImage2.interactable = true;
                    HigherRollButton2.interactable = false;
                    LowerRollButton2.interactable = true;
                }
                else
                {
                    LowerRollImage2.interactable = false;
                    HigherRollImage2.interactable = true;
                    LowerRollButton2.interactable = false;
                    HigherRollButton2.interactable = true;
                }
            }
            else
            {
                HigherRollImage2.interactable = false;
                LowerRollImage2.interactable = false;
                HigherRollButton2.interactable = false;
                LowerRollButton2.interactable = false;
            }
        }
    }
    private IEnumerator SetSelectableIngredients()
    {
        yield return StartCoroutine(DeactivateAllSelectors());

        List<Ingredient> moveableList;
        if (higherMoveSelected || lowerMove == higherMove)
            moveableList = GetActivePlayer().myIngredients.Where(x => x.IngredientId != firstIngredientMoved && (x.currentTile.ingredients.Peek() == x || x.routePosition == 0)).ToList();
        else
            moveableList = playerList.SelectMany(y => y.myIngredients.Where(x => x.IngredientId != firstIngredientMoved && (x.currentTile.ingredients.Peek() == x || x.routePosition == 0))).ToList();

        for (int i = 0; i < moveableList.Count(); i++)
        {
            moveableList[i].SetSelector(true);
        }

        if (IsCPUTurn())
            yield return new WaitForSeconds(.5f);

        if (moveableList.Count() == 0)
        {
            yield return StartCoroutine(DoneMoving());
        }
        
        yield return new WaitForSeconds(0.1f);
    }
    private void GameIsOver(bool debug = false)
    {
        GameOver = true;
        if (!Settings.IsDebug)
        {
            var url = $"analytic/GameEnd?GameId={GameId}&Player1Cooked={playerList[0].myIngredients.Count(x => x.isCooked)}&Player2Cooked={playerList[1].myIngredients.Count(x => x.isCooked)}&TotalTurns={TurnNumber}&HardMode={Settings.HardMode}";
            StartCoroutine(sql.RequestRoutine(url, null, true));
        }
        var BonusXP = Settings.HardMode ? 100 : 0;
        var BonusStars = Settings.HardMode ? 50 : 0;
        playerWhoWon = playerList.FirstOrDefault(x => x.myIngredients.All(y => y.isCooked));
        if (debug)
        {
            playerWhoWon = playerList.FirstOrDefault(x => !x.TeamYellow);
        }
        var playerwhoLost = playerList.FirstOrDefault(x => x.player.Username != playerWhoWon.player.Username);
        var lostCount = playerwhoLost.myIngredients.Count(x => x.isCooked);
        eventText.text = @"GAME OVER! " + playerWhoWon.player.Username + " won. \n \n";
        var wonXp = BonusXP + 300 + ((3 - lostCount) * 50);
        var lostXp = BonusXP + 150 + (lostCount * 50);
        if (playerList.Any(x => x.player.playerType == PlayerTypes.CPU) && playerList.Any(x => x.player.playerType == PlayerTypes.HUMAN))
        {
            if (playerWhoWon.player.playerType == PlayerTypes.HUMAN)
            {
                eventText.text += $" You earned: \n {150+BonusStars} Calories \n {wonXp} Xp";
            }
            else
            {
                eventText.text += $" You earned: \n { BonusStars + (lostCount * 50)} Calories \n " + lostXp + " Xp";
            }
        }
        else
        {
            eventText.text += "You each gained 50 Calories for each of your cooked ingredients! \n" + playerWhoWon.player.Username + " earned " + wonXp + " XP \n " + playerwhoLost.player.Username + " earned " + lostXp + " xp!";
        }

        if (Settings.LoggedInPlayer.WineMenu)
            eventText.text += "\n \n" + (playerWhoWon.TeamYellow ? " Purple" : " Yellow") + " Team finish your drinks!";

        if (playerWhoWon.player.Username == Settings.LoggedInPlayer.Username)
            Settings.LoggedInPlayer.Wins++;

        Settings.HardMode = false;
        EventCanvas.SetActive(true);
    }
    #endregion

    #region Used by ingredient
    internal bool IsCPUTurn()
    {
        return GetActivePlayer().player.playerType == PlayerTypes.CPU;
    }
    internal GamePlayer GetActivePlayer()
    {
        return playerList[activePlayer];
    }
    internal void ActivateShitTalk()
    {
        if (!string.IsNullOrEmpty(talkShitText.text) && !TalkShitPanel.activeInHierarchy)
        {
            talkingTimeStart = Time.time;
            TalkShitPanel.SetActive(true);
        }
    }
    internal IEnumerator DoneMoving()
    {
        while (IsReading)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (playerList.Any(x => x.myIngredients.All(y => y.isCooked)))
        {
            GameIsOver();
        }
        else if (firstMoveTaken)
        {
            SwitchPlayer();
        }
        else
        {
            firstMoveTaken = true;
            if (!IsCPUTurn())
            {
                if (activePlayer == 0)
                {
                    undoButton1.gameObject.SetActive(true);
                }
                else
                {
                    undoButton2.gameObject.SetActive(true);
                }
                yield return RollSelected(!higherMoveSelected, !IsCPUTurn());
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
    internal IEnumerator MoveToNextEmptySpace(Ingredient ingredientToMove, bool shouldPush = true)
    {
        ingredientToMove.routePosition = 0;
        //if (prepTiles.Any(x => x.ingredients.Any(y=>y.IngredientId == ingredientToMove.IngredientId)))
        //{ //happens when scoring
        //    ingredientToMove.currentTile = prepTiles.FirstOrDefault(x => x.ingredients.FirstOrDefault(y => y.IngredientId == ingredientToMove.IngredientId));
        //}
        //else
        //{
            ingredientToMove.currentTile = prepTiles.FirstOrDefault(x => x.ingredients.Count() == 0);
        if(shouldPush)
            ingredientToMove.currentTile.ingredients.Push(ingredientToMove);
        //}

        yield return ingredientToMove.MoveToNextTile(ingredientToMove.currentTile.transform.position, shouldPush,45f);
    }
    internal void FirstScoreHelp()
    {
            pageNum = Library.helpTextList.Count() - 1;
            helpText.text = "An Ingredient was cooked! \n \n Remember: Cooked ingredients can NOT be cooked again. \n \n Cooked ingredients can still be used to send ingredients back to prep and are always skipped over while moving to give you a further move distance!";
            StartReading();
    }
    internal void setWineMenuText(bool teamYellow, int v)
    {
        helpText.text = (teamYellow ? "Yellow" : "Purple") + " team drinks for " + v + (v == 1 ? " second" : " seconds") + ". \n \n Math: 1 second for each ingredient in Prep, other team drinks if cooked.";
        StartReading();
    }
    internal void UpdateMoveText(int? moveAmount = null)
    {
        actionText.text = moveAmount != null ? "Move: " + moveAmount : "";
    }
    internal IEnumerator DeactivateAllSelectors()
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            for (int j = 0; j < playerList[i].myIngredients.Length; j++)
            {
                playerList[i].myIngredients[j].SetSelector(false);
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
    internal IEnumerator AskShouldTrash()
    {
        ShouldTrashPopup.SetActive(true);
        while (ShouldTrash == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
    #endregion

    #region Button Clicks
    public void GameOverClicked()
    {
        Settings.IsDebug = false;
        SceneManager.LoadScene("MainMenu");
    }
    public void promptExit()
    {
        exitPanel.SetActive(true);
    }
    public void exitChoice(bool willExit)
    {
        if (willExit)
        {
            SceneManager.LoadScene("MainMenu");
        }
        exitPanel.SetActive(false);
    }
    public void promptUndo()
    {
        undoPanel.SetActive(true);
    }
    public void getHelp()
    {
        if (!IsReading)
        {
            pageNum = 0;
            helpText.text = Library.helpTextList[pageNum];
            StartReading();
        }
        else
        {
            IsReading = false;
            pageNum = 0;
            HelpCanvas.SetActive(false);
        }
    }
    public void ShouldTrashButton(bool trash)
    {
        ShouldTrash = trash;
        ShouldTrashPopup.SetActive(false);
    }
    public void nextPage()
    {
        if (IsReading)
        {
            if (Library.helpTextList.Count - 1 <= pageNum)
            {
                HelpCanvas.SetActive(false);
                IsReading = false;
            }
            else
            {
                pageNum++;
                helpText.text = Library.helpTextList[pageNum];
            }    
        }
    }
    public void RollDice(bool HUMAN)
    {
        if (IsCPUTurn() && HUMAN)
        {
            return;
        }

        if (activePlayer == 0) RollButton1.SetActive(false); else RollButton2.SetActive(false);


        var roll1 = Random.Range(1, 9);
        var roll2 = Random.Range(1, 9); 
        //var roll1 = Random.Range(4, 5);
        //var roll2 = Random.Range(4, 5);

        higherMove = roll1 > roll2 ? roll1 : roll2;
        lowerMove = roll1 > roll2 ? roll2 : roll1;

        UpdateDiceSkin(activePlayer == 0 ? HigherRollImage1 : HigherRollImage2,
            activePlayer == 0 ? LowerRollImage1 : LowerRollImage2,
            GetActivePlayer().player.Username,
            GetActivePlayer().TeamYellow);
       
        actionText.text = "Select a move";

        ResetDice();

        if (IsCPUTurn())
        {
            StartCoroutine(MoveCPUIngredient());
        }
        
    }

    private void ResetDice(bool undo = false)
    {
        if (activePlayer == 0)
        {
            HigherRollText1.text = higherMove.ToString();
            LowerRollText1.text = lowerMove.ToString();
            RolledPanel1.SetActive(true);

            HigherRollButton1.interactable = true;
            LowerRollButton1.interactable = true;
            HigherRollImage1.interactable = true;
            LowerRollImage1.interactable = true;
            if (!undo)
            {
                HigherRollImage1.GetComponent<Animation>().Play("Roll");
                LowerRollImage1.GetComponent<Animation>().Play("Roll2");
            }
        }
        else
        {
            HigherRollText2.text = higherMove.ToString();
            LowerRollText2.text = lowerMove.ToString();
            RolledPanel2.SetActive(true);
            HigherRollButton2.interactable = true;
            LowerRollButton2.interactable = true;
            HigherRollImage2.interactable = true;
            LowerRollImage2.interactable = true;
            if (!undo)
            {
                HigherRollImage2.GetComponent<Animation>().Play("Roll");
                LowerRollImage2.GetComponent<Animation>().Play("Roll2");
            }
        } 
    }

    private void UpdateDiceSkin(Button HigherRoll, Button LowerRoll, string Username, bool team)
    {
        //if (Username == "Jenn")
        //{
        //    HigherRoll.gameObject.GetComponent<Image>().sprite = allD10s[JennDie1];
        //    LowerRoll.gameObject.GetComponent<Image>().sprite = allD10s[JennDie2];
        //}
        //else 
        if (Username == Settings.LoggedInPlayer.Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.SelectedDie != 0 ? allD10s[Settings.LoggedInPlayer.SelectedDie] : team ? yellowD8 : purpleD8;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.SelectedDie2 != 0 ? allD10s[Settings.LoggedInPlayer.SelectedDie2] : team ? yellowD8 : purpleD8;
        }
        else if (Username == Settings.SecondPlayer.Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.SecondPlayer.SelectedDie != 0 ? allD10s[Settings.SecondPlayer.SelectedDie] : team ? yellowD8 : purpleD8;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.SecondPlayer.SelectedDie2 != 0 ? allD10s[Settings.SecondPlayer.SelectedDie2] : team ? yellowD8 : purpleD8;
        }
    }

    public void RollSelected(bool isHigher)
    {
        StartCoroutine(RollSelected(isHigher, true));
    }
    #endregion

    #region CPU Methods
    private IEnumerator CPUTurn()
    {
        yield return new WaitForSeconds(.5f);
        RollDice(false);
    }

    public IEnumerator MoveCPUIngredient()
    {
        yield return new WaitForSeconds(.5f);

        if (IsCPUTurn())
            yield return StartCoroutine(FindBestMove());

        if (!GameOver && IsCPUTurn() && (IngredientMovedWithLower == null || IngredientMovedWithHigher == null))
            yield return StartCoroutine(FindBestMove());

        IngredientMovedWithLower = null;
        IngredientMovedWithHigher = null;
    }
    
    private IEnumerator SetCPUVariables()
    {
        UseableIngredients = AllIngredients.Where(x => x.IngredientId != firstIngredientMoved && (x.currentTile.ingredients.Peek() == x || x.routePosition == 0)).ToList();
        foreach (var ing in AllIngredients)
        {
            //find what ingredients actual end will be accounting for cooked ingredients
            ing.endHigherPositionWithoutSlide = ing.routePosition+higherMove;
            ing.endLowerPositionWithoutSlide = ing.routePosition+lowerMove;
            ing.distanceFromScore = 0;
            for (int i = ing.routePosition+1; i <= ing.endHigherPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i%26].ingredients.Count() > 0 && ing.fullRoute[i % 26].ingredients.Peek().isCooked) {
                    ing.endHigherPositionWithoutSlide++;
                }
            }
            for (int i = ing.routePosition+1; i <= ing.endLowerPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i%26].ingredients.Count() > 0 && ing.fullRoute[i % 26].ingredients.Peek().isCooked) {
                    ing.endLowerPositionWithoutSlide++;
                }
            }

            for (int i = ing.routePosition+1; i <= 26; i++)
            {
                if (ing.fullRoute[i%26].ingredients.Count() == 0 || !ing.fullRoute[i%26].ingredients.Peek().isCooked) {
                    ing.distanceFromScore++;
                }
            }

            //account for slides
            if (ing.fullRoute[ing.endLowerPositionWithoutSlide % 26].hasSpoon)
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide + 6;
            }
            else if (ing.fullRoute[ing.endLowerPositionWithoutSlide % 26].hasSpatula)
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide - 6;
            }
            else
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide;
            }

            if (ing.fullRoute[ing.endHigherPositionWithoutSlide % 26].hasSpoon)
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide + 6;
            }
            if (ing.fullRoute[ing.endHigherPositionWithoutSlide % 26].hasSpatula)
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide - 6;
            }
            else
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide;
            }
        }
        //create subsets based on new info
        TeamIngredients = AllIngredients.Where(x => x.TeamYellow == GetActivePlayer().TeamYellow).ToList();
        EnemyIngredients = AllIngredients.Where(x => x.TeamYellow != GetActivePlayer().TeamYellow).ToList();

        UseableIngredients = UseableIngredients.OrderBy(x => x.isCooked).ThenBy(x => x.distanceFromScore).ToList();
        UseableTeamIngredients = UseableIngredients.Where(x => x.TeamYellow == GetActivePlayer().TeamYellow).ToList();
        UseableEnemyIngredients = UseableIngredients.Where(x => x.TeamYellow != GetActivePlayer().TeamYellow).ToList();

        //If reading wait
        while (IsReading)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator FindBestMove()
    {
        yield return StartCoroutine(SetCPUVariables());
        //TODO add HelpStomp
        var ingredientToMove = CookIngredient()
            ?? HelpScore()
            ?? MoveOffStackToScore()
            ?? BeDumb()
            ?? MovePastPrep()
            ?? StompEnemy(true)
            ?? GoToTrash()
            ?? StompEnemy(false)
            ?? MoveIntoScoring()
            ?? Slide(false)
            ?? BoostWithCookedIngredient()
            ?? MoveFrontMostEnemy()
            ?? Slide(true)
            ?? StackEnemy(true)
            ?? StackEnemy(false)
            ?? MoveOffStack(false)
            ?? MoveFrontMostIngredient(false,false)
            ?? MoveOffStack(true)
            ?? MoveFrontMostIngredient(true, false)
            ?? MoveFrontMostIngredient(false, true)
            ?? MoveNotPastPrep()
            ?? MoveCookedPastPrep()
            ?? MoveEnemyIngredient()
            ?? MoveRandomly();
        yield return StartCoroutine(MoveCPUIngredient(ingredientToMove));
    }
    private IEnumerator MoveCPUIngredient(Ingredient ingredientToMove)
    {
        if (ingredientToMove == null)
        {
            firstMoveTaken = true;
            yield return StartCoroutine(DoneMoving());
        }
        else
        {
            yield return StartCoroutine(RollSelected(ingredientToMove == IngredientMovedWithHigher, false));
            if (!firstMoveTaken && higherMove == lowerMove)
            {
                IngredientMovedWithHigher = null;
            }
            yield return new WaitForSeconds(.5f);
            yield return StartCoroutine(DeactivateAllSelectors());
            yield return StartCoroutine(ingredientToMove.Move());
        }
    }
    internal void PrepShitTalk(TalkType talk)
    {
        if (!string.IsNullOrEmpty(talkShitText.text))
            return;

        var username = GetActivePlayer().player.Username;
        switch (talk)
        {
            case TalkType.MoveRandomly:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "Hmm, you stumped me!";
                        break;
                    case "Joe":
                        talkShitText.text = "It really didn't matter...";
                        break;
                    case "Jenn":
                        talkShitText.text = "#ImNotEvenTrying";
                        break;
                    case "Chrissy":
                        talkShitText.text = "You did't leave me any good moves!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Trash:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "My pa paw taught me to take out the trash.";
                        break;
                    case "Joe":
                        talkShitText.text = "Go back where you belong!";
                        break;
                    case "Jenn":
                        talkShitText.text = "#YouAreTrash";
                        break;
                    case "Chrissy":
                        talkShitText.text = "Watch out for the trash cans!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Stomped:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "Stomped!";
                        break;
                    case "Joe":
                        talkShitText.text = "Have fun in Prep...";
                        break;
                    case "Jenn":
                        talkShitText.text = "#SorryNotSorry";
                        break;
                    case "Chrissy":
                        talkShitText.text = "Oops, didn't see you there!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.StompedBySelf:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "Self Stomp!";
                        break;
                    case "Joe":
                        talkShitText.text = "Stop hitting yourself.";
                        break;
                    case "Jenn":
                        talkShitText.text = "#GetRekt";
                        break;
                    case "Chrissy":
                        talkShitText.text = "Oh no, I was trying to help!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.SafeZoned:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "Safe for me, not you!";
                        break;
                    case "Joe":
                        talkShitText.text = "You owe me one for moving you...";
                        break;
                    case "Jenn":
                        talkShitText.text = "#SickBurn";
                        break;
                    case "Chrissy":
                        talkShitText.text = "I'm just teaching you how the safe zone works";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Cook:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "My me maw taught me to cook like this.";
                        break;
                    case "Joe":
                        talkShitText.text = "Watch and learn!";
                        break;
                    case "Jenn":
                        talkShitText.text = "#Winning";
                        break;
                    case "Chrissy":
                        talkShitText.text = "This is fun!";
                        break;
                    default:
                        break;
                }
                break; 
            case TalkType.HelpCook:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "Alley Oop!";
                        break;
                    case "Joe":
                        talkShitText.text = "This is my final form!";
                        break;
                    case "Jenn":
                        talkShitText.text = "#StrategicAF";
                        break;
                    case "Chrissy":
                        talkShitText.text = "Teamwork makes the dreamwork!";
                        break;
                    default:
                        break;
                }
                break; 
            case TalkType.MovePastPrep:
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = "You know what they say...";
                        break;
                    case "Joe":
                        talkShitText.text = "HAHA you got too close to the end!";
                        break;
                    case "Jenn":
                        talkShitText.text = "#ByeFelicia";
                        break;
                    case "Chrissy":
                        talkShitText.text = "I'm sorry, I just had to!";
                        break;
                    default:
                        break;
                }
                break; 
            case TalkType.SentBack:
                username = playerList.FirstOrDefault(x=> x.player.playerType == PlayerTypes.CPU).player.Username;

                var ZachOptions = new List<string>() { "", "Man it sucks to suck...", "Dag Nabbit!", "What do you think your doing!?" };
                var JoeOptions = new List<string>() { "", "Wait, you can't do that to me!!", "Watch your back!", "I'll remember that!"};
                var JennOptions = new List<string>() { "", "#Oooof", "#Toxic", "#OhNoYouDidnt" };
                var ChrissyOptions = new List<string>() { "", "Well that wasn't very nice!", "Hey, quit doing that!", "Treat others how you want to be treated..." };
                switch (username)
                {
                    case "Zach":
                        talkShitText.text = ZachOptions[Random.Range(0, ZachOptions.Count())];
                        break;
                    case "Joe":
                        talkShitText.text = JoeOptions[Random.Range(0, JoeOptions.Count())];
                        break;
                    case "Jenn":
                        talkShitText.text = JennOptions[Random.Range(0, JennOptions.Count())];
                        break;
                    case "Chrissy":
                        talkShitText.text = ChrissyOptions[Random.Range(0, ChrissyOptions.Count())];
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }
    private Ingredient MoveRandomly()
    {
        if (UseableTeamIngredients.Count > 0)
        {
            if (IngredientMovedWithHigher == null)
            {

                IngredientMovedWithHigher = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return IngredientMovedWithHigher;
                }
            }

            if (IngredientMovedWithLower == null && higherMove != lowerMove)
            {
                IngredientMovedWithLower = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveNotPastPrep()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Not Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Not Past Prep"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    
    private Ingredient MoveIntoScoring()
    {
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 23
            && x.endLowerPosition > 17
            && !x.isCooked
            && x.distanceFromScore > 8
            && CanMoveSafely(x, x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Into Scoring"; }
                return IngredientMovedWithLower;
            }
        }
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 23
            && x.endHigherPosition > 17
            && !x.isCooked
            && x.distanceFromScore > 8
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Into Scoring"; }
                return IngredientMovedWithHigher;
            }
        }
        return null;
    }

    //private Ingredient MoveCookedIngredient()
    //{
    //    if (IngredientMovedWithHigher == null)
    //    {
    //        IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endHigherPosition < 26
    //        && CanMoveSafely(x, x.endHigherPosition));
    //        if (IngredientMovedWithHigher != null)
    //        {
    //            if (Settings.IsDebug) { talkShitText.text = "Move Cooked Ingredient"; }
    //            return IngredientMovedWithHigher;
    //        }
    //    }
    //    if (IngredientMovedWithLower == null && higherMove != lowerMove)
    //    {
    //        IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endLowerPosition < 26
    //        && CanMoveSafely(x, x.endLowerPosition));
    //        if (IngredientMovedWithLower != null)
    //        {
    //            if (Settings.IsDebug) { talkShitText.text = "Move Cooked Ingredient"; }
    //            return IngredientMovedWithLower;
    //        }
    //    }
    //    return null;
    //}  
    private Ingredient BoostWithCookedIngredient()
    {
        if (!UseableIngredients.Any(x => x.isCooked) || IngredientMovedWithHigher != null || IngredientMovedWithLower != null)
            return null;

        if (IngredientMovedWithHigher == null)
        {
            var CookedIngredientsThatCanHelp = UseableTeamIngredients.Where(x => x.isCooked
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endLowerPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endHigherPosition && y.endLowerPosition >= x.endHigherPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endLowerPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endLowerPosition > x.routePosition && y.endLowerPosition < x.endHigherPosition))).ToList();

            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                IngredientMovedWithHigher = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x,x.endHigherPosition));
                if (IngredientMovedWithHigher != null)
                {
                    if (Settings.IsDebug) { talkShitText.text = "Boosted with Cooked"; }
                    return IngredientMovedWithHigher;
                }
            }
        }

        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            var CookedIngredientsThatCanHelp = UseableIngredients.Where(x => x.isCooked 
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endHigherPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endLowerPosition && y.endHigherPosition >= x.endLowerPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endHigherPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endHigherPosition > x.routePosition && y.endHigherPosition < x.endLowerPosition))).ToList();
            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                IngredientMovedWithLower = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x, x.endLowerPosition));
                if (IngredientMovedWithLower != null)
                {
                    if (Settings.IsDebug) { talkShitText.text = "Boosted with Cooked"; }
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }

    private bool CanMoveSafely(Ingredient x, int endPosition)
    {
        return !TeamIngredients.Any(y => y.routePosition == endPosition % 26) && (!x.fullRoute[endPosition % 26].isSafe || x.fullRoute[endPosition % 26].ingredients.Count() == 0);
    }

    private Ingredient MoveCookedPastPrep()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
             && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveFrontMostIngredient(bool withCooked, bool moveStacked)
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderByDescending(x => x.endHigherPosition).FirstOrDefault(x => x.endHigherPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 8 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.fullRoute[x.routePosition].ingredients.Count == 1 )
            && CanMoveSafely(x, x.endHigherPosition)); //Dont stomp on safe area
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Front Most " + (withCooked? "cooked ": "uncooked ") + (moveStacked? "stacked ":"unstacked ") + "Ingredient"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.endLowerPosition).FirstOrDefault(x => x.endLowerPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 8 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.fullRoute[x.routePosition].ingredients.Count == 1)
            && CanMoveSafely(x, x.endLowerPosition)); //Dont stomp on safe area
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Front Most Ingredient"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }  
    private Ingredient MoveEnemyIngredient()
    {

        if (IngredientMovedWithLower == null && higherMove != lowerMove && UseableTeamIngredients.Count(x=>x.routePosition == 0) == 0)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.OrderBy(x => x.endLowerPosition).FirstOrDefault(x =>
            !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26 && !x.fullRoute[x.endLowerPosition % 26].isSafe));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Enemy"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    } 
    private Ingredient MoveOffStack(bool notInScoring = true)
    {
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => (notInScoring || x.distanceFromScore < 9) 
            && (x.currentTile.ingredients.Count > 1 && x.currentTile.ingredients.Any(x=>x.TeamYellow == GetActivePlayer().TeamYellow))
            && CanMoveSafely(x, x.endLowerPosition)); //Dont stomp on safe area
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Off stack" + (notInScoring?" from non-scoring" : " from scoring"); }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveOffStackToScore()
    {
        if (IngredientMovedWithHigher == null && IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            var ingredientThatCouldScore = TeamIngredients.FirstOrDefault(x => x.endHigherPosition == 26 && !x.isCooked && x.fullRoute[x.routePosition].ingredients.Peek() != x);
            if (ingredientThatCouldScore != null)
            {
                IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x.routePosition == ingredientThatCouldScore.routePosition && x.endLowerPosition != x.routePosition); //Dont stomp on safe area
                if (IngredientMovedWithLower != null)
                {
                    if (Settings.IsDebug) { talkShitText.text = "Move off stack to score"; }
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveFrontMostEnemy()
    {
        if (IngredientMovedWithLower == null && higherMove != lowerMove && EnemyIngredients.Count(x=>!x.isCooked) == 1)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.distanceFromScore < 9 //move from scoring position
            && !x.isCooked
            && !x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula
            && !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26)); //Dont stomp yourself
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Front Most Enemy"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient Slide(bool WithCooked)
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 8 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x, x.endHigherPosition)
            && ((x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpoon));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Slide"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 8 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x, x.endHigherPosition)
            && ((x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpoon));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Slide"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient GoToTrash()
    {
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.routePosition != 0 
            && !x.isCooked
            && ((lowerMove < 6 && (x.endLowerPositionWithoutSlide % 26) == 10) || (x.endLowerPositionWithoutSlide % 26) == 18));
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.Trash);
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient StompEnemy(bool useEither = true)
    {
        var teamToMove = useEither ? UseableTeamIngredients : UseableEnemyIngredients;
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation unless cooked
             && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
             && x.endLowerPosition != 0
             && x.endLowerPosition != x.routePosition
             && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
             && x.fullRoute[x.endLowerPosition % 26].isSafe); //stomp safe area if someone is there
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithLower;
            }
        }

        if (useEither && IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && x.fullRoute[x.endHigherPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (IngredientMovedWithHigher != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithHigher;
            }
        }
        
        return null;
    }  
    private Ingredient StackEnemy(bool useEither = true)
    {
        var teamToMove = useEither ? UseableTeamIngredients : UseableEnemyIngredients;
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation
             && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
             && x.endLowerPosition != 0
             && x.endLowerPosition != x.routePosition
             && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
             && !x.fullRoute[x.endLowerPosition % 26].isSafe); //stomp safe area if someone is there
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Stack"; }
                return IngredientMovedWithLower;
            }
        }

        if (useEither && IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && !x.fullRoute[x.endHigherPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Stack"; }
                return IngredientMovedWithHigher;
            }
        }
        
        return null;
    } 
    //private Ingredient StompSafeZone()   
    //{
    //    if (IngredientMovedWithLower == null && higherMove != lowerMove)
    //    {
    //        IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
    //        && x.routePosition != 0 //dont move them from prep
    //        && AllIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition % 26) //anyone there?
    //        && x.fullRoute[x.endLowerPosition % 26].isSafe);
    //        if (IngredientMovedWithLower != null)
    //        {
    //            PrepShitTalk(TalkType.SafeZoned);
    //            return IngredientMovedWithLower;
    //        }
    //    }
    //    return null;
    //}
    private Ingredient CookIngredient()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition == 26 && !x.isCooked);
            if (IngredientMovedWithHigher != null)
            {
                PrepShitTalk(TalkType.Cook);
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition == 26 && !x.isCooked); //Move into pot if not cooked
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.Cook);
                return IngredientMovedWithLower;
            }
        }
        return null;
    } 
    private Ingredient HelpScore()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        Ingredient ScoreableIng = null;
        if (IngredientMovedWithLower == null && higherMove != lowerMove && IngredientMovedWithHigher == null)
        {
            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 25);
            if (ScoreableIng != null)
            {
                IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition < ScoreableIng.routePosition 
                && x.endHigherPosition > ScoreableIng.routePosition
                && x.endHigherPosition < 26
                && CanMoveSafely(x, x.endHigherPosition));
                if (IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return IngredientMovedWithHigher;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 25);

            if (ScoreableIng != null)
            {
                IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition < ScoreableIng.routePosition 
                && x.endLowerPosition > ScoreableIng.routePosition
                && x.endLowerPosition < 26
                && CanMoveSafely(x, x.endLowerPosition));
                if (IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return IngredientMovedWithLower;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 27);
            if (ScoreableIng != null)
            {
                IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition > ScoreableIng.routePosition 
                && x.endHigherPosition % 26 < ScoreableIng.routePosition
                && CanMoveSafely(x, x.endHigherPosition));
                if (IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return IngredientMovedWithHigher;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 27);
            if (ScoreableIng != null)
            {
                IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition > ScoreableIng.routePosition 
                && x.endLowerPosition % 26 < ScoreableIng.routePosition
                && CanMoveSafely(x, x.endLowerPosition));
                if (IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }

    private Ingredient MovePastPrep()
    {
        if (IngredientMovedWithLower == null && higherMove != lowerMove)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => x.endLowerPosition >= 26 
            && !x.isCooked); //Move enemy past pot if uncooked
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.MovePastPrep);
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient BeDumb()
    {
        if (!Settings.IsDebug && !Settings.HardMode && (Settings.LoggedInPlayer.Wins == 0 || (!hasBeenDumb && (Random.Range(0, Mathf.Min(Settings.LoggedInPlayer.Wins,50)) == 0 ))))
        {
            if (IngredientMovedWithHigher == null)
            {
                var ingsToMove = UseableTeamIngredients.Where(x => x.distanceFromScore > 8).ToList();
                if (ingsToMove.Count() > 0)
                {
                    var toMove = ingsToMove[Random.Range(0, ingsToMove.Count())];
                    hasBeenDumb = true;
                    IngredientMovedWithHigher = toMove;
                    return IngredientMovedWithHigher;
                }
            }

            if (IngredientMovedWithLower == null && higherMove != lowerMove)
            {
                var ingsToMove = UseableIngredients.Where(x => x.distanceFromScore > 8).ToList();
                if (ingsToMove.Count() > 0)
                {
                    var toMove = ingsToMove[Random.Range(0, ingsToMove.Count())];
                    hasBeenDumb = true;
                    IngredientMovedWithLower = toMove;
                    return IngredientMovedWithLower;
                }
            } 
        }
        return null;
    }

   
    #endregion

    #region TODO
    public void undoChoice(bool willPay)
    {
        if (willPay)
        {
            ResetMovementVariables();
            ResetDice(true);
            StartCoroutine(DeactivateAllSelectors());
            for (var i = 0; i < TurnStartPositions.Count(); i++)
            {
                if (TurnStartPositions[i].ingPos != AllIngredients[i].routePosition || TurnStartPositions[i].ingCooked != AllIngredients[i].isCooked)
                {
                    StartCoroutine(RollbackIngredient(AllIngredients[i], TurnStartPositions[i]));
                        //.MoveToNextTile(AllIngredients[i].fullRoute[TurnStartPositions[i].ingPos].transform.position));
                }
            }
        }
        undoPanel.SetActive(false);
    }

    private IEnumerator RollbackIngredient(Ingredient ingredientToRollback, TurnPosition turnPosition)
    {
        if (ingredientToRollback.isCooked != turnPosition.ingCooked)
        {
            ingredientToRollback.anim.Play("flip");
            ingredientToRollback.isCooked = false;
            ingredientToRollback.CookedQuad.gameObject.SetActive(false);
            ingredientToRollback.BackCookedQuad.gameObject.SetActive(true);
        }

        if (ingredientToRollback.routePosition != turnPosition.ingPos)
        {
            ingredientToRollback.anim.Play("flip");
            ingredientToRollback.currentTile.ingredients.Pop();
            ingredientToRollback.routePosition = turnPosition.ingPos;
            if (turnPosition.ingPos == 0)
            {
                yield return StartCoroutine(MoveToNextEmptySpace(ingredientToRollback));
            }
            else
            {
                yield return StartCoroutine(ingredientToRollback.MoveToNextTile(ingredientToRollback.fullRoute[turnPosition.ingPos].transform.position));
                ingredientToRollback.currentTile = ingredientToRollback.fullRoute[turnPosition.ingPos];
                ingredientToRollback.currentTile.ingredients.Push(ingredientToRollback);
            }
        }
    }
    #endregion
}
