using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using HomeAssignmentPFTC.Services;
using HomeAssignmentPFTC.Interfaces;
using HomeAssignmentPFTC.DataAccess;
using Microsoft.AspNetCore.HttpOverrides;

namespace HomeAssignmentPFTC
{
    public class Program
    {
        // 1. Update Main to be an async Task
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                // This will pull the connection string from your appsettings.json
                options.Configuration = builder.Configuration.GetConnectionString("RedisCache");
                options.InstanceName = "PFTC_Menu_";
            });
            builder.Services.AddHttpClient();

            string? authPath = builder.Configuration["Authentication:Google:Credentials"];
            if (!string.IsNullOrEmpty(authPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", authPath);
            }

            string projectId = builder.Configuration["Authentication:Google:ProjectId"] ?? "";
            
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
            var logger = loggerFactory.CreateLogger<GoogleSecretManagerService>();

            // Instantiate your custom service
            var secretManagerService = new GoogleSecretManagerService(projectId, logger);

            // Register it in Dependency Injection so you can inject it into Controllers if needed later
            builder.Services.AddSingleton<IGoogleSecretManagerService>(secretManagerService);

            try
            {
                // Load the secrets dynamically into builder.Configuration
                await secretManagerService.LoadSecretsIntoConfigurationAsync(builder.Configuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Failed to load secrets from Google Cloud. Error: {ex.Message}");
            }
            // ----------------------------------------------

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            }).AddCookie().AddGoogle(GoogleDefaults.AuthenticationScheme,options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]; 
                options.Scope.Add("profile");
                options.Events.OnCreatingTicket = ctx =>
                {
                    if (ctx.User.TryGetProperty("email", out var email))
                    {
                        ctx.Identity?.AddClaim(new System.Security.Claims.Claim("email", email.GetString() ?? ""));
                    }
                    if (ctx.User.TryGetProperty("picture", out var picture))
                    {
                        ctx.Identity?.AddClaim(new System.Security.Claims.Claim("picture", picture.GetString() ?? ""));
                    }
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<FirestoreRepository>();
            builder.Services.AddScoped<IBucketStorageService, BucketStorageService>();
            
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear(); 
            });

            var app = builder.Build();
            
            app.UseForwardedHeaders();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}