using keycloak_role_net6.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IAuthorizationHandler, KeycloakIDTokenGetRoleHandler>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Keycloak ���� Authentication�]�m
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/api/user/Login";
    options.Cookie.Name = builder.Configuration.GetSection("Keycloak")["CookieName"];
    options.Cookie.MaxAge = TimeSpan.FromMinutes(60);
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
}).AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration.GetSection("Keycloak")["ServerRealm"];

    // �u���˦b�}�o���Ҩϥ�
    options.RequireHttpsMetadata = bool.Parse(builder.Configuration.GetSection("Keycloak")["RequireHttpsMetadata"]);
    
    options.ClientId = builder.Configuration.GetSection("Keycloak")["ClientId"];
    options.ClientSecret = builder.Configuration.GetSection("Keycloak")["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;

    // �o��p�G�A�Q�qHttpContext���Token���ܧA�N�n�[�o�ӳ]�w
    options.SaveTokens = bool.Parse(builder.Configuration.GetSection("Keycloak")["SaveTokens"]);

    // Access Token �ѪR��ʩ�JClaims
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            // �ˬd�o�̬O�_������ Access Token
            if (context.TokenEndpointResponse == null)
            {
                Console.WriteLine("TokenEndpointResponse is null!!!!!!");
                return Task.CompletedTask;
            }
            var accessToken = context.TokenEndpointResponse.AccessToken;
            if (accessToken == null)
            {
                Console.WriteLine("AccessToken is null!!!!!!");
                return Task.CompletedTask;
            }
            if (context.Principal == null)
            {
                Console.WriteLine("Principal is null!!!!!!");
                return Task.CompletedTask;
            }

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;
            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

            if (jsonToken == null || claimsIdentity == null)
            {
                Console.WriteLine("jsonToken or claimsIdentity is null!!!!!!");
                return Task.CompletedTask;
            }

            var resourceName = builder.Configuration.GetSection("Keycloak")["ResourceTag"];
            var resourceAccess = jsonToken.Claims.FirstOrDefault(c => c.Type == resourceName)?.Value;
            if(resourceAccess == null)
            {
                Console.WriteLine("resourceAccess is null!!!!!!");
                return Task.CompletedTask;
            }

            var parsedResourceAccess = JObject.Parse(resourceAccess);

            if(parsedResourceAccess == null)
            {
                Console.WriteLine("parsedResourceAccess is null!!!!!!");
                return Task.CompletedTask;
            }

            var clientID = builder.Configuration.GetSection("Keycloak")["ClientId"]; 
            var roleTagName = builder.Configuration.GetSection("Keycloak")["RoleTag"];

            var roles = parsedResourceAccess[clientID][roleTagName];

            foreach (var role in roles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
            }

            return Task.CompletedTask;
        },
    };


});

List<string> requiredRoles = new List<string> { "read","write" };
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustHaveGetRole", policy =>
    {
        policy.Requirements.Add(new GetKeycloakRoleRequirement(requiredRoles));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
