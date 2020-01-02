using System;
using System.Buffers;
using System.Diagnostics;

namespace Inasync {

    /// <summary>
    /// base16 のエンコード及びデコードを行うクラス。
    /// https://tools.ietf.org/html/rfc4648#section-8
    /// </summary>
    public static class Base16 {
        private const string _lowerEncodingMap = "0123456789abcdef";
        private const string _upperEncodingMap = "0123456789ABCDEF";
        private static readonly sbyte[] _decodingMap = CreateDecodingMap(_lowerEncodingMap);

        private static sbyte[] CreateDecodingMap(string encodingMap) {
            var decodingMap = new sbyte[0xff];
            decodingMap.AsSpan().Fill(-1);
            for (var i = 0; i < encodingMap.Length; i++) {
                var ch = encodingMap[i];
                decodingMap[Char.ToLowerInvariant(ch)] = (sbyte)i;
                decodingMap[Char.ToUpperInvariant(ch)] = (sbyte)i;
            }
            return decodingMap;
        }

        /// <summary>
        /// <see cref="byte"/> 配列を base16 文字列にエンコードします。
        /// </summary>
        /// <param name="bytes">base16 エンコードの対象となる <see cref="byte"/> 型の <see cref="Span{T}"/>。</param>
        /// <param name="toUpper">エンコード後の 16 進文字列を大文字にする場合は <c>true</c>、それ以外は <c>false</c>。</param>
        /// <returns>エンコードによって算出された base16 形式の文字列。</returns>
        /// <remarks>コアロジックは <see cref="TryEncode(ReadOnlySpan{byte}, Span{char}, out int, bool)"/> に委譲します。</remarks>
        public static string Encode(ReadOnlySpan<byte> bytes, bool toUpper = false) {
            if (bytes.Length == 0) { return ""; }

            var charsLength = bytes.Length * 2;
            char[]? charArray = null;
            try {
                Span<char> chars = charsLength <= Consts.StackallocThreshold ? stackalloc char[charsLength] : (charArray = ArrayPool<char>.Shared.Rent(charsLength));

                var success = TryEncode(bytes, chars, out var charsWritten, toUpper);
                Debug.Assert(success);
                Debug.Assert(charsWritten == charsLength);

                unsafe {
                    fixed (char* cp = chars) {
                        return new string(cp, 0, charsWritten);
                    }
                }
            }
            finally {
                if (charArray != null) {
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }

        /// <summary>
        /// <see cref="byte"/> 配列を base16 文字列にエンコードします。
        /// </summary>
        /// <param name="bytes">base16 エンコードの対象となる <see cref="byte"/> 型の <see cref="Span{T}"/>。</param>
        /// <param name="chars">エンコードによって算出された <see cref="char"/> の書き込み先 <see cref="Span{T}"/>。</param>
        /// <param name="charsWritten">実際に <paramref name="chars"/> に書き込まれた文字数。</param>
        /// <param name="toUpper">エンコード後の 16 進文字列を大文字にする場合は <c>true</c>、それ以外は <c>false</c>。</param>
        /// <returns>
        /// エンコードに成功した場合は <c>true</c>、失敗した場合は <c>false</c>。
        /// 失敗するケースは以下の通り:
        /// <list type="bullet">
        ///     <term><paramref name="chars"/> の長さが <paramref name="bytes"/> の長さの 2 倍より短い。</term>
        /// </list>
        /// </returns>
        public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten, bool toUpper = false) {
            if (bytes.Length == 0) {
                charsWritten = 0;
                return true;
            }
            var charsLength = bytes.Length * 2;
            if (chars.Length < charsLength) {
                charsWritten = 0;
                return false;
            }

            switch (Mode) {
                case EncodingMode.Lookup:
                default:
                    var encodingMap = toUpper ? _upperEncodingMap : _lowerEncodingMap;
                    charsWritten = 0;
                    foreach (var b in bytes) {
                        chars[charsWritten++] = encodingMap[b >> 4];
                        chars[charsWritten++] = encodingMap[b & 0xf];
                    }
                    break;

                case EncodingMode.Manipulate:
                    var (@base, sub) = toUpper ? (55, -7) : (87, -39);
                    var ci = 0;
                    foreach (var b in bytes) {
                        var high = b >> 4;
                        chars[ci++] = (char)(@base + high + (((high - 10) >> 31) & sub));

                        var low = b & 0xF;
                        chars[ci++] = (char)(@base + low + (((low - 10) >> 31) & sub));
                    }
                    charsWritten = ci;
                    break;
            }


            return true;
        }

        internal static EncodingMode Mode { get; } = EncodingMode.Lookup;

        internal enum EncodingMode {
            Manipulate,
            Lookup,
        }

        /// <summary>
        /// 入力文字列を base16 形式とみなして <see cref="byte"/> 配列にデコードします。
        /// </summary>
        /// <param name="input">base16 デコードの対象となる入力文字列。</param>
        /// <returns>デコードによって算出された <see cref="byte"/> 配列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="input"/> が base16 ではありません。</exception>
        /// <remarks>コアロジックは <see cref="TryDecode(ReadOnlySpan{char}, Span{byte}, out int)"/> に委譲します。</remarks>
        public static byte[] Decode(string? input) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            if (!TryDecode(input, out var result)) { throw new FormatException("base16 文字列ではありません。"); }
            return result!;
        }

        /// <summary>
        /// 入力文字列を base16 形式とみなして <see cref="byte"/> 配列にデコードします。
        /// </summary>
        /// <param name="input">base16 デコードの対象となる入力文字列。</param>
        /// <param name="result">デコードによって算出された <see cref="byte"/> 配列。失敗した場合は <c>null</c>。</param>
        /// <returns>デコードに成功した場合は <c>true</c>、それ以外は <c>false</c>。</returns>
        /// <remarks>コアロジックは <see cref="TryDecode(ReadOnlySpan{char}, Span{byte}, out int)"/> に委譲します。</remarks>
        public static bool TryDecode(string? input, out byte[]? result) {
            if (input == null) { goto Failure; }
            if (input.Length == 0) {
                result = Array.Empty<byte>();
                return true;
            }

            var bytesLength = input.Length / 2;
            var byteArray = new byte[bytesLength];
            if (!TryDecode(input.AsSpan(), byteArray, out var bytesWritten)) { goto Failure; }
            Debug.Assert(bytesWritten == bytesLength);

            result = byteArray;
            return true;

Failure:
            result = null;
            return false;
        }

        /// <summary>
        /// 入力文字列を base16 形式とみなして <see cref="byte"/> 配列にデコードします。
        /// </summary>
        /// <param name="chars">base16 デコードの対象となる <see cref="char"/> 型の <see cref="Span{T}"/>。</param>
        /// <param name="bytes">デコードによって算出された <see cref="byte"/> の書き込み先 <see cref="Span{T}"/>。</param>
        /// <param name="bytesWritten">実際に <paramref name="bytes"/> に書き込まれたバイト サイズ。</param>
        /// <returns>
        /// デコードに成功した場合は <c>true</c>、失敗した場合は <c>false</c>。
        /// 失敗するケースは以下の通り:
        /// <list type="bullet">
        ///     <term><paramref name="chars"/> の長さが 2 の倍数ではない。</term>
        ///     <term><paramref name="bytes"/> の長さが <paramref name="chars"/> の長さの 1/2 より短い。</term>
        ///     <term><paramref name="chars"/> に base16 以外の文字が含まれている。</term>
        /// </list>
        /// </returns>
        public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten) {
            if (chars.Length == 0) {
                bytesWritten = 0;
                return true;
            }

            bytesWritten = 0;
            if (chars.Length % 2 == 1) { return false; }
            if (bytes.Length < chars.Length / 2) { return false; }

            var charSpan = chars;
            foreach (ref var b in bytes) {
                var ch0 = charSpan[0];
                var ch1 = charSpan[1];
                if ((ch0 | ch1) >> 8 != 0) { return false; }

                int i0 = _decodingMap[ch0];
                int i1 = _decodingMap[ch1];
                if ((i0 | i1) < 0) { return false; }

                b = (byte)((byte)i0 << 4 | (byte)i1);
                bytesWritten++;
                charSpan = charSpan.Slice(start: 2);
            }

            return true;
        }
    }
}
