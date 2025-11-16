using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.ElevatorMusic;

public static class ElevatorMusicBuilderExtensions
{
    public static IResourceBuilder<ExecutableResource> AddElevatorMusic(
        this IDistributedApplicationBuilder builder,
        string name = "ElevatorMusic",
        string? trackFile = null)
    {
        var assemblyLocation = typeof(ElevatorMusicBuilderExtensions).Assembly.Location;
        var workingDir = Path.GetDirectoryName(assemblyLocation)!;
        var dllName = Path.GetFileName(assemblyLocation);

        var args = new List<string> { dllName };
        if (!string.IsNullOrWhiteSpace(trackFile))
            args.Add(trackFile);

        var elevator = builder.AddExecutable(
            name: name,
            command: "dotnet",
            workingDirectory: workingDir,
            args: args.ToArray());

        // Wait for all other resources to be ready
        // We need IResourceBuilder objects for WaitFor, but builder.Resources gives us IResource objects.
        // Use reflection to access the internal resource builder collection.
        var builderType = builder.GetType();
        
        // Try common property/field names that might contain resource builders
        var possibleNames = new[] { "ResourceBuilders", "_resourceBuilders", "Resources" };
        
        foreach (var propName in possibleNames)
        {
            var prop = builderType.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
            {
                var value = prop.GetValue(builder);
                if (value is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        // Check if it's a resource builder
                        var itemType = item.GetType();
                        if (itemType.IsGenericType && 
                            itemType.GetGenericTypeDefinition().Name.StartsWith("IResourceBuilder"))
                        {
                            if (item is IResourceBuilder<IResource> rb && rb.Resource != elevator.Resource)
                            {
                                elevator = elevator.WaitFor(rb);
                            }
                        }
                    }
                    break; // Found and processed, no need to check other properties
                }
            }
            
            // Also check fields
            var field = builderType.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(builder);
                if (value is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        var itemType = item.GetType();
                        if (itemType.IsGenericType && 
                            itemType.GetGenericTypeDefinition().Name.StartsWith("IResourceBuilder"))
                        {
                            if (item is IResourceBuilder<IResource> rb && rb.Resource != elevator.Resource)
                            {
                                elevator = elevator.WaitFor(rb);
                            }
                        }
                    }
                    break;
                }
            }
        }
        
        return elevator;
    }
}