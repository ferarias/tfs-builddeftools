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
            this.ReferencedAssemblies = new HashSet<string>();
            this.ReferencedProjects = new HashSet<Guid>();
            this.DependentProjects = new List<ProjectInfo>();
            this.Solution = solutionInfo;
        }
        public override string ToString()
        {
            return string.Format("[{0} ({1}) ({2} refs)]", GeneratedAssembly, ProjectGuid, ReferencedAssemblies.Count);
        }
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            ProjectInfo projectInfo = obj as ProjectInfo;
            if (projectInfo == null)
            {
                throw new ArgumentException("Object is not a ProjectInfo");
            }
            if (this.ProjectGuid == projectInfo.ProjectGuid)
            {
                return 0;
            }
            if (this.ReferencedProjects.Contains(projectInfo.ProjectGuid))
            {
                return -1;
            }
            if (projectInfo.ReferencedProjects.Contains(this.ProjectGuid))
            {
                return 0;
            }
            if (this.ReferencedAssemblies.Contains(projectInfo.GeneratedAssembly))
            {
                return -1;
            }
            if (projectInfo.ReferencedAssemblies.Contains(this.GeneratedAssembly))
            {
                return 0;
            }
            return 1;
        }
        public string GetLabel()
        {
            return string.Format("{0}", this.GeneratedAssembly);
        }
    }
}