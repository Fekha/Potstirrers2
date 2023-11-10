
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
    internal Ingredient IngredientMovedWithLower;
    internal Ingredient IngredientMovedWithHigher;
    private bool hasBeenDumb = false;
    private bool SecondMoveDouble = false;
    internal static CpuLogic i;
    private void Awake()
    {
        i = this;
    }
    public IEnumerator FindCPUIngredientMoves()
    {
        yield return new WaitForSeconds(.5f);

        yield return StartCoroutine(FindBestMove());

        if (GameManager.i.DoublesRolled())
        {
            SecondMoveDouble = true;
            GameManager.i.higherMoveSelected = null;
            IngredientMovedWithHigher = null;
            IngredientMovedWithLower = null;
        }

        yield return new WaitForSeconds(.5f);

        if (!GameManager.i.GameOver && GameManager.i.IsCPUTurn() && (IngredientMovedWithLower == null || IngredientMovedWithHigher == null))
            yield return StartCoroutine(FindBestMove());

        IngredientMovedWithLower = null;
        IngredientMovedWithHigher = null;
        hasBeenDumb = false;
        SecondMoveDouble = false;
    }

    private IEnumerator SetCPUVariables()
    {
        UseableIngredients = GameManager.i.AllIngredients.Where(x => (x.IngredientId != GameManager.i.lastMovedIngredient || GameManager.i.DoublesRolled())  && (x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Peek() == x)).ToList();
        foreach (var ing in GameManager.i.AllIngredients)
        {
            //find what ingredients actual end will be accounting for cooked ingredients
            ing.endHigherPositionWithoutSlide = ing.routePosition + GameManager.i.higherMove;
            ing.endLowerPositionWithoutSlide = ing.routePosition + GameManager.i.lowerMove;
            ing.distanceFromScore = 0;
            for (int i = ing.routePosition + 1; i <= ing.endHigherPositionWithoutSlide; i++)
            {
                if (Route.i.FullRoute[i % 26].ingredients.Count() > 0 && Route.i.FullRoute[i % 26].ingredients.Peek().isCooked)
                {
                    ing.endHigherPositionWithoutSlide++;
                }
            }
            for (int i = ing.routePosition + 1; i <= ing.endLowerPositionWithoutSlide; i++)
            {
                if (Route.i.FullRoute[i % 26].ingredients.Count() > 0 && Route.i.FullRoute[i % 26].ingredients.Peek().isCooked)
                {
                    ing.endLowerPositionWithoutSlide++;
                }
            }

            for (int i = ing.routePosition + 1; i <= 26; i++)
            {
                if (Route.i.FullRoute[i % 26].ingredients.Count() == 0 || !Route.i.FullRoute[i % 26].ingredients.Peek().isCooked)
                {
                    ing.distanceFromScore++;
                }
            }

            //account for slides
            if (Route.i.FullRoute[ing.endLowerPositionWithoutSlide % 26].hasSpoon)
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide + 6;
            }
            else if (Route.i.FullRoute[ing.endLowerPositionWithoutSlide % 26].hasSpatula)
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide - 6;
            }
            else
            {
                ing.endLowerPosition = ing.endLowerPositionWithoutSlide;
            }

            if (Route.i.FullRoute[ing.endHigherPositionWithoutSlide % 26].hasSpoon)
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide + 6;
            }
            if (Route.i.FullRoute[ing.endHigherPositionWithoutSlide % 26].hasSpatula)
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide - 6;
            }
            else
            {
                ing.endHigherPosition = ing.endHigherPositionWithoutSlide;
            }
        }
        //create subsets based on new info
        TeamIngredients = GameManager.i.AllIngredients.Where(x => x.Team == GameManager.i.activePlayer).ToList();
        EnemyIngredients = GameManager.i.AllIngredients.Where(x => x.Team != GameManager.i.activePlayer).ToList();

        UseableIngredients = UseableIngredients.OrderBy(x => x.isCooked).ThenBy(x => x.distanceFromScore).ToList();
        UseableTeamIngredients = UseableIngredients.Where(x => x.Team == GameManager.i.activePlayer).ToList();
        UseableEnemyIngredients = UseableIngredients.Where(x => x.Team != GameManager.i.activePlayer).ToList();
        while (GameManager.i.IsReading || GameManager.i.TutorialStopActions)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator FindBestMove()
    {
        yield return StartCoroutine(SetCPUVariables());
        //TODO add HelpStomp
        var ingredientToMove = CookIngredient()
            ?? CookIngredientWithDoubleMove()
            ?? HelpScore()
            ?? MoveOffStackToScore()
            ?? BeDumb()
            ?? MovePastPrep()
            ?? MoveOffLastPiece()
            ?? StompEnemy(true)
            ?? GoToTrash()
            ?? StompEnemy(false)
            ?? MoveIntoScoring()
            ?? Slide(false)
            ?? MoveFrontMostEnemy()
            ?? StackEnemy(true)
            ?? MoveOffStack(false)
            ?? MoveFrontMostIngredient(false, false)
            ?? Slide(true)
            ?? MoveOffStack(true)
            ?? BoostWithCookedIngredient()
            ?? MoveFrontMostIngredient(true, false)
            ?? MoveFrontMostIngredient(false, true)
            ?? StackEnemy(false)
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
            yield return new WaitForSeconds(.1f);
            GameManager.i.SwitchPlayer();
        }
        else
        {
            var selected = GameManager.i.DoublesRolled() ? SecondMoveDouble : ingredientToMove == IngredientMovedWithHigher;
            yield return StartCoroutine(GameManager.i.RollSelected(selected, false));
            yield return new WaitForSeconds(.5f);
            yield return StartCoroutine(ingredientToMove.Move());
        }
    }
    internal void ActivateShitTalk()
    {
        if (!Global.CPUGame)
            return;

        if (!string.IsNullOrEmpty(GameManager.i.talkShitText.text) && !GameManager.i.TalkShitPanel.activeInHierarchy)
        {
            GameManager.i.talkingTimeStart = Time.time;
            GameManager.i.TalkShitPanel.SetActive(true);
        }
    }
    internal void PrepShitTalk(TalkType talk)
    {
        if (!Global.CPUGame || Global.IsTutorial || !string.IsNullOrEmpty(GameManager.i.talkShitText.text) || !GameManager.i.playerList.Any(x => x.IsCPU))
            return;

        var username = GameManager.i.GetActivePlayer().Username;
        switch (talk)
        {
            case TalkType.MoveRandomly:
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = "Hmm, you stumped me!";
                        break;
                    case "Ethan":
                        GameManager.i.talkShitText.text = "It really didn't matter...";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#ImNotEvenTrying";
                        break;
                    case "Mike":
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "Go back where you belong!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#YouAreTrash";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "What did I teach you about the trash cans!";
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "Have fun in Prep...";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#SorryNotSorry";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "It's called a DANGER zone for a reason!";
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "Stop hitting yourself!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#GetRekt";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "Oh no, was that your ingredient!?";
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "Watch and learn!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#Winning";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "I'm catching up!";
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "This is my final form!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#StrategicAF";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "This is a lesson on Teamwork!";
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
                    case "Ethan":
                        GameManager.i.talkShitText.text = "HAHA you got too close to the end!";
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = "#ByeFelicia";
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = "I'm sorry, I couldn't resist!";
                        break;
                    default:
                        break;
                }
                break;
            case TalkType.SentBack:
                username = GameManager.i.playerList.FirstOrDefault(x => x.IsCPU).Username;

                var EthanOptions = new List<string>() { "", "Man it sucks to suck...", "Dag Nabbit!", "What do you think your doing!?" };
                var JoeOptions = new List<string>() { "", "Wait, you can't do that to me!!", "Watch your back!", "I'll remember that!" };
                var JennOptions = new List<string>() { "", "#Oooof", "#Toxic", "#OhNoYouDidnt" };
                var MikeOptions = new List<string>() { "", "Hey, who taught you that!", "Oh man, I underestimated you!", "Treat others how you want to be treated..." };
                switch (username)
                {
                    case "Zach":
                        GameManager.i.talkShitText.text = EthanOptions[Random.Range(0, EthanOptions.Count())];
                        break;
                    case "Ethan":
                        GameManager.i.talkShitText.text = JoeOptions[Random.Range(0, JoeOptions.Count())];
                        break;
                    case "Jenn":
                        GameManager.i.talkShitText.text = JennOptions[Random.Range(0, JennOptions.Count())];
                        break;
                    case "Mike":
                        GameManager.i.talkShitText.text = MikeOptions[Random.Range(0, MikeOptions.Count())];
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
            if (IngredientMovedWithHigher == null)
            {

                IngredientMovedWithHigher = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (IngredientMovedWithHigher != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return IngredientMovedWithHigher;
                }
            }

            if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
            {
                IngredientMovedWithLower = UseableTeamIngredients[Random.Range(0, UseableTeamIngredients.Count())];
                if (IngredientMovedWithLower != null)
                {
                    PrepShitTalk(TalkType.MoveRandomly);
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveNotPastPrep()
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 26
            && Route.i.FullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Not Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 26
            && Route.i.FullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Not Past Prep"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveIntoScoring()
    {
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.endLowerPosition < 22
            && x.endLowerPosition > 16
            && !x.isCooked
            && x.distanceFromScore > 9
            && CanMoveSafely(x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Into Scoring"; }
                return IngredientMovedWithLower;
            }
        }
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x.endHigherPosition < 22
            && x.endHigherPosition > 16
            && !x.isCooked
            && x.distanceFromScore > 9
            && CanMoveSafely(x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Into Scoring"; }
                return IngredientMovedWithHigher;
            }
        }
        return null;
    }

    //private Ingredient MoveCookedIngredient()
    //{
    //    if (IngredientMovedWithHigher == null)
    //    {
    //        IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endHigherPosition < 26
    //        && CanMoveSafely(x.endHigherPosition));
    //        if (IngredientMovedWithHigher != null)
    //        {
    //            if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Cooked Ingredient"; }
    //            return IngredientMovedWithHigher;
    //        }
    //    }
    //    if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
    //    {
    //        IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
    //        && x.endLowerPosition < 26
    //        && CanMoveSafely(x.endLowerPosition));
    //        if (IngredientMovedWithLower != null)
    //        {
    //            if (Settings.IsDebug) { GameManager.i.talkShitText.text = "Move Cooked Ingredient"; }
    //            return IngredientMovedWithLower;
    //        }
    //    }
    //    return null;
    //}  
    private Ingredient BoostWithCookedIngredient()
    {
        if (!UseableIngredients.Any(x => x.isCooked) || IngredientMovedWithHigher != null || IngredientMovedWithLower != null)
            return null;

        if (IngredientMovedWithHigher == null)
        {
            var CookedIngredientsThatCanHelp = UseableTeamIngredients.Where(x => x.isCooked
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y.endLowerPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endHigherPosition && y.endLowerPosition >= x.endHigherPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && y.routePosition != 0 && CanMoveSafely(y.endLowerPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endLowerPosition > x.routePosition && y.endLowerPosition < x.endHigherPosition))).ToList();

            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                IngredientMovedWithHigher = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x.endHigherPosition));
                if (IngredientMovedWithHigher != null)
                {
                    if (Global.IsDebug) { GameManager.i.talkShitText.text = "Boosted with Cooked"; }
                    return IngredientMovedWithHigher;
                }
            }
        }

        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            var CookedIngredientsThatCanHelp = UseableIngredients.Where(x => x.isCooked
                && (UseableTeamIngredients.Where(y => !y.isCooked && CanMoveSafely(y.endHigherPosition + 1)).Any(y => y.routePosition >= x.routePosition && y.routePosition < x.endLowerPosition && y.endHigherPosition >= x.endLowerPosition)
                    || UseableTeamIngredients.Where(y => !y.isCooked && y.routePosition != 0 && CanMoveSafely(y.endHigherPosition - 1)).Any(y => y.routePosition < x.routePosition && y.endHigherPosition > x.routePosition && y.endHigherPosition < x.endLowerPosition))).ToList();
            if (CookedIngredientsThatCanHelp.Count() > 0)
            {
                IngredientMovedWithLower = CookedIngredientsThatCanHelp.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => CanMoveSafely(x.endLowerPosition));
                if (IngredientMovedWithLower != null)
                {
                    if (Global.IsDebug) { GameManager.i.talkShitText.text = "Boosted with Cooked"; }
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }

    private bool CanMoveSafely(int endPosition)
    {
        if (endPosition < 0)
        {
            return false;
        }
        return !TeamIngredients.Any(y => y.routePosition == endPosition % 26) && (endPosition % 26 == 0 || !Route.i.FullRoute[endPosition % 26].isDangerZone || Route.i.FullRoute[endPosition % 26].ingredients.Count() == 0);
    }

    private Ingredient MoveCookedPastPrep()
    {
        if (!UseableIngredients.Any(x => x.isCooked))
            return null;

        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
            && Route.i.FullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x.endHigherPosition));
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.isCooked
                && Route.i.FullRoute[x.routePosition].ingredients.Count == 1
            && CanMoveSafely(x.endLowerPosition));
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Cooked Past Prep"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }

    private Ingredient MoveFrontMostIngredient(bool withCooked, bool moveStacked)
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderByDescending(x => x.endHigherPosition).FirstOrDefault(x => x.endHigherPosition < 22 //Dont move past prep
            && (x.distanceFromScore > 9 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Count == 1)
            && CanMoveSafely(x.endHigherPosition)); //Dont stomp on safe area
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Front Most " + (withCooked ? "cooked " : "uncooked ") + (moveStacked ? "stacked " : "unstacked ") + "Ingredient"; }
                return IngredientMovedWithHigher;
            }
        }

        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderByDescending(x => x.endLowerPosition).FirstOrDefault(x => x.endLowerPosition < 22 //Dont move past prep
            && (x.distanceFromScore > 9 || withCooked) //Dont move from scoring position unless cooked
            && x.isCooked == withCooked
            && (moveStacked || x.routePosition == 0 || Route.i.FullRoute[x.routePosition].ingredients.Count == 1)
            && CanMoveSafely(x.endLowerPosition)); //Dont stomp on safe area
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Front Most Ingredient"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveEnemyIngredient()
    {

        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0 && UseableTeamIngredients.Count(x => x.routePosition == 0) == 0)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.OrderBy(x => x.endLowerPosition).FirstOrDefault(x =>
            !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26 && !Route.i.FullRoute[x.endLowerPosition % 26].isDangerZone));
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Enemy"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveOffStack(bool notInScoring = true)
    {
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableIngredients.OrderByDescending(x => x.distanceFromScore).FirstOrDefault(x => (notInScoring || x.distanceFromScore < 9)
            && (x.routePosition != 0 && Route.i.FullRoute[x.routePosition].ingredients.Count > 1 && Route.i.FullRoute[x.routePosition].ingredients.Any(x => x.Team == GameManager.i.activePlayer))
            && CanMoveSafely(x.endLowerPosition)); //Dont stomp on safe area
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Off stack" + (notInScoring ? " from non-scoring" : " from scoring"); }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient MoveOffStackToScore()
    {
        if (IngredientMovedWithHigher == null && IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            var ingredientThatCouldScore = TeamIngredients.FirstOrDefault(x => x.endHigherPosition == 26 && !x.isCooked && Route.i.FullRoute[x.routePosition].ingredients.Peek().IngredientId != x.IngredientId);
            if (ingredientThatCouldScore != null)
            {
                IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => x.routePosition == ingredientThatCouldScore.routePosition && Route.i.FullRoute[x.routePosition].ingredients.Peek().IngredientId == x.IngredientId && x.endLowerPosition != x.routePosition);
                if (IngredientMovedWithLower != null)
                {
                    if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move off stack to score"; }
                    return IngredientMovedWithLower;
                }
            }
        }
        return null;
    }
    private Ingredient MoveFrontMostEnemy()
    {
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0 && EnemyIngredients.Count(x => !x.isCooked) == 1)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.distanceFromScore < 9 //move from scoring position
            && !x.isCooked
            && !Route.i.FullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula
            && !TeamIngredients.Any(y => y.routePosition == x.endLowerPosition % 26)); //Dont stomp yourself
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Front Most Enemy"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    } 
    
    private Ingredient MoveOffLastPiece()
    {
        if (TeamIngredients.Count(x => !x.isCooked) == 1) {

            var lastIng = TeamIngredients.FirstOrDefault(x => !x.isCooked);
            if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
            {
                IngredientMovedWithLower = UseableIngredients.FirstOrDefault(x => !Route.i.FullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula
                && (x.routePosition != 0
                    && Route.i.FullRoute[x.routePosition].ingredients.Count > 1
                    && Route.i.FullRoute[x.routePosition].ingredients.Any(x => x.IngredientId == lastIng.IngredientId)
                    && Route.i.FullRoute[x.routePosition].ingredients.Peek().IngredientId != lastIng.IngredientId)); //Dont stomp yourself
                if (IngredientMovedWithLower != null)
                {
                    if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Off Last Piece"; }
                    return IngredientMovedWithLower;
                }
            }

            if (IngredientMovedWithHigher == null)
            {
                IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => !Route.i.FullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula
                && (x.routePosition != 0
                    && Route.i.FullRoute[x.routePosition].ingredients.Count > 1
                    && Route.i.FullRoute[x.routePosition].ingredients.Any(x => x.IngredientId == lastIng.IngredientId)
                    && Route.i.FullRoute[x.routePosition].ingredients.Peek().IngredientId != lastIng.IngredientId)); //Dont stomp yourself
                if (IngredientMovedWithHigher != null)
                {
                    if (Global.IsDebug) { GameManager.i.talkShitText.text = "Move Off Last Piece"; }
                    return IngredientMovedWithHigher;
                }
            }
        }
        return null;
    }

    private Ingredient Slide(bool WithCooked)
    {
        if (IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = UseableTeamIngredients.OrderBy(x=>x.distanceFromScore).FirstOrDefault(x => x.endHigherPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 9 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x.endHigherPosition)
            && ((Route.i.FullRoute[x.endHigherPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || Route.i.FullRoute[x.endHigherPositionWithoutSlide % 26].hasSpoon));
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Slide"; }
                return IngredientMovedWithHigher;
            }
        }
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableTeamIngredients.OrderBy(x => x.distanceFromScore).FirstOrDefault(x => x.endLowerPosition < 26 //Dont move past preparation
            && x.isCooked == WithCooked
            && (x.distanceFromScore > 9 || x.distanceFromScore < 3)  //Dont move from scoring position
            && CanMoveSafely(x.endHigherPosition)
            && ((Route.i.FullRoute[x.endLowerPositionWithoutSlide % 26].hasSpatula && !x.isCooked)
            || Route.i.FullRoute[x.endLowerPositionWithoutSlide % 26].hasSpoon));
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Slide"; }
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient GoToTrash()
    {
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = UseableEnemyIngredients.FirstOrDefault(x => x.routePosition != 0
            && !x.isCooked
            && ((GameManager.i.lowerMove < 6 && (x.endLowerPositionWithoutSlide % 26) == 10) || (x.endLowerPositionWithoutSlide % 26) == 18));
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
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation unless cooked
                && (x.routePosition != 0 || x.Team == GameManager.i.activePlayer) // if not your piece then dont move from prep
                && x.endLowerPosition != 0
                && x.endLowerPosition != x.routePosition
                && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
                && Route.i.FullRoute[x.endLowerPosition % 26].isDangerZone); //stomp safe area if someone is there
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithLower;
            }
        }

        if (useEither && IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.Team == GameManager.i.activePlayer) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && Route.i.FullRoute[x.endHigherPosition % 26].isDangerZone); //Dont stomp safe area if someone is there
            if (IngredientMovedWithHigher != null)
            {
                PrepShitTalk(useEither ? TalkType.Stomped : TalkType.StompedBySelf);
                return IngredientMovedWithHigher;
            }
        }

        return null;
    }
    private Ingredient StackEnemy(bool useEither = true)
    {
        var teamToMove = useEither ? UseableTeamIngredients : UseableEnemyIngredients;
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
        {
            IngredientMovedWithLower = teamToMove.FirstOrDefault(x => (x.endLowerPosition < 26 || x.isCooked) //Dont move past preparation
                && (x.routePosition != 0 || x.Team == GameManager.i.activePlayer) // if not your piece then dont move from prep
                && x.endLowerPosition != 0
                && x.endLowerPosition != x.routePosition
                && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endLowerPosition) //Stomp Enemy
                && Route.i.FullRoute[x.endLowerPosition % 26].ingredients.Peek().Team != GameManager.i.activePlayer // if enemy is on top
                && !Route.i.FullRoute[x.endLowerPosition % 26].isDangerZone); //stomp safe area if someone is there
            if (IngredientMovedWithLower != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Stack"; }
                return IngredientMovedWithLower;
            }
        }

        if (useEither && IngredientMovedWithHigher == null)
        {
            IngredientMovedWithHigher = teamToMove.FirstOrDefault(x => (x.endHigherPosition < 26 || x.isCooked) //Dont move past preparation
            && (x.routePosition != 0 || x.Team == GameManager.i.activePlayer) // if not your piece then dont move from prep
            && x.endHigherPosition != 0
            && x.endHigherPosition != x.routePosition
            && EnemyIngredients.Any(y => !y.isCooked && y.routePosition == x.endHigherPosition) //Stomp Enemy
            && Route.i.FullRoute[x.endHigherPosition % 26].ingredients.Peek().Team != GameManager.i.activePlayer // if enemy is on top
            && !Route.i.FullRoute[x.endHigherPosition % 26].isDangerZone); //Dont stomp safe area if someone is there
            if (IngredientMovedWithHigher != null)
            {
                if (Global.IsDebug) { GameManager.i.talkShitText.text = "Stack"; }
                return IngredientMovedWithHigher;
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
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
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
    
    private Ingredient CookIngredientWithDoubleMove()
    {
        if (IngredientMovedWithHigher == null && IngredientMovedWithLower == null && GameManager.i.firstMoveTaken == false && GameManager.i.DoublesRolled())
        {
            IngredientMovedWithLower = UseableTeamIngredients.FirstOrDefault(x => x.distanceFromScore == (GameManager.i.higherMove + GameManager.i.lowerMove) && !x.isCooked);
            if (IngredientMovedWithLower != null)
            {
                PrepShitTalk(TalkType.HelpCook);
                return IngredientMovedWithLower;
            }
        }
        return null;
    }
    private Ingredient HelpScore()
    {
        if (!UseableIngredients.Any(x => x.isCooked) || IngredientMovedWithLower != null || IngredientMovedWithHigher != null)
            return null;

        Ingredient ScoreableIng = null;
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0 && IngredientMovedWithHigher == null)
        {
            ScoreableIng = UseableTeamIngredients.FirstOrDefault(x => !x.isCooked && x.endLowerPosition == 25);
            if (ScoreableIng != null)
            {
                IngredientMovedWithHigher = UseableTeamIngredients.FirstOrDefault(x => x != ScoreableIng
                && x.isCooked
                && x.routePosition < ScoreableIng.routePosition
                && x.endHigherPosition > ScoreableIng.routePosition
                && x.endHigherPosition < 26
                && CanMoveSafely(x.endHigherPosition));
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
                && x.endLowerPosition < 26
                && CanMoveSafely(x.endLowerPosition));
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
                && CanMoveSafely(x.endHigherPosition));
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
                && CanMoveSafely(x.endLowerPosition));
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
        if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
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
        if (!Global.IsDebug && !Global.IsTutorial && !Global.FakeOnlineGame && (!hasBeenDumb && (Random.Range(0, Mathf.Min((Global.LoggedInPlayer.Wins == 0 ? 1 : Global.LoggedInPlayer.Wins), 50)) == 0)))
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

            if (IngredientMovedWithLower == null && GameManager.i.lowerMove != 0)
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
}

