using System.Threading.Tasks;

namespace ClinicPatient.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
