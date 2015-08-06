using CommandLine;
using CommandLine.Text;
using TfsBuildDefinitionsCommon;

namespace TfsCreateServiceBuildDefinition
{
    class Options : BuildDefinitionOptions
    {

        [Option('w', "service", Required = true, HelpText = "Windows Service to deploy, in format <ServiceName>;<ServiceAssembly>. E.g.: MyService;MyService.exe")]
        public string ServiceToDeploy { get; set; }

        [Option('h', "servicehost", Required = true, HelpText = "Server hostname. E.g.: \"log-prc01.logitravelprod.local\"")]
        public string ServiceHost { get; set; }

        [Option('l', "servicelocation", Required = true, HelpText = "Remote location into server. This value will be appended to hostname to form a UNC path. E.g.: set to \"EjecutablesNet\\\\back\" and the UNC path will be \"\\\\<server>\\EjecutablesNet\\Back\"")]
        public string ServiceLocation { get; set; }

        [Option('e', "servicelocalpath", Required = true, HelpText = "Local path into server where the services will be installed. It must match the remote UNC location set in RemoteLocation. E.g.: \"D:\\EjecutablesNet\\Back\"")]
        public string ServiceLocalPath { get; set; }

        [Option('u', "serviceuser", Required = false, HelpText = "User name.")]
        public string ServiceUser { get; set; }

        [Option('y', "servicepassword", Required = false, DefaultValue = "", HelpText = "Password.")]
        public string ServicePassword { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsCreateServiceBuildDefinition", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory - Logitravel - 20 cool pillows :-)", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: free, as in free beer.");
            help.AddPreOptionsLine("Usage: TfsCreateServiceBuildDefinition.exe [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
