using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Devices.Sensors;

namespace IG.Crypto
{


    public interface IHashCalculator
    {

    }

    public class HashConst
    {

        public static string MD5Hash { get; } = HashAlgorithmName.MD5.Name;

        public static string SHA1Hash { get; } = HashAlgorithmName.SHA1.Name;

        public static string SHA256Hash { get; } = HashAlgorithmName.SHA256.Name;

        public static string SHA384Hash { get; } = HashAlgorithmName.SHA384.Name;

        public static string SHA512Hash { get; } = HashAlgorithmName.SHA512.Name;

        public static string HashToHexString(Byte[] bytes, bool capitalize=false)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes), "Cannot convert null byte array to hexadecimal string.");
            string ret = BitConverter.ToString(bytes).Replace("-", "");
            if (capitalize)
                return ret.ToUpper();
            else return ret.ToLower();
        }

    }

    public class HashResults: HashConst
    {
        
        private IDictionary<string, string> Hashes { get; } = new Dictionary<string, string>();

        public void AddHash(string hashName, string hashValue)
        {
            if (string.IsNullOrEmpty(hashName))
                throw new ArgumentException("Name of the hash function cannot be null or empty string.");
            if (string.IsNullOrEmpty(hashValue))
                throw new ArgumentException("Hash function value cannot be null or empty string.");

        }

    }

    public class HashCalculator: HashCalculatorBase, IHashCalculator
    {

        public HashCalculator() : base()
        {
            InstallStandardHashAlgorithms(this);
        }

    }

    public class HashCalculatorBase : HashConst, IHashCalculator
    {

        public HashCalculatorBase() : base()
        {
        }

        public static void InstallStandardHashAlgorithms(HashCalculatorBase hashCalculator)
        {
            hashCalculator.SetHashAlgorithm(MD5Hash, MD5.Create());
            hashCalculator.SetHashAlgorithm(SHA1Hash, System.Security.Cryptography.SHA1.Create());
            hashCalculator.SetHashAlgorithm(SHA256Hash, System.Security.Cryptography.SHA256.Create());
            hashCalculator.SetHashAlgorithm(SHA384Hash, System.Security.Cryptography.SHA384.Create());
            hashCalculator.SetHashAlgorithm(SHA512Hash, System.Security.Cryptography.SHA512.Create());

        }

        private Dictionary<string, HashAlgorithm> HashAlgorithms = new Dictionary<string, HashAlgorithm>();

        public virtual void SetHashAlgorithm(string hashName, HashAlgorithm algorithm, bool allowResetting = false)
        {
            if (allowResetting && HashAlgorithms.ContainsKey(hashName))
                throw new ArgumentException("Modifying hash algorithm is not allowed.",nameof(hashName));
            HashAlgorithms[hashName] = algorithm;
        }

        public virtual HashAlgorithm GetHashAlgorithm(string hashName)
        {
            if (!HashAlgorithms.ContainsKey(hashName))
                throw new ArgumentException($"Do not have hash computation algorithm corresponding to hash name {hashName}", hashName);
            return HashAlgorithms[hashName];
        }


        public virtual byte[] CalculateHash(string hashName, Byte[] bytes)
        {
            return GetHashAlgorithm(hashName).ComputeHash(bytes);
        }

        public virtual string CalculateHashString(string hashName, Byte[] bytes)
        {
            return HashToHexString(CalculateHash(hashName, bytes));
        }

        public virtual byte[] CalculateFileHash(string hashName, string filePath)
        {
            using (Stream fileStream = File.OpenRead(filePath))
            {
                return GetHashAlgorithm(hashName).ComputeHash(fileStream);
            }
        }

        public virtual string CalculateFileHashString(string hashName, string filePath)
        {
            return HashToHexString(CalculateFileHash(hashName, filePath));
        }


        public virtual byte[] CalculateTextHash(string hashName, string text)
        {
            return CalculateTextHash(hashName, text, Encoding.UTF8);
        }

        public virtual byte[] CalculateTextHash(string hashName, string text, Encoding encoding)
        {
            Byte[] bytes = encoding.GetBytes(text);
            return CalculateHash(hashName, bytes);

        }

        public virtual string CalculateTextHashString(string hashName, string text)
        {
            return HashToHexString(CalculateTextHash(hashName, text));
        }

        public virtual string CalculateTextHashString(string hashName, string text, Encoding encoding)
        {
            return HashToHexString(CalculateTextHash(hashName, text, encoding));
        }

    }

}

