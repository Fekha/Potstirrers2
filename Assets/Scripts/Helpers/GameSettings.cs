using Assets.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Settings
{
    public static Player LoggedInPlayer = new Player();
    public static Player SecondPlayer = new Player() { Username = "Jenn", IsCPU = true, UserId = 41 };
    public static bool IsConnected = true;
    public static bool IsDebug = false;
    public static bool HardMode = true;
    public static bool EnteredGame = false;
    public static bool JustWonOnline = false;
    public static int OnlineGameId = 0;
    public static double AppVersion = 1.03;
    public static bool FakeOnlineGame = false;

    //public static List<Player> CPUPlayers = new List<Player>() { 
    //    new Player() { Username = "Joe", IsCPU = true, UserId = 43 },
    //    new Player() { Username = "Ethan", IsCPU = true, UserId = 42  },
    //    new Player() { Username = "Chrissy", IsCPU = true, UserId = 44  },
    //    new Player() { Username = "Jenn", IsCPU = true, UserId = 41  }
    //};
}
public static class Library
{
    public static List<string> helpTextList = new List<string>() { @"Welcome, " + Settings.LoggedInPlayer.Username.Trim() +"!"+
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

Rules for moving:

You may move one of your ingredients with either roll.

With your lower roll you may also move an ingredient from the other team.

If you rolled doubles, they both count as a lower roll, and you may move the same ingredient twice.

You may not move an ingredient that has been stacked on.

You must always move an ingredient if possible.
",

@"Landing on an ingredient:

If an ingredient is landed on, it stacks on top of it, unless it is in a danger zone.

If it was in a danger zone, you do not stack but instead, send the ingredient that was landed on to Prep.

An ingredient that has been stacked on may not be moved until the pieces above it are moved.

Note: Cooked ingredients can't be landed on because you never count the space they are on while moving.",

@"Exact Spaces:

There are three spaces labeled Exact.

Landing on them does NOTHING. 

They signal a split in the path that may be taken if you have exactly one move left.

The first two you come across lead the ingredient to a trash can that sends it back to Prep.

The last leads to the pot, where you cook your ingredients.",

@"Sliding:

If an ingredient ends its movement on a space with a utensil handle, immediately move it to the other side of the utensil.

Note: If there is a cooked ingredient on the other side of the utensil, skip over it!",

@"Prep:

All ingredients start on Prep, and return here after being cooked, being landed on in a danger zone, or moving onto a trash can.

Ingredients all share Prep and do not stack.

Prep is not skipped if there is a cooked ingredient on it when being sent there or when getting moved onto it from the end of the board.
",

@"Conclusion:

" + Settings.LoggedInPlayer.Username + @", thanks for playing and taking the time to learn the rules.

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