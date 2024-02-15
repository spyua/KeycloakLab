using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using pushNotification.service.cdp.Core.Config;
using pushNotification.service.cdp.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.Configure<CloudOption>(builder.Configuration.GetSection("CloudOption"));

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<PubSubSubscriberService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();



// SSO Midleware 配置參考
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(options =>
{
    var keycloakOptions = builder.Configuration.GetSection("Keycloak").Get<KeycloakOptions>();

    options.Authority = keycloakOptions.ServerRealmEndpoint;
    options.ClientId = keycloakOptions.ClientId;
    options.ClientSecret = keycloakOptions.ClientSecret;
    options.ResponseType = OpenIdConnectResponseType.Code;

    options.CallbackPath = new PathString(keycloakOptions.CallbackPath);

    options.RequireHttpsMetadata = false;
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles",
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = options.Authority,
        ValidAudience = options.ClientId,
        // and other parameters to validate
    };


    // GKE走內網.需要設這個.
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    // For  Authentication and Authorization print log  
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // 這段For特規系統流程.照理來說IDP

            // 獲取當前請求的基礎URL部分（協議+主機名+端口）
            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // 假設你想要將原始的查詢參數保持不變並加到這個基礎URL後面
            var originalQueryString = request.QueryString.Value;

            // 組合成完整的URL
            var redirectUrl = baseUrl + request.Path + originalQueryString;

            var entireQueryString = context.HttpContext.Request.QueryString.ToString();
            Console.WriteLine($"Captured Query String: {entireQueryString}");

            if (!string.IsNullOrEmpty(entireQueryString))
            {
                context.ProtocolMessage.SetParameter("client_id", keycloakOptions.ClientId);
                context.ProtocolMessage.SetParameter("response_type", "code");
                context.ProtocolMessage.SetParameter("scope", "openid");
                context.ProtocolMessage.SetParameter("redirect_uri", redirectUrl);
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = ctx =>
        {         
            Console.WriteLine($"Token validated for {ctx.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine($"Authentication failed: {ctx.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CDP API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure forwarding headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
