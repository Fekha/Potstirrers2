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
    internal bool Automating = false;
    internal int TurnNumber = 1;
    private int MoveNumber = 0;
    private int TutorialAction = 0;
    internal int higherMove = 0;
    internal int lowerMove = 0;
    internal bool? higherMoveSelected;
    internal List<Ingredient> AllIngredients = new List<Ingredient>();
    private SqlController sql;
    private Player playerWhoWon;
    private int pageNum = 0;
    private float readingTimeStart;
    internal float talkingTimeStart;
    internal int TutorialIngId;
    private float turnTime = 0;
    private float rollDuration = 5;
    private float turnDuration = 35;
    private bool? Player1Turn = null;
    internal bool GameOver = false;
    private bool? OnlineDiceFound = null;
    private bool LookingForTurn = false;
    private TurnPosition[] TurnStartPositions = new TurnPosition[8];
    private States State = States.Switching;
    private bool IsPlayer1Player1 = true;
    List<Ingredient> MoveableList = new List<Ingredient>();
    public GameObject XpText;
    public AudioSource AudioSource;
    public AudioSource TutorialSource;
    private AudioSource audioSourceGlobal;
    public AudioClip turnClip;
    public AudioClip[] tutorialClips;
    private Slider GameMusicSlider;
    private Slider TurnVolumeSlider;
    public GameObject SelectedDieHigher1;
    public GameObject SelectedDieLower1;
    public GameObject SelectedDieHigher2;
    public GameObject SelectedDieLower2;
    public GameObject TutorialChangeDiceArrow;
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
    internal bool IsDrinking = false;
    internal int Steps;
    internal bool? ShouldTrash = null;
    internal bool isMoving = false;
    internal bool firstMoveTaken = false;
    internal int activePlayer = 0;
    #endregion

    #region Unity Editor Variables
    internal Player[] playerList = new Player[2];
    public List<Tile> prepTiles = new List<Tile>();
    public int? lastMovedIngredient;
    public bool checkingForTrash = false;

    [Header("GameObject")]
    public GameObject EventCanvas;
    public GameObject ShouldTrashPopup;
    public GameObject ShouldTrashPopup2;
    public GameObject WineCanvas;
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
    public Text TitleText1;
    public GameObject RollButton1;
    public Image Timer1;

    [Header("Player2Roll")]
    public GameObject TurnBorder2;
    public Button HigherRollImage2;
    public Text HigherRollText2;
    public Button LowerRollImage2;
    public Text LowerRollText2;
    public Text ProfileText2;
    public Text TitleText2;
    public Image Timer2;

    [Header("Undo")]
    public Button undoButton1;
    public Button undoButton2;

    [Header("Text")]
    public Text eventText;
    public Text helpText;
    public Text helpTextTitle;
    public Text talkShitText;
    private GameObject tutorialPanel;
    private Text tutorialText;

    [Header("Sprite")]
    public Sprite yellowDie;
    public Sprite purpleDie;
    public List<Sprite> allD10s;
    
    [Header("Material")]
    public List<Material> allIngredientMaterials;
    public List<Material> allColorMaterials;
    private List<GameObject> objectsInScene;
    private int readClicks = 0;
    internal bool TutorialStopActions = false;
    private bool firstRollUpdate = false;
    private List<int> actions = new List<int>();
    private bool wasTutorial;
    #endregion
    private void GetAllObjectsOnlyInScene()
    {
        objectsInScene = new List<GameObject>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (!(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                objectsInScene.Add(go);
        }
    }
    private GameObject GetObject(string v)
    {
        return objectsInScene.FirstOrDefault(x => x.name == v);
    }
    private void Awake()
    {
        Global.EnteredGame = true;
        wasTutorial = Global.IsTutorial;
        GetAllObjectsOnlyInScene();
        tutorialPanel = GetObject("TutorialPanel");
        tutorialText = tutorialPanel.GetComponentInChildren<Text>();
        GameMusicSlider = GetObject("MusicVolumeSlider").GetComponent<Slider>();
        TurnVolumeSlider = GetObject("TurnVolumeSlider").GetComponent<Slider>();
        i = this;
        Application.targetFrameRate = 60;
        sql = new SqlController();
        if(GameObject.FindGameObjectsWithTag("GameMusic").Length > 0)
            audioSourceGlobal = GameObject.FindGameObjectWithTag("GameMusic").GetComponent<AudioSource>();

        GameMusicSlider.value = Global.LoggedInPlayer.MusicVolume;
        TurnVolumeSlider.value = Global.LoggedInPlayer.TurnVolume;
#if UNITY_EDITOR
        //Global.IsDebug = true;
        //Settings.LoggedInPlayer.Experimental = true;
        rollDuration = 2;
        turnDuration = 2;
#endif
        if (Global.CPUGame)
        {
            activePlayer = Random.Range(0, 2);

            if (Global.LoggedInPlayer.Wins == 0 || Global.IsTutorial)
            {
                activePlayer = 0;
            }
            playerList[0] = Global.LoggedInPlayer;
            playerList[1] = Global.SecondPlayer;

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

            SetSkins();
            if (!Global.LoggedInPlayer.IsGuest && !Global.IsTutorial)
            {
                StartCoroutine(sql.RequestRoutine($"multiplayer/CPUGameStart?Player1={playerList[0].UserId}&Player2={playerList[1].UserId}", GetNewGameCallback));
            }
            else
            {
                TakeTurn();
            }
        }
        else
        {
            StartCoroutine(sql.RequestRoutine($"multiplayer/FindMyGame?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}", GetOnlineGameCallback));
        }
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Mouse0) && (TutorialStopActions || (tutorialPanel.activeInHierarchy && IsCPUTurn())))
        {
            readClicks++;
        }

        if (Global.IsTutorial)
        {
            CheckTutorial();
        }

        elapsed += Time.deltaTime;
        if (elapsed >= 1f)
        {
            elapsed = elapsed % 1f;
            if (!Global.CPUGame)
            {
                if (GetActivePlayer().UserId != Global.LoggedInPlayer.UserId && !checkingForTrash)
                {
                    if (OnlineDiceFound == false)
                    {
                        StartCoroutine(sql.RequestRoutine($"multiplayer/GetGameRoll?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}", CheckForRollsCallback));
                    }
                    if (LookingForTurn == true)
                    {
                        StartCoroutine(sql.RequestRoutine($"multiplayer/GetTurn?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}", CheckForGameTurnsCallback));
                    }
                }
            }
            //Global.GameId != 0 ensures we wait till the game id has actually been gotten
            if (!GameOver && Global.GameId != 0)
            {
                StartCoroutine(sql.RequestRoutine($"multiplayer/CheckGameAlive?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}&OtherUserId={Global.SecondPlayer.UserId}", GameIsAliveCallback));
            }
        }
        
        if (!Global.FriendlyGame && (!Global.CPUGame || Global.FakeOnlineGame))
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
                    if (Player1Turn == true && (!isMoving || checkingForTrash))
                    {
                        if (!Timer1.gameObject.activeInHierarchy)
                            Timer1.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer1.fillAmount = Mathf.InverseLerp(0, turnDuration, turnTime);
                    }

                    if (Player1Turn == false && (!isMoving || checkingForTrash))
                    {
                        if (!Timer2.gameObject.activeInHierarchy)
                            Timer2.gameObject.SetActive(true);
                        turnTime = turnTime - Time.deltaTime;
                        Timer2.fillAmount = Mathf.InverseLerp(0, turnDuration, turnTime);
                    }

                    if (turnTime < 0 && GetActivePlayer().UserId == Global.LoggedInPlayer.UserId)
                    {
                        State = States.Switching;
                        Automating = true;
                        BlockPlayerActionPanel.SetActive(true);
                        if (checkingForTrash)
                        {
                            ShouldTrash = true;
                            if (!Global.CPUGame)
                            {
                                StartCoroutine(sql.RequestRoutine($"multiplayer/UpdateShouldTrash?GameId={Global.GameId}&trash={ShouldTrash}", AfterTrashingCallback));
                            }
                            else
                            {
                                AfterTrashingCallback("");
                            }
                        }
                        else
                        {
                            StartCoroutine(TimeRanOut());
                        }
                    }
                    break;

                default:
                    // code block
                    break;
            }
        }

        //For Wine Menu
        if (Global.IsDebug && IsReading && IsCPUTurn())
        {
            var timeSince = Time.time - readingTimeStart;
            if (timeSince > 2.0)
            {
                WineCanvas.SetActive(false);
                IsReading = false;
                IsDrinking = false;
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

    private void CheckTutorial()
    {
        switch (TutorialAction)
        {
            case 0:
                TutorialTextTurns(0, 2, 0);
                if (readClicks == 2)
                {
                    RollButton1.SetActive(true);
                }
                break;
            case 1:
                TutorialTextTurns(3, 0, 6);
                break;
            case 3:
                TutorialTextTurns(4, 2, 2);
                break;
            case 5:
                TutorialTextTurns(7, 0, 0, false);
                break;
            case 7:
                TutorialTextTurns(8, 0, 0, false);
                break;
            case 9:
                TutorialTextTurns(9, 1, 4);
                break;
            case 11:
                TutorialTextTurns(11, 2, 8);
                break;
            case 13:
                TutorialTextTurns(14, 1, 0, false);
                break;
            case 15:
                TutorialTextTurns(16, 0, 0, false);
                break;
            case 17:
                TutorialTextTurns(17, 0);
                TutorialChangeDiceArrow.SetActive(true);
                break;
            case 18:
                TutorialChangeDiceArrow.SetActive(false);
                TutorialTextTurns(18, 0, 3);
                break;
            case 20:
                TutorialTextTurns(19, 1, 6);
                break;
            case 22:
                TutorialTextTurns(21, 0, 0, false);
                break;
            case 26:
                TutorialTextTurns(22, 1, 6);
                break;
            case 30:
                TutorialTextTurns(24, 0, 0, false);
                break;
            case 32:
                TutorialTextTurns(25, 1, 6);
                break;
            case 34:
                TutorialTextTurns(27, 1, 7);
                break;
            case 36:
                TutorialTextTurns(29, 0, 0, false);
                break;
            case 38:
                TutorialTextTurns(30, 1, 0, false);
                break;
            case 40:
                TutorialTextTurns(32, 1);
                if (readClicks == 33)
                {
                    TutorialChangeDiceArrow.SetActive(true);
                }
                break;
            case 41:
                TutorialChangeDiceArrow.SetActive(false);
                TutorialTextTurns(34, 0, 3);
                break;
            case 43:
                TutorialTextTurns(35, 0, 2);
                break;
            case 45:
                TutorialTextTurns(36, 0, 0, false);
                break;
            case 49:
                TutorialTextTurns(37);
                TutorialChangeDiceArrow.SetActive(true);
                break;
            case 50:
                TutorialChangeDiceArrow.SetActive(false);
                TutorialTextTurns(38, 0, 6);
                break;
            case 52:
                TutorialTextTurns(39, 0, 4);
                break;
            case 56:
                TutorialTextTurns(40, 0, 0, false);
                break;
            case 58:
                TutorialTextTurns(41, 1, 1);
                break;
            case 60:
                TutorialTextTurns(43, 0, 4);
                break;
            case 62:
                TutorialTextTurns(44, 0, 0, false);
                break;
            case 63:
                TutorialTextTurns(45, 1, 4);
                break;
            case 65:
                TutorialTextTurns(47, 0, 4);
                break;
            case 67:
                TutorialTextTurns(48, 1);
                break;
            case 68:
                tutorialPanel.SetActive(false);
                Global.IsTutorial = false;
                UpdateTitle();
                break;
            default:
                tutorialPanel.SetActive(false);
                break;
        }
    }

    private void TutorialTextTurns(int start, int turns = 0, int toMoveId = 0, bool remain = true)
    {
        if (readClicks <= start + turns)
        {
            if (!actions.Contains(readClicks))
            {
                TutorialSource.Stop();
                TutorialSource.clip = tutorialClips[readClicks];
                TutorialSource.volume = Global.LoggedInPlayer.VoiceVolume;
                TutorialSource.Play();
                actions.Add(readClicks);
            }
            tutorialText.text = Library.tutorialTextList[readClicks];
        }
        if (readClicks == start)
        {
            TutorialStopActions = true;
            tutorialPanel.SetActive(true);
        }
        if (readClicks == start + turns)
        {
            TutorialStopActions = false;
            TutorialIngId = toMoveId;
            if(TutorialIngId != 0)
                AllIngredients.FirstOrDefault(x => x.IngredientId == TutorialIngId).SetSelector(true);
        }
        if (readClicks > start + turns)
        {
            if (!remain || readClicks > start + turns + 1)
            {
                tutorialPanel.SetActive(false);
            }
        }
    }

    internal IEnumerator StartedMoving()
    {
        isMoving = true;
        TutorialIngId = 0;
        MoveNumber++;
        if(!IsCPUTurn())
            readClicks++;
        yield return StartCoroutine(TutorialUpdate()); 
    }

    private IEnumerator TutorialUpdate()
    {
        while (tutorialPanel.activeInHierarchy && IsCPUTurn())
        {
            yield return new WaitForSeconds(.1f);
        }
        TutorialAction++;
    }
    #region Button Clicks
    public void GameOverClicked()
    {
        Global.IsDebug = false;
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
            if (!Global.LoggedInPlayer.IsGuest && !Global.IsTutorial)
            {
                var rageQuiterId = Global.LoggedInPlayer.UserId;
                var player1Count = AllIngredients.Count(x => x.Team == 0 && x.isCooked);
                var player2Count = AllIngredients.Count(x => x.Team == 1 && x.isCooked);
                var player1Cooked = IsPlayer1Player1 ? player1Count : player2Count;
                var player2Cooked = IsPlayer1Player1 ? player2Count : player1Count;
                StartCoroutine(sql.RequestRoutine($"multiplayer/GameEnd?GameId={Global.GameId}&Player1Cooked={player1Cooked}&Player2Cooked={player2Cooked}&TotalTurns={TurnNumber}&RageQuit={(rageQuiterId)}", EndGamePopupCallback));
            }
            SceneManager.LoadScene("MainMenu");
        }
        exitPanel.SetActive(false);
    }
    public void promptUndo()
    {
        if (Global.CPUGame)
        {
            undoPanel.SetActive(true);
        }
    }
    public void getHelp()
    {
        if (!IsReading)
        {
            pageNum = 0;
            helpText.text = Library.helpTextList[pageNum];
            StartReading(false);
        }
        else
        {
            IsReading = false;
            IsDrinking = false;
            pageNum = 0;
            WineCanvas.SetActive(false);
        }
    }
    public void ShouldTrashButton(bool trash)
    {
        if (!Global.CPUGame)
        {
            if (GetActivePlayer().UserId == Global.LoggedInPlayer.UserId)
            {
                ShouldTrash = trash;
                StartCoroutine(sql.RequestRoutine($"multiplayer/UpdateShouldTrash?GameId={Global.GameId}&trash={trash}"));
            }
        }
        else
        {
            ShouldTrash = trash;
        }
    }
    public void nextPage()
    {
        if (IsReading && !IsDrinking)
        {
            if (Library.helpTextList.Count - 1 <= pageNum)
            {
                WineCanvas.SetActive(false);
                IsReading = false;
                IsDrinking = false;
            }
            else
            {
                pageNum++;
                helpText.text = Library.helpTextList[pageNum];
            }
        }
        else
        {
            IsReading = false;
            IsDrinking = false;
            WineCanvas.SetActive(false);
        }
    }
    public void RollDice(bool HUMAN)
    {
        if ((Global.IsTutorial && TutorialStopActions) || (!Global.CPUGame && GetActivePlayer().UserId != Global.LoggedInPlayer.UserId) || (IsCPUTurn() && HUMAN))
        {
            return;
        }

        if (!firstRollUpdate)
        {
            firstRollUpdate = true;
            readClicks++;
        }
        var roll1 = Random.Range(0, 10);
        var roll2 = Random.Range(0, 10);

        if (!Global.IsTutorial || Library.TutorialRolls.Count < MoveNumber + 1)
        {
            //extra random because it feels sticky sometimes, #sanitycheck
            roll1 = Random.Range(0, 10);
            roll2 = Random.Range(0, 10);
        }
        else
        {
            roll1 = Library.TutorialRolls[MoveNumber];
            roll2 = Library.TutorialRolls[MoveNumber + 1];
        }

        if (!Global.CPUGame)
        {
            StartCoroutine(sql.RequestRoutine($"multiplayer/UpdateGameRoll?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}&roll1={roll1}&roll2={roll2}"));
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
        if (GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
        {
            turnTime++;
        }

        if (ZerosRolled())
        {
            MoveNumber = MoveNumber + 2;
            TutorialAction++;

            if (Global.IsTutorial) {
                while (readClicks == 44)
                {
                    yield return new WaitForSeconds(.1f);
                }
            } else {
                yield return new WaitForSeconds(2f);
            }

            SwitchPlayer();

        } else {
            if (lowerMove == 0)
            {
                turnTime = Mathf.Min(turnTime + 5, turnDuration);
                firstMoveTaken = true;
                MoveNumber++;
            }
            if (IsCPUTurn())
            {
                yield return StartCoroutine(CpuLogic.i.FindCPUIngredientMoves());
            }
            else
            {
                yield return StartCoroutine(RollSelected(true, false));
            }
        }
    }

    private void ResetDice(bool undo = false)
    {
        if (activePlayer == 0)
        {
            HigherRollText1.text = higherMove.ToString();
            LowerRollText1.text = lowerMove.ToString();
            if (undo)
            {
                HigherRollButton1.interactable = true;
                HigherRollImage1.interactable = true;
                LowerRollButton1.interactable = true;
                LowerRollImage1.interactable = true;
            }
            else
            {
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
                HigherRollImage1.GetComponent<Animation>().Play("Roll");
                LowerRollImage1.GetComponent<Animation>().Play("Roll2");
            }
        }
        else
        {
            HigherRollText2.text = higherMove.ToString();
            LowerRollText2.text = lowerMove.ToString();
            if (undo)
            {
                HigherRollImage2.interactable = true;
                LowerRollImage2.interactable = true;
            }
            else
            {
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
                HigherRollImage2.GetComponent<Animation>().Play("Roll");
                LowerRollImage2.GetComponent<Animation>().Play("Roll2");
            }
        }
    }

    private void UpdateDiceSkin(Player User = null)
    {
        if (User == null)
            User = GetActivePlayer();
        var HigherRollToChange = User.UserId == playerList[0].UserId ? HigherRollImage1 : HigherRollImage2;
        var LowerRollToChange = User.UserId == playerList[0].UserId ? LowerRollImage1 : LowerRollImage2;
        Sprite higherSprite = User.UserId == playerList[0].UserId ? yellowDie : purpleDie;
        Sprite lowerSprite = User.UserId == playerList[0].UserId ? yellowDie : purpleDie;
        if (User.IsCPU)
        {
            higherSprite = allD10s[Random.Range(0, allD10s.Count())];
            lowerSprite = allD10s[Random.Range(0, allD10s.Count())];
        }
        else if (User.SelectedDice.Count > 0)
        {
            var random = new System.Random();
            int index1 = random.Next(User.SelectedDice.Count);
            int index2 = random.Next(User.SelectedDice.Count);
            higherSprite = allD10s[User.SelectedDice[index1] - 1];
            lowerSprite = allD10s[User.SelectedDice[index2] - 1];
        }
        HigherRollToChange.gameObject.GetComponent<Image>().sprite = higherSprite;
        LowerRollToChange.gameObject.GetComponent<Image>().sprite = lowerSprite;
    }

    private void UpdateTitle(Player User = null)
    {
        if (User == null)
            User = GetActivePlayer();

        var textToChange = User.UserId == playerList[0].UserId ? TitleText1 : TitleText2;
        if (User.UserId == 3)
        {
            textToChange.text = "The Dev";
        }
        else if (User.UserId == 8)
        {
            if (Global.IsTutorial)
                textToChange.text = "Teacher";
            else
                textToChange.text = "Your worst nightmare";
        }
        else if (User.IsCPU)
        {
            textToChange.text = "CPU";
        }
        else if (Global.IsTutorial)
        {
            textToChange.text = "Student";
        }
        else if (User.IsGuest)
        {
            textToChange.text = "Account Hater";
        }
        else if (User.SelectedTitles.Count > 0)
        {
            var random = new System.Random();
            int index1 = random.Next(User.SelectedTitles.Count);
            textToChange.text = User.SelectedTitles[index1];
        }
        else
        {
            textToChange.text = "Loser";
        }
    }

    public void RollSelected(bool isHigher)
    {
        if (!Global.CPUGame && GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
        {
            return;
        }

        //if (!Global.CPUGame && GetActivePlayer().UserId == Global.LoggedInPlayer.UserId)
        //{
        //    StartCoroutine(sql.RequestRoutine($"multiplayer/UpdateSelected?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}&Higher={isHigher}"));
        //}

        StartCoroutine(RollSelected(isHigher, true));
    }
    #endregion

    #region UNDO
    public void undoChoice(bool willPay)
    {
        if (willPay)
        {
            turnTime = Mathf.Min(turnTime + 5, turnDuration);
            higherMoveSelected = null;
            undoButton1.interactable = false;
            undoButton2.interactable = false;
            lastMovedIngredient = null;
            firstMoveTaken = false;
            ResetDice(true);
            ClearSelectedDie();
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
        ingredientToRollback.trail.enabled = true;

        if (ingredientToRollback.isCooked != turnPosition.ingCooked)
        {
            ingredientToRollback.isCooked = false;
            ingredientToRollback.CookedQuad.gameObject.SetActive(false);
            ingredientToRollback.BackCookedQuad.gameObject.SetActive(true);
        }

        if (ingredientToRollback.routePosition != turnPosition.ingPos)
        {
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
                yield return StartCoroutine(ingredientToRollback.MoveToNextTile(Route.i.FullRoute[turnPosition.ingPos].transform.position, sentBack:true));
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

    #region Used by ingredient
    internal bool IsCPUTurn()
    {
        return GetActivePlayer().IsCPU;
    }
    internal Player GetActivePlayer()
    {
        return playerList[activePlayer];
    }

    internal IEnumerator DoneMoving()
    {
        if (IsCPUTurn())
            yield return new WaitForSeconds(.5f);

        while (IsReading && Global.CPUGame)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (playerList.Where((x, i) => AllIngredients.Where(y => y.Team == i).All(y => y.isCooked)).Count() > 0)
        {
            GameIsOver();
        }
        else if (firstMoveTaken || lowerMove == 0)
        {
            yield return new WaitForSeconds(.5f);
            SwitchPlayer();
        }
        else
        {
            checkingForTrash = false;
            turnTime = Mathf.Min(turnTime + 5, turnDuration);
            if (GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
            {
                turnTime++;
            }
            firstMoveTaken = true;
            
            if (!Global.CPUGame && GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
                LookingForTurn = true;

            if (!IsCPUTurn())
            {
                ClearSelectedDie();
                //todo add undo to multiplayer
                if (Global.CPUGame && !Global.IsTutorial)
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
                yield return StartCoroutine(RollSelected(!(bool)higherMoveSelected, false));
            }
        }
    }
    internal IEnumerator MoveToNextEmptySpace(Ingredient ingredientToMove, bool shouldPush = true, bool sentBack = true)
    {
        ingredientToMove.routePosition = 0;
        var prepTile = prepTiles.FirstOrDefault(x => x.ingredients.Count() == 0);
        if (shouldPush)
            prepTile.ingredients.Push(ingredientToMove);
        yield return ingredientToMove.MoveToNextTile(prepTile.transform.position, isforEffect: shouldPush, sentBack: sentBack);
    }
    internal void setWineMenuText(bool teamYellow, int v)
    {
        IsDrinking = true;
        helpText.text = (teamYellow ? "Yellow" : "Purple") + " team drinks for " + v + (v == 1 ? " second" : " seconds") + ". \n \n Math: 1 second for each ingredient in Prep, other team drinks if cooked.";
        StartReading(true);
    }
    internal void UpdateMoveText(int? moveAmount = null)
    {
        //actionText.text = moveAmount != null ? "Move: " + moveAmount : "";
    }
    internal IEnumerator AskShouldTrash()
    {
        turnTime = Mathf.Min(turnTime + 5, turnDuration);
        if (GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
        {
            ShouldTrashPopup2.SetActive(true);

        }
        else
        {
            ShouldTrashPopup.SetActive(true);
        }

        while (ShouldTrash == null)
        {
            checkingForTrash = true;
            if (!Global.CPUGame && GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
            {
                StartCoroutine(sql.RequestRoutine($"multiplayer/GetShouldTrash?GameId={Global.GameId}", GetShouldTrashCallback));
            }
            yield return new WaitForSeconds(.5f);
        }

        ShouldTrashPopup.SetActive(false);
        ShouldTrashPopup2.SetActive(false);
    }
    #endregion
    private void AfterTrashingCallback(string obj)
    {
        checkingForTrash = false;
        StartCoroutine(TimeRanOut());
    }

    public void OnVolumeChanged(string type)
    {
        if (type == "turn")
        {
            Global.LoggedInPlayer.TurnVolume = TurnVolumeSlider.value;
        }
        else if (type == "music")
        {
            Global.LoggedInPlayer.MusicVolume = GameMusicSlider.value;
            if (audioSourceGlobal != null)
                audioSourceGlobal.volume = Global.LoggedInPlayer.MusicVolume;
        }
        else if (type == "master")
        {
            Global.LoggedInPlayer.MasterVolume = GetObject("MasterVolumeSlider").GetComponent<Slider>().value;
            AudioListener.volume = Global.LoggedInPlayer.MasterVolume;
        }
        else if (type == "voice")
        {
            Global.LoggedInPlayer.VoiceVolume = GetObject("VoiceVolumeSlider").GetComponent<Slider>().value;
            TutorialSource.volume = Global.LoggedInPlayer.VoiceVolume;
        }
        else if (type == "effect")
        {
            Global.LoggedInPlayer.EffectsVolume = GetObject("EffectsVolumeSlider").GetComponent<Slider>().value;
        }
    }
    public void ExitSettings()
    {
        StartCoroutine(sql.RequestRoutine($"player/UpdateSettings?UserId={(Global.LoggedInPlayer.UserId)}" +
                $"&MasterVolume={(Global.LoggedInPlayer.MasterVolume)}" +
                $"&MusicVolme={(Global.LoggedInPlayer.MusicVolume)}" +
                $"&TurnVolume={(Global.LoggedInPlayer.TurnVolume)}" +
                $"&VoiceVolume={(Global.LoggedInPlayer.VoiceVolume)}" +
                $"&EffectVolume={(Global.LoggedInPlayer.EffectsVolume)}"));
    }
    private IEnumerator TimeRanOut()
    {
        while (isMoving)
        {
            yield return new WaitForSeconds(0.5f);
        }
        if (firstMoveTaken)
        {
            if (lowerMove == 0)
            {
                yield return StartCoroutine(RollSelected(true, false));
            }
        }
        else
        {
            yield return StartCoroutine(RollSelected(true, false));
            yield return new WaitForSeconds(0.5f);
            var ing = MoveableList[Random.Range(0, MoveableList.Count())];
            yield return StartCoroutine(ing.Move());
           
        }

        yield return new WaitForSeconds(0.5f);
        var ing2 = MoveableList[Random.Range(0, MoveableList.Count())];
        yield return StartCoroutine(ing2.Move());
    }

    private void SetSkins()
    {
        ProfileText1.text = Global.LoggedInPlayer.Username;
        ProfileText2.text = Global.SecondPlayer.Username;
        UpdateIngredientSkins();
        UpdateDiceSkin(Global.LoggedInPlayer);
        UpdateDiceSkin(Global.SecondPlayer);
        UpdateTitle(Global.LoggedInPlayer);
        UpdateTitle(Global.SecondPlayer);
    }

    private void UpdateIngredientSkins()
    {
        List<int>[] originalMats = new List<int>[2] { new List<int>(), new List<int>() };
        List<int>[] unusedMats = new List<int>[2] { new List<int>(), new List<int>() };
        for (int i = 0; i < playerList.Length; i++)
        {
            if (playerList[i].IsCPU)
            {
                if (!Global.IsTutorial)
                {
                    int k = 0;
                    allIngredientMaterials.ForEach(x => { unusedMats[i].Add(k); k++; });
                }
                else
                {
                    originalMats[i].Add(allIngredientMaterials.Count - 1);
                    originalMats[i].Add(allIngredientMaterials.Count - 2);
                    originalMats[i].Add(allIngredientMaterials.Count - 3);
                    originalMats[i].Add(allIngredientMaterials.Count - 4);
                }
            }
            else
            {
                playerList[i].SelectedIngs.ForEach(x => originalMats[i].Add(x-1));
                playerList[i].SelectedIngs.ForEach(x => unusedMats[i].Add(x-1));
            }
           
            var playerIngs = AllIngredients.Where(x => x.Team == i).ToList();
            for (int j = 0; j < playerIngs.Count; j++)
            {
                int SelMat = j;
               
                if (unusedMats[i].Count > 0)
                {
                    var index = Random.Range(0, unusedMats[i].Count);
                    SelMat = unusedMats[i][index];
                    unusedMats[i].RemoveAt(index);
                }
                else if (originalMats[i].Count > 0)
                {
                    var index = j%originalMats[i].Count;
                    SelMat = originalMats[i][index];
                }

                var frontQuads = playerIngs[j].NormalQuad.GetComponent<MeshRenderer>();
                var frontMats = frontQuads.materials;
                frontMats[0] = allIngredientMaterials[SelMat];
                frontQuads.materials = frontMats;
                
                var backQads = playerIngs[j].BackNormalQuad.GetComponent<MeshRenderer>();
                var backMats = backQads.materials;
                backMats[0] = allIngredientMaterials[SelMat];
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
        Global.GameId = sql.jsonConvert<int>(data);
        TakeTurn();
    }  
    private void GetShouldTrashCallback(string data)
    {
        ShouldTrash = sql.jsonConvert<bool?>(data);
    }

    private void GameIsAliveCallback(string data)
    {
        var GameAlive = sql.jsonConvert<int>(data);
        if (GameAlive != 0)
        {
            GameIsOver(GameAlive);
        }
    }
    private void GetOnlineGameCallback(string data)
    {
        var GameState = sql.jsonConvert<GameState>(data);
        GameState.Player1.IsGuest = false;
        GameState.Player2.IsGuest = false;
        GameState.Player1.IsCPU = false;
        GameState.Player2.IsCPU = false;
        playerList[0] = Global.LoggedInPlayer;
        Global.SecondPlayer = GameState.Player2.UserId == Global.LoggedInPlayer.UserId ? GameState.Player1 : GameState.Player2;
        playerList[1] = Global.SecondPlayer;
        if (GameState.Player1.UserId == Global.LoggedInPlayer.UserId)
        {
            IsPlayer1Player1 = true;
            activePlayer = GameState.IsPlayer1Turn ? 0 : 1;
        }
        else
        {
            IsPlayer1Player1 = false;
            activePlayer = GameState.IsPlayer1Turn ? 1 : 0;
        }
        int k = 0;
        int j = IsPlayer1Player1 ? 0 : 1;
        foreach (var tile in prepTiles)
        {
            j++;
            k++;
            var ingObj = Instantiate(IngredientToSpawn);
            ingObj.transform.position = tile.transform.position;
            var ing = ingObj.GetComponent<Ingredient>();
            ing.IngredientId = k;
            ing.Team = j % 2;
            tile.ingredients.Push(ing);
            AllIngredients.Add(ing);
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
            StartCoroutine(MoveOnlineIngredient(AllIngredients.FirstOrDefault(x => x.IngredientId == turn.IngId), turn.Higher));
        }
    }
    internal IEnumerator MoveOnlineIngredient(Ingredient ingredientToMove, bool higher)
    {
        yield return StartCoroutine(RollSelected(higher, false));
        yield return new WaitForSeconds(.5f);
        yield return StartCoroutine(ingredientToMove.Move());
    }
    private void CheckSelectedCallback(string data)
    {
        var isHigher = sql.jsonConvert<bool?>(data);
        if (isHigher != null)
        {
            StartCoroutine(RollSelected((bool)isHigher, false));
        }
    }
    internal void SwitchPlayer()
    {
        State = States.Switching;
        ClearSelectedDie();
        higherMoveSelected = null;
        TurnNumber++;
        Player1Turn = null;
        Automating = false;
        BlockPlayerActionPanel.SetActive(false);
        Timer1.gameObject.SetActive(false);
        Timer2.gameObject.SetActive(false);
        HigherRollImage1.interactable = false;
        LowerRollImage1.interactable = false;
        HigherRollButton1.interactable = false;
        LowerRollButton1.interactable = false;
        HigherRollImage2.interactable = false;
        LowerRollImage2.interactable = false; 
        undoButton1.gameObject.SetActive(false);
        undoButton2.gameObject.SetActive(false);
        undoButton1.interactable = true;
        undoButton2.interactable = true;
        TurnBorder1.SetActive(false);
        TurnBorder2.SetActive(false);
        OnlineDiceFound = null;
        lastMovedIngredient = null;
        firstMoveTaken = false;
        checkingForTrash = false;
        activePlayer = (activePlayer + 1) % playerList.Length;
        CpuLogic.i.IngredientMovedWithHigher = null;
        CpuLogic.i.IngredientMovedWithLower = null;
        TakeTurn();
    }
    private void StartReading(bool isWine)
    {
        helpTextTitle.text = isWine ? "Wine Menu" : "Instructions";
        IsReading = true;
        readingTimeStart = Time.time;
        WineCanvas.SetActive(true);
    }
    private void TakeTurn()
    {
        SetIngredientPositions();
        turnTime = rollDuration;
        if (GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
        {
            turnTime++;
        }
        State = States.Rolling;
        if (activePlayer == 0)
        {
            AudioSource.PlayOneShot(turnClip, Global.LoggedInPlayer.TurnVolume);
            BlockPlayerActionPanel.SetActive(false);
            HigherRollText1.text = "";
            LowerRollText1.text = "";
            if(TurnNumber != 1 || !Global.IsTutorial)
                RollButton1.SetActive(true);
            TurnBorder1.SetActive(true);
            HigherRollImage1.interactable = true;
            LowerRollImage1.interactable = true;
            Player1Turn = true;
        }
        else
        {
            BlockPlayerActionPanel.SetActive(true);
            HigherRollText2.text = "";
            LowerRollText2.text = "";
            TurnBorder2.SetActive(true);
            HigherRollImage2.interactable = true;
            LowerRollImage2.interactable = true;
            Player1Turn = false;
        }
        UpdateDiceSkin();
        UpdateTitle();
        if (!Global.CPUGame && GetActivePlayer().UserId != Global.LoggedInPlayer.UserId)
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
        for (int i = 0; i < AllIngredients.Count(); i++)
        {
            TurnStartPositions[i] = new TurnPosition(AllIngredients[i].routePosition, AllIngredients[i].isCooked);
        }
    }

    internal IEnumerator RollSelected(bool isHigher, bool HUMAN)
    {
        if (isMoving)
        {
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            if (higherMoveSelected != isHigher && (!HUMAN || (!Global.IsTutorial || TutorialChangeDiceArrow.activeInHierarchy)))
            {
                if (TutorialChangeDiceArrow.activeInHierarchy)
                {
                    readClicks++;
                }
                yield return StartCoroutine(TutorialUpdate());
                higherMoveSelected = isHigher;
                Steps = higherMoveSelected == true ? higherMove : lowerMove;
                SetDice();
                yield return StartCoroutine(SetSelectableIngredients());
            }
        }     
    }
    internal void ClearSelectedDie()
    {
        SelectedDieHigher1.SetActive(false);
        SelectedDieLower1.SetActive(false);
        SelectedDieHigher2.SetActive(false);
        SelectedDieLower2.SetActive(false);
    }
    private void SetDice()
    {
        if (higherMoveSelected != null)
        {
            if (activePlayer == 0)
            {
                if ((bool)higherMoveSelected)
                {
                    SelectedDieHigher1.SetActive(true);
                    SelectedDieLower1.SetActive(false);
                }
                else
                {
                    SelectedDieLower1.SetActive(true);
                    SelectedDieHigher1.SetActive(false);
                }
                if (lowerMove == 0)
                {
                    LowerRollImage1.interactable = false;
                    LowerRollButton1.interactable = false;
                }
                if (firstMoveTaken)
                {
                    if ((bool)higherMoveSelected)
                    {
                        HigherRollImage1.interactable = true;
                        LowerRollImage1.interactable = false;
                        HigherRollButton1.interactable = true;
                        LowerRollButton1.interactable = false;
                    }
                    else
                    {
                        HigherRollImage1.interactable = false;
                        LowerRollImage1.interactable = true;
                        HigherRollButton1.interactable = false;
                        LowerRollButton1.interactable = true;
                    }
                }
                if(!firstMoveTaken && lowerMove != 0)
                {
                    HigherRollImage1.interactable = true;
                    LowerRollImage1.interactable = true;
                    HigherRollButton1.interactable = true;
                    LowerRollButton1.interactable = true;
                }
            }
            else
            {
                if ((bool)higherMoveSelected)
                {
                    SelectedDieHigher2.SetActive(true);
                    SelectedDieLower2.SetActive(false);
                }
                else
                {
                    SelectedDieLower2.SetActive(true);
                    SelectedDieHigher2.SetActive(false);
                }
                if (lowerMove == 0)
                {
                    LowerRollImage2.interactable = false;
                }
                if (firstMoveTaken)
                {
                    if ((bool)higherMoveSelected)
                    {
                        HigherRollImage2.interactable = true;
                        LowerRollImage2.interactable = false;
                    }
                    else
                    {
                        HigherRollImage2.interactable = false;
                        LowerRollImage2.interactable = true;
                    }
                }
                if (!firstMoveTaken && lowerMove != 0)
                {
                    HigherRollImage2.interactable = true;
                    LowerRollImage2.interactable = true;
                }
            }
        }
    }
    private IEnumerator SetSelectableIngredients()
    {
        AllIngredients.ForEach(x => x.SetSelector(false));

        if (ZerosRolled())
            MoveableList = new List<Ingredient>();
        else if (DoublesRolled()) 
            MoveableList = AllIngredients.Where(x=> x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x).ToList();
        else if (higherMoveSelected == true)
            MoveableList = AllIngredients.Where(x => x.Team == activePlayer && x.IngredientId != lastMovedIngredient && (x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x)).ToList();
        else
            MoveableList = AllIngredients.Where(x => x.IngredientId != lastMovedIngredient && (x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x)).ToList();

        if (MoveableList.Count() == 0)
        {
            yield return new WaitForSeconds(.5f);
            SwitchPlayer();
        }
        else
        {
            MoveableList.ForEach(x => x.SetSelector(true));

            if (IsCPUTurn())
                yield return new WaitForSeconds(.5f);
        }
    }

    private void GameIsOver(int rageQuiterId = 0)
    {
        GameOver = true;
        BlockPlayerActionPanel.SetActive(false);
        var player1Count = AllIngredients.Count(x => x.Team == 0 && x.isCooked);
        var player2Count = AllIngredients.Count(x => x.Team == 1 && x.isCooked);
        if (rageQuiterId != 0)
        {
            playerWhoWon = playerList.FirstOrDefault(x => x.UserId != rageQuiterId);
        }
        else
        {
            playerWhoWon = playerList.Where((x, i) => AllIngredients.Where(y => y.Team == i).All(y => y.isCooked)).FirstOrDefault();
        }
        if (!Global.LoggedInPlayer.IsGuest && !wasTutorial)
        {
            var player1Cooked = IsPlayer1Player1 ? player1Count : player2Count;
            var player2Cooked = IsPlayer1Player1 ? player2Count : player1Count;
            StartCoroutine(sql.RequestRoutine($"multiplayer/GameEnd?GameId={Global.GameId}&Player1Cooked={player1Cooked}&Player2Cooked={player2Cooked}&TotalTurns={TurnNumber}&RageQuit={(rageQuiterId)}", EndGamePopupCallback));
        }
        else
        {
            eventText.text = (player1Count > player2Count ? "You Won!" : "You Lost!");

            if(Global.LoggedInPlayer.IsGuest)
                eventText.text += "\n \n Create an account to track your games and get rewards!";
            else
                eventText.text += "\n \n Once you are ready, play online for better rewards!";

            
            if (player1Count > player2Count)
            {
                if (wasTutorial)
                {
                    eventText.text += "\n \n Great work beating the tutorial! Now go try out the game online!";
                }
                else if (Global.SecondPlayer.UserId == 5)
                {
                    Global.LoggedInPlayer.Wins++;
                    eventText.text += $" \n \n Jenn gets better each time you win against her, so play again and see if you got what it takes! You earned 50 Calories.";
                }
            }
            else
            {
                if (wasTutorial)
                {
                    eventText.text += "\n \n No worries, many of the greats have lost vs Mike, try again from the main menu!";
                }
                else if (Global.SecondPlayer.UserId == 5)
                {
                    eventText.text += "\n \n Keep practicing, there's more skill to the game than you might think!";
                }
            }

            if (Global.LoggedInPlayer.WineMenu)
                eventText.text += "\n \n" + (playerWhoWon.UserId == Global.LoggedInPlayer.UserId ? " Purple" : " Yellow") + " Team finish your drinks!";

            eventText.text += "\n \n (Click to continue)";

            EventCanvas.SetActive(true);
        }

        if (playerWhoWon.Username == Global.LoggedInPlayer.Username)
            Global.LoggedInPlayer.Wins++;
    }

    private void EndGamePopupCallback(string data)
    {
        eventText.text = data;

        if (Global.LoggedInPlayer.WineMenu)
            eventText.text += "\n \n" + (playerWhoWon.UserId == Global.LoggedInPlayer.UserId ? " Purple" : " Yellow") + " Team finish your drinks!";

        if (TurnNumber > 19 && !Global.FriendlyGame && (!Global.CPUGame || Global.FakeOnlineGame))
            Global.JustWonOnline = playerWhoWon.UserId == Global.LoggedInPlayer.UserId;

        EventCanvas.SetActive(true);
    }
}
