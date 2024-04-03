using ShareBook.Domain;
using System.Threading.Tasks;

namespace ShareBook.Service
{
    public interface IBooksEmailService
    {
        Task SendEmailNewBookInserted(Book book);

        Task SendEmailBookApproved(Book book);

        void SendEmailBookReceived(Book book);
    }
}