using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServiceClient.Common.Settings
{
    /// <summary>
    /// We use the IApplicationSettings type as a way to inject settings
    /// and resource connections into our classes from our main entry points.
    /// </summary>
    public class ApplicationSettings : IApplicationSettings
    {
        public ApplicationSettings(IConfiguration configuration)
        {
            // New up our root classes
            Application = new ApplicationDetails();
            Cookies = new Cookies();
            Hosting = new HostingConfiguration();
            JSONWebTokens = new JWTConfiguration();
            Endpoints = new EndpointSettings();
            IdentityService = new IdentityService();

            // Map appsettings.json
            Application.Name = configuration.GetSection("Application").GetSection("Name").Value;

            Cookies.CookieExpirationHours = Convert.ToInt32(configuration.GetSection("Cookies").GetSection("CookieExpirationHours").Value);
            Cookies.JwtCookieName = configuration.GetSection("Cookies").GetSection("JwtCookieName").Value;
            Cookies.RefreshTokenCookieName = configuration.GetSection("Cookies").GetSection("RefreshTokenCookieName").Value;

            JSONWebTokens.RefreshTokenEncryptionPassPhrase = configuration.GetSection("JWT").GetSection("RefreshTokenEncryptionPassPhrase").Value;
            JSONWebTokens.Issuer = configuration.GetSection("JWT").GetSection("Issuer").Value;
            JSONWebTokens.Audience = configuration.GetSection("JWT").GetSection("Audience").Value;
            JSONWebTokens.PublicKeyXmlString = configuration.GetSection("JWT").GetSection("PublicKeyXmlString").Value;

            Endpoints.Domain = configuration.GetSection("Endpoints").GetSection("Domain").Value;

            IdentityService.ApiUrl = configuration.GetSection("IdentityService").GetSection("ApiUrl").Value;
            IdentityService.ApiKeyName = configuration.GetSection("IdentityService").GetSection("ApiKeyName").Value;
            IdentityService.ApiKeyValue = configuration.GetSection("IdentityService").GetSection("ApiKeyValue").Value;

            #region Hosting configuration details (if available)

            try
            {
                // Azure WebApp provides these settings when deployed.
                Hosting.SiteName = configuration["WEBSITE_SITE_NAME"];
                Hosting.InstanceId = configuration["WEBSITE_INSTANCE_ID"];
            }
            catch
            {
            }


            #endregion


        }

        public ApplicationDetails Application { get; set; }
        public HostingConfiguration Hosting { get; set; }
        public JWTConfiguration JSONWebTokens { get; set; }
        public Cookies Cookies { get; set; }
        public EndpointSettings Endpoints { get; set; }
        public IdentityService IdentityService { get; set; }
    }
}
