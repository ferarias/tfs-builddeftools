using CommandLine;
using Microsoft.Build.Evaluation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TfsBuildDefinitionsCommon;
using TfsBuildRelationships.Structures;


namespace TfsBuildRelationships
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    // Read and process collections, build definitions, solutions, projects and assemblies
                    var parser = new TfsBuildsParser();
                    var assemblyData = parser.Process(options.TeamCollections, options.ExcludedBuildDefinitions, options.Verbose);
                    PrintAssemblyData(options, assemblyData);

                    var graph = assemblyData.GetSolutionDependencies();
                    var graphNodes = graph.GetNodes();

                    // Calculate start and end nodes
                    var startNodes = graphNodes.Where(x => !graphNodes.Any(y => graph.GetDependenciesForNode(y).Contains(x)));
                    var endNodes = graphNodes.Where(x => graph.GetDependenciesForNode(x).Count() == 0);
                    PrintStartAndEndNodes(startNodes, endNodes);

                    // Find circular references between solutions
                    var circularReferences = CircularReferencesHelper.FindCircularReferences(graph, startNodes, endNodes);
                    PrintCircularReferences(circularReferences);

                    // Export dependencies graph
                    ExportDependencyGraph(options, graph, circularReferences);

                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("An error occured:");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Couldn't read options");
                Console.WriteLine();
            }

        }

        private static void ExportDependencyGraph(Options options, DependencyGraph<string> graph, List<List<string>> circularReferences)
        {
            try
            {
                if (circularReferences.Count() == 0 && options.TransitiveReduction)
                    graph.TransitiveReduction();
                var dotCommandBuilder = new DotCommandBuilder();
                dotCommandBuilder.ProcessLabel = new Func<string, string>(x => RenameLabel(x));
                var dotCommand = dotCommandBuilder.GenerateDotCommand(graph, circularReferences, options.GraphExtracommands);
                File.WriteAllText(options.OutFile, dotCommand, Encoding.ASCII);
                Console.WriteLine("Graph exported to '{0}'", System.IO.Path.GetFullPath(options.OutFile));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while exporting graph: {0}", ex.Message);
            }
        }

        private static void PrintCircularReferences(List<List<string>> circularReferences)
        {
            if (circularReferences.Count > 0)
            {
                Console.WriteLine("Warning! Circular references found!");
                foreach (var circularReference in circularReferences)
                    Console.WriteLine(String.Join("->", circularReference));
            }
            else
            {
                Console.WriteLine("No circular references found between solutions");
            }
        }

        private static void PrintStartAndEndNodes(IEnumerable<string> startNodes, IEnumerable<string> endNodes)
        {
            Console.WriteLine("*** START NODES -- Nodes nobody depends upon (highest-level assemblies)");
            foreach (var node in startNodes)
                Console.WriteLine(node);
            Console.WriteLine();
            Console.WriteLine("*** FINAL NODES -- Nodes with no dependencies (low-level assemblies)");
            foreach (var node in endNodes)
                Console.WriteLine(node);
            Console.WriteLine();
        }

        private static void PrintAssemblyData(Options options, TeamCollectionsAssembliesInfo assemblyData)
        {
            if (options.Verbose)
                Console.WriteLine(assemblyData);
            else
                Console.WriteLine("{0} assemblies", assemblyData.OwnAssemblies().Count);
        }

        /// <summary>
        /// This method transforms a solution (.sln) TFS path into a more human-readable
        /// label suitable for a graph
        /// </summary>
        /// <param name="solutionRoute"></param>
        /// <returns>Transformed label</returns>
        private static string RenameLabel(string solutionRoute)
        {
            string strRegex = @"\$/([^/]+)/([^/]*)/(.*/)*(.*).sln";
            Regex myRegex = new Regex(strRegex, RegexOptions.None);
            var pieces = myRegex.Split(solutionRoute);
            var sb = new StringBuilder();
            sb.Append(pieces[1]); sb.Append("\\n");
            sb.Append(pieces[pieces.Length - 2]);

            return sb.ToString();
        }

        

    }
}
