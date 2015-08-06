using CommandLine;

namespace TfsBuildDefinitionsCommon
{
    public class BuildDefinitionOptions : BaseOptions
    {

        [Option('n', "name", Required = true, HelpText = "Name of the project or build (suffix will be appended)")]
        public string BuildName { get; set; }

        [Option('d', "description", Required = false, DefaultValue = "*** Created with Toolfactory TFS tools ***", HelpText = "Build definition description.")]
        public string Description { get; set; }

        [Option('o', "solution", Required = true, HelpText = "Solution Name. E.g.: \"Architecture.MySolution.sln\"")]
        public string SolutionName { get; set; }

        [Option('r', "tfspath", Required = true, HelpText = "Project path in TFS.")]
        public string TfsProjectPath { get; set; }

        [Option('b', "controller", Required = true, HelpText = "Build Controller.")]
        public string BuildController { get; set; }

        [Option('a', "assemblies", Required = false, DefaultValue = "", HelpText = "Binary assemblies to synchronize, separated by ;. E.g.: \"Toolfactory.MyAssembly.dll;Toolfactory.MyLibrary.dll\"")]
        public string BinariesPattern { get; set; }

        [Option('p', "project", Required = true, HelpText = "Team Project.")]
        public string TeamProject { get; set; }
    }
}
