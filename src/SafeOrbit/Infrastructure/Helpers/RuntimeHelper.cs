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

#if !NETCORE
using System.Runtime.CompilerServices;
#endif

namespace SafeOrbit.Helpers
{
    public class RuntimeHelper
    {
        public static void ExecuteCodeWithGuaranteedCleanup(Action action, Action cleanup)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (cleanup == null) throw new ArgumentNullException(nameof(cleanup));

#if !NETCORE
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(
            delegate
                {
                    action.Invoke();
                }
                ,
            delegate
            {
                cleanup.Invoke();
            },
                null);
#else
            try
            {
                action.Invoke();
            }
            finally
            {
                cleanup.Invoke();
            }
#endif
        }

        /// <summary>
        /// Create a CER (Constrained Execution Region)
        /// </summary>
        public static void PrepareConstrainedRegions()
        {
#if !NETCORE
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
        }
    }
}