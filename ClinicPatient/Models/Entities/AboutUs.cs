using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicPatient.Models.Entities
{
    public class AboutUs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
