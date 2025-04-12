using ClinicPatient.Data.Interfaces;
using ClinicPatient.Models.Entities;
using System.Threading.Tasks;

namespace ClinicPatient.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<Doctor> Doctors { get; private set; }
        public IRepository<Patient> Patients { get; private set; }
        public IRepository<Appointment> Appointments { get; private set; }
        public IRepository<ContactUsMessage> ContactUsMessages { get; private set; }
        public IRepository<DoctorRating> DoctorRatings { get; private set; }
        public IRepository<AboutUs> AboutUs { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Doctors = new Repository<Doctor>(context);
            Patients = new Repository<Patient>(context);
            Appointments = new Repository<Appointment>(context);
            ContactUsMessages = new Repository<ContactUsMessage>(context);
            DoctorRatings = new Repository<DoctorRating>(context);
            AboutUs = new Repository<AboutUs>(context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
