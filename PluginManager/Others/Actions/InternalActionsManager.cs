﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Loaders;

namespace PluginManager.Others.Actions;

public class InternalActionManager
{
    public Dictionary<string, ICommandAction> Actions = new();
    public ActionsLoader                      loader;

    public InternalActionManager(string path, string extension)
    {
        loader = new ActionsLoader(path, extension);
    }

    public async Task Initialize()
    {
        //loader.ActionLoadedEvent += OnActionLoaded;
        var m_actions = await loader.Load();
        if (m_actions == null) return;
        foreach (var action in m_actions)
        {
            Actions.TryAdd(action.ActionName, action);
        }
    }
    
    public async Task Refresh()
    {
        Actions.Clear();
        await Initialize();
    }

    // private void OnActionLoaded(string name, string typeName, bool success, Exception? e)
    // {
    //     if (!success)
    //     {
    //         Config.Logger.Error(e);
    //         return;
    //     }
    //     
    //     Config.Logger.Log($"Action {name} loaded successfully", LogLevel.INFO, true);
    // }

    public async Task<string> Execute(string actionName, params string[]? args)
    {
        if (!Actions.ContainsKey(actionName))
        {
            Config.Logger.Log($"Action {actionName} not found", type: LogType.ERROR, source: typeof(InternalActionManager));
            return "Action not found";
        }

        try
        {
            await Actions[actionName].Execute(args);
            return "Action executed";
        }
        catch (Exception e)
        {
            Config.Logger.Log(e.Message , type: LogType.ERROR, source: typeof(InternalActionManager));
            return e.Message;
        }
    }
}
