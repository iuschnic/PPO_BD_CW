using Microsoft.AspNetCore.Mvc.Testing;


public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetEndpoint_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/endpoint");
        response.EnsureSuccessStatusCode();
    }
}