using System.Collections.Generic;
using Microsoft.TeamFoundation.Build.Client;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class BuildDefinitionInfo
    {
        public IBuildDefinition BuildDefinition;
        public string Name
        {
            get
            {
                return this.BuildDefinition.Name;
            }
        }
        public List<SolutionInfo> Solutions
        {
            get;
            set;
        }
        public TeamCollectionInfo TeamCollection
        {
            get;
            set;
        }
        public HashSet<string> ReferencedAssemblies
        {
            get;
            set;
        }
        public BuildDefinitionInfo(TeamCollectionInfo teamCollectionInfo, IBuildDefinition buildDefinition)
        {
            this.BuildDefinition = buildDefinition;
            this.TeamCollection = teamCollectionInfo;
            this.ReferencedAssemblies = new HashSet<string>();
            this.Solutions = new List<SolutionInfo>();
        }
        public override string ToString()
        {
            return string.Format("'{0}' ({1} solutions)", Name, Solutions.Count);
        }
    }
}