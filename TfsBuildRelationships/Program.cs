using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TfsBuildRelationships.AssemblyInfo;


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
                    var sw = Stopwatch.StartNew();

                    var directoryName = Path.GetDirectoryName(options.OutFile);
                    if (directoryName == null) throw new ArgumentNullException(nameof(directoryName));
                    var logFileName = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(options.OutFile) + ".log");
                    var dotFileName = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(options.OutFile) + ".dot");
                    
                    // Read and process collections, build definitions, solutions, projects and assemblies
                    var parser = new TfsBuildsParser()
                                 {
                                     ExcludeBuildDefinitions = options.ExcludedBuildDefinitions,
                                     FilterTeamProjects = options.TeamProjects,
                                     Verbose = options.Verbose
                                 };
                    var assemblyData = parser.Process(options.TeamCollections);
                    Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to process all build definitions.");
                    

                    using (var outputFile = new StreamWriter(logFileName))
                    {
                        // Build a dependency graph based on the assembly data
                        if (options.Mode == "solution")
                        {
                            sw.Restart();
                            PrintAssemblyData(assemblyData, outputFile);
                            var graph = assemblyData.GetSolutionsDependencies();
                            var graphNodes = graph.Nodes;
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate solution dependencies.");

                            // Calculate start and end nodes
                            sw.Restart();
                            var startNodes = graphNodes.Where(x => !graphNodes.Any(y => graph.GetDependenciesForNode(y).Contains(x))).ToList();
                            var endNodes = graphNodes.Where(x => !graph.GetDependenciesForNode(x).Any()).ToList();
                            PrintStartAndEndNodes(startNodes, endNodes, outputFile);
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate start/end nodes.");

                            // Find circular references between solutions
                            sw.Restart();
                            var circularReferences = CircularReferencesHelper.FindCircularReferences(graph, startNodes, endNodes);
                            PrintCircularReferences(circularReferences, outputFile);
                            if (!circularReferences.Any())
                            {
                                var sortedNodes = graphNodes.ToList();
                                sortedNodes.Sort();
                                PrintBuildOrder(sortedNodes, outputFile);
                            }
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate circular references.");

                            //// Export dependencies graph
                            sw.Restart();
                            ExportDependencyGraph(dotFileName, graph, options.TransitiveReduction, circularReferences, options.GraphExtracommands);
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to export dependency graph.");

                        }
                        else if (options.Mode == "project")
                        {
                            sw.Restart();
                            PrintAssemblyData(assemblyData, outputFile);
                            var graph = assemblyData.GetProjectsDependencies();
                            var graphNodes = graph.Nodes;
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate solution dependencies.");

                            // Calculate start and end nodes
                            sw.Restart();
                            var startNodes = graphNodes.Where(x => !graphNodes.Any(y => graph.GetDependenciesForNode(y).Contains(x))).ToList();
                            var endNodes = graphNodes.Where(x => !graph.GetDependenciesForNode(x).Any()).ToList();
                            PrintStartAndEndNodes(startNodes, endNodes, outputFile);
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate start/end nodes.");

                            // Find circular references between solutions
                            sw.Restart();
                            var circularReferences = CircularReferencesHelper.FindCircularReferences(graph, startNodes, endNodes);
                            PrintCircularReferences(circularReferences, outputFile);

                            if (!circularReferences.Any())
                            {
                                var sortedNodes = graphNodes.ToList();
                                sortedNodes.Sort();
                                PrintBuildOrder(sortedNodes, outputFile);
                            }
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to calculate circular references.");

                            //// Export dependencies graph
                            sw.Restart();
                            ExportDependencyGraph(dotFileName, graph, options.TransitiveReduction, circularReferences, options.GraphExtracommands);
                            Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to export dependency graph.");
                        }
                        else
                        {
                            PrintList(assemblyData, outputFile);
                        }

                    }
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


        private static void ExportDependencyGraph<T>(string fileName, DependencyGraph<T> graph, bool transitiveReduction, List<List<T>> circularReferences, string graphExtraCommands) where T : IGraphNode, IComparable
        {
            try
            {
                if (!circularReferences.Any() && transitiveReduction)
                    graph.TransitiveReduction();
                var dotCommandBuilder = new DotCommandBuilder<T>();
                var dotCommand = dotCommandBuilder.GenerateDotCommand(graph, circularReferences, graphExtraCommands);
                File.WriteAllText(fileName, dotCommand, Encoding.ASCII);

                Console.WriteLine("Graph exported to '{0}'", Path.GetFullPath(fileName));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while exporting graph: {0}", ex.Message);
            }
        }

        private static void PrintCircularReferences<T>(List<List<T>> circularReferences, StreamWriter outFile)
        {
            outFile.WriteLine("CIRCULAR REFERENCES");
            outFile.WriteLine("===================");
            if (circularReferences.Count > 0)
            {
                outFile.WriteLine("Warning! Circular references found!");

                foreach (var circularReference in circularReferences)
                {
                    foreach (var item in circularReference)
                    {
                        outFile.Write("{0} ->", item);
                    }
                    outFile.WriteLine("¶");
                }

            }
            else
            {
                outFile.WriteLine("No circular references found.");
            }
            outFile.WriteLine("*************");
            outFile.WriteLine();
        }

        private static void PrintStartAndEndNodes<T>(IEnumerable<T> startNodes, IEnumerable<T> endNodes, StreamWriter outFile)
        {
            outFile.WriteLine("START NODES");
            outFile.WriteLine("===========");
            outFile.WriteLine("Nodes nobody depends upon (highest-level assemblies)");
            foreach (var node in startNodes)
                outFile.WriteLine("\t{0}", node);
            outFile.WriteLine();
            outFile.WriteLine("FINAL NODES");
            outFile.WriteLine("===========");
            outFile.WriteLine("Nodes with no dependencies (low-level assemblies)");
            foreach (var node in endNodes)
                outFile.WriteLine("\t{0}", node);
            outFile.WriteLine("*************");
            outFile.WriteLine();
        }

        private static void PrintAssemblyData(AssembliesInfo assemblyData, StreamWriter outFile)
        {
            outFile.WriteLine("ASSEMBLY DATA");
            outFile.WriteLine("=============");
            foreach (var data in assemblyData.TeamCollections)
            {
                outFile.WriteLine(data);
                foreach (var buildDefinitionInfo in data.BuildDefinitions)
                {
                    outFile.WriteLine(buildDefinitionInfo);
                    outFile.WriteLine("{");
                    foreach (var solutionData in buildDefinitionInfo.Solutions)
                    {
                        outFile.WriteLine("\t" + solutionData);
                        outFile.WriteLine("\t{");
                        foreach (var projectData in solutionData.Projects)
                        {
                            outFile.WriteLine("\t\t" + projectData);
                            outFile.WriteLine("\t\t{");
                            foreach (var referencedAssemblies in projectData.ReferencedAssemblies)
                            {
                                outFile.WriteLine("\t\t\t" + referencedAssemblies);
                            }
                            outFile.WriteLine("\t\t}");
                        }
                        outFile.WriteLine("\t}");
                    }
                    outFile.WriteLine("}");
                }
            }
            outFile.WriteLine("*************");
            outFile.WriteLine();
        }

        private static void PrintList(AssembliesInfo assemblyData, StreamWriter outFile)
        {
            outFile.WriteLine("Collection;BuildDefinition;Solution;Project;Guid;DependentProjects;ReferencedAssemblies");
            foreach (var collection in assemblyData.TeamCollections)
            {
                foreach (var buildDefinitionInfo in collection.BuildDefinitions)
                {
                    foreach (var solutionData in buildDefinitionInfo.Solutions)
                    {
                        foreach (var projectData in solutionData.Projects)
                        {
                            outFile.WriteLine($"{collection.Name};{buildDefinitionInfo.Name};{solutionData.Name};{projectData.GeneratedAssembly};{projectData.ProjectGuid};{projectData.DependentProjects.Count};{projectData.ReferencedAssemblies.Count}");
                        }

                    }

                }
            }

        }


        private static void PrintBuildOrder<T>(IEnumerable<T> nodes, StreamWriter outFile)
        {
            outFile.WriteLine("BUILD ORDER");
            outFile.WriteLine("===========");
            int i = 0;
            foreach (var node in nodes)
                outFile.WriteLine("{0}. {1}", ++i, node);
            outFile.WriteLine("*************");
            outFile.WriteLine();
        }


    }
}
