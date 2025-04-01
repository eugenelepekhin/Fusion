using System.Text;
using System.Text.RegularExpressions;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	public abstract class CompileTestBase {
		public TestContext? TestContext { get; set; }

		protected string Folder() {
			Assert.IsNotNull(this.TestContext);
			Assert.IsNotNull(this.TestContext.TestRunDirectory);
			Assert.IsNotNull(this.TestContext.TestName);

			string folder = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName);
			if(!Directory.Exists(folder)) {
				Directory.CreateDirectory(folder);
			}
			return folder;
		}

		private string TestFile() {
			bool tryCreate(string file) {
				if(!File.Exists(file)) {
					try {
						using FileStream stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None);
						return true;
					} catch(IOException) {
						// ignore this exception
					}
				}
				return false;
			}

			Assert.IsNotNull(this.TestContext);
			string folder = this.Folder();
			string file = string.Empty;
			int i = 0;
			do {
				file = Path.Combine(folder, $"AssemblerCompileTest{++i:D3}.asm");
			} while(!tryCreate(file));
			return file;
		}

		protected byte[]? CompileFile(string file, IEnumerable<string>? searchPath, out int errorCount, out string errors) {
			Assert.IsNotNull(this.TestContext);
			StringBuilder output = new StringBuilder();
			using OutputWriter writer = new OutputWriter(OutputWriter.OutputType.Binary, 16, " ");
			Assembler assembler = AssemblerFactory.Create(this.TestContext, writer, output, searchPath);
			assembler.Compile(file);
			errorCount = assembler.ErrorCount;
			errors = output.ToString();
			if(assembler.ErrorCount <= 0) {
				return writer.ToArray();
			}
			return null;
		}

		protected string? CompileFileToText(string file, OutputWriter.OutputType outputType, int rowWidth, out int errorCount, out string errors) {
			Assert.IsNotNull(this.TestContext);
			StringBuilder output = new StringBuilder();
			using OutputWriter writer = new OutputWriter(outputType, rowWidth, " ");
			Assembler assembler = AssemblerFactory.Create(this.TestContext, writer, output, null);
			assembler.Compile(file);
			errorCount = assembler.ErrorCount;
			errors = output.ToString();
			if(assembler.ErrorCount <= 0) {
				string outputFile = file + ".txt";
				writer.SaveFile(outputFile);
				return File.ReadAllText(outputFile);
			}
			return null;
		}

		protected string? CompileToText(string text, OutputWriter.OutputType outputType, int rowWidth, out int errorCount, out string errors) {
			Assert.IsNotNull(this.TestContext);
			string file = this.TestFile();
			File.WriteAllText(file, text);
			return this.CompileFileToText(file, outputType, rowWidth, out errorCount, out errors);
		}

		private byte[]? Compile(string text, out int errorCount, out string errors) {
			Assert.IsNotNull(this.TestContext);
			string file = this.TestFile();
			File.WriteAllText(file, text);
			return this.CompileFile(file, null, out errorCount, out errors);
		}
		protected byte[]? Compile(string text, out int errorCount) {
			return this.Compile(text, out errorCount, out _);
		}

		protected void AreEqual<E, A>(E[] expected, A[]? actual) where E:IComparable where A:IComparable {
			Assert.IsNotNull(actual, "Expecting compiler to create an output set of bytes");
			Assert.AreEqual(expected.Length, actual.Length, "Expecting {0} elements output, while actually it's {1}", expected.Length, actual.Length);
			for(int i = 0; i < expected.Length; i++) {
				Assert.AreEqual(0, expected[i].CompareTo(actual[i]), "Elements at {0} index are different: expecting {1:x}, actual {2:x}", i, expected[i], actual[i]);
			}
		}

		protected void AssertNoErrors(int errorCount) {
			Assert.AreEqual(0, errorCount, "Expecting compilation pass without errors, while actually it's {0} errors", errorCount);
		}

		protected void CompileTest(string text, params byte[] expected) {
			int errorCount;
			byte[]? actual = this.Compile(text, out errorCount);
			this.AssertNoErrors(errorCount);
			this.AreEqual(expected, actual);
		}

		protected void CompileCharTest(string text, string expected) {
			this.CompileTest(text, expected.Select(c => (byte)c).Append((byte)0).ToArray());
		}

		protected void CompileTest16(string text, params int[] expected) {
			int errorCount;
			byte[]? actual = this.Compile(text, out errorCount);
			Assert.IsNotNull(actual, "Expecting compiler to create a set of bytes");
			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(expected.Length * 2, actual.Length);
			using(MemoryStream stream = new MemoryStream(actual)) {
				using(BinaryReader reader = new BinaryReader(stream)) {
					foreach(int expectedItem in expected) {
						Assert.AreEqual((int)(ushort)reader.ReadInt16(), expectedItem);
					}
				}
			}
		}

		protected void CompileTest32(string text, params int[] expected) {
			int errorCount;
			byte[]? actual = this.Compile(text, out errorCount);
			Assert.IsNotNull(actual, "Expecting compiler to create a set of bytes");
			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(expected.Length * 4, actual.Length);
			using(MemoryStream stream = new MemoryStream(actual)) {
				using(BinaryReader reader = new BinaryReader(stream)) {
					foreach(int expectedItem in expected) {
						Assert.AreEqual(reader.ReadInt32(), expectedItem);
					}
				}
			}
		}

		private string CompileErrors(string text) {
			int count;
			string errors;
			this.Compile(text, out count, out errors);
			Assert.IsTrue(0 < count && !string.IsNullOrEmpty(errors), "Expecting compilation errors");
			return errors;
		}

		protected string CompileListing(string text) {
			int count;
			string listing;
			this.Compile(text, out count, out listing);
			Assert.IsTrue(0 == count && !string.IsNullOrEmpty(listing), "Expecting listing and no errors.");
			return listing;
		}

		protected void CompileErrorsTest(string text, string? errorFragment) {
			string errors = this.CompileErrors(text);
			errorFragment = string.IsNullOrWhiteSpace(errorFragment) ? ".*" : errorFragment;
			StringAssert.Matches(errors, new Regex(errorFragment, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				"Expected error fragment ({0}) not found in error message: {1}", errorFragment, errors
			);
		}

		protected void SyntaxErrorTest(string text) {
			this.CompileErrorsTest(text, null);
		}
	}
}
