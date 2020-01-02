using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelpers;

namespace Inasync.Tests {

    [TestClass]
    public class Base64UrlTests {

        [TestMethod]
        public void Encode() {
            Action TestCase(TestNumber testNumber, byte[]? bytes, bool padding, string? expected = default) => () => {
                TestAA
                    .Act(() => Base64Url.Encode(bytes, padding))
                    .Assert(expected!, message: testNumber);
            };

            new[]{
                TestCase(10, Bytes()      , padding: false, expected: ""    ),
                TestCase(11, Bytes(0)     , padding: false, expected: "AA"  ),
                TestCase(12, Bytes(250)   , padding: false, expected: "-g"  ),
                TestCase(13, Bytes(255, 0), padding: false, expected: "_wA" ),
                TestCase(20, Bytes()      , padding: true , expected: ""    ),
                TestCase(21, Bytes(0)     , padding: true , expected: "AA=="),
                TestCase(22, Bytes(250)   , padding: true , expected: "-g=="),
                TestCase(23, Bytes(255, 0), padding: true , expected: "_wA="),
            }.Run();
        }

        [TestMethod]
        public void Encode_vsConvertToBase64String() {
            var bytes = Rand.Bytes();

            TestAA
                .Act(() => Base64Url.Encode(bytes, padding: true))
                .Assert(Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_'));

            TestAA
                .Act(() => Base64Url.Encode(bytes, padding: false))
                .Assert(Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('='));
        }

        [TestMethod]
        public void Decode() {
            Action TestCase(TestNumber testNumber, string? input, byte[]? expected = default, Type? expectedExceptionType = null) => () => {
                TestAA
                    .Act(() => Base64Url.Decode(input))
                    .Assert(expected!, expectedExceptionType, message: testNumber);
            };

            new[]{
                //TestCase( 0, null  , expectedExceptionType: typeof(ArgumentNullException)),
                //TestCase( 1, "@"   , expectedExceptionType: typeof(FormatException)),
                //TestCase(10, ""    , expected: Bytes()      ),
                //TestCase(11, "AA"  , expected: Bytes(0)     ),
                //TestCase(12, "-g"  , expected: Bytes(250)   ),
                //TestCase(13, "_wA" , expected: Bytes(255, 0)),
                //TestCase(21, "AA==", expected: Bytes(0)     ),
                //TestCase(22, "-g==", expected: Bytes(250)   ),
                //TestCase(23, "_wA=", expected: Bytes(255, 0)),

                TestCase(50, "Aあ"  , expectedExceptionType: typeof(FormatException)),
            }.Run();
        }

        [TestMethod]
        public void Dencode_vsConvertFromBase64String() {
            {
                var encoded = Base64Url.Encode(Rand.Bytes(), padding: true);
                Console.WriteLine(encoded);

                TestAA
                    .Act(() => Base64Url.Decode(encoded))
                    .Assert(Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/')));
            }

            {
                var encoded = Base64Url.Encode(Rand.Bytes(), padding: false);
                Console.WriteLine(encoded);

                TestAA
                    .Act(() => Base64Url.Decode(encoded))
                    .Assert(Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/').PadRight(encoded.Length + 3 & ~0x3, '=')));
            }
        }

        [TestMethod]
        public void TryDecode() {
            Action TestCase(TestNumber testNumber, string? input, (bool success, byte[]? result) expected) => () => {
                TestAA
                    .Act(() => (success: Base64Url.TryDecode(input, out var result), result))
                    .Assert(expected, message: testNumber);
            };

            new[] {
                TestCase( 0, null  , expected: (false, null)         ),
                TestCase( 1, "@"   , expected: (false, null)         ),
                TestCase(10, ""    , expected: (true , Bytes())      ),
                TestCase(11, "AA"  , expected: (true , Bytes(0))     ),
                TestCase(12, "-g"  , expected: (true , Bytes(250))   ),
                TestCase(13, "_wA" , expected: (true , Bytes(255, 0))),
                TestCase(21, "AA==", expected: (true , Bytes(0))     ),
                TestCase(22, "-g==", expected: (true , Bytes(250))   ),
                TestCase(23, "_wA=", expected: (true , Bytes(255, 0))),
            }.Run();
        }

        #region Helper

        private static byte[] Bytes(params byte[] bin) => bin;

        #endregion Helper
    }
}
