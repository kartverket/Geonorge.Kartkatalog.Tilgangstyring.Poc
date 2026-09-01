using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Kartverket.Metadatakatalog.Controllers;

public class TokenExchangeController(
    Geonorge.Utilities.Organization.IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TokenExchangeController> logger)
    : Controller
{
    private readonly string _texasUrl = configuration["TEXAS_URL"];

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
        var authorization = Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var userToken = authorization["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(userToken))
        {
            return Unauthorized();
        }

        logger.LogInformation("Usertoken: {userToken}", userToken);

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{_texasUrl}/api/v1/token/exchange"),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new ExchangeRequest
            {
                IdentityProvider = "tokenx", UserToken = userToken,
                Target = "atkv3-dev:geonorge-nedlasting-tilgangstyring:geonorge-nedlasting-api"
            })
        };
        var response = await httpClientFactory.GetHttpClient().SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Texas call failed, {response} {body}", response,
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
            logger.LogError(e, "Could not parse response body from Texas service");
            return UnprocessableEntity();
        }
    }
}