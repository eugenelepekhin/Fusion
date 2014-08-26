using System;
using System.IO;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {

	/// <summary>
	/// This is a test class for ParseStream and is intended
	/// to contain all ParseStream Unit Tests
	/// </summary>
	[TestClass()]
	public class ParseStreamTest {
		public TestContext TestContext { get; set; }

		private string CreateChain(params string[] content) {
			int index = 0;
			string prev = null;
			string path = null;
			foreach(string text in content) {
				string name = this.TestContext.TestName + ++index + ".asm";
				string value = string.Format("\r\n; hello world {0}\r\n", index);
				if(prev != null) {
					value += string.Format("include \"{0}\" ;including file\r\n", prev);
				}
				value += string.Format("\r\n\r\n{0}\r\n\r\n\r\n", text);
				path = Path.Combine(this.TestContext.TestDeploymentDir, name);
				File.WriteAllText(path, value);
				prev = name;
			}
			return path;
		}

		private void TestFirst(ParseStream stream, TokenType tokenType, string value) {
			Token token = stream.First();
			Assert.AreEqual(tokenType, token.TokenType);
			Assert.AreEqual(value, token.Value);
		}

		private void TestNext(ParseStream stream, TokenType tokenType, string value) {
			Token token = stream.Next();
			Assert.AreEqual(tokenType, token.TokenType);
			Assert.AreEqual(value, token.Value);
		}

		/// <summary>
		///A test for First
		///</summary>
		[TestMethod()]
		public void ParseStreamFirstTest() {
			Assembler assembler = AssemblerFactory.Create(this.TestContext);
			string file = this.CreateChain("abc 123", "def 456");
			using(ParseStream stream = new ParseStream(assembler, file)) {
				this.TestFirst(stream, TokenType.Identifier, "abc");
				this.TestFirst(stream, TokenType.Number, "123");
				this.TestFirst(stream, TokenType.Identifier, "def");
				this.TestFirst(stream, TokenType.Number, "456");
				this.TestFirst(stream, TokenType.EOS, null);
			}
		}

		/// <summary>
		///A test for Next
		///</summary>
		[TestMethod()]
		public void ParseStreamNextTest() {
			Assembler assembler = AssemblerFactory.Create(this.TestContext);
			string file = this.CreateChain("abc 123", "def 456");
			using(ParseStream stream = new ParseStream(assembler, file)) {
				this.TestFirst(stream, TokenType.Identifier, "abc");
				this.TestNext(stream, TokenType.Number, "123");
				this.TestFirst(stream, TokenType.Identifier, "def");
				this.TestNext(stream, TokenType.Number, "456");
				this.TestFirst(stream, TokenType.EOS, null);
			}
		}
	}
}
