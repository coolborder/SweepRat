using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

enum ObfuscationLevel
{
    None,
    Low,
    Medium,
    High
}

class Obfuscator
{
    private readonly ObfuscationLevel _level;
    private readonly Random _random = new Random();

    private MethodDefinition decryptMethod;
    private FieldDefinition xorKeyField;

    public Obfuscator(ObfuscationLevel level)
    {
        _level = level;
    }

    public Obfuscator(string level)
    {
        if (!Enum.TryParse(level, ignoreCase: true, out ObfuscationLevel parsedLevel))
            throw new ArgumentException($"Invalid obfuscation level: {level}. Use None, Low, Medium, or High.");

        _level = parsedLevel;
    }

    public void Obfuscate(string inputPath, string outputPath)
    {
        var assembly = AssemblyDefinition.ReadAssembly(inputPath, new ReaderParameters { ReadWrite = true });
        var module = assembly.MainModule;

        if (_level == ObfuscationLevel.None)
        {
            assembly.Write(outputPath);
            Console.WriteLine($"No obfuscation applied. Output → {outputPath}");
            return;
        }

        if (_level == ObfuscationLevel.High)
        {
            // Create XOR key first, then decrypt method
            InjectXorKey(module);
            InjectStringDecryptor(module);
        }

        foreach (var type in module.Types.ToList())
        {
            ObfuscateType(type);
        }

        assembly.Write(outputPath);
        Console.WriteLine($"Obfuscation ({_level}) complete → {outputPath}");
    }

    private void ObfuscateType(TypeDefinition type)
    {
        if (ShouldSkipType(type)) return;

        if (_level >= ObfuscationLevel.Low)
        {
            type.Name = RandomName();

            if (!string.IsNullOrEmpty(type.Namespace) && !type.Namespace.StartsWith("System"))
                type.Namespace = RandomName();
        }

        foreach (var nested in type.NestedTypes.ToList())
            ObfuscateType(nested);

        foreach (var method in type.Methods.ToList())
        {
            if (_level >= ObfuscationLevel.Medium && !method.IsConstructor && !method.IsRuntimeSpecialName)
                method.Name = RandomName();

            if (_level == ObfuscationLevel.High)
            {
                foreach (var param in method.Parameters)
                    param.Name = RandomName();

                ObfuscateStrings(method);
            }
        }

        if (_level >= ObfuscationLevel.Medium)
        {
            foreach (var field in type.Fields.ToList())
            {
                if (!field.IsLiteral && !field.IsSpecialName)
                {
                    field.Name = RandomName();
                }
            }
        }
    }

    private bool ShouldSkipType(TypeDefinition type) =>
        type.FullName.StartsWith("<") || type.Namespace.StartsWith("System") || type.Name.StartsWith("StringDecryptor");

    private string RandomName()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, 10).Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private void InjectXorKey(ModuleDefinition module)
    {
        // Find or create StringDecryptor type
        var decryptType = module.Types.FirstOrDefault(t => t.Name == "StringDecryptor");
        if (decryptType == null)
        {
            decryptType = new TypeDefinition("", "StringDecryptor",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                module.ImportReference(typeof(object)));
            module.Types.Add(decryptType);
        }

        var keyBytes = new byte[1];
        _random.NextBytes(keyBytes);

        xorKeyField = new FieldDefinition(
            "xorKey",
            FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
            module.TypeSystem.Byte);

        decryptType.Fields.Add(xorKeyField);

        // Static constructor for decryptType
        var cctor = decryptType.Methods.FirstOrDefault(m => m.Name == ".cctor");
        if (cctor == null)
        {
            cctor = new MethodDefinition(".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);

            decryptType.Methods.Add(cctor);
            var il = cctor.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ret));
        }

        var ilProcessor = cctor.Body.GetILProcessor();
        var instructions = cctor.Body.Instructions;
        var retInstruction = instructions.Last(i => i.OpCode == OpCodes.Ret);

        ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Ldc_I4, (int)keyBytes[0]));
        ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Stsfld, xorKeyField));
    }

    private void InjectStringDecryptor(ModuleDefinition module)
    {
        // Find the StringDecryptor type (should already exist from InjectXorKey)
        var decryptType = module.Types.FirstOrDefault(t => t.Name == "StringDecryptor");
        if (decryptType == null)
        {
            throw new InvalidOperationException("StringDecryptor type not found. InjectXorKey should be called first.");
        }

        decryptMethod = new MethodDefinition("Decrypt",
            MethodAttributes.Public | MethodAttributes.Static,
            module.ImportReference(typeof(string)));

        var param = new ParameterDefinition("input", ParameterAttributes.None, module.ImportReference(typeof(string)));
        decryptMethod.Parameters.Add(param);

        var il = decryptMethod.Body.GetILProcessor();

        var charArrayType = module.ImportReference(typeof(char[]));
        var stringCtor = module.ImportReference(
            typeof(string).GetConstructors()
                .First(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(char[]);
                })
        );

        decryptMethod.Body.Variables.Add(new VariableDefinition(charArrayType));
        decryptMethod.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32));
        decryptMethod.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Char));
        decryptMethod.Body.InitLocals = true;

        // Use the xorKeyField that was created in InjectXorKey
        if (xorKeyField == null)
        {
            throw new InvalidOperationException("XOR key field not found. InjectXorKey should be called first.");
        }

        var ret = il.Create(OpCodes.Ret);

        // Add null check at the beginning
        var notNull = il.Create(OpCodes.Ldarg_0);
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Brtrue_S, notNull));
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ret));

        il.Append(notNull);
        il.Append(il.Create(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("ToCharArray", Type.EmptyTypes))));
        il.Append(il.Create(OpCodes.Stloc_0));

        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc_1));

        var loopStart = il.Create(OpCodes.Ldloc_1);
        il.Append(loopStart);

        il.Append(il.Create(OpCodes.Ldloc_0));
        il.Append(il.Create(OpCodes.Ldlen));
        il.Append(il.Create(OpCodes.Conv_I4));

        var endLoop = il.Create(OpCodes.Ldloc_0);
        il.Append(il.Create(OpCodes.Bge_S, endLoop));

        il.Append(il.Create(OpCodes.Ldloc_0));
        il.Append(il.Create(OpCodes.Ldloc_1));
        il.Append(il.Create(OpCodes.Ldelema, module.TypeSystem.Char));

        il.Append(il.Create(OpCodes.Dup));
        il.Append(il.Create(OpCodes.Ldobj, module.TypeSystem.Char));

        il.Append(il.Create(OpCodes.Ldsfld, xorKeyField));

        il.Append(il.Create(OpCodes.Xor));
        il.Append(il.Create(OpCodes.Conv_U2));

        il.Append(il.Create(OpCodes.Stobj, module.TypeSystem.Char));

        il.Append(il.Create(OpCodes.Ldloc_1));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Add));
        il.Append(il.Create(OpCodes.Stloc_1));

        il.Append(il.Create(OpCodes.Br_S, loopStart));

        il.Append(endLoop);
        il.Append(il.Create(OpCodes.Newobj, stringCtor));
        il.Append(ret);

        decryptType.Methods.Add(decryptMethod);
    }

    private void ObfuscateStrings(MethodDefinition method)
    {
        if (!method.HasBody || decryptMethod == null) return;

        var il = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions.ToList(); // Create a copy to avoid modification during iteration

        for (int i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCodes.Ldstr)
            {
                var original = instructions[i].Operand as string;
                if (!string.IsNullOrEmpty(original))
                {
                    var encrypted = EncryptString(original);

                    // Replace the original instruction
                    var newLoadStr = il.Create(OpCodes.Ldstr, encrypted);
                    il.Replace(instructions[i], newLoadStr);

                    // Insert decrypt call after
                    var decryptCall = il.Create(OpCodes.Call, decryptMethod);
                    il.InsertAfter(newLoadStr, decryptCall);
                }
            }
        }
    }

    private string EncryptString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        byte key = GetXorKey();

        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(chars[i] ^ key);
        }
        return new string(chars);
    }

    private byte _xorKeyCache = 0;
    private byte GetXorKey()
    {
        if (_xorKeyCache == 0)
        {
            _xorKeyCache = (byte)_random.Next(1, 255);
        }
        return _xorKeyCache;
    }
}