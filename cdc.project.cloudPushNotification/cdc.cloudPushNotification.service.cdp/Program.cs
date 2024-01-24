using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";

  
})
.AddOpenIdConnect(options =>
{
    options.RequireHttpsMetadata = false;


    options.Authority = builder.Configuration.GetSection("Keycloak")["ServerRealm"];
    options.ClientId = builder.Configuration.GetSection("Keycloak")["ClientId"];
    options.ClientSecret = builder.Configuration.GetSection("Keycloak")["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // �o��p�G�A�Q�qHttpContext���Token���ܧA�N�n�[�o�ӳ]�w
    options.SaveTokens = bool.Parse(builder.Configuration.GetSection("Keycloak")["SaveTokens"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
