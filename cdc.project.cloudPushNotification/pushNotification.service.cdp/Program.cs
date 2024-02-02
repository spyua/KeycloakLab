using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using pushNotification.service.cdp;
using pushNotification.service.cdp.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(options =>
{
    var cloudConfig = new CloudConfig();
    cloudConfig.ProjectId = builder.Configuration.GetSection("CloudConfig")["ProjectId"];
    cloudConfig.TopicId = builder.Configuration.GetSection("CloudConfig")["TopicId"];
    cloudConfig.SubscriptionId = builder.Configuration.GetSection("CloudConfig")["SubscriptionId"];
    return cloudConfig;
});

// 注冊 Pub/Sub 訂閱者服務
builder.Services.AddHostedService<PubSubSubscriberService>();

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
    // TODO:待改. 目前無用到...
    options.LoginPath = "/Account/Login";
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration.GetSection("Keycloak")["ServerRealm"];
    options.ClientId = builder.Configuration.GetSection("Keycloak")["ClientId"];
    options.ClientSecret = builder.Configuration.GetSection("Keycloak")["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;

    options.RequireHttpsMetadata = false;
    options.SaveTokens = true;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };

    // For  Authentication and Authorization print log  
    options.Events = new OpenIdConnectEvents
    {
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
