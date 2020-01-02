using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHelpers {

    [TestClass]
    public class AssemblyTestSetup {

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context) {
            TestAA.TestAssert = new MSTestAssert();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup() {
        }
    }
}
