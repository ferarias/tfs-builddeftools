﻿using System.Collections.Generic;

namespace TfsBuildRelationships.Structures
{
    public class BuildDefinitionsAssembliesInfo : Dictionary<string, BuildDefinitionAssembliesInfo>
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
