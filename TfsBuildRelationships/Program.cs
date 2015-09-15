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
        private const string ProjectsToBuildParamName = "ProjectsToBuild";
        private static Options _options = new Options();
        private static DependencyGraph<string> _dependencies = new DependencyGraph<string>();
        private static Dictionary<string, SolutionRelations> _solutionRelationships = new Dictionary<string, SolutionRelations>();

        static void Main(string[] args)
        {
            // Try to parse options from command line
            if (Parser.Default.ParseArguments(args, _options))
            {
                try
                {
                    foreach (var teamCollection in _options.TeamCollections)
                    {
                        var tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamCollection));
                        ProcessTeamCollection(tfsTeamProjectCollection);
                    }

                    var solutionDependencies = GetSolutionDependencies(_solutionRelationships);

                    // Nodes nobody depends upon (highest-level assemblies)
                    var startNodes = FindStartNodes(solutionDependencies);

                    // Nodes with no dependencies (low-level assemblies)
                    var endNodes = solutionDependencies.Where(x => x.Value.Count() == 0);

                    foreach(var solutionDependency in solutionDependencies)
                    {
                        if (solutionDependency.Value.Count() == 0)
                            Console.WriteLine("{0} does not have dependencies", solutionDependency.Key);
                        else
                        {
                            Console.WriteLine("{0} depends on", solutionDependency.Key);
                            foreach (var dep in solutionDependency.Value)
                                Console.WriteLine("\t{0}", dep);
                        }
                    }

                    FindCircularReferences(solutionDependencies, startNodes);

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
        private static void FindCircularReferences(
            Dictionary<string, HashSet<string>> solutionDependencies,
            Dictionary<string, HashSet<string>> startNodes)
        {
            var processed = new HashSet<string>();
            var processing = new Queue<string>();
            
            foreach (var node in startNodes)
                ProcessNode(node.Key, solutionDependencies, ref processing, ref processed);
        }

        private static Dictionary<string, HashSet<string>> GetSolutionDependencies(Dictionary<string, SolutionRelations> solutionRelationships)
        {
            var solutionDependencies = new Dictionary<string, HashSet<string>>();
            foreach (var relationship in solutionRelationships)
            {
                var solution = relationship.Key;
                var deps = new HashSet<string>();
                foreach (var reference in relationship.Value.ReferencedAssemblies)
                {
                    var relatedSolutions = solutionRelationships.Where(x => x.Value.OwnAssemblies.Contains(reference));
                    foreach (var relatedSolution in relatedSolutions)
                        deps.Add(relatedSolution.Key);
                }
                solutionDependencies.Add(solution, deps);
            }
            return solutionDependencies;
        }



        private static Dictionary<string, HashSet<string>> FindStartNodes(Dictionary<string, HashSet<string>> solutionDependencies)
        {
            var startNodes = new Dictionary<string, HashSet<string>>(solutionDependencies);
            var nodesToRemove = new List<string>();
            foreach (var x in solutionDependencies)
            {
                foreach (var y in solutionDependencies)
                {
                    if (y.Value.Contains(x.Key))
                        nodesToRemove.Add(x.Key);
                }
            }
            foreach (var nodeToRemove in nodesToRemove)
                startNodes.Remove(nodeToRemove);
            return startNodes;
        }

        private static void ProcessNode(string node, Dictionary<string, HashSet<string>> solutionDependencies, ref Queue<string> processing, ref HashSet<string> processed)
        {
            processing.Enqueue(node);
            foreach (var subnode in solutionDependencies[node])
            {
                if (processing.Contains(subnode))
                {
                    Console.WriteLine("Circular reference: {0}->{1}", String.Join("->", processing.ToArray()), subnode);
                }
                else
                {
                    if(!processed.Contains(subnode))
                        ProcessNode(subnode, solutionDependencies, ref processing, ref processed);
                }
            }
            processed.Add(processing.Dequeue());
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
            if (_options.Verbose)
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

            if (_options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessSolution(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile)
        {
            if (_options.Verbose)
                Console.WriteLine("Solution: '{0}'", solutionFile);

            if (!_solutionRelationships.ContainsKey(solutionFile))
                _solutionRelationships.Add(solutionFile, new SolutionRelations());

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

            if (_options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessProject(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile)
        {
            if (_options.Verbose)
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

                        if (!isFrameworkAssembly)
                            _solutionRelationships[solutionFile].OwnAssemblies.Add(assemblyName);

                        if(_options.Verbose)
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
                                if (_options.Verbose)
                                    Console.WriteLine("\treferences {0}", includeAssemblyName);
                                _dependencies.AddDependency(assemblyName, includeAssemblyName);
                                _solutionRelationships[solutionFile].ReferencedAssemblies.Add(includeAssemblyName);
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
            if (_options.Verbose)
                Console.WriteLine();
        }

        private static void ProcessInclude(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile, string include)
        {
            if(_options.Verbose)
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
