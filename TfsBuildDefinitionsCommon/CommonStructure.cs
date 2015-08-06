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
        public readonly IProcessTemplate StandardTemplate;
        public readonly string StandardTemplatePath;
        public readonly Uri TeamProjectCollection;

        public CommonStructure(BaseOptions options)
        {
            // Read config options
            TeamProjectCollection =
                new Uri(String.Format(GetConfigValueAsString(SettingsKeys.TfsUri, Constants.DefaultTfsUri),
                    options.TeamCollection));
            Collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(TeamProjectCollection);
            BuildServer = Collection.GetService<IBuildServer>();
            CommonStructureService = Collection.GetService<ICommonStructureService>();

            string standardTemplateName = GetConfigValueAsString(SettingsKeys.StandardTemplateName,
                Constants.DefaultStandardTemplateName);
            string servicesTemplateName = GetConfigValueAsString(SettingsKeys.ServicesTemplate,
                Constants.DefaultServicesTemplateName);
            string noCompileFullTemplateName = GetConfigValueAsString(SettingsKeys.NoCompileFullTemplate,
                Constants.DefaultNoCompileFullTemplateName);

            StandardTemplatePath = String.Format("$/{0}/BuildProcessTemplates/{1}", options.TemplatesTeamProject,
                standardTemplateName);
            ServicesTemplatePath = String.Format("$/{0}/BuildProcessTemplates/{1}", options.TemplatesTeamProject,
                servicesTemplateName);
            NoCompileFullTemplatePath = String.Format("$/{0}/BuildProcessTemplates/{1}", options.TemplatesTeamProject,
                noCompileFullTemplateName);

            StandardTemplate = CheckCreate(options.TemplatesTeamProject, StandardTemplatePath);
            if (StandardTemplate == null)
                Console.WriteLine("Standard template not found in '{0}' of '{1}'", StandardTemplatePath,
                    TeamProjectCollection);

            ServicesTemplate = CheckCreate(options.TemplatesTeamProject, ServicesTemplatePath);
            if (ServicesTemplate == null)
                Console.WriteLine("Services template not found in '{0}' of '{1}'", ServicesTemplatePath,
                    TeamProjectCollection);

            NoCompileFullTemplate = CheckCreate(options.TemplatesTeamProject, NoCompileFullTemplatePath);
            if (NoCompileFullTemplate == null)
                Console.WriteLine("No-compile template not found in '{0}' of '{1}'", NoCompileFullTemplatePath,
                    TeamProjectCollection);

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
                    return templates.ElementAt(0);
                

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