﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [System.Serializable]
    public class GamePlayer
    {
        public Player player;
        public Ingredient[] myIngredients;
        public bool hasTurn;
        public bool TeamYellow;
    }

    
    private bool DoublesRolled = false;
    private bool DoubleDoubles = false;
    public List<GamePlayer> playerList = new List<GamePlayer>();
    public List<Tile> tiles = new List<Tile>();
    public GameObject EventCanvas;
    public GameObject HelpCanvas;
    public GameObject RollButton;
    public Button HigherRoll;
    public Button LowerRoll;   
    public Button HigherRollButton;
    public Button LowerRollButton;
    public GameObject RolledPanel;
    public GameObject TrashCan2;
    public GameObject TrashCan3;
    public GameObject exitPanel;
    public GameObject undoPanel;
    public Button undoButton;
    public Text higherRollText;
    public Text lowerRollText;
    public Text eventText;
    public Text helpText;
    public Text actionText;
    public Text turnText;
    public Text doublesText;
    private Ingredient lastMovedIngredient;
    public Ingredient LastLandedOnIngredient;
    public Sprite yellowDie;
    public Sprite purpleDie;
    public Sprite yellowD8;
    public Sprite purpleD8;
    private bool hasBeenDumb = false;
    private bool moveWithLowerFirst = false;
    public List<Sprite> allD10s;
    public List<Material> allMeatMaterials;
    public List<Material> allVeggieMaterials;
    public List<Material> allFruitMaterials;

    private int pageNum = 0;
    public bool isMoving = false;
    public bool isPaused = false;
    private List<string> helpTextList = new List<string>() { @"Welcome, " + Settings.LoggedInPlayer.Username +"!"+
@"

The following pages will explain the rules of Pot Stirrers to you.
Click to view the next page or press the x at the top right to play at any point.

Note: The AI's skill gets better as you get more wins!

For more in depth help join our discord! https://discord.gg/fab.",

@"How to win:

Cook all 3 of your ingredients!
One of your uncooked ingredients becomes cooked when you enter the pot with any of your ingredients.",

@"Taking a turn:

Roll two dice.
Move one ingredient from your team with the highest roll.
Move one ingredient from any team with the lowest roll.
Do the moves in either order, but the same ingredient may not be moved twice.
If doubles were rolled, take another turn.",

@"Landing on an ingredient:

If you land on another ingredient, send it to Prep, unless it is in a safe area, in which the ingredient that was moved goes back to Prep instead.",

@"Sliding:

If an ingredient ends it's movement exactly on a tile with a utensil handle on it, immediately move it to the other side of the utensil.
If another ingredient was on the other side, send them back to Prep!",

@"Prep:

All ingredients start on and respawn on Prep, at the top left side of the board.
You can be moved onto or past Prep from the end of the board so be careful!
Prep is it's own tile when counting.",

@"Trash cans:

The first two arrows you come across on the board each lead into a trash can.
You must get an exact number to move an ingredient into one.
When an ingredient is moved into a trash can it goes to Prep.",

@"The Pot:

The arrow coming from the last tile on the board sends an ingredient to the pot.
Just as with the other two arrows, you must get an exact number to move an ingredient here.
Upon entering the pot, one of the uncooked ingredients on your team are flipped, and is now considered “cooked” with a pot as their background image.
The ingredient that moved into the pot moves back to Prep.",

@"Conclusion:

" + Settings.LoggedInPlayer.Username + @", thanks for playing and taking the time to learn the rules.
Playtesting is the core of game design, without you there is no game, so please let me know about any feedback you have!
Additionally, the AI will make less mistakes the more wins you have, enjoy :)"
 };

    private List<Ingredient> AllIngredients;
    private List<Ingredient> TeamIngredients;
    private List<Ingredient> EnemyIngredients;
    private GamePlayer playerWhoWon;
    public int higherMove = 0;
    public int lowerMove = 0;
    public bool firstMoveTaken;
    public bool higherMoveSelected;
    private float drinkingTimeStart;
    internal bool IsCPUTurn()
    {
        return GetActivePlayer().player.playerType == PlayerTypes.CPU;
    }
    internal GamePlayer GetActivePlayer()
    {
        return playerList[activePlayer];
    }
    //STATEMACHINE
    public enum States
    {
        WAITING,
        TAKE_TURN,
        SWITCH_PLAYER,
        GAME_OVER,
        DRINKING
    }

    public States state;
    public int activePlayer;
    bool switchingPlayer;
    SqlController sql;
    [HideInInspector]public int Steps;

    private void Start()
    {
#if UNITY_EDITOR
        Settings.IsDebug = true;
#endif
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
    }
    private void Awake()
    {
        instance = this;
        sql = new SqlController();
        if (Settings.LoggedInPlayer.Wins > 0)
            activePlayer = Random.Range(0, 2);
        else
        {
            getHelp();
            activePlayer = 0;
        }
        state = States.TAKE_TURN;
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
        switch (state)
        {
            case States.WAITING:
                break;
            case States.TAKE_TURN:
                //if (higherMove == lowerMove && higherMove != 0)
                //{
                //    StartCoroutine(SendEventToLog(GetActivePlayer().playerName + " - Rolled doubles last turn so takes another turn!"));
                //}
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
                //StartCoroutine(SendEventToLog(GetActivePlayer().playerName + " - Turn " + TurnNumber + " start."));
                state = States.WAITING;                 
                StartCoroutine(TakeTurn());
                break;
            case States.SWITCH_PLAYER:
                if (switchingPlayer)
                    return;

                switchingPlayer = true;
                activePlayer = (activePlayer+1)%playerList.Count();
                state = States.TAKE_TURN;
                switchingPlayer = false;
                break; 
            case States.GAME_OVER:
                UpdateMoveText(Steps);
               
                EventCanvas.SetActive(true);
                break;
            case States.DRINKING:
                var timeSince = Time.time - drinkingTimeStart;
                EventCanvas.SetActive(true);
                if (Input.GetKeyDown(KeyCode.Mouse0) || timeSince > 2.0)
                {                  
                    EventCanvas.SetActive(false);
                    state = States.WAITING;
                }
                break;
        }
    }
    public void GameOverClicked()
    {
        Settings.IsDebug = false;
        SceneManager.LoadScene("MainMenu");
    }
  
    private IEnumerator TakeTurn()
    {
        RollButton.SetActive(true);
        if (IsCPUTurn()) //Take turn for CPU
        {
            yield return StartCoroutine(CPUTurn());
        }
    }

    public void setWineMenuText(bool teamYellow, int v)
    {
        drinkingTimeStart = Time.time;
        state = States.DRINKING;
        eventText.text = (teamYellow ? "Yellow" : "Purple") + " team drinks for " + v + (v == 1 ? " second" : " seconds") + @".

(1 second for each ingredient in Prep)";
        EventCanvas.SetActive(true);
    }
    internal void UpdateMoveText(int? moveAmount = null)
    {
        actionText.text = moveAmount != null ? "Move: " + moveAmount : "";
    }

    public IEnumerator DeactivateAllSelectors()
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
    public void SetLastMovedIngredient(Ingredient ingredient)
    {
        lastMovedIngredient = ingredient;
    }
    public void getHelp()
    {
        if (!isPaused)
        {
            isPaused = true;
            pageNum = 0;
            helpText.text = helpTextList[pageNum];
            HelpCanvas.SetActive(true);
        }
        else
        {
            isPaused = false;
            pageNum = 0;
            HelpCanvas.SetActive(false);
        }
    }
    public void nextPage()
    {
        if (isPaused)
        {
            if (helpTextList.Count - 1 <= pageNum)
            {
                HelpCanvas.SetActive(false);
                isPaused = false;
            }
            else
            {
                pageNum++;
                helpText.text = helpTextList[pageNum];
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


        if (GetActivePlayer().player.Username == Settings.LoggedInPlayer.Username && !Settings.LoggedInPlayer.UseD8s && Settings.LoggedInPlayer.SelectedDie != 0)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = allD10s[Settings.LoggedInPlayer.SelectedDie];
            LowerRoll.gameObject.GetComponent<Image>().sprite = allD10s[Settings.LoggedInPlayer.SelectedDie];
        }
        else if (GetActivePlayer().player.Username == Settings.SecondPlayer.Username && !Settings.LoggedInPlayer.UseD8s && Settings.SecondPlayer.SelectedDie != 0)
        {
            HigherRoll.gameObject.GetComponent<Image>().sprite = allD10s[Settings.SecondPlayer.SelectedDie];
            LowerRoll.gameObject.GetComponent<Image>().sprite = allD10s[Settings.SecondPlayer.SelectedDie];
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
    public IEnumerator RollSelected(bool isHigher, bool HUMAN)
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
    public IEnumerator SetSelectableIngredients(List<Ingredient> moveableList = null)
    {
        yield return StartCoroutine(DeactivateAllSelectors());

        if (moveableList == null)
        {
            if (higherMoveSelected)
                moveableList = GetActivePlayer().myIngredients.Where(x=> x != lastMovedIngredient).ToList();
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

    #region CPU Methods
    IEnumerator CPUTurn()
    {
        yield return new WaitForSeconds(.5f);
        RollButton.SetActive(false);
        RollDice(false);
    }

    public IEnumerator MoveCPUIngredient()
    {
        yield return new WaitForSeconds(1f);

        if (!DoubleDoubles || !Settings.LoggedInPlayer.DisableDoubles)
        {
            if(lowerMove != 0 && Settings.LoggedInPlayer.Wins > 0)
                yield return StartCoroutine(CheckShouldMoveLowerFirst());

            if (moveWithLowerFirst)
            {
                if (state != States.GAME_OVER)
                    yield return StartCoroutine(GetBestIngredientToMoveWithLower());

                if (state != States.GAME_OVER)
                    yield return StartCoroutine(GetBestIngredientToMoveWithHigher());
            }
            else
            {
                if (state != States.GAME_OVER)
                    yield return StartCoroutine(GetBestIngredientToMoveWithHigher());

                if (lowerMove != 0 && state != States.GAME_OVER)
                    yield return StartCoroutine(GetBestIngredientToMoveWithLower());
            }
            moveWithLowerFirst = false;
        }
    }
    private void DoneReset()
    {
        undoButton.interactable = false;
        hasBeenDumb = false;
        moveWithLowerFirst = false;
        doublesText.text = "";
        firstMoveTaken = false;
        lastMovedIngredient = null;
        RolledPanel.SetActive(false);
        moveWithLowerFirst = false;
        LastLandedOnIngredient = null;
    }
    internal IEnumerator DoneMoving()
    {
        while (state == States.DRINKING)
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
            state = GameManager.States.SWITCH_PLAYER; 
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
                    state = GameManager.States.TAKE_TURN;
                }
                else
                {
                    DoublesRolled = false;
                    state = GameManager.States.SWITCH_PLAYER;
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

    public void GameIsOver(bool debug = false)
    {
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
You gained 150 Calories and " + wonXp + " Xp!";
            }
            else
            {
                eventText.text += @"
You gained " + lostCount * 50 + " Calories and " + lostXp + " Xp!";
            }
        }
        else
        {
            eventText.text += @"
You each gained 50 Calories for each of your cooked ingredients! " + playerWhoWon.player.Username +  " gained " + wonXp + " XP and " + playerwhoLost.player.Username + " gained " + lostXp + " xp!";
        }

        foreach (var gamePlayer in playerList.Where(x=>x.player.playerType == PlayerTypes.HUMAN && !x.player.IsGuest))
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

        state = GameManager.States.GAME_OVER;
    }

    private IEnumerator SetCPUVariables()
    {
        AllIngredients = playerList.SelectMany(y => y.myIngredients.Where(x => x != lastMovedIngredient)).ToList();
        TeamIngredients = AllIngredients.Where(x => x.TeamYellow == GetActivePlayer().TeamYellow).OrderByDescending(x => x.routePosition).ToList();
        EnemyIngredients = AllIngredients.Where(x => x.TeamYellow != GetActivePlayer().TeamYellow).OrderByDescending(x => x.routePosition).ToList();
        while (state == States.DRINKING)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
    private IEnumerator GetBestIngredientToMoveWithHigher()
    {
        yield return StartCoroutine(SetCPUVariables());
        yield return RollSelected(true, false);
        yield return StartCoroutine(MoveCPUIngredient(CookIngredient(TeamIngredients)
            ?? BeDumb(TeamIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? SlideOnThermometor(TeamIngredients)
            ?? MoveFrontMostIngredient(TeamIngredients)
            ?? MoveNotPastPrep(TeamIngredients)
            ?? MoveRandomly(TeamIngredients)));
    }

    private IEnumerator GetBestIngredientToMoveWithLower()
    {
        yield return StartCoroutine(SetCPUVariables());
        yield return RollSelected(false, false);
        yield return StartCoroutine(MoveCPUIngredient(CookIngredient(TeamIngredients)
            ?? BeDumb(AllIngredients)
            ?? MovePastPrep(EnemyIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? StompSafeZone(EnemyIngredients)
            ?? GoToTrash(EnemyIngredients)
            ?? SlideOnThermometor(TeamIngredients)
            ?? MoveFrontMostIngredient(TeamIngredients)
            ?? MoveNotPastPrep(TeamIngredients)
            ?? MoveNotPastPrep(EnemyIngredients)
            ?? MoveRandomly(TeamIngredients)));
    }
    private IEnumerator CheckShouldMoveLowerFirst()
    {
        yield return StartCoroutine(SetCPUVariables());
        Steps = lowerMove;
        var ing = CookIngredient(TeamIngredients)
            ?? MovePastPrep(EnemyIngredients)
            ?? SlideOnEnemy(TeamIngredients)
            ?? StompEnemy(TeamIngredients)
            ?? StompSafeZone(EnemyIngredients)
            ?? GoToTrash(EnemyIngredients)
            ?? SlideOnThermometor(TeamIngredients);
        moveWithLowerFirst = (ing != null);
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
        var BestMoves = teamToMove.Where(x => (x.routePosition + Steps) < 26
        && !TeamIngredients.Any(y => y.routePosition == (x.routePosition + Steps) % 26)).ToList();
        if (BestMoves.Count == 0) return null;
        return BestMoves[Random.Range(0, BestMoves.Count())];
    }

    private Ingredient MoveFrontMostIngredient(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;

        var smartMoves = teamToMove.Where(x => (x.routePosition + Steps) < 23 //Dont move past prep
        && !(x.routePosition > 16) //Dont move from scoring position
        && !TeamIngredients.Any(y => y.routePosition == (x.routePosition + Steps) % 26) //Dont stomp yourself
        && !(x.fullRoute[(x.routePosition + Steps) % 26].isSafe && x.fullRoute[(x.routePosition + Steps) % 26].ingredient != null) //Dont stomp on safe area
        && !x.fullRoute[(x.routePosition + Steps) % 26].hasSpatula //weve already checked if sliding was a good idea
        && !x.fullRoute[(x.routePosition + Steps) % 26].hasSpoon).ToList(); //weve already checked if sliding was a good idea

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
        return teamToMove.FirstOrDefault(x => (x.routePosition + Steps) < 26 //Dont move past preparation
        && !(x.routePosition > 17 && x.routePosition < 20) //Dont move from scoring position
        && ((x.fullRoute[(x.routePosition + Steps) % 26].hasSpatula && !TeamIngredients.Any(y => y.routePosition == (x.routePosition + Steps -6) % 26))
        || (x.fullRoute[(x.routePosition + Steps) % 26].hasSpoon && !TeamIngredients.Any(y => y.routePosition == (x.routePosition + Steps + 6) % 26))));
    } 
    
    private Ingredient SlideOnEnemy(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => (x.routePosition + Steps) < 26 //Dont move past preparation
        && ((x.fullRoute[(x.routePosition + Steps) % 26].hasSpatula && EnemyIngredients.Any(y => y.routePosition == (x.routePosition + Steps - 6) % 26))
        || (x.fullRoute[(x.routePosition + Steps) % 26].hasSpoon && EnemyIngredients.Any(y => y.routePosition == (x.routePosition + Steps + 6) % 26))));

    }
    private Ingredient GoToTrash(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => ( Steps < 6 && (x.routePosition + Steps % 26) == 10) || (x.routePosition + Steps % 26) == 18); //Check for trash cans
    }

    private Ingredient StompEnemy(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => (x.routePosition + Steps) < 26 //Dont move past preparation
        && EnemyIngredients.Any(y => y.routePosition == (x.routePosition + Steps) % 26) //Stomp Enemy
        && !x.fullRoute[(x.routePosition + Steps) % 26].isSafe); //Dont stomp safe area if someone is there
    } 
    private Ingredient StompSafeZone(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => (x.routePosition + Steps) < 26 //Dont move past preparation
        && x.routePosition != 0 //dont move them from prep
        && AllIngredients.Any(y => y.routePosition == (x.routePosition + Steps) % 26 //anyone there?
        && x.fullRoute[(x.routePosition + Steps) % 26].isSafe)); //Stomp self if safe
    }

    private Ingredient CookIngredient(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.FirstOrDefault(x => (x.routePosition + Steps) == 26); //Move into pot
    }

    private Ingredient MovePastPrep(List<Ingredient> teamToMove)
    {
        if (teamToMove.Count == 0) return null;
        return teamToMove.OrderBy(x=>x.routePosition).FirstOrDefault(x => (x.routePosition + Steps) >= 26); //Move past pot
    }
    private Ingredient BeDumb(List<Ingredient> teamToMove)
    {
        //var dumbRange = Settings.currentPlayer.Wins + GetActivePlayer().playerName == "Zach" ? 1 : 0;
        if (!Settings.IsDebug && (Settings.LoggedInPlayer.Wins == 0 || (Random.Range(0, Settings.LoggedInPlayer.Wins) == 0 && !hasBeenDumb))) {
            hasBeenDumb = true;
            var toMove = teamToMove[Random.Range(0, teamToMove.Count)];
            if (toMove.routePosition < 18)
            {
                return toMove;
            }
        }
        return null;
    }
    #endregion

    public IEnumerator setTileNull(string ingName)
    {
        var tileToNull = tiles.FirstOrDefault(x => x.ingredient?.name == ingName);
        yield return tileToNull.ingredient = null;
    }

    public IEnumerator MoveToNextEmptySpace(Ingredient ingredientToMove)
    {
        ingredientToMove.routePosition = 0;
        ingredientToMove.currentTile = ingredientToMove.fullRoute[0];
        Tile nextTile;
        if (tiles.Any(x => x.ingredient?.name == ingredientToMove.name))
        {
            nextTile = tiles.FirstOrDefault(x => x.ingredient?.name == ingredientToMove.name);
        }
        else
        {
            nextTile = tiles.FirstOrDefault(x => x.ingredient == null);
            nextTile.ingredient = ingredientToMove;
        }

        yield return ingredientToMove.MoveToNextTile(nextTile.transform.position);
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
}
