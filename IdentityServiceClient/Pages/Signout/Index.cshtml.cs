using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServiceClient.Pages.Signout
{
    public class IndexModel : PageModel
    {
        readonly IApplicationSettings _applicationSettings;

        public IndexModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public async Task<IActionResult> OnGet()
        {
            var jwtCookieName = _applicationSettings.Cookies.JwtCookieName;
            var refreshTokenCookieName = _applicationSettings.Cookies.RefreshTokenCookieName;

            Response.Cookies.Delete(jwtCookieName);
            Response.Cookies.Delete(refreshTokenCookieName);

            return Redirect("/login");
        }
    }
}