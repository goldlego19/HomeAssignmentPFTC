using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using HomeAssignmentPFTC.Services;
using HomeAssignmentPFTC.Interfaces;
using HomeAssignmentPFTC.DataAccess;
using Google.Cloud.SecretManager.V1; // 1. Add this namespace

namespace HomeAssignmentPFTC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();

            // Setup Credentials
            string? authPath = builder.Configuration["Authentication:Google:Credentials"];
            if (!string.IsNullOrEmpty(authPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", authPath);
            }

            // --- 2. LOAD SECRET FROM GOOGLE CLOUD SECRET MANAGER ---
            string projectId = builder.Configuration["Authentication:Google:ProjectId"] ?? "";
            
            try
            {
                // Create the client
                SecretManagerServiceClient client = SecretManagerServiceClient.Create();
                
                // Construct the resource name for the latest version of our secret
                SecretVersionName secretVersionName = new SecretVersionName(projectId, "oauth-client-secret", "latest");
                
                // Fetch the secret
                AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);
                
                // Overwrite the local configuration with the secure cloud payload
                builder.Configuration["Authentication:Google:ClientSecret"] = result.Payload.Data.ToStringUtf8();
                
                Console.WriteLine("Successfully loaded OAuth Client Secret from Secret Manager.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Failed to load secret from Google Cloud. Falling back to local secrets. Error: {ex.Message}");
            }
            // --------------------------------------------------------

            // Google Auth Schemes
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            }).AddCookie().AddGoogle(GoogleDefaults.AuthenticationScheme,options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                // This now uses the value dynamically loaded from the cloud!
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

            var app = builder.Build();

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