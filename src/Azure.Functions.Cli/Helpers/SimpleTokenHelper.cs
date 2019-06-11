using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Azure.Functions.Cli.Helpers
{
    public static class SimpleTokenHelper
    {
        public static string CreateToken(DateTime validUntil, string authkey, byte[] key = null) => Encrypt(authkey, $"exp={validUntil.Ticks}", key);

        internal static string Encrypt(string authkey, string value, byte[] key = null)
        {
            if (key == null)
            {
                TryGetEncryptionKey(authkey, out key);
            }

            using (var aes = new AesManaged { Key = key })
            {
                // IV is always generated for the key every time
                aes.GenerateIV();
                var input = Encoding.UTF8.GetBytes(value);
                var iv = Convert.ToBase64String(aes.IV);

                using (var encrypter = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        binaryWriter.Write(input);
                        cryptoStream.FlushFinalBlock();
                    }

                    // return {iv}.{swt}.{sha236(key)}
                    return string.Format("{0}.{1}.{2}", iv, Convert.ToBase64String(cipherStream.ToArray()), GetSHA256Base64String(aes.Key));
                }
            }
        }

        private static string GetSHA256Base64String(byte[] key)
        {
            using (var sha256 = new SHA256Managed())
            {
                return Convert.ToBase64String(sha256.ComputeHash(key));
            }
        }

        private static bool TryGetEncryptionKey(string authkey, out byte[] encryptionKey, bool throwIfFailed = true)
        {
            encryptionKey = null;
            var hexOrBase64 = authkey;
            encryptionKey = hexOrBase64.ToKeyBytes();
            return true;
        }

        public static byte[] ToKeyBytes(this string hexOrBase64)
        {
            // only support 32 bytes (256 bits) key length
            if (hexOrBase64.Length == 64)
            {
                return Enumerable.Range(0, hexOrBase64.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(hexOrBase64.Substring(x, 2), 16))
                    .ToArray();
            }

            return Convert.FromBase64String(hexOrBase64);
        }
    }
}
