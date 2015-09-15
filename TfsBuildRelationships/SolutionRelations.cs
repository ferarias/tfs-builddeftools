using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    public class SolutionRelations
    {
        /// <summary>
        /// Own assemblies
        /// </summary>
        public HashSet<string> OwnAssemblies { get; set; }

        /// <summary>
        /// Referenced Assemblies
        /// </summary>
        public HashSet<string> ReferencedAssemblies { get; set; }

        public SolutionRelations()
        {
            OwnAssemblies = new HashSet<string>();
            ReferencedAssemblies = new HashSet<string>();
        }
    }
}
