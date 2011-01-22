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

		#region Additional test attributes
		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion

		private string CreateChain(params string[] content) {
			int index = 0;
			string prev = null;
			foreach(string text in content) {
				string name = this.TestContext.TestName + ++index + ".asm";
				string value = string.Format("\r\n; hello world {0}\r\n", index);
				if(prev != null) {
					value += string.Format("include \"{0}\" ;including file\r\n", prev);
				}
				value += string.Format("\r\n\r\n{0}\r\n\r\n\r\n", text);
				File.WriteAllText(Path.Combine(this.TestContext.TestDeploymentDir, name), value);
				prev = name;
			}
			return prev;
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
