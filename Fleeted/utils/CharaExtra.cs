using System;

namespace Fleeted.utils;

public static class CharaExtra
{
    public static string CharaName(this LobbyManager.PlayerInfo player)
    {
        return player.Chara switch
        {
            0u => "Tofe",
            1u => "Nobu",
            2u => "Taka",
            3u => "Waba",
            4u => "Miki",
            5u => "Lico",
            6u => "Naru",
            7u => "Pita",
            8u => "Lari",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}