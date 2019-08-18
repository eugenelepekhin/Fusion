using System;
using System.IO;
using System.Text;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	public class TestWriter : TextWriter {
		private TestContext TestContext { get; set; }
		public StringBuilder Output { get; set; }

		public TestWriter(TestContext testContext) {
			this.TestContext = testContext;
		}

		public override Encoding Encoding { get { return Encoding.UTF8; } }

		public override void WriteLine(string value) {
			// escape formatting characters.
			this.TestContext.WriteLine(value.Replace("{", "{{").Replace("}", "}}"));

			if(this.Output != null) {
				this.Output.AppendLine(value);
			}
		}
	}

	public static class AssemblerFactory {
		public static Assembler Create(TestContext testContext, BinaryWriter writer, StringBuilder output) {
			TestWriter testWriter = new TestWriter(testContext);
			testWriter.Output = output;
			return new Assembler(testWriter, testWriter, writer);
		}

		public static Assembler Create(TestContext testContext, BinaryWriter writer) {
			return AssemblerFactory.Create(testContext, writer, null);
		}

		public static Assembler Create(TestContext testContext) {
			return AssemblerFactory.Create(testContext, new BinaryWriter(new MemoryStream(16 * 1024)));
		}
	}
}
