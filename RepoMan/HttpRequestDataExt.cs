using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace RepoMan;

internal static class HttpRequestDataExt
{
    public static HttpResponseData CreateBadRequestResponse(this HttpRequestData request, string message)
    {
        HttpResponseData response = request.CreateResponse();
        response.StatusCode = HttpStatusCode.BadRequest;
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString(message);
        return response;
    }

    public static HttpResponseData CreateBadRequestResponse(this HttpRequestData request)
    {
        HttpResponseData response = request.CreateResponse();
        response.StatusCode = HttpStatusCode.BadRequest;
        return response;
    }
}
