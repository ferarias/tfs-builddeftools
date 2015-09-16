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

            //// TODO can this first loop be replaced with LINQ, maybe with a zip?
            //var idsByNameMap = new Dictionary<string, int>();
            //var id = 1;
            //foreach (var nodeName in nodes)
            //{
            //    idsByNameMap.Add(nodeName, id);
            //    id++;
            //}

            var commandText = new StringBuilder();
            commandText.AppendLine("digraph G {");

            // handle extra commands
            if (extraCommands.Trim().Length > 0)
            {
                commandText.Append("    ");
                commandText.Append(extraCommands.Trim());
                commandText.Append("\r\n");
            }

            foreach (var dependant in nodes)
            {
                commandText.AppendFormat(" \"{0}\" -> {{ ", dependant);
                foreach (var dependency in graph.GetDependenciesForNode(dependant))
                {
                    commandText.AppendFormat("\"{0}\";", dependency);
                }
                commandText.AppendFormat(" }}");
                commandText.AppendLine();
            }
            commandText.AppendLine("}}");

            return commandText.ToString();
        }
    }
}
