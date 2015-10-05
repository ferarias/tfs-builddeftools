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

        [OptionArray('p', "projects", Required = false, DefaultValue = new string[] { }, HelpText = "Team projects. If not set, builds from all projects will be chosen.")]
        public string[] TeamProjects { get; set; }

        [OptionArray('x', "excludebuilds", Required = false, DefaultValue = new string[] { }, HelpText = "A list of build definitions to be excluded from analysis.")]
        public string[] ExcludedBuildDefinitions { get; set; }

        [Option('m', "mode", Required = false, DefaultValue = "project", HelpText = "project|solution")]
        public string Mode { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output name for log and Graphviz DOT file.")]
        public string OutFile { get; set; }

        [Option('r', "graphreduction", Required = false, HelpText = "Enable transitive reduction on generated dependencies graph.")]
        public bool TransitiveReduction { get; set; }

        [Option('g', "graphextracommands", Required = false, HelpText = "Graph extra commands to be included in DOT file.")]
        public string GraphExtracommands { get; set; }

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
            help.AddPreOptionsLine("Example: TfsBuildRelationships.exe -c http://logpmtfs01v:8080/tfs/Logitravel -o Logitravel.dot -x Toolfactory.Framework.Main DeviceDetection.Main Toolfactory.Web.Main CommonCommon.Main Common.Main -g \"size=\"\"8.267,11.692\"\"; ratio=auto;rankdir=LR;node[fontsize=24];\"");
            help.AddOptions(this);
            return help;
        }
    }
}
