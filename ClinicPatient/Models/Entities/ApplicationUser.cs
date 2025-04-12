using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Numerics;

namespace ClinicPatient.Models.Entities
{
    public enum UserType
    {
        Patient,
        Doctor,
        Admin
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public UserType UserType { get; set; } 

        // Navigation properties
        public virtual Doctor Doctor { get; set; }
        public virtual Patient Patient { get; set; }
    }
}

