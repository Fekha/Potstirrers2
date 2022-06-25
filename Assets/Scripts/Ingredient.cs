using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Ingredient : MonoBehaviour
{
    public int IngredientId;

    [Header("Routes")]
    public Route route;

    [Header("Tiles")]
    public List<Tile> fullRoute = new List<Tile>();
    public Tile currentTile;

    public Animator anim;
    public TrailRenderer trail;
    public ParticleSystem fire;
    public ParticleSystem stomp;

    internal int routePosition;
    internal int endLowerPosition;
    internal int endHigherPosition;
    internal int endHigherPositionWithoutSlide;
    internal int endLowerPositionWithoutSlide;
    internal int distanceFromScore;
    private int startNodeIndex;
    public string type;

    [Header("Bools")]
    private bool hasTurn; //human input
    public bool isCooked;
    public bool TeamYellow;

    [Header("Selector")]
    public GameObject selector;
    public GameObject NormalQuad;
    public GameObject BackNormalQuad;
    public GameObject CookedQuad;
    public GameObject BackCookedQuad;
    private void Start()
    {
        //plane = new Plane(this.transform.up, Vector3.zero);
        startNodeIndex = 0;
        CreateFullRoute();
        SetSelector(false);
    }
    void CreateFullRoute()
    {
        for (int i = 0; i < route.childNodeList.Count; i++)
        {
            int tempPos = startNodeIndex + i;
            tempPos %= route.childNodeList.Count;

            fullRoute.Add(route.childNodeList[tempPos].GetComponent<Tile>());
        }
    }

    public IEnumerator Move()
    {
        if (GameManager.i.isMoving)
        {
            yield break;
        }
        GameManager.i.isMoving = true;

        yield return StartCoroutine(BeforeMoving());
      
        yield return StartCoroutine(DoMovement()); 

        yield return StartCoroutine(AfterMovement());

        GameManager.i.isMoving = false;
        if (GameManager.i.IsCPUTurn())
            yield return new WaitForSeconds(.5f);

        yield return StartCoroutine(GameManager.i.DoneMoving());
    }

    private IEnumerator BeforeMoving()
    {
        GameManager.i.firstIngredientMoved = IngredientId;
        currentTile.ingredients.Pop();
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator DoMovement()
    {
        while (GameManager.i.Steps > 0)
        {
            while (GameManager.i.IsReading)
            {
                yield return new WaitForSeconds(0.5f);
            }

            GameManager.i.UpdateMoveText(GameManager.i.Steps);
            var didMove = false;

            if (GameManager.i.Steps == 1 && (routePosition == 9 || routePosition == 17))
            {
                if (!GameManager.i.IsCPUTurn() && isCooked)
                    yield return StartCoroutine(GameManager.i.AskShouldTrash());

                if (TeamYellow != GameManager.i.GetActivePlayer().TeamYellow || GameManager.i.ShouldTrash == true) {
                    if (!isCooked || GameManager.i.ShouldTrash == true || GameManager.i.IsCPUTurn())
                    {
                        if (routePosition == 9)
                        {
                            didMove = true;
                            if (!GameManager.i.IsCPUTurn() && this.TeamYellow != GameManager.i.GetActivePlayer().TeamYellow && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                GameManager.i.PrepShitTalk(TalkType.SentBack);
                                GameManager.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan2.transform.position));
                        }
                        else if (routePosition == 17)
                        {
                            didMove = true;
                            if (!GameManager.i.IsCPUTurn() && this.TeamYellow != GameManager.i.GetActivePlayer().TeamYellow && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                GameManager.i.PrepShitTalk(TalkType.SentBack);
                                GameManager.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan3.transform.position));
                        }
                        yield return new WaitForSeconds(0.2f);
                        routePosition = 0;
                    }
                }
            }
           
            if (!didMove)
            {
                if (GameManager.i.Steps == 1 && routePosition == fullRoute.Count - 2 && TeamYellow == GameManager.i.GetActivePlayer().TeamYellow && !isCooked) //go to pot only if its your team
                {
                    if (routePosition == fullRoute.Count - 2) {
                        fire.Play();
                    }
                    routePosition++;
                }
                else if (routePosition != fullRoute.Count - 2) //go forward one
                {
                    routePosition++;
                }
                else //go back to prep
                {
                    routePosition = 0;
                }
            }

            if (GameManager.i.Steps == 1)
            {
                if (fullRoute[routePosition].hasSpoon || fullRoute[routePosition].hasSpatula)
                {
                    trail.enabled = true;
                }
                GameManager.i.ActivateShitTalk();
            }

            if (routePosition == 0)
            {
                yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this, GameManager.i.Steps == 1));
                GameManager.i.Steps--;
            }
            else if (fullRoute[routePosition].ingredients.Count() == 0 || !fullRoute[routePosition].ingredients.Peek().isCooked)
            {
                yield return StartCoroutine(MoveToNextTile());
                GameManager.i.Steps--;
            }

            GameManager.i.ShouldTrash = null;
            didMove = false;
        }
        GameManager.i.UpdateMoveText();
    }
    private IEnumerator Slide() {
        if (fullRoute[routePosition].hasSpoon || fullRoute[routePosition].hasSpatula)
        {
            routePosition = routePosition + (fullRoute[routePosition].hasSpoon ? 6 : -6 );
            yield return StartCoroutine(MoveToNextTile(null, true, 20f));
        }
    }
    private IEnumerator AfterMovement()
    {
        //Cook!
        if (routePosition == fullRoute.Count - 1)
        {
            if (!isCooked)
            {
                trail.enabled = true;
                stomp.Play();
                isCooked = true;
                CookedQuad.gameObject.SetActive(true);
                BackCookedQuad.gameObject.SetActive(false);
                //if (GameManager.i.playerList.SelectMany(x => x.myIngredients).Count(y => y.isCooked) == 1 && Settings.LoggedInPlayer.Wins == 0 && !Settings.IsDebug)
                //{
                //    GameManager.i.FirstScoreHelp();
                //}
            }
            yield return new WaitForSeconds(.1f);
            yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this));
        }
        else
        {
            yield return StartCoroutine(Slide());
            //Check for kill after slide
            if (fullRoute[routePosition].ingredients.Count() > 0)
            {
                //skip the spot if cooked
                if (fullRoute[routePosition].ingredients.Count() > 0 && fullRoute[routePosition].ingredients.Peek().isCooked)
                {
                    while (fullRoute[routePosition].ingredients.Count() > 0 && fullRoute[routePosition].ingredients.Peek().isCooked)
                    {
                        routePosition++;
                        yield return StartCoroutine(MoveToNextTile());
                        yield return StartCoroutine(Slide());
                    }
                }

                if (fullRoute[routePosition].ingredients.Count() > 0)
                {
                    if (fullRoute[routePosition].isSafe)
                    {
                        stomp.Play();
                        if (!GameManager.i.IsCPUTurn() && fullRoute[routePosition].ingredients.Peek().TeamYellow != GameManager.i.GetActivePlayer().TeamYellow && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                        {
                            GameManager.i.PrepShitTalk(TalkType.SentBack);
                            GameManager.i.ActivateShitTalk();
                        }
                        var ingToMove = fullRoute[routePosition].ingredients.Peek();
                        fullRoute[routePosition].ingredients.Pop();
                        yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(ingToMove));
                    }
                    //else //moving other ingredient
                    //{
                    //    fullRoute[routePosition].ingredient.stomp.Play();
                    //    if (!GameManager.i.IsCPUTurn() && this.TeamYellow != GameManager.i.GetActivePlayer().TeamYellow && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                    //    {
                    //        GameManager.i.PrepShitTalk(TalkType.SentBack);
                    //        GameManager.i.ActivateShitTalk();
                    //    }
                    //    yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this));
                    //}
                }
            }
        }
        if (routePosition != 0)
        { 
            currentTile = fullRoute[routePosition];
            currentTile.ingredients.Push(this);
        }
        

        yield return new WaitForSeconds(.1f);
    }

    public IEnumerator MoveToNextTile(Vector3? nextPos = null, bool isforEffect=false, float speed = 35f)
    {
        var yValue = .2f;

        if (isforEffect)
            anim.Play("flip");
        else if(routePosition != 0)
            anim.Play("Moving");

        if(nextPos == null)
            nextPos = fullRoute[routePosition].gameObject.transform.position;
        
        if(!fullRoute[routePosition].isSafe || (fullRoute[routePosition].isSafe && GameManager.i.Steps != 1))
            yValue += (.4f * fullRoute[routePosition].ingredients.Count());

        var goalPos = new Vector3(nextPos.Value.x, yValue, nextPos.Value.z);

        while (goalPos != (transform.position = Vector3.MoveTowards(transform.position, goalPos, speed * Time.deltaTime)))
        { 
            yield return null;
        }

        if (routePosition == 0 && Settings.LoggedInPlayer.WineMenu)
        {
            GameManager.i.setWineMenuText((TeamYellow && !isCooked || !TeamYellow && isCooked), GameManager.i.prepTiles.Count(x => x.ingredients.Count() > 0));
        }

        yield return new WaitForSeconds(0.2f);
    }
    public void SetSelector(bool on)
    {
        trail.enabled = false;
        selector.SetActive(on);
        hasTurn = on;
        if (on)
        {
            anim.Play("Moving");
        }
    }

    private void OnMouseDown()
    {
        if (hasTurn && !GameManager.i.IsCPUTurn())
        {
            if (GameManager.i.firstIngredientMoved != null)
            {
                GameManager.i.undoButton1.gameObject.SetActive(false);
                GameManager.i.undoButton2.gameObject.SetActive(false);
            }
            else if (GameManager.i.firstMoveTaken)
            {
                if (GameManager.i.activePlayer == 0)
                {
                    GameManager.i.undoButton1.gameObject.SetActive(true);
                }
                else
                {
                    GameManager.i.undoButton2.gameObject.SetActive(true);
                }
                
            }
            else
            {
                GameManager.i.undoButton1.gameObject.SetActive(false);
                GameManager.i.undoButton2.gameObject.SetActive(false);
            }
            StartCoroutine(MoveSelectedIngredient());
        }
    }

    private IEnumerator MoveSelectedIngredient()
    {
        yield return StartCoroutine(GameManager.i.DeactivateAllSelectors());
        yield return StartCoroutine(Move());
    }
}
public static class MyEnumExtensions
{
    public static string ToDescriptionString(int val)
    {
        DescriptionAttribute[] attributes = (DescriptionAttribute[])val
           .GetType()
           .GetField(val.ToString())
           .GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : string.Empty;
    }
}