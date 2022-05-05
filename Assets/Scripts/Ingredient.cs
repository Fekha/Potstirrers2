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
    public string ingredientName;
    public int routePosition;
    public bool TeamYellow;
    private int startNodeIndex;
    
    [Header("Bools")]
    bool isMoving;
    bool isAnimating;
    bool hasTurn; //human input
    public bool isCooked;

    [Header("Selector")]
    public GameObject selector;

    public GameObject Material;
    public GameObject NormalQuad;
    public GameObject CookedQuad;
    Plane plane;
    private void Start()
    {
        plane = new Plane(this.transform.up, Vector3.zero);
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
       
        while (GameManager.instance.Steps > 0)
        {
            GameManager.instance.UpdateMoveText(GameManager.instance.Steps);

            yield return StartCoroutine(DoMovement());
            GameManager.instance.Steps--;
        }

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
            currentTile.isTaken = false;
        }
        else
        {
            yield return StartCoroutine(GameManager.instance.setTileNull(this.name));
        }
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator DoMovement()
    {
        if (GameManager.instance.Steps == 1 && TeamYellow != GameManager.instance.GetActivePlayer().TeamYellow && (routePosition == 9 || routePosition == 17))
        {
            if (routePosition == 9)
            {
                yield return StartCoroutine(MoveToNextTile(GameManager.instance.TrashCan2.transform.position));
            }
            else if (routePosition == 17)
            {
                yield return StartCoroutine(MoveToNextTile(GameManager.instance.TrashCan3.transform.position));
            }
            yield return new WaitForSeconds(0.2f);
            routePosition = 0;
        }
        else if (GameManager.instance.Steps == 1 && routePosition == fullRoute.Count - 2 && TeamYellow == GameManager.instance.GetActivePlayer().TeamYellow) //go to pot only if its your team
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

        if (routePosition == 0)
        {
            yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
        }
        else
        {
            yield return StartCoroutine(MoveToNextTile());
        }
    }

    private IEnumerator AfterMovement()
    {
        GameManager.instance.UpdateMoveText();

        if (fullRoute[routePosition].hasSpoon)
        {
            //move to thermometor tip
            routePosition = routePosition + 6;
            yield return StartCoroutine(MoveToNextTile(null,11f));
        }
        else if (fullRoute[routePosition].hasSpatula)
        {
            //move to thermometor tip
            routePosition = routePosition - 6;
            yield return StartCoroutine(MoveToNextTile(null, 11f));
        }
        else if (routePosition == fullRoute.Count - 1)
        {
            Ingredient IngredientToCook = null;
            if (!isCooked)
            {
                IngredientToCook = this;
            }
            else
            {
                IngredientToCook = GameManager.instance.GetActivePlayer().myIngredients.FirstOrDefault(x => !x.isCooked);
            }
            if (IngredientToCook != null)
            {
                IngredientToCook.isCooked = true;
                IngredientToCook.anim.Play("flip");
                IngredientToCook.CookedQuad.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(.1f);
            yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
        }
        
        //cant be else if because slide then kill
        if (fullRoute[routePosition].isTaken)
        {
            if (fullRoute[routePosition].isSafe)
            {
                yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(this));
            }
            else //moving other ingredient
            {
                var landedOnIngredient = fullRoute[routePosition].ingredient;
                //landedOnIngredient.startTurnPos = routePosition;
                //landedOnIngredient.startTurnTile = fullRoute[routePosition];
                //GameManager.instance.LastLandedOnIngredient = landedOnIngredient;
                yield return StartCoroutine(GameManager.instance.MoveToNextEmptySpace(landedOnIngredient));
            }
        }

        currentTile = fullRoute[routePosition];
        if (routePosition != 0)
        {
            currentTile.ingredient = this;
            currentTile.isTaken = true;
        }

        yield return new WaitForSeconds(.1f);
    }

    public IEnumerator MoveToNextTile(Vector3? nextPos = null, float speed = 9f)
    {
        if(routePosition != 0)
            anim.Play("Moving");

        if(nextPos == null)
            nextPos = fullRoute[routePosition].gameObject.transform.position;

        var yValue = .12f;
        if (fullRoute[routePosition].isTaken && GameManager.instance.Steps > 1)
        {
            speed = 12f;
            yValue = .34f;
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