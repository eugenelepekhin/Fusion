using System;
using System.IO;
using System.Text;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	public class TestWriter : TextWriter {
		private TestContext TestContext { get; set; }

		public TestWriter(TestContext testContext) {
			this.TestContext = testContext;
		}

		public override Encoding Encoding { get { return Encoding.UTF8; } }

		public override void WriteLine(string value) {
			this.TestContext.WriteLine(value.Replace("{", "{{").Replace("}", "}}"));
		}
	}

	public static class AssemblerFactory {
		public static Assembler Create(TestContext testContext, BinaryWriter writer) {
			TestWriter testWriter = new TestWriter(testContext);
			return new Assembler(testWriter, testWriter, writer);
		}
		public static Assembler Create(TestContext testContext) {
			return AssemblerFactory.Create(testContext, new BinaryWriter(new MemoryStream(16 * 1024)));
		}
	}
}
