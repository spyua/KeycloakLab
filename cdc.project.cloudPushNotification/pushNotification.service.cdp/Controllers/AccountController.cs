using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _memoryCache;

        private readonly KeycloakOptions _keycloakOptions;

        public AccountController(ILogger<AccountController> logger
                               , IHttpClientFactory httpClientFactory
                               , IOptions<KeycloakOptions> keycloakOptions
                               , IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keycloakOptions = keycloakOptions.Value;
            _memoryCache = memoryCache;     
        }

        // 走SSO Midleware 配置在Program的AddAuthentication處
        [Authorize]
        [HttpGet(nameof(LoginSSO))]
        public async Task<string> LoginSSO()
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


        // 測試用
        [HttpGet(nameof(GetTokenCustomData))]
        public async Task<IActionResult> GetTokenCustomData()
        {
         
            var client = _httpClientFactory.CreateClient();
            var authUri = "https://ovs-cp-lnk-01-keycloak.gcubut.gcp.uwccb/realms/ChannelWeb/protocol/openid-connect/auth";

            var requestContent = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("client_id", "ChannelWebSSO"),
            new KeyValuePair<string, string>("response_type", "code"),
            new KeyValuePair<string, string>("scope", "openid"),
             new KeyValuePair<string, string>("redirect_uri", "https://localhost:51022/*?Token=E7E7668DDE87421E9068B978D54AA275&UserID=95352&EncTicket=F7DCB55B1A27C6294C04B81C83C45B3F&username=95352&langCode=ZHT&ssousername=95352&clientType=&site2pstoretoken=&AppName=X100206&Field=Int"),
            });

            var response = await client.PostAsync(authUri, requestContent);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                // 處理回應內容
                return Content(responseContent);
            }

            return BadRequest("無法從Keycloak獲得回應");
        }

        // 測試用
        [HttpGet(nameof(Login))]
        public async Task<string> Login()
        {
            
            try
            {
                // Debug config value
                var debugKeycloakConfig = JsonConvert.SerializeObject(_keycloakOptions);
                _logger.LogInformation(debugKeycloakConfig);
            }catch(Exception ex)
            {
                _logger.LogError("Json SerializeObject Fail");
            }

            // Step1:Post keycloak token endpoint
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
            var tokenResponse = await response.Content.ReadAsStringAsync();


            // Step2 : Parse response json in Cache       
            var tokenObj = JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);

            // 檢查ExpiresInSeconds是否有值
            if (tokenObj.ExpiresInSeconds.HasValue)
            {
                // 獲取ExpiresInSeconds的值
                long expiresIn = tokenObj.ExpiresInSeconds.Value;

                // 存儲Token到Cache
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // 設置Cache過期時間，(Token過期前一分鐘)
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(expiresIn - 60));
                _memoryCache.Set("access_token", tokenObj.AccessToken, cacheEntryOptions);
            }
            else
            {
                _logger.LogError("Token expiration time is missing.");
            }



            _logger.LogInformation("Token received: " + tokenResponse);
            return tokenResponse;
        }

        // 測試用
        [HttpGet(nameof(TestGet))]
        public string TestGet()
        {
            return "Test Get OK";
        }
    }
}
