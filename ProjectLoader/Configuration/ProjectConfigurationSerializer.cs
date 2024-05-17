using System;
using System.IO;
using Newtonsoft.Json;

namespace Recliner2GCBM.Configuration
{
    /// <summary>
    /// Handles serialization and deserialization of JSON configurations.
    /// If useRelpaths is set to true:
    /// 1. unrooted paths stored in a file to be deserialized will be
    ///    made absolute with respect to the path of the file.  
    ///    Rooted paths will not be modified.
    /// 2. paths stored in a ProjectConfiguration object will be made
    ///    relative to the file to which they are being serialized
    /// Otherwise deserialization will make no changes to the data being
    /// serialized or deserialized.
    /// </summary>
    public class ProjectConfigurationSerializer
    {
        private bool useRelPaths;

        public ProjectConfigurationSerializer(bool useRelPaths = true)
        {
            this.useRelPaths = useRelPaths;
        }

        private static ProjectConfiguration Copy(ProjectConfiguration other)
        {
            return JsonConvert.DeserializeObject<ProjectConfiguration>(
                JsonConvert.SerializeObject(other));
        }

        private static ProjectConfiguration ProcessPaths(ProjectConfiguration configuration,
                                                         Func<string, string> pathProcessor)
        {
            configuration.AIDBPath = String.IsNullOrWhiteSpace(configuration.AIDBPath)
                ? configuration.AIDBPath
                : pathProcessor(configuration.AIDBPath);

            configuration.TransitionRules.Path = String.IsNullOrWhiteSpace(configuration.TransitionRules.Path)
                ? configuration.TransitionRules.Path
                : pathProcessor(configuration.TransitionRules.Path);

            configuration.GrowthCurves.Path = String.IsNullOrWhiteSpace(configuration.GrowthCurves.Path)
                ? configuration.GrowthCurves.Path
                : pathProcessor(configuration.GrowthCurves.Path);

            if (configuration.OutputConfiguration.Parameters.ContainsKey("path"))
            {
                var outputPath = configuration.OutputConfiguration.Parameters["path"];
                configuration.OutputConfiguration.Parameters["path"] = String.IsNullOrWhiteSpace(outputPath)
                    ? outputPath
                    : pathProcessor(outputPath);
            }

            foreach (var classifier in configuration.ClassifierSet)
            {
                classifier.Path = pathProcessor(classifier.Path);
            }

            return configuration;
        }

        public ProjectConfiguration Load(string filePath)
        {
            var projectConfiguration = JsonConvert.DeserializeObject<ProjectConfiguration>(
                File.ReadAllText(filePath));

            var absProjectConfigPath = Path.GetFullPath(filePath);
            if (useRelPaths)
            {
                Func<string, string> pathProcessor = x => MakeAbsolutePath(x, absProjectConfigPath);
                projectConfiguration = ProcessPaths(projectConfiguration, pathProcessor);
            }

            return projectConfiguration;
        }

        public void Save(string filePath, ProjectConfiguration conf)
        {
            ProjectConfiguration output = null;
            if (useRelPaths)
            {
                // Create a copy of the configuration so that the gui wont be affected.
                Func<string, string> pathProcessor = x => MakeRelativePath(x, filePath);
                output = Copy(conf);
                output = ProcessPaths(output, pathProcessor);
            }
            else
            {
                output = conf;
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(output, Formatting.Indented));
        }

        private static string MakeAbsolutePath(string relpath, string root)
        {
            if (Path.IsPathRooted(relpath))
            {
                // Do not attempt to convert a path into an absolute path if it is already rooted.
                return relpath;
            }

            var rootUri = new Uri(root, UriKind.Absolute);
            var rel = new Uri(rootUri, relpath);
            var abspath = Uri.UnescapeDataString(rel.AbsolutePath);

            return abspath;
        }

        private static string MakeRelativePath(string fullpath, string relroot)
        {
            var absPath = new Uri(fullpath, UriKind.Absolute);
            var relRoot = new Uri(relroot, UriKind.Absolute);
            var relPath = Uri.UnescapeDataString(relRoot.MakeRelativeUri(absPath).ToString());

            return relPath;
        }
    }
}
