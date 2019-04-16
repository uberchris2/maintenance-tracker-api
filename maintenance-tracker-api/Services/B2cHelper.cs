using System;
using System.Security.Claims;

namespace maintenance_tracker_api.Services
{
    public interface IB2cHelper
    {
        Guid GetOid(ClaimsPrincipal principal);
        string GetEmail(ClaimsPrincipal principal);
        string GetName(ClaimsPrincipal principal);
    }

    public class B2cHelper : IB2cHelper
    {
        private const string OidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string EmailClaim = "emails";
        private const string NameClaim = "name";

        public Guid GetOid(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? Guid.Empty : Guid.Parse(principal.FindFirst(OidClaim).Value);
        }

        public string GetEmail(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? "localuser@wrench.cafe" : principal.FindFirst(EmailClaim).Value;
        }

        public string GetName(ClaimsPrincipal principal)
        {
            return IsRunningLocally() ? "Local User" : principal.FindFirst(NameClaim).Value;
        }

        private bool IsRunningLocally()
        {
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
        }
    }
}
