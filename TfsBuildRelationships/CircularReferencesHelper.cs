using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    public class CircularReferencesHelper
    {
        public static List<List<string>>
            FindCircularReferences(DependencyGraph<string> dependencies, IEnumerable<string> startNodes, IEnumerable<string> endNodes)
        {
            var processed = new HashSet<string>(endNodes);
            var processing = new Queue<string>();
            var circularReferences = new List<List<string>>();

            if (startNodes.Count() == 0 && endNodes.Count() == 0 && dependencies.GetNodes().Count() > 0)
            {
                // Probably, it's all a big cycle. Let's start with one
                startNodes = new List<string>() { dependencies.GetNodes().First() };
            }

            foreach (var node in startNodes)
                FindCircularReferencesForNode(node, dependencies, ref processing, ref processed, ref circularReferences);

            foreach(var referencesList in circularReferences)
            {
                var lastItem = referencesList.Last();
                var firstIndex = referencesList.FindIndex(x => x == lastItem);
                referencesList.RemoveRange(0, firstIndex + 1);
            }

            return circularReferences;
        }

        public static void FindCircularReferencesForNode(string node, DependencyGraph<string> dependencies, ref Queue<string> processing, ref HashSet<string> processed, ref List<List<string>> circularReferences)
        {
            processing.Enqueue(node);
            foreach (var subnode in dependencies.GetDependenciesForNode(node))
            {
                if (processing.Contains(subnode))
                {
                    var circularReference = new List<string>(processing);
                    circularReference.Add(subnode);
                    circularReferences.Add(circularReference);
                }
                else
                {
                    if (!processed.Contains(subnode))
                        FindCircularReferencesForNode(subnode, dependencies, ref processing, ref processed, ref circularReferences);
                }
            }
            processed.Add(processing.Dequeue());
        }
    }
}
