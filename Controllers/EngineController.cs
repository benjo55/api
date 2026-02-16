using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EngineController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<EngineController> _logger;

        public EngineController(ISchedulerFactory schedulerFactory, ILogger<EngineController> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Lance un job Quartz à la demande (par son nom : UpdateValuations, ComputePru, etc.)
        /// </summary>
        [HttpPost("run-job/{jobName}")]
        public async Task<IActionResult> RunJob(string jobName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName);

            if (!await scheduler.CheckExists(jobKey))
            {
                _logger.LogWarning("❌ Job {jobName} introuvable dans Quartz", jobName);
                return NotFound(new { message = $"Job {jobName} introuvable" });
            }

            await scheduler.TriggerJob(jobKey);
            _logger.LogInformation("▶️ Job {jobName} déclenché manuellement", jobName);

            return Ok(new { message = $"Job {jobName} déclenché manuellement" });
        }

        /// <summary>
        /// Retourne la liste des jobs Quartz enregistrés (nom, groupe, description éventuelle).
        /// </summary>
        [HttpGet("list-jobs")]
        public async Task<IActionResult> ListJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobGroups = await scheduler.GetJobGroupNames();

            var jobs = new List<object>();

            foreach (var group in jobGroups)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));
                foreach (var jobKey in jobKeys)
                {
                    var detail = await scheduler.GetJobDetail(jobKey);
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);

                    jobs.Add(new
                    {
                        Name = jobKey.Name,
                        Group = jobKey.Group,
                        Description = detail?.Description ?? "",
                        Triggers = triggers.Select(t => new
                        {
                            Type = t.GetType().Name,
                            NextFireTimeUtc = t.GetNextFireTimeUtc()?.ToLocalTime().ToString("u"),
                            PreviousFireTimeUtc = t.GetPreviousFireTimeUtc()?.ToLocalTime().ToString("u")
                        })
                    });
                }
            }

            return Ok(jobs);
        }
    }
}
