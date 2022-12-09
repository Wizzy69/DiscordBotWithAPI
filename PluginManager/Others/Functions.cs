﻿namespace PluginManager.Others;

/// <summary>
///     A special class with functions
/// </summary>
public static class Functions
{
    /// <summary>
    ///     The location for the Resources folder
    /// </summary>
    public static readonly string dataFolder = @"./Data/Resources/";

    /// <summary>
    ///     Get the Operating system you are runnin on
    /// </summary>
    /// <returns>An Operating system</returns>
    public static OperatingSystem GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OperatingSystem.WINDOWS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OperatingSystem.LINUX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OperatingSystem.MAC_OS;
        return OperatingSystem.UNKNOWN;
    }

    public static List<string> GetArguments(SocketMessage message)
    {
        var command = new Command(message);
        return command.Arguments;
    }

    /// <summary>
    ///     Copy one Stream to another <see langword="async" />
    /// </summary>
    /// <param name="stream">The base stream</param>
    /// <param name="destination">The destination stream</param>
    /// <param name="bufferSize">The buffer to read</param>
    /// <param name="progress">The progress</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <exception cref="ArgumentNullException">Triggered if any <see cref="Stream" /> is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Triggered if <paramref name="bufferSize" /> is less then or equal to 0</exception>
    /// <exception cref="InvalidOperationException">Triggered if <paramref name="stream" /> is not readable</exception>
    /// <exception cref="ArgumentException">Triggered in <paramref name="destination" /> is not writable</exception>
    public static async Task CopyToOtherStreamAsync(this Stream stream, Stream destination, int bufferSize,
                                                    IProgress<long>? progress = null,
                                                    CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (!stream.CanRead) throw new InvalidOperationException("The stream is not readable.");
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream is not writable", nameof(destination));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    /// <summary>
    ///     Save to JSON file
    /// </summary>
    /// <typeparam name="T">The class type</typeparam>
    /// <param name="file">The file path</param>
    /// <param name="Data">The values</param>
    /// <returns></returns>
    public static async Task SaveToJsonFile<T>(string file, T Data)
    {
        var str = new MemoryStream();
        await JsonSerializer.SerializeAsync(str, Data, typeof(T), new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllBytesAsync(file, str.ToArray());
    }

    /// <summary>
    ///     Convert json text or file to some kind of data
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    /// <param name="input">The file or json text</param>
    /// <returns></returns>
    public static async Task<T> ConvertFromJson<T>(string input)
    {
        Stream text;
        if (File.Exists(input))
            text = new MemoryStream(await File.ReadAllBytesAsync(input));
        else
            text = new MemoryStream(Encoding.ASCII.GetBytes(input));
        text.Position = 0;
        var obj = await JsonSerializer.DeserializeAsync<T>(text);
        text.Close();
        return (obj ?? default)!;
    }
}