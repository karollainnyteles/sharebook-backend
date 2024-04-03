namespace ShareBook.Service.Notification
{
    public class PushNotificationSettings
    {
        public bool IsActive { get; set; }
        public string AppId { get; set; }
        public string ApiKey { get; set; }
    }
}