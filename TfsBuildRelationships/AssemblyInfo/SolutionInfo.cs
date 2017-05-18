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
        }
        public List<ProjectInfo> Projects
        {
            get;
        }
        public ProjectInfo this[Guid projectGuid]
        {
            get
            {
                return Projects.FirstOrDefault(x => x.ProjectGuid == projectGuid);
            }
        }
        public HashSet<string> ReferencedAssemblies
        {
            get;
            
        }
        public BuildDefinitionInfo BuildDefinition
        {
            get;
            
        }
        public List<SolutionInfo> DependentSolutions
        {
            get;
            set;
        }
        public SolutionInfo(BuildDefinitionInfo buildDefinitionInfo, string name)
        {
            Name = name;
            ReferencedAssemblies = new HashSet<string>();
            BuildDefinition = buildDefinitionInfo;
            DependentSolutions = new List<SolutionInfo>();
            Projects = new List<ProjectInfo>();
        }
        public override string ToString()
        {
            return $"[{Name} ({Projects.Count()} prjs)]";
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
            if (Name == solutionInfo.Name)
            {
                return 0;
            }
            if (!DependentSolutions.Contains(solutionInfo))
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
            return $"{Name}";
        }
        private static string RenameLabel(string solutionRoute)
        {
            var pattern = "\\$/([^/]+)/([^/]*)/(.*/)*(.*).sln";
            var regex = new Regex(pattern, RegexOptions.None);
            var array = regex.Split(solutionRoute);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(array[1]);
            stringBuilder.Append("\\n");
            stringBuilder.Append(array[array.Length - 2]);
            return stringBuilder.ToString();
        }
    }
}