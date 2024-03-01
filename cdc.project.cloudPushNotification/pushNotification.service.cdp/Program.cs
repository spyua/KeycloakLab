using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using pushNotification.service.cdp.Core.Config;
using pushNotification.service.cdp.Hanlder;
using pushNotification.service.cdp.Middleware;
using pushNotification.service.cdp.Service;

var builder = WebApplication.CreateBuilder(args);


// 配置服務日誌
//builder.Services.AddLogging();


builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.Configure<CloudOption>(builder.Configuration.GetSection("CloudOption"));

builder.Services.AddTransient<LoggingDelegatingHandler>();
//builder.Services.AddHttpClient();
builder.Services.AddHttpClient("LogClient")
        .AddHttpMessageHandler<LoggingDelegatingHandler>();

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

    //options.CallbackPath = new PathString(keycloakOptions.CallbackPath);

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
            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            // For keycloak plugin處理，append QueryString
            var originalQueryString = request.QueryString.Value;
            context.ProtocolMessage.RedirectUri = context.ProtocolMessage.RedirectUri + originalQueryString;
            Console.WriteLine($"Redirect to identityProvider, the RedirectUri is {context.ProtocolMessage.RedirectUri}");
            return Task.CompletedTask;
        },

        OnAuthorizationCodeReceived = context =>
        {
            Console.WriteLine($"Authorization code received, the code is {context.ProtocolMessage.Code}");
            return Task.CompletedTask;
        },
 
        OnTokenValidated = context =>
        {         
            Console.WriteLine($"Token validated for {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
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

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
