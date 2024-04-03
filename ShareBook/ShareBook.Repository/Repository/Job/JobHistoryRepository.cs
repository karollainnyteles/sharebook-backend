using ShareBook.Domain;

namespace ShareBook.Repository
{
    public class JobHistoryRepository : RepositoryGeneric<JobHistory>, IJobHistoryRepository
    {
        public JobHistoryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}