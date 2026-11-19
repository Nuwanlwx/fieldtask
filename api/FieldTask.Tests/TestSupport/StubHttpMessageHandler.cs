using System.Net;
using System.Text;

namespace FieldTask.Tests.TestSupport;

// Stands in for the network: hands back pre-scripted responses in order and records every
// request, so tests can assert both what the service did with a reply and what it sent.
public class StubHttpMessageHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _next;

    public List<string> RequestBodies { get; } = [];
    public int CallCount => RequestBodies.Count;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        RequestBodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct));

        if (_next >= responses.Length)
            throw new InvalidOperationException($"Unexpected extra HTTP call #{_next + 1}.");

        return responses[_next++]();
    }

    public static Func<HttpResponseMessage> Json(HttpStatusCode status, string json) =>
        () => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

// IHttpClientFactory that always wraps the same stub handler. disposeHandler: false because the
// service disposes its HttpClient after each call, and the test still needs to inspect the handler.
public class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
