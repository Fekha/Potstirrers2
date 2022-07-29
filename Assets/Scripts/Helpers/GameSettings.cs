using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Global
{
    public static Player LoggedInPlayer = new Player();
    public static Player SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
    public static int GameId = 0;
    public static double AppVersion = 1.21;
    public static bool IsConnected = true;
    public static bool IsDebug = false;
    public static bool OnlyGetOnlineFriends = false;
    public static bool EnteredGame = false;
    public static bool JustWonOnline = false;
    public static bool FakeOnlineGame = false;
    public static bool FriendlyGame = false;
    public static bool CPUGame = false;
    public static bool PlayingTutorial = false;
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
        PlayingTutorial = false;
        hasNewIng = false;
        hasNewDie = false;
        hasNewTitle = false;
    }
}
public static class Library
{
    public static List<int> TutorialRolls = new List<int>(new[] { 7,3,4,8,4,2,7,1,8,3,5,6,3,0,8,4,8,1,7,1,8,6,3,2,9,4,7,3,2,2,0,0,4,4 });
    public static List<string> TutorialHelpQueue = new List<string>(new[] { "Welcome to Potstirrers!" });
    public static List<string> helpTextList = new List<string>() { @"Welcome, " + Global.LoggedInPlayer.Username.Trim() +"!"+
@"

The following pages will explain the rules of Pot Stirrers.
Click anywhere to view the next page.

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

If it was in a danger zone, you do not stack, you instead send the ingredient that was landed on to Prep.

An ingredient that has been stacked on may not be moved until the pieces above it are moved.

Note: Cooked ingredients can't be landed on because you never count the space they are on while moving.",

@"Exact Spaces:

There are three spaces labeled Exact.

Landing on them does NOTHING, instead they signal a split in the path that may be taken if you have exactly one move left.

The first two you come across lead the ingredient to a trash can that sends it back to Prep.

The last leads to the pot, where you cook your ingredients.",

@"Sliding:

If an ingredient ends its movement on a space with a utensil handle, immediately move it to the other side of the utensil.

Note: If there is a cooked ingredient on the other side of the utensil, skip over it!",

@"Prep:

All ingredients start on Prep, and return there after being cooked, being landed on in a danger zone, or moving onto a trash can.

Ingredients do not stack on Prep and Prep may never be skipped regadless of the amount of cooked ingredients on it.

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