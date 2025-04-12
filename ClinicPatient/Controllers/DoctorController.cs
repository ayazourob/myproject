using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using ClinicPatient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    [Authorize]
    public class DoctorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileService _fileService;

        public DoctorController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Doctor)
            {
                return Forbid();
            }

            var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();

            if (doctor == null)
            {
                return NotFound();
            }

            // Get upcoming appointments
            var appointments = await _unitOfWork.Appointments.FindAsync(a =>
                a.DoctorId == doctor.Id &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));

            var appointmentViewModels = appointments.Select(a => new AppointmentViewModel
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.User?.FullName ?? "Unknown",
                DoctorId = a.DoctorId,
                DoctorName = doctor.FullName,
                DoctorSpecialization = doctor.Specialization,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).OrderBy(a => a.AppointmentDate).ToList();

            var viewModel = new DoctorDetailsViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Specialization = doctor.Specialization,
                ExperienceYears = doctor.ExperienceYears,
                AvailableSlots = doctor.AvailableSlots,
                Rating = doctor.Rating,
                ImageUrl = doctor.ImageUrl,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Appointments = appointmentViewModels
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Doctor)
            {
                return Forbid();
            }

            var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();

            if (doctor == null)
            {
                return NotFound();
            }

            var viewModel = new DoctorDetailsViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Specialization = doctor.Specialization,
                ExperienceYears = doctor.ExperienceYears,
                AvailableSlots = doctor.AvailableSlots,
                Rating = doctor.Rating,
                ImageUrl = doctor.ImageUrl,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Doctor)
            {
                return Forbid();
            }

            var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();

            if (doctor == null)
            {
                return NotFound();
            }

            var viewModel = new DoctorEditViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                PhoneNumber = user.PhoneNumber,
                Specialization = doctor.Specialization,
                ExperienceYears = doctor.ExperienceYears,
                AvailableSlots = doctor.AvailableSlots,
                CurrentImageUrl = doctor.ImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DoctorEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Doctor)
            {
                return Forbid();
            }

            var doctor = (await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id)).FirstOrDefault();

            if (doctor == null || doctor.Id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update user information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                await _userManager.UpdateAsync(user);

                // Update doctor information
                doctor.FullName = model.FullName;
                doctor.Specialization = model.Specialization;
                doctor.ExperienceYears = model.ExperienceYears;
                doctor.AvailableSlots = model.AvailableSlots;

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(doctor.ImageUrl))
                    {
                        _fileService.DeleteFile(doctor.ImageUrl);
                    }

                    // Save new image
                    doctor.ImageUrl = await _fileService.SaveFileAsync(model.ImageFile, "doctors");
                }

                _unitOfWork.Doctors.Update(doctor);
                await _unitOfWork.CompleteAsync();

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var doctors = await _unitOfWork.Doctors.GetAllAsync();

            var viewModels = doctors.Select(d => new DoctorDetailsViewModel
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialization = d.Specialization,
                ExperienceYears = d.ExperienceYears,
                Rating = d.Rating,
                ImageUrl = d.ImageUrl,
                Email = d.User?.Email ?? "Unknown",
                PhoneNumber = d.User?.PhoneNumber ?? "Unknown"
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            return View(new DoctorCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(DoctorCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email is already in use
                var existingUser = await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already in use.");
                    return View(model);
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    UserType = UserType.Doctor
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to Doctor role
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    // Create doctor profile
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        FullName = model.FullName,
                        Specialization = model.Specialization,
                        ExperienceYears = model.ExperienceYears,
                        AvailableSlots = model.AvailableSlots
                    };

                    // Handle image upload
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        doctor.ImageUrl = await _fileService.SaveFileAsync(model.ImageFile, "doctors");
                    }

                    await _unitOfWork.Doctors.AddAsync(doctor);
                    await _unitOfWork.CompleteAsync();

                    TempData["SuccessMessage"] = "Doctor created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);

            if (doctor == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(doctor.UserId);

            if (user == null)
            {
                return NotFound();
            }

            var ratings = await _unitOfWork.DoctorRatings.FindAsync(r => r.DoctorId == id);

            var viewModel = new DoctorDetailsViewModel
            {
                Id = doctor.Id,
                FullName = doctor.FullName,
                Specialization = doctor.Specialization,
                ExperienceYears = doctor.ExperienceYears,
                AvailableSlots = doctor.AvailableSlots,
                Rating = doctor.Rating,
                ImageUrl = doctor.ImageUrl,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Ratings = ratings.Select(r => new DoctorRatingViewModel
                {
                    Id = r.Id,
                    DoctorId = r.DoctorId,
                    PatientName = r.Patient?.User?.FullName ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);

            if (doctor == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(doctor.UserId);

            if (user != null)
            {
                // Delete doctor's image if exists
                if (!string.IsNullOrEmpty(doctor.ImageUrl))
                {
                    _fileService.DeleteFile(doctor.ImageUrl);
                }

                // Delete user
                await _userManager.DeleteAsync(user);
            }

            TempData["SuccessMessage"] = "Doctor deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int doctorId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Rating must be between 1 and 5.";
                return RedirectToAction("DoctorDetails", "Home", new { id = doctorId });
            }

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

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId);

            if (doctor == null)
            {
                return NotFound();
            }

            // Check if patient has already rated this doctor
            var existingRating = (await _unitOfWork.DoctorRatings.FindAsync(r =>
                r.DoctorId == doctorId && r.PatientId == patient.Id.ToString())).FirstOrDefault();

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = rating;
                existingRating.Comment = comment;

                _unitOfWork.DoctorRatings.Update(existingRating);
            }
            else
            {
                // Create new rating
                var doctorRating = new DoctorRating
                {
                    DoctorId = doctorId,
                    PatientId = patient.Id.ToString(),
                    Rating = rating,
                    Comment = comment
                };

                await _unitOfWork.DoctorRatings.AddAsync(doctorRating);
            }

            await _unitOfWork.CompleteAsync();

            // Update doctor's average rating
            var allRatings = await _unitOfWork.DoctorRatings.FindAsync(r => r.DoctorId == doctorId);
            doctor.Rating = allRatings.Average(r => r.Rating);

            _unitOfWork.Doctors.Update(doctor);
            await _unitOfWork.CompleteAsync();

            TempData["SuccessMessage"] = "Thank you for rating this doctor.";
            return RedirectToAction("DoctorDetails", "Home", new { id = doctorId });
        }
    }
}
