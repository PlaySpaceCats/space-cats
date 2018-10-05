using System;
using System.Security.Cryptography;
using System.Text;

namespace NKN.Client.Crypto
{
    public class Tools
    {
        private static readonly RandomNumberGenerator randomNumberGenerator = new RNGCryptoServiceProvider();

        private static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static byte[] GenPID(long? timestamp = null)
        {
            if (timestamp == null)
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            var nonce = new byte[32];
            randomNumberGenerator.GetBytes(nonce);
            var sha256 = new SHA256Managed();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(timestamp + BytesToHex(nonce)));
        }
    }
}