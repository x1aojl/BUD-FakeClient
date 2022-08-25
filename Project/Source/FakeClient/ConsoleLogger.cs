using System;

public class ConsoleLogger
{
    public static void Info(string data)
    {
        WriteInColor(data, ConsoleColor.Green);
    }

    public static void Warn(string data)
    {
        WriteInColor(data, ConsoleColor.Yellow);
    }

    public static void Error(string data)
    {
        WriteInColor(data, ConsoleColor.Red);
    }

    private static void WriteInColor(string data, ConsoleColor color)
    {
        //Console.ForegroundColor = color;
        Console.WriteLine(data);
    }
}