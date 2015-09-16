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
            commandText.Append("digraph G {\r\n");

            // handle extra commands
            if (extraCommands.Trim().Length > 0)
            {
                commandText.Append("    ");
                commandText.Append(extraCommands.Trim());
                commandText.Append("\r\n");
            }

            var nodeLabels = new StringBuilder();

            foreach (var dependant in nodes)
            {
                var dependantId = idsByNameMap[dependant];

                // 1 [label="SampleProject",shape=circle,hight=0.12,width=0.12,fontsize=1];
                nodeLabels.AppendFormat("    {0} [shape=box,label=\"{1}\"];\r\n", dependantId, dependant);

                foreach (var dependency in graph.GetDependenciesForNode(dependant))
                {
                    var dependencyId = idsByNameMap[dependency];
                    commandText.AppendFormat("    {0} -> {1};\r\n", dependantId, dependencyId);
                }
            }

            commandText.Append(nodeLabels.ToString());
            commandText.Append("}");

            return commandText.ToString();
        }
    }
}
