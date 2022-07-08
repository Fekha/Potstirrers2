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
    public static int OnlineGameId = 0;

    //public static List<Player> CPUPlayers = new List<Player>() { 
    //    new Player() { Username = "Joe", IsCPU = true, UserId = 43 },
    //    new Player() { Username = "Zach", IsCPU = true, UserId = 42  },
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

Cook all 3 of your ingredients!

An ingredient becomes cooked when you enter the pot with one of your uncooked ingredients.

Note: Cooked ingredients can NOT enter the pot again, but may still be used to send ingredients back to prep. 
You also skip spaces cooked ingredients are on while moving, which gives you a boost forward!",

@"Taking a turn:

Roll both of your dice and move two different ingredients.

Your own ingredients are valid moves with either die.

If one of the die has a lower value than the other, you may instead move any of your opponents ingredients with it. 

An ingredient is a valid move if it is not part of a stack or on top of a stack.

You must always move an ingredient if possible.

If you have taken both moves or can not make a legal move move then your turn is over.
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

Ingredients all share Prep and do not stack

Prep is not skipped if there is a cooked ingredient on it when being sent there or when getting moved onto it from the end of the board.
",

@"Conclusion:

" + Settings.LoggedInPlayer.Username + @", thanks for playing and taking the time to learn the rules.

Playtesting is the core of game design. 
Without you there is no game, so please let me know about any feedback you have!"
 };
}

public enum PlayerTypes
{
    HUMAN,
    CPU,
    NO_PLAYER
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