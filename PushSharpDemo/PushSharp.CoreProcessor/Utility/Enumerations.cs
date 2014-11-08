namespace PushSharp.CoreProcessor.Utility
{
    internal enum PushNotificationStatus
    {
        Unprocessed = 0,
        Processing = 100,
        Processed = 200,
        Undefined = 400,
        Error = 500
    }
}
