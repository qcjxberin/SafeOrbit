﻿/*
MIT License

Copyright (c) 2016-2018 Erkin Ekici - undergroundwires@safeorb.it

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SafeOrbit.Cryptography.Encryption.Kdf;
using SafeOrbit.Cryptography.Random;
using SafeOrbit.Exceptions;
using SafeOrbit.Extensions;
using SafeOrbit.Helpers;
using SafeOrbit.Library;

namespace SafeOrbit.Cryptography.Encryption
{
    /// <summary>
    ///     <p><see cref="AesEncryptor" /> encryption implementation is designed to be slow.</p>
    ///     <p>
    ///         Use it if you prefer stronger security over performance. Prefer <see cref="BlowfishEncryptor" /> if you want a
    ///         faster
    ///         alternative.
    ///     </p>
    ///     <p>
    ///         An implementation of AES 512 using a slow <see cref="IKeyDerivationFunction" /> so as to frustrate
    ///         brute-force attack or dictionary attack on the password or passphrase input value.
    ///     </p>
    /// </summary>
    /// <example>
    ///     <code>
    /// </code>
    /// </example>
    /// <seealso cref="Padding" />
    /// <seealso cref="Mode" />
    /// <seealso cref="KeySize" />
    /// <seealso cref="IKeyDerivationFunction" />
    /// <seealso cref="ISafeEncryptor" />
    public class AesEncryptor : EncryptorBase, ISafeEncryptor
    {
        /// <summary>
        ///     Gets the padding to apply when the message data block is shorter than the full number of bytes needed for a
        ///     cryptographic
        ///     operation.
        /// </summary>
        public const PaddingMode Padding = PaddingMode.PKCS7;

        /// <summary>
        ///     Gets the mode of the encryption operation.
        /// </summary>
        public const CipherMode Mode = CipherMode.CBC;

        /// <summary>
        ///     Gets or sets the size, in bits, of the secret key.
        /// </summary>
        /// <remarks>
        ///     The minimum size of the key is 128 bits, and the maximum size is 256 bits.
        /// </remarks>
        public const int KeySize = 256;

        /// <summary>
        ///     The key derivation function to strengthen the key.
        /// </summary>
        private readonly IKeyDerivationFunction _keyDeriver;

        public AesEncryptor() : this(SafeOrbitCore.Current.Factory.Get<IKeyDerivationFunction>())
        {
        }

        public AesEncryptor(IKeyDerivationFunction keyDeriver) : this(
            keyDeriver,
            SafeOrbitCore.Current.Factory.Get<IFastRandom>())
        {
        }

        public AesEncryptor(IKeyDerivationFunction keyDeriver, IFastRandom fastRandom) : base(fastRandom)
        {
            _keyDeriver = keyDeriver ?? throw new ArgumentNullException(nameof(keyDeriver));
        }

        /// <summary>
        ///     <p>Gets the size of the block in bits.</p>
        ///     <p>The block size for AES is always 128 bits.</p>
        /// </summary>
        /// <value>The size of the block in bits.</value>
        /// <remarks>https://en.wikipedia.org/wiki/Advanced_Encryption_Standard</remarks>
        public override int BlockSize { get; } = 128;

        /// <summary>
        ///     <see cref="AesEncryptor" /> can function with any positive integer as it uses <see cref="IKeyDerivationFunction" />
        ///     to generate a key with length of <see cref="KeySize" />.
        /// </summary>
        /// <value>The minimum size of the key.</value>
        public override int MinKeySize { get; } = 8;

        /// <summary>
        ///     <see cref="AesEncryptor" /> can function with any positive integer as it uses <see cref="IKeyDerivationFunction" />
        ///     to generate a key with length of <see cref="KeySize" />.
        /// </summary>
        /// <value>The maximum size of the key.</value>
        public override int MaxKeySize { get; } = int.MaxValue;

        public override int IvSize { get; } = 16;

        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="salt" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="KeySizeException">
        ///     Length of the <paramref name="key" /> must be between <see cref="MinKeySize" /> and
        ///     <see cref="MaxKeySize" /> values.
        /// </exception>
        public byte[] Encrypt(byte[] input, byte[] key, byte[] salt)
            => EncryptAsync(input, key, salt).RunSync();

        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="salt" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="KeySizeException">
        ///     Length of the <paramref name="key" /> must be between <see cref="MinKeySize" /> and
        ///     <see cref="MaxKeySize" /> values.
        /// </exception>
        public byte[] Decrypt(byte[] input, byte[] key, byte[] salt)
            => DecryptAsync(input, key, salt).RunSync();

        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="salt" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="KeySizeException">
        ///     Length of the <paramref name="key" /> must be between <see cref="MinKeySize" /> and
        ///     <see cref="MaxKeySize" /> values.
        /// </exception>
        public Task<byte[]> EncryptAsync(byte[] input, byte[] key, byte[] salt)
        {
            EnsureKeyParameter(key);
            EnsureParameters(input, key, salt);
            return InternalEncryptAsync(input, key, salt);
        }

        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="salt" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="KeySizeException">
        ///     Length of the <paramref name="key" /> must be between <see cref="MinKeySize" /> and
        ///     <see cref="MaxKeySize" /> values.
        /// </exception>
        public Task<byte[]> DecryptAsync(byte[] input, byte[] key, byte[] salt)
        {
            EnsureParameters(input, key, salt);
            return InternalDecryptAsync(input, key, salt);
        }

        /// <summary>
        ///     Ensures the parameters are valid.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="salt" /> is <see langword="null" /> or empty.</exception>
        /// <exception cref="KeySizeException">
        ///     Length of the <paramref name="key" /> must be between <see cref="MinKeySize" /> and
        ///     <see cref="MaxKeySize" /> values.
        /// </exception>
        private void EnsureParameters(byte[] input, byte[] key, byte[] salt)
        {
            if ((key == null) || !key.Any()) throw new ArgumentNullException(nameof(key));
            if ((salt == null) || !salt.Any()) throw new ArgumentNullException(nameof(salt));
            if ((input == null) || !input.Any()) throw new ArgumentNullException(nameof(input));
            EnsureKeyParameter(key);
        }

        private async Task<byte[]> InternalEncryptAsync(byte[] input, byte[] key, byte[] salt)
        {
            byte[] iv = null;
            byte[] encryptedContent = null;
            var derivedKey = _keyDeriver.Derive(key, salt, KeySize/8);
            using (var ms = new MemoryStream())
            {
                using (var algorithm = GetAlgorithm(derivedKey))
                {
                    iv = GetIv(); //Use the random generated iv created by AesManaged
                    using (var encryptor = algorithm.CreateEncryptor(derivedKey, iv))
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            await cs.WriteAsync(input, 0, input.Length);
                        }
                    }
                }
                encryptedContent = ms.ToArray();
            }
            //Create new byte array that should contain both unencrypted iv and encrypted data
            var result = iv.Combine(encryptedContent);
            return result;
        }

        private async Task<byte[]> InternalDecryptAsync(byte[] input, byte[] key, byte[] salt)
        {
            var iv = new byte[IvSize];
            var encryptedContent = new byte[input.Length - IvSize];
            Buffer.BlockCopy(input, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(input, iv.Length, encryptedContent, 0, encryptedContent.Length);
            var derivedKey = _keyDeriver.Derive(key, salt, KeySize/8);
            using (var ms = new MemoryStream())
            {
                using (var algorithm = GetAlgorithm(derivedKey))
                {
                    using (var decryptor = algorithm.CreateDecryptor(derivedKey, iv))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                        {
                            await cs.WriteAsync(encryptedContent, 0, encryptedContent.Length);
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        ///     Derives the <paramref name="key" /> and gets the cryptographic algorithm.
        /// </summary>
        /// <param name="key">The key that'll be derived.</param>
        /// <seealso cref="Padding" />
        /// <seealso cref="Mode" />
        /// <seealso cref="KeySize" />
        private SymmetricAlgorithm GetAlgorithm(byte[] key)
        {
            var algorithm = Aes.Create();
            algorithm.Mode = Mode;
            algorithm.Padding = Padding;
            algorithm.BlockSize = BlockSize;
            algorithm.KeySize = KeySize;
            algorithm.Key = key;
            return algorithm;
        }
    }
}