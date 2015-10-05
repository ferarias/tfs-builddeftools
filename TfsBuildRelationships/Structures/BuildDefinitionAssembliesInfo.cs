using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    /// <summary>
    /// Key = Solution Name
    /// Value = Own and referenced assemblies for the solution
    /// </summary>
    public class BuildDefinitionAssembliesInfo : Dictionary<string, SolutionAssembliesInfo>
    {
        public HashSet<string> OwnAssemblies()
        {
            var set = new HashSet<string>();
            foreach (var assemblyInfo in this)
            {
                set.UnionWith(assemblyInfo.Value.GeneratedAssemblies);
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
