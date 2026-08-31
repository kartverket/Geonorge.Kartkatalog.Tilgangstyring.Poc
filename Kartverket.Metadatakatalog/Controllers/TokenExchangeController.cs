using System.Text.Json;
using System.Text.Json.Serialization;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kartverket.Metadatakatalog.Controllers;

public class TokenExchangeController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenExchangeController> _logger;

    public TokenExchangeController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TokenExchangeController> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        var texasUrl = configuration["TEXAS_URL"];
        _logger.LogInformation("Texas url: {}", texasUrl);
        _httpClient.BaseAddress = new Uri(texasUrl!);
    }

    private struct ExchangeRequest
    {
        [JsonPropertyName("identity_provider")]
        public string IdentityProvider { get; set; }
        [JsonPropertyName("user_token")]
        public string UserToken { get; set; }
        [JsonPropertyName("target")]
        public string Target { get; set; }
    }
    private struct ExchangeResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public string expiresIn { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }

    [Route("api/token-exchange")]
    public async Task<IActionResult> ExchangeToken()
    {
        if (Request.Headers.Authorization.IsNullOrEmpty())
        {
            return Unauthorized();
        }
        var userToken = Request.Headers.Authorization[0];
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri("/api/v1/token/exchange"),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new ExchangeRequest
                { IdentityProvider = "ansattporten", UserToken = userToken, Target = "<nedlastingsapi>" })
        };
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return new StatusCodeResult(503);
        }
        try
        {
            var responseBody = await response.Content.ReadFromJsonAsync<ExchangeResponse>();

            Response.Cookies.Append(
                "oidcAccessToken",
                responseBody.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Could not parse response body from Texas service");
            return UnprocessableEntity();
        }
    }
}