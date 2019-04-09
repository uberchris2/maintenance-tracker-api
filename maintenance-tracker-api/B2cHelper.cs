using System;
using System.Security.Claims;

namespace maintenance_tracker_api
{
    static class B2cHelper
    {
        private const string OidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        public static Guid GetOid(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? Guid.Empty : Guid.Parse(principal.FindFirst(OidClaim).Value);
        }

        private static bool IsRunningLocally()
        {
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
        }
    }
}
