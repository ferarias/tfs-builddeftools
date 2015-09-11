using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TfsBuildDefinitionsCommon;

namespace TfsCompareBuildDefinitions
{
    class Program
    {
        static void Main(string[] args)
        {
            // Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    CompareBuilds(options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("An error occured:");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Couldn't read options");
                Console.WriteLine();
            }
        }

        private static void CompareBuilds(Options options)
        {
            var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(options.TeamCollection));
            var buildServer = collection.GetService<IBuildServer>();
            var commonStructureService = collection.GetService<ICommonStructureService>();

            Console.Write("Finding build definitions for collection '{0}'...", collection.DisplayName);
            var buildDefinitionResults = Helpers.QueryBuildDefinitions(commonStructureService, buildServer, buildName: options.BuildName);

            var buildDefinitions = new List<IBuildDefinition>();
            foreach (var buildDefinitionResult in buildDefinitionResults)
            {
                if (buildDefinitionResult.Failures != null && buildDefinitionResult.Failures.Length > 0)
                {
                    // print out the errors
                    foreach (var f in buildDefinitionResult.Failures)
                    {
                        Console.WriteLine(f.Code + ": " + f.Message);
                    }
                }

                // There still might be some definitions to modify in this result
                buildDefinitions.AddRange(buildDefinitionResult.Definitions.Where(buildDefinition => buildDefinition != null && buildDefinition.QueueStatus == DefinitionQueueStatus.Enabled && !buildDefinition.Name.EndsWith(".Service")));

            }
            
            Console.WriteLine("{0} found", buildDefinitions.Count);

            var builds = ClassifyBuilds(buildDefinitions);


            foreach (var buildDefinition in builds)
            {
                if(options.Verbose)
                {
                    Console.WriteLine("Processing '{0}'", buildDefinition.Key);
                }
                if (buildDefinition.Value.Main == null)
                {
                    Console.WriteLine("Build definition '{0}' exists only in Release", buildDefinition.Key);
                    continue;
                }
                if (buildDefinition.Value.Release == null)
                {
                    Console.WriteLine("Build definition '{0}' exists only in Main", buildDefinition.Key);
                    continue;
                }

                var paramValuesMain = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.Value.Main.ProcessParameters);
                var paramValuesRelease = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.Value.Release.ProcessParameters);
                if (!paramValuesMain.ContainsKey(options.ParameterToCompare) || !paramValuesRelease.ContainsKey(options.ParameterToCompare)) continue;
                var assembliesPatternsMain = (string)paramValuesMain[options.ParameterToCompare];
                var assembliesPatternsRelease = (string)paramValuesRelease[options.ParameterToCompare];
                
                if(assembliesPatternsMain != assembliesPatternsRelease)
                {
                    Console.WriteLine("Build definitions '{0}' differ in Main and Release for parameter '{1}'", buildDefinition.Key, options.ParameterToCompare);
                    Console.WriteLine("\tMain:    '{0}'.", assembliesPatternsMain);
                    Console.WriteLine("\tRelease: '{0}'.", assembliesPatternsRelease);
                }
            }

        }

        private static Dictionary<string, BuildClassification> ClassifyBuilds(List<IBuildDefinition> buildDefinitions)
        {
            var builds = new Dictionary<string, BuildClassification>();
            foreach (var buildDefinition in buildDefinitions)
            {
                var lastDot = buildDefinition.Name.LastIndexOf('.');
                var name = buildDefinition.TeamProject + "\\" + buildDefinition.Name.Substring(0, lastDot);
                var extension = buildDefinition.Name.Substring(lastDot + 1);

                if (extension.ToLower() == "main")
                {
                    if (builds.ContainsKey(name))
                        builds[name].Main = buildDefinition;
                    else
                        builds.Add(name, new BuildClassification() { Main = buildDefinition });
                }

                if (extension.ToLower() == "release")
                {
                    if (builds.ContainsKey(name))
                        builds[name].Release = buildDefinition;
                    else
                        builds.Add(name, new BuildClassification() { Release = buildDefinition });
                }
            }
            return builds;
        }
    }

    internal class BuildClassification
    {
        public IBuildDefinition Main { get; set; }
        public IBuildDefinition Release { get; set; }

    }
}
