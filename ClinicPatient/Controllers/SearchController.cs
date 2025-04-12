using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    public class SearchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public SearchController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Doctors(string specialization = null, string name = null)
        {
            var doctors = await _unitOfWork.Doctors.GetAllAsync();

            if (!string.IsNullOrEmpty(specialization))
            {
                doctors = doctors.Where(d => d.Specialization.Contains(specialization, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(name))
            {
                doctors = doctors.Where(d => d.FullName.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            var viewModels = doctors.Select(d => new DoctorDetailsViewModel
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialization = d.Specialization,
                ExperienceYears = d.ExperienceYears,
                Rating = d.Rating,
                ImageUrl = d.ImageUrl
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> Appointments(DateTime? date = null, string status = null)
        {
            // This endpoint should be accessible only to authenticated users
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/Account/Login");
            }

            var appointments = Enumerable.Empty<Appointment>();

            // Check user role and filter appointments accordingly
            if (User.IsInRole("Admin"))
            {
                appointments = await _unitOfWork.Appointments.GetAllAsync();
            }
            else if (User.IsInRole("Doctor"))
            {
                var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.User.UserName == User.Identity.Name)).FirstOrDefault();

                if (doctor != null)
                {
                    appointments = await _unitOfWork.Appointments.FindAsync(a => a.DoctorId == doctor.Id);
                }
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = (await _unitOfWork.Patients.FindAsync(p => p.User.UserName == User.Identity.Name)).FirstOrDefault();

                if (patient != null)
                {
                    appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == patient.Id);
                }
            }

            // Apply filters
            if (date.HasValue)
            {
                appointments = appointments.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
            {
                appointments = appointments.Where(a => a.Status == statusEnum);
            }

            var viewModels = appointments.Select(a => new AppointmentViewModel
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.User?.FullName ?? "Unknown",
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor?.FullName ?? "Unknown",
                DoctorSpecialization = a.Doctor?.Specialization ?? "Unknown",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public async Task<JsonResult> GetDoctorsBySpecialization(string specialization)
        {
            var doctors = await _unitOfWork.Doctors.GetAllAsync();

            if (!string.IsNullOrEmpty(specialization))
            {
                doctors = doctors.Where(d => d.Specialization.Contains(specialization, StringComparison.OrdinalIgnoreCase));
            }

            var result = doctors.Select(d => new
            {
                id = d.Id,
                name = d.FullName,
                specialization = d.Specialization,
                rating = d.Rating
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableSlots(int doctorId, DateTime date)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId);

            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor not found" });
            }

            // Get all appointments for the doctor on the specified date
            var appointments = await _unitOfWork.Appointments.FindAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == date.Date &&
                a.Status != AppointmentStatus.Cancelled);

            // Parse available slots from doctor's profile
            var availableSlots = doctor.AvailableSlots?.Split(',')
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            // Filter out already booked slots
            var bookedTimes = appointments.Select(a => a.AppointmentDate.ToString("HH:mm")).ToList();
            var availableTimes = availableSlots.Where(s => !bookedTimes.Contains(s)).ToList();

            return Json(new { success = true, slots = availableTimes });
        }
    }
}
