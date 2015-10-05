using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships.GraphStructures
{
    public class GraphStructure
    {
        public IDictionary<int, GraphNode> Nodes { get; set; }

        public IDictionary<KeyValuePair<int, int>, GraphEdge> Relationships { get; set; }


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


    public class GraphNode
    {

        public int Id { get; set; }

        public string Label { get; set; }

        public bool Processed { get; set; }

        public int Level { get; set; }

        public string Attributes { get; set; }
    }

    public class GraphEdge
    {

        public string Attributes { get; set; }
    }
}
