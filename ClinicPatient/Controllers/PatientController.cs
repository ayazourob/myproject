using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using ClinicPatient.Models.ViewModels;
using ClinicPatient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicPatient.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileService _fileService;

        public PatientController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
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

            var viewModel = new PatientDetailsViewModel
            {
                Id = patient.Id,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                MedicalHistory = patient.MedicalHistory,
                ImageUrl = patient.ImageUrl
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
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

            var viewModel = new PatientEditViewModel
            {
                Id = patient.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                MedicalHistory = patient.MedicalHistory,
                CurrentImageUrl = patient.ImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PatientEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.UserType != UserType.Patient)
            {
                return Forbid();
            }

            var patient = (await _unitOfWork.Patients.FindAsync(p => p.UserId == user.Id)).FirstOrDefault();

            if (patient == null || patient.Id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Update user information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                await _userManager.UpdateAsync(user);

                // Update patient information
                patient.DateOfBirth = model.DateOfBirth;
                patient.MedicalHistory = model.MedicalHistory;

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(patient.ImageUrl))
                    {
                        _fileService.DeleteFile(patient.ImageUrl);
                    }

                    // Save new image
                    patient.ImageUrl = await _fileService.SaveFileAsync(model.ImageFile, "patients");
                }

                _unitOfWork.Patients.Update(patient);
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
            var patients = await _unitOfWork.Patients.GetAllAsync();

            var viewModels = patients.Select(p => new PatientDetailsViewModel
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = p.User?.FullName ?? "Unknown",
                Email = p.User?.Email ?? "Unknown",
                PhoneNumber = p.User?.PhoneNumber ?? "Unknown",
                DateOfBirth = p.DateOfBirth,
                MedicalHistory = p.MedicalHistory,
                ImageUrl = p.ImageUrl
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(patient.UserId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new PatientDetailsViewModel
            {
                Id = patient.Id,
                UserId = patient.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                MedicalHistory = patient.MedicalHistory,
                ImageUrl = patient.ImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(patient.UserId);

            if (user != null)
            {
                // Delete user's image if exists
                if (!string.IsNullOrEmpty(patient.ImageUrl))
                {
                    _fileService.DeleteFile(patient.ImageUrl);
                }

                // Delete user
                await _userManager.DeleteAsync(user);
            }

            TempData["SuccessMessage"] = "Patient deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
