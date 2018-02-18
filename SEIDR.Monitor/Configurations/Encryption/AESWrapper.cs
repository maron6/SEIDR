using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Ryan_UtilityCode.Dynamics.Configurations.Encryption
{
    /// <summary>
    /// <see href="stackoverflow.com/questions/10168240/encrypting-decrypting-a-string-in-c-sharp">Code source for wrapper</see>
    /// </summary>
    public static class AESWrapper
    {
        private const int KeySize = 256;
        private const int DerivationIterations = 1000;
        /// <summary>
        /// Encrypts the string. Note: this shouldn't really be considered especially secure, although it does use
        /// classes and methods out of System.Security.Cryptography
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="password">Password for encrypting and decrypting the Text</param>
        /// <returns>Encrypted version of Text</returns>
        public static string Encrypt(this string Text, string password)
        {
            var keySaltBytes = GenerateEntropy();
            var ivStringBytes = GenerateEntropy();
            var textBytes = Encoding.UTF8.GetBytes(Text);
            using(var pw = new Rfc2898DeriveBytes(password, keySaltBytes, DerivationIterations))
            {
                var keyBytes = pw.GetBytes(KeySize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using(var memoryStream = new MemoryStream())
                        {
                            using(var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(textBytes, 0, textBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                var cipherTextBytes = keySaltBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
            
        }
        /// <summary>
        /// Decrypts the encrypted text
        /// </summary>
        /// <param name="EncryptedText"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Decrypt(this string EncryptedText, string password)
        {
            var cipherTextBytesWithSaltAndIV = Convert.FromBase64String(EncryptedText);

            var saltBytes = cipherTextBytesWithSaltAndIV.Take(KeySize / 8).ToArray();
            var ivBytes = cipherTextBytesWithSaltAndIV.Skip(KeySize / 8).Take(KeySize / 8).ToArray();

            var EncryptedBytes = cipherTextBytesWithSaltAndIV.Skip(KeySize / 4)
                .Take(cipherTextBytesWithSaltAndIV.Length - (KeySize / 4)).ToArray();
            using (var pw = new Rfc2898DeriveBytes(password, saltBytes, DerivationIterations))
            {
                var keyBytes = pw.GetBytes(KeySize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivBytes))
                    {
                        using (var ms = new MemoryStream(EncryptedBytes))
                        {
                            using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                            {
                                var plainText = new byte[EncryptedBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainText, 0, plainText.Length);
                                ms.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainText, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }

        }
        private static byte[] GenerateEntropy()
        {
            var randomBytes = new byte[32];
            using(var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}
