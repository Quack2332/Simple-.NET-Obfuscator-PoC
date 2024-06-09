using System;
using dnlib.DotNet;
using System.Reflection;
using System.Runtime.CompilerServices;
using dnlib.DotNet.Writer;
using Obfu_Net;


namespace SimpleObfuscatorCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Obfu_Net.exe <path_to_binary>");
                return;
            }

            string filePath = args[0].ToString();
            string obfPath = filePath + "._obf.exe";

            try
            {
                ModuleDefMD moduleDef = ModuleDefMD.Load(filePath);
                Assembly Default_Assembly;
                Default_Assembly = System.Reflection.Assembly.UnsafeLoadFrom(filePath);
                AssemblyDef Assembly = moduleDef.Assembly;
                Console.WriteLine("[+] Loaded " + filePath);
                StringEncrypt.EncryptStrings(moduleDef);
                MethodNameChange.ChangeMethodNames(moduleDef, Default_Assembly);


                SaveToFile(moduleDef, obfPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void SaveToFile(ModuleDefMD moduleDef, string path)
        {
            ModuleWriterOptions moduleWriterOption = new ModuleWriterOptions(moduleDef);
            moduleWriterOption.MetadataOptions.Flags = moduleWriterOption.MetadataOptions.Flags | MetadataFlags.KeepOldMaxStack;
            moduleWriterOption.Logger = DummyLogger.NoThrowInstance;
            moduleDef.Write(path, moduleWriterOption);
        }
    }
}