using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Global
{
    public static Player LoggedInPlayer = new Player();
    public static Player SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
    public static int GameId = 0;
    public static double AppVersion = 1.25;
    public static bool IsConnected = true;
    public static bool IsDebug = false;
    public static bool OnlyGetOnlineFriends = false;
    public static bool EnteredGame = false;
    public static bool JustWonOnline = false;
    public static bool FakeOnlineGame = false;
    public static bool FriendlyGame = false;
    public static bool CPUGame = false;
    public static bool IsTutorial = false;
    public static bool hasNewIng = false;
    public static bool hasNewDie = false;
    public static bool hasNewTitle = false;

    public static void Reset()
    {
        SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
        GameId = 0;
        IsConnected = true;
        IsDebug = false;
        OnlyGetOnlineFriends = false;
        FakeOnlineGame = false;
        FriendlyGame = false;
        CPUGame = false;
        IsTutorial = false;
        hasNewIng = false;
        hasNewDie = false;
        hasNewTitle = false;
    }
}
public static class Library
{
    public static List<int> TutorialRolls = new List<int>(new[] { 7,3,4,8,4,2,7,1,8,3,5,6,3,0,8,4,8,1,7,1,7,6,3,2,9,4,5,3,2,2,0,0,4,4 });
    public static List<string> TutorialHelpQueue = new List<string>(new[] { "Welcome to Potstirrers!" });
    public static List<string> tutorialTextList = new List<string>()
    {
        @"Welcome to Pot Stirrers! You look new, so I'll teach you the ropes.",
        @"The game is simple. Get all 4 of your ingredients into the pot in the middle of the board!",
        @"Get started by rolling your two 10-sided die.",

        @"Nice roll! Now select your eggplant to move it with the first die.",

        @"Seems easy enough, right? Well here’s the twist!",
        @"With the lower rolled die, you may also move your opponent's ingredients!",
        @"I'll get more into that later, but for now go ahead and move your tomato.",

        @"I'll take my turn now. Watch and learn!",

        @"The selectors show what you can move. The salmon isn't valid because it's already been moved this turn.",

        @"Keep an eye on the 3 danger zones. Ingredients are sent back to prep if you land on them there!",
        @"Try it for yourself by moving your potato with that 4 you rolled!",

        @"If you land on an ingredient that's not in the danger zone, you stack on top of them.",
        @"If you are stacked on top of an ingredient, it can’t be moved!",
        @"The spoons let you take a shortcut. Try sliding with your chicken and stacking on my salmon!",

        @"Now that it benefits me, I'll show you a good reason to move your opponent's ingredients.",
        @"I'll move your tomato with my lower die to send your potato back to prep!",

        @"To add insult to injury, I'll stack on top of your eggplant with my mushroom!",

        @"You don't always have to use your higher rolled die first. Select the other die to switch!",

        @"Now that you have your 3 selected, move my mushroom off of your eggplant!",

        @"Notice when you moved me, it was the exact number needed to put me into a trash can which sent me back to prep!",
        @"Now that your eggplant is free, move it with your 8!",

        @"My turn! Not every move has to be so flashy. Sometimes it's good to just progress your ingredients.",

        @"You rolled a 0! That'll happen sometimes. A 0 die roll doesn't allow you to move, so that's why it's already disabled.",
        @"With your 3, move your eggplant so it's less than 9 away from the pot, giving you a chance to cook it next turn.",

        @"My beef stomped on your tomato in the danger zone! Have fun in prep!",

        @"Oh look! You rolled the exact number you need to get into the pot!",
        @"Move your eggplant into the pot to cook it.",

        @"Congrats!!! You cooked your first ingredient!! You're winning!",
        @"Remember to think of creative ways to use your lower die, like moving my beef onto my lemon to send it to prep!",

        @"You may have noticed, cooked ingredients return to prep and are still in play, however, they can not be cooked again.",

        @"Instead, cooked ingredients have a new role to play. Other ingredients skip over them!",

        @"So remember, don’t count tiles that cooked ingredients are on when planning out your next move.",

        @"Oh wow! You rolled a big enough number to move my mushroom past the pot!",

        @"Switch to your lower die so you can move my mushroom 6.",

        @"Now move my mushroom past the pot, but beware- this can happen to your ingredients, too!",

        @"You probably want to stay stacked on my salmon, so move your tomato from prep!",

        @"I found a strategic way to get your ingredient off mine. Perfect time to use the trash can!",

        @"Instead of moving first with your 9, switch to your 4, and I'll show you a cool move to do with your cooked ingredient.",

        @"Now move your eggplant to set up your next move. You'll be able to slide on the second spoon from prep!",

        @"Great! Now move your potato with the 9, and watch how you skip over the cooked ingredient to ultimately move 10!",

        @"What a great turn for me. I've got you right where I want you!",

        @"You rolled doubles! When you roll doubles, count both dice as ‘the lower die’.",
        @"This means you can move your opponent's ingredients with either die. Try it by moving my salmon off of your potato.",

        @"Your potato is now free to move into a scoreable position!",

        @"Oof! Double zeros... the worst kind of doubles! Looks like I lose my turn!",

        @"Nice, doubles again! With doubles you can also break the rules and move the same ingredient twice!",
        @"With that in mind, move your potato to get it 4 away from scoring!",

        @"Now move your potato again to score!",
       
        @"Congrats! You are half way to winning, and you're beating me 2 to 0!",
        @"I think I've taught you enough. Now let's see if the student can beat the teacher. Game on!"
    };
    public static List<string> helpTextList = new List<string>() { @"Welcome, " + Global.LoggedInPlayer.Username.Trim() +"!"+
@"

The following pages will explain the rules of Pot Stirrers.
Tap anywhere to view the next page.

For more in depth help join our discord! https://discord.gg/fab.",

@"How to win:

Cook all 4 of your ingredients!

An ingredient becomes cooked when you enter the pot with one of your uncooked ingredients.

Note: Cooked ingredients can NOT enter the pot again, but may still be used to send ingredients back to prep. 
You also skip spaces cooked ingredients are on while moving, which gives you a boost forward!",

@"Taking a turn:

Roll both of your dice and move two different ingredients.

You may move one of your ingredients with either roll.

You may move one of the other teams ingredients with your lower roll.

If you roll doubles, you may move the same ingredient twice and you may move ingredients from either team.

You may not move an ingredient that has been stacked on and you must always move an ingredient if possible.
",

@"Landing on an ingredient:

If an ingredient is landed on, it stacks on top of it, unless it is in a danger zone.

If it was in a danger zone, you do not stack, you instead send the ingredient that was landed on to prep.

An ingredient that has been stacked on may not be moved until the pieces above it are moved.

Note: Cooked ingredients can't be landed on because you never count the space they are on while moving.",

@"Exact Spaces:

There are three spaces labeled Exact.

Landing on them does NOTHING, instead they signal a split in the path that may be taken if you have exactly one move left.

The first two you come across lead the ingredient to a trash can that sends it back to prep.

The last leads to the pot, where you cook your ingredients.",

@"Sliding:

If an ingredient ends its movement on a space with a utensil handle, immediately move it to the other side of the utensil.

Note: If there is a cooked ingredient on the other side of the utensil, skip over it!",

@"prep:

All ingredients start on prep, and return there after being cooked, being landed on in a danger zone, or moving onto a trash can.

Ingredients do not stack on prep and prep may never be skipped regadless of the amount of cooked ingredients on it.

",

@"Conclusion:

" + Global.LoggedInPlayer.Username + @", thanks for playing and taking the time to learn the rules.

Playtesting is the core of game design. 
Without you there is no game, so please let me know about any feedback you have!"
 };
}
public enum TalkType
{
    MoveRandomly,
    Stomped,
    StompedBySelf,
    SafeZoned,
    Cook,
    HelpCook,
    Trash,
    MovePastPrep,
    SentBack
}