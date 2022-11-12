﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DiscordBot.Discord.Core;

using PluginManager;
using PluginManager.Database;
using PluginManager.Items;
using PluginManager.Online;
using PluginManager.Online.Helpers;
using PluginManager.Others;

using Terminal.Gui;

using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace DiscordBot;

public class Program
{
    private static bool loadPluginsOnStartup;
    private static bool listPluginsAtStartup;
    private static ConsoleCommandsHandler consoleCommandsHandler;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Startup(string[] args)
    {

        PreLoadComponents().Wait();

        if (!Config.Variables.Exists("ServerID") || !Config.Variables.Exists("token") ||
            Config.Variables.GetValue("token") == null ||
            (Config.Variables.GetValue("token")?.Length != 70 && Config.Variables.GetValue("token")?.Length != 59) ||
            !Config.Variables.Exists("prefix") || Config.Variables.GetValue("prefix") == null ||
            Config.Variables.GetValue("prefix")?.Length != 1 ||
            (args.Length == 1 && args[0] == "/reset"))
        {
            Application.Init();
            var top = Application.Top;
            var win = new Window("Discord Bot Config - " + Assembly.GetExecutingAssembly().GetName().Version)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            top.Add(win);

            var labelInfo = new Label(
                    "Configuration file not found or invalid. " +
                    "Please fill the following fields to create a new configuration file."
                )
            {
                X = Pos.Center(),
                Y = 2
            };


            var labelToken = new Label("Please insert your token here: ")
            {
                X = 5,
                Y = 5
            };

            var textFiledToken = new TextField("")
            {
                X = Pos.Left(labelToken) + labelToken.Text.Length + 2,
                Y = labelToken.Y,
                Width = 70
            };

            var labelPrefix = new Label("Please insert your prefix here: ")
            {
                X = 5,
                Y = 8
            };
            var textFiledPrefix = new TextField("")
            {
                X = Pos.Left(labelPrefix) + labelPrefix.Text.Length + 2,
                Y = labelPrefix.Y,
                Width = 1
            };

            var labelServerid = new Label("Please insert your server id here (optional): ")
            {
                X = 5,
                Y = 11
            };
            var textFiledServerID = new TextField("")
            {
                X = Pos.Left(labelServerid) + labelServerid.Text.Length + 2,
                Y = labelServerid.Y,
                Width = 18
            };

            var button = new Button("Submit")
            {
                X = Pos.Center() - 10,
                Y = 16
            };

            var button2 = new Button("License")
            {
                X = Pos.Center() + 10,
                Y = 16
            };

            var button3 = new Button("ⓘ")
            {
                X = Pos.Left(textFiledServerID) + 20,
                Y = textFiledServerID.Y
            };

            Console.CancelKeyPress += (sender, e) => { top.Running = false; };

            button.Clicked += () =>
            {
                var passMessage = "";
                if (textFiledToken.Text.Length != 70 && textFiledToken.Text.Length != 59)
                    passMessage += "Invalid token, ";
                if (textFiledPrefix.Text.ContainsAny("0123456789/\\ ") || textFiledPrefix.Text.Length != 1)
                    passMessage += "Invalid prefix, ";
                if (textFiledServerID.Text.Length != 18 && textFiledServerID.Text.Length > 0)
                    passMessage += "Invalid serverID";

                if (passMessage != "")
                {
                    MessageBox.ErrorQuery("Discord Bot Settings",
                                          "Failed to pass check. Invalid information given:\n" + passMessage, "Retry");
                    return;
                }


                Config.Variables.Add("ServerID", (string)textFiledServerID.Text, true);
                Config.Variables.Add("token", (string)textFiledToken.Text, true);
                Config.Variables.Add("prefix", (string)textFiledPrefix.Text, true);

                MessageBox.Query("Discord Bot Settings", "Successfully saved config !\nJust start the bot :D",
                                 "Start :D");
                top.Running = false;
            };

            button2.Clicked += async () =>
            {
                var license =
                    await ServerCom.ReadTextFromURL(
                        "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/LICENSE.txt");
                var ProductLicense =
                    "Seth Discord Bot\n\nDeveloped by Wizzy#9181\nThis application can be used and modified by anyone. Plugin development for this application is also free and supported";
                var r = MessageBox.Query("Discord Bot Settings", ProductLicense, "Close", "Read about libraries used");
                if (r == 1)
                {
                    var i = 0;
                    while (i < license.Count)
                    {
                        var print_message = license[i++] + "\n";
                        for (; i < license.Count && !license[i].StartsWith("-----------"); i++)
                            print_message += license[i] + "\n";
                        if (print_message.Contains("https://"))
                            print_message += "\n\nCTRL + Click on a link to open it";
                        if (MessageBox.Query("Licenses", print_message, "Next", "Quit") == 1) break;
                    }
                }
            };

            button3.Clicked += () =>
            {
                MessageBox.Query("Discord Bot Settings",
                                 "Server ID can be found in Server settings => Widget => Server ID",
                                 "Close");
            };

            win.Add(labelInfo, labelPrefix, labelServerid, labelToken);
            win.Add(textFiledToken, textFiledPrefix, textFiledServerID, button3);
            win.Add(button, button2);
            Application.Run();
            Application.Shutdown();
        }

        HandleInput(args).Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static void NoGUI()
    {
#if DEBUG
        Settings.Variables.outputStream.WriteLine();
        ConsoleCommandsHandler.ExecuteCommad("lp").Wait();
#else
        if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
        if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");
#endif

        while (true)
        {
            var cmd = Console.ReadLine();
            if (!consoleCommandsHandler.HandleCommand(cmd!
#if DEBUG
                                                    , false
#endif

            ) && cmd.Length > 0)
                Settings.Variables.outputStream.WriteLine("Failed to run command " + cmd);
        }
    }

    /// <summary>
    ///     Start the bot without user interface
    /// </summary>
    /// <returns>Returns the boot loader for the Discord Bot</returns>
    private static async Task<Boot> StartNoGui()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        var startupMessageList =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/StartupMessage");

        foreach (var message in startupMessageList)
            Settings.Variables.outputStream.WriteLine(message);

        Settings.Variables.outputStream.WriteLine(
            $"Running on version: {Assembly.GetExecutingAssembly().GetName().Version}");
        Settings.Variables.outputStream.WriteLine($"Git URL: {Settings.Variables.WebsiteURL}");

        Utilities.WriteColorText(
            "&rRemember to close the bot using the ShutDown command (&ysd&r) or some settings won't be saved\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (Config.Variables.Exists("LaunchMessage"))
            Utilities.WriteColorText(Config.Variables.GetValue("LaunchMessage"));


        Utilities.WriteColorText(
            "Please note that the bot saves a backup save file every time you are using the shudown command (&ysd&c)");
        Settings.Variables.outputStream.WriteLine("============================ LOG ============================");

        try
        {
            string token = "";
#if DEBUG

            if (await Settings.sqlDatabase.TableExistsAsync("BetaTest"))
            {
                Settings.Variables.outputStream.WriteLine("Starting in DEBUG MODE");
                token = await Settings.sqlDatabase.GetValueAsync("BetaTest", "VariableName", "Token", "Value");
            }
            else token = Config.Variables.GetValue("token");
#else
            token = Config.Variables.GetValue("token");
#endif
            var prefix = Config.Variables.GetValue("prefix");
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
            return discordbooter;
        }
        catch (Exception ex)
        {
            Settings.Variables.outputStream.WriteLine(ex);
            return null;
        }
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    /// <param name="args">The arguments</param>
    private static async Task HandleInput(string[] args)
    {
        var len = args.Length;

        var b = await StartNoGui();
        consoleCommandsHandler = new ConsoleCommandsHandler(b.client);

        if (len > 0 && args[0] == "/remplug")
        {
            var plugName = string.Join(' ', args, 1, args.Length - 1);
            Settings.Variables.outputStream.WriteLine("Starting to remove " + plugName);
            await ConsoleCommandsHandler.ExecuteCommad("remplug " + plugName);
            loadPluginsOnStartup = true;
        }

        if (len > 0 && args[0] == "/lp")
            loadPluginsOnStartup = true;

        var mainThread = new Thread(() =>
        {
            try
            {
                NoGUI();
            }
            catch (IOException ex)
            {
                if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
                {
                    if (Config.Variables.Exists("LaunchMessage"))
                        Config.Variables.Add("LaunchMessage",
                                                   "An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !",
                                                   false);
                    Functions.WriteErrFile(ex.ToString());
                }
            }
        });
        mainThread.Start();
    }

    private static async Task PreLoadComponents()
    {
        Settings.Variables.outputStream = Console.Out;
        Settings.Variables.outputStream.WriteLine("Loading resources ...");
        var main = new Utilities.ProgressBar(ProgressBarType.NO_END);
        main.Start();
        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins");
        Directory.CreateDirectory("./Data/PAKS");

        Settings.sqlDatabase = new SqlDatabase(Functions.dataFolder + "SetDB.dat");

        await Settings.sqlDatabase.Open();
        await Config.Initialize();



        if (await Config.Variables.ExistsAsync("DeleteLogsAtStartup"))
            if (await Config.Variables.GetValueAsync("DeleteLogsAtStartup") == "true")
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        var OnlineDefaultKeys =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/SetupKeys");


        if (!await Config.Variables.ExistsAsync("Version"))
            await Config.Variables.AddAsync("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(), false);
        else
            await Config.Variables.SetValueAsync("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString());


        foreach (var key in OnlineDefaultKeys)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;
            var s = key.Split(' ');
            try
            {
                if (await Config.Variables.ExistsAsync(s[0])) await Config.Variables.SetValueAsync(s[0], s[1]);
                else
                    await Config.Variables.AddAsync(s[0], s[1], s[2].ToLower() == "true");
            }
            catch (Exception ex)
            {
                Functions.WriteErrFile(ex.Message);
            }
        }


        var onlineSettingsList =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/OnlineData");
        main.Stop("Loaded online settings. Loading updates ...");
        foreach (var key in onlineSettingsList)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;

            var s = key.Split(' ');
            switch (s[0])
            {
                case "CurrentVersion":
                    var newVersion = s[1];
                    if (!newVersion.Equals(await Config.Variables.GetValueAsync("Version")))
                    {
                        var nVer = new VersionString(newVersion.Substring(2));
                        var cVer = new VersionString((await Config.Variables.GetValueAsync("Version")).Substring(2));
                        if (cVer > nVer)
                        {
                            await Config.Variables.SetValueAsync("Version", "1." + cVer.ToShortString() + " (Beta)");
                            break;
                        }

                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("A new version of the bot is available !");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Current version : " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("New version : " + newVersion);
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine("Changelog :");

                        List<string> changeLog = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/VersionData/DiscordBot");
                        foreach (var item in changeLog)
                            Utilities.WriteColorText(item);
                        Console.WriteLine("Do you want to update the bot ? (y/n)");
                        if (Console.ReadKey().Key == ConsoleKey.Y)
                        {


                            if (Functions.GetOperatingSystem() == OperatingSystem.WINDOWS)
                            {
                                var url =
                                    $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0.zip";
                                Process.Start(".\\Updater\\Updater.exe",
                                              $"{newVersion} {url} {Process.GetCurrentProcess().ProcessName}");
                            }
                            else
                            {
                                var url =
                                    $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0_linux.zip";
                                Settings.Variables.outputStream.WriteLine("Downloading update ...");
                                await ServerCom.DownloadFileNoProgressAsync(url, "./update.zip");
                                await File.WriteAllTextAsync("Install.sh",
                                                             "#!/bin/bash\nunzip -qq update.zip -d ./\nrm update.zip\nchmod +x SethDiscordBot\n./DiscordBot");
                                Process.Start("Install.sh").WaitForExit();
                                Environment.Exit(0);
                            }
                        }
                    }

                    break;
                case "UpdaterVersion":
                    var updaternewversion = s[1];
                    if (Functions.GetOperatingSystem() == OperatingSystem.LINUX)
                        break;

                    if (!await Config.Variables.ExistsAsync("UpdaterVersion"))
                        await Config.Variables.AddAsync("UpdaterVersion", "0.0.0.0", false);
                    if (await Config.Variables.GetValueAsync("UpdaterVersion") != updaternewversion ||
                        !Directory.Exists("./Updater") ||
                        !File.Exists("./Updater/Updater.exe"))
                    {
                        Console.Clear();
                        Settings.Variables.outputStream.WriteLine("Installing updater ...\nDo NOT close the bot during update !");
                        var bar = new Utilities.ProgressBar(ProgressBarType.NO_END);
                        bar.Start();
                        await ServerCom.DownloadFileNoProgressAsync(
                            "https://github.com/Wizzy69/installer/releases/download/release-1-discordbot/Updater.zip",
                            "./Updater.zip");
                        await Functions.ExtractArchive("./Updater.zip", "./", null,
                                                       UnzipProgressType.PercentageFromTotalSize);
                        await Config.Variables.SetValueAsync("UpdaterVersion", updaternewversion);
                        File.Delete("Updater.zip");
                        bar.Stop("Updater has been updated !");
                        Console.Clear();
                    }

                    break;
            }
        }
        Console.Clear();
    }
}