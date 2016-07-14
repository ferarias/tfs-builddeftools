using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;

namespace TfsBuildRelationships.AssemblyInfo
{
    public class TeamCollectionInfo
    {
        public TfsTeamProjectCollection Collection;
        public string Name
        {
            get
            {
                return this.Collection.Name;
            }
        }
        public List<BuildDefinitionInfo> BuildDefinitions
        {
            get;
            set;
        }
        public TeamCollectionInfo(TfsTeamProjectCollection collection)
        {
            this.Collection = collection;
            this.BuildDefinitions = new List<BuildDefinitionInfo>();
        }
        public override string ToString()
        {
            return string.Format("'{0}' ({1} build definitions)", Name, BuildDefinitions.Count);
        }
    }
}