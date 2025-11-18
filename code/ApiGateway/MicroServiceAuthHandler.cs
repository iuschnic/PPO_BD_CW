using Ocelot.Logging;
using System.Net;

public record MicroserviceAuthHandlerArgs
{
    public required string SecretKey { get; set; }
}

public class MicroserviceAuthHandler : DelegatingHandler
{
    private readonly IOcelotLogger _logger;
    private readonly string _secretKey;

    public MicroserviceAuthHandler(IOcelotLoggerFactory loggerFactory, MicroserviceAuthHandlerArgs args)
    {
        _logger = loggerFactory.CreateLogger<MicroserviceAuthHandler>();
        _secretKey = args.SecretKey;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-Microservice-Auth"))
        {
            _logger.LogWarning($"Missing microservice auth header for request to {request.RequestUri}");
            return CreateUnauthorizedResponse("Microservice authentication header required");
        }

        var authHeader = request.Headers.GetValues("X-Microservice-Auth").FirstOrDefault();
        if (authHeader != _secretKey)
        {
            _logger.LogWarning($"Missing microservice auth header for request to {request.RequestUri}");
            return CreateUnauthorizedResponse("Invalid microservice authentication");
        }

        _logger.LogDebug($"Microservice authentication successful for {request.RequestUri}");
        return await base.SendAsync(request, cancellationToken);
    }

    private HttpResponseMessage CreateUnauthorizedResponse(string message)
    {
        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "MICROSERVICE_AUTH_REQUIRED",
                message = message,
                timestamp = DateTime.UtcNow
            }), System.Text.Encoding.UTF8, "application/json")
        };
    }
}