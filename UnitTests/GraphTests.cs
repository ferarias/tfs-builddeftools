using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TfsBuildRelationships;
using TfsBuildRelationships.AssemblyInfo;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Build.Client;
using TfsBuildDefinitionsCommon;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class GraphTests
    {
        private DependencyGraph<ProjectInfo> Graph;

        [TestInitialize]
        public void Init()
        {
            var teamCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri("http://logpmtfs01v:8080/tfs/Logitravel"));
            var buildServer = teamCollection.GetService<IBuildServer>();
            var commonStructureService = teamCollection.GetService<Microsoft.TeamFoundation.Server.ICommonStructureService>();
            var buildDefinitionResults = Helpers.QueryBuildDefinitions(commonStructureService, buildServer, buildName: "");
            var buildDefinition = buildDefinitionResults.FirstOrDefault().Definitions.FirstOrDefault(bd => bd != null && bd.QueueStatus == DefinitionQueueStatus.Enabled);

            Graph = new DependencyGraph<ProjectInfo>();

            var ti = new TeamCollectionInfo(teamCollection);
            var bi = new BuildDefinitionInfo(ti, buildDefinition);
            var si = new SolutionInfo(bi, "S1");
            var pA = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "A", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pB = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "B", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pC = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "C", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pD = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "D", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pE = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "E", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pF = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "F", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pG = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "G", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };
            var pH = new ProjectInfo(si) { ProjectGuid = Guid.Empty, GeneratedAssembly = "H", ReferencedProjects = new System.Collections.Generic.HashSet<Guid>() };

            Graph.Nodes.Add(pA);
            Graph.Nodes.Add(pB);
            Graph.Nodes.Add(pC);
            Graph.Nodes.Add(pD);
            Graph.Nodes.Add(pE);
            Graph.Nodes.Add(pF);
            Graph.Nodes.Add(pG);
            Graph.Nodes.Add(pH);

            Graph.AddDependency(pA, pC);
            Graph.AddDependency(pA, pF);
            Graph.AddDependency(pA, pD);
            Graph.AddDependency(pD, pF);
            Graph.AddDependency(pD, pG);
            Graph.AddDependency(pB, pD);
            Graph.AddDependency(pB, pG);
            Graph.AddDependency(pE, pG);
            Graph.AddDependency(pG, pF);
            Graph.AddDependency(pG, pH);

        }

        [TestMethod]
        public void GenerateDot()
        {
            var startNodes = Graph.Nodes.Where(x => !Graph.Nodes.Any(y => Graph.GetDependenciesForNode(y).Contains(x)));
            var endNodes = Graph.Nodes.Where(x => !Graph.GetDependenciesForNode(x).Any());
            var circularReferences = CircularReferencesHelper.FindCircularReferences(Graph, startNodes.ToList(), endNodes.ToList());

            var dotCommandBuilder = new DotCommandBuilder<ProjectInfo>();
            var dotCommand = dotCommandBuilder.GenerateDotCommand(Graph, circularReferences, "", true);
            Console.WriteLine(dotCommand);
        }

        [TestMethod]
        public void GenerateDotWithCircularReferences()
        {
            var pA = Graph.Nodes.First(x => x.GeneratedAssembly == "A");
            var pG = Graph.Nodes.First(x => x.GeneratedAssembly == "G");
            Graph.AddDependency(pG, pA);

            var startNodes = Graph.Nodes.Where(x => !Graph.Nodes.Any(y => Graph.GetDependenciesForNode(y).Contains(x)));
            var endNodes = Graph.Nodes.Where(x => !Graph.GetDependenciesForNode(x).Any());
            var circularReferences = CircularReferencesHelper.FindCircularReferences(Graph, startNodes.ToList(), endNodes.ToList());

            var dotCommandBuilder = new DotCommandBuilder<ProjectInfo>();
            var dotCommand = dotCommandBuilder.GenerateDotCommand(Graph, circularReferences, "", true);
            Console.WriteLine(dotCommand);
        }
    }
}
