using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TfsBuildRelationships;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class DependenciesTests
    {
        [TestMethod]
        public void CircularReferences()
        {
            var dependencies = new DependencyGraph<string>();
            dependencies.AddDependency("A", "C");
            dependencies.AddDependency("A", "D");
            dependencies.AddDependency("A", "F");
            dependencies.AddDependency("D", "F");
            dependencies.AddDependency("D", "G");
            dependencies.AddDependency("B", "D");
            dependencies.AddDependency("B", "G");
            dependencies.AddDependency("E", "G");
            dependencies.AddDependency("G", "F");
            dependencies.AddDependency("G", "H");
            dependencies.AddDependency("G", "A");

            var allNodes = dependencies.Nodes;
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => !dependencies.GetDependenciesForNode(x).Any());

            Assert.IsFalse(startNodes.Contains("A"));
            Assert.IsTrue(startNodes.Contains("B"));
            Assert.IsTrue(startNodes.Contains("E"));

            Assert.IsFalse(endNodes.Contains("A"));
            Assert.IsTrue(endNodes.Contains("C"));
            Assert.IsTrue(endNodes.Contains("F"));
            Assert.IsTrue(endNodes.Contains("H"));

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes.ToList(), endNodes.ToList());
            Assert.IsTrue(circularReferences.Any());


        }

        [TestMethod]
        public void CircularReferences2()
        {
            var dependencies = new DependencyGraph<string>();
            dependencies.AddDependency("A", "A");

            var allNodes = dependencies.Nodes;
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => !dependencies.GetDependenciesForNode(x).Any());

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes.ToList(), endNodes.ToList());
            Assert.IsTrue(circularReferences.Any());


        }

        [TestMethod]
        public void CircularReferences3()
        {
            var dependencies = new DependencyGraph<string>();
            dependencies.AddDependency("1", "2");
            dependencies.AddDependency("1", "3");
            dependencies.AddDependency("3", "2");
            dependencies.AddDependency("3", "10");
            dependencies.AddDependency("3", "1");
            dependencies.AddDependency("4", "2");
            
            dependencies.AddDependency("4", "3");
            dependencies.AddDependency("4", "1");
            dependencies.AddDependency("5", "3");
            dependencies.AddDependency("5", "2");
            dependencies.AddDependency("6", "2");
            dependencies.AddDependency("7", "2");
            dependencies.AddDependency("8", "2");
            dependencies.AddDependency("8", "8");
            dependencies.AddDependency("9", "2");
            dependencies.AddDependency("9", "3");
            dependencies.AddDependency("10", "2");
            dependencies.AddDependency("11", "2");
            dependencies.AddDependency("12", "2");

            var allNodes = dependencies.Nodes;
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => !dependencies.GetDependenciesForNode(x).Any());

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes.ToList(), endNodes.ToList());
            Assert.IsTrue(circularReferences.Count == 1);
            Assert.IsTrue(circularReferences[0][0] == "1");
            Assert.IsTrue(circularReferences[0][1] == "");


        }
    }
}
