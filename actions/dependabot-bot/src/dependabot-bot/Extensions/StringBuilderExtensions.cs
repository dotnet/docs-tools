namespace Dependabot.Bot.Extensions;

internal static class StringBuilderExtensions
{
    internal static void WriteLineToBufferAndOutput(
        this StringBuilder buffer, string content, bool isLimitReached)
    {
        if (isLimitReached)
        {
            WriteLine("LIMIT REACHED, OVERFLOW IS DISCARDED!");
            WriteLine(content);
        }
        else
        {
            buffer.AppendLine(content);
            WriteLine(content);
        }
    }
}
