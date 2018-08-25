﻿
/*
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SafeOrbit.Cryptography.Random;
using SafeOrbit.Cryptography.Encryption;
using SafeOrbit.Library;
using SafeOrbit.Memory.SafeBytesServices.DataProtection;
using SafeOrbit.Memory.SafeBytesServices.Factory;
using SafeOrbit.Extensions;
using SafeOrbit.Helpers;

namespace SafeOrbit.Memory.SafeBytesServices.Collection
{
    /// <summary>
    ///     An encrypted collection of <see cref="ISafeByte" /> instances.
    /// </summary>
    /// <seealso cref="ISafeByte" />
    internal class EncryptedSafeByteCollection : ISafeByteCollection
    {
        /// <summary>
        ///     The encryption key to encrypt/decrypt the inner list.
        /// </summary>
        private readonly byte[] _encryptionKey;

        private readonly IFastEncryptor _encryptor;
        private readonly IByteArrayProtector _memoryProtector;
        private readonly ISafeByteFactory _safeByteFactory;
        private readonly IByteIdListSerializer<int> _serializer;
        /// <summary>
        ///     Encrypted inner collection.
        /// </summary>
        private byte[] _encryptedCollection;

        private bool _isDisposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EncryptedSafeByteCollection" /> class.
        /// </summary>
        public EncryptedSafeByteCollection() : this(
            SafeOrbitCore.Current.Factory.Get<IFastEncryptor>(),
            SafeOrbitCore.Current.Factory.Get<IByteArrayProtector>(),
            SafeOrbitCore.Current.Factory.Get<IFastRandom>(),
            SafeOrbitCore.Current.Factory.Get<ISafeByteFactory>(),
            SafeOrbitCore.Current.Factory.Get<IByteIdListSerializer<int>>())
        {
        }

        internal EncryptedSafeByteCollection(IFastEncryptor encryptor, IByteArrayProtector memoryProtector,
            IFastRandom fastRandom, ISafeByteFactory safeByteFactory, IByteIdListSerializer<int> serializer)
        {
            _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
            _memoryProtector = memoryProtector ?? throw new ArgumentNullException(nameof(memoryProtector));
            _safeByteFactory = safeByteFactory ?? throw new ArgumentNullException(nameof(safeByteFactory));
            _serializer = serializer;
            _encryptionKey = fastRandom.GetBytes(_memoryProtector.BlockSizeInBytes);
            _memoryProtector.Protect(_encryptionKey);
        }

        /// <summary>
        ///     Appends the specified <see cref="ISafeByte" /> instance to the inner encrypted collection.
        /// </summary>
        /// <param name="safeByte">The safe byte.</param>
        /// <exception cref="ArgumentNullException"><paramref name="safeByte" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">Throws if the <see cref="EncryptedSafeByteCollection" /> instance is disposed</exception>
        /// <seealso cref="ISafeByte" />
        public void Append(ISafeByte safeByte)
        {
            EnsureNotDisposed();
            if (safeByte == null) throw new ArgumentNullException(nameof(safeByte));
            _memoryProtector.Unprotect(_encryptionKey);
            var list = DecryptAndDeserializeAsync(_encryptedCollection, _encryptionKey).RunSync();
            list.Add(safeByte.Id);
            _encryptedCollection = SerializeAndEncryptAsync(list.ToArray(), _encryptionKey).RunSync();
            list.Clear();
            _memoryProtector.Protect(_encryptionKey);
            Length++;
        }

        /// <summary>
        ///     Gets the byte as <see cref="ISafeByte" /> for the specified index.
        /// </summary>
        /// <param name="index">The position of the byte.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is lower than zero or higher/equals to the
        ///     <see cref="Length" />.
        /// </exception>
        /// <exception cref="InvalidOperationException"><see cref="EncryptedSafeByteCollection" /> instance is empty.</exception>
        /// <exception cref="ObjectDisposedException"><see cref="EncryptedSafeByteCollection" /> instance is disposed</exception>
        /// <seealso cref="ISafeByte" />
        public ISafeByte Get(int index)
        {
            if ((index < 0) && (index >= Length))
                throw new ArgumentOutOfRangeException(nameof(index));
            EnsureNotDisposed();
            EnsureNotEmpty();
            _memoryProtector.Unprotect(_encryptionKey);
            var list = DecryptAndDeserializeAsync(_encryptedCollection, _encryptionKey).RunSync();
            _memoryProtector.Protect(_encryptionKey);
            var id = list.ElementAt(index);
            var safeByte = _safeByteFactory.GetById(id);
            list.Clear();
            return safeByte;
        }

        /// <summary>
        ///     Returns all of the real byte values that <see cref="ISafeByteCollection" /> holds.
        ///     Reveals all protected data in memory.
        /// </summary>
        /// <returns>System.Byte[].</returns>
        /// <exception cref="InvalidOperationException"><see cref="EncryptedSafeByteCollection" /> instance is empty.</exception>
        /// <exception cref="ObjectDisposedException"><see cref="EncryptedSafeByteCollection" /> instance is disposed</exception>
        public byte[] ToDecryptedBytes()
        {
            EnsureNotEmpty();
            EnsureNotDisposed();
            _memoryProtector.Unprotect(_encryptionKey);
            var safeBytesList =
                DecryptAndDeserializeAsync(_encryptedCollection, _encryptionKey).RunSync()
                .Select(id => _safeByteFactory.GetById(id));
            var decryptedBytes = GetAllAndMerge(safeBytesList);
            return decryptedBytes;
        }

        /// <summary>
        ///     Gets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length { get; private set; }

        /// <inheritdoc />
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_encryptedCollection != null) Array.Clear(_encryptedCollection, 0, _encryptedCollection.Length);
            if (_encryptionKey != null) Array.Clear(_encryptionKey, 0, _encryptionKey.Length);
            _isDisposed = true;
            Length = 0;
        }

        private byte[] GetAllAndMerge(IEnumerable<ISafeByte> safeBytes)
        {
            var buffer = new byte[Length];
            Fast.For(0, Length, i =>
            {
                var @byte = safeBytes.ElementAt(i).Get();
                buffer[i] = @byte;
            });
            return buffer;
        }
        private async Task<ICollection<int>> DecryptAndDeserializeAsync(byte[] encryptedCollection, byte[] encryptionKey)
        {
            if (encryptedCollection == null)
                return new List<int>();
            var decryptedBytes = _encryptor.Decrypt(encryptedCollection, encryptionKey);
            try
            {
                var deserializedBytes = await _serializer.DeserializeAsync(decryptedBytes);
                return deserializedBytes.ToList();
            }
            finally
            {
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            }
        }

        private async Task<byte[]> SerializeAndEncryptAsync(IReadOnlyCollection<int> safeByteIdList, byte[] encryptionKey)
        {
            var serializedBytes = await _serializer.SerializeAsync(safeByteIdList);
            try
            {
                var encrypted = await _encryptor.EncryptAsync(serializedBytes, encryptionKey);
                return encrypted;
            }
            finally
            {
                Array.Clear(serializedBytes, 0, serializedBytes.Length);
            }
        }

      

        /// <exception cref="ObjectDisposedException">Throws if the <see cref="EncryptedSafeByteCollection" /> instance is disposed</exception>
        private void EnsureNotDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(EncryptedSafeByteCollection));
        }

        /// <exception cref="InvalidOperationException">Throws if the <see cref="EncryptedSafeByteCollection" /> instance is empty.</exception>
        private void EnsureNotEmpty()
        {
            if (Length == 0) throw new InvalidOperationException($"{nameof(SafeBytes)} is empty");
        }
    }
}