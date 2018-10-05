using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using UnityEngine;
using WebSocketSharp;
using RPC = NKN.Client.RPC;

namespace NKN.Wallet
{
    public class Account
    {
        private static X9ECParameters curve = NistNamedCurves.GetByName("P-256");
        private static ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        public byte[] PrivateKey;
        public byte[] PublicKey;
        public string Name;

        private static byte[] GenerateRandom()
        {
            var data = new byte[32];
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(data);
            }

            return data;
        }

        public static Account GetOrCreate()
        {
            return Get() ?? Create();
        }

        public static Account Get()
        {
            var privateKey = PlayerPrefs.GetString("privateKey");
            if (privateKey.IsNullOrEmpty())
            {
                return null;
            }
            var account = Create(privateKey.HexToBytes());
            var name = PlayerPrefs.GetString("name");
            if (!name.IsNullOrEmpty())
            {
                account.Name = name;
            }
            return account;
        }

        public static Account Create()
        {
            return Create(GenerateRandom());
        }

        public static Account Create(byte[] privateKey)
        {
            var account = new Account();
            account.PrivateKey = privateKey;
            var publicKey = curve.G.Multiply(new BigInteger(privateKey));
            account.PublicKey = publicKey.GetEncoded(true);
            PlayerPrefs.SetString("privateKey", privateKey.ToHexString());
            PlayerPrefs.Save();
            return account;
        }

        private byte[] Sign(byte[] data)
        {
            var signer = SignerUtilities.GetSigner("SHA256withECDSA");
            signer.Init(true, new ECPrivateKeyParameters(new BigInteger(PrivateKey), domain));
            signer.BlockUpdate(data, 0, data.Length);
            var signature = signer.GenerateSignature();
            var seq = (Asn1Sequence) Asn1Object.FromByteArray(signature);
            var r = ((DerInteger) seq[0]).Value.ToByteArrayUnsigned();
            var s = ((DerInteger) seq[1]).Value.ToByteArrayUnsigned();
            var result = new byte[64];
            Array.Copy(r, result, 32);
            Array.Copy(s, 0, result, 32, 32);
            return result;
        }

        public async Task<bool> RegisterName(string addr, string name)
        {
            byte[] tx;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((byte) 80);
                    writer.Write((byte) 0);
                    writer.Write((byte) 33);
                    writer.Write(PublicKey);
                    writer.Write(name);
                    writer.Write((byte) 1);
                    writer.Write((byte) 0);
                    writer.Write((byte) 32);
                    writer.Write(GenerateRandom());
                    writer.Write((byte) 0);
                    writer.Write((byte) 0);
                    var signature = Sign(stream.ToArray());
                    writer.Write((byte) 1);
                    writer.Write((byte) 65);
                    writer.Write((byte) 64);
                    writer.Write(signature);
                    writer.Write((byte) 35);
                    writer.Write((byte) 33);
                    writer.Write(PublicKey);
                    writer.Write((byte) 172);
                    tx = stream.ToArray();
                }
            }

            var txHash = (await RPC.Call(addr, "sendrawtransaction", new RPC.SendRawTxParams(tx.ToHexString()))).result;
            Debug.Log(txHash);
            if (txHash == null)
            {
                return false;
            }
            RPC.Response response = null;
            do
            {
                await Task.Delay(10000);
                Debug.Log("wait");
                response = await RPC.Call(addr, "gettransaction", new RPC.GetTxParams(txHash));
                if(response?.error.code == -42002)
                {
                    return false;
                }
            } while (response.result == null);
            Name = name;
            PlayerPrefs.SetString("name", name);
            PlayerPrefs.Save();
            return true;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey("privateKey");
            PlayerPrefs.DeleteKey("name");
            PlayerPrefs.Save();
        }
    }
}