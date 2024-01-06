using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.Loader;

namespace PipeHow.AzBobbyTables.LoadContext;

public class ModuleAssemblyContextHandler : IModuleAssemblyInitializer
{
    private static readonly string assemblyName = "AzBobbyTables.Core";

    // Get the path of the dependencies directory relative to the module file
    private static readonly string dependencyDirPath = Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "dependencies"));

    // Create the custom load context to use, with the path to the dependencies directory
    private static readonly DependencyAssemblyLoadContext dependencyLoadContext = new(dependencyDirPath);

    // This will run when the module is imported
    // Hook up our own assembly resolving method
    // It will run when the default load context fails to resolve an assembly
    public void OnImport() => AssemblyLoadContext.Default.Resolving += ResolveAssembly;

    // If the assembly is our dependency assembly
    // Load it using our custom assembly load context
    // Otherwise indicate that nothing was loaded
    private static Assembly ResolveAssembly(AssemblyLoadContext defaultAlc, AssemblyName assemblyToResolve) =>
        assemblyToResolve.Name == assemblyName ? dependencyLoadContext.LoadFromAssemblyName(assemblyToResolve) : null;
}
