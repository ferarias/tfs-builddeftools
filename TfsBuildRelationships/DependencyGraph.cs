using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    /// <summary>
    /// Models a directed graph, intended to represent a dependency hierarchy.
    /// </summary>
    public sealed class DependencyGraph<T>
    {
        public DependencyGraph()
        {
            Nodes = new HashSet<T>();
        }
        /// <summary>
        /// Gets all nodes
        /// </summary>
        /// <returns>Enumeration of nodes</returns>
        public HashSet<T> Nodes { get; set; }

        private readonly Dictionary<T, HashSet<T>> _dependenciesByNode = new Dictionary<T, HashSet<T>>();

        /// <summary>
        /// Adds a dependency between two nodes
        /// </summary>
        /// <param name="dependant">Dependant node</param>
        /// <param name="dependency">Dependency</param>
        public void AddDependency(T dependant, T dependency)
        {
            // take note of these nodes
            Nodes.Add(dependant);
            Nodes.Add(dependency);

            // get the list of dependencies for this dependant (create if it doesn't exist yet)
            HashSet<T> dependencySet;
            if (!_dependenciesByNode.TryGetValue(dependant, out dependencySet))
                dependencySet = _dependenciesByNode[dependant] = new HashSet<T>();

            dependencySet.Add(dependency);
        }

        
        

        /// <summary>
        /// Gets all the dependencies for a node
        /// </summary>
        /// <param name="dependant"></param>
        /// <returns></returns>
        public IEnumerable<T> GetDependenciesForNode(T dependant)
        {
            HashSet<T> dependencyList;
            return _dependenciesByNode.TryGetValue(dependant, out dependencyList)
                       ? dependencyList
                       : Enumerable.Empty<T>();
        }

        /// <summary>
        /// Reduces a graph to its transitive closure
        /// E.g.: 1-> 2; 1->3; 2->3 becomes 1->2; 2->3 (1->3 is removed because 1->2 and 2->3 so it's "redundant")
        /// This is useful to determine ordering of nodes based on precedence
        /// </summary>
        public void TransitiveReduction()
        {
            foreach (var x in Nodes)
            {
                foreach (var y in Nodes)
                {
                    foreach (var z in Nodes)
                    {
                        if (GetDependenciesForNode(x).Contains(y) && GetDependenciesForNode(y).Contains(z))
                            _dependenciesByNode[x].Remove(z);
                    }
                }
            }

        }


    }
}
