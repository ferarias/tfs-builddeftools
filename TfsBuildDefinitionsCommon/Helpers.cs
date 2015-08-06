using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Server;

namespace TfsBuildDefinitionsCommon
{
    public static class Helpers
    {
        public static string GetBuildName(string name, string folder)
        {
            var buildName = name;
            if (!buildName.EndsWith("." + folder))
            {
                buildName = buildName + "." + folder;
            }
            return buildName;
        }

        /// <summary>
        /// Queries TFS for a list of build definitions
        /// </summary>
        /// <param name="projectName">If set, only builddefs for this project are queried</param>
        /// <param name="buildName"></param>
        /// <returns></returns>
        public static IEnumerable<IBuildDefinitionQueryResult> QueryBuildDefinitions(ICommonStructureService css, IBuildServer bs, string projectName = "", string buildName = "")
        {
            var specs = new List<IBuildDefinitionSpec>();

            if (String.IsNullOrWhiteSpace(projectName))
            {
                // Get a query spec for each team project
                if (String.IsNullOrWhiteSpace(buildName))
                    specs.AddRange(css.ListProjects().Select(pi => bs.CreateBuildDefinitionSpec(pi.Name)));
                else
                    specs.AddRange(css.ListProjects().Select(pi => bs.CreateBuildDefinitionSpec(pi.Name, buildName)));
            }
            else
            {
                // Get a query spec just for this team project
                if (String.IsNullOrWhiteSpace(buildName))
                    specs.Add(bs.CreateBuildDefinitionSpec(projectName));
                else
                    specs.Add(bs.CreateBuildDefinitionSpec(projectName, buildName));
            }

            // Query the definitions
            var results = bs.QueryBuildDefinitions(specs.ToArray());
            return results;
        }

        public static bool ObjectsAreEquivalent(object a, object b)
        {
            if (!a.GetType().IsArray || !b.GetType().IsArray) return a.Equals(b);
            var list1 = a as Array;
            var list2 = b as Array;
            if (list1 == null || list2 == null) return false;
            if (list1.Length != list2.Length) return false;

            for (var i = 0; i < list1.Length; i++)
                if (!ObjectsAreEquivalent(list1.GetValue(i), list2.GetValue(i)))
                    return false;
            return true;
        }

        

        public static string ObjectToString(Object obj)
        {
            if (obj == null) return "<NULL>";
            var objects = obj as IEnumerable<object>;
            return objects != null ? String.Join(",", objects.Select(x => x.ToString()).ToArray()) : obj.ToString();
        }
    }
}
