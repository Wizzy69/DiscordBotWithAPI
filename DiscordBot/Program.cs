﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DiscordBotCore;
using DiscordBotCore.Bot;
using DiscordBotCore.Others;
using DiscordBotCore.Updater.Application;

using Spectre.Console;

namespace DiscordBot;

public class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    public static async Task Startup(string[] args)
    {
        await LoadComponents(args);

        await PrepareConsole();
        await ConsoleInputHandler();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static async Task ConsoleInputHandler()
    {
        Application.CurrentApplication.InternalActionManager.Execute("plugin", "load").Wait();

        while (true)
        {
            var cmd     = Console.ReadLine();
            var args    = cmd.Split(' ');
            var command = args[0];
            args = args.Skip(1).ToArray();
            if (args.Length == 0)
                args = null;

            await Application.CurrentApplication.InternalActionManager.Execute(command, args); 
        }
    }

    /// <summary>
    ///     Start the bot without user interface
    /// </summary>
    /// <returns>Returns the bootloader for the Discord Bot</returns>
    private static async Task PrepareConsole()
    {

        AnsiConsole.MarkupLine($"[yellow]Running on version: {Application.CurrentApplication.ApplicationEnvironmentVariables["Version"]}[/]");
        AnsiConsole.MarkupLine("[yellow]Git SethBot: https://github.com/andreitdr/SethDiscordBot [/]");
        AnsiConsole.MarkupLine("[yellow]Git Plugins: https://github.com/andreitdr/SethPlugins [/]");

        AnsiConsole.MarkupLine("[yellow]Remember to close the bot using the shutdown command ([/][red]exit[/][yellow]) or some settings won't be saved[/]");
        AnsiConsole.MarkupLine($"[yellow]Running on [/][magenta]{(OperatingSystem.IsWindows() ? "Windows" : "Linux")}[/]");

        AnsiConsole.MarkupLine("[yellow]===== Seth Discord Bot =====[/]");

        try
        {
            var token         = Application.CurrentApplication.ApplicationEnvironmentVariables["token"];
            var prefix        = Application.CurrentApplication.ApplicationEnvironmentVariables["prefix"];
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
        }
        catch (Exception ex)
        {
            Application.CurrentApplication.Logger.Log(ex.ToString(), typeof(Program), LogType.CRITICAL);
        }
    }

    private static async Task LoadComponents(string[] args)
    {
        await Application.CreateApplication();

        Application.CurrentApplication.ApplicationEnvironmentVariables["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        AppUpdater updater = new();
        var update = await updater.CheckForUpdates();

        if (update != Update.None)
        {
            Console.WriteLine($"New update available: {update.UpdateVersion}");
            Console.WriteLine($"Download link: {update.UpdateUrl}");
            Console.WriteLine($"Update notes: {update.UpdateNotes}\n\n");

            Console.WriteLine("Waiting 5 seconds ...");
            await Task.Delay(5000);
        }

        Application.CurrentApplication.Logger.OnFormattedLog += async (sender, logMessage) =>
        {
            await File.AppendAllTextAsync(Application.CurrentApplication.LogFile, logMessage.Message + "\n");
            var messageColor = logMessage.Type switch
            {
                LogType.INFO => "[green]",
                LogType.WARNING => "[yellow]",
                LogType.ERROR => "[red]",
                LogType.CRITICAL => "[red]",
                _ => "[white]"
            };

            if (logMessage.Message.Contains('['))
            {
                Console.WriteLine(logMessage.Message);
                return;
            }

            string messageToPrint = $"{messageColor}{logMessage.Message}[/]";
            AnsiConsole.MarkupLine(messageToPrint);
        };


        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID") ||
            !Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token") ||
            !Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix")
        )
            Installer.GenerateStartupConfig().Wait();
    }
}
