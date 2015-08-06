using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;

namespace TfsBuildDefinitionsCommon
{
    public class TabbedPrinter : IPrinter
    {
        public bool PrintDefinitionDetails(
            IEnumerable<IBuildDefinition> definitions,
            bool printTemplate = false,
            bool printTrigger = false,
            bool printDropLocation = false,
            bool printParams = false,
            bool printPolicies = false,
            bool printBuilds = false,
            string filterKeys = null,
            string filterValues = null)
        {
            string[] paras = null;
            if (filterKeys != null)
                paras = filterKeys.Split(',');

            var buildDefinitions = definitions as IList<IBuildDefinition> ?? definitions.ToList();
            IEnumerable<string> headers = new List<string>();

            // Print headers
            Console.Write("BuildDefinition\t");
            if (printTemplate) Console.Write("Template\t");
            if (printTrigger) Console.Write("Trigger\t");
            if (printDropLocation) Console.Write("DropLocation\t");
            if (printBuilds) Console.Write("Builds\t");

            if (printParams)
            {
                foreach (var buildDefinition in buildDefinitions)
                {
                    var keys = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters).Keys.ToList();
                    headers = headers.Union(keys);
                }
                if (filterKeys != null) headers = headers.Where(p => paras.Contains(p)).ToArray();

                foreach (var header in headers)
                {
                    Console.Write("{0}\t", header);
                }

            }
            Console.WriteLine();

            // Print values
            foreach (var buildDefinition in buildDefinitions)
            {
                Console.Write("{0}\t", buildDefinition.Name);
                if (printTemplate) Console.Write("{0}\t", buildDefinition.Process.ServerPath);
                if (printTrigger) Console.Write("{0}\t", buildDefinition.TriggerType);
                if (printDropLocation) Console.Write("{0}\t", buildDefinition.DefaultDropLocation);
                if (printBuilds) Console.Write("{0}\t", buildDefinition.QueryBuilds().Count());
                
                if (printParams)
                {
                    var paramValues = WorkflowHelpers.DeserializeProcessParameters(buildDefinition.ProcessParameters);
                    foreach (var header in headers)
                    {
                        if (paramValues.ContainsKey(header))
                            Console.Write("{0}\t", PrintObject(paramValues[header]));
                        else
                            Console.Write("\t");
                    }
                }

                Console.WriteLine();
            }
            return false;
        }

        public string PrintObject(Object obj)
        {
            var objects = obj as IEnumerable<object>;
            return objects != null ? String.Join(",", objects.Select(x => x.ToString()).ToArray()) : obj.ToString();
        }
    }
}
