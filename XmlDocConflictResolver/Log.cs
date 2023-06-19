public static class Log
{
    public static void Print(bool endline, ConsoleColor foregroundColor, string format, params object[]? args)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = foregroundColor;

        string msg = args != null ? (args.Length > 0 ? string.Format(format, args) : format) : format;
        if (endline)
        {
            Console.WriteLine(msg);
        }
        else
        {
            Console.Write(msg);
        }
        Console.ForegroundColor = originalColor;
    }

    public static void Info(string format) => Info(format, null);

    public static void Info(string format, params object[]? args) => Info(true, format, args);

    public static void Info(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.White, format, args);

    public static void Success(string format) => Success(format, null);

    public static void Success(string format, params object[]? args) => 
        Success(true, format, args);

    public static void Success(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.Green, format, args);

    public static void Warning(string format) => Warning(format, null);

    public static void Warning(string format, params object[]? args) => 
        Warning(true, format, args);

    public static void Warning(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.Yellow, format, args);

    public static void Error(string format) => Error(format, null);

    public static void Error(string format, params object[]? args) => 
        Error(true, format, args);

    public static void Error(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.Red, format, args);

    public static void Cyan(string format) => Cyan(format, null);

    public static void Cyan(string format, params object[]? args) => 
        Cyan(true, format, args);

    public static void Cyan(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.Cyan, format, args);

    public static void Magenta(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.Magenta, format, args);

    public static void Magenta(string format) => Magenta(format, null);

    public static void Magenta(string format, params object[]? args) => 
        Magenta(true, format, args);

    public static void DarkYellow(bool endline, string format, params object[]? args) => 
        Print(endline, ConsoleColor.DarkYellow, format, args);

    public static void DarkYellow(string format) => DarkYellow(format, null);

    public static void DarkYellow(string format, params object[]? args) => 
        DarkYellow(true, format, args);

    public static void Assert(bool condition, string format) => 
        Assert(true, condition, format, null);

    public static void Assert(bool condition, string format, params object[]? args) => 
        Assert(true, condition, format, args);

    public static void Assert(bool endline, bool condition, string format, params object[]? args)
    {
        if (condition)
        {
            Success(endline, format, args);
        }
        else
        {
            string msg = args != null ? string.Format(format, args) : format;
            throw new Exception(msg);
        }
    }

    public static void Line() => Print(endline: true, Console.ForegroundColor, "", null);

    public delegate void PrintHelpFunction();

    public static void ErrorAndExit(string format, params object[]? args)
    {
        Error(format, args);
        Cyan("Use the -h|-help argument to view the usage instructions.");
        Environment.Exit(-1);
    }
}