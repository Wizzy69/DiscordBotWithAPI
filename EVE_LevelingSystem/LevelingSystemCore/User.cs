﻿using Discord;
using Discord.WebSocket;

namespace EVE_LevelingSystem.LevelingSystemCore
{
    public class DiscordUser
    {
        public string Username   { get; set; }
        public ushort DiscordTag { get; set; }
        public ulong  userID     { get; set; }
    }

    public class User
    {
        public DiscordUser user                 { get; set; }
        public int         CurrentLevel         { get; set; }
        public long        CurrentEXP           { get; set; }
        public long        RequiredEXPToLevelUp { get; set; }
    }
}