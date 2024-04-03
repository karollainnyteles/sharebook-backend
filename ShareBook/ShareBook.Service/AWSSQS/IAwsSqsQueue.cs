using ShareBook.Service.AwsSqs.Dto;
using System.Threading.Tasks;

namespace ShareBook.Service.AwsSqs
{
    public interface IAwsSqsQueue<T>
    {
        Task SendMessage(T message);

        Task<SharebookMessage<T>> GetMessage();

        Task DeleteMessage(string receiptHandle);
    }
}