using System;
using System.Security.Claims;

namespace maintenance_tracker_api
{
    static class B2cHelper
    {
        private const string OidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        public static Guid GetOid(ClaimsPrincipal principal)
        {
            return Guid.Parse(principal.FindFirst(OidClaim).Value);
        }
    }
}
