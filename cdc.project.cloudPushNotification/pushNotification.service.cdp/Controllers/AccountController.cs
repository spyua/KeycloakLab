using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using pushNotification.service.cdp.Core.Config;
using System.Net.Http.Headers;
using System.Web;

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
        private readonly KeycloakOptions _keycloakConfig;

        public AccountController(ILogger<AccountController> logger
                               , IHttpClientFactory httpClientFactory
                               , IOptions<KeycloakOptions> keycloakOptions
                               , IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keycloakConfig = keycloakOptions.Value;
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

        // 測試用(打Keycloak Auth API)
        [HttpGet(nameof(CustomLoginSSO))]
        public IActionResult CustomLoginSSO()
        {
            // Keycloak的授權端點URL
            var authRequestUri = "http://localhost:8082/realms/api-role-lab/protocol/openid-connect/auth";

            _memoryCache.Set("querystr", Request.QueryString);
            // 重導向URI - 用戶完成登入後將被重導回此URI，並附帶授權碼
            var redirectUri = $"https://localhost:7297/api/user/CustomLoginSSOCallback{Request.QueryString}";

            // 構建授權請求的查詢參數
            var queryParams = new Dictionary<string, string>
             {
                 {"client_id", _keycloakConfig.ClientId},
                 {"response_type", "code"},
                 {"scope", "openid"},
                 {"redirect_uri", redirectUri}
                 // 可選：如果需要，可以加入"state"和"nonce"參數以增強安全性
             };

            // 將查詢參數轉換為URL編碼的字串
            var queryString = string.Join("&", queryParams.Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));

            // 完整的重導向URL
            var finalRedirectUri = $"{authRequestUri}?{queryString}";

            // 重導向到Keycloak的登入頁面
            return Redirect(finalRedirectUri);
        }
        
        [HttpGet(nameof(CustomLoginSSOCallback))]
        public async Task<IActionResult> CustomLoginSSOCallback(string code) // 接收授權碼
        {
            var tokenEndpoint = "http://localhost:8082/realms/api-role-lab/protocol/openid-connect/token";
            var client = _httpClientFactory.CreateClient("LogClient");

            var idpQueryInfo = _memoryCache.Get("querystr");
            // 確保這裡的redirect_uri與原始授權請求中的redirect_uri完全一致
            var redirectUri = $"https://localhost:7297/api/user/CustomLoginSSOCallback{idpQueryInfo}";

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                 new KeyValuePair<string, string>("grant_type", "authorization_code"),
                 new KeyValuePair<string, string>("client_id", _keycloakConfig.ClientId),
                 new KeyValuePair<string, string>("client_secret", _keycloakConfig.ClientSecret), // 如果客戶端是機密的，需要此參數
                 new KeyValuePair<string, string>("code", code),
                 new KeyValuePair<string, string>("redirect_uri", redirectUri)
             });

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            var response = await client.PostAsync(tokenEndpoint, tokenRequestContent);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                // 解析回應內容來獲取令牌
                // 處理令牌（例如，保存令牌，使用令牌調用受保護的資源等）
                return Ok(responseContent); // 或者將用戶導向另一個頁面
            }

            return BadRequest("無法從Keycloak獲得令牌");
        }

        // 測試用(打Keycloak API索取Token)
        [HttpGet(nameof(Login))]
        public async Task<string> Login()
        {
            
            try
            {
                // Debug config value
                var debugKeycloakConfig = JsonConvert.SerializeObject(_keycloakConfig);
                _logger.LogInformation(debugKeycloakConfig);
            }catch(Exception ex)
            {
                _logger.LogError("Json SerializeObject Fail");
                _logger.LogError(ex.ToString());
            }

            // Step1:Post keycloak token endpoint
            var client = _httpClientFactory.CreateClient();

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", _keycloakConfig.GrantType),
                new KeyValuePair<string, string>("client_id", _keycloakConfig.ClientId),
                new KeyValuePair<string, string>("client_secret", _keycloakConfig.ClientSecret)
            });

            var response = await client.PostAsync(_keycloakConfig.POSTTokenEndpoint, requestBody);
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
        [Authorize]
        [HttpGet(nameof(TestGet))]
        public string TestGet()
        {
            return "RESTful API Work, This is Login API";
        }
    }
}
