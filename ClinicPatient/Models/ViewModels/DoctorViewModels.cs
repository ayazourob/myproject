using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ClinicPatient.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace ClinicPatient.Models.ViewModels
{
    public class DoctorDetailsViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Specialization { get; set; }
        public int ExperienceYears { get; set; }
        public string AvailableSlots { get; set; }
        public double Rating { get; set; }
        public string ImageUrl { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public List<DoctorRatingViewModel> Ratings { get; set; }
        public List<AppointmentViewModel> Appointments { get; set; }
    }

    public class DoctorCreateViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Specialization")]
        public string Specialization { get; set; }

        [Required]
        [Display(Name = "Experience Years")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Available Slots")]
        public string AvailableSlots { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile ImageFile { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class DoctorEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Specialization")]
        public string Specialization { get; set; }

        [Required]
        [Display(Name = "Experience Years")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Available Slots")]
        public string AvailableSlots { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile ImageFile { get; set; }

        public string CurrentImageUrl { get; set; }
    }

    public class DoctorRatingViewModel
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
