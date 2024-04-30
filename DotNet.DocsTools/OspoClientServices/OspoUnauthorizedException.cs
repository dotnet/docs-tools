// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using System.Net;

namespace Microsoft.DotnetOrg.Ospo;

[Serializable]
public class OspoUnauthorizedException : OspoException
{
    public OspoUnauthorizedException()
    {
    }

    public OspoUnauthorizedException(string? message)
        : base(message)
    {
    }

    public OspoUnauthorizedException(string? message, Exception? inner)
        : base(message, inner)
    {
    }

    public OspoUnauthorizedException(string? message, HttpStatusCode code)
        : base(message, code)
    {
    }
}