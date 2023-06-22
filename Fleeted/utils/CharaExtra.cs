using System;

namespace Fleeted.utils;

public static class CharaExtra
{
    public static string CharaName(this LobbyManager.PlayerInfo player)
    {
        return player.Chara switch
        {
            0 => "Tofe",
            1 => "Nobu",
            2 => "Taka",
            3 => "Waba",
            4 => "Miki",
            5 => "Lico",
            6 => "Naru",
            7 => "Pita",
            8 => "Lari",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}