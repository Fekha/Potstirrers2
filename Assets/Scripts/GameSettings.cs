using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Settings
{
    public static Player LoggedInPlayer = new Player();
    public static Player SecondPlayer = new Player();
    public static bool IsDebug = false;
    
    public static bool EnteredGame = false;
    public static Player[] PlayingPlayers = new Player[2] {
        new Player() { Username = "Joe", playerType = PlayerTypes.CPU },
        new Player() { Username = "Zach", playerType = PlayerTypes.CPU },
    };
    public static List<Player> CPUPlayers = new List<Player>() { 
        new Player() { Username = "Joe", playerType = PlayerTypes.CPU },
        new Player() { Username = "Zach", playerType = PlayerTypes.CPU },
        new Player() { Username = "Jenn", playerType = PlayerTypes.CPU },
        new Player() { Username = "Chrissy", playerType = PlayerTypes.CPU } 
    };
}
public static class Library
{
    public static List<string> helpTextList = new List<string>() { @"Welcome, " + Settings.LoggedInPlayer.Username +"!"+
@"

The following pages will explain the rules of Pot Stirrers.
Click anywhere to view the next page.

Note: The AI's skill gets better as you get more wins!

For more in depth help join our discord! https://discord.gg/fab.",

@"How to win:

Cook all 3 of your ingredients!

An ingredient becomes cooked when you enter the pot with one of your uncooked ingredients.

Note: Cooked ingredients can NOT enter the pot again, but may still be used to send ingredients back to prep. 
You also skip spaces cooked ingredients are on while moving, which gives you a boost forward!",

@"Taking a turn:

Roll two dice.

Move one ingredient from your team with the highest roll.

Move one ingredient from any team with the lowest roll.

Take these moves in any order, but the same ingredient may not be moved twice.

If doubles were rolled, take another turn.",

@"Exact Spaces:

There are three spaces labeled Exact.
Landing on them does NOTHING. 

They signal a split in the path that may be taken if you have exactly one move left.

The first two you come across lead the ingredient to a trash can that sends it back to Prep.

The last leads to the pot, where you cook your ingredients.",

@"Landing on an ingredient:

If an ingredient is landed on, send it to Prep, unless it is in a safe area.

If it was in a safe area, send the ingredient that landed on it to Prep instead.

Note: Cooked ingredients can't be landed on because you never count the space they are on while moving.",

@"Sliding:

If an ingredient ends its movement on a space with a utensil handle, immediately move it to the other side of the utensil.

If an uncooked ingredient was on the other side, send them back to Prep! 

If it was a cooked ingredient, skip over it.",

@"Prep:

All ingredients start on Prep, and are sent there after being landed on. Prep is its own space when counting.

Prep is never skipped, despite the number of cooked ingredients on it.

Note: You can be moved onto or past Prep from the end of the board, so be careful!
",

@"Conclusion:

" + Settings.LoggedInPlayer.Username + @", thanks for playing and taking the time to learn the rules.

Playtesting is the core of game design. 
Without you there is no game, so please let me know about any feedback you have!"
 };
}
[System.Serializable]
public class Player
{
    public int UserId = 0;
    public string Username = "Guest";
    public string Password = "";
    public PlayerTypes playerType = PlayerTypes.HUMAN;
    public int Wins = 0;
    public int LocalWins = 0;
    public int SelectedDie = 0;
    public int SelectedDie2 = 0;
    public int SelectedMeat = 0;
    public int SelectedVeggie = 0;
    public int SelectedFruit = 0;
    public int Stars = 0;
    public int Cooked = 0;
    public int Xp = 0;
    public int Level = 1;
    public bool IsGuest = true;
    public bool WineMenu = false;
    public bool UseD8s = false;
    public bool DisableDoubles = false;
    public bool PlayAsPurple = false;
    public bool Experimental = true;
}

public enum PlayerTypes
{
    HUMAN,
    CPU,
    NO_PLAYER
}