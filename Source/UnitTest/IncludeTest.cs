using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	[TestClass]
	public class IncludeTest : CompileTestBase {
		private string WriteFile(string fileName, string content) {
			string folder = this.Folder();
			string file = Path.Combine(folder, fileName);
			File.WriteAllText(file, content);
			return file;
		}

		private void AssertEqual(byte[]? actual, params byte[] expected) {
			this.AreEqual(expected, actual);
		}

		[TestMethod]
		public void SimpleIncludeTest() {
			this.WriteFile("a", "macro a{123}");
			string file = this.WriteFile("main", "include \"a\" macro main{a}");
			byte[]? result = this.CompileFile(file, out int errorCount, out string errors);
			this.AssertNoErrors(errorCount);
			this.AssertEqual(result, 123);
		}
	}
}
