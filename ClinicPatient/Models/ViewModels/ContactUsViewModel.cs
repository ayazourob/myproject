using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicPatient.Models.ViewModels
{
    public class ContactUsViewModel
    {
        [Required]
        [Display(Name = "Your Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Your Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Your Message")]
        public string Message { get; set; }
    }

    public class ContactUsMessageViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
