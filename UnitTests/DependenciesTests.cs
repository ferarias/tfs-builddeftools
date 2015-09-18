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

            var allNodes = dependencies.GetNodes();
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => dependencies.GetDependenciesForNode(x).Count() == 0);

            Assert.IsFalse(startNodes.Contains("A"));
            Assert.IsTrue(startNodes.Contains("B"));
            Assert.IsTrue(startNodes.Contains("E"));

            Assert.IsFalse(endNodes.Contains("A"));
            Assert.IsTrue(endNodes.Contains("C"));
            Assert.IsTrue(endNodes.Contains("F"));
            Assert.IsTrue(endNodes.Contains("H"));

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes, endNodes);
            Assert.IsTrue(circularReferences.Count() > 0);


        }

        [TestMethod]
        public void CircularReferences2()
        {
            var dependencies = new DependencyGraph<string>();
            dependencies.AddDependency("A", "A");

            var allNodes = dependencies.GetNodes();
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => dependencies.GetDependenciesForNode(x).Count() == 0);

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes, endNodes);
            Assert.IsTrue(circularReferences.Count() > 0);


        }

        [TestMethod]
        public void CircularReferences3()
        {
            var dependencies = new DependencyGraph<string>();
            dependencies.AddDependency("A", "B");
            dependencies.AddDependency("B", "C");
            dependencies.AddDependency("C", "D");
            dependencies.AddDependency("B", "A");
            dependencies.AddDependency("C", "B");
            dependencies.AddDependency("D", "D");

            var allNodes = dependencies.GetNodes();
            var startNodes = allNodes.Where(x => !allNodes.Any(y => dependencies.GetDependenciesForNode(y).Contains(x)));
            var endNodes = allNodes.Where(x => dependencies.GetDependenciesForNode(x).Count() == 0);

            var circularReferences = CircularReferencesHelper.FindCircularReferences(dependencies, startNodes, endNodes);
            Assert.IsTrue(circularReferences.Count() > 0);


        }
    }
}
