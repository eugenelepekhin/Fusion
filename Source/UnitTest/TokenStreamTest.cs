using System;
using System.IO;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {

	/// <summary>
	/// This is a test class for TokenStream and is intended
	/// to contain all TokenStream Unit Tests
	///</summary>
	[TestClass()]
	public class TokenStreamTest {
		public TestContext TestContext { get; set; }

		private void TestNextToken(TokenStream ts, int line, TokenType tokenType, string value) {
			int errorCount = ts.Assembler.ErrorCount;
			Token token = ts.Next();
			Assert.AreEqual(errorCount, ts.Assembler.ErrorCount, "Unexpected error");
			Assert.AreEqual(line, token.Position.Line, "Wrong line number");
			Assert.AreEqual(tokenType, token.TokenType, "Wrong token type");
			Assert.AreEqual(value, token.Value, "Wrong token value");
		}

		private void TestNextToken(TokenStream ts) {
			int errorCount = ts.Assembler.ErrorCount;
			Token token = ts.Next();
			Assert.AreEqual(errorCount + 1, ts.Assembler.ErrorCount, "Error expected");
			Assert.IsTrue(token == null || token.IsString() && token.Value == "skip", "Unexpected token");
		}

		/// <summary>
		///A test for Next
		///</summary>
		[TestMethod()]
		public void TokenStreamNextTest() {
			Assembler assembler = AssemblerFactory.Create(this.TestContext);
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "TokenStreamNextTest.asm");
			File.WriteAllText(file, Resource.TokenStreamNextTest);
			using(TokenStream ts = new TokenStream(assembler, file)) {
				this.TestNextToken(ts, 2, TokenType.Identifier, "abc");
				this.TestNextToken(ts, 2, TokenType.Separator, ":");
				this.TestNextToken(ts, 2, TokenType.Identifier, "m");
				this.TestNextToken(ts, 2, TokenType.Identifier, "r1");
				this.TestNextToken(ts, 2, TokenType.Separator, ",");
				this.TestNextToken(ts, 2, TokenType.Number, "0x20");
				this.TestNextToken(ts, 3, TokenType.Identifier, "l");
				this.TestNextToken(ts, 3, TokenType.Identifier, "a2");
				this.TestNextToken(ts, 5, TokenType.Number, "1");
				this.TestNextToken(ts, 5, TokenType.Number, "067");
				this.TestNextToken(ts, 5, TokenType.Number, "0b111000111");
				this.TestNextToken(ts, 7, TokenType.Identifier, "if");
				this.TestNextToken(ts, 7, TokenType.Identifier, "a");
				this.TestNextToken(ts, 7, TokenType.Operator, "+");
				this.TestNextToken(ts, 7, TokenType.Number, "4");
				this.TestNextToken(ts, 7, TokenType.Comparison, "<=");
				this.TestNextToken(ts, 7, TokenType.Number, "2");
				this.TestNextToken(ts, 7, TokenType.Separator, "{");
				this.TestNextToken(ts, 8, TokenType.Separator, "}");

				this.TestNextToken(ts);
				this.TestNextToken(ts, 11, TokenType.Number, "123");

				this.TestNextToken(ts);
				this.TestNextToken(ts, 15, TokenType.Number, "1234");

				this.TestNextToken(ts, 19, TokenType.Operator, "!");
				this.TestNextToken(ts, 20, TokenType.Identifier, "abc");

				this.TestNextToken(ts, 21, TokenType.Operator, "=");
				this.TestNextToken(ts, 22, TokenType.Identifier, "def");
				this.TestNextToken(ts, 23, TokenType.Comparison, "!=");
				this.TestNextToken(ts, 23, TokenType.String, "hello,\n world");
				this.TestNextToken(ts, 24, TokenType.Eos, null);

				Assert.AreEqual(2, assembler.ErrorCount);
			}
		}

		[TestMethod()]
		public void TokenStreamNextPathStringTest() {
			Assembler assembler = AssemblerFactory.Create(this.TestContext);
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "TokenStreamNextPathStringTest.asm");
			File.WriteAllText(file, "\"hello\\world\"");
			using(TokenStream ts = new TokenStream(assembler, file)) {
				Token actual = ts.NextPathString();
				Assert.IsNotNull(actual);
				Assert.IsTrue(TokenType.String == actual.TokenType && "hello\\world" == actual.Value);
			}

			File.WriteAllText(file, "\"hello\nworld\"");
			using(TokenStream ts = new TokenStream(assembler, file)) {
				Token actual = ts.NextPathString();
				Assert.IsNull(actual);
			}
		}
	}
}
