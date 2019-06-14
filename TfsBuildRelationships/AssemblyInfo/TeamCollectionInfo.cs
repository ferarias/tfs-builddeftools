using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class TeamCollectionInfo
    {
        public readonly TfsTeamProjectCollection Collection;
        public string Name => Collection.Name;

        public List<BuildDefinitionInfo> BuildDefinitions
        {
            get;
        }
        public TeamCollectionInfo(TfsTeamProjectCollection collection)
        {
            Collection = collection;
            BuildDefinitions = new List<BuildDefinitionInfo>();
        }
        public override string ToString()
        {
            return $"'{Name}' ({BuildDefinitions.Count} build definitions)";
        }
    }
}