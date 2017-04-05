using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace TfsFindBuildAssemblies
{
    class Options
    {
        [OptionArray('c', "collections", Required = true, HelpText = "Team Collections Uris.")]
        public string[] TeamCollections { get; set; }

        [Option('a', "assemblies", Required = true, HelpText = "Assemblies location.")]
        public string AssembliesLocation { get; set; }

        [OptionArray('b', "build", Required = true, HelpText = "Build definition names (wildcards accepted). E.g.: *.Main")]
        public string[] BuildName { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output file. E.g.: D:\\TEMP\\out.csv")]
        public string OutFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsFindBuildAssemblies", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: As is");
            help.AddPreOptionsLine("Usage: TfsFindBuildAssemblies.exe -c http://logpmtfs01v:8080/tfs/Logitravel http://logpmtfs01v:8080/tfs/Sales -a \\\\acmecatfs01v\\SharedTFS\\Assemblies -b *.Main -o D:\\TEMP\\out.csv");
            help.AddOptions(this);
            return help;
        }
    }
}
