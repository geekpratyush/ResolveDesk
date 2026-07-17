using Microsoft.AspNetCore.Identity;

namespace ResolveDesk.Services.Identity.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Provider { get; set; } = "Local"; // Local, Google, GitHub
    }
}
