using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    public class ProjectAssembliesInfo
    {
        /// <summary>
        /// Own assemblies
        /// </summary>
        public string GeneratedAssembly { get; set; }

        /// <summary>
        /// Referenced Assemblies
        /// </summary>
        public HashSet<string> ReferencedAssemblies { get; set; }

        public ProjectAssembliesInfo()
        {
            ReferencedAssemblies = new HashSet<string>();
        }

        public void MergeWith(ProjectAssembliesInfo other)
        {
            ReferencedAssemblies.UnionWith(other.ReferencedAssemblies);
        }

    }
}
