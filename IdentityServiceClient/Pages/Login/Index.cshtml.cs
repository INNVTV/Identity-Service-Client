using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.IdentityService;

namespace IdentityServiceClient.Pages.Login
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public AuthenticateUserCommand AuthenticateUser { get; set; }


        readonly IApplicationSettings _applicationSettings;

        public IndexModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public void OnGet()
        {
            ViewData["Message"] = String.Empty;
            AuthenticateUser = new AuthenticateUserCommand();
        }

        public async Task<IActionResult> OnPost()
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("X-API-KEY", "X-API-KEY-1");

                var authenticatonClient = new Services.IdentityService.AuthenticationClient("https://platform-identity-service-stage.azurewebsites.net", httpClient);
                var result = await authenticatonClient.UserAsync(AuthenticateUser);

                if (!result.IsSuccess.Value)
                {
                    ViewData["Message"] = result.Message;
                    return Page();
                }

                // CROSS DOMAIN COOKIE NOTES AND LOCAL DEBUGGING ----------------
                // You will need to be on the same domain to use this cookie on the application requiring authentication
                // To ease with local development it is recommended that you use the API endpoint to authenticate users from the application requring authentication.
                // This will require building out your own version of the login UI in your web or mobile app
                // You can still use the password reset, invitiation acceptance, and other UIs in this web app.

                var jwtCookieName = _applicationSettings.Cookies.JwtCookieName;
                var refreshTokenCookieName = _applicationSettings.Cookies.RefreshTokenCookieName;

                Response.Cookies.Delete(jwtCookieName);
                Response.Cookies.Delete(refreshTokenCookieName);

                Response.Cookies.Append(
                  jwtCookieName,
                  result.JwtToken,
                  new CookieOptions()
                  {
                      IsEssential = true,
                      HttpOnly = true,
                      Secure = true,
                      Expires = DateTime.UtcNow.AddHours(_applicationSettings.Cookies.CookieExpirationHours),
                      SameSite = SameSiteMode.Strict
                  });

                // Refresh tokens last longer than JWT tokens and can be used by an attacker to gain access to secure areas even after a JWT expires.
                // Since every request sends the cookies that belong to the domain we need to store our refresh token in a more secure manner in case they are ever intercepted.
                // We use a passphrase that is only known to the server to encrypt and decrypt ALL user reresh tokens
                // In a desktop/native app they should be stored encrypted until ready for use.

                Response.Cookies.Append(
                  refreshTokenCookieName,
                  // Encrypted Token
                  Common.Encryption.StringEncryption.EncryptString(
                      result.RefreshToken, _applicationSettings.JSONWebTokens.RefreshTokenEncryptionPassPhrase
                      ),
                  new CookieOptions()
                  {
                      IsEssential = true,
                      HttpOnly = true,
                      Secure = true,
                      Expires = DateTime.UtcNow.AddHours(_applicationSettings.Cookies.CookieExpirationHours),
                      SameSite = SameSiteMode.Strict
                  });

                return Redirect(Request.Query["returnUrl"]);
            } 
        }
    }
}