using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace TfsCompareBuildDefinitions
{
    class Options
    {
        [Option('c', "collection", Required = true, HelpText = "Team Collection Uri.")]
        public string TeamCollection { get; set; }

        [Option('b', "build", Required = false, HelpText = "Build definition name (wildcards accepted).")]
        public string BuildName { get; set; }

        [Option('p', "param", Required = false, HelpText = "Which value to compare from process parameters.")]
        public string ParameterToCompare { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Increase verbosity.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsCompareBuildDefinitions", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: As is");
            help.AddPreOptionsLine("Usage: TfsCompareBuildDefinitions.exe -c http://logpmtfs01v:8080/tfs/Logitravel [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
