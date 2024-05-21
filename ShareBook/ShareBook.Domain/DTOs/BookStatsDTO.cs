namespace ShareBook.Domain.DTOs
{
    public class BookStatsDto
    {
        public int TotalWaitingApproval { get; set; }
        public int TotalLate { get; set; }
        public int TotalOk { get; set; }
    }
}