using CommandLine;
using CommandLine.Text;
using Recliner2GCBM.Configuration;
using System;
using System.IO;

namespace Recliner2GCBMCLI
{
    class Options
    {
        [Option('c', "config", Required = true, HelpText = "Recliner2GCBM project config file")]
        public string ConfigFile { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) =>
                HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    enum ExitCode : int {
        Success=0,
        ArgumentError=1,
        ConfigPathError=2,
        RuntimeError=3
    }

    class Program
    {
        static int Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                return (int)ExitCode.ArgumentError;
            }

            if (!File.Exists(options.ConfigFile))
            {
                Console.WriteLine($"File not found: {options.ConfigFile}");
                return (int)ExitCode.ConfigPathError;
            }

            Console.WriteLine($"Reading configuration file: {options.ConfigFile}");

            try
            {
                var s = new ProjectConfigurationSerializer();
                var projectConfiguration = s.Load(options.ConfigFile);

                var loaderFactory = new DataLoaderFactory(new ProviderTypeFactory());
                var loader = loaderFactory.GetLoader(projectConfiguration);
                var loaderCount = loader.Count();
                int loaderNum = 1;
            
                foreach (var name in loader.Load(projectConfiguration.OutputConfiguration))
                {
                    Console.WriteLine($"Loaded {name} ({loaderNum++} of {loaderCount})");
                }

                Console.WriteLine("Finished creating project.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (int)ExitCode.RuntimeError;
            }

            return (int)ExitCode.Success;
        }
    }
}
