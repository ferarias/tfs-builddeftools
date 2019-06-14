using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Build.Client;

namespace TfsBuildDefinitionsCommon
{
    public interface IPrinter
    {
        bool PrintDefinitionDetails(
            IEnumerable<IBuildDefinition> definitions,
            bool printTemplate = false,
            bool printTrigger = false,
            bool printDropLocation = false,
            bool printParams = false,
            bool printPolicies = false,
            bool printBuilds = false,
            string filterKeys = null,
            string filterValues = null);

        string PrintObject(Object obj);
    }
}