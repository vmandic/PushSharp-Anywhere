using PushSharp.CoreProcessor;
using PushSharp.DataAccessLayer;
using System.Web.Http;

namespace PushSharp.WebAPI.Controllers
{
    public class BaseApiController : ApiController
    {
        protected readonly string[] _validDevices = new[] { "ios", "android", "wp8" };

        public PushNotificationProcessor Processor { get; set; }
        public PushSharpDatabaseContext Context { get; set; }

        public BaseApiController()
        {
            this.Processor = new PushNotificationProcessor();
            this.Context = new PushSharpDatabaseContext();
        }
    }
}
