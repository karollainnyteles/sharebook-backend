using System;

namespace ShareBook.Domain.DTOs
{
    public class UserAnonymizeDto
    {
        public Guid UserId { get; set; }
        public string Password { get; set; }
        public string Reason { get; set; }
    }
}