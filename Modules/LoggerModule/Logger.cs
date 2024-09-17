﻿using DiscordBotCore.Others;

namespace LoggerModule;

public sealed class Logger : ILogger
{
    private readonly FileStream _LogFileStream;

    public List<string> LogMessageProperties = typeof(ILogMessage).GetProperties().Select(p => p.Name).ToList();
    private Action<string, LogType>? _OutFunction;
    public string LogMessageFormat { get ; set; }

    public Logger(string logFolder, string logMessageFormat, Action<string,LogType>? outFunction = null)
    {
        this.LogMessageFormat = logMessageFormat;
        this._OutFunction = outFunction;
        var logFile = logFolder + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        _LogFileStream = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
    }

    /// <summary>
    /// Generate a formatted string based on the default parameters of the ILogMessage and a string defined as model
    /// </summary>
    /// <param name="message">The message</param>
    /// <returns>A formatted string with the message values</returns>
    private string GenerateLogMessage(ILogMessage message)
    {
        string messageAsString = new string(LogMessageFormat);
        foreach (var prop in LogMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType?.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    private async void LogToFile(string message)
    {
        byte[] messageAsBytes = System.Text.Encoding.ASCII.GetBytes(message);
        await _LogFileStream.WriteAsync(messageAsBytes, 0, messageAsBytes.Length);

        byte[] newLine = System.Text.Encoding.ASCII.GetBytes(Environment.NewLine);
        await _LogFileStream.WriteAsync(newLine, 0, newLine.Length);

        await _LogFileStream.FlushAsync();
    }

    private string GenerateLogMessage(ILogMessage message, string customFormat)
    {
        string messageAsString = customFormat;
        foreach (var prop in LogMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType?.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    public void Log(ILogMessage message, string format)
    {
        string messageAsString = GenerateLogMessage(message, format);
        _OutFunction?.Invoke(messageAsString, message.LogMessageType);
        LogToFile(messageAsString);
    }

    public void Log(ILogMessage message)
    {
        string messageAsString = GenerateLogMessage(message);
        _OutFunction?.Invoke(messageAsString, message.LogMessageType);
        LogToFile(messageAsString);
        
    }

    public void Log(string message) => Log(new LogMessage(message, string.Empty, LogType.Info));
    public void Log(string message, LogType logType, string format) => Log(new LogMessage(message, logType), format);
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object Sender) => Log(new LogMessage(message, Sender));
    public void Log(string message, object Sender, LogType type) => Log(new LogMessage(message, Sender, type));
    public void LogException(Exception exception, object Sender, bool logFullStack = false) => Log(LogMessage.CreateFromException(exception, Sender, logFullStack));

    public void SetOutFunction(Action<string, LogType> outFunction)
    {
        this._OutFunction = outFunction;
    }
}
