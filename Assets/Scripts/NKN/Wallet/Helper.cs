using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NKN.Wallet
{
    public static class Helper
    {
        public static byte[] HexToBytes(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new byte[0];
            }

            if (value.Length % 2 == 1)
            {
                throw new FormatException();
            }

            var result = new byte[value.Length / 2];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            }

            return result;
        }

        public static string ToHexString(this IEnumerable<byte> value)
        {
            var sb = new StringBuilder();
            foreach (var b in value)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            return sb.ToString();
        }
    }
}