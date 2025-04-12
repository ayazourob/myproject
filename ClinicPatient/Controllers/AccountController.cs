using ClinicPatient.Data;
using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using ClinicPatient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork,
            IFileService fileService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    // Redirect based on user type
                    if (user.UserType == UserType.Admin)
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (user.UserType == UserType.Doctor)
                    {
                        return RedirectToAction("Dashboard", "Doctor");
                    }
                    else // Patient
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    UserType = model.UserType
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to role based on UserType
                    await _userManager.AddToRoleAsync(user, model.UserType.ToString());

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                    // Create patient or doctor profile
                    if (model.UserType == UserType.Patient)
                    {
                        var patient = new Patient
                        {
                            UserId = user.Id,
                            DateOfBirth = DateTime.Now.AddYears(-20) // Default value, to be updated later
                        };

                        await _unitOfWork.Patients.AddAsync(patient);
                    }
                    else if (model.UserType == UserType.Doctor)
                    {
                        var doctor = new Doctor
                        {
                            UserId = user.Id,
                            FullName = model.FullName
                        };

                        await _unitOfWork.Doctors.AddAsync(doctor);
                    }

                    await _unitOfWork.CompleteAsync();

                    // Send confirmation email
                    await _emailService.SendEmailAsync(
                        model.Email,
                        "Welcome to Clinic Patient",
                        $"<h1>Welcome to Clinic Patient</h1><p>Dear {model.FullName},</p><p>Thank you for registering with us. Your account has been created successfully.</p>"
                    );

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect based on user type
                    if (model.UserType == UserType.Admin)
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (model.UserType == UserType.Doctor)
                    {
                        return RedirectToAction("Dashboard", "Doctor");
                    }
                    else // Patient
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserType == UserType.Patient)
            {
                var patient = await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id);
                var patientEntity = patient.FirstOrDefault();

                if (patientEntity == null)
                {
                    return NotFound();
                }

                var viewModel = new PatientDetailsViewModel
                {
                    Id = patientEntity.Id,
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = patientEntity.DateOfBirth,
                    MedicalHistory = patientEntity.MedicalHistory,
                    ImageUrl = patientEntity.ImageUrl
                };

                return View("PatientProfile", viewModel);
            }
            else if (user.UserType == UserType.Doctor)
            {
                var doctor = await _unitOfWork.Doctors.FindAsync(d => d.UserId == user.Id);
                var doctorEntity = doctor.FirstOrDefault();

                if (doctorEntity == null)
                {
                    return NotFound();
                }

                var viewModel = new DoctorDetailsViewModel
                {
                    Id = doctorEntity.Id,
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Specialization = doctorEntity.Specialization,
                    ExperienceYears = doctorEntity.ExperienceYears,
                    AvailableSlots = doctorEntity.AvailableSlots,
                    Rating = doctorEntity.Rating,
                    ImageUrl = doctorEntity.ImageUrl
                };

                return View("DoctorProfile", viewModel);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
