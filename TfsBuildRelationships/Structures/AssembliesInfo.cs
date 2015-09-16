using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    public class AssembliesInfo
    {
        /// <summary>
        /// Own assemblies
        /// </summary>
        public HashSet<string> OwnAssemblies { get; set; }

        /// <summary>
        /// Referenced Assemblies
        /// </summary>
        public HashSet<string> ReferencedAssemblies { get; set; }

        public AssembliesInfo()
        {
            OwnAssemblies = new HashSet<string>();
            ReferencedAssemblies = new HashSet<string>();
        }

        public void MergeWith(AssembliesInfo other)
        {
            OwnAssemblies.UnionWith(other.OwnAssemblies);
            ReferencedAssemblies.UnionWith(other.ReferencedAssemblies);
        }

    }
}
