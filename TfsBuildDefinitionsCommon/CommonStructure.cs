using System;
using System.Configuration;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace TfsBuildDefinitionsCommon
{
    public class CommonStructure
    {
        public readonly IBuildServer BuildServer;
        public readonly TfsTeamProjectCollection Collection;
        public readonly ICommonStructureService CommonStructureService;
        public readonly string DeploymentPackagesLocation;

        public readonly IProcessTemplate NoCompileFullTemplate;
        public readonly string NoCompileFullTemplatePath;

        public readonly IProcessTemplate ServicesTemplate;
        public readonly string ServicesTemplatePath;

        public readonly IProcessTemplate TopShelfServicesTemplate;
        public readonly string TopShelfServicesTemplatePath;

        public readonly IProcessTemplate NewStandardTemplate;
        public readonly IProcessTemplate StandardTemplate;

        public readonly string NewStandardTemplatePath;
        public readonly string StandardTemplatePath;

        public CommonStructure(BaseUpdateOptions options)
        {
            // Read config options
            Collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(options.TeamCollection));
            BuildServer = Collection.GetService<IBuildServer>();
            CommonStructureService = Collection.GetService<ICommonStructureService>();

            string newStandardTemplateName = GetConfigValueAsString(SettingsKeys.NewStandardTemplateName,
                Constants.DefaultNewStandardTemplateName);
            string standardTemplateName = GetConfigValueAsString(SettingsKeys.StandardTemplateName,
                Constants.DefaultStandardTemplateName);
            string servicesTemplateName = GetConfigValueAsString(SettingsKeys.ServicesTemplate,
                Constants.DefaultServicesTemplateName);
            string topShelfServicesTemplateName = GetConfigValueAsString(SettingsKeys.TopShelfServicesTemplate,
                Constants.DefaultTopShelfServicesTemplateName);
            string noCompileFullTemplateName = GetConfigValueAsString(SettingsKeys.NoCompileFullTemplate,
                Constants.DefaultNoCompileFullTemplateName);

            NewStandardTemplatePath = $"$/{options.TemplatesTeamProject}/BuildProcessTemplates/{newStandardTemplateName}";
            StandardTemplatePath = $"$/{options.TemplatesTeamProject}/BuildProcessTemplates/{standardTemplateName}";
            ServicesTemplatePath = $"$/{options.TemplatesTeamProject}/BuildProcessTemplates/{servicesTemplateName}";
            TopShelfServicesTemplatePath = $"$/{options.TemplatesTeamProject}/BuildProcessTemplates/{topShelfServicesTemplateName}";
            NoCompileFullTemplatePath = $"$/{options.TemplatesTeamProject}/BuildProcessTemplates/{noCompileFullTemplateName}";

            NewStandardTemplate = CheckCreate(options.TemplatesTeamProject, NewStandardTemplatePath);
            if (NewStandardTemplate == null)
                Console.WriteLine("New Standard template not found in '{0}' of '{1}'", NewStandardTemplatePath,
                    options.TeamCollection);
            StandardTemplate = CheckCreate(options.TemplatesTeamProject, StandardTemplatePath);
            if (StandardTemplate == null)
                Console.WriteLine("Standard template not found in '{0}' of '{1}'", StandardTemplatePath,
                    options.TeamCollection);

            ServicesTemplate = CheckCreate(options.TemplatesTeamProject, ServicesTemplatePath);
            if (ServicesTemplate == null)
                Console.WriteLine("Services template not found in '{0}' of '{1}'", ServicesTemplatePath,
                    options.TeamCollection);

            TopShelfServicesTemplate = CheckCreate(options.TemplatesTeamProject, TopShelfServicesTemplatePath);
            if (TopShelfServicesTemplate == null)
                Console.WriteLine("TopShelf Services template not found in '{0}' of '{1}'", TopShelfServicesTemplatePath,
                    options.TeamCollection);

            NoCompileFullTemplate = CheckCreate(options.TemplatesTeamProject, NoCompileFullTemplatePath);
            if (NoCompileFullTemplate == null)
                Console.WriteLine("No-compile template not found in '{0}' of '{1}'", NoCompileFullTemplatePath,
                    options.TeamCollection);

            DeploymentPackagesLocation = GetConfigValueAsString(SettingsKeys.PackagesDropLocation,
                Constants.DefaultPackagesDropLocation);
        }


        private IProcessTemplate CheckCreate(string teamProject, string serverPath)
        {
            try
            {
                var allTemplates = BuildServer.QueryProcessTemplates(teamProject);
                var templates = allTemplates.Where(p => p.ServerPath == serverPath).ToList();
                if (templates.Any())
                    return templates.Last();
                

                var pt = BuildServer.CreateProcessTemplate(teamProject, serverPath);
                pt.Save();
                return pt;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while checking or creating template: '{0}'", ex.Message);
                return null;
            }
        }

        private static string GetConfigValueAsString(string key, string defaultValue = "")
        {
            var v = ConfigurationManager.AppSettings[key];
            if (String.IsNullOrEmpty(v))
                v = defaultValue;
            return v;
        }
    }
}