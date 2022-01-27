using Microsoft.Extensions.Logging;
using MTRMQExample.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTRMQExample.Controllers
{
    public class Test1Controller
    {
        public Test1Controller(ILogger<TestController> logger)
        {
            this.Logger = logger;
        }

        public ILogger<TestController> Logger { get; }

        public Task Do(IEnumerable<JsonText> events)
        {
            return Task.CompletedTask;
        }
    }
}