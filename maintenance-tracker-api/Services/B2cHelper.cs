using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace maintenance_tracker_api.Services
{
    public interface IB2cHelper
    {
        Guid GetOid(HttpRequest request);
        string GetEmail(HttpRequest request);
        string GetName(HttpRequest request);
    }

    public class B2cHelper : IB2cHelper
    {
        private const string OidClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string EmailClaim = "emails";
        private const string NameClaim = "name";

        public Guid GetOid(HttpRequest request)
        {
            return IsRunningLocally() ? Guid.Empty : Guid.Parse(GetClaim(request, OidClaim));
        }

        public string GetEmail(HttpRequest request)
        {
            return IsRunningLocally() ? "localuser@wrench.cafe" : GetClaim(request, EmailClaim);
        }

        public string GetName(HttpRequest request)
        {
            return IsRunningLocally() ? "Local User" : GetClaim(request, NameClaim);
        }

        private static string GetClaim(HttpRequest request, string claimType)
        {
            var header = request.Headers["x-ms-client-principal"].FirstOrDefault();
            if (string.IsNullOrEmpty(header))
                return string.Empty;

            var bytes = Convert.FromBase64String(header);
            using var document = JsonDocument.Parse(bytes);
            foreach (var claim in document.RootElement.GetProperty("claims").EnumerateArray())
            {
                if (claim.GetProperty("typ").GetString() == claimType)
                    return claim.GetProperty("val").GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        private static bool IsRunningLocally()
        {
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
        }
    }
}
