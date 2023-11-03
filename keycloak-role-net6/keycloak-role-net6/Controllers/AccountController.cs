using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace keycloak_role_net6.Controllers
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

            var tokenMessage = new StringBuilder();
            tokenMessage.AppendLine("access token:" + accessToken);
            tokenMessage.AppendLine("id token:"+ idToken);
            tokenMessage.AppendLine("refresh token:" + refreshToken);
            return tokenMessage.ToString();
        }

        // �ȼg ((���T���k����B�z)
        [Authorize]
        [HttpGet(nameof(Logout))]
        public async Task<IActionResult> Logout()
        {
            // �n�X�᪺���w�VURL
            var redirectUri = Url.Action(nameof(Login));

            // �]�m�n�X�ݩʨë��w���w�VURL
            var properties = new AuthenticationProperties { RedirectUri = redirectUri };

            return SignOut(properties,
                OpenIdConnectDefaults.AuthenticationScheme,
                CookieAuthenticationDefaults.AuthenticationScheme);

        }


        
        /* ���ըϥ�
        [HttpGet("trigger-auth")]
        public IActionResult TriggerAuth()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
        }
        */

    }
}