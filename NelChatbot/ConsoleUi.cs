namespace NelChatbot;

public static class ConsoleUi
{
    public static void WriteDim(string text) => WriteColored(text, ConsoleColor.DarkGray);

    public static void WriteError(string text) => WriteColored(text, ConsoleColor.Red);

    public static void WriteColored(string text, ConsoleColor color, bool newLine = true)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text); else Console.Write(text);
        Console.ForegroundColor = previous;
    }
}
