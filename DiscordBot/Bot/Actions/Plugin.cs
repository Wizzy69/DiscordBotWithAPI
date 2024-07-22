using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Bot.Actions.Extra;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions;

public class Plugin: ICommandAction
{
    private bool pluginsLoaded;
    public string ActionName => "plugin";
    public string Description => "Manages plugins. Use plugin help for more info.";
    public string Usage => "plugin <option!>";

    public IEnumerable<InternalActionOption> ListOfOptions => new List<InternalActionOption>
    {
        new InternalActionOption("help", "Displays this message"),
        new InternalActionOption("list", "Lists all plugins"),
        new InternalActionOption("load", "Loads all plugins"),
        new InternalActionOption("install", "Installs a plugin", [
            new InternalActionOption("name", "The name of the plugin to install")
        ]),
        new InternalActionOption("refresh", "Refreshes the plugin list"),
        new InternalActionOption("uninstall", "Uninstalls a plugin"),
        new InternalActionOption("branch", "Sets a plugin option", [
            new InternalActionOption("set", "Sets the branch"),
            new InternalActionOption("get", "Gets the branch")
        ]),
        new InternalActionOption("enable", "Enables a plugin", [
            new InternalActionOption("name", "The name of the plugin to enable")
        ]),
        new InternalActionOption("disable", "Disables a plugin", [
            new InternalActionOption("name", "The name of the plugin to disable")
        ])
    };

    public InternalActionRunType RunType => InternalActionRunType.OnCall;
    
    public bool RequireOtherThread => false;

    public async Task Execute(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            await Application.CurrentApplication.InternalActionManager.Execute("help", ActionName);
            return;
        }

        switch (args[0])
        {
            case "enable":
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage : plugin enable <plugin name>");
                    return;
                }

                string pluginName = string.Join(' ', args, 1, args.Length - 1);
                await PluginMethods.EnablePlugin(pluginName);

                break;
            }
            case "disable":
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage : plugin disable <plugin name>");
                    return;
                }

                string pluginName = string.Join(' ', args, 1, args.Length - 1);
                await PluginMethods.DisablePlugin(pluginName);

                break;
            }
            case "branch":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage : plugin branch <option> <value>");
                    return;
                }

                string option = args[1];
                switch (option)
                {
                    case "set":
                    {
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Usage : plugin branch set <value>");
                            return;
                        }

                        string value = string.Join(' ', args, 2, args.Length - 2);
                        Application.CurrentApplication.PluginManager.Branch = value;
                        Console.WriteLine($"Branch set to {value}");
                    }
                        break;
                    case "get":
                        Console.WriteLine($"Branch is set to {Application.CurrentApplication.PluginManager.Branch}");
                        break;

                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }
                break;
            case "refresh":
                await PluginMethods.RefreshPlugins(true);
                break;

            case "uninstall":
            {
                string plugName = string.Join(' ', args, 1, args.Length - 1);
                bool result = await Application.CurrentApplication.PluginManager.MarkPluginToUninstall(plugName);
                if (result)
                    Console.WriteLine($"Marked to uninstall plugin {plugName}. Please restart the bot");
                break;
            }
            case "list":
                await PluginMethods.List();
                break;
            case "load":
                if (pluginsLoaded)
                {
                    Application.CurrentApplication.Logger.Log("Plugins already loaded", this, LogType.Warning);
                    break;
                }

                if (Application.CurrentApplication.DiscordBotClient is null)
                {
                    Application.CurrentApplication.Logger.Log("DiscordBot is null", this, LogType.Warning);
                    break;
                }

                pluginsLoaded = await PluginMethods.LoadPlugins(args);
                break;

            case "install":
            {
                var pluginName = string.Join(' ', args, 1, args.Length - 1);
                if (string.IsNullOrEmpty(pluginName) || pluginName.Length < 2)
                {
                    Console.WriteLine("Please specify a plugin name");
                    Console.Write("Plugin name : ");
                    pluginName = Console.ReadLine();
                    if (string.IsNullOrEmpty(pluginName) || pluginName.Length < 2)
                    {
                        Console.WriteLine("Invalid plugin name");
                        break;
                    }
                }

                await PluginMethods.DownloadPlugin(pluginName);

                break;
            }
        }
    }
}
