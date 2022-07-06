
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CpuLogic : MonoBehaviour
{
    private List<Ingredient> UseableIngredients;
    private List<Ingredient> TeamIngredients;
    private List<Ingredient> UseableTeamIngredients;
    private List<Ingredient> EnemyIngredients;
    private List<Ingredient> UseableEnemyIngredients;
    private bool hasBeenDumb = false;
    internal static CpuLogic i;
    private void Awake()
    {
        i = this;
    }
    public IEnumerator FindCPUIngredientMoves()
    {
        yield return new WaitForSeconds(.5f);
        yield return new WaitForSeconds(.5f);

        if (GameManager.i.IsCPUTurn())
            yield return StartCoroutine(FindBestMove());

        if (!GameManager.i.GameOver && GameManager.i.IsCPUTurn() && (GameManager.i.IngredientMovedWithLower == null || GameManager.i.IngredientMovedWithHigher == null))
            yield return StartCoroutine(FindBestMove());

        GameManager.i.IngredientMovedWithLower = null;
        GameManager.i.IngredientMovedWithHigher = null;
        hasBeenDumb = false;
    }

    private IEnumerator SetCPUVariables()
    {
        UseableIngredients = GameManager.i.AllIngredients.Where(x => x.IngredientId != GameManager.i.firstIngredientMoved && (x.currentTile.ingredients.Peek() == x || x.routePosition == 0)).ToList();
        foreach (var ing in GameManager.i.AllIngredients)
        {
            //find what ingredients actual end will be accounting for cooked ingredients
            ing.endHigherPositionWithoutSlide = ing.routePosition + GameManager.i.higherMove;
            ing.endLowerPositionWithoutSlide = ing.routePosition + GameManager.i.lowerMove;
            ing.distanceFromScore = 0;
            for (int i = ing.routePosition + 1; i <= ing.endHigherPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i % 26].ingredients.Count() > 0 && ing.fullRoute[i % 26].ingredients.Peek().isCooked)
                {
                    ing.endHigherPositionWithoutSlide++;
                }
            }
            for (int i = ing.routePosition + 1; i <= ing.endLowerPositionWithoutSlide; i++)
            {
                if (ing.fullRoute[i % 26].ingredients.Count() > 0 && ing.fullRoute[i % 26].ingredients.Peek().isCooked)
                {
                    ing.endLowerPositionWithoutSlide++;
                }
            }

            for (int i = ing.routePosition + 1; i <= 26; i++)
            {
                if (ing.fullRoute[i % 26].ingredients.Count() == 0 || !ing.fullRoute[i % 26].ingredients.Peek().isCooked)
                {
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
        TeamIngredients = GameManager.i.AllIngredients.Where(x => x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow).ToList();
        EnemyIngredients = GameManager.i.AllIngredients.Where(x => x.TeamYellow != GameManager.i.GetActivePlayer().TeamYellow).ToList();

        UseableIngredients = UseableIngredients.OrderBy(x => x.isCooked).ThenBy(x => x.distanceFromScore).ToList();
        UseableTeamIngredients = UseableIngredients.Where(x => x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow).ToList();
        UseableEnemyIngredients = UseableIngredients.Where(x => x.TeamYellow != GameManager.i.GetActivePlayer().TeamYellow).ToList();

        //If reading wait
        while (GameManager.i.IsReading)
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
            ?? MoveOffStackToScore()
            ?? BeDumb()
            ?? MovePastPrep()
            ?? StompEnemy(true)
            ?? GoToTrash()
            ?? StompEnemy(false)
            ?? MoveIntoScoring()
            ?? Slide(false)
            ?? BoostWithCookedIngredient()
            ?? MoveFrontMostEnemy()
            ?? Slide(true)
            ?? StackEnemy(true)
            ?? StackEnemy(false)
            ?? MoveOffStack(false)
            ?? MoveFrontMostIngredient(false, false)
            ?? MoveOffStack(true)
            ?? MoveFrontMostIngredient(true, false)
            ?? MoveFrontMostIngredient(false, true)
            ?? MoveNotPastPrep()
            ?? MoveCookedPastPrep()
            ?? MoveEnemyIngredient()
            ?? MoveRandomly();
        yield return StartCoroutine(MoveCPUIngredient(ingredientToMove));
    }
    internal IEnumerator MoveCPUIngredient(Ingredient ingredientToMove)
    {
        if (ingredientToMove == null)
        {
            GameManager.i.firstMoveTaken = true;
            yield return StartCoroutine(GameManager.i.DoneMoving());
        }
        else
        {
            yield return StartCoroutine(GameManager.i.RollSelected(ingredientToMove == GameManager.i.IngredientMovedWithHigher, false));
            if (!GameManager.i.firstMoveTaken)
            {
                GameManager.i.IngredientMovedWithHigher = null;
            }
            yield return new WaitForSeconds(.5f);
            yield return StartCoroutine(GameManager.i.DeactivateAllSelectors());
            yield return StartCoroutine(ingredientToMove.Move());
        }
    }
    internal void ActivateShitTalk()
    {
        if (Settings.OnlineGameId != 0)
            return;

        if (!string.IsNullOrEmpty(GameManager.i.talkShitText.text) && !GameManager.i.TalkShitPanel.activeInHierarchy)
        {
            GameManager.i.talkingTimeStart = Time.time;
            GameManager.i.TalkShitPanel.SetActive(true);
        }
    }
    internal void PrepShitTalk(TalkType talk)
    {
        if (Settings.OnlineGameId != 0 || !string.IsNullOrEmpty(GameManager.i.talkShitText.text))
            return;

        var username = GameManager.i.GetActivePlayer().player.Username;
        switch (talk)
        {
            case TalkType.MoveRandomly:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Hmm, you stumped me!";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "It really didn't matter...";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#ImNotEvenTrying";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "You did't leave me any good moves!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Trash:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "My pa paw taught me to take out the trash.";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "Go back where you belong!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#YouAreTrash";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "Watch out for the trash cans!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Stomped:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Stomped!";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "Have fun in Prep...";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#SorryNotSorry";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "Oops, didn't see you there!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.StompedBySelf:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Self Stomp!";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "Stop hitting yourself.";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#GetRekt";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "Oh no, I was trying to help!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.SafeZoned:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Safe for me, not you!";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "You owe me one for moving you...";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#SickBurn";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "I'm just teaching you how the safe zone works";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.Cook:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "My me maw taught me to cook like this.";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "Watch and learn!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#Winning";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "This is fun!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.HelpCook:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Alley Oop!";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "This is my final form!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#StrategicAF";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "Teamwork makes the dreamwork!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.MovePastPrep:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "You know what they say...";
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = "HAHA you got too close to the end!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#ByeFelicia";
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = "I'm sorry, I just had to!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.SentBack:
                username = GameManager.i.playerList.FirstOrDefault(x => x.player.playerType == PlayerTypes.CPU).player.Username;

                var ZachOptions = new List<string>() { "", "Man it sucks to suck...", "Dag Nabbit!", "What do you think your doing!?" };
                var JoeOptions = new List<string>() { "", "Wait, you can't do that to me!!", "Watch your back!", "I'll remember that!" };
                var JennOptions = new List<string>() { "", "#Oooof", "#Toxic", "#OhNoYouDidnt" };
                var ChrissyOptions = new List<string>() { "", "Well that wasn't very nice!", "Hey, quit doing that!", "Treat others how you want to be treated..." };
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = ZachOptions[Random.Range(0, ZachOptions.Count())];
                        break;
                    case "Joe":
                        GameManager.i.talkShitText.text = JoeOptions[Random.Range(0, JoeOptions.Count())];
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = JennOptions[Random.Range(0, JennOptions.Count())];
                        break;
                    case "Chrissy":
                        GameManager.i.talkShitText.text = ChrissyOptions[Random.Range(0, ChrissyOptions.Count())];
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
        if (UseableTeamIngredients.Count > 0)
        {
            if (GameManager.i.IngredientMovedWithHigher == null)
            {

                GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (GameManager.i.IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return GameManager.i.IngredientMovedWithHigher;
                }
            }

            if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
            {
                GameManager.i.IngredientMovedWithLower = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (GameManager.i.IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return GameManager.i.IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveNotPastPrep()
    {
        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endHigherPosition));
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Not Past Prep"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }

        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endLowerPosition));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Not Past Prep"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveIntoScoring()
    {
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 23
            && x.endLowerPosition > 17
            && !x.isCooked
            && x.distanceFromScore > 8
            && CanMoveSafely(x, x.endLowerPosition));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Into Scoring"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 23
            && x.endHigherPosition > 17
            && !x.isCooked
            && x.distanceFromScore > 8
            && CanMoveSafely(x, x.endHigherPosition));
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Into Scoring"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }
        return null;
    }

    //private Ingredient MoveCookedIngredient()
    //{
    //    if (GameManager.i.IngredientMovedWithHigher == null)
    //    {
    //        GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endHigherPosition < 26
    //        && CanMoveSafely(x, x.endHigherPosition));
    //        if (GameManager.i.IngredientMovedWithHigher != null)
    //        {
    //            if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Cooked Ingredient"; }
    //            return GameManager.i.IngredientMovedWithHigher;
    //        }
    //    }
    //    if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
    //    {
    //        GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endLowerPosition < 26
    //        && CanMoveSafely(x, x.endLowerPosition));
    //        if (GameManager.i.IngredientMovedWithLower != null)
    //        {
    //            if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Cooked Ingredient"; }
    //            return GameManager.i.IngredientMovedWithLower;
    //        }
    //    }
    //    return null;
    //}  
    private Ingredient BoostWithCookedIngredient()
    {
        if (!UseableIngredients.Any(x => x.isCooked) || GameManager.i.IngredientMovedWithHigher != null || GameManager.i.IngredientMovedWithLower != null)
            return null;

        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            var CookedIngredientsThatCanHelp = UseableTeamIngredients.Where(x => x.isCooked
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endLowerPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endHigherPosition && y.endLowerPosition >= x.endHigherPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endLowerPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endLowerPosition > x.routePosition && y.endLowerPosition < x.endHigherPosition))).ToList();

            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                GameManager.i.IngredientMovedWithHigher = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x, x.endHigherPosition));
                if (GameManager.i.IngredientMovedWithHigher != null)
                {
                    if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Boosted with Cooked"; }
                    return GameManager.i.IngredientMovedWithHigher;
                }
            }
        }

        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            var CookedIngredientsThatCanHelp = UseableIngredients.Where(x => x.isCooked
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endHigherPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endLowerPosition && y.endHigherPosition >= x.endLowerPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y, y.endHigherPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endHigherPosition > x.routePosition && y.endHigherPosition < x.endLowerPosition))).ToList();
            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                GameManager.i.IngredientMovedWithLower = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x, x.endLowerPosition));
                if (GameManager.i.IngredientMovedWithLower != null)
                {
                    if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Boosted with Cooked"; }
                    return GameManager.i.IngredientMovedWithLower;
                }
            }
        }
        return null;
    }

    private bool CanMoveSafely(Ingredient x, int endPosition)
    {
        return !TeamIngredients.Any(y => y.routePosition == endPosition % 26) && (!x.fullRoute[endPosition % 26].isSafe || x.fullRoute[endPosition % 26].ingredients.Count() == 0);
    }

    private Ingredient MoveCookedPastPrep()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endHigherPosition));
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Cooked Past Prep"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
                && x.fullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x, x.endLowerPosition));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Cooked Past Prep"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveFrontMostIngredient(bool withCooked, bool moveStacked)
    {
        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.OrderByDescending(x => x.endHigherPosition).FirstOrDefault(x => x.endHigherPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 8 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.fullRoute[x.routePosition].ingredients.Count == 1)
            && CanMoveSafely(x, x.endHigherPosition)); //Dont stomp on safe area
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Front Most " + (withCooked ? "cooked " : "uncooked ") + (moveStacked ? "stacked " : "unstacked ") + "Ingredient"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }

        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.endLowerPosition).FirstOrDefault(x => x.endLowerPosition < 23 //Dont move past prep
            && (x.distanceFromScore > 8 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.fullRoute[x.routePosition].ingredients.Count == 1)
            && CanMoveSafely(x, x.endLowerPosition)); //Dont stomp on safe area
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Front Most Ingredient"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveEnemyIngredient()
    {

        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove && UseableTeamIngredients.Count(x => x.routePosition == 0) == 0)
        {
            GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.OrderBy(x => x.endLowerPosition).FirstOrDefault(x =>
            !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26 && !x.fullRoute[x.endLowerPosition % 26].isSafe));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Enemy"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveOffStack(bool notInScoring = true)
    {
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => (notInScoring || x.distanceFromScore < 9)
            && (x.currentTile.ingredients.Count > 1 && x.currentTile.ingredients.Any(x => x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow))
            && CanMoveSafely(x, x.endLowerPosition)); //Dont stomp on safe area
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Off stack" + (notInScoring ? " from non-scoring" : " from scoring"); }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveOffStackToScore()
    {
        if (GameManager.i.IngredientMovedWithHigher == null && GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            var ingredientThatCouldScore = TeamIngredients.FirstOrDefault(x => x.endHigherPosition == 26 && !x.isCooked && x.fullRoute[x.routePosition].ingredients.Peek() != x);
            if (ingredientThatCouldScore != null)
            {
                GameManager.i.IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x.routePosition == ingredientThatCouldScore.routePosition && x.endLowerPosition != x.routePosition); //Dont stomp on safe area
                if (GameManager.i.IngredientMovedWithLower != null)
                {
                    if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move off stack to score"; }
                    return GameManager.i.IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveFrontMostEnemy()
    {
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove && EnemyIngredients.Count(x => !x.isCooked) == 1)
        {
            GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.distanceFromScore < 9 //move from scoring position
            && !x.isCooked
            && !x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula
            && !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26)); //Dont stomp yourself
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Front Most Enemy"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient Slide(bool WithCooked)
    {
        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 8 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x, x.endHigherPosition)
            && ((x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || x.fullRoute[x.endHigherPositionWithoutSlide % 26].hasSpoon));
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Slide"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 8 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x, x.endHigherPosition)
            && ((x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || x.fullRoute[x.endLowerPositionWithoutSlide % 26].hasSpoon));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Slide"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient GoToTrash()
    {
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.routePosition != 0
            && !x.isCooked
            && ((GameManager.i.lowerMove < 6 && (x.endLowerPositionWithoutSlide % 26) == 10) || (x.endLowerPositionWithoutSlide % 26) == 18));
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.Trash);
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient StompEnemy(bool useEither = true)
    {
        var teamToMove = useEither ? UseableTeamIngredients : UseableEnemyIngredients;
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation unless cooked
                && (x.routePosition != 0 || x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
                && x.endLowerPosition != 0
                && x.endLowerPosition != x.routePosition
                && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
                && x.fullRoute[x.endLowerPosition % 26].isSafe); //stomp safe area if someone is there
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return GameManager.i.IngredientMovedWithLower;
            }
        }

        if (useEither && GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && x.fullRoute[x.endHigherPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return GameManager.i.IngredientMovedWithHigher;
            }
        }

        return null;
    }
    private Ingredient StackEnemy(bool useEither = true)
    {
        var teamToMove = useEither ? UseableTeamIngredients : UseableEnemyIngredients;
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation
                && (x.routePosition != 0 || x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
                && x.endLowerPosition != 0
                && x.endLowerPosition != x.routePosition
                && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
                && x.fullRoute[x.endLowerPosition % 26].ingredients.Peek().TeamYellow != GameManager.i.GetActivePlayer().TeamYellow // if enemy is on top
                && !x.fullRoute[x.endLowerPosition % 26].isSafe); //stomp safe area if someone is there
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Stack"; }
                return GameManager.i.IngredientMovedWithLower;
            }
        }

        if (useEither && GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.TeamYellow == GameManager.i.GetActivePlayer().TeamYellow) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && x.fullRoute[x.endHigherPosition % 26].ingredients.Peek().TeamYellow != GameManager.i.GetActivePlayer().TeamYellow // if enemy is on top
            && !x.fullRoute[x.endHigherPosition % 26].isSafe); //Dont stomp safe area if someone is there
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Stack"; }
                return GameManager.i.IngredientMovedWithHigher;
            }
        }

        return null;
    }
    //private Ingredient StompSafeZone()   
    //{
    //    if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
    //    {
    //        GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
    //        && x.routePosition != 0 //dont move them from prep
    //        && GameManager.i.AllIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition % 26) //anyone there?
    //        && x.fullRoute[x.endLowerPosition % 26].isSafe);
    //        if (GameManager.i.IngredientMovedWithLower != null)
    //        {
    //            PrepShitTalk(TalkType.SafeZoned);
    //            return GameManager.i.IngredientMovedWithLower;
    //        }
    //    }
    //    return null;
    //}
    private Ingredient CookIngredient()
    {
        if (GameManager.i.IngredientMovedWithHigher == null)
        {
            GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition == 26 && !x.isCooked);
            if (GameManager.i.IngredientMovedWithHigher != null)
            {
                PrepShitTalk(TalkType.Cook);
                return GameManager.i.IngredientMovedWithHigher;
            }
        }
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition == 26 && !x.isCooked); //Move into pot if not cooked
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.Cook);
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient HelpScore()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        Ingredient ScoreableIng = null;
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove && GameManager.i.IngredientMovedWithHigher == null)
        {
            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 25);
            if (ScoreableIng != null)
            {
                GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng
                && x.isCooked
                && x.routePosition < ScoreableIng.routePosition
                && x.endHigherPosition > ScoreableIng.routePosition
                && x.endHigherPosition < 26
                && CanMoveSafely(x, x.endHigherPosition));
                if (GameManager.i.IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return GameManager.i.IngredientMovedWithHigher;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 25);

            if (ScoreableIng != null)
            {
                GameManager.i.IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x != ScoreableIng
                && x.isCooked
                && x.routePosition < ScoreableIng.routePosition
                && x.endLowerPosition > ScoreableIng.routePosition
                && x.endLowerPosition < 26
                && CanMoveSafely(x, x.endLowerPosition));
                if (GameManager.i.IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return GameManager.i.IngredientMovedWithLower;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 27);
            if (ScoreableIng != null)
            {
                GameManager.i.IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng
                && x.isCooked
                && x.routePosition > ScoreableIng.routePosition
                && x.endHigherPosition % 26 < ScoreableIng.routePosition
                && CanMoveSafely(x, x.endHigherPosition));
                if (GameManager.i.IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return GameManager.i.IngredientMovedWithHigher;
                }
            }

            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endHigherPosition == 27);
            if (ScoreableIng != null)
            {
                GameManager.i.IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x != ScoreableIng
                && x.isCooked
                && x.routePosition > ScoreableIng.routePosition
                && x.endLowerPosition % 26 < ScoreableIng.routePosition
                && CanMoveSafely(x, x.endLowerPosition));
                if (GameManager.i.IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.HelpCook);
                    return GameManager.i.IngredientMovedWithLower;
                }
            }
        }
        return null;
    }

    private Ingredient MovePastPrep()
    {
        if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
        {
            GameManager.i.IngredientMovedWithLower = UseableEnemyIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => x.endLowerPosition >= 26
            && !x.isCooked); //Move enemy past pot if uncooked
            if (GameManager.i.IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.MovePastPrep);
                return GameManager.i.IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient BeDumb()
    {
        if (!Settings.IsDebug && !Settings.HardMode && (Settings.LoggedInPlayer.Wins == 0 || (!hasBeenDumb && (Random.Range(0, Mathf.Min(Settings.LoggedInPlayer.Wins, 50)) == 0))))
        {
            if (GameManager.i.IngredientMovedWithHigher == null)
            {
                var ingsToMove = UseableTeamIngredients.Where(x => x.distanceFromScore > 8).ToList();
                if (ingsToMove.Count() > 0)
                {
                    var toMove = ingsToMove[Random.Range(0, ingsToMove.Count())];
                    hasBeenDumb = true;
                    GameManager.i.IngredientMovedWithHigher = toMove;
                    return GameManager.i.IngredientMovedWithHigher;
                }
            }

            if (GameManager.i.IngredientMovedWithLower == null && GameManager.i.higherMove != GameManager.i.lowerMove)
            {
                var ingsToMove = UseableIngredients.Where(x => x.distanceFromScore > 8).ToList();
                if (ingsToMove.Count() > 0)
                {
                    var toMove = ingsToMove[Random.Range(0, ingsToMove.Count())];
                    hasBeenDumb = true;
                    GameManager.i.IngredientMovedWithLower = toMove;
                    return GameManager.i.IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
}

