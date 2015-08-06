﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using TfsBuildDefinitionsCommon;

namespace TfsViewBuildDefinitions
{
    static class Program
    {

        private static TfsTeamProjectCollection _collection;
        private static IBuildServer _buildServer;
        private static ICommonStructureService _commonStructureService;
        private static IPrinter _printer;

        static void Main(string[] args)
        {
            // Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    _collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(options.TeamCollection));
                    _buildServer = _collection.GetService<IBuildServer>();
                    _commonStructureService = _collection.GetService<ICommonStructureService>();
                    _printer = new TabbedPrinter();

                    PrintDefinitions(options);
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

        private static void PrintDefinitions(Options options)
        {
            var buildDefinitionResults = GetBuildDefinitions(options.TeamProject, options.BuildName);

            var list = new List<IBuildDefinition>();
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
                list.AddRange(buildDefinitionResult.Definitions.Where(buildDefinition => buildDefinition != null));
                
            }

            _printer.PrintDefinitionDetails(list, options.PrintTemplate, options.PrintTrigger, options.PrintDropLocation,
                options.PrintParameters, options.PrintPolicies, options.PrintBuilds, options.Parameters, options.Filters);
            

        }

        
        private static IEnumerable<IBuildDefinitionQueryResult> GetBuildDefinitions(string projectName = "", string buildName = "")
        {
            var specs = new List<IBuildDefinitionSpec>();

            if (String.IsNullOrWhiteSpace(projectName))
            {
                // Get a query spec for each team project
                if (String.IsNullOrWhiteSpace(buildName))
                specs.AddRange(_commonStructureService.ListProjects().Select(pi => _buildServer.CreateBuildDefinitionSpec(pi.Name)));
                else
                    specs.AddRange(_commonStructureService.ListProjects().Select(pi => _buildServer.CreateBuildDefinitionSpec(pi.Name, buildName)));
            }
            else
            {
                // Get a query spec just for this team project
                if (String.IsNullOrWhiteSpace(buildName))
                    specs.Add(_buildServer.CreateBuildDefinitionSpec(projectName));
                else
                    specs.Add(_buildServer.CreateBuildDefinitionSpec(projectName, buildName));
            }
            
            // Query the definitions
            var results = _buildServer.QueryBuildDefinitions(specs.ToArray());
            return results;
        }

    }
}
