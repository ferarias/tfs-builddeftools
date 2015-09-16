using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    /// <summary>
    /// Key = Solution Name
    /// Value = Own and referenced assemblies for the solution
    /// </summary>
    class SolutionsAssembliesInfo : Dictionary<string, AssembliesInfo>
    {
        public HashSet<string> OwnAssemblies()
        {
            var set = new HashSet<string>();
            foreach (var assemblyInfo in this)
            {
                set.UnionWith(assemblyInfo.Value.OwnAssemblies);
            }
            return set;
        }

        public HashSet<string> ReferencedAssemblies()
        {
            var set = new HashSet<string>();
            foreach(var assemblyInfo in this)
            {
                set.UnionWith(assemblyInfo.Value.ReferencedAssemblies);
            }
            return set;
        }

    }
}
