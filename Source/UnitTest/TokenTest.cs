using System;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {


	/// <summary>
	///This is a test class for Token and is intended
	///to contain all Token Unit Tests
	///</summary>
	[TestClass()]
	public class TokenTest {
		public TestContext TestContext { get; set; }

		private void TestTokenNumber(string text, int value) {
			Token token = new Token(new Position("test", 2), TokenType.Number, text);
			Assert.AreEqual(value, token.Number);
		}

		/// <summary>
		///A test for Number
		///</summary>
		[TestMethod()]
		public void TokenNumberTest() {
			this.TestTokenNumber("0", 0);
			this.TestTokenNumber("1", 1);
			this.TestTokenNumber("10", 10);
			this.TestTokenNumber(int.MaxValue.ToString(), int.MaxValue);
			this.TestTokenNumber("123456789", 123456789);
			this.TestTokenNumber("01", 1);
			this.TestTokenNumber("01234567", 1234567);

			this.TestTokenNumber("0x0", 0x0);
			this.TestTokenNumber("0xabc", 0xabc);
			this.TestTokenNumber("0xABC", 0xabc);
			this.TestTokenNumber("0Xabc", 0xabc);
			this.TestTokenNumber("0XABC", 0xabc);
			this.TestTokenNumber("0x12345678", 0x12345678);
			this.TestTokenNumber("0x23456789", 0x23456789);
			this.TestTokenNumber("0xabcdef", 0xabcdef);
			this.TestTokenNumber("0xABCDEF", 0xabcdef);
			this.TestTokenNumber("0x7fFFffFF", int.MaxValue);

			this.TestTokenNumber("0b1", 1);
			this.TestTokenNumber("0b0", 0);
			this.TestTokenNumber("0b0101", 5);
			this.TestTokenNumber("0b1010", 10);
			this.TestTokenNumber("0b01011010010110100101101001011010", 0x5A5A5A5A);
		}
	}
}
