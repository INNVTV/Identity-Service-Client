using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServiceClient.Common.Settings
{
    public interface IApplicationSettings
    {
        ApplicationDetails Application { get; set; }
        JWTConfiguration JSONWebTokens { get; set; }
        Cookies Cookies { get; set; }

        EndpointSettings Endpoints { get; set; }
        HostingConfiguration Hosting { get; set; }

    }

    #region Classes

    #region Application

    public class ApplicationDetails
    {
        public string Name { get; set; }
    }


    #endregion

    #region Cookies

    public class Cookies
    {
        public int CookieExpirationHours { get; set; }
        public string JwtCookieName { get; set; }
        public string RefreshTokenCookieName { get; set; }

    }


    #endregion

    #region JSONWebTokens

    public class JWTConfiguration
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string RefreshTokenEncryptionPassPhrase { get; set; }
        public string PublicKeyXmlString { get; set; }
        public string PublicKeyPEM { get; set; }
    }


    #endregion

    #region Endpoints

    public class EndpointSettings
    {
        public string Domain { get; set; }
    }

    #endregion

    #region Hosting

    /// <summary>
    /// Only used in Azure WebApp hosted deployments.
    /// Returns info on the WebApp instance for the current process. 
    /// Can be used to log which WebApp instance a process ran on.
    /// </summary>
    public class HostingConfiguration
    {

        public string SiteName { get; set; }
        public string InstanceId { get; set; }
    }

    #endregion

    #endregion
}
