using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace TargetFramework
{
    class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input file or folder")]
        public string InputFile {get; set;}

        [Option('o', "output", Required = false, HelpText = "Output file")]
        public string OutputFile { get; set; }

        [Option('v', "verbose", Required = false, DefaultValue = false, HelpText = "Verbose output.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TargetFramework", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: As is");
            help.AddPreOptionsLine("Usage: TargetFramework.exe --input <File|Dir> [--output <outputfile>] [--verbose]");
            help.AddPreOptionsLine(@"Example: TargetFramework.exe -i Toolfactory.Mail.dll -o Assemblies.txt --verbose");
            help.AddOptions(this);
            return help;
        }
    }
}
