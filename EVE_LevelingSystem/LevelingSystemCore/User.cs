﻿namespace EVE_LevelingSystem.LevelingSystemCore;

public class User
{
    public string userID               { get; set; }
    public int    CurrentLevel         { get; set; }
    public long   CurrentEXP           { get; set; }
    public long   RequiredEXPToLevelUp { get; set; }
}
