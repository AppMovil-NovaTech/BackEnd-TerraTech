using System.Net;

public sealed class RecordingEmailHandler(HttpStatusCode status) : HttpMessageHandler
{
    public string? RequestBody { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public Uri? RequestUri { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        IdempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
        RequestUri = request.RequestUri;
        return new HttpResponseMessage(status);
    }
}
