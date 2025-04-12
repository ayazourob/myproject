using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get counts for dashboard
            var doctors = await _unitOfWork.Doctors.GetAllAsync();
            var patients = await _unitOfWork.Patients.GetAllAsync();
            var appointments = await _unitOfWork.Appointments.GetAllAsync();
            var pendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Pending);
            var messages = await _unitOfWork.ContactUsMessages.GetAllAsync();
            var unreadMessages = messages.Count(m => !m.IsRead);

            ViewBag.DoctorsCount = doctors.Count();
            ViewBag.PatientsCount = patients.Count();
            ViewBag.AppointmentsCount = appointments.Count();
            ViewBag.PendingAppointmentsCount = pendingAppointments;
            ViewBag.MessagesCount = messages.Count();
            ViewBag.UnreadMessagesCount = unreadMessages;

            // Get recent appointments
            var recentAppointments = appointments
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient?.User?.FullName ?? "Unknown",
                    DoctorId = a.DoctorId,
                    DoctorName = a.Doctor?.FullName ?? "Unknown",
                    DoctorSpecialization = a.Doctor?.Specialization ?? "Unknown",
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            return View(recentAppointments);
        }

        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            var messages = await _unitOfWork.ContactUsMessages.GetAllAsync();

            var viewModels = messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new ContactUsMessageViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Email = m.Email,
                    Message = m.Message,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead
                })
                .ToList();

            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> MessageDetails(int id)
        {
            var message = await _unitOfWork.ContactUsMessages.GetByIdAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            // Mark message as read
            if (!message.IsRead)
            {
                message.IsRead = true;
                _unitOfWork.ContactUsMessages.Update(message);
                await _unitOfWork.CompleteAsync();
            }

            var viewModel = new ContactUsMessageViewModel
            {
                Id = message.Id,
                Name = message.Name,
                Email = message.Email,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _unitOfWork.ContactUsMessages.GetByIdAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            _unitOfWork.ContactUsMessages.Remove(message);
            await _unitOfWork.CompleteAsync();

            TempData["SuccessMessage"] = "Message deleted successfully.";
            return RedirectToAction(nameof(Messages));
        }

        [HttpGet]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AboutUs(AboutUsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var aboutUs = await _unitOfWork.AboutUs.GetByIdAsync(model.Id);

                if (aboutUs == null)
                {
                    return NotFound();
                }

                aboutUs.Title = model.Title;
                aboutUs.Content = model.Content;
                aboutUs.LastUpdated = System.DateTime.Now;

                _unitOfWork.AboutUs.Update(aboutUs);
                await _unitOfWork.CompleteAsync();

                TempData["SuccessMessage"] = "About Us page updated successfully.";
                return RedirectToAction(nameof(AboutUs));
            }

            return View(model);
        }
    }
}
