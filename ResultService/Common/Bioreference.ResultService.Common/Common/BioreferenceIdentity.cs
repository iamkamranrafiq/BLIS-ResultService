using Bioreference.Security;
using System;
using System.Collections.Generic;

using System.Security.Principal;
namespace Bioreference.ResultService.Common
{
    public class BioreferenceIdentity : IIdentity, IBioreferenceIdentity
    {
        public BioreferenceIdentity(string name, string authenticationType = "Custom")
        {
            Name = name;
            AuthenticationType = authenticationType;
            IsAuthenticated = !string.IsNullOrEmpty(name);
            RoleKeys = Array.Empty<string>();
            CurrentSessionId = Guid.NewGuid();
        }

        public string Name { get; private set; }

        public string AuthenticationType { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public Guid CurrentSessionId { get; private set; }

        public string[] RoleKeys { get; private set; }

        public bool IsInRole(string role)
        {
            return RoleKeys != null && Array.Exists(RoleKeys, r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        public void SetSessionId(Guid sessionId)
        {
            CurrentSessionId = sessionId;
        }

        public void SetRoles(string[] roles)
        {
            RoleKeys = roles ?? Array.Empty<string>();
        }
    }
}
