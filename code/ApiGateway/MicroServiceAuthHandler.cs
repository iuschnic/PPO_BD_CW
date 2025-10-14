using Ocelot.Logging;
using System.Net;

public class MicroserviceAuthHandler : DelegatingHandler
{
    private readonly IOcelotLogger _logger;
    private const string MicroserviceSecret = "microservice-secret-key-2024";

    public MicroserviceAuthHandler(IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MicroserviceAuthHandler>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Проверяем заголовок микросервиса
        if (!request.Headers.Contains("X-Microservice-Auth"))
        {
            _logger.LogWarning($"Missing microservice auth header for request to {request.RequestUri}");
            return CreateUnauthorizedResponse("Microservice authentication header required");
        }

        var authHeader = request.Headers.GetValues("X-Microservice-Auth").FirstOrDefault();
        if (authHeader != MicroserviceSecret)
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