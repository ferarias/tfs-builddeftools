﻿using Microsoft.Build.Evaluation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TfsBuildDefinitionsCommon;
using TfsBuildRelationships.AssemblyInfo;

namespace TfsBuildRelationships
{
    public class TfsBuildsParser
    {

        public bool Verbose { get; set; }

        public IEnumerable<string> ExcludeBuildDefinitions { get; set; }

        public string[] FilterTeamProjects { get; set; }

        public string BuildNameFilter { get; set; }

        public AssembliesInfo Process(IEnumerable<string> teamCollections)
        {
            var assembliesInfo = new AssembliesInfo();
            // Process each collection, build definition, solution, project and assembly
            foreach (var teamCollection in teamCollections)
            {
                var tfsTeamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamCollection));
                Console.WriteLine($"Collection '{tfsTeamProjectCollection.DisplayName}'");

                var teamCollectionInfo = new TeamCollectionInfo(tfsTeamProjectCollection);
                var buildDefinitions = SearchBuildDefinitions(tfsTeamProjectCollection, BuildNameFilter);
                if (FilterTeamProjects.Any())
                    buildDefinitions = buildDefinitions.Where(x => FilterTeamProjects.Contains(x.TeamProject));
                foreach (var buildDefinition in buildDefinitions.Where(x => !ExcludeBuildDefinitions.Contains(x.Name)))
                {
                    var buildDefinitionAssembliesInfo = ProcessBuildDefinition(teamCollectionInfo, buildDefinition);
                    teamCollectionInfo.BuildDefinitions.Add(buildDefinitionAssembliesInfo);
                }

                Console.WriteLine();
                assembliesInfo.TeamCollections.Add(teamCollectionInfo);
            }
            return assembliesInfo;
        }

        private BuildDefinitionInfo ProcessBuildDefinition(TeamCollectionInfo teamCollectionInfo, IBuildDefinition buildDefinition)
        {
            if (Verbose)
                Console.WriteLine($"Build definition: '{buildDefinition.Name}'");

            var buildDefinitionInfo = new BuildDefinitionInfo(teamCollectionInfo, buildDefinition);
            try
            {
                var paramValues = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters);
                //if (!paramValues.ContainsKey(SharedAssembliesParamName)) continue;
                //var sharedAssemblies = (string)paramValues[SharedAssembliesParamName];

                if (!paramValues.ContainsKey(Constants.ProjectsToBuildParamName))
                    Console.WriteLine("Build definition '{0}' does not compile any project.", buildDefinition.Name);
                else
                {
                    var solutionFiles = (string[])paramValues[Constants.ProjectsToBuildParamName];
                    foreach (var solutionFile in solutionFiles.Where(x => x.EndsWith(".sln")))
                    {
                        var assembliesInfo = ProcessSolution(buildDefinitionInfo, solutionFile);
                        buildDefinitionInfo.Solutions.Add(assembliesInfo);
                        buildDefinitionInfo.ReferencedAssemblies.UnionWith(assembliesInfo.ReferencedAssemblies);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not process build definition '{0}'. {1}", buildDefinition.Name, ex.Message);
            }

            if (Verbose)
                Console.WriteLine();

            return buildDefinitionInfo;
        }

        private SolutionInfo ProcessSolution(BuildDefinitionInfo buildDefinitionInfo, string solutionFile)
        {
            if (Verbose)
                Console.WriteLine("Solution: '{0}'", solutionFile);

            var solutionInfo = new SolutionInfo(buildDefinitionInfo, solutionFile);

            try
            {

                using (var solutionFileStream = ReadFileFromVersionControlServer(buildDefinitionInfo.TeamCollection.Collection, solutionFile))
                {
                    using (StreamReader reader = new StreamReader(solutionFileStream))
                    {
                        var solutionParser = new Solution(reader);

                        var projects = solutionParser.Projects;
                        foreach (var solutionProject in projects)
                        {
                            if (solutionProject.ProjectType == "SolutionFolder" || Regex.Match(solutionProject.ProjectName, "Test", RegexOptions.IgnoreCase).Success) continue;
                            var projectDir = Path.GetDirectoryName(solutionFile);
                            if (projectDir == null) continue;
                            var fullPath = Path.Combine(projectDir, solutionProject.RelativePath);
                            var projectFile = fullPath.Replace('\\', '/');
                            var projectAssembliesInfo = ProcessProject(solutionInfo, projectFile);
                            solutionInfo.Projects.Add(projectAssembliesInfo);
                            solutionInfo.ReferencedAssemblies.UnionWith(projectAssembliesInfo.ReferencedAssemblies);
                        }
                    }
                }

                // if there are references between projects, add them also
                foreach (var project in solutionInfo.Projects)
                {
                    foreach (var referencedProject in project.ReferencedProjects)
                    {
                        var referenceMatches = solutionInfo[referencedProject];
                        if (referenceMatches != null)
                        {
                            project.ReferencedAssemblies.Add(referenceMatches.GeneratedAssembly);
                            solutionInfo.ReferencedAssemblies.UnionWith(project.ReferencedAssemblies);
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

            return solutionInfo;
        }

        private AssemblyInfo.ProjectInfo ProcessProject(SolutionInfo solutionInfo, string projectFile)
        {
            if (Verbose)
                Console.WriteLine("Project: '{0}'", projectFile);

            var projectInfo = new AssemblyInfo.ProjectInfo(solutionInfo);
            try
            {

                using (var projectFileStream = ReadFileFromVersionControlServer(solutionInfo.BuildDefinition.TeamCollection.Collection, projectFile))
                {
                    using (var reader = System.Xml.XmlReader.Create(projectFileStream))
                    {
                        var project = new Project(reader, null, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);

                        var assemblyName = project.Properties.FirstOrDefault(x => x.Name == "AssemblyName")?.EvaluatedValue;
                        var projectGuidProperty = project.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
                        if (projectGuidProperty != null)
                        {
                            var projectGuid = new Guid(projectGuidProperty.EvaluatedValue);

                            var isFrameworkAssembly = IsFrameworkAssembly(assemblyName);

                            if (!isFrameworkAssembly)
                            {
                                projectInfo.GeneratedAssembly = assemblyName;
                                projectInfo.ProjectGuid = projectGuid;
                            }

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
                                        Console.WriteLine("\tReferences {0}", includeAssemblyName);
                                    projectInfo.ReferencedAssemblies.Add(includeAssemblyName);
                                }
                                ProcessInclude(projectInfo, include);
                            }
                        }
                        else
                        {
                            Console.WriteLine("\t has no project GUID!");
                        }

                        var projectReferences =
                            from item in project.Items
                            where item.ItemType == "ProjectReference"
                            select item;

                        foreach (var projectReference in projectReferences)
                        {
                            if (Verbose)
                                Console.WriteLine("\t references project '{0}'", projectReference);

                            var referenceGuid = projectReference.DirectMetadata.FirstOrDefault(x => x.Name == "Project");
                            if (referenceGuid != null)
                                projectInfo.ReferencedProjects.Add(new Guid(referenceGuid.EvaluatedValue));
                            else
                                Console.WriteLine("\t has no project GUID!");
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

            return projectInfo;
        }

        private void ProcessInclude(AssemblyInfo.ProjectInfo projectInfo, string include)
        {
            if (Verbose)
                Console.WriteLine($"Project: {projectInfo}, Include: '{include}'");
        }

        #region "Helpers"
        private static Stream ReadFileFromVersionControlServer(IServiceProvider teamCollection, string projectFile)
        {
            var versionControlServer = (VersionControlServer)teamCollection.GetService(typeof(VersionControlServer));
            var item = versionControlServer.GetItem(projectFile);
            return item.DownloadFile();
        }

        private static IEnumerable<IBuildDefinition> SearchBuildDefinitions(TfsTeamProjectCollection teamCollection, string buildNameFilter)
        {
            var buildServer = teamCollection.GetService<IBuildServer>();
            var commonStructureService = teamCollection.GetService<ICommonStructureService>();
            var buildDefinitionResults = Helpers.QueryBuildDefinitions(commonStructureService, buildServer, buildName: buildNameFilter);

            var buildDefinitions = new List<IBuildDefinition>();
            foreach (var buildDefinitionResult in buildDefinitionResults)
            {
                if (buildDefinitionResult.Failures != null && buildDefinitionResult.Failures.Length > 0)
                {
                    // print out the errors
                    foreach (var f in buildDefinitionResult.Failures)
                    {
                        Console.WriteLine($"{f.Code}: {f.Message}");
                    }
                }

                // There still might be some definitions to modify in this result
                buildDefinitions.AddRange(buildDefinitionResult.Definitions.Where(buildDefinition => buildDefinition != null && buildDefinition.QueueStatus == DefinitionQueueStatus.Enabled));

            }
            return buildDefinitions;
        }

        private static bool IsFrameworkAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft") || assemblyName == "mscorlib";
        }
        #endregion

    }
}
