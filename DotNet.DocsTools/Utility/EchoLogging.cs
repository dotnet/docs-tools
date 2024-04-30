namespace DotNet.DocsTools.Utility;

public static class EchoLogging
{
    /// <summary>
    /// Simple function return an indented string.
    /// </summary>
    /// <param name="level">How many spaces to add.</param>
    /// <remarks>Instead of using the name <code>Indent</code>, I chose <code>Ind</code> to shorten string interpolation usage.</remarks>
    public static string Ind(int level) =>
        new(' ', level);

    /// <summary>
    /// Creates a GitHub logging group with the specified title.
    /// </summary>
    /// <param name="title">The title to display for the group.</param>
    /// <remarks>
    /// Sends <code>::group::{title}</code> output to the console.
    /// </remarks>
    public static void CreateGroup(string title) =>
        Console.WriteLine($"::group::{title}");

    /// <summary>
    /// Ends the current GitHub logging group.
    /// </summary>
    /// <remarks>
    /// Sends <code>::endgroup::</code> output to the console.
    /// </remarks>
    public static void EndGroup() =>
        Console.WriteLine("::endgroup::");

    /// <summary>
    /// Writes the <paramref name="message"/> using <see cref="Console.WriteLine(string?)"/>. Prefixes the output with the number of spaces indicated by <paramref name="indentation"/>.
    /// </summary>
    /// <param name="indentation">The amount of spaces to prefix the message with.</param>
    /// <param name="message">The message to output.</param>
    public static void Write(int indentation, string? message) =>
        Console.WriteLine($"{Ind(indentation)}{message}");

    /// <summary>
    /// Writes the <paramref name="message"/> using <see cref="Console.WriteLine(object?)"/>. Prefixes the output with the number of spaces indicated by <paramref name="indentation"/>.
    /// </summary>
    /// <param name="indentation">The amount of spaces to prefix the message with.</param>
    /// <param name="message">The message to output.</param>
    public static void Write(int indentation, object? message) =>
        Console.WriteLine($"{Ind(indentation)}{message}");

}
