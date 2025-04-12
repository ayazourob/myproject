using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicPatient.Models.ViewModels
{
    public class AboutUsViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
