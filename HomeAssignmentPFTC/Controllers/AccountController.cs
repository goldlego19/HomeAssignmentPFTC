using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
namespace HomeAssignmentPFTC.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            string? redirectURL = Url.Action("GoogleResponse", "Account");
            AuthenticationProperties properties = new AuthenticationProperties { RedirectUri = redirectURL };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (!result.Succeeded )
            {
                return RedirectToAction("Login");
            }
            return RedirectToAction("Index", "Home");
        }


        public IActionResult Logout()
        {
            return SignOut("Cookies");
        }

    }
}
