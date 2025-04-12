using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using ClinicPatient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public HomeController(
            ILogger<HomeController> logger,
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            // Get top rated doctors
            var doctors = await _unitOfWork.Doctors.GetAllAsync();
            var topDoctors = doctors.OrderByDescending(d => d.Rating).Take(4).ToList();

            return View(topDoctors);
        }

        public async Task<IActionResult> AboutUs()
        {
            var aboutUs = (await _unitOfWork.AboutUs.GetAllAsync()).FirstOrDefault();

            if (aboutUs == null)
            {
                aboutUs = new AboutUs
                {
                    Title = "About Clinic Patient",
                    Content = "Welcome to Clinic Patient, your trusted healthcare platform."
                };

                await _unitOfWork.AboutUs.AddAsync(aboutUs);
                await _unitOfWork.CompleteAsync();
            }

            var viewModel = new AboutUsViewModel
            {
                Id = aboutUs.Id,
                Title = aboutUs.Title,
                Content = aboutUs.Content,
                LastUpdated = aboutUs.LastUpdated
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(ContactUsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var message = new ContactUsMessage
                {
                    Name = model.Name,
                    Email = model.Email,
                    Message = model.Message,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.ContactUsMessages.AddAsync(message);
                await _unitOfWork.CompleteAsync();

                // Send notification email to admin
                await _emailService.SendEmailAsync(
                    "admin@clinicpatient.com", // Replace with actual admin email
                    "New Contact Message",
                    $"<h1>New Contact Message</h1><p>From: {model.Name} ({model.Email})</p><p>Message: {model.Message}</p>"
                );

                TempData["SuccessMessage"] = "Your message has been sent successfully. We will get back to you soon.";
                return RedirectToAction(nameof(ContactUs));
            }

            return View(model);
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

            return View(doctors.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> DoctorDetails(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);

            if (doctor == null)
            {
                return NotFound();
            }

            // Get doctor ratings
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
