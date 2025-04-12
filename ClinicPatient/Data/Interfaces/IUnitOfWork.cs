using System;
using System.Threading.Tasks;
using ClinicPatient.Models.Entities;

namespace ClinicPatient.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Doctor> Doctors { get; }
        IRepository<Patient> Patients { get; }
        IRepository<Appointment> Appointments { get; }
        IRepository<ContactUsMessage> ContactUsMessages { get; }
        IRepository<DoctorRating> DoctorRatings { get; }
        IRepository<AboutUs> AboutUs { get; }

        Task<int> CompleteAsync();
    }
}
