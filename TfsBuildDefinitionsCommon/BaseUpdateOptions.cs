using CommandLine;

namespace TfsBuildDefinitionsCommon
{
    public class BaseUpdateOptions
    {
        [Option('c', "collection", Required = true, HelpText = "Team Collection Uri.")]
        public string TeamCollection { get; set; }

        [Option('x', "tplproject", Required = false, DefaultValue = Constants.DefaultTemplatesTeamProject, HelpText = "Templates Team Project. E.g.: \"Arquitectura\"")]
        public string TemplatesTeamProject { get; set; }

        [Option('s', "sharedtfs", Required = false, DefaultValue = Constants.DefaultSharedTfsLocation, HelpText = "Shared location root, such as \\\\LLOSETA\\SharedTFS")]
        public string SharedTfsLocation { get; set; }

        [Option('v', null, HelpText = "Print verbose details during execution.")]
        public bool Verbose { get; set; }

        [Option('t', "save", HelpText = "Save parameters of the build definition. You MUST set this value for the changes to happen, else the execution will be a simulation.")]
        public bool Save { get; set; }
    }
}
