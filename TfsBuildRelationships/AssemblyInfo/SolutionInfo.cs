using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class SolutionInfo : IComparable, IGraphNode
    {
        public string Name
        {
            get;
            set;
        }
        public List<ProjectInfo> Projects
        {
            get;
            set;
        }
        public ProjectInfo this[Guid projectGuid]
        {
            get
            {
                return this.Projects.FirstOrDefault((ProjectInfo x) => x.ProjectGuid == projectGuid);
            }
        }
        public HashSet<string> ReferencedAssemblies
        {
            get;
            set;
        }
        public BuildDefinitionInfo BuildDefinition
        {
            get;
            set;
        }
        public List<SolutionInfo> DependentSolutions
        {
            get;
            set;
        }
        public SolutionInfo(BuildDefinitionInfo buildDefinitionInfo, string name)
        {
            this.Name = name;
            this.ReferencedAssemblies = new HashSet<string>();
            this.BuildDefinition = buildDefinitionInfo;
            this.DependentSolutions = new List<SolutionInfo>();
            this.Projects = new List<ProjectInfo>();
        }
        public override string ToString()
        {
            return string.Format("[{0} ({1} prjs)]", this.Name, this.Projects.Count<ProjectInfo>());
        }
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            SolutionInfo solutionInfo = obj as SolutionInfo;
            if (solutionInfo == null)
            {
                throw new ArgumentException("Object is not a SolutionInfo");
            }
            if (this.Name == solutionInfo.Name)
            {
                return 0;
            }
            if (!this.DependentSolutions.Contains(solutionInfo))
            {
                return 1;
            }
            if (solutionInfo.DependentSolutions.Contains(this))
            {
                return 0;
            }
            return -1;
        }
        public string GetLabel()
        {
            return string.Format("{0}", this.Name);
        }
        private static string RenameLabel(string solutionRoute)
        {
            string pattern = "\\$/([^/]+)/([^/]*)/(.*/)*(.*).sln";
            Regex regex = new Regex(pattern, RegexOptions.None);
            string[] array = regex.Split(solutionRoute);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(array[1]);
            stringBuilder.Append("\\n");
            stringBuilder.Append(array[array.Length - 2]);
            return stringBuilder.ToString();
        }
    }
}