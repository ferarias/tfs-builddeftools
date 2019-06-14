using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using TfsBuildDefinitionsCommon;

namespace TfsFindBuildAssemblies
{
    static class Program
    {
        private const string SharedAssembliesParamName = "EnsambladosACompartir";

        static HashSet<string> _assembliesList;
        static List<string> _orphanedAssemblies;

        static void Main(string[] args)
        {
            // Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    FindAndProcess(options);
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

        private static void FindAndProcess(Options options)
        {
            var collectionsBuildDefinitions = new Dictionary<string, IEnumerable<IBuildDefinition>>();
            foreach (var teamCollection in options.TeamCollections)
            {
                var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamCollection));
                var buildServer = collection.GetService<IBuildServer>();
                var commonStructureService = collection.GetService<ICommonStructureService>();

                Console.WriteLine("Finding build definitions for collection '{0}'...", collection.DisplayName);
                

                var buildDefinitions = new List<IBuildDefinition>();
                foreach(var bdName in options.BuildName)
                {
                    var buildDefinitionResults = Helpers.QueryBuildDefinitions(commonStructureService, buildServer, buildName: bdName);

                    foreach (var buildDefinitionResult in buildDefinitionResults)
                    {
                        if (buildDefinitionResult.Failures != null && buildDefinitionResult.Failures.Length > 0)
                        {
                            // print out the errors
                            foreach (var f in buildDefinitionResult.Failures)
                            {
                                Console.WriteLine(string.Format("{0}: {1}", f.Code, f.Message));
                            }
                        }

                        // There still might be some definitions to modify in this result
                        buildDefinitions.AddRange(buildDefinitionResult.Definitions.Where(buildDefinition => buildDefinition != null && buildDefinition.QueueStatus == DefinitionQueueStatus.Enabled));

                    }
                    Console.WriteLine("{0} found for '{1}'", buildDefinitions.Count, bdName);
                }
                collectionsBuildDefinitions.Add(collection.DisplayName, buildDefinitions);
            }

            _assembliesList = new HashSet<string>();
    
            using (var outputFile = new StreamWriter(options.OutFile, false))
            {
                outputFile.WriteLine("Team Collection\tTeam Project\tBuild definition\tAssembly Pattern\tAssembly\tDate\tDuplicate");
                foreach (var collectionsBuildDefinition in collectionsBuildDefinitions)
                {
                    Console.Write("Processing collection '{0}'... ", collectionsBuildDefinition.Key);
                    int count = FindAssembliesFromBuildDefinitions(collectionsBuildDefinition, options, outputFile);
                    Console.WriteLine("Done!", count);
                }

                _orphanedAssemblies = Directory.GetFiles(options.AssembliesLocation, "*.dll").ToList();
                _orphanedAssemblies.RemoveAll(e => _assembliesList.Contains(e));
                foreach (var assembly in _orphanedAssemblies)
                {
                    outputFile.WriteLine("N/A\tN/A\tN/A\tN/A\t{0}\t{1}\tFalse", Path.GetFileName(assembly), File.GetLastWriteTime(assembly));
                }
            }

            Console.WriteLine("{0} items, {1} orphaned assemblies",
                _assembliesList.Count(), _orphanedAssemblies.Count);


            Console.WriteLine("Output written to '{0}'",options.OutFile);
        }

        private static int FindAssembliesFromBuildDefinitions(KeyValuePair<string, IEnumerable<IBuildDefinition>> buildDefinitions, Options options, TextWriter outputFile)
        {
            var count = 0;
            var dup = 0;
            foreach (var buildDefinition in buildDefinitions.Value)
            {
                var paramValues = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters);
                if (!paramValues.ContainsKey(SharedAssembliesParamName)) continue;
                var assembliesPatterns = ((string)paramValues[SharedAssembliesParamName]).Split(';');
                foreach (var assembliesPattern in assembliesPatterns)
                {
                    var assemblies = Directory.GetFiles(options.AssembliesLocation, assembliesPattern);
                    foreach (var assembly in assemblies)
                    {
                        var duplicate = _assembliesList.Contains(assembly);
                        if (duplicate)
                            dup++;
                        else
                        {
                            _assembliesList.Add(assembly);
                            count++;
                        }

                        outputFile.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", buildDefinitions.Key, buildDefinition.TeamProject, buildDefinition.Name, assembliesPattern, Path.GetFileName(assembly),
                            File.GetLastWriteTime(assembly), duplicate);
                    }
                }
            }

            Console.Write("{0} items ({1} added, {2} duplicates). ", count + dup, count, dup);
            return count;
        }

        
    }
}
