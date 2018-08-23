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
using SafeOrbit.Exceptions.SerializableException;

#if NETFRAMEWORK
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace SafeOrbit.Exceptions
{
    /// <summary>
    /// An exception to throw when an <see cref="Enum"/> is out of range.
    /// </summary>
    /// <typeparam name="TEnum">Type of the enum</typeparam>
    /// <seealso cref="SafeOrbitException" />
    /// <seealso cref="SerializableExceptionBase" />
#if NETFRAMEWORK
   [Serializable]
#endif
    public class UnexpectedEnumValueException<TEnum> : SafeOrbitException where TEnum: IComparable, IFormattable, IConvertible
    {
        public TEnum Value { get; set; }
        protected override void ConfigureSerialize(ISerializationContext serializationContext)
        {
            serializationContext.Add(() => Value);
            base.ConfigureSerialize(serializationContext);
        }
        public UnexpectedEnumValueException(TEnum value)
            : base("Value " + value + " of enum " + typeof(TEnum).Name + " is not supported")
        {
            Value = value;
        }
#if NETFRAMEWORK
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        private UnexpectedEnumValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}