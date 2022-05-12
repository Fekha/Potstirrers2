using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Ingredient : MonoBehaviour
{
    [Header("Routes")]
    public Route route;

    [Header("Tiles")]
    public List<Tile> fullRoute = new List<Tile>();
    public Tile currentTile;

    //public int startTurnPos;
    //public Tile startTurnTile;

    public Animator anim;

    internal int routePosition;
    internal int endPosition;
    internal int endLowerPosition;
    internal int endHigherPosition;
    private int startNodeIndex;
    
    [Header("Bools")]
    private bool hasTurn; //human input
    internal bool isCooked;
    public bool TeamYellow;

    [Header("Selector")]
    public GameObject selector;
    public GameObject Material;
    public GameObject NormalQuad;
    public GameObject CookedQuad;
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
        if (GameManager.instance.isMoving)
        {
            yield break;
        }
        GameManager.instance.isMoving = true;

        yield return StartCoroutine(BeforeMoving());
      
        yield return StartCoroutine(DoMovement()); 

        yield return StartCoroutine(AfterMovement());

        GameManager.instance.isMoving = false;
        if (GameManager.instance.IsCPUTurn())
            yield return new WaitForSeconds(.5f);

        yield return StartCoroutine(GameManager.instance.DoneMoving());
    }

    private IEnumerator BeforeMoving()
    {
        GameManager.instance.SetLastMovedIngredient(this);
        //startTurnPos = routePosition;
        //startTurnTile = currentTile;
        if (routePosition != 0)
        {
            currentTile = fullRoute[routePosition];
            currentTile.ingredient = null;
        }
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator DoMovement()
    {
        while (GameManager.instance.Steps > 0)
        {
            while (GameManager.instance.IsReading)
            {
                yield return new WaitForSeconds(0.5f);
            }

            GameManager.instance.UpdateMoveText(GameManager.instance.Steps);
            var didMove = false;

            if (GameManager.instance.Steps == 1 && TeamYellow != GameManager.instance.GetActivePlayer().TeamYellow && (routePosition == 9 || routePosition == 17))
            {
                if (!GameManager.instance.IsCPUTurn() && isCooked)
                    yield return StartCoroutine(GameManager.instance.AskShouldTrash());

                if (!isCooked || GameManager.instance.ShouldTrash == true || GameManager.instance.IsCPUTurn())
                {
                    if (routePosition == 9)
                    {
                        didMove = true;
                        yield return StartCoroutine(MoveToNextTile(GameManager.instance.TrashCan2.transform.position));
                    }
                    else if (routePosition == 17)
                    {
                        didMove = true;
                        yield return StartCoroutine(MoveToNextTile(GameManager.instance.TrashCan3.transform.position));
                    }
                    yield return new WaitForSeconds(0.2f);
                    routePosition = 0;
                }
                
            }

            if (!didMove)
            {
                if (GameManager.instance.Steps == 1 && routePosition == fullRoute.Count - 2 && TeamYellow == GameManager.instance.GetActivePlayer().TeamYellow && (!Settings.LoggedInPlayer.Experimental || !isCooked)) //go to pot only if its your team
                {
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

            if (routePosition == 0)
            {
                yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
                GameManager.instance.Steps--;
            }
            else if (!Settings.LoggedInPlayer.Experimental || fullRoute[routePosition].ingredient == null || !fullRoute[routePosition].ingredient.isCooked)
            {
                yield return StartCoroutine(MoveToNextTile());
                GameManager.instance.Steps--;
            }

            GameManager.instance.ShouldTrash = null;
            didMove = false;
        }
        GameManager.instance.UpdateMoveText();
    }
    private IEnumerator Slide() {
        if (fullRoute[routePosition].hasSpoon)
        {
            routePosition = routePosition + 6;
            yield return StartCoroutine(MoveToNextTile(null, 11f));
        }
        if (fullRoute[routePosition].hasSpatula)
        {
            routePosition = routePosition - 6;
            yield return StartCoroutine(MoveToNextTile(null, 11f));
        }
    }
    private IEnumerator AfterMovement()
    {
        //Cook!
        if (routePosition == fullRoute.Count - 1)
        {
            Ingredient IngredientToCook = null;
            if (!isCooked)
            {
                IngredientToCook = this;
            }
            else if (!Settings.LoggedInPlayer.Experimental)
            {
                IngredientToCook = GameManager.instance.GetActivePlayer().myIngredients.FirstOrDefault(x => !x.isCooked);
            }
            if (IngredientToCook != null)
            {
                IngredientToCook.isCooked = true;
                IngredientToCook.anim.Play("flip");
                IngredientToCook.CookedQuad.gameObject.SetActive(true);
                if (Settings.LoggedInPlayer.Experimental && GameManager.instance.playerList.SelectMany(x => x.myIngredients).Count(y => y.isCooked) == 1)
                {
                    GameManager.instance.FirstScoreHelp();
                }
            }
            yield return new WaitForSeconds(.1f);
            yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
        }
        else
        {
            yield return StartCoroutine(Slide());
            //Check for kill after slide
            if (fullRoute[routePosition].ingredient != null)
            {
                //skip the spot if experimental and cooked
                if (Settings.LoggedInPlayer.Experimental && fullRoute[routePosition].ingredient != null && fullRoute[routePosition].ingredient.isCooked)
                {
                    while (fullRoute[routePosition].ingredient != null && fullRoute[routePosition].ingredient.isCooked)
                    {
                        routePosition++;
                        yield return StartCoroutine(MoveToNextTile());
                        yield return StartCoroutine(Slide());
                    }
                }

                if (fullRoute[routePosition].ingredient != null)
                {
                    if (fullRoute[routePosition].isSafe)
                    {
                        yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
                    }
                    else //moving other ingredient
                    {
                        yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(fullRoute[routePosition].ingredient));
                    }
                }
            }
        }

        currentTile = fullRoute[routePosition];
        if (routePosition != 0)
        {
            currentTile.ingredient = this;
            GameManager.instance.setTileNull(this.name);
        }

        yield return new WaitForSeconds(.1f);
    }

    public IEnumerator MoveToNextTile(Vector3? nextPos = null, float speed = 8.5f)
    {
        var yValue = .12f;

        if (routePosition != 0)
            anim.Play("Moving");

        if(nextPos == null)
            nextPos = fullRoute[routePosition].gameObject.transform.position;
        
        if (fullRoute[routePosition].ingredient != null && GameManager.instance.Steps > 1)
        {
            yValue = .24f;
        }

        var goalPos = new Vector3(nextPos.Value.x, yValue, nextPos.Value.z);

        while (goalPos != (transform.position = Vector3.MoveTowards(transform.position, goalPos, speed * Time.deltaTime)))
        { 
            yield return null;
        }

        if (routePosition == 0 && Settings.LoggedInPlayer.WineMenu)
        {
            GameManager.instance.setWineMenuText((TeamYellow && !isCooked || !TeamYellow && isCooked), GameManager.instance.tiles.Count(x => x.ingredient != null));
        }

        yield return new WaitForSeconds(0.2f);
    }
    public void SetSelector(bool on)
    {
        selector.SetActive(on);
        hasTurn = on;
        if (on)
        {
            anim.Play("Moving");
        }
    }

    private void OnMouseDown()
    {
        if (hasTurn && !GameManager.instance.IsCPUTurn())
        {
            if (GameManager.instance.firstMoveTaken)
            {
                GameManager.instance.undoButton.interactable = false;
            }
            StartCoroutine(MoveSelectedIngredient());
        }
    }

    private IEnumerator MoveSelectedIngredient()
    {
        yield return StartCoroutine(GameManager.instance.DeactivateAllSelectors());
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