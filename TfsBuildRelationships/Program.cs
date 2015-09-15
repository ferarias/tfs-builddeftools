using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using TfsBuildDefinitionsCommon;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Build.Evaluation;


namespace TfsBuildRelationships
{
    static class Program
    {
        private const string SharedAssembliesParamName = "EnsambladosACompartir";
        private const string ProjectsToBuildParamName = "ProjectsToBuild";

        private static DependencyGraph<string> Dependencies = new DependencyGraph<string>();
        private static Options options = new Options();

        static void Main(string[] args)
        {
            // Try to parse options from command line
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    foreach (var teamCollection in options.TeamCollections)
                    {
                        var tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamCollection));
                        ProcessTeamCollection(tfsTeamProjectCollection);
                    }

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

            var nodes = Dependencies.GetNodes();
            Console.WriteLine("{0} nodes", nodes.Count());
            Console.ReadKey();
        }

        private static void ProcessTeamCollection(TfsTeamProjectCollection teamCollection)
        {
            Console.WriteLine("Collection '{0}'", teamCollection.DisplayName.ToUpper());

            var collectionsBuildDefinitions = SearchBuildDefinitions(teamCollection);
            foreach (var collectionsBuildDefinition in collectionsBuildDefinitions)
            {
                ProcessBuildDefinitions(teamCollection, collectionsBuildDefinition);
            }

            Console.WriteLine();
        }




        private static void ProcessBuildDefinitions(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition)
        {
            if (options.Verbose)
                Console.WriteLine("Build definition: '{0}'", buildDefinition.Name);

            try
            {
                var paramValues = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters);
                //if (!paramValues.ContainsKey(SharedAssembliesParamName)) continue;
                //var sharedAssemblies = (string)paramValues[SharedAssembliesParamName];

                if (!paramValues.ContainsKey(ProjectsToBuildParamName))
                    Console.WriteLine("Build definition '{0}' does not compile any project.", buildDefinition.Name);
                else
                {

                    
                    var projectsToBuild = (string[])paramValues[ProjectsToBuildParamName];

                    foreach (var solutionFile in projectsToBuild.Where(x => x.EndsWith(".sln")))
                    {
                        ProcessSolution(teamCollection, buildDefinition, solutionFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process build definition '{0}'. {1}", buildDefinition.Name, ex.Message);
            }

            if (options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessSolution(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile)
        {
            if (options.Verbose)
                Console.WriteLine("Solution: '{0}'", solutionFile);

            try
            {

                using (var solutionFileStream = ReadFileFromServer(teamCollection, solutionFile))
                {
                    using (StreamReader reader = new StreamReader(solutionFileStream))
                    {
                        var solutionParser = new Solution(reader);
                        var projects = solutionParser.Projects;
                        foreach (var solutionProject in projects)
                        {
                            if (solutionProject.ProjectType != "SolutionFolder")
                            {
                                var projectDir = Path.GetDirectoryName(solutionFile);
                                var fullPath = Path.Combine(projectDir, solutionProject.RelativePath);
                                var projectFile = fullPath.Replace('\\', '/');
                                ProcessProject(teamCollection, buildDefinition, solutionFile, projectFile);

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process solution '{0}'. {1}", solutionFile, ex.Message);
            }

            if (options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessProject(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile)
        {
            if (options.Verbose)
                Console.WriteLine("Project: '{0}'", projectFile);

            try
            {

                using (var projectFileStream = ReadFileFromServer(teamCollection, projectFile))
                {
                    using (var reader = System.Xml.XmlReader.Create(projectFileStream))
                    {
                        var project = new Project(reader);

                        var assemblyName = project.Properties.FirstOrDefault(x => x.Name == "AssemblyName").EvaluatedValue;
                        var isFrameworkAssembly = IsFrameworkAssembly(assemblyName);

                        if(options.Verbose)
                            Console.WriteLine(assemblyName);

                        var references =
                            from item in project.Items
                            where item.ItemType == "Reference"
                            select item;

                        foreach (var reference in references)
                        {
                            var include = reference.EvaluatedInclude;
                            var includeAssemblyName = include.Split(',')[0];
                            var isIncludeFrameworkAssembly = IsFrameworkAssembly(includeAssemblyName);
                            if (!isFrameworkAssembly && !isIncludeFrameworkAssembly)
                            {
                                if (options.Verbose)
                                    Console.WriteLine("\treferences {0}", includeAssemblyName);
                                Dependencies.AddDependency(assemblyName, includeAssemblyName);
                            }
                            ProcessInclude(teamCollection, buildDefinition, solutionFile, projectFile, include);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process project '{0}'. {1}", projectFile, ex.Message);
            }
            if (options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessInclude(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile, string include)
        {
            if(options.Verbose)
                Console.WriteLine("Include: '{0}'", include);
        }

        private static Stream ReadFileFromServer(TfsTeamProjectCollection teamCollection, string projectFile)
        {
            var versionControlServer = (VersionControlServer)teamCollection.GetService(typeof(VersionControlServer));
            var item = versionControlServer.GetItem(projectFile);
            string tempFileName = System.IO.Path.GetTempFileName();
            return item.DownloadFile();

        }


        private static IEnumerable<IBuildDefinition> SearchBuildDefinitions(TfsTeamProjectCollection teamCollection)
        {
            var buildServer = teamCollection.GetService<IBuildServer>();
            var commonStructureService = teamCollection.GetService<ICommonStructureService>();


            Console.Write("Finding build definitions for collection '{0}'...", teamCollection.DisplayName);
            var buildDefinitionResults = Helpers.QueryBuildDefinitions(commonStructureService, buildServer, buildName: "*.Main");

            var buildDefinitions = new List<IBuildDefinition>();
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
            return buildDefinitions;
        }

        public static bool IsFrameworkAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft") || assemblyName == "mscorlib";
        }

    }
}
