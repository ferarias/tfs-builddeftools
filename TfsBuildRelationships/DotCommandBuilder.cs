using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    /// <summary>
    /// Builds a Dot command for a given dependency graph.
    /// </summary>
    public sealed class DotCommandBuilder
    {
        /// <summary>
        /// Method that allows the transformation of labels for the graph
        /// </summary>
        public Func<string, string> ProcessLabel { get; set; }

        public string GenerateDotCommand(DependencyGraph<string> graph, List<List<string>> circularReferences, string extraCommands)
        {
            var allNodes = graph.GetNodes();
            var circularReferenceInvolvedNodes = new List<string>();
            foreach (var circularReference in circularReferences)
                circularReferenceInvolvedNodes.AddRange(circularReference);
            var startNodes = allNodes.Where(x => !allNodes.Any(y => graph.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => graph.GetDependenciesForNode(x).Count() == 0);
            var idsByNameMap = new Dictionary<string, int>();
            var id = 1;
            foreach (var nodeName in allNodes)
            {
                idsByNameMap.Add(nodeName, id);
                id++;
            }

            var commandText = new StringBuilder();
            commandText.AppendLine("digraph G {");

            // handle extra commands
            if (!String.IsNullOrWhiteSpace(extraCommands))
            {
                commandText.AppendFormat("\t{0}", extraCommands.Trim());
                commandText.AppendLine();
            }
            commandText.AppendLine("\t// Start nodes");
            commandText.AppendFormat("\t{{ rank = same; {0} }} ", String.Join(";", startNodes.Select(x => idsByNameMap[x])));
            commandText.AppendLine();
            commandText.AppendLine("\t// End nodes");
            commandText.AppendFormat("\t{{ rank = same; {0} }} ", String.Join(";", endNodes.Select(x => idsByNameMap[x])));
            commandText.AppendLine();

            commandText.AppendLine("\t// Node relationship");
            var nodeLabels = new StringBuilder();
            foreach (var node in allNodes)
            {
                var isCircularRefNode = circularReferenceInvolvedNodes.Contains(node);

                var style = new StringBuilder();
                if (startNodes.Contains(node) || endNodes.Contains(node))
                    style.Append("shape=ellipse");
                else
                    style.Append("shape=box");

                if (isCircularRefNode)
                    style.Append(",color=red,style=filled");
                else
                    if (startNodes.Contains(node))
                        style.Append(",color=lightblue,style=filled");
                    else if (endNodes.Contains(node))
                        style.Append(",color=green,style=filled");


                var nodeId = idsByNameMap[node];
                var nodeLabel = node;
                if (ProcessLabel != null)
                    nodeLabel = ProcessLabel(node);
                nodeLabels.AppendFormat("\t{0} [{1},label=\"{2}\"];\r\n", nodeId, style, nodeLabel);
                foreach (var dep in graph.GetDependenciesForNode(node))
                {
                    var isCircularRefDepNode = circularReferenceInvolvedNodes.Contains(dep);
                    string edgeAttributes;
                    if (isCircularRefNode && isCircularRefDepNode)
                        edgeAttributes = "color=red,style=bold";
                    else
                        edgeAttributes = "color=black";
                    commandText.AppendFormat("\t{0} -> {1} [{2}]; ", nodeId, idsByNameMap[dep], edgeAttributes);
                    commandText.AppendLine();
                } 
            }
            commandText.AppendLine("\t// Node labels");
            commandText.Append(nodeLabels.ToString());
            commandText.AppendLine("}");


            commandText.AppendLine("\t// Circular references");
            foreach (var circularReference in circularReferences)
            {
                commandText.Append("\t// ");
                foreach (var item in circularReference)
                {
                    commandText.AppendFormat("{0} ->", ProcessLabel(item));
                }
                commandText.AppendLine("...");
            }
            

            return commandText.ToString();
        }
    }
}
