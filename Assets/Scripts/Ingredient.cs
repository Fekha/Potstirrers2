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
    public GameObject TutorialArrow;
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
    public bool isCooked = false;
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
        sql = new SqlController();
    }
   

    public IEnumerator Move()
    {
        yield return StartCoroutine(BeforeMoving());

        yield return StartCoroutine(DoMovement());

        yield return StartCoroutine(AfterMovement());

        yield return StartCoroutine(GameManager.i.DoneMoving());
    }

    private IEnumerator BeforeMoving()
    {
        while (GameManager.i.isMoving)
        {
            yield return new WaitForSeconds(.5f);
        }

        yield return StartCoroutine(GameManager.i.StartedMoving()); 

        if (GameManager.i.lastMovedIngredient != null)
        {
            GameManager.i.undoButton1.gameObject.SetActive(false);
            GameManager.i.undoButton2.gameObject.SetActive(false);
        }

        anim.Play("Selected");

        GameManager.i.AllIngredients.ForEach(x => x.SetSelector(false));

        if (!Global.CPUGame && GameManager.i.GetActivePlayer().UserId == Global.LoggedInPlayer.UserId)
            StartCoroutine(sql.RequestRoutine($"multiplayer/UpdateTurn?UserId={Global.LoggedInPlayer.UserId}&GameId={Global.GameId}&IngId={IngredientId}&Higher={GameManager.i.higherMoveSelected}"));

        GameManager.i.lastMovedIngredient = IngredientId;

        if (routePosition != 0)
            Route.i.FullRoute[routePosition].ingredients.Pop();
        else
            GameManager.i.prepTiles.FirstOrDefault(x => x.ingredients.Any(y=>y.IngredientId==IngredientId)).ingredients.Pop();
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator DoMovement()
    {
        bool skipping = false;
        while (GameManager.i.Steps > 0)
        {
            while (GameManager.i.IsReading || GameManager.i.TutorialStopActions)
            {
                yield return new WaitForSeconds(0.5f);
            }

            GameManager.i.UpdateMoveText(GameManager.i.Steps);
            var isTrashed = false;

            if (GameManager.i.Steps == 1 && (routePosition == 9 || routePosition == 17))
            {
                if (!GameManager.i.IsCPUTurn() && isCooked && !GameManager.i.Automating)
                {
                    selector.SetActive(true);
                    yield return StartCoroutine(GameManager.i.AskShouldTrash());
                    selector.SetActive(false);
                }

                if (Team != GameManager.i.activePlayer || GameManager.i.ShouldTrash == true) 
                {
                    if (!isCooked || GameManager.i.ShouldTrash == true || GameManager.i.IsCPUTurn() || GameManager.i.Automating)
                    {
                        if (routePosition == 9)
                        {
                            isTrashed = true;
                            if (!GameManager.i.IsCPUTurn() && this.Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                                CpuLogic.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan2.transform.position));
                        }
                        else if (routePosition == 17)
                        {
                            isTrashed = true;
                            if (!GameManager.i.IsCPUTurn() && this.Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                            {
                                CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                                CpuLogic.i.ActivateShitTalk();
                            }
                            trail.enabled = true;
                            yield return StartCoroutine(MoveToNextTile(GameManager.i.TrashCan3.transform.position));
                        }
                        yield return new WaitForSeconds(0.2f);
                        routePosition = 0;
                    }
                }
            }
           
            if (!isTrashed)
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
                yield return StartCoroutine(GameManager.i.MoveToNextEmptySpace(this, GameManager.i.Steps == 1 || isTrashed, isTrashed));
                GameManager.i.Steps--;
                skipping = false;
            }
            else if (Route.i.FullRoute[routePosition].ingredients.Count() == 0 || !Route.i.FullRoute[routePosition].ingredients.Peek().isCooked)
            {
                yield return StartCoroutine(MoveToNextTile(skipping: skipping));
                GameManager.i.Steps--;
                skipping = false;
            }
            else
            {
                skipping = true;
            }

            GameManager.i.ShouldTrash = null;
            isTrashed = false;
        }
        if (routePosition != 0 && Route.i.FullRoute[routePosition].ingredients.Count() > 0)
        {
            if (Route.i.FullRoute[routePosition].isDangerZone)
            {
                var rot = new Vector3(0, 0, 0);
                stomp.transform.rotation = Quaternion.Euler(rot);
                stomp.Play();
            }
            else
            {
                var ingStomp = Route.i.FullRoute[routePosition].ingredients.Peek().stomp;
                var rot = new Vector3(180, 0, 0);
                ingStomp.transform.rotation = Quaternion.Euler(rot);
                ingStomp.Play();
            }
        }
        GameManager.i.UpdateMoveText();
    }
    private IEnumerator Slide() {
        if (Route.i.FullRoute[routePosition].hasSpoon || Route.i.FullRoute[routePosition].hasSpatula)
        {
            routePosition = routePosition + (Route.i.FullRoute[routePosition].hasSpoon ? 6 : -6 );
            yield return StartCoroutine(MoveToNextTile(null,true));
            if (Route.i.FullRoute[routePosition].ingredients.Count() > 0)
            {
                var ingStomp = Route.i.FullRoute[routePosition].ingredients.Peek().stomp;
                var rot = new Vector3(180, 0, 0);
                ingStomp.transform.rotation = Quaternion.Euler(rot);
                ingStomp.Play();
            }
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
                if (Team == 0)
                {
                    GameManager.i.XpText.GetComponent<Animation>().Play("CookedXP");
                }
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
                    var checkForInfinite = 0;
                    while (Route.i.FullRoute[routePosition].ingredients.Count() > 0 && Route.i.FullRoute[routePosition].ingredients.Peek().isCooked && checkForInfinite < 12)
                    {
                        checkForInfinite++;
                        routePosition++;
                        yield return StartCoroutine(MoveToNextTile(null,true));
                        yield return StartCoroutine(Slide());
                    }
                    if (checkForInfinite >= 12)
                    {
                        routePosition = 24;
                        yield return StartCoroutine(MoveToNextTile(null,true));
                    }
                }

                if (Route.i.FullRoute[routePosition].ingredients.Count() > 0)
                {
                    if (!GameManager.i.IsCPUTurn() && Route.i.FullRoute[routePosition].ingredients.Peek().Team != GameManager.i.activePlayer && string.IsNullOrEmpty(GameManager.i.talkShitText.text))
                    {
                        CpuLogic.i.PrepShitTalk(TalkType.SentBack);
                        CpuLogic.i.ActivateShitTalk();
                    }
                    if (Route.i.FullRoute[routePosition].isDangerZone)
                    {
                        var ingToMove = Route.i.FullRoute[routePosition].ingredients.Peek();
                        ingToMove.trail.enabled = true;
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

        GameManager.i.isMoving = false;
    }

    public IEnumerator MoveToNextTile(Vector3? nextPos = null, bool isforEffect = false, bool skipping = false, bool sentBack = false)
    {
        float time = .2f;
        float yValue = .25f;

        if (sentBack)
        {
            anim.Play("Flip");
            time += .55f;
        }
        else if (skipping)
        {
            anim.SetBool("StartedJumping", true);
        }
        else if (isforEffect)
        {
            anim.Play("Flip");
            time += .25f;
        } 
        else if (routePosition == 1)
        {
            anim.SetBool("StartedMoving", true);
            time += .15f;
        }
        else
        {
            anim.SetBool("StartedMoving", true);
        }

        if (nextPos == null)
        {
            nextPos = Route.i.FullRoute[routePosition].gameObject.transform.position;
        }
        
        if (!(Route.i.FullRoute[routePosition].isDangerZone && GameManager.i.Steps == 1))
        {
            yValue += (.45f * Route.i.FullRoute[routePosition].ingredients.Count());
        }

        Vector3 goalPos = new Vector3(nextPos.Value.x, yValue, nextPos.Value.z);

        float elapsedTime = 0;
        Vector3 startingPos = transform.position;
        while (elapsedTime <= time)
        {
            transform.position = Vector3.Lerp(startingPos, goalPos, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= time - .05f)
            {
                anim.SetBool("StartedJumping", false);
                anim.SetBool("StartedMoving", false);
            }
            yield return new WaitForEndOfFrame();
        }
        anim.SetBool("StartedJumping", false);
        anim.SetBool("StartedMoving", false);
        transform.position = goalPos;

        if (routePosition == 0 && Global.LoggedInPlayer.WineMenu)
        {
            GameManager.i.setWineMenuText((Team == 0 && !isCooked || Team == 1 && isCooked), GameManager.i.prepTiles.Count(x => x.ingredients.Count() > 0));
        }

        yield return new WaitForSeconds(.1f);
    }
    public void SetSelector(bool on)
    {
        trail.enabled = false;
        if (on)
        {
            var currentPlayer = GameManager.i.GetActivePlayer();
            if (currentPlayer.IsCPU || !Global.IsTutorial || GameManager.i.TutorialIngId == IngredientId)
                IsMovableBy = currentPlayer.UserId;
            else
                IsMovableBy = 0;
            if (GameManager.i.TutorialIngId == IngredientId)
                TutorialArrow.SetActive(true);
            if (!selector.activeInHierarchy)
                anim.Play("Selected");
        }
        else
        {
            IsMovableBy = 0;
            TutorialArrow.SetActive(false);
        }
        selector.SetActive(on);
    }

    private void OnMouseDown()
    {
        if (GameManager.i.GetActivePlayer().UserId == Global.LoggedInPlayer.UserId 
            && IsMovableBy == Global.LoggedInPlayer.UserId 
            && !GameManager.i.isMoving 
            && !GameManager.i.IsCPUTurn() 
            && !GameManager.i.Automating
            && !GameManager.i.TutorialStopActions)
        {
            StartCoroutine(Move());
        }
    }
}