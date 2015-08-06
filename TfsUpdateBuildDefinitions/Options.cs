using CommandLine;
using CommandLine.Text;
using TfsBuildDefinitionsCommon;

namespace TfsUpdateBuildDefinitions
{
    class Options : BaseOptions
    {
        [Option('p', "project", Required = false, HelpText = "Team Project.")]
        public string TeamProject { get; set; }

        [Option('m', "parallel", HelpText = "Parallel processing.")]
        public bool Parallel { get; set; }

        [Option('d', "datafile", HelpText = "Data file path. Data file is a tab-separated file with format BuildDefinition\tIISApplication\tIISServer")]
        public string DataFile { get; set; }

        [OptionArray('k', "skip", Required = false, HelpText = "List of parameters to skip.")]
        public string[] SkipParameters { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsUpdateBuildDefinitions", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory - Logitravel - 20 cool pillows :-)", 2014),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: Free, as in free beer.");
            help.AddPreOptionsLine("Usage: TfsUpdateBuildDefinitions.exe [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
