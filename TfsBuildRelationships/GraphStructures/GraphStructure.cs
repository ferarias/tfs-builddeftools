using System.Collections.Generic;
using System.Linq;

namespace TfsBuildRelationships.GraphStructures
{
    public class GraphStructure
    {
        public IDictionary<int, GraphNode> Nodes { get; }

        public IDictionary<KeyValuePair<int, int>, GraphEdge> Relationships { get; }


        public GraphStructure()
        {
            Nodes = new Dictionary<int, GraphNode>();
            Relationships = new Dictionary<KeyValuePair<int, int>, GraphEdge>();
            ResetProcessed();
        }

        public void ResetProcessed()
        {
            Nodes.Values.AsParallel().ForAll(x => x.Processed = false);
        }

        public bool IsNodeProcessed(int i)
        {
            return Nodes[i].Processed;
        }
    }

    public class GraphEdge
    {

        public string Attributes { get; set; }
    }
}
