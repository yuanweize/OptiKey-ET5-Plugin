using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.IO.Compression;

namespace OptiKey.ET5.Plugin.PackagedLoaderHarness
{
    internal static class Program
    {
        private const string PointServiceContract = "JuliusSweetland.OptiKey.Contracts.IPointService";
        private const string ExpectedPointService = "OptiKey.ET5.Plugin.ET5PointService";

        private static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine("Usage: PackagedLoaderHarness.exe <plugin-zip> <contracts-dll> [host-dependency-directory]");
                return 2;
            }

            string zipPath = Path.GetFullPath(args[0]);
            string contractsPath = Path.GetFullPath(args[1]);
            string hostDependencyDirectory = args.Length > 2
                ? Path.GetFullPath(args[2])
                : Path.GetDirectoryName(contractsPath);

            try
            {
                return Run(zipPath, contractsPath, hostDependencyDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PACKAGED LOADER SMOKE TEST FAILED: " + ex);
                return 1;
            }
        }

        private static int Run(string zipPath, string contractsPath, string hostDependencyDirectory)
        {
            RequireFile(zipPath, "plugin ZIP");
            RequireFile(contractsPath, "Contracts assembly");
            RequireDirectory(hostDependencyDirectory, "host dependency directory");

            string sandbox = Path.Combine(Path.GetTempPath(), "OptiKey_LoaderHarness_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sandbox);
            try
            {
                ZipFile.ExtractToDirectory(zipPath, sandbox);
                string pluginPath = Path.Combine(sandbox, "OptiKey.ET5.Plugin.dll");
                RequireFile(pluginPath, "packaged plugin assembly");

                ResolveEventHandler resolver = (sender, eventArgs) => ResolveAssembly(eventArgs, sandbox, hostDependencyDirectory, contractsPath);
                AppDomain.CurrentDomain.AssemblyResolve += resolver;
                try
                {
                    Assembly contractsAssembly = Assembly.LoadFrom(contractsPath);
                    Assembly pluginAssembly = Assembly.LoadFrom(pluginPath);
                    Type contractType = contractsAssembly.GetType(PointServiceContract, true);
                    Type[] services = pluginAssembly.GetTypes()
                        .Where(type => contractType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        .ToArray();

                    if (services.Length != 1)
                    {
                        throw new InvalidOperationException("Expected exactly one concrete IPointService; found " + services.Length + ".");
                    }
                    if (services[0].FullName != ExpectedPointService)
                    {
                        throw new InvalidOperationException("Unexpected point service type: " + services[0].FullName);
                    }

                    object instance = Activator.CreateInstance(services[0]);
                    if (instance == null)
                    {
                        throw new InvalidOperationException("Activator.CreateInstance returned null.");
                    }

                    try
                    {
                        if (!(instance is IDisposable))
                        {
                            throw new InvalidOperationException("Point service does not implement IDisposable.");
                        }
                        ((IDisposable)instance).Dispose();
                    }
                    finally
                    {
                        var disposable = instance as IDisposable;
                        if (disposable != null)
                        {
                            instance = null;
                        }
                    }

                    Console.WriteLine("PACKAGED LOADER SMOKE TEST PASSED: " + services[0].FullName);
                    return 0;
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= resolver;
                }
            }
            finally
            {
                if (Directory.Exists(sandbox))
                {
                    Directory.Delete(sandbox, true);
                }
            }
        }

        private static Assembly ResolveAssembly(ResolveEventArgs eventArgs, string packageDirectory, string hostDependencyDirectory, string contractsPath)
        {
            string assemblyFileName = new AssemblyName(eventArgs.Name).Name + ".dll";
            string packageCandidate = Path.Combine(packageDirectory, assemblyFileName);
            if (File.Exists(packageCandidate))
            {
                return Assembly.LoadFrom(packageCandidate);
            }

            string hostCandidate = Path.Combine(hostDependencyDirectory, assemblyFileName);
            if (File.Exists(hostCandidate))
            {
                return Assembly.LoadFrom(hostCandidate);
            }

            if (string.Equals(Path.GetFileName(contractsPath), assemblyFileName, StringComparison.OrdinalIgnoreCase))
            {
                return Assembly.LoadFrom(contractsPath);
            }

            return null;
        }

        private static void RequireFile(string path, string description)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Missing " + description, path);
            }
        }

        private static void RequireDirectory(string path, string description)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("Missing " + description + ": " + path);
            }
        }
    }
}
