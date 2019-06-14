namespace TfsBuildRelationships.GraphStructures
{
    public class GraphNode
    {

        public int Id { get; set; }

        public string Label { get; set; }

        public bool Processed { get; set; }

        public int Level { get; set; }

        public string Attributes { get; set; }
    }
}