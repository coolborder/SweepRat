using Mono.Cecil;
using System;
using System.IO;
using System.Text;

public class StubBuilder
{
    /// <summary>
    /// Replaces an embedded resource (e.g., config.json) in a compiled .NET stub using string data.
    /// </summary>
    /// <param name="inputStubPath">Path to the input stub file (e.g., Stub.bin)</param>
    /// <param name="outputStubPath">Path where the modified stub will be saved</param>
    /// <param name="resourceName">Name of the embedded resource to replace (e.g., "config.json")</param>
    /// <param name="newResourceContent">New string content to embed</param>
    /// <returns>True if successful, false if resource not found</returns>
    public static bool ReplaceEmbeddedResourceFromString(
        string inputStubPath,
        string outputStubPath,
        string resourceName,
        string newResourceContent)
    {
        if (!File.Exists(inputStubPath))
            throw new FileNotFoundException("Stub file not found.", inputStubPath);

        // Load the stub assembly
        var assembly = AssemblyDefinition.ReadAssembly(inputStubPath, new ReaderParameters { ReadWrite = false });

        bool replaced = false;

        // Convert string to UTF-8 byte array
        byte[] newResourceData = Encoding.UTF8.GetBytes(newResourceContent);

        // Find and replace the embedded resource
        for (int i = 0; i < assembly.MainModule.Resources.Count; i++)
        {
            if (assembly.MainModule.Resources[i] is EmbeddedResource embedded &&
                embedded.Name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
            {
                var newRes = new EmbeddedResource(embedded.Name, ManifestResourceAttributes.Private, newResourceData);
                assembly.MainModule.Resources[i] = newRes;
                replaced = true;
                break;
            }
        }

        // Save only if replaced
        if (replaced)
        {
            assembly.Write(outputStubPath);
        }

        return replaced;
    }
}
