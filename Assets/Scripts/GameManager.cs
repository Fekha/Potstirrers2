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
    private int? lastMovedType;
    private Ingredient IngredientMovedWithLower;
    private Ingredient IngredientMovedWithHigher;
    private SqlController sql;
    private GamePlayer playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    private float talkingTimeStart;
    private int activePlayer;
    private bool hasBeenDumb = false;
    private bool GameOver = false;
    private bool DoublesRolled = false;
    private bool DoubleDoubles = false;
    private int GameId = 0;
    private int JennDie1;
    private int JennDie2;
    #endregion

    #region Ingredient Variables
    internal static GameManager i;
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
    public GameObject TalkShitPanel;

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

        if (Settings.LoggedInPlayer.Wins == 0 && !Settings.IsDebug)
        {
            activePlayer = 0;
            getHelp();
        }

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

        if (!Settings.LoggedInPlayer.IsGuest)
        {
            //Change first player to there skins
            AllIngredients = playerList.SelectMany(y => y.myIngredients).ToList();
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
            yield return new WaitForSeconds(0.5f);
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
                moveableList = GetActivePlayer().myIngredients.Where(x => x.IngredientId != lastMovedType).ToList();
            else
                moveableList = playerList.SelectMany(y => y.myIngredients.Where(x => x.IngredientId != lastMovedType)).ToList();
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
        lastMovedType = null;
        RolledPanel.SetActive(false);
        IngredientMovedWithLower = null;
        IngredientMovedWithHigher = null;
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
                if (!doublesText.text.Contains("rolled doubles"))
                    doublesText.text = "";
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
            //I just double call this method for the wine menu, so this often double calls
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

        yield return ingredientToMove.MoveToNextTile(nextTile.transform.position, true);
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
    internal void SetLastMovedIngredient(int ingredient)
    {
        lastMovedType = ingredient;
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

        if (GetActivePlayer().player.Username == "Jenn" && !Settings.LoggedInPlayer.UseD8s)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = allD10s[JennDie1];
            LowerRoll.gameObject.GetComponent<Image>().sprite = allD10s[JennDie2];
        }
        else if (GetActivePlayer().player.Username == Settings.LoggedInPlayer.Username && !Settings.LoggedInPlayer.UseD8s )
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
                doublesText.text = GetActivePlayer().player.Username.Trim() + " rolled doubles so they get an extra turn!" + (!Settings.LoggedInPlayer.DisableDoubles ? "" : " Unless you roll doubles again...");
            }
        }

        RolledPanel.SetActive(true);

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
        RollDice(false);
    }

    public IEnumerator MoveCPUIngredient()
    {
        yield return new WaitForSeconds(.5f);

        if (!DoubleDoubles || !Settings.LoggedInPlayer.DisableDoubles)
        {
            if (IsCPUTurn())
                yield return StartCoroutine(FindBestMove());

            if (!GameOver && IsCPUTurn() && lowerMove != 0 && (IngredientMovedWithLower == null || IngredientMovedWithHigher == null))
                yield return StartCoroutine(FindBestMove());

            IngredientMovedWithLower = null;
            IngredientMovedWithHigher = null;
        }
    }
    
    private IEnumerator SetCPUVariables()
    {
        AllIngredients = playerList.SelectMany(y => y.myIngredients).ToList();
        UseableIngredients = AllIngredients.Where(x => x.IngredientId != lastMovedType).ToList();
        foreach (var ing in AllIngredients)
        {
            //find what ingredients actual end will be accounting for cooked ingredients
            ing.endHigherPositionWithoutSlide = ing.routePosition+higherMove;
            ing.endLowerPositionWithoutSlide = ing.routePosition+lowerMove;
            ing.distanceFromScore = 0;
            for (int i = ing.routePosition+1; i <= ing.endHigherPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i%26].ingredient != null && ing.fullRoute[i % 26].ingredient.isCooked) {
                    ing.endHigherPositionWithoutSlide++;
                }
            }
            for (int i = ing.routePosition+1; i <= ing.endLowerPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i%26].ingredient != null && ing.fullRoute[i % 26].ingredient.isCooked) {
                    ing.endLowerPositionWithoutSlide++;
                }
            }

            for (int i = ing.routePosition+1; i <= 26; i++)
            {
                if (ing.fullRoute[i%26].ingredient == null || !ing.fullRoute[i%26].ingredient.isCooked) {
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
            ?? BeDumb()
            ?? MovePastPrep()
            ?? StompEnemy(true)
            ?? StompSafeZone()
            ?? GoToTrash()
            ?? StompEnemy(false)
            ?? MoveIntoScoring()
            ?? Slide(false)
            ?? BoostWithCookedIngredient()
            ?? MoveFrontMostEnemy()
            ?? MoveOffSpoon()
            ?? Slide(true)
            ?? MoveFrontMostIngredient(false)
            ?? MoveFrontMostIngredient(true)
            ?? MoveNotPastPrep()
            ?? MoveCookedPastPrep()
            ?? MoveEnemyIngredient()
            ?? MoveRandomly();
        yield return StartCoroutine(MoveCPUIngredient(ingredientToMove));
    }
    private IEnumerator MoveCPUIngredient(Ingredient ingredientToMove)
    {
        yield return StartCoroutine(RollSelected(ingredientToMove == IngredientMovedWithHigher, false));
        yield return new WaitForSeconds(.5f);
        yield return StartCoroutine(DeactivateAllSelectors());
        yield return StartCoroutine(ingredientToMove.Move());
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
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
            if (IngredientMovedWithHigher != null)
            {
                PrepShitTalk(TalkType.MoveRandomly); 
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.MoveRandomly);
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveNotPastPrep()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Not Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26
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
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 23
            && x.endLowerPosition > 16
            && !x.isCooked
            && x.distanceFromScore > 9
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
            && x.endHigherPosition > 16
            && !x.isCooked
            && x.distanceFromScore > 9
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Into Scoring"; }
                return IngredientMovedWithHigher;
            }
        }
        return null;
    }

    private Ingredient MoveCookedIngredient()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && x.endHigherPosition < 26
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Cooked Ingredient"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && x.endLowerPosition < 26
            && CanMoveSafely(x, x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Cooked Ingredient"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }  
    private Ingredient BoostWithCookedIngredient()
    {
        if (!UseableIngredients.Any(x => x.isCooked) || IngredientMovedWithHigher != null || IngredientMovedWithLower != null || lowerMove == 0)
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

        if (IngredientMovedWithLower == null && lowerMove != 0)
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
        return !TeamIngredients.Any(y => y.routePosition == endPosition % 26) && (!x.fullRoute[endPosition % 26].isSafe || x.fullRoute[endPosition % 26].ingredient == null);
    }

    private Ingredient MoveCookedPastPrep()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && CanMoveSafely(x, x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && CanMoveSafely(x, x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveFrontMostIngredient(bool withCooked)
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderByDescending(x => x.endHigherPosition).FirstOrDefault(x => x.endHigherPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 9 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && CanMoveSafely(x, x.endHigherPosition)); //Dont stomp on safe area
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Front Most Ingredient"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.endLowerPosition).FirstOrDefault(x => x.endLowerPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 9 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
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

        if (IngredientMovedWithLower == null && lowerMove != 0 && UseableTeamIngredients.Count(x=>x.routePosition == 0) == 0)
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
    private Ingredient MoveOffSpoon()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderByDescending(x=>x.distanceFromScore).FirstOrDefault(x => x.endHigherPosition < 23 
            && (x.routePosition == 8 || x.routePosition == 16) //on a spoon
            && x.distanceFromScore > 9
            && !x.isCooked
            && CanMoveSafely(x, x.endHigherPosition)); //Dont stomp on safe area
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Off Spoon"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => x.endLowerPosition < 23
            && (x.routePosition == 8 || x.routePosition == 16) //on a spoon
              && x.distanceFromScore > 9
              && !x.isCooked
              && CanMoveSafely(x, x.endLowerPosition)); //Dont stomp on safe area
            if (IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Move Off Spoon"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveFromSpawn()
    {
        if (UseableTeamIngredients.All(x => x.routePosition == 0))
        {
            //bm
            if (lowerMove == higherMove && TeamIngredients.Count(x => x.isCooked) == 0 && lowerMove != 4 && lowerMove != 5 && lowerMove != 6)
            {
                if (IngredientMovedWithLower == null && AllIngredients.All(x => x.routePosition == 0))
                {
                    IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault();
                    if (IngredientMovedWithLower != null)
                        return IngredientMovedWithLower;
                }
                //stomp covers the second half of this
            }

            if (lowerMove == higherMove && TeamIngredients.Count(x => x.isCooked) == 0 && (lowerMove == 4 || lowerMove == 5 || lowerMove == 6))
            {
                if (IngredientMovedWithHigher == null && AllIngredients.All(x => x.routePosition == 0))
                {
                    IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault();
                    if (IngredientMovedWithHigher != null)
                        return IngredientMovedWithHigher;
                }

                if (IngredientMovedWithLower == null && AllIngredients.Count(x => x.routePosition == 0) == 5 && AllIngredients.FirstOrDefault(x => x.routePosition == lowerMove))
                {
                    IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault();
                    if (IngredientMovedWithLower != null)
                        return IngredientMovedWithLower;
                }
            }

            if (IngredientMovedWithLower == null && lowerMove != 0)
            {
                IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.isCooked).FirstOrDefault();
                if (IngredientMovedWithLower != null)
                    return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveFrontMostEnemy()
    {
        if (IngredientMovedWithLower == null && lowerMove != 0 && EnemyIngredients.Count(x=>!x.isCooked) == 1)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.distanceFromScore < 10 //move from scoring position
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
            && (x.distanceFromScore > 9 || x.distanceFromScore < 6)  //Dont move from scoring position
            && CanMoveSafely(x, x.endHigherPosition)
            && ((x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpoon));
            if (IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { talkShitText.text = "Slide"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 9 || x.distanceFromScore < 6)  //Dont move from scoring position
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
        if (IngredientMovedWithLower == null && lowerMove != 0)
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
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = teamToMove.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
             && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
             && x.endLowerPosition != 0
             && x.endLowerPosition != x.routePosition
             && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
             && !x.fullRoute[x.endLowerPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithLower;
            }
        }

        if (useEither && IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => x.endHigherPosition < 26 //Dont move past preparation
            && (x.routePosition != 0 || x.TeamYellow == GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && !x.fullRoute[x.endHigherPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (IngredientMovedWithHigher != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithHigher;
            }
        }
        
        return null;
        
    } 
    private Ingredient StompSafeZone()   
    {
        if (IngredientMovedWithLower == null && lowerMove != 0)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
            && x.routePosition != 0 //dont move them from prep
            && AllIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition % 26) //anyone there?
            && x.fullRoute[x.endLowerPosition % 26].isSafe);
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.SafeZoned);
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
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
        if (IngredientMovedWithLower == null && lowerMove != 0)
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
        if (IngredientMovedWithLower == null && IngredientMovedWithHigher == null && lowerMove != 0)
        {
            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 25);
            if (ScoreableIng != null)
            {
                IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng 
                && x.isCooked 
                && x.routePosition < ScoreableIng.routePosition 
                && x.endHigherPosition > ScoreableIng.routePosition
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
        if (IngredientMovedWithLower == null && lowerMove != 0)
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
                var ingsToMove = UseableTeamIngredients.Where(x => x.distanceFromScore > 9).ToList();
                if (ingsToMove.Count() > 0)
                {
                    var toMove = ingsToMove[Random.Range(0, ingsToMove.Count())];
                    hasBeenDumb = true;
                    IngredientMovedWithHigher = toMove;
                    return IngredientMovedWithHigher;
                }
            }

            if (IngredientMovedWithLower == null && lowerMove != 0)
            {
                var ingsToMove = UseableIngredients.Where(x => x.distanceFromScore > 9).ToList();
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
