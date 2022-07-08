using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject IngredientToSpawn;
    #region GameManger Variables
    private float elapsed = 0f;
    private int aliveCheck = 0;
    internal int TurnNumber = 1;
    internal int higherMove = 0;
    internal int lowerMove = 0;
    internal bool higherMoveSelected;
    internal List<Ingredient> AllIngredients = new List<Ingredient>();
    private SqlController sql;
    private Player playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    internal float talkingTimeStart;
    private float turnTime = 0;
    private float rollDuration = 1; //10;
    private float turnDuration = 5; //25;
    private bool? Player1Turn = null;
    internal bool GameOver = false;
    private bool? OnlineDiceFound = null;
    private bool LookingForTurn = false;
    private int GameId = 0;
    private TurnPosition[] TurnStartPositions = new TurnPosition[6];
    private States State = States.Switching;
    List<Ingredient> MoveableList = new List<Ingredient>();
    private enum States
    {
        Switching,
        Rolling,
        Moving
    }
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
    internal Player[] playerList = new Player[2];
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
    public GameObject BlockPlayerActionPanel;

    [Header("Player1Roll")]
    public GameObject TurnBorder1;
    public Button HigherRollImage1;
    public Button HigherRollButton1;
    public Text HigherRollText1;
    public Button LowerRollImage1;   
    public Button LowerRollButton1;
    public Text LowerRollText1;
    public Text ProfileText1;
    public GameObject RollButton1;
    public Image Timer1;

    [Header("Player2Roll")]
    public GameObject TurnBorder2;
    public Button HigherRollImage2;
    public Text HigherRollText2;
    public Button LowerRollImage2;
    public Text LowerRollText2;
    public Text ProfileText2;
    public Image Timer2;


    //[Header("Undo")]
    //public Button undoButton1;
    //public Button undoButton2;

    [Header("Text")]
    public Text eventText;
    public Text helpText;
    public Text talkShitText;

    [Header("Sprite")]
    public Sprite yellowDie;
    public Sprite purpleDie;
    public Sprite yellowD8;
    public Sprite purpleD8;
    public List<Sprite> allD10s;
    
    [Header("Material")]
    public List<Material> allMeatMaterials;
    public List<Material> allVeggieMaterials;
    public List<Material> allFruitMaterials;
    public List<Material> allColorMaterials;
    #endregion
  
    #region GameManager Only
    private void Awake()
    {
        i = this;
        Application.targetFrameRate = 30;
        sql = new SqlController();
        int j = 0;
        foreach (var tile in prepTiles)
        {
            j++;
            var ingObj = Instantiate(IngredientToSpawn);
            ingObj.transform.position = tile.transform.position;
            var ing = ingObj.GetComponent<Ingredient>();
            ing.IngredientId = j;
            ing.Team = j % 2;
            tile.ingredients.Push(ing);
            AllIngredients.Add(ing);
        }
        
#if UNITY_EDITOR
        Settings.IsDebug = true;
        //Settings.LoggedInPlayer.Experimental = true;
#endif
    }

    private void Start()
    {
        if (Settings.OnlineGameId == 0)
        {
            activePlayer = Random.Range(0, 2);

            if (Settings.LoggedInPlayer.Wins == 0 && !Settings.IsDebug)
            {
                activePlayer = 0;
                //getHelp();
            }
            playerList[0] = Settings.LoggedInPlayer;
            playerList[1] = Settings.SecondPlayer;
            if (Settings.IsDebug)
            {
                activePlayer = 0;
                //playerList[0] = Settings.CPUPlayers[0];
                //playerList[1] = Settings.CPUPlayers[1];
            }

            SetSkins();

            if (!Settings.IsDebug && !Settings.LoggedInPlayer.IsGuest)
            {
                var url = $"analytic/GameStart?Player1={playerList[0].UserId}&Player2={playerList[1].UserId}&WineMenu={Settings.LoggedInPlayer.WineMenu}";
                StartCoroutine(sql.RequestRoutine(url, GetNewGameCallback));
            }
            else
            {
                TakeTurn();
            }
        }
        else
        {
            GameId = Settings.OnlineGameId;
            StartCoroutine(sql.RequestRoutine($"analytic/FindMyGame?UserId={Settings.LoggedInPlayer.UserId}&GameId={Settings.OnlineGameId}", GetOnlineGameCallback));
        }
    }
    private void Update()
    {
        if (!IsCPUTurn())
        {
            switch (State)
            {
                case States.Switching:
                    turnTime = 0;
                    if (Timer1.gameObject.activeInHierarchy)
                        Timer1.gameObject.SetActive(false);
                    if (Timer2.gameObject.activeInHierarchy)
                        Timer2.gameObject.SetActive(false);
                    break;
                case States.Rolling:
                    if (Player1Turn == true)
                    {
                        if (!Timer1.gameObject.activeInHierarchy)
                            Timer1.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer1.fillAmount = Mathf.InverseLerp(0, rollDuration, turnTime);
                    }

                    if (Player1Turn == false)
                    {
                        if (!Timer2.gameObject.activeInHierarchy)
                            Timer2.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer2.fillAmount = Mathf.InverseLerp(0, rollDuration, turnTime);
                    }

                    if (turnTime < 0)
                    {
                        State = States.Moving;
                        RollDice(true);
                    }
                    break;
                case States.Moving:
                    if (Player1Turn == true && !isMoving)
                    {
                        if (!Timer1.gameObject.activeInHierarchy)
                            Timer1.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer1.fillAmount = Mathf.InverseLerp(0, turnDuration, turnTime);
                    }

                    if (Player1Turn == false && !isMoving)
                    {
                        if (!Timer2.gameObject.activeInHierarchy)
                            Timer2.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer2.fillAmount = Mathf.InverseLerp(0, turnDuration, turnTime);
                    }

                    if (turnTime < 0)
                    {
                        State = States.Switching;
                        StartCoroutine(MoveForHuman());
                    }
                    break;

                default:
                    // code block
                    break;
            }
        }

        if (Settings.OnlineGameId != 0 && !IsReading)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= 1f)
            {
                aliveCheck++;
                elapsed = elapsed % 1f;
                if (GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
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
        //For Wine Menu
        if (Settings.IsDebug && IsReading && IsCPUTurn())
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

    private IEnumerator MoveForHuman()
    {
        BlockPlayerActionPanel.SetActive(true);
        if (!firstMoveTaken || lowerMove == 0)
        {
            yield return StartCoroutine(RollSelected(true, true));
            if (!firstMoveTaken)
            {
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(MoveableList[Random.Range(0, MoveableList.Count())].Move());
            }
        }
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MoveableList[Random.Range(0, MoveableList.Count())].Move());
    }

    private void SetSkins()
    {
        ProfileText1.text = Settings.LoggedInPlayer.Username;
        ProfileText2.text = Settings.SecondPlayer.Username;
        UpdateIngredientSkins();
        UpdateDiceSkin(HigherRollImage1, LowerRollImage1, Settings.LoggedInPlayer.Username);
        UpdateDiceSkin(HigherRollImage2, LowerRollImage2, Settings.SecondPlayer.Username);
    }

    private void UpdateIngredientSkins()
    {
        for (int i = 0; i < playerList.Length; i++)
        {
            var playerIngs = AllIngredients.Where(x => x.Team == i).ToList();
            for (int j = 0; j < playerIngs.Count; j++)
            {
                var frontQuads = playerIngs[j].NormalQuad.GetComponent<MeshRenderer>();
                var frontMats = frontQuads.materials;
                frontMats[0] = allMeatMaterials[playerList[i].SelectedMeat];
                frontQuads.materials = frontMats;
                
                var backQads = playerIngs[j].BackNormalQuad.GetComponent<MeshRenderer>();
                var backMats = backQads.materials;
                backMats[0] = allMeatMaterials[playerList[i].SelectedMeat];
                backQads.materials = backMats;

                var colorQads = playerIngs[j].ColorQuad.GetComponent<MeshRenderer>();
                var colorMats = colorQads.materials;
                colorMats[0] = allColorMaterials[i];
                colorQads.materials = colorMats;
            }
        }
    }

    private void GetNewGameCallback(string data)
    {
        GameId = sql.jsonConvert<int>(data);
        TakeTurn();
    }  
    private void GetShouldTrashCallback(string data)
    {
        ShouldTrash = sql.jsonConvert<bool?>(data);
    }

    private void GameIsAliveCallback(string data)
    {
        var GameAlive = sql.jsonConvert<bool>(data);
        if (!GameAlive)
        {
            GameIsOver(playerList.FirstOrDefault(x=>x.Username == Settings.LoggedInPlayer.Username));
        }
    }
    private void GetOnlineGameCallback(string data)
    {
        var GameState = sql.jsonConvert<GameState>(data);
        GameState.Player1.IsGuest = false;
        GameState.Player2.IsGuest = false; 
        GameState.Player1.playerType = PlayerTypes.HUMAN;
        GameState.Player2.playerType = PlayerTypes.HUMAN;
        playerList[0] = Settings.LoggedInPlayer;
        Settings.SecondPlayer = GameState.Player2.UserId == Settings.LoggedInPlayer.UserId ? GameState.Player1 : GameState.Player2;
        playerList[1] = Settings.SecondPlayer;
        if (GameState.Player1.UserId == Settings.LoggedInPlayer.UserId)
        {
            activePlayer = GameState.IsPlayer1Turn ? 0 : 1;
        }
        else
        {
            activePlayer = GameState.IsPlayer1Turn ? 1 : 0;
        }
        SetSkins();
        TakeTurn();
    }
   
    private void CheckForRollsCallback(string data)
    {
        var state = sql.jsonConvert<GameRoll>(data);
        if (state != null)
        {
            OnlineDiceFound = true;
            LookingForTurn = true;
            StartCoroutine(SetRollState(state.roll1, state.roll2));
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
                CpuLogic.i.IngredientMovedWithHigher = ingToMove;
            else
                CpuLogic.i.IngredientMovedWithLower = ingToMove;
            StartCoroutine(CpuLogic.i.MoveCPUIngredient(ingToMove));
        }

    }
    internal void SwitchPlayer()
    {
        State = States.Switching;
        TurnNumber++;
        Player1Turn = null;
        BlockPlayerActionPanel.SetActive(false);
        Timer1.gameObject.SetActive(false);
        Timer2.gameObject.SetActive(false);
        HigherRollImage1.interactable = false;
        LowerRollImage1.interactable = false;
        HigherRollButton1.interactable = false;
        LowerRollButton1.interactable = false;
        HigherRollImage2.interactable = false;
        LowerRollImage2.interactable = false;
        TurnBorder1.SetActive(false);
        TurnBorder2.SetActive(false);
        OnlineDiceFound = null;
        firstIngredientMoved = null;
        firstMoveTaken = false;
        activePlayer = (activePlayer + 1) % playerList.Length;
        TakeTurn();
    }
    private void StartReading()
    {
        IsReading = true;
        readingTimeStart = Time.time;
        HelpCanvas.SetActive(true);
    }
    private void TakeTurn()
    {
        SetIngredientPositions();
        turnTime = rollDuration;
        State = States.Rolling;
        if (activePlayer == 0)
        {
            HigherRollText1.text = "";
            LowerRollText1.text = "";
            RollButton1.SetActive(true);
            TurnBorder1.SetActive(true);
            HigherRollImage1.interactable = true;
            LowerRollImage1.interactable = true;
            Player1Turn = true;
        }
        else
        {
            HigherRollText2.text = "";
            LowerRollText2.text = "";
            TurnBorder2.SetActive(true);
            HigherRollImage2.interactable = true;
            LowerRollImage2.interactable = true;
            Player1Turn = false;
        }
        UpdateDiceSkin(activePlayer == 0 ? HigherRollImage1 : HigherRollImage2,
            activePlayer == 0 ? LowerRollImage1 : LowerRollImage2,
            GetActivePlayer().Username);
        if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
        {
            OnlineDiceFound = false;
        }
        if (IsCPUTurn()) //Take turn for CPU
        {
            RollDice(false);
        }
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
            SetDice();
            yield return StartCoroutine(SetSelectableIngredients());
        }
    }
    private void SetDice()
    {
        if (activePlayer == 0)
        {
            if (!firstMoveTaken && lowerMove != 0)
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
            if (!firstMoveTaken && lowerMove != 0)
            {
                if (higherMoveSelected)
                {
                    HigherRollImage2.interactable = false;
                    LowerRollImage2.interactable = true;
                }
                else
                {
                    LowerRollImage2.interactable = false;
                    HigherRollImage2.interactable = true;
                }
            }
            else
            {
                HigherRollImage2.interactable = false;
                LowerRollImage2.interactable = false;
            }
        }
    }
    private IEnumerator SetSelectableIngredients()
    {
        if (ZerosRolled())
            MoveableList = new List<Ingredient>();
        else if (DoublesRolled()) 
            MoveableList = AllIngredients.Where(x=> x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x).ToList();
        else if (higherMoveSelected)
            MoveableList = AllIngredients.Where(x => x.Team == activePlayer && x.IngredientId != firstIngredientMoved && (x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x)).ToList();
        else
            MoveableList = AllIngredients.Where(x => x.IngredientId != firstIngredientMoved && (x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x)).ToList();

        for (int i = 0; i < MoveableList.Count(); i++)
        {
            MoveableList[i].SetSelector(true);
        }
        
        if (MoveableList.Count() == 0)
        {
            yield return new WaitForSeconds(2f);
            SwitchPlayer();
        }

        if (IsCPUTurn())
            yield return new WaitForSeconds(.5f);
    }
    private void GameIsOver(Player rageQuit = null)
    {
        GameOver = true;
        var player1Count = (rageQuit == null || rageQuit.Username == playerList[0].Username) ? AllIngredients.Count(x => x.Team == 0 && x.isCooked) : 0;
        var player2Count = (rageQuit == null || rageQuit.Username == playerList[1].Username) ? AllIngredients.Count(x => x.Team == 1 && x.isCooked) : 0;
        if (!Settings.IsDebug)
        {
            var url = $"analytic/GameEnd?GameId={GameId}&Player1Cooked={player1Count}&Player2Cooked={player2Count}&TotalTurns={TurnNumber}&HardMode={Settings.HardMode}&RageQuit={rageQuit != null}";
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
            playerWhoWon = playerList.FirstOrDefault(x => AllIngredients.Where((y,i)=> y.Team == i).All(y => y.isCooked));
        }
        var playerwhoLost = playerList.FirstOrDefault(x => x.UserId != playerWhoWon.UserId);
        var lostCount = rageQuit == null ? player1Count > player2Count ? player2Count : player1Count : 0;
        eventText.text = "GAME OVER! \n \n" ;
        if (rageQuit == null)
        {
            eventText.text += playerWhoWon.Username + " won. \n \n";
        }
        else
        {
            eventText.text += playerwhoLost.Username + " quit. \n \n";
        }
        var wonXp = BonusXP + 300 + ((3 - lostCount) * 50);
        var lostXp = BonusXP + 150 + (lostCount * 50);
        if (playerList.Any(x => x.playerType == PlayerTypes.CPU) && playerList.Any(x => x.playerType == PlayerTypes.HUMAN))
        {
            if (playerWhoWon.playerType == PlayerTypes.HUMAN)
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
                eventText.text += "You each gained 50 Calories for each of your cooked ingredients! \n" + playerWhoWon.Username + " earned " + wonXp + " XP \n " + playerwhoLost.Username + " earned " + lostXp + " xp!";
            }
            else
            {
                eventText.text += "You gained 50 Calories for each of your cooked ingredients and they gained nothing! \n" + playerWhoWon.Username + " earned " + wonXp + " XP \n ";
            }
        }

        if (Settings.LoggedInPlayer.WineMenu)
            eventText.text += "\n \n" + (playerWhoWon.UserId == Settings.LoggedInPlayer.UserId ? " Purple" : " Yellow") + " Team finish your drinks!";

        if (playerWhoWon.Username == Settings.LoggedInPlayer.Username)
            Settings.LoggedInPlayer.Wins++;

        Settings.HardMode = false;
        EventCanvas.SetActive(true);
    }
    #endregion

    #region Used by ingredient
    internal bool IsCPUTurn()
    {
        return GetActivePlayer().playerType == PlayerTypes.CPU;
    }
    internal Player GetActivePlayer()
    {
        return playerList[activePlayer];
    }

    internal IEnumerator DoneMoving()
    {
        while (IsReading)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (playerList.Where((x,i) => AllIngredients.Where(y => y.Team == i).All(y => y.isCooked)).Count() > 0)
        {
            GameIsOver();
        }
        else if (firstMoveTaken || lowerMove == 0)
        {
            SwitchPlayer();
        }
        else
        {
            turnTime = Mathf.Min(turnTime + 10, turnDuration);
            firstMoveTaken = true;

            if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
                LookingForTurn = true;

            if (!IsCPUTurn())
            {
                //todo add undo to multiplayer
                //if (Settings.OnlineGameId == 0)
                //{
                //    if (activePlayer == 0)
                //    {
                //        undoButton1.gameObject.SetActive(true);
                //    }
                //    else
                //    {
                //        undoButton2.gameObject.SetActive(true);
                //    }
                //}
                yield return RollSelected(!higherMoveSelected, !IsCPUTurn());
            }
        }
    }
    internal IEnumerator MoveToNextEmptySpace(Ingredient ingredientToMove, bool shouldPush = true)
    {
        ingredientToMove.routePosition = 0;
        var prepTile = prepTiles.FirstOrDefault(x => x.ingredients.Count() == 0);
        if (shouldPush)
            prepTile.ingredients.Push(ingredientToMove);
        yield return ingredientToMove.MoveToNextTile(prepTile.transform.position, shouldPush,45f);
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
        //actionText.text = moveAmount != null ? "Move: " + moveAmount : "";
    }
    internal IEnumerator AskShouldTrash()
    {
        if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId == Settings.LoggedInPlayer.UserId)
        {
            ShouldTrashPopup.SetActive(true);
        }
        
        while (ShouldTrash == null)
        {
            if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
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
            if (GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
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
        if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
        {
            return;
        }

        if (IsCPUTurn() && HUMAN)
        {
            return;
        }

        var roll1 = Random.Range(0, 10);
        var roll2 = Random.Range(0, 10);
        if (Settings.OnlineGameId != 0)
        {
            StartCoroutine(sql.RequestRoutine($"analytic/UpdateGameRoll?GameId={Settings.OnlineGameId}&roll1={roll1}&roll2={roll2}"));
        }

        StartCoroutine(SetRollState(roll1, roll2));
    }

    private IEnumerator SetRollState(int roll1, int roll2)
    {
        if (IsCPUTurn())
        {
            yield return new WaitForSeconds(.5f);
        }

        RollButton1.SetActive(false);
        higherMove = roll1 > roll2 ? roll1 : roll2;
        lowerMove = roll1 > roll2 ? roll2 : roll1;

        ResetDice();
        State = States.Moving;
        turnTime = turnDuration;

        if (lowerMove == 0)
        {
            turnTime = Mathf.Min(turnTime + 10, turnDuration);
            firstMoveTaken = true;
        }
        if (ZerosRolled())
        {
            yield return new WaitForSeconds(2f);
            SwitchPlayer();
        }
        else if(IsCPUTurn())
        {
            yield return StartCoroutine(CpuLogic.i.FindCPUIngredientMoves());
        }
    }

    private void ResetDice(bool undo = false)
    {
        if (activePlayer == 0)
        {
            HigherRollText1.text = higherMove.ToString();
            LowerRollText1.text = lowerMove.ToString();
            if (higherMove == 0)
            {
                HigherRollButton1.interactable = false;
                HigherRollImage1.interactable = false;
            }
            else
            {
                HigherRollButton1.interactable = true;
            }
            if (lowerMove == 0)
            {
                LowerRollButton1.interactable = false;
                LowerRollImage1.interactable = false;
            }
            else
            {
                LowerRollButton1.interactable = true;
            }
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
            if (higherMove == 0)
            {
                HigherRollImage2.interactable = false;
            }
            else
            {
                HigherRollImage2.interactable = true;
            }
            if (lowerMove == 0)
            {
                LowerRollImage2.interactable = false;
            }
            else
            {
                LowerRollImage2.interactable = true;
            }
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
        else if (Username == playerList[0].Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = playerList[0].SelectedDie != 0 ? allD10s[playerList[0].SelectedDie] : yellowDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = playerList[0].SelectedDie2 != 0 ? allD10s[playerList[0].SelectedDie2] : yellowDie;
        }
        else if (Username == playerList[1].Username)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = playerList[1].SelectedDie != 0 ? allD10s[playerList[1].SelectedDie] : purpleDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = playerList[1].SelectedDie2 != 0 ? allD10s[playerList[1].SelectedDie2] : purpleDie;
        }
    }

    public void RollSelected(bool isHigher)
    {
        if (Settings.OnlineGameId != 0 && GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
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
            //undoButton1.gameObject.SetActive(false);
            //undoButton2.gameObject.SetActive(false);
            firstIngredientMoved = null;
            firstMoveTaken = false;
            ResetDice(true);
            AllIngredients.ForEach(x => x.SetSelector(false));
            for (var i = 0; i < TurnStartPositions.Count(); i++)
            {
                if (TurnStartPositions[i].ingPos != AllIngredients[i].routePosition || TurnStartPositions[i].ingCooked != AllIngredients[i].isCooked)
                {
                    StartCoroutine(RollbackIngredient(AllIngredients[i], TurnStartPositions[i]));
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
            if (ingredientToRollback.routePosition != 0)
            {
                Route.i.FullRoute[ingredientToRollback.routePosition].ingredients.Pop();
            }
            else
            {
                prepTiles.FirstOrDefault(x => x.ingredients.Any(y => y.IngredientId == ingredientToRollback.IngredientId)).ingredients.Pop();
            }
            ingredientToRollback.routePosition = turnPosition.ingPos;
            if (turnPosition.ingPos == 0)
            {
                yield return StartCoroutine(MoveToNextEmptySpace(ingredientToRollback));
            }
            else
            {
                yield return StartCoroutine(ingredientToRollback.MoveToNextTile(Route.i.FullRoute[turnPosition.ingPos].transform.position));
                Route.i.FullRoute[turnPosition.ingPos].ingredients.Push(ingredientToRollback);
            }
        }
    }

    internal bool DoublesRolled()
    {
        return higherMove == lowerMove;
    }  
    internal bool ZerosRolled()
    {
        return lowerMove == 0 && higherMove == 0;
    }
    #endregion
}
