using System;
using System.Collections.Generic;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class ProjectInfo : IComparable, IGraphNode
    {
        public Guid ProjectGuid
        {
            get;
            set;
        }
        public SolutionInfo Solution
        {
            get;
            set;
        }
        public string GeneratedAssembly
        {
            get;
            set;
        }
        public HashSet<string> ReferencedAssemblies
        {
            get;
            set;
        }
        public HashSet<Guid> ReferencedProjects
        {
            get;
            set;
        }
        public List<ProjectInfo> DependentProjects
        {
            get;
            set;
        }
        public ProjectInfo(SolutionInfo solutionInfo)
        {
            ReferencedAssemblies = new HashSet<string>();
            ReferencedProjects = new HashSet<Guid>();
            DependentProjects = new List<ProjectInfo>();
            Solution = solutionInfo;
        }
        public override string ToString()
        {
            return $"[{GeneratedAssembly} ({ProjectGuid}) ({ReferencedAssemblies.Count} refs)]";
        }
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            var projectInfo = obj as ProjectInfo;
            if (projectInfo == null)
            {
                throw new ArgumentException("Object is not a ProjectInfo");
            }
            if (ProjectGuid == projectInfo.ProjectGuid)
            {
                return 0;
            }
            if (ReferencedProjects.Contains(projectInfo.ProjectGuid))
            {
                return -1;
            }
            if (projectInfo.ReferencedProjects.Contains(ProjectGuid))
            {
                return 0;
            }
            if (ReferencedAssemblies.Contains(projectInfo.GeneratedAssembly))
            {
                return -1;
            }
            if (projectInfo.ReferencedAssemblies.Contains(GeneratedAssembly))
            {
                return 0;
            }
            return 1;
        }
        public string GetLabel()
        {
            return $"{GeneratedAssembly}";
        }
    }
}