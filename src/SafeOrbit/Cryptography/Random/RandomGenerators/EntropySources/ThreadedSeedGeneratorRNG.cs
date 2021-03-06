﻿
/*
MIT License

Copyright (c) 2016 Erkin Ekici - undergroundwires@safeorb.it

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
using System.Security.Cryptography;
using System.Threading;
using SafeOrbit.Cryptography.Random.RandomGenerators.Crypto.Prng;
using SafeOrbit.Memory;

namespace SafeOrbit.Cryptography.Random.RandomGenerators
{
    /// <summary>
    ///     A simple wrapper around BouncyCastle ThreadedSeedGenerator. Starts a thread in a tight increment loop,
    ///     while another thread samples the variable being incremented.  Entropy is generated by the OS thread
    ///     scheduler, not knowing how many times the first thread will loop in the period of time the second thread loops
    ///     once.
    ///     It is recommended to use ThreadedSeedGeneratorRNG as one of the entropy sources, but not all by itself,
    ///     because thread scheduling is deterministically controlled by your OS, and easily influenced by outsiders.
    /// </summary>
    public sealed class ThreadedSeedGeneratorRng : RandomNumberGenerator
    {
        /// <summary>
        ///     ThreadedSeedGeneratorRNG will always try to fill up to MaxPoolSize bytes available for read
        /// </summary>
        public static int MaxPoolSize { get; }

        private static readonly object FifoStreamLock = new object();
        private static readonly SafeMemoryStream SafeStream = new SafeMemoryStream();
        private static readonly AutoResetEvent PoolFullAre = new AutoResetEvent(false);
        private static readonly ThreadedSeedGenerator MyThreadedSeedGenerator = new ThreadedSeedGenerator();

        // Interlocked cannot handle bools.  So using int as if it were bool.
        private const int TrueInt = 1;
        private const int FalseInt = 0;
        private int _disposed = FalseInt;

        static ThreadedSeedGeneratorRng()
        {
            MaxPoolSize = 4096;
            var mainThread = new Thread(MainThreadLoop)
            {
                IsBackground = true // Don't prevent application from dying if it wants to.
            };
            mainThread.Start();
        }

        private static int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var pos = offset;
                lock (FifoStreamLock)
                {
                    while (pos < count)
                    {
                        var readCount = SafeStream.Length; // All the available bytes
                        if (pos + readCount >= count)
                            readCount = count - pos; // Don't try to read more than we need
                        if (readCount > 0)
                        {
                            var bytesRead = SafeStream.Read(buffer, pos, (int) readCount);
                            pos += bytesRead;
                        }
                        if (pos < count)
                            if (pos < count)
                                Thread.Sleep(1);
                    }
                    return count;
                }
            }
            finally
            {
                PoolFullAre.Set();
            }
        }

        public static byte[] GetAvailableBytes(int maxLength)
        {
            lock (FifoStreamLock)
            {
                var availBytesCount = SafeStream.Length;
                byte[] allBytes;
                if (availBytesCount > maxLength)
                    allBytes = new byte[maxLength];
                else // availBytesCount could be 0, or greater
                    allBytes = new byte[availBytesCount];
                if (availBytesCount > 0)
                    Read(allBytes, 0, allBytes.Length);
                return allBytes;
            }
        }

        public override void GetBytes(byte[] data)
        {
            if (Read(data, 0, data.Length) != data.Length)
                throw new CryptographicException("Failed to return requested number of bytes");
        }

#if !NETCORE
        public override void GetNonZeroBytes(byte[] data)
        {
            int offset = 0;
            while (offset < data.Length)
            {
                var newBytes = new byte[data.Length - offset];
                if (Read(newBytes, 0, newBytes.Length) != newBytes.Length)
                {
                    throw new CryptographicException("Failed to return requested number of bytes");
                }
                for (int i = 0; i < newBytes.Length; i++)
                {
                    if (newBytes[i] != 0)
                    {
                        data[offset] = newBytes[i];
                        offset++;
                    }
                }
            }
        }
#endif

        /// <summary>
        ///     When overridden in a derived class, releases the unmanaged resources used by the
        ///     <see cref="T:System.Security.Cryptography.RandomNumberGenerator" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     true to release both managed and unmanaged resources; false to release only unmanaged
        ///     resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, TrueInt) == TrueInt)
                return;
            PoolFullAre.Set();
            /*
             * TODO We need to think about the architecture here, such that we don't have problems disposing static things.
             * 
            mainThreadLoopARE.Dispose();
            myFifoStream.Dispose();
            */
            base.Dispose(disposing);
        }

        private static void MainThreadLoop()
        {
            try
            {
                // ThreadedSeedGenerator performs better with large hunks of data, but if we just use MaxPoolSize, then the whole
                // thing gets drained before it starts refilling.  In effect, the pool will drain by one unit of byteCount, before it
                // starts refilling, and likewise, there will be zero bytes available until at least one unit of byteCount becomes
                // available.  So there's a balancing act happening here... Faster throughput versus faster response time...
                // Divide by 8 seems to be a reasonable compromise between the two.
                var byteCount = MaxPoolSize/8;
                while (true)
                    // The only time we ever quit is on the terminate signal ... interrupt signal ... whatever.  OS kills my thread.
                    if (SafeStream.Length < MaxPoolSize)
                    {
                        var newBytes = new byte[byteCount];
                        // By my measurements, estimated entropy returned by ThreadedSeedGenerator is approx 0.6 or 0.7 bits per bit
                        // when fast=false, and 0.5 when fast=true.  Occasionally we see measurements between 0.4 and 0.5. So round this 
                        // down to 0.125, and just generate 8x as much data as you need. And mix it.
                        for (var i = 0; i < 8; i++)
                        {
                            var maskBytes = MyThreadedSeedGenerator.GenerateSeed(byteCount, true);
                            for (var j = 0; j < newBytes.Length; j++)
                                newBytes[j] ^= maskBytes[j];
                            Array.Clear(maskBytes, 0, maskBytes.Length);
                        }
                        SafeStream.Write(newBytes, 0, newBytes.Length);
                    }
                    else
                    {
                        PoolFullAre.WaitOne();
                    }
            }
            catch
            {
                // TODO think about changing architecture so we correctly avoid disposing static objects, and we gracefully terminate, etc
                // It is not good to catch and swallow all exceptions.
                // 
                // If we got disposed while in the middle of doing stuff, we could throw any type of exception, and 
                // I would want to suppress those.
            }
        }

        ~ThreadedSeedGeneratorRng()
        {
            Dispose(false);
        }
    }
}