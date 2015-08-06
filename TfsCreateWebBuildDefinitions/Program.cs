using System;
using System.IO;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using TfsBuildDefinitionsCommon;

namespace TfsCreateWebBuildDefinitions
{
    static class Program
    {

        private static CommonStructure CommonData { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("\tTfsCreateWebBuildDefinitions");
            Console.WriteLine();
            //#if DEBUG // Nos paramos aquí y pedimos si queremos depurar (sólo en DEBUG)
            //            Debugger.Launch();
            //#endif
            // Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    CommonData = new CommonStructure(options);

                    Console.WriteLine("\tTeam Project Collection: {0}", CommonData.TeamProjectCollection);
                    Console.WriteLine("\tTeam Project: {0}", options.TeamProject);
                    Console.WriteLine("\tShared TFS location: {0}", options.SharedTfsLocation);
                    Console.WriteLine("\tTemplates Team Project: {0}", options.TemplatesTeamProject);
                    Console.WriteLine("\tStandard Template: {0}", CommonData.StandardTemplatePath);
                    Console.WriteLine("\tServices Template: {0}", CommonData.ServicesTemplatePath);
                    Console.WriteLine("\tNo-compile Template: {0}", CommonData.NoCompileFullTemplatePath);
                    Console.WriteLine("\tSave build definitions: {0}", options.Save);
                    Console.WriteLine();
                    Console.WriteLine("\tBuild Definition name: {0}", options.BuildName);
                    Console.WriteLine("\tDescription: {0}", options.Description);
                    Console.WriteLine("\tSolution: {0}", options.SolutionName);
                    Console.WriteLine("\tPath: {0}", options.TfsProjectPath);
                    Console.WriteLine("\tBuild Controller: {0}", options.BuildController);
                    Console.WriteLine();
                    Console.WriteLine("\tBinaries pattern: {0}", options.BinariesPattern);
                    Console.WriteLine("\tIIS Server Pre: {0}", options.IisServerPre);
                    Console.WriteLine("\tIIS Sitename Pre: {0}", options.IisSiteNamePre);
                    Console.WriteLine("\tIIS Server Pro: {0}", options.IisSiteNamePro);
                    Console.WriteLine("\tDeployment packages UNC: {0}", CommonData.DeploymentPackagesLocation);
                    Console.WriteLine("\tJS Parsing Pre: {0}", options.JsParsingPre);
                    Console.WriteLine("\tJS Parsing Pro: {0}", options.JsParsingPro);
                    Console.WriteLine("-------------------------------------------------------------------------------");

                    var buildDefMain = CreateDefinition(options, Constants.MainFolder);
                    var buildDefRelease = CreateDefinition(options, Constants.ReleaseFolder);

                    if (options.Save)
                    {
                        buildDefMain.Save();
                        Console.WriteLine("Main build definition was created");
                        buildDefRelease.Save();
                        Console.WriteLine("Release build definition was created");
                    }
                    else
                    {
                        Console.WriteLine(
                            "The simulation was OK. Use --save to actually create the build definition.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\nAn error occured:\n{0}\n", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Couldn't read options!");
                Console.WriteLine();
                Console.WriteLine("Example: TfsCreateWebBuildDefinition.exe --project Hoteles --name Hot.Web --description \"Super build\" --tfspath $/Hoteles/Release/Web/Hoteles.Web\n --solution Toolfactory.Hotels.Web.sln --controller logpmcompile01p\n --assemblies \"Toolfactory.Hotels.BaseTypes.dll;Toolfactory.Hotels.Dao.dll\" --issserver logvidevweb01v.logitravelprod.local --iissitenamepre hotels.dev.logitravel.com --iissitenamepro www.logitravel.com/hoteles\n --test false");

            }

        }


        private static IBuildDefinition CreateDefinition(Options options, string folder)
        {
            CommonData.Collection.EnsureAuthenticated();

            //Create build definition and give it a name and desription
            var buildDef = CommonData.BuildServer.CreateBuildDefinition(options.TeamProject);
            buildDef.Name = Helpers.GetBuildName(options.BuildName, folder);
            buildDef.Description = options.Description;
            buildDef.ContinuousIntegrationType = ContinuousIntegrationType.Individual;

            //Controller and default build process template
            buildDef.BuildController = CommonData.BuildServer.GetBuildController(options.BuildController);

            buildDef.Process = CommonData.StandardTemplate;

            //Drop location & source settings
            var tfsProjectPath = options.TfsProjectPath;
            if (folder.Equals(Constants.MainFolder))
            {
                tfsProjectPath = tfsProjectPath.Replace("/Release/", "/Main/");
                buildDef.Workspace.AddMapping(tfsProjectPath, "$(SourceDir)", WorkspaceMappingType.Map);
                buildDef.DefaultDropLocation = Path.Combine(options.SharedTfsLocation, Constants.DropFolder, folder);
            }
            else if (folder.Equals(Constants.ReleaseFolder))
            {
                tfsProjectPath = tfsProjectPath.Replace("/Main/", "/Release/");
                buildDef.Workspace.AddMapping(tfsProjectPath, "$(SourceDir)", WorkspaceMappingType.Map);
                buildDef.DefaultDropLocation = "#/";
            }

            //Process params
            var process = WorkflowHelpers.DeserializeProcessParameters(buildDef.ProcessParameters);

            //What to build
            process.Add("ProjectsToBuild", new[] { options.SolutionName });
            process.Add("ConfigurationsToBuild", new[] { "Any CPU|Release" });

            process.Add("CreateWorkItem", false);
            process.Add("PerformTestImpactAnalysis", false);
            process.Add("CreateLabel", true);
            process.Add("AssociateChangesetsAndWorkItems", false);
            process.Add("DisableTests", false);
            process.Add("DropBuild", true);

            process.Add("CustomBinariesReferencePath", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, folder));
            process.Add("CustomBinariesDestination", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, folder));
            process.Add("EnsambladosACompartir", options.BinariesPattern);
            process.Add("IsRelease", folder.Equals(Constants.ReleaseFolder));

            // Web-specific builddef parameters
            if (folder.Equals(Constants.MainFolder))
            {
                process.Add("ServidorDespliegue", options.IisServerPre);
                process.Add("AplicacionIIS", options.IisSiteNamePre);
                process.Add("ParseJS", bool.Parse(options.JsParsingPre));
                
            }
            else if (folder.Equals(Constants.ReleaseFolder))
            {
                process.Add("AplicacionIIS", options.IisSiteNamePro);
                process.Add("ParseJS", bool.Parse(options.JsParsingPro));
            }
            process.Add("DeploymentPackagesLocation", CommonData.DeploymentPackagesLocation);
           
            //Symbol settings
            process.Add("SymbolStorePath", Path.Combine(options.SharedTfsLocation, Constants.SymbolsFolder, folder));

            buildDef.ProcessParameters = WorkflowHelpers.SerializeProcessParameters(process);

            //Retention policies
            RetentionPoliciesHelper.SetRetentionPolicies(buildDef, 3, 2, 2, 2);

            return buildDef;
        }
    }
}
