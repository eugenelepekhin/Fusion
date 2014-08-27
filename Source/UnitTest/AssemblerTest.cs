using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {

	/// <summary>
	///This is a test class for Assembler and is intended
	///to contain all Assembler Unit Tests
	///</summary>
	[TestClass()]
	public class AssemblerTest {
		public TestContext TestContext { get; set; }

		private void ValidateParameters(Assembler assembler, string macro, params string[] expected) {
			List<Token> actual = assembler.Macro[macro].Parameter;
			Assert.AreEqual(expected.Length, actual.Count);
			foreach(Token token in actual) {
				Assert.IsTrue(token.IsIdentifier());
				Assert.IsTrue(expected.Contains(token.Value));
			}
		}

		/// <summary>
		///A test for Compile First Pass
		///</summary>
		[TestMethod()]
		public void AssemblerCompileFirstPassTest() {
			Assembler assembler = AssemblerFactory.Create(this.TestContext);
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "AssemblerCompileFirstPassTest.asm");
			File.WriteAllText(file, Resource.AssemblerCompileFirstPassTest);
			assembler.Parse(file);
			Assert.AreEqual(12, assembler.Macro.Count);
			Assert.AreEqual(1, assembler.ErrorCount);
			this.ValidateParameters(assembler, "r0");
			this.ValidateParameters(assembler, "r1");

			this.ValidateParameters(assembler, "validateRegister", "r");
			this.ValidateParameters(assembler, "validateAddress", "a");
			this.ValidateParameters(assembler, "validateByte", "b");

			this.ValidateParameters(assembler, "dw", "value");

			this.ValidateParameters(assembler, "m", "r", "d");
		}

		/// <summary>
		///A test for Compile Second Pass
		///</summary>
		[TestMethod()]
		public void AssemblerCompileSecondPassTest() {
			Assembler assembler1 = AssemblerFactory.Create(this.TestContext);
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "AssemblerCompileSecondPassTest.asm");
			File.WriteAllText(file, Resource.AssemblerCompileSecondPassTest);
			assembler1.Parse(file);
			Assert.AreEqual(16, assembler1.Macro.Count);
			Assert.AreEqual(0, assembler1.ErrorCount);
			List<string> list = new List<string>(assembler1.Macro.Keys);
			list.Sort();
			string p1;
			using(StringWriter writer = new StringWriter()) {
				foreach(string name in list) {
					assembler1.Macro[name].Write(writer);
					writer.WriteLine();
				}
				p1 = writer.ToString();
			}
			file = Path.Combine(this.TestContext.TestDeploymentDir, "AssemblerCompileSecondPassTest1.asm");
			File.WriteAllText(file, p1);
			Assembler assembler2 = AssemblerFactory.Create(this.TestContext);
			assembler2.Parse(file);
			Assert.AreEqual(assembler1.Macro.Count, assembler2.Macro.Count);
			Assert.AreEqual(0, assembler2.ErrorCount);
			list = new List<string>(assembler2.Macro.Keys);
			list.Sort();
			string p2;
			using(StringWriter writer = new StringWriter()) {
				foreach(string name in list) {
					assembler2.Macro[name].Write(writer);
					writer.WriteLine();
				}
				p2 = writer.ToString();
			}
			Assert.AreEqual(p1, p2, "Parse results are different");
		}
	}
}
