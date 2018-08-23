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
using System.Diagnostics;
using System.Linq;
using SafeOrbit.Infrastructure;
using SafeOrbit.Extensions;

namespace SafeOrbit.Memory.SafeContainerServices.Instance.Validation
{
    internal class InstanceValidator : IInstanceValidator
    {
        public void ValidateAll(IReadOnlyList<IInstanceProvider> instanceProviders)
        {
            var rules = GetAllRules(instanceProviders).ToArray();
            foreach (var instanceProvider in instanceProviders)
            {
                rules.ForEach(rule => Validate(rule, instanceProvider));
            }
        }
        private void Validate(IInstanceProviderRule rule, IInstanceProvider instanceProvider)
        {
            if (rule.IsSatisfiedBy(instanceProvider)) return;
            var errors = string.Join(",", rule.Errors);
            if (rule.Type == RuleType.Warning)
            {
                Debug.WriteLine($"{nameof(SafeContainer)} - Warning : {errors}");
            }
            if (rule.Type == RuleType.Error)
            {
                throw new ArgumentException(errors);
            }
        }
        protected IEnumerable<IInstanceProviderRule> GetAllRules(IEnumerable<IInstanceProvider> instanceProviders)
        {
            yield return new SingletonShouldNotDependOnTransientRule(instanceProviders);
            yield return new SingletonShouldNotImplementIDisposable();
        }
    }
}