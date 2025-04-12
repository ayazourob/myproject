using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using ClinicPatient.Models.Entities;

namespace ClinicPatient.Models.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AppointmentCreateViewModel
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }
    }

    public class AppointmentEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public AppointmentStatus Status { get; set; }

        [Display(Name = "Notes")]

        public string Notes { get; set; }
    }
}
