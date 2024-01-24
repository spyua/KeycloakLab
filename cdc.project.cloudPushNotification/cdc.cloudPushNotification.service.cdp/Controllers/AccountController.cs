using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cdc.cloudPushNotification.service.cdp.Controllers
{

    [ApiController]
    [Route("api/user")]
    public class AccountController : ControllerBase
    {

        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        [HttpGet(nameof(Login))]
        public async Task<string> Login()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var idToken = await HttpContext.GetTokenAsync("id_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            return "auth check ok";
        }

    }
}
