using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicPatient.Models.Entities
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Specialization { get; set; }

        public int ExperienceYears { get; set; }

        public string AvailableSlots { get; set; }

        [Range(0, 5)]
        public double Rating { get; set; }

        public string ImageUrl { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
        public virtual ICollection<DoctorRating> Ratings { get; set; }

        public Doctor()
        {
            Appointments = new HashSet<Appointment>();
            Ratings = new HashSet<DoctorRating>();
        }
    }

    public class DoctorRating
    {
        [Key]
        public int Id { get; set; }

        public int DoctorId { get; set; }
        public virtual Doctor Doctor { get; set; }

        public string PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

