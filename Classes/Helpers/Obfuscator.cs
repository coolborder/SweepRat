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

    // Key bytes for XOR depending on level
    private byte[] GetKeyBytes()
    {
        return encryptionLevel switch
        {
            EncryptionLevel.None => Array.Empty<byte>(),
            EncryptionLevel.Low => Encoding.UTF8.GetBytes("key1"),
            EncryptionLevel.Medium => Encoding.UTF8.GetBytes("longerkey2"),
            EncryptionLevel.High => Encoding.UTF8.GetBytes("verylongerkey3!!"),
            _ => Array.Empty<byte>()
        };
    }

    // Public method to run obfuscation
    public void Obfuscate(string inputPath, string outputPath, EncryptionLevel level = EncryptionLevel.Medium)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input assembly not found.", inputPath);

        encryptionLevel = level;

        var assembly = AssemblyDefinition.ReadAssembly(inputPath, new ReaderParameters { ReadWrite = false });

        // FIRST: Inject decryption helper class (before any obfuscation)
        var decryptionHelper = InjectDecryptionHelper(assembly);

        // SECOND: Replace all resource access calls with decryption calls
        ReplaceResourceAccessCalls(assembly, decryptionHelper);

        // THIRD: Encrypt embedded resources
        EncryptEmbeddedResources(assembly);

        // FOURTH: Do other obfuscation techniques
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
        int decoyCount = random.Next(1, 3);
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
        // Rename namespace to random if any
        if (!string.IsNullOrEmpty(type.Namespace))
            type.Namespace = RandomString(random.Next(5, 12));

        // Rename type name
        type.Name = RandomString(random.Next(6, 14));

        // Obfuscate fields (including public)
        foreach (var field in type.Fields)
        {
            if (!field.IsSpecialName)
            {
                field.Name = RandomString(random.Next(6, 14));
            }
        }

        // Obfuscate methods
        foreach (var method in type.Methods)
        {
            if (!method.IsConstructor && !method.IsSpecialName)
            {
                method.Name = RandomString(random.Next(6, 14));
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

    private void AddDummyVariables(MethodDefinition method)
    {
        // Add some dummy local variables to confuse decompilers
        int dummyCount = random.Next(1, 4);
        for (int i = 0; i < dummyCount; i++)
        {
            var dummyVar = new VariableDefinition(method.Module.TypeSystem.Object);
            method.Body.Variables.Add(dummyVar);
        }
    }

    private void ReplaceResourceAccessCalls(AssemblyDefinition assembly, TypeDefinition decryptionHelper)
    {
        var decryptMethod = decryptionHelper.Methods.FirstOrDefault(m => m.IsPublic && m.IsStatic && m.Parameters.Count == 1);
        if (decryptMethod == null)
        {
            Console.WriteLine("Warning: Could not find decryption method in helper class");
            return;
        }

        Console.WriteLine("Scanning for resource access calls to replace...");
        int replacementCount = 0;

        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;

                    try
                    {
                        var instructions = method.Body.Instructions.ToArray(); // Create a copy to avoid modification during iteration
                        bool methodModified = false;

                        for (int i = 0; i < instructions.Length; i++)
                        {
                            var instruction = instructions[i];

                            // Look for calls to GetManifestResourceStream specifically
                            if (instruction.OpCode == OpCodes.Callvirt && instruction.Operand is MethodReference methodRef)
                            {
                                if (methodRef.Name == "GetManifestResourceStream" &&
                                    methodRef.DeclaringType.Name == "Assembly")
                                {
                                    // Look backwards for the resource name (string parameter)
                                    string resourceName = FindResourceNameInInstructions(instructions.ToList(), i);

                                    if (resourceName != null && (resourceName.ToLower().EndsWith(".json") || resourceName.ToLower().Contains("config")))
                                    {
                                        Console.WriteLine($"Found GetManifestResourceStream call in {type.Name}.{method.Name}: {resourceName}");

                                        // Find the current index in the actual instruction collection
                                        int actualIndex = -1;
                                        for (int j = 0; j < method.Body.Instructions.Count; j++)
                                        {
                                            var actualInstr = method.Body.Instructions[j];
                                            if (actualInstr.OpCode == instruction.OpCode &&
                                                actualInstr.Operand is MethodReference actualMethodRef &&
                                                actualMethodRef.Name == methodRef.Name)
                                            {
                                                actualIndex = j;
                                                break;
                                            }
                                        }

                                        if (actualIndex >= 0)
                                        {
                                            // Replace GetManifestResourceStream with our decryption call
                                            ReplaceResourceAccessInstruction(method, actualIndex, resourceName, decryptMethod);
                                            methodModified = true;
                                            replacementCount++;
                                        }
                                    }
                                }
                            }
                        }

                        if (methodModified)
                        {
                            // Use SimplifyMacros and OptimizeMacros instead of ComputeOffsets
                            method.Body.SimplifyMacros();
                            method.Body.OptimizeMacros();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing method {type.Name}.{method.Name}: {ex.Message}");
                    }
                }
            }
        }

        Console.WriteLine($"Replaced {replacementCount} resource access calls with decryption calls");
    }

    private string FindResourceNameInInstructions(List<Instruction> instructions, int callIndex)
    {
        // Look backwards from the call instruction to find the resource name
        for (int i = callIndex - 1; i >= Math.Max(0, callIndex - 10); i--)
        {
            if (instructions[i].OpCode == OpCodes.Ldstr)
            {
                return instructions[i].Operand as string;
            }
        }
        return null;
    }

    private bool IsStringUsedForResourceAccess(List<Instruction> instructions, int stringIndex)
    {
        // Look forward to see if this string is used in a resource access call
        for (int i = stringIndex + 1; i < Math.Min(instructions.Count, stringIndex + 10); i++)
        {
            if (instructions[i].OpCode == OpCodes.Call || instructions[i].OpCode == OpCodes.Callvirt)
            {
                if (instructions[i].Operand is MethodReference methodRef)
                {
                    return methodRef.Name == "GetManifestResourceStream" ||
                           methodRef.Name == "GetResourceStream" ||
                           (methodRef.Name == "ReadAllText" && methodRef.DeclaringType.Name == "File");
                }
            }
        }
        return false;
    }

    private void ReplaceResourceAccessInstruction(MethodDefinition method, int callIndex, string resourceName, MethodDefinition decryptMethod)
    {
        var processor = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;

        // Validate callIndex
        if (callIndex < 0 || callIndex >= instructions.Count)
        {
            Console.WriteLine($"Warning: Invalid call index {callIndex} for method {method.Name}");
            return;
        }

        var callInstruction = instructions[callIndex];
        Instruction ldstrInstruction = null;

        // Find the ldstr instruction that loads the resource name
        for (int i = Math.Max(0, callIndex - 10); i < callIndex; i++)
        {
            if (instructions[i].OpCode == OpCodes.Ldstr &&
                instructions[i].Operand as string == resourceName)
            {
                ldstrInstruction = instructions[i];
                break;
            }
        }

        if (ldstrInstruction == null)
        {
            Console.WriteLine($"Warning: Could not find ldstr instruction for resource {resourceName}");
            return;
        }

        try
        {
            // Create new instructions for our decryption call
            var newLdstr = processor.Create(OpCodes.Ldstr, resourceName);
            var newCall = processor.Create(OpCodes.Call, method.Module.ImportReference(decryptMethod));

            // Replace the ldstr instruction with our new ldstr
            processor.Replace(ldstrInstruction, newLdstr);

            // Replace the original call with our decryption call
            processor.Replace(callInstruction, newCall);

            Console.WriteLine($"Successfully replaced resource access for {resourceName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error replacing instruction for {resourceName}: {ex.Message}");
        }
    }

    private void EncryptEmbeddedResources(AssemblyDefinition assembly)
    {
        var resources = assembly.MainModule.Resources.OfType<EmbeddedResource>().ToList();

        Console.WriteLine($"Found {resources.Count} embedded resources:");
        foreach (var res in resources)
        {
            Console.WriteLine($"  - {res.Name}");
        }

        foreach (var resource in resources)
        {
            // Simple check - encrypt ALL .json files and anything with "config" in the name
            bool shouldEncrypt = resource.Name.ToLower().EndsWith(".json") ||
                               resource.Name.ToLower().Contains("config");

            if (shouldEncrypt)
            {
                Console.WriteLine($"Encrypting resource: {resource.Name}");

                try
                {
                    // Get the resource data using our helper method
                    byte[] data = GetResourceBytes(resource);

                    if (data == null || data.Length == 0)
                    {
                        Console.WriteLine($"Warning: Resource {resource.Name} is empty or could not be read");
                        continue;
                    }

                    Console.WriteLine($"Original resource size: {data.Length} bytes");

                    // Display first few bytes for debugging
                    if (data.Length > 0)
                    {
                        var preview = string.Join(" ", data.Take(Math.Min(10, data.Length)).Select(b => b.ToString("X2")));
                        Console.WriteLine($"Original first bytes: {preview}");
                    }

                    // Encrypt the data
                    byte[] encrypted = XorEncrypt(data);

                    Console.WriteLine($"Encrypted resource size: {encrypted.Length} bytes");

                    // Display first few bytes of encrypted data
                    if (encrypted.Length > 0)
                    {
                        var encPreview = string.Join(" ", encrypted.Take(Math.Min(10, encrypted.Length)).Select(b => b.ToString("X2")));
                        Console.WriteLine($"Encrypted first bytes: {encPreview}");
                    }

                    // Verify encryption actually changed the data
                    bool dataChanged = !data.SequenceEqual(encrypted);
                    Console.WriteLine($"Data was modified by encryption: {dataChanged}");

                    // Create new encrypted resource with same name and attributes
                    var newResource = new EmbeddedResource(resource.Name, resource.Attributes, encrypted);

                    // Replace the original resource
                    assembly.MainModule.Resources.Remove(resource);
                    assembly.MainModule.Resources.Add(newResource);

                    Console.WriteLine($"Successfully encrypted and replaced resource: {resource.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to encrypt resource {resource.Name}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"Skipping resource (not .json or config): {resource.Name}");
            }
        }
    }

    // Improved method to get resource data
    private byte[] GetResourceBytes(EmbeddedResource resource)
    {
        try
        {
            // Method 1: Try GetResourceData() first
            byte[] data = resource.GetResourceData();
            Console.WriteLine($"Successfully read {data.Length} bytes using GetResourceData()");
            return data;
        }
        catch (Exception ex1)
        {
            Console.WriteLine($"GetResourceData() failed: {ex1.Message}");
            try
            {
                // Method 2: Use GetResourceStream() as fallback
                using var stream = resource.GetResourceStream();
                if (stream == null)
                {
                    Console.WriteLine("GetResourceStream() returned null");
                    return Array.Empty<byte>();
                }

                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                byte[] data = memoryStream.ToArray();
                Console.WriteLine($"Successfully read {data.Length} bytes using GetResourceStream()");
                return data;
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"GetResourceStream() also failed: {ex2.Message}");
                throw new Exception($"Failed to read resource data using both methods: {ex1.Message} | {ex2.Message}");
            }
        }
    }

    private TypeDefinition InjectDecryptionHelper(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;

        // Create the decryption helper class
        var helperType = new TypeDefinition(
            RandomString(random.Next(8, 12)), // Random namespace
            RandomString(random.Next(8, 12)), // Random class name
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
            module.TypeSystem.Object);

        // Add encryption level field
        var levelField = new FieldDefinition(
            RandomString(random.Next(6, 10)),
            FieldAttributes.Private | FieldAttributes.Static,
            module.TypeSystem.Int32);
        helperType.Fields.Add(levelField);

        // Static constructor to set encryption level
        var staticCtor = new MethodDefinition(
            ".cctor",
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);

        var il = staticCtor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldc_I4, (int)encryptionLevel);
        il.Emit(OpCodes.Stsfld, levelField);
        il.Emit(OpCodes.Ret);
        helperType.Methods.Add(staticCtor);

        // Create DecryptResource method that returns a Stream (not string)
        var decryptMethod = new MethodDefinition(
            RandomString(random.Next(8, 12)), // Random method name
            MethodAttributes.Public | MethodAttributes.Static,
            module.ImportReference(typeof(Stream)));

        decryptMethod.Parameters.Add(new ParameterDefinition("resourceName", ParameterAttributes.None, module.TypeSystem.String));

        var bodyIL = decryptMethod.Body.GetILProcessor();

        // Method body will call our decrypt logic
        bodyIL.Emit(OpCodes.Ldarg_0); // Load resource name
        bodyIL.Emit(OpCodes.Ldsfld, levelField); // Load encryption level
        bodyIL.Emit(OpCodes.Call, CreateDecryptResourceCore(module, helperType));
        bodyIL.Emit(OpCodes.Ret);

        helperType.Methods.Add(decryptMethod);
        module.Types.Add(helperType);

        Console.WriteLine($"Injected decryption helper class: {helperType.Namespace}.{helperType.Name}");
        return helperType;
    }

    private MethodReference CreateDecryptResourceCore(ModuleDefinition module, TypeDefinition helperType)
    {
        // Create the core decryption method that actually decrypts and returns a Stream
        var coreMethod = new MethodDefinition(
            RandomString(random.Next(8, 12)),
            MethodAttributes.Private | MethodAttributes.Static,
            module.ImportReference(typeof(Stream)));

        coreMethod.Parameters.Add(new ParameterDefinition("resourceName", ParameterAttributes.None, module.TypeSystem.String));
        coreMethod.Parameters.Add(new ParameterDefinition("level", ParameterAttributes.None, module.TypeSystem.Int32));

        // Add local variables
        coreMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(Stream))));        // 0: original stream
        coreMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(byte[]))));       // 1: byte array
        coreMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(byte[]))));       // 2: decrypted array
        coreMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(MemoryStream)))); // 3: return stream

        var il = coreMethod.Body.GetILProcessor();

        try
        {
            // Get executing assembly
            il.Emit(OpCodes.Call, module.ImportReference(
                typeof(System.Reflection.Assembly).GetMethod("GetExecutingAssembly", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)));

            // Load resource name and get stream
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(System.Reflection.Assembly).GetMethod("GetManifestResourceStream", new[] { typeof(string) })));
            il.Emit(OpCodes.Stloc_0); // Store stream in local 0

            // Check if stream is null
            il.Emit(OpCodes.Ldloc_0);
            var nullLabel = il.Create(OpCodes.Ldnull);
            il.Emit(OpCodes.Brfalse_S, nullLabel);

            // Read stream to byte array
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Stream).GetProperty("Length").GetMethod));
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Newarr, module.TypeSystem.Byte);
            il.Emit(OpCodes.Stloc_1); // Store byte array in local 1

            // Read from stream: stream.Read(array, 0, array.Length)
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Callvirt, module.ImportReference(
                typeof(Stream).GetMethod("Read", new[] { typeof(byte[]), typeof(int), typeof(int) })));
            il.Emit(OpCodes.Pop); // Remove return value

            // Dispose original stream
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(IDisposable).GetMethod("Dispose")));

            // Decrypt the byte array: XorDecrypt(data, level)
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, module.ImportReference(GetType().GetMethod("XorDecrypt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)));
            il.Emit(OpCodes.Stloc_2); // Store decrypted array in local 2

            // Create MemoryStream from decrypted data
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Newobj, module.ImportReference(typeof(MemoryStream).GetConstructor(new[] { typeof(byte[]) })));
            il.Emit(OpCodes.Stloc_3);

            // Return the memory stream
            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Ret);

            // Null case label
            il.Append(nullLabel);
            il.Emit(OpCodes.Ret);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create full decryption method: {ex.Message}");
            // Fallback: return null
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
        }

        helperType.Methods.Add(coreMethod);
        return coreMethod;
    }

    private byte[] XorEncrypt(byte[] data)
    {
        if (encryptionLevel == EncryptionLevel.None || data.Length == 0)
        {
            Console.WriteLine("Skipping encryption: level is None or data is empty");
            return data;
        }

        byte[] key = GetKeyBytes();
        if (key.Length == 0)
        {
            Console.WriteLine("Warning: Encryption key is empty!");
            return data;
        }

        byte[] output = new byte[data.Length];
        Console.WriteLine($"Encrypting with key length: {key.Length}, level: {encryptionLevel}");

        for (int i = 0; i < data.Length; i++)
        {
            byte val = data[i];

            // Apply XOR based on encryption level - use different key bytes for each level
            for (int level = 1; level <= (int)encryptionLevel; level++)
            {
                // Use different key positions to avoid cancellation
                int keyIndex = (i + level - 1) % key.Length;
                val ^= key[keyIndex];
            }
            output[i] = val;
        }

        Console.WriteLine($"Encryption completed. First byte: {data[0]:X2} -> {output[0]:X2}");
        return output;
    }

    // Standalone decryption method (same as encryption since XOR is symmetric)
    public static byte[] XorDecrypt(byte[] encryptedData, EncryptionLevel level)
    {
        if (level == EncryptionLevel.None || encryptedData.Length == 0)
            return encryptedData;

        byte[] key = level switch
        {
            EncryptionLevel.Low => Encoding.UTF8.GetBytes("key1"),
            EncryptionLevel.Medium => Encoding.UTF8.GetBytes("longerkey2"),
            EncryptionLevel.High => Encoding.UTF8.GetBytes("verylongerkey3!!"),
            _ => Array.Empty<byte>()
        };

        byte[] output = new byte[encryptedData.Length];

        for (int i = 0; i < encryptedData.Length; i++)
        {
            byte val = encryptedData[i];

            // Apply XOR based on encryption level - use different key bytes for each level
            for (int levelCount = 1; levelCount <= (int)level; levelCount++)
            {
                // Use different key positions to match encryption
                int keyIndex = (i + levelCount - 1) % key.Length;
                val ^= key[keyIndex];
            }
            output[i] = val;
        }
        return output;
    }

    // Helper method to decrypt embedded resources at runtime
    public static string DecryptEmbeddedResource(string resourceName, EncryptionLevel level)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
            return null;

        byte[] data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);

        byte[] decrypted = XorDecrypt(data, level);
        return Encoding.UTF8.GetString(decrypted);
    }

    private string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
            buffer[i] = chars[random.Next(chars.Length)];
        return new string(buffer);
    }
}

// Example usage class for the obfuscated assembly
public static class ResourceHelper
{
    public static string GetDecryptedConfig(Obfuscator.EncryptionLevel level)
    {
        // This would be called from within the obfuscated assembly
        return Obfuscator.DecryptEmbeddedResource("config.json", level);
    }
}