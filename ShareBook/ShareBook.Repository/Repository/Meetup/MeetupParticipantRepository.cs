using ShareBook.Domain;

namespace ShareBook.Repository
{
    public class MeetupParticipantRepository : RepositoryGeneric<MeetupParticipant>, IMeetupParticipantRepository
    {
        public MeetupParticipantRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}