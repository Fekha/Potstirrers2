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
    private float elapsed = 0f;
    private int aliveCheck = 0;
    internal int TurnNumber = 1;
    internal int higherMove = 0;
    internal int lowerMove = 0;
    internal bool higherMoveSelected;
    internal List<Ingredient> AllIngredients;
    internal Ingredient IngredientMovedWithLower;
    internal Ingredient IngredientMovedWithHigher;
    private SqlController sql;
    private GamePlayer playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    internal float talkingTimeStart;
    
    internal bool GameOver = false;
    private bool? OnlineDiceFound = null;
    private bool LookingForTurn = false;
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
    public GameObject ConnectPanel;
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
        playerList[0].TeamYellow = true;
        playerList[1].TeamYellow = false;
        AllIngredients = playerList.SelectMany(y => y.myIngredients).OrderBy(x => x.IngredientId).ToList();
#if UNITY_EDITOR
        //Settings.IsDebug = true;
        //Settings.LoggedInPlayer.Experimental = true;
#endif
       
        if (Settings.OnlineGameId == 0)
        {
            activePlayer = Random.Range(0, 2);

            if (Settings.LoggedInPlayer.Wins == 0 && !Settings.IsDebug)
            {
                activePlayer = 0;
                getHelp();
            }

            if (Settings.IsDebug)
            {
                playerList[0].player = Settings.CPUPlayers[0];
                playerList[1].player = Settings.CPUPlayers[1];
            }
            else if (Settings.LoggedInPlayer.PlayAsPurple)
            {
                playerList[0].player = Settings.SecondPlayer.ShallowCopy(); ;
                playerList[1].player = Settings.LoggedInPlayer.ShallowCopy();
            }
            else
            {
                playerList[0].player = Settings.LoggedInPlayer.ShallowCopy(); ;
                playerList[1].player = Settings.SecondPlayer.ShallowCopy(); ;
            }
            SetSkins();
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
        else
        {
            GameId = Settings.OnlineGameId;
            StartCoroutine(sql.RequestRoutine($"analytic/FindMyGame?UserId={Settings.LoggedInPlayer.UserId}&GameId={Settings.OnlineGameId}", GetOnlineGameCallback));
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
        ProfileText1.color = new Color32(255, 202, 24, 255);
        ProfileText2.color = new Color32(171, 20, 157, 255);
    }
    private void Update()
    {
        if (Settings.OnlineGameId != 0)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= 1f)
            {
                aliveCheck++;
                elapsed = elapsed % 1f;
                if (GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
                {
                    if (OnlineDiceFound == false)
                    {
                        StartCoroutine(sql.RequestRoutine($"analytic/GetGameRoll?GameId={Settings.OnlineGameId}", CheckForRollsCallback));
                    }
                    if (LookingForTurn == true)
                    {
                        StartCoroutine(sql.RequestRoutine($"analytic/GetTurn?GameId={Settings.OnlineGameId}", CheckForGameTurnsCallback));
                    }
                }
                if (aliveCheck >= 20 && !GameOver)
                {
                    aliveCheck = 0;
                    StartCoroutine(sql.RequestRoutine($"analytic/CheckGameAlive?UserId={Settings.LoggedInPlayer.UserId}&GameId={Settings.OnlineGameId}", GameIsAliveCallback));
                }
            }
        }

        if (Settings.IsDebug && IsReading == true && IsCPUTurn())
        {
            var timeSince = Time.time - readingTimeStart;
            if (timeSince > 2.0)
            {
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
    private void SetSkins()
    {
        ProfileText1.text = playerList[0].player.Username;
        ProfileText2.text = playerList[1].player.Username;
        UpdateIngredientSkins(0);
        UpdateIngredientSkins(1);
        UpdateDiceSkin(HigherRollImage1, LowerRollImage1, playerList[0].player.Username);
        UpdateDiceSkin(HigherRollImage2, LowerRollImage2, playerList[1].player.Username);
    }

    private void UpdateIngredientSkins(int v)
    {
        var HumanIngredients = AllIngredients.Where(x => x.TeamYellow == playerList[v].TeamYellow).ToList();

        var meatQuads = HumanIngredients.FirstOrDefault(x => x.type == "Meat").NormalQuad.GetComponent<MeshRenderer>();
        var meatMats = meatQuads.materials;
        meatMats[0] = allMeatMaterials[playerList[v].player.SelectedMeat];
        meatQuads.materials = meatMats;

        var veggieQuads = HumanIngredients.FirstOrDefault(x => x.type == "Veggie").NormalQuad.GetComponent<MeshRenderer>();
        var veggieMats = veggieQuads.materials;
        veggieMats[0] = allVeggieMaterials[playerList[v].player.SelectedVeggie];
        veggieQuads.materials = veggieMats;

        var fruitQuads = HumanIngredients.FirstOrDefault(x => x.type == "Fruit").NormalQuad.GetComponent<MeshRenderer>();
        var fruitMats = fruitQuads.materials;
        fruitMats[0] = allFruitMaterials[playerList[v].player.SelectedFruit];
        fruitQuads.materials = fruitMats;

        meatQuads = HumanIngredients.FirstOrDefault(x => x.type == "Meat").BackNormalQuad.GetComponent<MeshRenderer>();
        meatMats = meatQuads.materials;
        meatMats[0] = allMeatMaterials[playerList[v].player.SelectedMeat];
        meatQuads.materials = meatMats;

        veggieQuads = HumanIngredients.FirstOrDefault(x => x.type == "Veggie").BackNormalQuad.GetComponent<MeshRenderer>();
        veggieMats = veggieQuads.materials;
        veggieMats[0] = allVeggieMaterials[playerList[v].player.SelectedVeggie];
        veggieQuads.materials = veggieMats;

        fruitQuads = HumanIngredients.FirstOrDefault(x => x.type == "Fruit").BackNormalQuad.GetComponent<MeshRenderer>();
        fruitMats = fruitQuads.materials;
        fruitMats[0] = allFruitMaterials[playerList[v].player.SelectedFruit];
        fruitQuads.materials = fruitMats;
    }

    private void GetNewGameCallback(string data)
    {
        GameId = sql.jsonConvert<int>(data);
        StartCoroutine(TakeTurn());
    }  
    private void GetShouldTrashCallback(string data)
    {
        ShouldTrash = sql.jsonConvert<bool?>(data);
    }
    //private void EndTurnCallback(string data)
    //{
    //    var Player1Turn = sql.jsonConvert<bool>(data);
    //    activePlayer = Player1Turn ? 0 : 1;
    //    StartCoroutine(TakeTurn());
    //} 
    private void GameIsAliveCallback(string data)
    {
        var GameAlive = sql.jsonConvert<bool>(data);
        if (!GameAlive)
        {
            GameIsOver(playerList.FirstOrDefault(x=>x.player.Username == Settings.LoggedInPlayer.Username));
        }
    }
    private void GetOnlineGameCallback(string data)
    {
        var GameState = sql.jsonConvert<GameState>(data);
        GameState.Player1.IsGuest = false;
        GameState.Player2.IsGuest = false; 
        GameState.Player1.playerType = PlayerTypes.HUMAN;
        GameState.Player2.playerType = PlayerTypes.HUMAN;
        playerList[0].player = GameState.Player1;
        playerList[1].player = GameState.Player2;
        activePlayer = GameState.IsPlayer1Turn ? 0 : 1;
        SetSkins();
        StartCoroutine(TakeTurn());
    }
   
    private void CheckForRollsCallback(string data)
    {
        var state = sql.jsonConvert<GameRoll>(data);
        if (state != null)
        {
            OnlineDiceFound = true;
            LookingForTurn = true;
            SetMoveNumbers(state.roll1, state.roll2);
        }
    }
    private void CheckForGameTurnsCallback(string data)
    {
        var turn = sql.jsonConvert<GameTurn>(data);
        if (turn != null && turn.IngId != 0)
        {
            LookingForTurn = false;
            var ingToMove = AllIngredients.FirstOrDefault(x => x.IngredientId == turn.IngId);
            if (turn.Higher)
                IngredientMovedWithHigher = ingToMove;
            else
                IngredientMovedWithLower = ingToMove;
            StartCoroutine(CpuLogic.i.MoveCPUIngredient(ingToMove));
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
       
        HigherRollImage1.interactable = false;
        LowerRollImage1.interactable = false;
        HigherRollButton1.interactable = false;
        LowerRollButton1.interactable = false;
        HigherRollImage2.interactable = false;
        LowerRollImage2.interactable = false;
        HigherRollButton2.interactable = false;
        LowerRollButton2.interactable = false;
        OnlineDiceFound = null;
        ResetMovementVariables();
        activePlayer = (activePlayer + 1) % playerList.Count();
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
        SetIngredientPositions();
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
        if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
        {
            OnlineDiceFound = false;
        }
    }
    private IEnumerator CPUTurn()
    {
        yield return new WaitForSeconds(.5f);
        RollDice(false);
    }
    private void SetIngredientPositions()
    {
        for (int i = 0; i < TurnStartPositions.Count(); i++)
        {
            TurnStartPositions[i] = new TurnPosition(AllIngredients[i].routePosition, AllIngredients[i].isCooked);
        }
    }

    internal IEnumerator RollSelected(bool isHigher, bool HUMAN)
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
    private void GameIsOver(GamePlayer rageQuit = null)
    {
        GameOver = true;
        if (!Settings.IsDebug)
        {
            var player1Count = (rageQuit == null || rageQuit.player.Username == playerList[0].player.Username) ? playerList[0].myIngredients.Count(x => x.isCooked) : 0;
            var player2count = (rageQuit == null || rageQuit.player.Username == playerList[1].player.Username) ? playerList[1].myIngredients.Count(x => x.isCooked) : 0;
            var url = $"analytic/GameEnd?GameId={GameId}&Player1Cooked={player1Count}&Player2Cooked={player2count}&TotalTurns={TurnNumber}&HardMode={Settings.HardMode}&RageQuit={rageQuit != null}";
            StartCoroutine(sql.RequestRoutine(url, null, true));
        }
        var BonusXP = Settings.HardMode ? 100 : 0;
        var BonusStars = Settings.HardMode ? 50 : 0;
        if (rageQuit != null)
        {
            playerWhoWon = rageQuit;
        }
        else
        {
            playerWhoWon = playerList.FirstOrDefault(x => x.myIngredients.All(y => y.isCooked));
        }
        var playerwhoLost = playerList.FirstOrDefault(x => x.player.Username != playerWhoWon.player.Username);
        var lostCount = rageQuit == null ? playerwhoLost.myIngredients.Count(x => x.isCooked):0;
        eventText.text = "GAME OVER! \n \n" ;
        if (rageQuit == null)
        {
            eventText.text += playerWhoWon.player.Username + " won. \n \n";
        }
        else
        {
            eventText.text += playerwhoLost.player.Username + " quit. \n \n";
        }
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
            if (rageQuit == null)
            {
                eventText.text += "You each gained 50 Calories for each of your cooked ingredients! \n" + playerWhoWon.player.Username + " earned " + wonXp + " XP \n " + playerwhoLost.player.Username + " earned " + lostXp + " xp!";
            }
            else
            {
                eventText.text += "You gained 50 Calories for each of your cooked ingredients and they gained nothing! \n" + playerWhoWon.player.Username + " earned " + wonXp + " XP \n ";
            }
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
   
    internal IEnumerator DoneMoving()
    {
        TurnNumber++;

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

            if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
                LookingForTurn = true;

            if (!IsCPUTurn())
            {
                //todo add undo to multiplayer
                if (Settings.OnlineGameId == 0)
                {
                    if (activePlayer == 0)
                    {
                        undoButton1.gameObject.SetActive(true);
                    }
                    else
                    {
                        undoButton2.gameObject.SetActive(true);
                    }
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
        if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username == Settings.LoggedInPlayer.Username)
        {
            ShouldTrashPopup.SetActive(true);
        }
        
        while (ShouldTrash == null)
        {
            if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
            {
                StartCoroutine(sql.RequestRoutine($"analytic/GetShouldTrash?GameId={Settings.OnlineGameId}", GetShouldTrashCallback));
            }
            yield return new WaitForSeconds(.5f);
        }
        ShouldTrashPopup.SetActive(false);
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
            StartCoroutine(sql.RequestRoutine($"analytic/EndGame?GameId={Settings.OnlineGameId}"));
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
        if (Settings.OnlineGameId != 0)
        {
            if (GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
                return;
            else
            {
                ShouldTrash = trash;
                StartCoroutine(sql.RequestRoutine($"analytic/UpdateShouldTrash?GameId={Settings.OnlineGameId}&trash={trash}"));
            }
        }
        else
        {
            ShouldTrash = trash;
        }
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
        if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
        {
            return;
        }

        if (IsCPUTurn() && HUMAN)
        {
            return;
        }
        var roll1 = Random.Range(1, 9);
        var roll2 = Random.Range(1, 9);
        if (Settings.OnlineGameId != 0)
        {
            StartCoroutine(sql.RequestRoutine($"analytic/UpdateGameRoll?GameId={Settings.OnlineGameId}&roll1={roll1}&roll2={roll2}"));
        }

        SetMoveNumbers(roll1, roll2);

        if (IsCPUTurn())
        {
            StartCoroutine(CpuLogic.i.FindCPUIngredientMoves());
        }
    }

    private void SetMoveNumbers(int roll1, int roll2)
    {
        RollButton1.SetActive(false);
        RollButton2.SetActive(false);
        higherMove = roll1 > roll2 ? roll1 : roll2;
        lowerMove = roll1 > roll2 ? roll2 : roll1;

        UpdateDiceSkin(activePlayer == 0 ? HigherRollImage1 : HigherRollImage2,
            activePlayer == 0 ? LowerRollImage1 : LowerRollImage2,
            GetActivePlayer().player.Username);

        actionText.text = "Select a move";

        ResetDice();
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

    private void UpdateDiceSkin(Button HigherRoll, Button LowerRoll, string Username)
    {
        if (Username == "Jenn")
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = allD10s[Random.Range(allD10s.Count() - 3, allD10s.Count())];
            LowerRoll.gameObject.GetComponent<Image>().sprite = allD10s[Random.Range(allD10s.Count() - 3, allD10s.Count())];
        }
        else if (Username == playerList[0].player.Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = playerList[0].player.SelectedDie != 0 ? allD10s[playerList[0].player.SelectedDie] : yellowD8;
            LowerRoll.gameObject.GetComponent<Image>().sprite = playerList[0].player.SelectedDie2 != 0 ? allD10s[playerList[0].player.SelectedDie2] : yellowD8;
        }
        else if (Username == playerList[1].player.Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = playerList[1].player.SelectedDie != 0 ? allD10s[playerList[1].player.SelectedDie] : purpleD8;
            LowerRoll.gameObject.GetComponent<Image>().sprite = playerList[1].player.SelectedDie2 != 0 ? allD10s[playerList[1].player.SelectedDie2] : purpleD8;
        }
    }

    public void RollSelected(bool isHigher)
    {
        if (Settings.OnlineGameId != 0 && GetActivePlayer().player.Username != Settings.LoggedInPlayer.Username)
        {
            return;
        }

        StartCoroutine(RollSelected(isHigher, true));
    }
    #endregion

   
    #region UNDO
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
