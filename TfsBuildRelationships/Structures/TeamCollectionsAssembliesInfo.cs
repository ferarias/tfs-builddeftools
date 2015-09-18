using System;
using System.Collections.Generic;
using System.Text;

namespace TfsBuildRelationships.Structures
{
    /// <summary>
    /// Key = build definition name
    /// Value = Assemblies information for each solution
    /// </summary>
    public class TeamCollectionsAssembliesInfo : Dictionary<string, BuildDefinitionsAssembliesInfo>
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

        public Dictionary<string, AssembliesInfo> Flatten()
        {
            var dic = new Dictionary<string, AssembliesInfo>();
            foreach (var tcAssInfo in this)
            {
                foreach (var bdAss in tcAssInfo.Value)
                {
                    foreach (var slnAss in bdAss.Value)
                    {
                        if(dic.ContainsKey(slnAss.Key))
                        {
                            Console.WriteLine("Element already exists: {0}{1}{2}", tcAssInfo.Key, bdAss.Key, slnAss.Key);
                            continue;
                        }
                        dic.Add(slnAss.Key, slnAss.Value);
                    }
                }
            }
            return dic;
        }

        public DependencyGraph<string> GetSolutionDependencies()
        {
            var solutionDependencies = new DependencyGraph<string>();

            var all = Flatten();

            foreach (var a in all)
            {
                foreach(var reference in a.Value.ReferencedAssemblies)
                    foreach(var b in all)
                        if(b.Value.OwnAssemblies.Contains(reference))
                            solutionDependencies.AddDependency(a.Key, b.Key);
            }

            
            return solutionDependencies;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var teamCollectionAssembliesInfo in this)
            {
                Console.WriteLine("COLLECTION: {0}", teamCollectionAssembliesInfo.Key.ToUpper());
                foreach (var buildDefinitionAssembliesInfo in teamCollectionAssembliesInfo.Value)
                {
                    Console.WriteLine("\tBuild definition: '{0}'", buildDefinitionAssembliesInfo.Key);
                    foreach (var solutionsAssembliesInfo in buildDefinitionAssembliesInfo.Value)
                    {
                        Console.WriteLine("\t\tSolution: '{0}'", solutionsAssembliesInfo.Key);
                        Console.WriteLine("\t\t- Own: {0}", String.Join(",", solutionsAssembliesInfo.Value.OwnAssemblies));
                        Console.WriteLine("\t\t- Ref: {0}", String.Join(",", solutionsAssembliesInfo.Value.ReferencedAssemblies));
                    }
                }
            }
            return sb.ToString();
        }
    }
}
