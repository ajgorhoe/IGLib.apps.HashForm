using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
//using Windows.Services.Maps;
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

        public static void InstallStandardHashAlgorithms(HashCalculatorBase hashCalculator)
        {
            hashCalculator.RegisterHashAlgorithmFactoryMethod(MD5Hash, () => { return MD5.Create(); });
            hashCalculator.RegisterHashAlgorithmFactoryMethod(SHA1Hash, () => { return SHA1.Create(); });
            hashCalculator.RegisterHashAlgorithmFactoryMethod(SHA256Hash, () => { return SHA256.Create(); });
            hashCalculator.RegisterHashAlgorithmFactoryMethod(SHA384Hash, () => { return SHA384.Create(); });
            hashCalculator.RegisterHashAlgorithmFactoryMethod(SHA512Hash, () => { return SHA512.Create(); });

        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class HashCalculatorBase : HashConst, IHashCalculator
    {

        public HashCalculatorBase() : base()
        {
        }

        protected object Lock { get; } = new object();

        protected Dictionary<string, Func<HashAlgorithm>> HashAlgorithms { get; }
            = new Dictionary<string, Func<HashAlgorithm>>();


        /// <summary>Registers the specified hashing algorithm creator function under the specified name.
        /// <para>Thread safe.</para></summary>
        /// <param name="hashName">Name under which the new algoeithm is registered on the current hashing calculator.</param>
        /// <param name="algorithmCreate">The algorithm registered.</param>
        /// <param name="allowRedefinition">If true then overriding an already rehistered alhorithm is allowed. If not then attempt
        /// to registering a different algorithm under the same name will throw an exception.</param>
        /// <exception cref="ArgumentNullException">Thrown when provided alhorithm is null.</exception>
        /// <exception cref="ArgumentException">Thrown when algorithm name is null or empty string or when attempt is made to
        /// register a different algorithm under the name that is already used, while parameter <paramref name="allowRedefinition"/> is false.</exception>
        public virtual void RegisterHashAlgorithmFactoryMethod(
            string hashName, Func<HashAlgorithm> algorithmCreate, bool allowRedefinition = false)
        {
            if (string.IsNullOrEmpty(hashName))
            {
                throw new ArgumentException("Provided algorithm name is null or empty string.", nameof(hashName));
            }
            if (algorithmCreate == null)
            {
                throw new ArgumentNullException(nameof(algorithmCreate), $@"Attempt to register null hashing algorithm under name ""{hashName}"".");
            }
            lock (Lock)
            {
                if (!allowRedefinition && HashAlgorithms.ContainsKey(hashName))
                {
                    if (HashAlgorithms[hashName] == algorithmCreate)
                    {
                        // The same algorithm already registered under this name:
                        return;
                    }
                    throw new ArgumentException("Modifying hash algorithm is not allowed.", nameof(hashName));
                }
                HashAlgorithms[hashName] = algorithmCreate;
            }
        }

        /// <summary>Removes the specified registration of hashing algorithm.</summary>
        /// <param name="hashName">Name of registered hash algorithm whose registration is removed.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hashName"/> is null or empty string.</exception>
        public virtual void UnregisterHashAlgorithmCreationMethod(string hashName)
        {
            if (string.IsNullOrEmpty(hashName))
            {
                throw new ArgumentException("Provided algorithm name is null or empty string.", nameof(hashName));
            }
            lock (Lock)
            {
                if (HashAlgorithms.ContainsKey(hashName))
                {
                    HashAlgorithms.Remove(hashName);
                }
            }
        }

        /// <summary>Returns the function that creates a new hash calculation algorithm.
        /// <para>Werning: Algorithm created needs to be disposed of when not sed any more.</para></summary>
        /// <param name="hashName">Name underr which the desired hash algorithm is registered.</param>
        /// <returns>A function whos call returns a newly creaded hashing algorithm of the type corresponding to <paramref name="hashName"/>.
        /// <see cref="HashAlgorithm"/> objects that will be created by calling this function need to be disposed of afterr use.</returns>
        /// <exception cref="ArgumentException">Thrown when algorithm name (parameter <paramref name="hashName"/>) is null or empty string.</exception>
        protected virtual Func<HashAlgorithm> GetHashAlgorithmFactoryMethod(string hashName)
        {
            lock (Lock)
            {
                if (!HashAlgorithms.ContainsKey(hashName))
                    throw new ArgumentException($"Do not have hash computation algorithm corresponding to hash name {hashName}", hashName);
                return HashAlgorithms[hashName];
            }
        }



        // Synchronous hash calculation:


        // Synchronous hash calculation on streams and files:

        public virtual byte[] CalculateHash(string hashName, Stream inputStream)
        {
            using (HashAlgorithm hashAlgorithm = GetHashAlgorithmFactoryMethod(hashName)())
            {
                if (hashAlgorithm == null)
                {
                    throw new InvalidOperationException($@"Could not retrieve the hashing algorithm named ""{hashName}"".");
                }
                return hashAlgorithm.ComputeHash(inputStream);
            }
        }

        public virtual string CalculateHashString(string hashName, Stream inputStream)
        {
            return HashToHexString(CalculateHash(hashName, inputStream));
        }


        public virtual byte[] CalculateFileHash(string hashName, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("The specified path of the file to be hashed is null or empty string.");
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File to be hashed does not exist: " + Environment.NewLine + $@"""{filePath}""");
            }
            using (Stream fileStream = File.OpenRead(filePath))
            {
                return CalculateHash(hashName, fileStream);
            }
        }

        public virtual string CalculateFileHashString(string hashName, string filePath)
        {
            return HashToHexString(CalculateFileHash(hashName, filePath));
        }


        // Synchronous hash calculation on bytes or text: 

        public virtual byte[] CalculateHash(string hashName, Byte[] bytes)
        {
            using (HashAlgorithm hashAlgorithm = GetHashAlgorithmFactoryMethod(hashName)())
            {
                if (hashAlgorithm == null)
                {
                    throw new InvalidOperationException($@"Could not retrieve the hashing algorithm named ""{hashName}"".");
                }
                return hashAlgorithm.ComputeHash(bytes);
            }
        }

        public virtual string CalculateHashString(string hashName, Byte[] bytes)
        {
            return HashToHexString(CalculateHash(hashName, bytes));
        }


        public virtual byte[] CalculateTextHash(string hashName, string text, Encoding encoding)
        {
            Byte[] bytes = encoding.GetBytes(text);
            return CalculateHash(hashName, bytes);

        }

        public virtual string CalculateTextHashString(string hashName, string text, Encoding encoding)
        {
            return HashToHexString(CalculateTextHash(hashName, text, encoding));
        }


        public virtual Encoding DefaultEncoding { get; } = Encoding.UTF8;

        // Default parameter for encoding:

        public virtual byte[] CalculateTextHash(string hashName, string text)
        {
            return CalculateTextHash(hashName, text, DefaultEncoding);
        }

        public virtual string CalculateTextHashString(string hashName, string text)
        {
            return HashToHexString(CalculateTextHash(hashName, text));
        }


        // Asynchronous methods:



        // Asynchronous hash calculation on streams and files:

        public virtual async Task<byte[]> CalculateHashAsync(string hashName, Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            using (HashAlgorithm hashAlgorithm = GetHashAlgorithmFactoryMethod(hashName)())
            {
                if (hashAlgorithm == null)
                {
                    throw new InvalidOperationException($@"Could not retrieve the hashing algorithm named ""{hashName}"".");
                }
                return await hashAlgorithm.ComputeHashAsync(inputStream, cancellationToken);
            }
        }

        public virtual async Task<string> CalculateHashStringAsync(string hashName, Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            return HashToHexString( await CalculateHashAsync(hashName, inputStream, cancellationToken) );
        }



        public virtual async Task<byte[]> CalculateFileHashAsync(string hashName, string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("The specified path of the file to be hashed is null or empty string.");
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File to be hashed does not exist: " + Environment.NewLine + $@"""{filePath}""");
            }
            using (Stream fileStream = File.OpenRead(filePath))
            {
                return await CalculateHashAsync(hashName, fileStream, cancellationToken);
            }
        }

        public virtual async Task<string> CalculateFileHashStringAsync(string hashName, string filePath,
            CancellationToken cancellationToken = default)
        {
            return HashToHexString( await CalculateFileHashAsync(hashName, filePath, cancellationToken) );
        }



        // Asynchronous hash calculation on bytes or text: 



        public virtual async Task<byte[]> CalculateHashAsync(string hashName, Byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            using (Stream inputStream = new MemoryStream(bytes))
            {
                // A Stream was created for byte arrray in order to enabl use of async method on very long byte arrays.
                return await CalculateHashAsync(hashName, inputStream, cancellationToken);
            }
        }

        public virtual async Task<string> CalculateHashStringAsync(string hashName, Byte[] bytes, 
            CancellationToken cancellationToken = default)
        {
            return HashToHexString( await CalculateHashAsync(hashName, bytes, cancellationToken) );
        }







        public virtual async Task<byte[]> CalculateTextHashAsync(string hashName, string text, Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            Byte[] bytes = encoding.GetBytes(text);
            return await CalculateHashAsync(hashName, bytes);
        }

        public virtual async Task<string> CalculateTextHashStringAsync(string hashName, string text, Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            return HashToHexString( await CalculateTextHashAsync(hashName, text, encoding) );
        }



        // Default parameter for encoding: 

        public virtual async Task<byte[]> CalculateTextHashAsync(string hashName, string text,
            CancellationToken cancellationToken = default)
        {
            return await CalculateTextHashAsync(hashName, text, Encoding.UTF8, cancellationToken);
        }



        public virtual async Task<string> CalculateTextHashStringAsync(string hashName, string text,
            CancellationToken cancellationToken = default)
        {
            return HashToHexString( await CalculateTextHashAsync(hashName, text, cancellationToken) );
        }




    }


}

