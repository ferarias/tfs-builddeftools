namespace TfsBuildDefinitionsCommon
{
    public class Constants
    {
        

        public const string DefaultTfsUri = "http://logpmtfs01v:8080/tfs/{0}";
        public const string DefaultTemplatesTeamProject = "Arquitectura";
        public const string DefaultNewStandardTemplateName = "Toolfactory-Standard-MAD.12.xaml";
        public const string DefaultStandardTemplateName = "Toolfactory-Standard.12.xaml";
        public const string DefaultServicesTemplateName = "Toolfactory-WindowsService.12.xaml";
        public const string DefaultTopShelfServicesTemplateName = "Toolfactory-TopShelf.12.xaml";
        public const string DefaultNoCompileFullTemplateName = "Toolfactory-NoCompile.Full.12.xaml";
        public const string DefaultSharedTfsLocation = @"\\toolfactory.net\tfs\";
        public const string DefaultPackagesDropLocation = "\\\\logvidpl01v.logitravelprod.local\\dp";

        public const string AssembliesFolder = "Assemblies";
        public const string SymbolsFolder = "Symbols";
        public const string MainFolder = "Main";
        public const string DevFolder = "Dev";
        public const string DropFolder = "Drop";
        public const string AnyCpuDebug = "Any CPU|Debug";
        public const string AnyCpuRelease = "Any CPU|Release";
        public const string DevBuildsNameExtension = ".Dev";
        public const string MainBuildsNameExtension = ".Main";
        public const string NewMainBuildsNameExtension = "New.Main";
        public const string ReleaseBuildsNameExtension = ".Release";
        public const string ServiceBuildsNameExtension = ".Service";
        public const string TopShelfServiceBuildsNameExtension = ".TopShelf";
        public const string CdnBuildsNamePrefix = "cdn";
        public const string ResourcesDevBuildsNameExtension = ".Resources.Dev";
        public const string ResourcesMainBuildsNameExtension = ".Resources.Main";
        public const string ResourcesReleaseBuildsNameExtension = ".Resources.Release";
        public const string ReleaseFolder = "Release";

        public const string ProjectsToBuildParamName = "ProjectsToBuild";
    }
}
