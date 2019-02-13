using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServiceClient.Middleware.Authentication
{
    //-----------------------------------------------------
    // The following extension method exposes the middleware through IApplicationBuilder:
    // giving us the ability to use app.UseAuthenticationMiddleware() inside of: Startup.Configure(IApplicationBuilder app)
    //---------------------------------------------------------
    
    // Without this extension method you would just call: app.UseMiddleware(typeof(AuthenticationMiddleware)); 
    
    public static class AuthenticationMiddlewareExtension
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }

}
