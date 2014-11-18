using System.ServiceProcess;

namespace PushSharp.WinService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new PushNotificationService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
