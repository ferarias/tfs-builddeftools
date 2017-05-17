using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TfsBuildRelationships.GraphStructures;

namespace TfsBuildRelationships
{
    /// <summary>
    /// Builds a Dot command for a given dependency graph.
    /// </summary>
    public sealed class DotCommandBuilder<T> where T : TfsBuildRelationships.AssemblyInfo.IGraphNode, IComparable
    {
        /// <summary>
        /// This dictionary maps nodes to an integer so the DOT file is more compact. Every node is a simple number.
        /// </summary>
        private Dictionary<T, int> NodeIdMap { get; set; }

        public string GenerateDotCommand(DependencyGraph<T> graph, List<List<T>> circularReferences, string extraCommands)
        {
            // Init
            NodeIdMap = new Dictionary<T, int>();
            var id = 0;
            foreach (var nodeName in graph.Nodes)
            {
                NodeIdMap.Add(nodeName, id++);
            }


            var graphStructure = PrepareGraph(graph, circularReferences);


            var commandText = new StringBuilder();
            commandText.AppendLine("digraph G {");

            // handle extra commands
            if (!String.IsNullOrWhiteSpace(extraCommands))
            {
                commandText.AppendFormat("\t{0}", extraCommands.Trim());
                commandText.AppendLine();
            }

            // Ranks
            commandText.AppendLine("\t// Node ranks");
            for (int i = 0; i <= graphStructure.Nodes.Max(ni => ni.Value.Level); i++)
            {
                commandText.AppendFormat("\t{{ rank = same; {0} }}\r\n", String.Join(";", graphStructure.Nodes.Where(x => x.Value.Level == i).Select(y => y.Key)));
            }
            commandText.AppendLine();

            // Relationships
            commandText.AppendLine("\t// Node relationship");
            foreach (var relationship in graphStructure.Relationships)
            {
                var attributes = relationship.Value.Attributes;
                commandText.AppendFormat("\t{0} -> {1} [{2}];\r\n", relationship.Key.Key, relationship.Key.Value, attributes);
            }
            commandText.AppendLine();

            // Nodes
            commandText.AppendLine("\t// Node labels");
            foreach (var node in graphStructure.Nodes)
            {
                var attributes = node.Value.Attributes;
                commandText.AppendFormat("\t{0} [{1},label=\"{0}\\n{2}\"];\r\n", node.Key, attributes, node.Value.Label);
            }
            commandText.AppendLine();

            commandText.AppendLine("}");

            return commandText.ToString();

        }

        private GraphStructure PrepareGraph(DependencyGraph<T> graph, List<List<T>> circularReferences)
        {
            var startNodes = graph.Nodes.Where(x => !graph.Nodes.Any(y => graph.GetDependenciesForNode(y).Contains(x))).Select(z => NodeIdMap[z]);
            var endNodes = graph.Nodes.Where(x => graph.GetDependenciesForNode(x).Count() == 0).Select(z => NodeIdMap[z]);

            var circularReferenceInvolvedNodes = new List<int>();
            foreach (var circularReference in circularReferences)
                foreach (var reference in circularReference)
                    circularReferenceInvolvedNodes.Add(NodeIdMap[reference]);


            var graphStructure = new GraphStructure();
            var processing = new Queue<GraphNode>();

            if (startNodes.Count() == 0 && endNodes.Count() == 0 && graph.Nodes.Count() > 0)
                // Probably, it's all a big cycle. Let's start with any one
                startNodes = new List<int>() { NodeIdMap[graph.Nodes.First()] };

            // Start from each start node, giving them a level of 0
            foreach (int nodeId in startNodes)
                processing.Enqueue(new GraphNode() { Id = nodeId, Label = graph.Nodes.First(x => NodeIdMap[x] == nodeId).GetLabel(), Level = 0, Processed = false });

            // While the stack is not empty
            while (processing.Any())
            {
                var sourceGraphNode = processing.Peek();
                var isCircularReferenceSourceNode = circularReferenceInvolvedNodes.Contains(sourceGraphNode.Id);
                sourceGraphNode.Attributes = GetNodeStyle(sourceGraphNode, isCircularReferenceSourceNode, startNodes.Contains(sourceGraphNode.Id), endNodes.Contains(sourceGraphNode.Id));
                graphStructure.Nodes.Add(sourceGraphNode.Id, sourceGraphNode);

                var dependencyIds = graph.GetDependenciesForNode(graph.Nodes.First(gn => NodeIdMap[gn] == sourceGraphNode.Id)).Select(z=>NodeIdMap[z]);
                foreach (var destId in dependencyIds)
                {
                    GraphNode destGraphNode;
                    if (graphStructure.Nodes.ContainsKey(destId))
                        destGraphNode = graphStructure.Nodes[destId];
                    else if (processing.Any(n=>n.Id == destId))
                        destGraphNode = processing.First(n=>n.Id == destId);
                    else
                        destGraphNode = new GraphNode() { Id = destId, Label = graph.Nodes.First(x => NodeIdMap[x] == destId).GetLabel(), Level = sourceGraphNode.Level + 1, Processed = false };
                    var isCircularReferenceDestNode = circularReferenceInvolvedNodes.Contains(destGraphNode.Id);
                    var relationship = new KeyValuePair<int, int>(sourceGraphNode.Id, destGraphNode.Id);
                    graphStructure.Relationships.Add(relationship, new GraphEdge() { Attributes = GetEdgeStyle(relationship, isCircularReferenceSourceNode && isCircularReferenceDestNode) });

                    if (destGraphNode.Level < sourceGraphNode.Level + 1) destGraphNode.Level = sourceGraphNode.Level + 1;

                    if (!destGraphNode.Processed && !processing.Contains(destGraphNode))
                    {
                        processing.Enqueue(destGraphNode);
                    }
                    // this subnode should be downgraded to be below current node
                    else if (graphStructure.Nodes.Any(n => n.Key == destGraphNode.Id && !n.Value.Processed))
                        graphStructure.Nodes[destGraphNode.Id].Level = destGraphNode.Level;
                }
                var nd = processing.Dequeue();
                nd.Processed = true;
            }
            return graphStructure;
        }




        private string GetNodeStyle(GraphNode node, bool isCircularRefNode, bool isSource, bool isSink)
        {
            var style = new StringBuilder();
            if (isSource || isSink)
                style.Append("shape=ellipse");
            else
                style.Append("shape=box");
            if (isCircularRefNode)
                style.Append(",color=\"#ff00005f\",style=filled");
            else
                if (isSource)
                    style.Append(",color=\"#0000ff5f\",style=filled");
                else if (isSink)
                    style.Append(",color=green,style=filled");

            return style.ToString();
        }

        private string GetEdgeStyle(KeyValuePair<int, int> edge, bool isCircularRefEdge)
        {
            var style = new StringBuilder();
            if (isCircularRefEdge)
                style.Append("color=\"#ff00005f\",style=bold");
            else
                style.Append("color=\"#3333335f\"");
            return style.ToString();
        }

    }
}
