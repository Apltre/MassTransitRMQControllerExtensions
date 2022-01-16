using Microsoft.Extensions.Logging;
using MTRMQExample.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTRMQExample.Controllers
{
    public class TestController1
    {
        public TestController1(ILogger<TestController> logger)
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