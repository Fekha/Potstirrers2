using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region GameManger Variables
    private int higherMove = 0;
    private int lowerMove = 0;
    private bool higherMoveSelected;
    private List<Ingredient> AllIngredients;
    private List<Ingredient> TeamIngredients;
    private List<Ingredient> EnemyIngredients;
    private Ingredient lastMovedIngredient;
    private Ingredient IngredientMovedWithLower;
    private Ingredient IngredientMovedWithHigher;
    private SqlController sql;
    private GamePlayer playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    private int activePlayer;
    private bool hasBeenDumb = false;
    private bool GameOver = false;
    private bool DoublesRolled = false;
    private bool DoubleDoubles = false;
    #endregion

    #region Ingredient Variables
    internal static GameManager instance;
    internal bool IsReading = false;
    internal int Steps;
    internal bool firstMoveTaken;
    internal bool? ShouldTrash = null;
    internal bool isMoving = false;
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
    public List<Tile> tiles = new List<Tile>();

    [Header("GameObject")]
    public GameObject EventCanvas;
    public GameObject ShouldTrashPopup;
    public GameObject HelpCanvas;
    public GameObject RollButton;
    public GameObject RolledPanel;
    public GameObject TrashCan2;
    public GameObject TrashCan3;
    public GameObject exitPanel;
    public GameObject undoPanel;
    public GameObject FullBoard;

    [Header("Button")]
    public Button HigherRoll;
    public Button LowerRoll;   
    public Button HigherRollButton;
    public Button LowerRollButton;
    public Button undoButton;

    [Header("Text")]
    public Text higherRollText;
    public Text lowerRollText;
    public Text eventText;
    public Text helpText;
    public Text actionText;
    public Text turnText;
    public Text doublesText;

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
    private void Start()
    {
        //if (Settings.LoggedInPlayer.Experimental)
        //{
        //    var boardQuad = FullBoard.GetComponent<MeshRenderer>();
        //    var boardMats = boardQuad.materials;
        //    boardMats[0] = AdvancedBoard;
        //    boardQuad.materials = boardMats;
        //}

        if (!Settings.LoggedInPlayer.IsGuest)
        {
            AllIngredients = playerList.SelectMany(y => y.myIngredients).ToList();
            var HumanIngredients = AllIngredients.Where(x => x.TeamYellow != Settings.LoggedInPlayer.PlayAsPurple).ToList();

            var meatQuads = HumanIngredients[0].NormalQuad.GetComponent<MeshRenderer>();
            var meatMats = meatQuads.materials;
            meatMats[0] = allMeatMaterials[Settings.LoggedInPlayer.SelectedMeat];
            meatQuads.materials = meatMats;

            var veggieQuads = HumanIngredients[1].NormalQuad.GetComponent<MeshRenderer>();
            var veggieMats = veggieQuads.materials;
            veggieMats[0] = allVeggieMaterials[Settings.LoggedInPlayer.SelectedVeggie];
            veggieQuads.materials = veggieMats;

            var fruitQuads = HumanIngredients[2].NormalQuad.GetComponent<MeshRenderer>();
            var fruitMats = fruitQuads.materials;
            fruitMats[0] = allFruitMaterials[Settings.LoggedInPlayer.SelectedFruit];
            fruitQuads.materials = fruitMats;

            if (Settings.SecondPlayer.IsGuest)
            {
                var CPUIngredients = AllIngredients.Where(x => x.TeamYellow == Settings.LoggedInPlayer.PlayAsPurple).ToList();

                allMeatMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedMeat);
                var randMeat = Random.Range(0, allMeatMaterials.Count());
                allVeggieMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedVeggie);
                var randVeggie = Random.Range(0, allVeggieMaterials.Count());
                allFruitMaterials.RemoveAt(Settings.LoggedInPlayer.SelectedFruit);
                var randFruit = Random.Range(0, allFruitMaterials.Count());

                var CPUmeatQuads = CPUIngredients[0].NormalQuad.GetComponent<MeshRenderer>();
                var CPUmeatMats = CPUmeatQuads.materials;
                CPUmeatMats[0] = allMeatMaterials[randMeat];
                CPUmeatQuads.materials = CPUmeatMats;

                var CPUveggieQuads = CPUIngredients[1].NormalQuad.GetComponent<MeshRenderer>();
                var CPUveggieMats = CPUveggieQuads.materials;
                CPUveggieMats[0] = allVeggieMaterials[randVeggie];
                CPUveggieQuads.materials = CPUveggieMats;

                var CPUfruitQuads = CPUIngredients[2].NormalQuad.GetComponent<MeshRenderer>();
                var CPUfruitMats = CPUfruitQuads.materials;
                CPUfruitMats[0] = allFruitMaterials[randFruit];
                CPUfruitQuads.materials = CPUfruitMats;
            }
            else
            {
                AllIngredients = playerList.SelectMany(y => y.myIngredients).ToList();
                var Human2Ingredients = AllIngredients.Where(x => x.TeamYellow == Settings.LoggedInPlayer.PlayAsPurple).ToList();

                var meat2Quads = Human2Ingredients[0].NormalQuad.GetComponent<MeshRenderer>();
                var meat2Mats = meat2Quads.materials;
                meat2Mats[0] = allMeatMaterials[Settings.SecondPlayer.SelectedMeat];
                meat2Quads.materials = meat2Mats;

                var veggie2Quads = Human2Ingredients[1].NormalQuad.GetComponent<MeshRenderer>();
                var veggie2Mats = veggie2Quads.materials;
                veggie2Mats[0] = allVeggieMaterials[Settings.SecondPlayer.SelectedVeggie];
                veggie2Quads.materials = veggie2Mats;

                var fruit2Quads = Human2Ingredients[2].NormalQuad.GetComponent<MeshRenderer>();
                var fruit2Mats = fruit2Quads.materials;
                fruit2Mats[0] = allFruitMaterials[Settings.SecondPlayer.SelectedFruit];
                fruit2Quads.materials = fruit2Mats;
            }
        }
        StartCoroutine(TakeTurn());
    }
    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 30;
#if UNITY_EDITOR
        Settings.IsDebug = true;
        Settings.LoggedInPlayer.Experimental = true;
#endif
        sql = new SqlController();
        activePlayer = Random.Range(0, 2);
        
        if ((Settings.LoggedInPlayer.Wins == 0 || Settings.LoggedInPlayer.Experimental) && !Settings.IsDebug)
        {
            getHelp();
        }
        
        if (Settings.LoggedInPlayer.PlayAsPurple)
        {
            playerList[0].player = Settings.PlayingPlayers[1];
            playerList[1].player = Settings.PlayingPlayers[0];
        }
        else
        {
            playerList[0].player = Settings.PlayingPlayers[0];
            playerList[1].player = Settings.PlayingPlayers[1];
        }

        playerList[0].TeamYellow = true;
        playerList[1].TeamYellow = false;
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
    }
    private void SwitchPlayer()
    {
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
        turnText.text = GetActivePlayer().player.Username + "'s Turn";
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
        StartCoroutine(DeactivateAllSelectors());
        RollButton.SetActive(true);
        if (IsCPUTurn()) //Take turn for CPU
        {
            yield return StartCoroutine(CPUTurn());
        }
    }
    private IEnumerator RollSelected(bool isHigher, bool HUMAN)
    {
        if (isMoving || (IsCPUTurn() && HUMAN))
        {
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            if (DoubleDoubles && Settings.LoggedInPlayer.DisableDoubles)
            {
                yield return StartCoroutine(DoneMoving());
            }
            else
            {
                higherMoveSelected = isHigher;
                Steps = higherMoveSelected ? higherMove : lowerMove;
                UpdateMoveText(Steps);
                if (!firstMoveTaken)
                {
                    if (higherMoveSelected)
                    {
                        HigherRoll.interactable = false;
                        LowerRoll.interactable = true;
                        HigherRollButton.interactable = false;
                        LowerRollButton.interactable = true;
                    }
                    else
                    {
                        LowerRoll.interactable = false;
                        HigherRoll.interactable = true;
                        LowerRollButton.interactable = false;
                        HigherRollButton.interactable = true;
                    }
                }
                else
                {
                    HigherRoll.interactable = false;
                    LowerRoll.interactable = false;
                    HigherRollButton.interactable = false;
                    LowerRollButton.interactable = false;
                }
                yield return StartCoroutine(SetSelectableIngredients());
            }
        }
    }
    private IEnumerator SetSelectableIngredients(List<Ingredient> moveableList = null)
    {
        yield return StartCoroutine(DeactivateAllSelectors());

        if (moveableList == null)
        {
            if (higherMoveSelected)
                moveableList = GetActivePlayer().myIngredients.Where(x => x != lastMovedIngredient).ToList();
            else
                moveableList = playerList.SelectMany(y => y.myIngredients.Where(x => x != lastMovedIngredient)).ToList();
        }

        for (int i = 0; i < moveableList.Count; i++)
        {
            moveableList[i].SetSelector(true);
        }

        if (IsCPUTurn())
            yield return new WaitForSeconds(.5f);

        yield return new WaitForSeconds(0.1f);
    }
    private void DoneReset()
    {
        undoButton.interactable = false;
        hasBeenDumb = false;
        doublesText.text = "";
        firstMoveTaken = false;
        lastMovedIngredient = null;
        RolledPanel.SetActive(false);
        IngredientMovedWithLower = null;
        IngredientMovedWithHigher = null;
    }
    private void GameIsOver(bool debug = false)
    {
        GameOver = true;
        playerWhoWon = playerList.FirstOrDefault(x => x.myIngredients.All(y => y.isCooked));
        if (debug)
        {
            playerWhoWon = playerList.FirstOrDefault(x => !x.TeamYellow);
        }
        var playerwhoLost = playerList.FirstOrDefault(x => x.player.Username != playerWhoWon.player.Username);
        var lostCount = playerwhoLost.myIngredients.Count(x => x.isCooked);
        eventText.text = @"GAME OVER! " + playerWhoWon.player.Username + " won.";
        var wasVsCPU = false;
        var wonXp = 300 + ((3 - lostCount) * 50);
        var lostXp = 150 + (lostCount * 50);
        if (playerList.Any(x => x.player.playerType == PlayerTypes.CPU) && playerList.Any(x => x.player.playerType == PlayerTypes.HUMAN))
        {
            wasVsCPU = true;
            if (playerWhoWon.player.playerType == PlayerTypes.HUMAN)
            {
                eventText.text += @"
You gained 150 Stars and " + wonXp + " Xp!";
            }
            else
            {
                eventText.text += @"
You gained " + lostCount * 50 + " Stars and " + lostXp + " Xp!";
            }
        }
        else
        {
            eventText.text += @"
You each gained 50 Stars for each of your cooked ingredients! " + playerWhoWon.player.Username + " gained " + wonXp + " XP and " + playerwhoLost.player.Username + " gained " + lostXp + " xp!";
        }

        foreach (var gamePlayer in playerList.Where(x => x.player.playerType == PlayerTypes.HUMAN && !x.player.IsGuest))
        {
            var cookedIngs = gamePlayer.myIngredients.Count(x => x.isCooked);
            var starsToEarn = cookedIngs * 50;
            gamePlayer.player.Stars += starsToEarn;
            gamePlayer.player.Cooked += cookedIngs;
            StartCoroutine(sql.RequestRoutine("player/UpdateStars?userId=" + gamePlayer.player.UserId + "&stars=" + starsToEarn));
            StartCoroutine(sql.RequestRoutine("player/UpdateCooked?userId=" + gamePlayer.player.UserId + "&cooked=" + cookedIngs));
            if (playerWhoWon.player.Username == gamePlayer.player.Username)
            {
                gamePlayer.player.Wins += 1;
                StartCoroutine(sql.RequestRoutine($"player/UpdateWins?userId={gamePlayer.player.UserId}&cpu={wasVsCPU}&lostCount={lostCount}"));
            }
            else
            {
                StartCoroutine(sql.RequestRoutine($"player/LoserXP?userId={gamePlayer.player.UserId}&lostCount={lostCount}"));
            }
        }

        if (Settings.LoggedInPlayer.WineMenu)
            eventText.text += (playerWhoWon.TeamYellow ? "Purple" : "Yellow") + " Team finish your drinks!";


        UpdateMoveText(Steps);
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
        while (IsReading)
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (DoubleDoubles && Settings.LoggedInPlayer.DisableDoubles)
        {
            if (IsCPUTurn())
            {
                yield return new WaitForSeconds(2f);
            }
            DoublesRolled = false;
            DoubleDoubles = false;
            DoneReset();
            SwitchPlayer();
        }
        else
        {
            if (playerList.Any(x => x.myIngredients.All(y => y.isCooked)))
            {
                GameIsOver();
            }
            else if (firstMoveTaken == true)
            {
                DoneReset();
                HigherRoll.interactable = false;
                LowerRoll.interactable = false;
                HigherRollButton.interactable = false;
                LowerRollButton.interactable = false;
                if (higherMove == lowerMove)
                {
                    StartCoroutine(TakeTurn());
                }
                else
                {
                    DoublesRolled = false;
                    SwitchPlayer();
                }
            }
            else
            {
                firstMoveTaken = true;
                if (!IsCPUTurn())
                {
                    undoButton.interactable = true;
                    yield return RollSelected(!higherMoveSelected, !IsCPUTurn());
                }

            }
        }
        yield return new WaitForSeconds(0.1f);
    }
    internal void setTileNull(string ingName)
    {
        var tileToNull = tiles.FirstOrDefault(x => x.ingredient?.name == ingName);
        if (tileToNull != null)
            tileToNull.ingredient = null;
        else
        {
            //this should never happen! But sometimes does....
        }
    }
    internal IEnumerator MoveToNextEmptySpace(Ingredient ingredientToMove)
    {
        ingredientToMove.routePosition = 0;
        ingredientToMove.currentTile = ingredientToMove.fullRoute[0];
        Tile nextTile;
        if (tiles.Any(x => x.ingredient?.name == ingredientToMove.name))
        { //happens when scoring
            nextTile = tiles.FirstOrDefault(x => x.ingredient?.name == ingredientToMove.name);
        }
        else
        {
            nextTile = tiles.FirstOrDefault(x => x.ingredient == null);
            nextTile.ingredient = ingredientToMove;
        }

        yield return ingredientToMove.MoveToNextTile(nextTile.transform.position, 10f);
    }
    internal void FirstScoreHelp()
    {
        if (Settings.LoggedInPlayer.Experimental)
        {
            pageNum = Library.helpTextList.Count() - 1;
            helpText.text = "An Ingredient was cooked! \n \n Remember: Cooked ingredients can NOT be cooked again. \n \n Cooked ingredients can still be used to send ingredients back to prep and are always skipped over while moving to give you a further move distance!";
            StartReading();
        }
    }
    internal void setWineMenuText(bool teamYellow, int v)
    {
        helpText.text = (teamYellow ? "Yellow" : "Purple") + " team drinks for " + v + (v == 1 ? " second" : " seconds") + ". \n \n (1 second for each ingredient in Prep)";
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
    internal void SetLastMovedIngredient(Ingredient ingredient)
    {
        lastMovedIngredient = ingredient;
    }
    internal IEnumerator AskShouldTrash()
    {
        ShouldTrashPopup.SetActive(true);
        while (ShouldTrash == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return ShouldTrash;

    }
    #endregion

    #region Button Clicks
    public void GameOverClicked()
    {
        Settings.IsDebug = false;
        SceneManager.LoadScene("MainMenu");
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

        RollButton.SetActive(false);

        var roll1 = 0;
        var roll2 = 0;
        
        while (roll1 == 0 && roll2 == 0)
        {
            if (Settings.LoggedInPlayer.UseD8s)
            {
                roll1 = Random.Range(1, 9);
                roll2 = Random.Range(1, 9);
            }
            else
            {
                roll1 = Random.Range(0, 10);
                roll2 = Random.Range(0, 10);
            }
        }

        higherMove = roll1 > roll2 ? roll1 : roll2;
        lowerMove = roll1 > roll2 ? roll2 : roll1;


        if (GetActivePlayer().player.Username == Settings.LoggedInPlayer.Username && !Settings.LoggedInPlayer.UseD8s )
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.SelectedDie != 0 ? allD10s[Settings.LoggedInPlayer.SelectedDie] : GetActivePlayer().TeamYellow ? yellowDie : purpleDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.SelectedDie2 != 0 ? allD10s[Settings.LoggedInPlayer.SelectedDie2] : GetActivePlayer().TeamYellow ? yellowDie : purpleDie;
        }
        else if (GetActivePlayer().player.Username == Settings.SecondPlayer.Username && !Settings.LoggedInPlayer.UseD8s)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.SecondPlayer.SelectedDie != 0 ? allD10s[Settings.SecondPlayer.SelectedDie] : GetActivePlayer().TeamYellow ? yellowDie : purpleDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.SecondPlayer.SelectedDie2 != 0 ? allD10s[Settings.SecondPlayer.SelectedDie2] : GetActivePlayer().TeamYellow ? yellowDie : purpleDie;
        }
        else if (GetActivePlayer().TeamYellow)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.UseD8s ? yellowD8 :  yellowDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.UseD8s ? yellowD8 : yellowDie;
        }
        else if (!GetActivePlayer().TeamYellow)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.UseD8s ? purpleD8 : purpleDie;
            LowerRoll.gameObject.GetComponent<Image>().sprite = Settings.LoggedInPlayer.UseD8s ? purpleD8 : purpleDie;
        }

        higherRollText.color = Color.black;
        lowerRollText.color = Color.black;

        HigherRoll.interactable = true;
        HigherRollButton.interactable = true;
        higherRollText.text = higherMove.ToString();
        lowerRollText.text = lowerMove.ToString();

        if (lowerMove == 0)
        {
            LowerRoll.interactable = false;
            LowerRollButton.interactable = false;
            firstMoveTaken = true;
        }
        else
        {
            LowerRoll.interactable = true;
            LowerRollButton.interactable = true;
        }
        if (higherMove == lowerMove)
        {
            if (DoublesRolled && Settings.LoggedInPlayer.DisableDoubles)
            {
                DoublesRolled = false;
                DoubleDoubles = true;
                doublesText.text = "Oh no! You rolled doubles AGAIN so you lost your extra turn!";
            }
            else
            {
                DoublesRolled = true;
                doublesText.text = "You rolled doubles so you get an extra turn!" + (!Settings.LoggedInPlayer.DisableDoubles ? "" : " Unless you roll doubles again...");
            }
        }
        if (DoubleDoubles && Settings.LoggedInPlayer.DisableDoubles)
        {
            actionText.text = "Lost your turn :(";
            if (IsCPUTurn()) {
                StartCoroutine(DoneMoving());
            }
        }
        else
        {
            actionText.text = "Select a move";
            if (IsCPUTurn())
            {
                StartCoroutine(MoveCPUIngredient());
            }
        }
        RolledPanel.SetActive(true);
    }
    public void RollSelected(bool isHigher)
    {
        StartCoroutine(RollSelected(isHigher, true));
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
    #endregion

    #region CPU Methods
    private IEnumerator CPUTurn()
    {
        yield return new WaitForSeconds(.5f);
        RollButton.SetActive(false);
        RollDice(false);
    }

    public IEnumerator MoveCPUIngredient()
    {
        yield return new WaitForSeconds(.5f);

        if (!DoubleDoubles || !Settings.LoggedInPlayer.DisableDoubles)
        {
            if (IsCPUTurn())
                yield return StartCoroutine(CheckForScore());

            if (lowerMove != 0 && IngredientMovedWithLower == null && IsCPUTurn() && (Settings.IsDebug || Settings.LoggedInPlayer.Wins > 0))
                yield return StartCoroutine(MoveWithLowerFirst());

            if (IngredientMovedWithLower != null)
            {
                if (!GameOver && IngredientMovedWithHigher == null && IsCPUTurn())
                    yield return StartCoroutine(GetBestIngredientToMoveWithHigher());
            }
            else
            {
                if (!GameOver && IngredientMovedWithHigher == null && IsCPUTurn())
                    yield return StartCoroutine(GetBestIngredientToMoveWithHigher());

                if (lowerMove != 0 && !GameOver && IngredientMovedWithLower == null && IsCPUTurn())
                    yield return StartCoroutine(GetBestIngredientToMoveWithLower());
            }
        }
    }
    
    private IEnumerator SetCPUVariables(bool higher)
    {
        AllIngredients = playerList.SelectMany(y => y.myIngredients.Where(x => x != lastMovedIngredient)).OrderBy(x=>x.isCooked).ThenByDescending(x => x.routePosition).ToList();
        foreach (var ing in AllIngredients)
        {
            ing.endHigherPosition = ing.routePosition+higherMove;
            ing.endLowerPosition = ing.routePosition+lowerMove;
            for (int i = ing.routePosition+1; i <= ing.endHigherPosition; i++)
            {
                if (ing.fullRoute[i%26].ingredient != null && ing.fullRoute[i % 26].ingredient.isCooked) {
                    ing.endHigherPosition++;
                }
            }
            for (int i = ing.routePosition+1; i <= ing.endLowerPosition; i++)
            {
                if (ing.fullRoute[i%26].ingredient != null && ing.fullRoute[i % 26].ingredient.isCooked) {
                    ing.endLowerPosition++;
                }
            }
            if (higher)
            {
                ing.endPosition = ing.endHigherPosition;
            }
            else
            {
                ing.endPosition = ing.endLowerPosition;
            }
        }
        TeamIngredients = AllIngredients.Where(x => x.TeamYellow == GetActivePlayer().TeamYellow).OrderByDescending(x => x.routePosition).ToList();
        EnemyIngredients = AllIngredients.Where(x => x.TeamYellow != GetActivePlayer().TeamYellow).OrderByDescending(x => x.routePosition).ToList();
        while (IsReading)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
    private IEnumerator GetBestIngredientToMoveWithHigher()
    {
        yield return RollSelected(true, false);
        yield return StartCoroutine(SetCPUVariables(true));
        yield return StartCoroutine(MoveCPUIngredient(CookIngredient(TeamIngredients)
            ?? BeDumb(TeamIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? SlideOnThermometor(TeamIngredients)
            ?? MoveFrontMostIngredient(TeamIngredients.Where(x => !x.isCooked).ToList())
            ?? MoveFrontMostIngredient(TeamIngredients)
            ?? MoveNotPastPrep(TeamIngredients)
            ?? MoveCookedIngredient(TeamIngredients)
            ?? MoveRandomly(TeamIngredients)));
    }

    private IEnumerator GetBestIngredientToMoveWithLower()
    {
        yield return RollSelected(false, false);
        yield return StartCoroutine(SetCPUVariables(false));
        yield return StartCoroutine(MoveCPUIngredient(CookIngredient(TeamIngredients)
            ?? BeDumb(AllIngredients)
            ?? MovePastPrep(EnemyIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? StompSafeZone(EnemyIngredients)
            ?? GoToTrash(EnemyIngredients)
            ?? StompEnemy(EnemyIngredients)
            ?? SlideOnThermometor(TeamIngredients)
            ?? MoveIntoScoring(TeamIngredients)
            ?? MoveFrontMostIngredient(TeamIngredients.Where(x=>!x.isCooked).ToList())
            ?? MoveFrontMostIngredient(TeamIngredients)
            ?? MoveCookedIngredient(TeamIngredients)
            ?? MoveNotPastPrep(TeamIngredients)
            ?? MoveRandomly(EnemyIngredients)
            ?? MoveRandomly(TeamIngredients)));
    }
    private IEnumerator MoveWithLowerFirst()
    {
        Steps = lowerMove;
        yield return StartCoroutine(SetCPUVariables(false));
        var ing = CookIngredient(TeamIngredients)
            ?? MovePastPrep(EnemyIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? StompSafeZone(EnemyIngredients)
            ?? GoToTrash(EnemyIngredients)
            ?? StompEnemy(EnemyIngredients)
            ?? SlideOnThermometor(TeamIngredients)
            ?? MoveIntoScoring(TeamIngredients);
        IngredientMovedWithLower = ing;
        if (ing != null)
        {
            yield return RollSelected(false, false);
            yield return StartCoroutine(MoveCPUIngredient(IngredientMovedWithLower));
        }
    } 
    private IEnumerator CheckForScore()
    {
        Steps = lowerMove;
        yield return StartCoroutine(SetCPUVariables(false));
        var ing = CookIngredient(TeamIngredients) ?? HelpScore(AllIngredients,false);
        IngredientMovedWithLower = ing;
        if (ing != null)
        {
            yield return RollSelected(false, false);
            yield return StartCoroutine(MoveCPUIngredient(IngredientMovedWithLower));
        }
        else
        {
            Steps = higherMove;
            yield return StartCoroutine(SetCPUVariables(true));
            var ing2 = CookIngredient(TeamIngredients) ?? HelpScore(TeamIngredients, true);
            IngredientMovedWithHigher = ing2;
            if (ing2 != null)
            {
                yield return RollSelected(true, false);
                yield return StartCoroutine(MoveCPUIngredient(IngredientMovedWithHigher));
            }
        }
    }
    private IEnumerator MoveCPUIngredient(Ingredient ingredientToMove)
    {
        yield return new WaitForSeconds(.5f);
        yield return StartCoroutine(DeactivateAllSelectors());
        yield return StartCoroutine(ingredientToMove.Move());
    }

    private Ingredient MoveRandomly(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0 || teamToMove == null) return null;
        return teamToMove[Random.Range(0, teamToMove.Count())];
    }
    
    private Ingredient MoveNotPastPrep(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 26
        && !x.fullRoute[x.endPosition % 26].hasSpatula
        && !x.fullRoute[x.endPosition % 26].hasSpoon
        && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
    }
    
    private Ingredient MoveIntoScoring(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 23
        && x.endPosition > 16
        && !x.isCooked
        && x.routePosition < 17
        && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null)
        && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
    }

    private Ingredient MoveCookedIngredient(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0 || !Settings.LoggedInPlayer.Experimental) return null;
        return teamToMove.FirstOrDefault(x => x.isCooked
        && !x.fullRoute[x.endPosition % 26].hasSpatula
        && !x.fullRoute[x.endPosition % 26].hasSpoon
        && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
    }

    private Ingredient MoveFrontMostIngredient(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;

        var smartMoves = teamToMove.Where(x => x.endPosition < 23 //Dont move past prep
        && x.routePosition < 17 //Dont move from scoring position
        && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26) //Dont stomp yourself, unless cooked and advanced
        && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null) //Dont stomp on safe area
        && !x.fullRoute[x.endPosition % 26].hasSpatula //weve already checked if sliding was a good idea
        && !x.fullRoute[x.endPosition % 26].hasSpoon).ToList(); //weve already checked if sliding was a good idea

        if (smartMoves.Count() > 0)
        {
            return smartMoves.OrderByDescending(x=>x.routePosition).FirstOrDefault();
        }
        else
        {
            return null;
        }
    }

    private Ingredient SlideOnThermometor(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 26 //Dont move past preparation
        && x.routePosition < 17 //Dont move from scoring position
        && ((x.fullRoute[x.endPosition % 26].hasSpatula && (Steps < 4 || Steps > 7) && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == (x.endPosition - 6) % 26))
        || (x.fullRoute[x.endPosition % 26].hasSpoon && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == (x.endPosition + 6) % 26))));
    } 
    
    private Ingredient SlideOnEnemy(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 26 //Dont move past preparation
        && ((x.fullRoute[x.endPosition % 26].hasSpatula && EnemyIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == (x.endPosition - 6) % 26))
        || (x.fullRoute[x.endPosition % 26].hasSpoon && EnemyIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == (x.endPosition + 6) % 26))));

    }
    private Ingredient GoToTrash(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => (!Settings.LoggedInPlayer.Experimental || !x.isCooked) && (Steps < 6 && (x.endPosition % 26) == 10) || (x.endPosition % 26) == 18); //Check for trash cans
    }

    private Ingredient StompEnemy(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 26 //Dont move past preparation
        && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow ) // if not your piece then dont move from prep
        && EnemyIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26) //Stomp Enemy
        && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26)
        && !x.fullRoute[x.endPosition % 26].isSafe); //Dont stomp safe area if someone is there
    } 
    private Ingredient StompSafeZone(List<Ingredient> teamToMove)   
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => x.endPosition < 26 //Dont move past preparation
        && x.routePosition != 0 //dont move them from prep
        && AllIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26) //anyone there?
        && x.fullRoute[x.endPosition % 26].isSafe); //Stomp self if safe
    }

    private Ingredient CookIngredient(List<Ingredient> teamToMove)
    {
        return teamToMove.FirstOrDefault(x => x.endPosition == 26 && (!Settings.LoggedInPlayer.Experimental || !x.isCooked)); //Move into pot
    } 
    private Ingredient HelpScore(List<Ingredient> teamToMove, bool isHigher)
    {
        if (!Settings.LoggedInPlayer.Experimental) return null;
        Ingredient ScoreableIng = null;
        if (isHigher)
        {
            ScoreableIng = TeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 25);
            if (ScoreableIng != null)
            {
                var ing = teamToMove.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition < ScoreableIng.routePosition 
                && x.endPosition > ScoreableIng.routePosition
                && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null)
                && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
                if(ing != null)
                    return ing;
            }

            ScoreableIng = TeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 27);
            if (ScoreableIng != null)
            {
                var ing = teamToMove.FirstOrDefault(x => x != ScoreableIng && x.isCooked && x.routePosition > ScoreableIng.routePosition && x.endHigherPosition % 26 < ScoreableIng.routePosition
                && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null)
                && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
                if (ing != null)
                    return ing;
            }
        }
        else
        {
            ScoreableIng = TeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 25);
            if (ScoreableIng != null)
            {
                var ing = teamToMove.FirstOrDefault(x => x != ScoreableIng && x.isCooked && x.routePosition < ScoreableIng.routePosition && x.endLowerPosition > ScoreableIng.routePosition
                && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null)
                && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
                if (ing != null)
                    return ing;
            }

            ScoreableIng = TeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 27);
            if (ScoreableIng != null)
            {
                var ing = teamToMove.FirstOrDefault(x => x != ScoreableIng && x.isCooked && x.routePosition > ScoreableIng.routePosition && x.endLowerPosition % 26 < ScoreableIng.routePosition
                && !(x.fullRoute[x.endPosition % 26].isSafe && x.fullRoute[x.endPosition % 26].ingredient != null)
                && !TeamIngredients.Any(y => (!Settings.LoggedInPlayer.Experimental || !y.isCooked) && y.routePosition == x.endPosition % 26));
                if (ing != null)
                    return ing;
            }
        }
        return null;
    }

    private Ingredient MovePastPrep(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.OrderBy(x=>x.routePosition).FirstOrDefault(x => x.endPosition >= 26 && (!Settings.LoggedInPlayer.Experimental || !x.isCooked)); //Move past pot
    }
    private Ingredient BeDumb(List<Ingredient> teamToMove)
    {
        //var dumbRange = Settings.currentPlayer.Wins + GetActivePlayer().playerName == "Zach" ? 1 : 0;
        if (!Settings.IsDebug && (Settings.LoggedInPlayer.Wins == 0 || (Random.Range(0, Settings.LoggedInPlayer.Wins) == 0 && !hasBeenDumb))) {
            hasBeenDumb = true;
            var toMove = teamToMove[Random.Range(0, teamToMove.Count)];
            if (toMove.routePosition < 17)
            {
                return toMove;
            }
        }
        return null;
    }
    #endregion

    #region TODO
    //public void undoChoice(bool willPay)
    //{
    //    if (willPay)
    //    {
    //        undoButton.interactable = false;
    //        HigherRoll.interactable = true;
    //        LowerRoll.interactable = true;
    //        HigherRollButton.interactable = true;
    //        LowerRollButton.interactable = true;
    //        firstMoveTaken = false;

    //        StartCoroutine(DeactivateAllSelectors());
    //        StartCoroutine(RollbackIngredient(lastMovedIngredient));
    //        if(LastLandedOnIngredient != null)
    //            StartCoroutine(RollbackIngredient(LastLandedOnIngredient));

    //        lastMovedIngredient = null;
    //        LastLandedOnIngredient = null;
    //    }
    //    undoPanel.SetActive(false);
    //}

    //private IEnumerator RollbackIngredient(Ingredient ingredientToRollback)
    //{
    //    if (ingredientToRollback.routePosition == 0)
    //    {
    //        yield return StartCoroutine(setTileNull(ingredientToRollback.name));
    //    }
    //    else
    //    {
    //        ingredientToRollback.currentTile = ingredientToRollback.fullRoute[ingredientToRollback.startTurnPos];
    //        ingredientToRollback.currentTile.isTaken = false;
    //        ingredientToRollback.currentTile.ingredient = null;
    //    }

    //    ingredientToRollback.routePosition = ingredientToRollback.startTurnPos;

    //    if (ingredientToRollback.startTurnPos == 0)
    //    {
    //        yield return StartCoroutine(MoveToNextEmptySpace(ingredientToRollback));
    //    }
    //    else
    //    {
    //        yield return StartCoroutine(ingredientToRollback.MoveToNextTile(ingredientToRollback.startTurnTile.transform.position));
    //    }
    //}
    #endregion
}
