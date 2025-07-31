using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class Obfuscator
{
    public enum EncryptionLevel
    {
        None,
        Low,
        Medium,
        High
    }

    private static Random random = new Random();
    private Dictionary<string, string> nameMap = new Dictionary<string, string>();
    private EncryptionLevel encryptionLevel = EncryptionLevel.None;

    // Public method to run obfuscation
    public void Obfuscate(string inputPath, string outputPath, EncryptionLevel level = EncryptionLevel.Medium)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input assembly not found.", inputPath);

        encryptionLevel = level;

        var assembly = AssemblyDefinition.ReadAssembly(inputPath, new ReaderParameters { ReadWrite = false });

        // Obfuscate all modules
        foreach (var module in assembly.Modules)
        {
            ObfuscateModule(module);
        }

        assembly.Write(outputPath);
        Console.WriteLine($"Obfuscation complete. Output saved to {outputPath}");
    }

    private void ObfuscateModule(ModuleDefinition module)
    {
        // Add decoy namespaces
        AddDecoyNamespaces(module);

        foreach (var type in module.Types.ToList())
        {
            ObfuscateType(type);
        }
    }

    private void AddDecoyNamespaces(ModuleDefinition module)
    {
        // Adjust decoy count based on encryption level
        int decoyCount = encryptionLevel switch
        {
            EncryptionLevel.None => 0,
            EncryptionLevel.Low => random.Next(1, 2),
            EncryptionLevel.Medium => random.Next(1, 3),
            EncryptionLevel.High => random.Next(2, 5),
            _ => 1
        };

        for (int i = 0; i < decoyCount; i++)
        {
            string decoyNamespace = RandomString(random.Next(5, 12));
            // Create an empty type as a decoy in that namespace
            var decoyType = new TypeDefinition(
                decoyNamespace,
                RandomString(8),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                module.TypeSystem.Object);

            module.Types.Add(decoyType);
        }
    }

    private void ObfuscateType(TypeDefinition type)
    {
        // Skip obfuscation if level is None
        if (encryptionLevel == EncryptionLevel.None)
            return;

        // Rename namespace to random if any
        if (!string.IsNullOrEmpty(type.Namespace))
            type.Namespace = RandomString(GetRandomStringLength());

        // Rename type name
        type.Name = RandomString(GetRandomStringLength());

        // Obfuscate fields (including public)
        foreach (var field in type.Fields)
        {
            if (!field.IsSpecialName)
            {
                field.Name = RandomString(GetRandomStringLength());
            }
        }

        // Obfuscate methods
        foreach (var method in type.Methods)
        {
            if (!method.IsConstructor && !method.IsSpecialName)
            {
                method.Name = RandomString(GetRandomStringLength());
            }

            // Variables in IL don't have names by default, they're referenced by index
            // We can add dummy local variables to make reverse engineering harder
            if (method.HasBody && method.Body.Variables.Count > 0)
            {
                AddDummyVariables(method);
            }
        }

        // Nested types
        foreach (var nested in type.NestedTypes.ToList())
        {
            ObfuscateType(nested);
        }
    }

    private int GetRandomStringLength()
    {
        return encryptionLevel switch
        {
            EncryptionLevel.Low => random.Next(4, 8),
            EncryptionLevel.Medium => random.Next(6, 14),
            EncryptionLevel.High => random.Next(10, 20),
            _ => random.Next(6, 14)
        };
    }

    private void AddDummyVariables(MethodDefinition method)
    {
        // Adjust dummy variable count based on encryption level
        int dummyCount = encryptionLevel switch
        {
            EncryptionLevel.None => 0,
            EncryptionLevel.Low => random.Next(1, 2),
            EncryptionLevel.Medium => random.Next(1, 4),
            EncryptionLevel.High => random.Next(3, 7),
            _ => random.Next(1, 4)
        };

        for (int i = 0; i < dummyCount; i++)
        {
            var dummyVar = new VariableDefinition(method.Module.TypeSystem.Object);
            method.Body.Variables.Add(dummyVar);
        }
    }

    private string RandomString(int length)
    {
        const string chars = "abcdefgjklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
            buffer[i] = chars[random.Next(chars.Length)];
        return new string(buffer);
    }
}