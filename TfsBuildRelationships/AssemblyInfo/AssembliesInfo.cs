using System.Collections.Generic;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class AssembliesInfo
    {
        public List<TeamCollectionInfo> TeamCollections { get; } = new List<TeamCollectionInfo>();
        
        public HashSet<string> GetAllAssemblies()
        {
            var hashSet = new HashSet<string>();
            foreach (var teamCollection in TeamCollections)
            {
                foreach (var buildDefinition in teamCollection.BuildDefinitions)
                {
                    foreach (var solution in buildDefinition.Solutions)
                    {
                        foreach (var project in solution.Projects)
                        {
                            hashSet.Add(project.GeneratedAssembly);
                        }
                    }
                }
            }
            return hashSet;
        }
        internal DependencyGraph<SolutionInfo> GetSolutionsDependencies()
        {
            var dependencyGraph = new DependencyGraph<SolutionInfo>();
            foreach (var teamCollection in TeamCollections)
            {
                foreach (var buildDefinition in teamCollection.BuildDefinitions)
                {
                    foreach (var solution in buildDefinition.Solutions)
                    {
                        dependencyGraph.Nodes.Add(solution);
                        solution.DependentSolutions = new List<SolutionInfo>();
                        foreach (var project in solution.Projects)
                        {
                            foreach (var otherCollection in TeamCollections)
                            {
                                foreach (var otherBuildDefinition in otherCollection.BuildDefinitions)
                                {
                                    foreach (var otherSolution in otherBuildDefinition.Solutions)
                                    {
                                        if (otherSolution.ReferencedAssemblies.Contains(project.GeneratedAssembly))
                                        {
                                            dependencyGraph.AddDependency(otherSolution, solution);
                                            otherSolution.DependentSolutions.Add(solution);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dependencyGraph;
        }
        internal DependencyGraph<ProjectInfo> GetProjectsDependencies()
        {
            var dependencyGraph = new DependencyGraph<ProjectInfo>();
            foreach (var teamCollection in TeamCollections)
            {
                foreach (var buildDefinition in teamCollection.BuildDefinitions)
                {
                    foreach (var solution in buildDefinition.Solutions)
                    {
                        foreach (var project in solution.Projects)
                        {
                            dependencyGraph.Nodes.Add(project);
                            project.DependentProjects = new List<ProjectInfo>();
                            foreach (var otherTeamCollection in TeamCollections)
                            {
                                foreach (var otherBuildDefinition in otherTeamCollection.BuildDefinitions)
                                {
                                    foreach (var otherSolution in otherBuildDefinition.Solutions)
                                    {
                                        foreach (var otherProject in otherSolution.Projects)
                                        {
                                            if (otherProject.ReferencedAssemblies.Contains(project.GeneratedAssembly))
                                            {
                                                dependencyGraph.AddDependency(otherProject, project);
                                                otherProject.DependentProjects.Add(project);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dependencyGraph;
        }
    }
}