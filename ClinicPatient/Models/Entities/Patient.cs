using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicPatient.Models.Entities
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string MedicalHistory { get; set; }

        public string ImageUrl { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }

        public Patient()
        {
            Appointments = new HashSet<Appointment>();
        }
    }
}
