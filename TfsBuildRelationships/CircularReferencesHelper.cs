using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    /// <summary>
    /// A class to help in determining graph cycles (i.e.: circular references)
    /// for a <see cref="TfsBuildRelationships.DependencyGraph"/>
    /// </summary>
    public class CircularReferencesHelper
    {
        /// <summary>
        /// Given a graph, obtain its 
        /// </summary>
        /// <param name="dependencies">An object representing relationships between objects</param>
        /// <param name="startNodes">A list of start nodes</param>
        /// <param name="endNodes">A list of final nodes</param>
        /// <returns></returns>
        public static List<List<string>> FindCircularReferences(DependencyGraph<string> dependencies, IEnumerable<string> startNodes, IEnumerable<string> endNodes)
        {
            var processed = new HashSet<string>(endNodes);
            var processing = new Stack<string>();
            var circularReferences = new List<List<string>>();

            if (startNodes.Count() == 0 && endNodes.Count() == 0 && dependencies.GetNodes().Count() > 0)
            {
                // Probably, it's all a big cycle. Let's start with one
                startNodes = new List<string>() { dependencies.GetNodes().First() };
            }

            // Start the search from each start node
            foreach (var node in startNodes)
                FindCircularReferencesForNode(node, dependencies, ref processing, ref processed, ref circularReferences);

            // Remove items into circular references that don't belong to the cycle
            // E.g.: 1->2->3->2 becomes 2->3
            foreach (var referencesList in circularReferences)
            {
                var lastItem = referencesList.Last();
                var firstIndex = referencesList.FindIndex(x => x == lastItem);
                referencesList.RemoveRange(0, firstIndex + 1);
            }

            return circularReferences;
        }

        /// <summary>
        /// This method is called recursively in a graph to locate dependencies
        /// It makes use of a stack and a list of already-processed items
        /// </summary>
        /// <param name="node">Current node</param>
        /// <param name="dependencies">Original dependencies graph</param>
        /// <param name="processing">Stack of items currently traversed</param>
        /// <param name="processed">List of elements already processed</param>
        /// <param name="circularReferences">The list of circular references found until now.</param>
        public static void FindCircularReferencesForNode(string node, DependencyGraph<string> dependencies, ref Stack<string> processing, ref HashSet<string> processed, ref List<List<string>> circularReferences)
        {
            // Add the node to the stack
            processing.Push(node);
            foreach (var subnode in dependencies.GetDependenciesForNode(node))
            {
                if (processing.Contains(subnode))
                {
                    // the subnode is into the stack: we found a cycle!
                    // Unroll the stack into a list and append the current subnode. 
                    var circularReference = new List<string>(processing);
                    circularReference.Reverse();
                    circularReference.Add(subnode);
                    // Add this to the list of circular references
                    circularReferences.Add(circularReference);
                }
                else
                {
                    // unless it's already processed, find the circular references for this node (recursion)
                    if (!processed.Contains(subnode))
                        FindCircularReferencesForNode(subnode, dependencies, ref processing, ref processed, ref circularReferences);
                }
            }
            // Pop the node and move it to the bag of processed items
            processed.Add(processing.Pop());
        }
    }
}
