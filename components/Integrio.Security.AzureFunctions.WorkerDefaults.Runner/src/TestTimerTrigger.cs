using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Integrio.Security.AzureFunctions.WorkerDefaults.Runner;

public class TestTimerTrigger
{
    private readonly ILogger<TestTimerTrigger> _logger;

    public TestTimerTrigger(ILogger<TestTimerTrigger> logger)
    {
        _logger = logger;
    }

    [Function("TestTimerTrigger")]
    public void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            
        }
    }
}