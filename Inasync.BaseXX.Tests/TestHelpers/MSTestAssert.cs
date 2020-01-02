using System;
using System.Collections;
using System.Collections.Generic;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHelpers {

    public sealed class MSTestAssert : TestAssert {

        public override void Is<TReturn>(TReturn actual, TReturn expected, string message) {
            Is(typeof(TReturn), actual, expected, message);
        }

        private static void Is(Type type, object? actual, object? expected, string message) {
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
                CollectionAssert.AreEqual(AsCollection((IEnumerable?)expected), AsCollection((IEnumerable?)actual), message);
                return;
            }

            if (type.FullName!.StartsWith("System.ValueTuple`")) {
                foreach (var field in type.GetFields()) {
                    Is(field.FieldType, field.GetValue(actual), field.GetValue(expected), message + ":" + field.Name);
                }
                return;
            }

            Assert.AreEqual(expected, actual, message);
        }

        private static ICollection? AsCollection(IEnumerable? source) {
            if (source is null) { return null; }
            if (source is ICollection casted) { return casted; }

            var list = new List<object?>();
            foreach (var item in source) {
                list.Add(item);
            }
            return list;
        }
    }
}
