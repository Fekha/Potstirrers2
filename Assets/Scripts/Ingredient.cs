using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Ingredient : MonoBehaviour
{
    public int IngredientId = 0;
    private SqlController sql;
    public Animator anim;
    public TrailRenderer trail;
    public ParticleSystem fire;
    public ParticleSystem stomp;

    internal int routePosition = 0;
    internal int endLowerPosition = 0;
    internal int endHigherPosition = 0;
    internal int endHigherPositionWithoutSlide = 0;
    internal int endLowerPositionWithoutSlide = 0;
    internal int distanceFromScore = 0;

    [Header("Bools")]
    private int IsMovableBy; //human input
    internal bool isCooked = false;
    internal int Team;

    [Header("Selector")]
    public GameObject selector;
    public GameObject ColorQuad;
    public GameObject NormalQuad;
    public GameObject BackNormalQuad;
    public GameObject CookedQuad;
    public GameObject BackCookedQuad;
    private void Start()
    {
        //plane = new Plane(this.transform.up, Vector3.zero);
        sql = new SqlController();
    }
   

    public IEnumerator Move()
    {
        GameManager.i.AllIngredients.ForEach(x => x.SetSelector(false));

        while (GameManager.i.isMoving)
        {
            yield return new WaitForSeconds(.5f);
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
        if (routePosition != 0)
            Route.i.FullRoute[routePosition].ingredients.Pop();
        else
            GameManager.i.prepTiles.FirstOrDefault(x => x.ingredients.Any(y=>y.IngredientId==IngredientId)).ingredients.Pop();
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
                if (!GameManager.i.IsCPUTurn() && isCooked && !GameManager.i.BlockPlayerActionPanel.activeInHierarchy)
                    yield return StartCoroutine(GameManager.i.AskShouldTrash());

                if (Team != GameManager.i.activePlayer || GameManager.i.ShouldTrash == true) 
                {
                    if (!isCooked || GameManager.i.ShouldTrash == true || GameManager.i.IsCPUTurn() || GameManager.i.BlockPlayerActionPanel.activeInHierarchy)
                    {
                        if (routePosition == 9)
                        {
                            didMove = true;
                            if (!GameManager.i.IsCPUTurn() && this.Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                                CpuLogic.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan2.transform.position,false,35,true));
                        }
                        else if (routePosition == 17)
                        {
                            didMove = true;
                            if (!GameManager.i.IsCPUTurn() && this.Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                                CpuLogic.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan3.transform.position, false, 35, true));
                        }
                        yield return new WaitForSeconds(0.2f);
                        routePosition = 0;
                    }
                }
            }
           
            if (!didMove)
            {
                if (GameManager.i.Steps == 1 && routePosition == Route.i.FullRoute.Count - 2 && Team == GameManager.i.activePlayer && !isCooked) //go to pot only if its your team
                {
                    if (routePosition == Route.i.FullRoute.Count - 2) {
                        fire.Play();
                    }
                    routePosition++;
                }
                else if (routePosition != Route.i.FullRoute.Count - 2) //go forward one
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
                if (Route.i.FullRoute[routePosition].hasSpoon || Route.i.FullRoute[routePosition].hasSpatula)
                {
                    trail.enabled = true;
                }
                CpuLogic.i.ActivateShitTalk();
            }

            if (routePosition == 0)
            {
                yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this, GameManager.i.Steps == 1));
                GameManager.i.Steps--;
            }
            else if (Route.i.FullRoute[routePosition].ingredients.Count() == 0 || !Route.i.FullRoute[routePosition].ingredients.Peek().isCooked)
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
        if (Route.i.FullRoute[routePosition].hasSpoon || Route.i.FullRoute[routePosition].hasSpatula)
        {
            routePosition = routePosition + (Route.i.FullRoute[routePosition].hasSpoon ? 6 : -6 );
            yield return StartCoroutine(MoveToNextTile(null, true, 20f));
        }
    }
    private IEnumerator AfterMovement()
    {
        //Cook!
        if (routePosition == Route.i.FullRoute.Count - 1)
        {
            if (!isCooked)
            {
                trail.enabled = true;
                stomp.Play();
                isCooked = true;
                CookedQuad.gameObject.SetActive(true);
                BackCookedQuad.gameObject.SetActive(false);
            }
            routePosition = 0;
            yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this));
        }
        else
        {
            yield return StartCoroutine(Slide());
            //Check for kill after slide
            if (Route.i.FullRoute[routePosition].ingredients.Count() > 0)
            {
                //skip the spot if cooked
                if (Route.i.FullRoute[routePosition].ingredients.Count() > 0 && Route.i.FullRoute[routePosition].ingredients.Peek().isCooked)
                {
                    while (Route.i.FullRoute[routePosition].ingredients.Count() > 0 && Route.i.FullRoute[routePosition].ingredients.Peek().isCooked)
                    {
                        routePosition++;
                        yield return StartCoroutine(MoveToNextTile());
                        yield return StartCoroutine(Slide());
                    }
                }

                if (Route.i.FullRoute[routePosition].ingredients.Count() > 0)
                {
                    if (Route.i.FullRoute[routePosition].isDangerZone)
                    {
                        stomp.Play();
                        if (!GameManager.i.IsCPUTurn() && Route.i.FullRoute[routePosition].ingredients.Peek().Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                        {
                            CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                            CpuLogic.i.ActivateShitTalk();
                        }
                        var ingToMove = Route.i.FullRoute[routePosition].ingredients.Peek();
                        Route.i.FullRoute[routePosition].ingredients.Pop();
                        yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(ingToMove));
                    }
                }
            }
        }
        if (routePosition != 0)
        { 
            Route.i.FullRoute[routePosition].ingredients.Push(this);
        }
    }

    public IEnumerator MoveToNextTile(Vector3? nextPos = null, bool isforEffect=false, float speed = 35f, bool trash = false)
    {
        var yValue = .25f;

        if (isforEffect)
            anim.Play("flip");
        else if(routePosition != 0)
            anim.Play("Moving");

        if(nextPos == null)
            nextPos = Route.i.FullRoute[routePosition].gameObject.transform.position;

        if(!trash && (!Route.i.FullRoute[routePosition].isDangerZone || (Route.i.FullRoute[routePosition].isDangerZone && GameManager.i.Steps != 1)))
            yValue += (.4f * Route.i.FullRoute[routePosition].ingredients.Count());

        var goalPos = new Vector3(nextPos.Value.x, yValue, nextPos.Value.z);

        while (goalPos != (transform.position = Vector3.MoveTowards(transform.position, goalPos, speed * Time.deltaTime)))
        { 
            yield return null;
        }

        if (routePosition == 0 && Settings.LoggedInPlayer.WineMenu)
        {
            GameManager.i.setWineMenuText((Team == 0 && !isCooked || Team == 1 && isCooked), GameManager.i.prepTiles.Count(x => x.ingredients.Count() > 0));
        }

        yield return new WaitForSeconds(0.2f);
    }
    public void SetSelector(bool on)
    {
        trail.enabled = false;
        if (on)
        {
            IsMovableBy = GameManager.i.GetActivePlayer().UserId;
            anim.Play("Moving");
        }
        else
        {
            IsMovableBy = 0;
        }
        selector.SetActive(on);
    }

    private void OnMouseDown()
    {
        if (Settings.OnlineGameId != 0 && GameManager.i.GetActivePlayer().UserId != Settings.LoggedInPlayer.UserId)
        {
            return;
        }

        if (IsMovableBy == Settings.LoggedInPlayer.UserId && !GameManager.i.isMoving && !GameManager.i.IsCPUTurn() && !GameManager.i.BlockPlayerActionPanel.activeInHierarchy)
        {
        //    if (GameManager.i.firstIngredientMoved != null)
        //    {
        //        GameManager.i.undoButton1.gameObject.SetActive(false);
        //        GameManager.i.undoButton2.gameObject.SetActive(false);
        //    }
        //    else if (GameManager.i.firstMoveTaken)
        //    {
        //        if (GameManager.i.activePlayer == 0)
        //        {
        //            GameManager.i.undoButton1.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            GameManager.i.undoButton2.gameObject.SetActive(true);
        //        }
                
        //    }
        //    else
        //    {
        //        GameManager.i.undoButton1.gameObject.SetActive(false);
        //        GameManager.i.undoButton2.gameObject.SetActive(false);
        //    }
            StartCoroutine(MoveSelectedIngredient());
        }
    }

    private IEnumerator MoveSelectedIngredient()
    {
        if (Settings.OnlineGameId != 0)
            yield return StartCoroutine(sql.RequestRoutine($"analytic/UpdateTurn?UserId={Settings.LoggedInPlayer.UserId}&GameId={Settings.OnlineGameId}&IngId={IngredientId}&Higher={GameManager.i.higherMoveSelected}"));

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