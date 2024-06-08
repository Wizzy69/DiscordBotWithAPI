﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotUI_Windows
{
    internal class Config
    {
        internal static DiscordBotCore.Others.SettingsDictionary<string, string> ApplicationSettings = new DiscordBotCore.Others.SettingsDictionary<string, string>(DiscordBotCore.Application.GetResourceFullPath("DiscordBotUI/config.json"));
        internal static ThemeManager ThemeManager = new ThemeManager();
    }
}
