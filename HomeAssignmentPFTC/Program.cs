using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace HomeAssignmentPFTC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", builder.Configuration["Authentication:Google:Credentials"]);

            //Google Auth Schemes
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
                    var email = ctx.User.GetProperty("email").GetString();
                    var picture = ctx.User.GetProperty("picture").GetString();
                    if (!string.IsNullOrEmpty(picture))
                    {
                        ctx.Identity.AddClaim(new System.Security.Claims.Claim("picture", picture));
                    }
                    ctx.Identity.AddClaim(new System.Security.Claims.Claim("email", email));

                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllersWithViews();
            //builder.Services.AddScoped<FirestoreRepository>();
            //builder.Services.AddScoped<IBucketStorageService, BucketStorageService>();


            var app = builder.Build();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
