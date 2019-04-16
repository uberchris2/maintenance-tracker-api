using System;
using System.Security.Claims;

namespace maintenance_tracker_api
{
    static class B2cHelper
    {
        private const string OidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string EmailClaim = "emails";
        private const string NameClaim = "name";

        public static Guid GetOid(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? Guid.Empty : Guid.Parse(principal.FindFirst(OidClaim).Value);
        }

        public static string GetEmail(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? "localuser@wrench.cafe" : principal.FindFirst(EmailClaim).Value;
        }

        public static string GetName(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? "Local User" : principal.FindFirst(NameClaim).Value;
        }

        private static bool IsRunningLocally()
        {
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
        }
    }
}
