using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace TfsBuildRelationships
{
    class Options
    {
        [OptionArray('c', "collections", Required = true, HelpText = "Team Collections Uris.")]
        public string[] TeamCollections { get; set; }

        [Option('o', "out", Required = false, HelpText = "Output file.")]
        public string OutFile { get; set; }

        [Option('v', "verbose", Required = false, DefaultValue = false, HelpText = "Verbose output.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsBuildRelationships", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: As is");
            help.AddPreOptionsLine("Usage: TfsBuildRelationships.exe -c http://logpmtfs01v:8080/tfs/Logitravel http://logpmtfs01v:8080/tfs/Sales [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
