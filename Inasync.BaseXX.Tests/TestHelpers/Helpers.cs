using System;
using System.Collections.Generic;

namespace TestHelpers {

    /// <summary>
    /// <see cref="Action"/> のシーケンスに対する拡張メソッド群。
    /// </summary>
    public static class ActionEnumerableExtensions {

        /// <summary>
        /// <see cref="Action"/> をシーケンシャルに実行します。
        /// </summary>
        /// <param name="actions">実行対象の <see cref="Action"/> のシーケンス。常に非 <c>null</c>。</param>
        public static void Run(this IEnumerable<Action> actions) {
            foreach (var action in actions) {
                action();
            }
        }
    }
}
