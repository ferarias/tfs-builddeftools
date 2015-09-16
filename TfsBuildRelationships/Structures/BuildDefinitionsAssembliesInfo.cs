using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    class BuildDefinitionsAssembliesInfo : Dictionary<string, SolutionsAssembliesInfo>
    {
        public HashSet<string> OwnAssemblies()
        {
            var set = new HashSet<string>();
            foreach (var assemblyInfo in this)
            {
                set.UnionWith(assemblyInfo.Value.OwnAssemblies());
            }
            return set;
        }

        public HashSet<string> ReferencedAssemblies()
        {
            var set = new HashSet<string>();
            foreach (var assemblyInfo in this)
            {
                set.UnionWith(assemblyInfo.Value.ReferencedAssemblies());
            }
            return set;
        }
    }
}
