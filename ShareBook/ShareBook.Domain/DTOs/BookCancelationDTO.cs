namespace ShareBook.Domain.DTOs
{
    public class BookCancelationDto
    {
        public Book Book { get; set; }
        public string CanceledBy { get; set; }
        public string Reason { get; set; }
    }
}