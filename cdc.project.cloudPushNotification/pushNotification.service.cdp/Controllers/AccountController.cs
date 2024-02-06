using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using pushNotification.service.cdp.Core.Config;

namespace pushNotification.service.cdp.Controllers
{

    /// <summary>
    /// For SSO Login Test Usage
    /// </summary>
    [ApiController]
    [Route("api/user")]
    public class AccountController : ControllerBase
    {

        private readonly ILogger<AccountController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KeycloakOptions _keycloakOptions;
        public AccountController(ILogger<AccountController> logger
                               , IHttpClientFactory httpClientFactory
                               , IOptions<KeycloakOptions> keycloakOptions)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keycloakOptions = keycloakOptions.Value;
        }

        [HttpGet(nameof(Login))]
        public async Task<string> Login()
        {
            
            try
            {
                var debugKeycloakConfig = JsonConvert.SerializeObject(_keycloakOptions);
                _logger.LogInformation(debugKeycloakConfig);
            }catch(Exception ex)
            {
                _logger.LogError("Json SerializeObject Fail");
            }

            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", _keycloakOptions.GrantType),
                new KeyValuePair<string, string>("client_id", _keycloakOptions.ClientId),
                new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret)
            });

            var response = await client.PostAsync(_keycloakOptions.POSTTokenEndpoint, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error fetching token: {error}");
                return "Error fetching token";
            }

            var token = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Token received: " + token);
            return token;
        }

        // 配置在Program的AddAuthentication處
        [Authorize]
        [HttpGet(nameof(LoginAuthorize))]
        public async Task<string> LoginAuthorize()
        {
            _logger.LogInformation("Login sucess");

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            _logger.LogInformation("access_token:" + accessToken);

            var idToken = await HttpContext.GetTokenAsync("id_token");
            _logger.LogInformation("idToken:" + idToken);

            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            _logger.LogInformation("refreshToken:" + refreshToken);

            return "SSO Auth check ok";
        }

        // For Get Test Debug使用
        [HttpGet(nameof(TestGet))]
        public string TestGet()
        {
            return "Test Get OK";
        }
    }
}
