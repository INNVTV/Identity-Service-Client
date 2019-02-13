using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServiceClient.Middleware.Authentication
{
    public class AuthenticationMiddleware
    {

        private readonly RequestDelegate next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context /* other dependencies */)
        {
            bool validToken = true;
            var cookies = context.Request.Cookies;

            if (!validToken)
            {
                context.Response.Redirect("");
            }
            else
            {
                await next.Invoke(context);
            }
        }
    }
}
