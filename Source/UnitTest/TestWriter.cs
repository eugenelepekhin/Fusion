﻿using System.Diagnostics;
using System.Text;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	public class TestWriter : TextWriter {
		private TestContext TestContext { get; set; }
		public StringBuilder? Output { get; set; }

		public TestWriter(TestContext testContext) {
			this.TestContext = testContext;
		}

		public override Encoding Encoding { get { return Encoding.UTF8; } }

		public override void Write(char value) {
			this.Write(value.ToString());
		}

		public override void Write(string? value) {
			#if DEBUG
				Debug.Write(value);
			#else
				this.TestContext.Write(value);
			#endif
			if(this.Output != null) {
				this.Output.Append(value);
			}
		}

		public override void WriteLine() {
			this.Write(Environment.NewLine);
		}

		public override void WriteLine(string? value) {
			this.Write(value);
			this.WriteLine();
		}
	}

	public static class AssemblerFactory {
		public static Assembler Create(TestContext testContext, OutputWriter writer, StringBuilder? output, IEnumerable<string>? searchPath) {
			TestWriter testWriter = new TestWriter(testContext);
			testWriter.Output = output;
			return new Assembler(testWriter, testWriter, writer, searchPath ?? new List<string>());
		}

		public static Assembler Create(TestContext testContext, OutputWriter writer, StringBuilder? output) {
			return AssemblerFactory.Create(testContext, writer, output, null);
		}

		public static Assembler Create(TestContext testContext, OutputWriter writer) {
			return AssemblerFactory.Create(testContext, writer, null);
		}

		public static Assembler Create(TestContext testContext) {
			return AssemblerFactory.Create(testContext, new OutputWriter(OutputWriter.OutputType.Binary, 15, " "));
		}
	}
}
