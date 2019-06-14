using System;
using System.IO;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using TfsBuildDefinitionsCommon;

namespace TfsCreateServiceBuildDefinition
{
    static class Program
    {

        private static CommonStructure CommonData { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("\tTfsCreateServiceBuildDefinition");
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

                    Console.WriteLine("\tTeam Project Collection: {0}", options.TeamCollection);
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
                    Console.WriteLine("\tServices: {0}", options.ServiceToDeploy);
                    Console.WriteLine("\tServer hostname: {0}", options.ServiceHost);
                    Console.WriteLine("\tRemote location into server: {0}", options.ServiceLocation);
                    Console.WriteLine("\tLocal path into server: {0}", options.ServiceLocalPath);
                    Console.WriteLine("\tUser name: {0}", options.ServiceUser);
                    Console.WriteLine("\tPassword: {0}", options.ServicePassword);
                    Console.WriteLine("-------------------------------------------------------------------------------");

                    var buildDef = CreateDefinition(options, Constants.ReleaseFolder);

                    if (options.Save)
                    {
                        buildDef.Save();
                        Console.WriteLine("The build definition was created");
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
                Console.WriteLine("Example: TfsCreateServiceBuildDefinition.exe --project Hoteles --name Hot.MyService.Service --description \"Super build\" --tfspath $/Hoteles/Release/Services/MyService\n --solution Toolfactory.Hotels.MyService.sln --controller logpmcompile02p\n --service MyService;Toolfactory.Hotels.MyService.exe --servicelocation EjecutablesNET\\Hoteles --servicelocalpath D:\\EjecutablesNET\\Hoteles\n --servicehost logviprc02v.logitravelprod.local --serviceuser logviprc02v\\administrator --servicepassword zlgaYrnnu2GM2N/2v96Jdw==\n --test false");

            }

        }


        private static IBuildDefinition CreateDefinition(Options options, string folder)
        {
            CommonData.Collection.EnsureAuthenticated();

            //Create build definition and give it a name and desription
            var buildDef = CommonData.BuildServer.CreateBuildDefinition(options.TeamProject);
            buildDef.Name = options.BuildName.EndsWith(".Service") ? options.BuildName : options.BuildName + ".Service";
            buildDef.Description = options.Description;
            buildDef.ContinuousIntegrationType = ContinuousIntegrationType.Individual;

            //Controller and default build process template
            buildDef.BuildController = CommonData.BuildServer.GetBuildController(options.BuildController);

            buildDef.Process = CommonData.ServicesTemplate;

            //Drop location & source settings
            buildDef.DefaultDropLocation = "";
            buildDef.Workspace.AddMapping(options.TfsProjectPath, "$(SourceDir)", WorkspaceMappingType.Map);

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
            process.Add("DropBuild", false);

            process.Add("CustomBinariesReferencePath", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, folder));
            process.Add("CustomBinariesDestination", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, folder));
            process.Add("EnsambladosACompartir", options.BinariesPattern);
            process.Add("IsRelease", folder.Equals(Constants.ReleaseFolder));

            // Windows service-specific builddef parameters
            process.Add("WindowsServicesToDeploy", new []{ options.ServiceToDeploy});
            process.Add("WindowsServicesMachine", options.ServiceHost);
            process.Add("WindowsServicesLocation", options.ServiceLocation);
            process.Add("WindowsServicesMachineLocalPath", options.ServiceLocalPath);

            if (String.IsNullOrWhiteSpace(options.ServiceUser))
                process.Add("IsVigo", false);
            else
            {
                process.Add("IsVigo", true);
                process.Add("RemoteUser", options.ServiceUser.Trim());
                process.Add("RemotePassword", options.ServicePassword.Trim());
            }

            //Symbol settings
            process.Add("SymbolStorePath", Path.Combine(options.SharedTfsLocation, Constants.SymbolsFolder, folder));

            buildDef.ProcessParameters = WorkflowHelpers.SerializeProcessParameters(process);

            //Retention policies
            RetentionPoliciesHelper.SetRetentionPolicies(buildDef, 3, 2, 2, 2);

            return buildDef;
        }
    }
}
