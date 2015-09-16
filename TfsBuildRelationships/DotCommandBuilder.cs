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
        public string GenerateDotCommand(DependencyGraph<string> graph)
        {
            return GenerateDotCommand(graph, string.Empty);
        }

        public string GenerateDotCommand(DependencyGraph<string> graph, string extraCommands)
        {
            var nodes = graph.GetNodes();

            // TODO can this first loop be replaced with LINQ, maybe with a zip?
            var idsByNameMap = new Dictionary<string, int>();
            var id = 1;
            foreach (var nodeName in nodes)
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

            var nodeLabels = new StringBuilder();

            foreach (var dependant in nodes)
            {
                var dependantId = idsByNameMap[dependant];
                // 1 [label="SampleProject",shape=circle,hight=0.12,width=0.12,fontsize=1];
                nodeLabels.AppendFormat("\t{0} [shape=box,fontsize=8,label=\"{1}\"];\r\n", dependantId, dependant);
                commandText.AppendFormat("\t{0} -> {{ {1} }} ", dependantId, String.Join(";", graph.GetDependenciesForNode(dependant).Select(x=>idsByNameMap[x])));
                commandText.AppendLine();
            }
            commandText.Append(nodeLabels.ToString());
            commandText.AppendLine("}");

            return commandText.ToString();
        }
    }
}
