using dnlib.DotNet;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using dnlib.DotNet.Emit;

namespace SimpleObfuscatorCSharp
{
    public class StringEncrypt
    {

        public static void EncryptStrings(ModuleDefMD module)
        {
            // Load the module containing the StringEncrypt class
            ModuleDef typeModule = ModuleDefMD.Load(typeof(StringEncrypt).Module);

            Console.WriteLine("[+] Injecting decryption method");

            MethodDef decryptMethod = FindAndPrepareDecryptMethod(typeModule);

            if (decryptMethod != null)
            {
                module.GlobalType.Methods.Add(decryptMethod);

                UpdateDecryptMethodBody(decryptMethod);

                Console.WriteLine("[+] Encrypting all strings");

                EncryptAllStrings(module, decryptMethod);
            }
        }

        // Find and prepare the decryption method for injection
        private static MethodDef FindAndPrepareDecryptMethod(ModuleDef typeModule)
        {
            foreach (TypeDef type in typeModule.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Name == "DecryptString")
                    {
                        method.DeclaringType = null;
                        method.Name = "Example123"; // Rename the method
                        method.Parameters[0].Name = "\u0011"; // Obfuscate the parameter name

                        Console.WriteLine("[+] DecryptString found");
                        return method;
                    }
                }
            }
            return null;
        }

        // Update the method's body instructions with encryption keys
        private static void UpdateDecryptMethodBody(MethodDef method)
        {
            foreach (Instruction i in method.Body.Instructions)
            {
                if (i.ToString().Contains("DEFAULT_KEY"))
                {
                    i.Operand = "K34VFiiu1qar95eRRZJ36EqMBxUrfhUWKK8Spqv3l5E=";
                }
                if (i.ToString().Contains("DEFAULT_IV"))
                {
                    i.Operand = "GvcTmDfSETQlYktniF88Kg==";
                }
            }
        }

        // Encrypt all string literals in the module
        private static void EncryptAllStrings(ModuleDefMD module, MethodDef decryptMethod)
        {
            foreach (TypeDef typedef in module.GetTypes().ToList())
            {
                if (!typedef.HasMethods)
                    continue;

                foreach (MethodDef typeMethod in typedef.Methods)
                {
                    if (typeMethod.Body == null || typeMethod.Name == decryptMethod.Name)
                        continue;

                    EncryptMethodStrings(typeMethod, decryptMethod);
                }
            }
        }

        // Encrypt string literals in a single method
        private static void EncryptMethodStrings(MethodDef typeMethod, MethodDef decryptMethod)
        {
            foreach (Instruction instr in typeMethod.Body.Instructions.ToList())
            {
                if (instr.OpCode == OpCodes.Ldstr)
                {
                    int instrIndex = typeMethod.Body.Instructions.IndexOf(instr);

                    // Replace the string operand with the encrypted string
                    typeMethod.Body.Instructions[instrIndex].Operand = EncryptString(typeMethod.Body.Instructions[instrIndex].Operand.ToString());

                    // Insert a call to the decryption method after the encrypted string
                    typeMethod.Body.Instructions.Insert(instrIndex + 1, new Instruction(OpCodes.Call, decryptMethod));
                }
            }

            // Update and optimize the method's body
            typeMethod.Body.UpdateInstructionOffsets();
            typeMethod.Body.OptimizeBranches();
            typeMethod.Body.SimplifyBranches();
        }


        public static string EncryptString(string plaintext)
        {
            byte[] encryptionKey = new byte[]
            {
                0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd6, 0xa6,
                0xab, 0xf7, 0x97, 0x91, 0x45, 0x92, 0x77, 0xe8,
                0x4a, 0x8c, 0x07, 0x15, 0x2b, 0x7e, 0x15, 0x16,
                0x28, 0xaf, 0x12, 0xa6, 0xab, 0xf7, 0x97 ,0x91
            };

            // Hardcoded AES initialization vector (IV) (128 bits = 16 bytes)
            byte[] iv = new byte[]
            {
                0x1a, 0xf7, 0x13, 0x98, 0x37, 0xd2, 0x11, 0x34,
                0x25, 0x62, 0x4b, 0x67, 0x88, 0x5f, 0x3c, 0x2a
            };

            byte[] encrypted;
            string base64_encrypted;
            using (Aes aesAlg = Aes.Create())
            {

                aesAlg.Key = encryptionKey;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using(StreamWriter sw = new StreamWriter(csEncrypt))
                        sw.Write(plaintext);
                        encrypted = msEncrypt.ToArray();

                        //csEncrypt.Write(plaintextBytes, 0, plaintextBytes.Length);
                        //csEncrypt.FlushFinalBlock();
                    }

                    //return ToBase64String(encrypted);
                }
            }
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                base64_encrypted = Convert.ToBase64String(encrypted);
            }
            return base64_encrypted;
        }



        public static string DecryptString(string ciphertext_b64)
        {
            string encryptionKey = "DEFAULT_KEY";
            string iv = "DEFAULT_IV";
            byte[] ciphertext;
            byte[] new_encryptionKey, new_iv;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                ciphertext = Convert.FromBase64String(ciphertext_b64);
                new_encryptionKey = Convert.FromBase64String(encryptionKey);
                new_iv = Convert.FromBase64String(iv);  
            }
            string plaintext;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = new_encryptionKey;
                aesAlg.IV = new_iv;
                aesAlg.Mode = CipherMode.CBC;
                //aesAlg.Padding = PaddingMode.PKCS7;
                
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                byte[] decryptedBytes;
                using (var msDecrypt = new System.IO.MemoryStream(ciphertext))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(csDecrypt))
                        {
                            plaintext = reader.ReadToEnd();
                            //csDecrypt.CopyTo(msPlain);
                            //decryptedBytes = msPlain.ToArray();
                        }
                    }
                }

                return plaintext;
            }
        }

    }
}
