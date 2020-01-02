using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelpers;

namespace Inasync.Tests {

    [TestClass]
    public class Base16Tests {

        [TestMethod]
        public void Encode() {
            Action TestCase(TestNumber testNumber, byte[] bytes, bool toUpper, string expected) => () => {
                TestAA
                    .Act(() => Base16.Encode(bytes, toUpper))
                    .Assert(expected, message: testNumber);
            };

            var rndBytes = Rand.Bytes();
            new[]{
                TestCase( 1, Bytes()                                       , toUpper: false, expected: ""                ),
                TestCase( 2, Bytes(0x0f)                                   , toUpper: false, expected: "0f"              ),
                TestCase( 3, Bytes(0x0f,0xf0)                              , toUpper: false, expected: "0ff0"            ),
                TestCase( 4, Bytes(0x0f,0xf0)                              , toUpper: true , expected: "0FF0"            ),
                TestCase(10, Bytes(0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef), toUpper: false, expected: "0123456789abcdef"),
                TestCase(11, Bytes(0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef), toUpper: true , expected: "0123456789ABCDEF"),
                TestCase(50, rndBytes                                      , toUpper: false, expected: BitConverter.ToString(rndBytes).Replace("-", "").ToLowerInvariant()),
                TestCase(51, rndBytes                                      , toUpper: true , expected: BitConverter.ToString(rndBytes).Replace("-", "").ToUpperInvariant()),
            }.Run();
        }

        [TestMethod]
        public void Decode() {
            Action TestCase(TestNumber testNumber, string? input, byte[]? expected = default, Type? expectedExceptionType = null) => () => {
                TestAA
                    .Act(() => Base16.Decode(input))
                    .Assert(expected!, expectedExceptionType, message: testNumber);
            };

            new[]{
                TestCase( 0, null              , expectedExceptionType: typeof(ArgumentNullException)),
                TestCase( 1, ""                , expected: Bytes()),
                TestCase( 2, " "               , expectedExceptionType: typeof(FormatException)),
                TestCase( 3, "0"               , expectedExceptionType: typeof(FormatException)),
                TestCase( 4, "0g"              , expectedExceptionType: typeof(FormatException)),
                TestCase( 5, "0f"              , expected: Bytes(0x0f)),
                TestCase( 6, "0fF0"            , expected: Bytes(0x0f,0xf0)),
                TestCase(10, "0123456789abcDEF", expected: Bytes(0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef)),
            }.Run();
        }

        [TestMethod]
        public void TryDecode() {
            Action TestCase(TestNumber testNumber, string? input, (bool success, byte[]? result) expected) => () => {
                TestAA
                    .Act(() => (success: Base16.TryDecode(input, out var result), result))
                    .Assert(expected, message: testNumber);
            };

            new[] {
                TestCase( 0, null              , expected: (false, null                                          )),
                TestCase( 1, ""                , expected: (true , Bytes()                                       )),
                TestCase( 2, " "               , expected: (false, null                                          )),
                TestCase( 3, "0"               , expected: (false, null                                          )),
                TestCase( 4, "0g"              , expected: (false, null                                          )),
                TestCase( 5, "0f"              , expected: (true , Bytes(0x0f)                                   )),
                TestCase( 6, "0fF0"            , expected: (true , Bytes(0x0f,0xf0)                              )),
                TestCase(10, "0123456789abcDEF", expected: (true , Bytes(0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef))),
            }.Run();
        }

        #region Helpers

        private static byte[] Bytes(params byte[] bytes) => bytes;

        #endregion Helpers
    }
}
