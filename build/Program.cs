using System.Linq;
using System.Net;
using static Build.BuildSteps;

namespace Build
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Orchestrator
                .CreateForTarget(args)
                .Then(Clean)
                .Then(LogIntoAzure)
                .Then(RestorePackages)
                .Then(ReplaceTelemetryInstrumentationKey)
                .Then(DotnetPublish)
                .Then(FilterPowershellRuntimes)
                .Then(AddDistLib)
                .Then(AddTemplatesNupkgs)
                .Run();
        }
    }
}
