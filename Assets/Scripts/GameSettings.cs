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
}

public enum PlayerTypes
{
    HUMAN,
    CPU,
    NO_PLAYER
}