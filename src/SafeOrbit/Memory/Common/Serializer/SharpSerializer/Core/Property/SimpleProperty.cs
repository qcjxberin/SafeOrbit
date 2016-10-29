
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

namespace SafeOrbit.Infrastructure.Serialization.SerializationServices.Core
{
    /// <summary>
    ///     Represents all primitive types (i.e. int, double...) and additionally
    ///     DateTime, TimeSpan, Decimal und enumerations
    ///     Contains no nested properties
    /// </summary>
    /// <remarks>
    ///     See SimpleValueConverter for a list of supported types.
    /// </remarks>
    internal sealed class SimpleProperty : Property
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public SimpleProperty(string name, Type type)
            : base(name, type)
        {
        }

        /// <summary>
        ///     It could only one of the simple types, see Tools.IsSimple(...)
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        ///     Gets the property art.
        /// </summary>
        /// <returns></returns>
        protected override PropertyArt GetPropertyArt()
        {
            return PropertyArt.Simple;
        }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public override string ToString()
        {
            var text = base.ToString();
            return Value != null
                ? $"{text}, ({Value})"
                : $"{text}, (null)";
        }
    }
}