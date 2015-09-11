using System;
using CommandLine;
using CommandLine.Text;

namespace TfsViewBuildDefinitions
{
    class Options
    {
        [Option('c', "collection", Required = true, HelpText = "Team Collection Uri.")]
        public String TeamCollection { get; set; }

        [Option('p', "project", Required = false, HelpText = "Team Project.")]
        public string TeamProject { get; set; }

        [Option('b', "build", Required = false, HelpText = "Build definition name (wildcards accepted).")]
        public string BuildName { get; set; }

        [Option('t', "printtemplate", HelpText = "Print Template")]
        public bool PrintTemplate { get; set; }
        [Option('g', "printtrigger", HelpText = "Print Trigger")]
        public bool PrintTrigger { get; set; }
        [Option('l', "printdroplocation", HelpText = "Print Drop Location")]
        public bool PrintDropLocation { get; set; }

        [Option('x', "printparams", HelpText = "Print Parameters")]
        public bool PrintParameters { get; set; }
        [Option('y', "printpolicies", HelpText = "Print Policies")]
        public bool PrintPolicies { get; set; }
        [Option('z', "printbuilds", HelpText = "Print Build count")]
        public bool PrintBuilds { get; set; }


        [Option('m', "params", Required = false, HelpText = "Comma-separated parameters to show.")]
        public string Parameters { get; set; }
        [Option('f', "filter", Required = false, HelpText = "Filter parameters by value. Set as comma-separated key=value. E.g.: IsRelease=true")]
        public string Filters { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsViewBuildOptions", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: As is");
            help.AddPreOptionsLine("Usage: TfsViewBuildOptions.exe -c http://logpmtfs01v:8080/tfs/Logitravel [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
