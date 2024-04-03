using ShareBook.Domain.Common;

namespace ShareBook.Domain
{
    public class MeetupParticipant : BaseEntity
    {
        public virtual Meetup Meetup { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}