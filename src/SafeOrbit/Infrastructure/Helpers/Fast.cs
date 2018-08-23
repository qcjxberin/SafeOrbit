﻿
/*
MIT License

Copyright (c) 2018 Erkin Ekici - undergroundwires@safeorb.it

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
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SafeOrbit.Helpers
{
    public class Fast
    {
        /// <summary>
        ///     Runs fast for-each using <see cref="Parallel" /> and <see cref="Partitioner" />
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="delegate">The delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="delegate" /> is <see langword="null" />.</exception>
        public static void For(int startIndex, int endIndex, Action<int> @delegate)
        {
            if (@delegate == null) throw new ArgumentNullException(nameof(@delegate));
#if NETFRAMEWORK
            var partitioner = Partitioner.Create(0, endIndex);
            Parallel.ForEach(partitioner, range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                    @delegate.Invoke(i);
            });
#elif NETCORE
            for (var i = startIndex; i < endIndex; i++)
            {
                @delegate.Invoke(i);
            }
#endif
        }
    }
}