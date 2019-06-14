using System.Collections.Generic;
using Microsoft.TeamFoundation.Build.Client;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class BuildDefinitionInfo
    {
        private readonly IBuildDefinition _buildDefinition;
        public string Name => _buildDefinition.Name;

        public List<SolutionInfo> Solutions
        {
            get;
            
        }
        public TeamCollectionInfo TeamCollection
        {
            get;
            
        }
        public HashSet<string> ReferencedAssemblies
        {
            get;
            
        }
        public BuildDefinitionInfo(TeamCollectionInfo teamCollectionInfo, IBuildDefinition buildDefinition)
        {
            _buildDefinition = buildDefinition;
            TeamCollection = teamCollectionInfo;
            ReferencedAssemblies = new HashSet<string>();
            Solutions = new List<SolutionInfo>();
        }
        public override string ToString()
        {
            return $"'{Name}' ({Solutions.Count} solutions)";
        }
    }
}