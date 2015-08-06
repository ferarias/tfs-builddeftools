using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;

namespace TfsBuildDefinitionsCommon
{
    public class Printer : IPrinter
    {
        public bool PrintDefinitionDetails(
            IBuildDefinition definition,
            bool printParams = false,
            bool printPolicies = false,
            bool printBuilds = false,
            string filterKeys = null,
            string filterValues = null)
        {
            var filtered = false;

            string[] paras = null;
            if (filterKeys != null)
                paras = filterKeys.Split(',');

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("{0}", definition.Name);
            Console.ResetColor();
            Console.WriteLine("\tTrigger: '{0}'\tDefault drop location: '{1}'",
                definition.TriggerType, definition.DefaultDropLocation);

            if (printParams)
            {

                var processParameters = definition.ProcessParameters;
                var paramValues = WorkflowHelpers.DeserializeProcessParameters(processParameters);
                var values = ((filterKeys == null) ? paramValues : paramValues.Where(p => paras.Contains(p.Key))).ToArray();
                if (filterValues != null)
                {
                    var filters = filterValues.Split(',');
                    //TODO
                }

                if (values.Any())
                {
                    Console.Write("Params");
                    foreach (var param in values)
                    {
                        //if (param.Key == "EnsambladosACompartir" && param.Value.ToString().Contains("*"))
                        //    filtered = true;
                        Console.WriteLine("\t'{0}': '{1}'", param.Key, PrintObject(param.Value));
                    }
                }
            }

            if (printPolicies)
            {
                Console.WriteLine("Retention Policies");
                foreach (var retentionPolicy in definition.RetentionPolicyList)
                {
                    Console.WriteLine("\t{0}: {1}, Delete: {2}", retentionPolicy.BuildStatus,
                        retentionPolicy.NumberToKeep, retentionPolicy.DeleteOptions);
                }
            }

            if (printBuilds)
            {
                var builds = definition.QueryBuilds();
                Console.WriteLine("\tBuilds: {0}", builds.Count());
            }
            return filtered;
        }

        public string PrintObject(Object obj)
        {
            var objects = obj as IEnumerable<object>;
            return objects != null ? String.Join(",", objects.Select(x => x.ToString()).ToArray()) : obj.ToString();
        }
    }
}
