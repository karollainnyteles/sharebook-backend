namespace ShareBook.Service.AwsSqs.Dto
{
    public class SharebookMessage<T>
    {
        public string ReceiptHandle { get; set; }
        public T Body { get; set; }
    }
}