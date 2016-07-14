using System.Collections.Generic;
using TfsBuildRelationships.GraphStructures;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class AssembliesInfo
    {
        public List<TeamCollectionInfo> TeamCollections
        {
            get;
            set;
        }
        public AssembliesInfo()
        {
            this.TeamCollections = new List<TeamCollectionInfo>();
        }
        public HashSet<string> GetAllAssemblies()
        {
            HashSet<string> hashSet = new HashSet<string>();
            foreach (TeamCollectionInfo current in this.TeamCollections)
            {
                foreach (BuildDefinitionInfo current2 in current.BuildDefinitions)
                {
                    foreach (SolutionInfo current3 in current2.Solutions)
                    {
                        foreach (ProjectInfo current4 in current3.Projects)
                        {
                            hashSet.Add(current4.GeneratedAssembly);
                        }
                    }
                }
            }
            return hashSet;
        }
        internal DependencyGraph<SolutionInfo> GetSolutionsDependencies()
        {
            DependencyGraph<SolutionInfo> dependencyGraph = new DependencyGraph<SolutionInfo>();
            foreach (TeamCollectionInfo current in this.TeamCollections)
            {
                foreach (BuildDefinitionInfo current2 in current.BuildDefinitions)
                {
                    foreach (SolutionInfo current3 in current2.Solutions)
                    {
                        dependencyGraph.Nodes.Add(current3);
                        current3.DependentSolutions = new List<SolutionInfo>();
                        foreach (ProjectInfo current4 in current3.Projects)
                        {
                            foreach (TeamCollectionInfo current5 in this.TeamCollections)
                            {
                                foreach (BuildDefinitionInfo current6 in current5.BuildDefinitions)
                                {
                                    foreach (SolutionInfo current7 in current6.Solutions)
                                    {
                                        if (current7.ReferencedAssemblies.Contains(current4.GeneratedAssembly))
                                        {
                                            dependencyGraph.AddDependency(current7, current3);
                                            current7.DependentSolutions.Add(current3);
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
            DependencyGraph<ProjectInfo> dependencyGraph = new DependencyGraph<ProjectInfo>();
            foreach (TeamCollectionInfo current in this.TeamCollections)
            {
                foreach (BuildDefinitionInfo current2 in current.BuildDefinitions)
                {
                    foreach (SolutionInfo current3 in current2.Solutions)
                    {
                        foreach (ProjectInfo current4 in current3.Projects)
                        {
                            dependencyGraph.Nodes.Add(current4);
                            current4.DependentProjects = new List<ProjectInfo>();
                            foreach (TeamCollectionInfo current5 in this.TeamCollections)
                            {
                                foreach (BuildDefinitionInfo current6 in current5.BuildDefinitions)
                                {
                                    foreach (SolutionInfo current7 in current6.Solutions)
                                    {
                                        foreach (ProjectInfo current8 in current7.Projects)
                                        {
                                            if (current8.ReferencedAssemblies.Contains(current4.GeneratedAssembly))
                                            {
                                                dependencyGraph.AddDependency(current8, current4);
                                                current8.DependentProjects.Add(current4);
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