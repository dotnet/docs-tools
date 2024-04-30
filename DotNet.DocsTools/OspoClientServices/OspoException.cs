// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using System.Net;

namespace Microsoft.DotnetOrg.Ospo;

[Serializable]
public class OspoException : Exception
{
    public OspoException()
    {
    }

    public OspoException(string? message)
        : base(message)
    {
    }

    public OspoException(string? message, Exception? inner)
        : base(message, inner)
    {
    }

    public OspoException(string? message, HttpStatusCode code)
        : this(message)
    {
        Code = code;
    }

    public HttpStatusCode Code { get; }
}