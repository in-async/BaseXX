namespace TestHelpers {

    /// <summary>
    /// テスト番号を表します。
    /// </summary>
    public readonly struct TestNumber {

        /// <summary>
        /// <see cref="TestNumber"/> 構造体の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="value"><see cref="Value"/> に渡される値。</param>
        public TestNumber(int value) {
            Value = value;
        }

        /// <summary>
        /// テスト番号。
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// テスト番号を表す文字列を返します。
        /// </summary>
        /// <returns>"No.{<see cref="Value"/>}" のフォーマット文字列。</returns>
        public override string ToString() => "No." + Value;

        /// <summary>
        /// <see cref="int"/> を <see cref="TestNumber"/> に暗黙的に変換します。
        /// </summary>
        /// <param name="testNumber">テスト番号。</param>
        public static implicit operator TestNumber(int testNumber) => new TestNumber(testNumber);

        /// <summary>
        /// <see cref="TestNumber"/> を <see cref="int"/> に暗黙的に変換します。
        /// </summary>
        /// <param name="testNumber">テスト番号。</param>
        public static implicit operator int(TestNumber testNumber) => testNumber.Value;

        /// <summary>
        /// <see cref="TestNumber"/> の文字列表現に暗黙的に変換します。
        /// </summary>
        /// <param name="testNumber"><see cref="TestNumber.ToString"/> で返される文字列。</param>
        public static implicit operator string(TestNumber testNumber) => testNumber.ToString();
    }
}
