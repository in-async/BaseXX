using System;
using System.Buffers;
using System.Diagnostics;

namespace Inasync {

    /// <summary>
    /// base64url のエンコード及びデコードを行うクラス。
    /// https://tools.ietf.org/html/rfc4648#section-5
    /// </summary>
    public static class Base64Url {
        private const string _encodingMap = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private static readonly sbyte[] _decodingMap = CreateDecodingMap(_encodingMap);

        private static sbyte[] CreateDecodingMap(string encodingMap) {
            var decodingMap = new sbyte[0xff];
            decodingMap.AsSpan().Fill(-1);
            for (var i = 0; i < encodingMap.Length; i++) {
                decodingMap[encodingMap[i]] = (sbyte)i;
            }
            return decodingMap;
        }

        /// <summary>
        /// <see cref="byte"/> 配列を base64url にエンコードします。
        /// </summary>
        /// <param name="bytes">エンコード対象の <see cref="byte"/> 配列。</param>
        /// <param name="padding">パディングをする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>base64url エンコード文字列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
        public static string Encode(ReadOnlySpan<byte> bytes, bool padding = false) {
            if (bytes.Length == 0) { return ""; }

            var maxCharsLength = (bytes.Length + 2) / 3 * 4;
            var charArray = ArrayPool<char>.Shared.Rent(maxCharsLength);
            try {
                var success = TryEncode(bytes, charArray, out var charsWritten, padding);
                Debug.Assert(success);
                Debug.Assert(charsWritten <= maxCharsLength);

                return new string(charArray, 0, charsWritten);
            }
            finally {
                ArrayPool<char>.Shared.Return(charArray);
            }
        }

        public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten, bool padding = false) {
            if (bytes.Length == 0) {
                charsWritten = 0;
                return true;
            }
            var maxCharsLength = (bytes.Length + 2) / 3 * 4;
            if (chars.Length < maxCharsLength) {
                charsWritten = 0;
                return false;
            }

            charsWritten = 0;
            var bytesSpan = bytes;
            while (true) {
                switch (bytesSpan.Length) {
                    case 0:
                        return true;

                    case 1:
                        chars[charsWritten++] = _encodingMap[bytesSpan[0] >> 2];
                        chars[charsWritten++] = _encodingMap[(bytesSpan[0] & 0b0011) << 4];
                        if (padding) {
                            chars[charsWritten++] = '=';
                            chars[charsWritten++] = '=';
                        }
                        return true;

                    case 2:
                        chars[charsWritten++] = _encodingMap[bytesSpan[0] >> 2];
                        chars[charsWritten++] = _encodingMap[(bytesSpan[0] & 0b0011) << 4 | bytesSpan[1] >> 4];
                        chars[charsWritten++] = _encodingMap[(bytesSpan[1] & 0b1111) << 2];
                        if (padding) {
                            chars[charsWritten++] = '=';
                        }
                        return true;
                }

                chars[charsWritten++] = _encodingMap[bytesSpan[0] >> 2];
                chars[charsWritten++] = _encodingMap[(bytesSpan[0] & 0b0011) << 4 | bytesSpan[1] >> 4];
                chars[charsWritten++] = _encodingMap[(bytesSpan[1] & 0b1111) << 2 | bytesSpan[2] >> 6];
                chars[charsWritten++] = _encodingMap[bytesSpan[2] & 0b0011_1111];
                bytesSpan = bytesSpan.Slice(start: 3);
            }
        }

        /// <summary>
        /// base64url 文字列を <see cref="byte"/> 配列にデコードします。
        /// </summary>
        /// <param name="input">base64url にエンコードされた文字列。</param>
        /// <returns>デコード後の <see cref="byte"/> 配列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="input"/> が base64url 文字列ではありません。</exception>
        public static byte[] Decode(string? input) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            if (!TryDecode(input, out var result)) { throw new FormatException("base64url 文字列ではありません。"); }
            return result!;
        }

        /// <summary>
        /// base64url でエンコードされた文字列をデコードします。
        /// </summary>
        /// <param name="input">base64url エンコードされた文字列。</param>
        /// <param name="result">デコード後の <see cref="byte"/> 配列。失敗した場合は <c>null</c>。</param>
        /// <returns>デコードに成功した場合は <c>true</c>、それ以外は <c>false</c>。</returns>
        public static bool TryDecode(string? input, out byte[]? result) {
            if (input == null) { goto Failure; }
            if (input.Length == 0) {
                result = Array.Empty<byte>();
                return true;
            }
            var chars = input.AsSpan().TrimEnd('=');

            var bytesLength = chars.Length * 3 / 4;
            var byteArray = new byte[bytesLength];
            if (!TryDecode(chars, byteArray, out var bytesWritten)) { goto Failure; }
            Debug.Assert(bytesWritten == bytesLength);

            result = byteArray;
            return true;

Failure:
            result = null;
            return false;
        }

        public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten) {
            if (chars.Length == 0) {
                bytesWritten = 0;
                return true;
            }
            chars = chars.TrimEnd('=');

            var bytesLength = chars.Length * 3 / 4;
            if (bytesLength > bytes.Length) {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = 0;
            var charSpan = chars;
            while (true) {
                switch (charSpan.Length) {
                    case 0:
                        return true;

                    case 1:
                        return false;

                    case 2: {
                            var ch0 = charSpan[0];
                            var ch1 = charSpan[1];
                            if ((ch0 | ch1) >> 8 != 0) { return false; }

                            int i0 = _decodingMap[ch0];
                            int i1 = _decodingMap[ch1];
                            if ((i0 | i1) < 0) { return false; }

                            bytes[bytesWritten++] = (byte)(i0 << 2 | i1 >> 4);
                        }
                        return true;

                    case 3: {
                            var ch0 = charSpan[0];
                            var ch1 = charSpan[1];
                            var ch2 = charSpan[2];
                            if ((ch0 | ch1 | ch2) >> 8 != 0) { return false; }

                            int i0 = _decodingMap[ch0];
                            int i1 = _decodingMap[ch1];
                            int i2 = _decodingMap[ch2];
                            if ((i0 | i1 | i2) < 0) { return false; }

                            bytes[bytesWritten++] = (byte)(i0 << 2 | i1 >> 4);
                            bytes[bytesWritten++] = (byte)(i1 << 4 | i2 >> 2);
                        }
                        return true;

                    default: {
                            var ch0 = charSpan[0];
                            var ch1 = charSpan[1];
                            var ch2 = charSpan[2];
                            var ch3 = charSpan[3];
                            if ((ch0 | ch1 | ch2 | ch3) >> 8 != 0) { return false; }

                            int i0 = _decodingMap[ch0];
                            int i1 = _decodingMap[ch1];
                            int i2 = _decodingMap[ch2];
                            int i3 = _decodingMap[ch3];
                            if ((i0 | i1 | i2 | i3) < 0) { return false; }

                            bytes[bytesWritten++] = (byte)(i0 << 2 | i1 >> 4);
                            bytes[bytesWritten++] = (byte)(i1 << 4 | i2 >> 2);
                            bytes[bytesWritten++] = (byte)(i2 << 6 | i3);
                        }
                        break;
                }
                charSpan = charSpan.Slice(start: 4);
            }
        }
    }
}
