using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using ClinicPatient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AppointmentController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var appointments = Enumerable.Empty<Appointment>();

            if (user.UserType == UserType.Patient)
            {
                var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();

                if (patient != null)
                {
                    appointments = await _unitOfWork.Appointments.FindAsync(a => a.PatientId == patient.Id);
                }
            }
            else if (user.UserType == UserType.Doctor)
            {
                var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();

                if (doctor != null)
                {
                    appointments = await _unitOfWork.Appointments.FindAsync(a => a.DoctorId == doctor.Id);
                }
            }
            else if (user.UserType == UserType.Admin)
            {
                appointments = await _unitOfWork.Appointments.GetAllAsync();
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
        public async Task<IActionResult> Create(int? doctorId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Patient)
            {
                return Forbid();
            }

            var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();

            if (patient == null)
            {
                return NotFound();
            }

            var doctors = await _unitOfWork.Doctors.GetAllAsync();
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", doctorId);

            var model = new AppointmentCreateViewModel();

            if (doctorId.HasValue)
            {
                model.DoctorId = doctorId.Value;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Patient)
            {
                return Forbid();
            }

            var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();

            if (patient == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if the doctor exists
                var doctor = await _unitOfWork.Doctors.GetByIdAsync(model.DoctorId);

                if (doctor == null)
                {
                    ModelState.AddModelError("DoctorId", "Selected doctor does not exist.");
                    var doctors = await _unitOfWork.Doctors.GetAllAsync();
                    ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", model.DoctorId);
                    return View(model);
                }

                // Check if the appointment time is available
                var existingAppointments = await _unitOfWork.Appointments.FindAsync(a =>
                    a.DoctorId == model.DoctorId &&
                    a.AppointmentDate.Date == model.AppointmentDate.Date &&
                    a.AppointmentDate.Hour == model.AppointmentDate.Hour &&
                    a.Status != AppointmentStatus.Cancelled);

                if (existingAppointments.Any())
                {
                    ModelState.AddModelError("AppointmentDate", "This time slot is already booked. Please select another time.");
                    var doctors = await _unitOfWork.Doctors.GetAllAsync();
                    ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", model.DoctorId);
                    return View(model);
                }

                var appointment = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = model.DoctorId,
                    AppointmentDate = model.AppointmentDate,
                    Status = AppointmentStatus.Pending,
                    Notes = model.Notes,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Appointments.AddAsync(appointment);
                await _unitOfWork.CompleteAsync();

                // Send notification email to doctor
                var doctorUser = await _userManager.FindByIdAsync(doctor.UserId);
                if (doctorUser != null)
                {
                    await _emailService.SendEmailAsync(
                        doctorUser.Email,
                        "New Appointment Request",
                        $"<h1>New Appointment Request</h1><p>Dear Dr. {doctor.FullName},</p><p>You have a new appointment request from {user.FullName} on {model.AppointmentDate.ToString("dddd, MMMM dd, yyyy")} at {model.AppointmentDate.ToString("hh:mm tt")}.</p><p>Please log in to your account to confirm or reschedule.</p>"
                    );
                }

                TempData["SuccessMessage"] = "Appointment created successfully. Waiting for doctor's confirmation.";
                return RedirectToAction(nameof(Index));
            }

            var doctorsList = await _unitOfWork.Doctors.GetAllAsync();
            ViewBag.Doctors = new SelectList(doctorsList, "Id", "FullName", model.DoctorId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to edit this appointment
            bool hasPermission = false;

            if (user.UserType == UserType.Admin)
            {
                hasPermission = true;
            }
            else if (user.UserType == UserType.Doctor)
            {
                var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();
                hasPermission = doctor != null && appointment.DoctorId == doctor.Id;
            }
            else if (user.UserType == UserType.Patient)
            {
                var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();
                hasPermission = patient != null && appointment.PatientId == patient.Id;
            }

            if (!hasPermission)
            {
                return Forbid();
            }

            var model = new AppointmentEditViewModel
            {
                Id = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                Notes = appointment.Notes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to edit this appointment
            bool hasPermission = false;

            if (user.UserType == UserType.Admin)
            {
                hasPermission = true;
            }
            else if (user.UserType == UserType.Doctor)
            {
                var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();
                hasPermission = doctor != null && appointment.DoctorId == doctor.Id;
            }
            else if (user.UserType == UserType.Patient)
            {
                var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();
                hasPermission = patient != null && appointment.PatientId == patient.Id;

                // Patients can only cancel appointments, not change status
                if (model.Status != AppointmentStatus.Cancelled && appointment.Status != model.Status)
                {
                    hasPermission = false;
                }
            }

            if (!hasPermission)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Check if the appointment time is available (if date is changed)
                if (appointment.AppointmentDate != model.AppointmentDate)
                {
                    var existingAppointments = await _unitOfWork.Appointments.FindAsync(a =>
                        a.DoctorId == appointment.DoctorId &&
                        a.AppointmentDate.Date == model.AppointmentDate.Date &&
                        a.AppointmentDate.Hour == model.AppointmentDate.Hour &&
                        a.Status != AppointmentStatus.Cancelled);

                    if (existingAppointments.Any())
                    {
                        ModelState.AddModelError("AppointmentDate", "This time slot is already booked. Please select another time.");
                        return View(model);
                    }
                }

                appointment.AppointmentDate = model.AppointmentDate;
                appointment.Status = model.Status;
                appointment.Notes = model.Notes;

                _unitOfWork.Appointments.Update(appointment);
                await _unitOfWork.CompleteAsync();

                // Send email to doctor or patient based on who made the change
                if (user.UserType == UserType.Patient)
                {
                    var doctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId);
                    var patient = await _unitOfWork.Patients.GetByIdAsync(appointment.PatientId);

                    if (doctor != null && patient != null)
                    {
                        await _emailService.SendEmailAsync(
                            doctor.User.Email,
                            "Appointment Updated",
                            $"<h1>Appointment Updated</h1><p>Dear Dr. {doctor.FullName},</p><p>Patient {patient.User.FullName} has updated their appointment on {appointment.AppointmentDate.ToString("dddd, MMMM dd, yyyy")} at {appointment.AppointmentDate.ToString("hh:mm tt")}.</p>"
                        );
                    }
                }

                TempData["SuccessMessage"] = "Appointment updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to cancel this appointment
            bool hasPermission = false;

            if (user.UserType == UserType.Admin)
            {
                hasPermission = true;
            }
            else if (user.UserType == UserType.Doctor)
            {
                var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();
                hasPermission = doctor != null && appointment.DoctorId == doctor.Id;
            }
            /*se if (user.UserType == UserType.Patient)
            {
                var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();
                hasPermission = patient != null && appointment.PatientId == patient.Id;
            }*/

            if (!hasPermission)
            {
                return Forbid();
            }

            appointment.Status = AppointmentStatus.Cancelled;

            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.CompleteAsync();

            // Send notification email
            var patient = await _unitOfWork.Patients.GetByIdAsync(appointment.PatientId);
            var appointmentDoctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId);

            if (user.UserType == UserType.Patient && appointmentDoctor != null && appointmentDoctor.User != null)
            {
                await _emailService.SendEmailAsync(
                    appointmentDoctor.User.Email,
                    "Appointment Cancelled",
                    $"<h1>Appointment Cancelled</h1><p>Dear Dr. {appointmentDoctor.FullName},</p><p>The appointment with {patient?.User?.FullName} on {appointment.AppointmentDate.ToString("dddd, MMMM dd, yyyy")} at {appointment.AppointmentDate.ToString("hh:mm tt")} has been cancelled by the patient.</p>"
                );
            }
            else if (user.UserType == UserType.Doctor && patient != null && patient.User != null)
            {
                await _emailService.SendEmailAsync(
                    patient.User.Email,
                    "Appointment Cancelled",
                    $"<h1>Appointment Cancelled</h1><p>Dear {patient.User.FullName},</p><p>Your appointment with Dr. {appointmentDoctor?.FullName} on {appointment.AppointmentDate.ToString("dddd, MMMM dd, yyyy")} at {appointment.AppointmentDate.ToString("hh:mm tt")} has been cancelled by the doctor.</p>"
                );
            }

            TempData["SuccessMessage"] = "Appointment cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
