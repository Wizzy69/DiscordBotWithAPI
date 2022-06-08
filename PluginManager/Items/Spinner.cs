using System;
using System.Threading.Tasks;
using PluginManager.Others.Exceptions;

namespace PluginManager.Items;

public class Spinner
{
    /// <summary>
    ///     True if active, false otherwise
    /// </summary>
    public bool isSpinning;

    /// <summary>
    ///     The Spinner constructor
    /// </summary>
    public Spinner()
    {
        isSpinning = false;
    }

    /// <summary>
    ///     The method that is called to start spinning the spinner
    /// </summary>
    public async void Start()
    {
        isSpinning = true;
        var cnt = 0;

        while (isSpinning)
        {
            cnt++;
            switch (cnt % 4)
            {
                case 0:
                    Console.Write("/");
                    break;
                case 1:
                    Console.Write("-");
                    break;
                case 2:
                    Console.Write("\\");
                    break;
                case 3:
                    Console.Write("|");
                    break;
            }

            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            await Task.Delay(250);
        }
    }

    /// <summary>
    ///     The method that is called to stop the spinner from spinning
    /// </summary>
    public void Stop()
    {
        if (!isSpinning) throw new APIException("Spinner was not spinning", GetType());
        isSpinning = false;
    }
}
