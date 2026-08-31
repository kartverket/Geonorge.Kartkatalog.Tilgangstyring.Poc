using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Kartverket.Metadatakatalog.Controllers;

public class TokenExchangeController : Controller
{
    private readonly Geonorge.Utilities.Organization.IHttpClientFactory _httpClientFactory;
    private readonly string _texasUrl;
    private readonly ILogger<TokenExchangeController> _logger;

    public TokenExchangeController(Geonorge.Utilities.Organization.IHttpClientFactory httpClientFactory,
        IConfiguration configuration, ILogger<TokenExchangeController> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _texasUrl = configuration["TEXAS_URL"];
    }

    private struct ExchangeRequest
    {
        [JsonPropertyName("identity_provider")]
        public string IdentityProvider { get; set; }

        [JsonPropertyName("user_token")] public string UserToken { get; set; }
        [JsonPropertyName("target")] public string Target { get; set; }
    }

    private struct ExchangeResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public string ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; }
    }

    [Route("api/token-exchange")]
    public async Task<IActionResult> ExchangeToken()
    {
        if (string.IsNullOrEmpty(Request.Headers.Authorization))
        {
            return Unauthorized();
        }

        var userToken = Request.Headers.Authorization[0];
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{_texasUrl}/api/v1/token/exchange"),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new ExchangeRequest
                { IdentityProvider = "ansattporten", UserToken = userToken, Target = "" })
        };
        var response = await _httpClientFactory.GetHttpClient().SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Texas call failed, {response} {body}", response,
                body);
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
            _logger.LogError(e, "Could not parse response body from Texas service");
            return UnprocessableEntity();
        }
    }
}