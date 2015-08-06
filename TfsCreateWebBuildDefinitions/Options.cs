using CommandLine;
using CommandLine.Text;
using TfsBuildDefinitionsCommon;

namespace TfsCreateWebBuildDefinitions
{
    class Options : BuildDefinitionOptions
    {

        [Option('h', "iisserver", Required = true, HelpText = "IIS deployment server hostname for Main (PRE). E.g.: \"log-pre.gst.toolfactory.net\"")]
        public string IisServerPre { get; set; }

        [Option('e', "iissitenamepre", Required = true, HelpText = "IIS site name for Main (PRE). E.g.: \"pre.logitravel.com\"")]
        public string IisSiteNamePre { get; set; }

        [Option('i', "iissitenamepro", Required = true, HelpText = "IIS site name for Release (PRO). E.g.: \"www.logitravel.com\"")]
        public string IisSiteNamePro { get; set; }

        [Option('j', "jsparsingpre", Required = false, DefaultValue = "false", HelpText = "Enable Javascript parsing in Main build")]
        public string JsParsingPre { get; set; }

        [Option('k', "jsparsingpro", Required = false, DefaultValue = "false", HelpText = "Enable Javascript parsing in Release build")]
        public string JsParsingPro { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TfsCreateWebBuildDefinitions", "1.0.0.0"),
                Copyright = new CopyrightInfo("Toolfactory - Logitravel - 20 cool pillows :-)", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("License: free, as in free beer.");
            help.AddPreOptionsLine("Usage: TfsCreateWebBuildDefinitions.exe [options]");
            help.AddOptions(this);
            return help;
        }
    }
}
