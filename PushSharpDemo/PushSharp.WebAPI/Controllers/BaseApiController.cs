using PushSharp.CoreProcessor;
using PushSharp.DataAccessLayer;
using System.Web.Http;

namespace PushSharp.WebAPI.Controllers
{
    public class BaseApiController : ApiController
    {
        public PushNotificationProcessor Processor { get; set; }
        public PushSharpDatabaseContext Context { get; set; } 

        public BaseApiController()
        {
            this.Processor = new PushNotificationProcessor();
        }
    }
}
