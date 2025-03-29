using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Campus_SMS.Data;
using Campus_SMS.Entities.User;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Okta.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using OpenAI.Examples;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri")!);
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
    var secretClient = new SecretClient(
        new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
        new DefaultAzureCredential());

    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()));

var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
logger.LogTrace($"Checcking builder configuration from program.cs file " +
                $"API Key - {builder.Configuration["OpenAI:RobertAPIKey"]}" +
                $"Account SID - {builder.Configuration["Twilio:AccountSID"]}" +
                $"Auth Token - {builder.Configuration["Twilio:AuthToken"]}" +
                $"From Number - {builder.Configuration["Twilio:FromPhoneNumber"]}");

//Inject the configuration into dependency injection -> Used by SmsService and other services
builder.Services.AddSingleton(builder.Configuration);

builder.Services.AddTransient<SmsService>();
builder.Services.AddTransient<AiService>();
builder.Services.AddTransient<AiServiceVectorStore>();

builder.Services.AddDefaultIdentity<AppUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

if (builder.Environment.IsProduction())
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Set DefaultAuthenticateScheme
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Set DefaultScheme
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        })
        .AddOpenIdConnect("Auth0", options =>
        {
            options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
            options.ClientId = builder.Configuration["Auth0:ClientId"];
            options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
            options.CallbackPath = builder.Configuration["Auth0:CallbackPath"];

            options.ResponseType = "code";
            options.SaveTokens = true;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = (context) =>
                {
                    var logoutUri = $"https://{builder.Configuration["Auth0:Domain"]}/v2/logout?client_id={builder.Configuration["Auth0:ClientId"]}";
                    context.Response.Redirect(logoutUri);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

var app = builder.Build();

//Automatically Apply DB migrations
using var scope = app.Services.CreateScope();
using var appContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//Apply DB migrations
 appContext.Database.Migrate();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
