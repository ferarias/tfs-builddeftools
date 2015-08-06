using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Server;
using TfsBuildDefinitionsCommon;

namespace TfsUpdateBuildDefinitions
{
    static class Program
    {

        private static CommonStructure CommonData { get; set; }

        private static Dictionary<string, Dictionary<string, string>> _externalConfiguration = new Dictionary<string, Dictionary<string, string>>();

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("\tTfsUpdateBuildDefinition");
            Console.WriteLine();
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
                    Console.WriteLine("\tParallel processing: {0}", options.Parallel);
                    Console.WriteLine("\tData file: {0}", options.DataFile);
                    Console.WriteLine("\tSkip Params: {0}", options.SkipParameters);
                    Console.WriteLine("\tSave build definitions: {0}", options.Save);
                    Console.WriteLine("\tDeployment packages UNC: {0}", CommonData.DeploymentPackagesLocation);

                    _externalConfiguration = ReadDataFile(options.DataFile);
                    if (_externalConfiguration.Any())
                    {
                        Console.WriteLine("\t{0} build definition configurations read from file", _externalConfiguration.Count);
                    }
                    Console.WriteLine("-------------------------------------------------------------------------------");

                    var buildDefinitionsToSave = ChangeDefinitions(options);

                    if (buildDefinitionsToSave.Count == 0)
                        Console.WriteLine("All build definitions are up to date!");
                    else
                    {
                        if (options.Save)
                        {
                            CommonData.BuildServer.SaveBuildDefinitions(buildDefinitionsToSave.ToArray());
                            Console.WriteLine("Updating {0} build definitions...", buildDefinitionsToSave.Count);
                        }
                        else
                        {
                            Console.WriteLine("The simulation was OK. {0} build definition(s) to update. Use --save to actually update the build definition.", buildDefinitionsToSave.Count);
                        }
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
            }
        }

        private static Dictionary<string, Dictionary<string, string>> ReadDataFile(string dataFile)
        {
            var customConfiguration = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                if (String.IsNullOrEmpty(dataFile)) return customConfiguration;
                if (!File.Exists(dataFile))
                {
                    Console.WriteLine("\nData file could not be found: '{0}'\n", dataFile);
                    return customConfiguration;
                }
                var count = 0;

                using (var file = new StreamReader(dataFile))
                {
                    string line;
                    string[] columns = { };
                    line = file.ReadLine();
                    if (line != null)
                    {
                        columns = line.Split('\t');
                    }


                    while ((line = file.ReadLine()) != null)
                    {
                        var parts = line.Split('\t');
                        var defName = parts[0];
                        var kvp = new Dictionary<string, string>();
                        for (int i = 1; i < parts.Length; i++)
                        {
                            kvp.Add(columns[i], parts[i]);
                        }
                        if (customConfiguration.ContainsKey(defName))
                            Console.WriteLine("\nDuplicate entry skipped: '{0}'\n", defName);
                        else
                            customConfiguration.Add(defName, kvp);
                        count++;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("\nAn error occured while reading data file:\n{0}\n", ex.Message);
                throw;
            }
            return customConfiguration;
        }


        private static List<IBuildDefinition> ChangeDefinitions(Options options)
        {
            var buildDefinitionResults = Helpers.QueryBuildDefinitions(CommonData.CommonStructureService, CommonData.BuildServer,
                options.TeamProject, options.BuildName);

            var buildDefinitionsToSave = new List<IBuildDefinition>();
            foreach (var buildDefinitionResult in buildDefinitionResults)
            {
                if (buildDefinitionResult.Failures != null && buildDefinitionResult.Failures.Length > 0)
                {
                    // print out the errors
                    foreach (var f in buildDefinitionResult.Failures) Console.WriteLine(f.Code + ": " + f.Message);
                }

                // There still might be some definitions to modify in this result
                var nonNullBuildDefs = buildDefinitionResult.Definitions.Where(buildDefinition => buildDefinition != null);
                if (options.Parallel)
                    Parallel.ForEach(nonNullBuildDefs, buildDefinition =>
                    {
                        if (ChangeDefinition(buildDefinition, options))
                            buildDefinitionsToSave.Add(buildDefinition);
                    });
                else
                    foreach (var buildDefinition in nonNullBuildDefs)
                    {
                        if (ChangeDefinition(buildDefinition, options))
                            buildDefinitionsToSave.Add(buildDefinition);
                    }
            }

            return buildDefinitionsToSave;


        }

        

        private static bool ChangeDefinition(IBuildDefinition definition, Options options)
        {
            try
            {
                var buildType = GetBuildType(definition);
                // for now, skip CDN and resources build definitions
                if (buildType == CustomBuildType.Cdn || buildType == CustomBuildType.NoCompileDev
                     || buildType == CustomBuildType.NoCompileMain || buildType == CustomBuildType.NoCompileRelease) return false;

                if (!CheckTemplates(buildType)) return false;
                
                var messages = new StringBuilder();

                var changed = SetTemplate(definition, buildType, messages) ||
                              SetDropLocation(definition, options, buildType, messages);

                var paramValues = WorkflowHelpers.DeserializeProcessParameters(definition.ProcessParameters);

                var config = GetConfiguration(definition, options, buildType);
                foreach (var c in config)
                {
                    if (options.SkipParameters != null && options.SkipParameters.Contains(c.Key))
                    {
                        if (options.Verbose) messages.AppendFormat("Skip parameter '{0}' ('{1}')\r\n", c.Key, Helpers.ObjectToString(c.Value));
                    }
                    else
                    {
                        changed = changed | AddOrUpdate(paramValues, c.Key, c.Value, messages);
                    }
                }


                if (!changed) return false;

                definition.ProcessParameters = WorkflowHelpers.SerializeProcessParameters(paramValues);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Build definition '{0}' has changes", definition.Name);
                Console.ResetColor();
                Console.WriteLine(messages);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        private static Dictionary<string, object> GetConfiguration(IBuildGroupItem definition, Options options, CustomBuildType buildType)
        {
            var isMain = buildType == CustomBuildType.StandardMain || buildType == CustomBuildType.NoCompileMain;
            var isRelease = buildType == CustomBuildType.StandardRelease || buildType == CustomBuildType.NoCompileRelease ||
                            buildType == CustomBuildType.WindowsService;
            string subfolder;
            if (isRelease) subfolder = Constants.ReleaseFolder;
            else if (isMain) subfolder = Constants.MainFolder;
            else subfolder = Constants.DevFolder;


            var config = new Dictionary<string, object>()
            {
                {"CreateWorkItem", false},
                {"PerformTestImpactAnalysis", false},
                {"CreateLabel", true},
                {"AssociateChangesetsAndWorkItems", false},
                {"ConfigurationsToBuild", (isMain || isRelease) ? new[] {Constants.AnyCpuRelease} : new[] {Constants.AnyCpuDebug}},
                {"DisableTests", !isRelease},
                {"DropBuild", (isMain || isRelease) && buildType != CustomBuildType.WindowsService},
                {"IsRelease", isRelease},
                {"DeploymentPackagesLocation", buildType == CustomBuildType.StandardRelease ? CommonData.DeploymentPackagesLocation : null},
                {"CustomBinariesReferencePath", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, isRelease ? Constants.ReleaseFolder : Constants.MainFolder)},
                {"CustomBinariesDestination", Path.Combine(options.SharedTfsLocation, Constants.AssembliesFolder, subfolder)},
                {"SymbolStorePath", Path.Combine(options.SharedTfsLocation, Constants.SymbolsFolder, subfolder)},
            };

            if (!_externalConfiguration.ContainsKey(definition.Name)) return config;

            var customConfig = _externalConfiguration[definition.Name];
            foreach (var customConfigItem in customConfig)
            {
                if (config.ContainsKey(customConfigItem.Key)) 
                    config[customConfigItem.Key] = customConfigItem.Value;
                else
                {
                    config.Add(customConfigItem.Key, customConfigItem.Value);
                }
            }
            return config;
        }

        /// <summary>
        /// Set the default drop location for the builddef according to its type
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="options"></param>
        /// <param name="buildType"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static bool SetDropLocation(IBuildDefinition definition, Options options, CustomBuildType buildType, StringBuilder messages)
        {
            var newLocation = String.Empty;
            switch (buildType)
            {
                case CustomBuildType.StandardDev:
                case CustomBuildType.NoCompileDev:
                    newLocation = Path.Combine(options.SharedTfsLocation, Constants.DropFolder, Constants.DevFolder);
                    break;
                case CustomBuildType.StandardMain:
                case CustomBuildType.NoCompileMain:
                    newLocation = Path.Combine(options.SharedTfsLocation, Constants.DropFolder, Constants.MainFolder);
                    break;
                case CustomBuildType.StandardRelease:
                case CustomBuildType.NoCompileRelease:
                    newLocation = "#/";
                    break;
                case CustomBuildType.WindowsService:
                    newLocation = "";
                    break;
            }

            if (newLocation == definition.DefaultDropLocation) return false;

            messages.AppendFormat("[#] DefaultDropLocation: '{0}' => '{1}'\r\n",
                definition.DefaultDropLocation, newLocation);
            definition.DefaultDropLocation = newLocation;
            return true;
        }

        /// <summary>
        /// Set the build definition template according to its type
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="buildType"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static bool SetTemplate(IBuildDefinition definition, CustomBuildType buildType, StringBuilder messages)
        {
            var newTemplate = CommonData.StandardTemplate;
            switch (buildType)
            {
                case CustomBuildType.StandardDev:
                case CustomBuildType.StandardMain:
                case CustomBuildType.StandardRelease:
                    newTemplate = CommonData.StandardTemplate;
                    break;
                case CustomBuildType.NoCompileDev:
                case CustomBuildType.NoCompileMain:
                case CustomBuildType.NoCompileRelease:
                    newTemplate = CommonData.NoCompileFullTemplate;
                    break;
                case CustomBuildType.WindowsService:
                    newTemplate = CommonData.ServicesTemplate;
                    break;
            }

            if (newTemplate.Id == definition.Process.Id) return false;

            messages.AppendFormat("[#] ProcessTemplate: '{0}' ({1}) => '{2}' ({3})\r\n",
                definition.Process.ServerPath, definition.Process.Id, newTemplate.ServerPath, newTemplate.Id);
            definition.Process = newTemplate;
            return true;

        }

        /// <summary>
        /// Checks that the required template exists
        /// </summary>
        /// <param name="buildType"></param>
        /// <returns></returns>
        private static bool CheckTemplates(CustomBuildType buildType)
        {
            switch (buildType)
            {
                case CustomBuildType.StandardDev:
                case CustomBuildType.StandardMain:
                case CustomBuildType.StandardRelease:
                    if (CommonData.StandardTemplate == null) return false;
                    break;
                case CustomBuildType.NoCompileDev:
                case CustomBuildType.NoCompileMain:
                case CustomBuildType.NoCompileRelease:
                    if (CommonData.NoCompileFullTemplate == null) return false;
                    break;
                case CustomBuildType.WindowsService:
                    if (CommonData.ServicesTemplate == null) return false;
                    break;
            }
            return true;
        }

        /// <summary>
        /// Obtain the build type from the build definition name
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        private static CustomBuildType GetBuildType(IBuildGroupItem definition)
        {
            var buildType = CustomBuildType.StandardDev;
            if (definition == null) return buildType;
            var buildDefName = definition.Name.ToLower();
            if (buildDefName.EndsWith(Constants.ServiceBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.WindowsService;
            else if (buildDefName.StartsWith(Constants.CdnBuildsNamePrefix.ToLower()))
                buildType = CustomBuildType.Cdn;
            else if (buildDefName.EndsWith(Constants.ResourcesDevBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.NoCompileDev;
            else if (buildDefName.EndsWith(Constants.ResourcesMainBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.NoCompileMain;
            else if (buildDefName.EndsWith(Constants.ResourcesReleaseBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.NoCompileRelease;
            else if (buildDefName.EndsWith(Constants.DevBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.StandardDev;
            else if (buildDefName.EndsWith(Constants.MainBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.StandardMain;
            else if (buildDefName.EndsWith(Constants.ReleaseBuildsNameExtension.ToLower()))
                buildType = CustomBuildType.StandardRelease;
            return buildType;
        }

        public static bool AddOrUpdate(IDictionary<String, Object> paramValues, String key, Object value, StringBuilder messages)
        {
            if (value == null)
            {
                if (paramValues.ContainsKey(key))
                {
                    messages.AppendFormat("[-] '{0}': '{1}'\r\n", key, Helpers.ObjectToString(paramValues[key]));
                    paramValues.Remove(key);
                    return true;
                }
                return false;
            }

            if (paramValues.ContainsKey(key))
            {
                if (Helpers.ObjectsAreEquivalent(paramValues[key], value)) return false;
                messages.AppendFormat("[#] '{0}': '{1}' => '{2}'\r\n", key, Helpers.ObjectToString(paramValues[key]), Helpers.ObjectToString(value));
                paramValues[key] = value;
                return true;
            }
            paramValues.Add(key, value);
            messages.AppendFormat("[+] '{0}': '{1}'\r\n", key, Helpers.ObjectToString(value));
            return true;
        }

        private static void DrawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }
    }
}