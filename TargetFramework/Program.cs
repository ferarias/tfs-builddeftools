using System;
using System.Linq;
using Mono.Cecil;
using CommandLine;
using System.IO;

namespace TargetFramework
{
    class Program
    {
        public static void Main(string[] args)
		{
			// Try to parse options from command line
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                bool noOutput = false;
                if (String.IsNullOrWhiteSpace(options.OutputFile))
                {
                    noOutput = true;
                    options.Verbose = true;
                    options.OutputFile = System.IO.Path.GetTempFileName();
                }

                using (var outputFile = new StreamWriter(options.OutputFile))
                {
                    if (Directory.Exists(options.InputFile))
                    {
                        foreach (var file in Directory.GetFiles(options.InputFile, "*.dll"))
                        {
                            ProcessFile(file, outputFile, options.Verbose);
                        }
                    }
                    else if (File.Exists(options.InputFile))
                    {
                        ProcessFile(options.InputFile, outputFile, options.Verbose);
                    }
                    else
                    {
                        Console.WriteLine("Input file or folder does not exist");
                    }
                }
                if (noOutput)
                    File.Delete(options.OutputFile);
            }
            else
            {
                Console.WriteLine("Couldn't read options");
                Console.WriteLine();
			}
			
		}

        private static void ProcessFile(string assemblyPath, StreamWriter outStream, bool verbose)
        {

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
            var runtime = "N/A";
            var mainModule = assemblyDefinition.MainModule;

            switch (mainModule.Runtime)
            {
                case TargetRuntime.Net_1_0:
                    runtime = "1.0";
                    break;
                case TargetRuntime.Net_1_1:
                    runtime = "1.1";
                    break;
                case TargetRuntime.Net_2_0:
                    runtime = "2.0";
                    break;
                case TargetRuntime.Net_4_0:
                    runtime = IsNet45(assemblyDefinition) ? "4.5" : "4.0";
                    break;
            }
            outStream.WriteLine("{0}\t{1}", assemblyDefinition.Name.Name, runtime);
            if(verbose)
                Console.WriteLine("{0}\t{1}", assemblyDefinition.Name.Name, runtime);
        }

        public static bool IsNet45(AssemblyDefinition assemblyDefinition)
        {
            foreach (var custom in assemblyDefinition.CustomAttributes)
            {
                if (custom.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    var framework = custom.Properties[0].Argument.Value;
                    if (framework.ToString().StartsWith(@".NET Framework 4.5", StringComparison.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}