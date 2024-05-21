using ShareBook.Domain;
using ShareBook.Domain.DTOs;
using System.Threading.Tasks;

namespace ShareBook.Service
{
    public interface IUserEmailService
    {
        Task SendEmailForgotMyPasswordToUserAsync(User user);

        void SendEmailRequestParentAproval(RegisterUserDto userDto, User user);

        void SendEmailParentAprovedNotifyUser(User user);

        void SendEmailAnonymizeNotifyAdms(UserAnonymizeDto dto);
    }
}