using Microsoft.Build.Evaluation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TfsBuildDefinitionsCommon;
using TfsBuildRelationships.Structures;

namespace TfsBuildRelationships
{
    public class TfsBuildsParser
    {

        public static bool Verbose { get; set; }

        public TeamCollectionsAssembliesInfo Process(string[] teamCollections, IEnumerable<string> excludeBuildDefinitions, bool verbose = false)
        {
            TfsBuildsParser.Verbose = verbose;
            var assemblyData = new TeamCollectionsAssembliesInfo();
            // Process each collection, build definition, solution, project and assembly
            foreach (var teamCollection in teamCollections)
            {
                var tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamCollection));
                Console.WriteLine("Collection '{0}'", tfsTeamProjectCollection.DisplayName);
                var buildDefinitionsAssembliesInfo = new BuildDefinitionsAssembliesInfo();
                var collectionBuildDefinitions = SearchBuildDefinitions(tfsTeamProjectCollection);
                foreach (var collectionBuildDefinition in collectionBuildDefinitions.Where(x => !excludeBuildDefinitions.Contains(x.Name)))
                {
                    var buildDefinitionAssembliesInfo = ProcessBuildDefinition(tfsTeamProjectCollection, collectionBuildDefinition);
                    buildDefinitionsAssembliesInfo.Add(collectionBuildDefinition.Name, buildDefinitionAssembliesInfo);
                }

                Console.WriteLine();
                assemblyData.Add(tfsTeamProjectCollection.DisplayName, buildDefinitionsAssembliesInfo);
            }
            return assemblyData;
        }

        private static SolutionsAssembliesInfo ProcessBuildDefinition(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition)
        {
            if (Verbose)
                Console.WriteLine("Build definition: '{0}'", buildDefinition.Name);

            var solutionsAssembliesInfo = new SolutionsAssembliesInfo();
            try
            {
                var paramValues = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters);
                //if (!paramValues.ContainsKey(SharedAssembliesParamName)) continue;
                //var sharedAssemblies = (string)paramValues[SharedAssembliesParamName];

                if (!paramValues.ContainsKey(Constants.ProjectsToBuildParamName))
                    Console.WriteLine("Build definition '{0}' does not compile any project.", buildDefinition.Name);
                else
                {
                    var projectsToBuild = (string[])paramValues[Constants.ProjectsToBuildParamName];
                    foreach (var solutionFile in projectsToBuild.Where(x => x.EndsWith(".sln")))
                    {
                        var assembliesInfo = ProcessSolution(teamCollection, buildDefinition, solutionFile);
                        solutionsAssembliesInfo.Add(solutionFile, assembliesInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process build definition '{0}'. {1}", buildDefinition.Name, ex.Message);
            }

            if (Verbose)
                Console.WriteLine();

            return solutionsAssembliesInfo;
        }

        private static AssembliesInfo ProcessSolution(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile)
        {
            if (Verbose)
                Console.WriteLine("Solution: '{0}'", solutionFile);

            var solutionAssemblies = new AssembliesInfo();

            try
            {

                using (var solutionFileStream = ReadFileFromVersionControlServer(teamCollection, solutionFile))
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
                                var projectAssembliesInfo = ProcessProject(teamCollection, buildDefinition, solutionFile, projectFile);
                                solutionAssemblies.MergeWith(projectAssembliesInfo);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process solution '{0}'. {1}", solutionFile, ex.Message);
            }

            if (Verbose)
                Console.WriteLine();

            return solutionAssemblies;
        }

        private static AssembliesInfo ProcessProject(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile)
        {
            if (Verbose)
                Console.WriteLine("Project: '{0}'", projectFile);

            var projectAssemblies = new AssembliesInfo();
            try
            {

                using (var projectFileStream = ReadFileFromVersionControlServer(teamCollection, projectFile))
                {
                    using (var reader = System.Xml.XmlReader.Create(projectFileStream))
                    {
                        var project = new Project(reader, null, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);

                        var assemblyName = project.Properties.FirstOrDefault(x => x.Name == "AssemblyName").EvaluatedValue;
                        var isFrameworkAssembly = IsFrameworkAssembly(assemblyName);

                        if (!isFrameworkAssembly)
                            projectAssemblies.OwnAssemblies.Add(assemblyName);

                        if (Verbose)
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
                                if (Verbose)
                                    Console.WriteLine("\treferences {0}", includeAssemblyName);
                                projectAssemblies.ReferencedAssemblies.Add(includeAssemblyName);
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
            if (Verbose)
                Console.WriteLine();

            return projectAssemblies;
        }

        private static void ProcessInclude(TfsTeamProjectCollection teamCollection, IBuildDefinition buildDefinition, string solutionFile, string projectFile, string include)
        {
            if (Verbose)
                Console.WriteLine("Include: '{0}'", include);
        }

        private static Stream ReadFileFromVersionControlServer(TfsTeamProjectCollection teamCollection, string projectFile)
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
