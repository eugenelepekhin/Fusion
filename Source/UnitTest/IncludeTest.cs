using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	[TestClass]
	public class IncludeTest : CompileTestBase {
		private string WriteFile(string fileName, string content) {
			string? folder = this.Folder();
			string file = Path.Combine(folder, fileName);
			folder = Path.GetDirectoryName(file);
			if(!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder)) {
				Directory.CreateDirectory(folder);
			}
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
			byte[]? result = this.CompileFile(file, null, out int errorCount, out string errors);
			this.AssertNoErrors(errorCount);
			this.AssertEqual(result, 123);
		}

		[TestMethod]
		public void RelativeFolderIncludeTest() {
			this.WriteFile("p1\\a", "macro a{123}");
			this.WriteFile("p2\\a", "macro a{234}");
			string file = this.WriteFile("main", "include \"a\" macro main{a}");
			byte[]? result = this.CompileFile(file, ["p2", "p1"], out int errorCount, out string errors);
			this.AssertNoErrors(errorCount);
			this.AssertEqual(result, 234);
		}

		[TestMethod]
		public void AbsoluteFolderIncludeTest() {
			this.WriteFile("p1\\a", "macro a{123}");
			this.WriteFile("p2\\a", "macro a{234}");
			string file = this.WriteFile("main", "include \"a\" macro main{a}");
			byte[]? result = this.CompileFile(file, [Path.Combine(this.Folder(), "p2"), Path.Combine(this.Folder(), "p1")], out int errorCount, out string errors);
			this.AssertNoErrors(errorCount);
			this.AssertEqual(result, 234);
		}
	}
}
