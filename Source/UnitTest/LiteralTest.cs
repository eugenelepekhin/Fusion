using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	[TestClass]
	public class LiteralTest : CompileTestBase {
		[TestMethod]
		public void NumberLiteralTest() {
			// decimal numbers
			this.CompileTest("macro main{0}", 0);
			this.CompileTest("macro main{1}", 1);
			this.CompileTest("macro main{2}", 2);
			this.CompileTest("macro main{7}", 7);
			this.CompileTest("macro main{8}", 8);
			this.CompileTest("macro main{10}", 10);
			this.CompileTest("macro main{16}", 16);
			this.CompileTest("macro main{32}", 32);
			this.CompileTest32("binary 32 macro main{315}", 315);
			this.CompileTest32("binary 32 macro main{1000000}", 1000000);
			this.CompileTest32("binary 32 macro main{123456789}", 123456789);
			this.CompileTest32("binary 32 macro main{1234567890}", 1234567890);
			this.CompileTest32("binary 32 macro main{2147483647}", int.MaxValue);

			this.CompileTest32("binary 32 macro main{1'000'000}", 1000000);
			this.CompileTest32("binary 32 macro main{123_456_789}", 123456789);
			this.CompileTest32("binary 32 macro main{1'234_567'890}", 1234567890);
			this.CompileTest32("binary 32 macro main{2'147_48'36_47}", int.MaxValue);

			this.CompileErrorsTest("binary 32 macro main{123x45}", "Undefined macro x45 at");
			this.SyntaxErrorTest("binary 32 macro main{'123x45}");

			// binary number
			this.CompileTest("macro main{0b0}", 0b0);
			this.CompileTest("macro main{0b1}", 0b1);
			this.CompileTest("macro main{0B10}", 0b10);
			this.CompileTest("macro main{0b0010}", 0b0010);
			this.CompileTest("macro main{0B1010}", 0b1010);
			this.CompileTest("macro main{0b01111111}", 0b01111111);
			this.CompileTest32("binary 32 macro main{0b01101101011011100111011101011011}", 0b01101101011011100111011101011011);
			this.CompileTest32("binary 32 macro main{0b01111111111111111111111111111111}", int.MaxValue);

			this.CompileTest("macro main{0b0'1_111'1'11}", 0b01111111);
			this.CompileTest32("binary 32 macro main{0b0_1'101_101'011'011_1_00_1110'1'11_0101'1011}", 0b01101101011011100111011101011011);
			this.CompileTest32("binary 32 macro main{0b0'111111_11'1'111_111111'111_1111_11'1'1'1_1}", int.MaxValue);

			this.CompileTest("macro main{0b0020}", 0, 20);

			// hex number
			this.CompileTest("macro main{0x0}", 0x0);
			this.CompileTest("macro main{0x1}", 0x1);
			this.CompileTest("macro main{0x3}", 0x3);
			this.CompileTest("macro main{0x9}", 0x9);
			this.CompileTest("macro main{0xa}", 0xa);
			this.CompileTest("macro main{0xC}", 0xc);
			this.CompileTest32("binary 32 macro main{0x12345678}", 0x12345678);
			this.CompileTest32("binary 32 macro main{0x9aBcdEF}", 0x9aBcdEF);
			this.CompileTest32("binary 32 macro main{0x7FffFFff}", int.MaxValue);

			this.CompileTest32("binary 32 macro main{0x1'2'34_56'78}", 0x12345678);
			this.CompileTest32("binary 32 macro main{0x9a_Bc_dE'F}", 0x9aBcdEF);
			this.CompileTest32("binary 32 macro main{0x7_Ff'f_FF'ff}", int.MaxValue);

			this.CompileErrorsTest("binary 32 macro main{0x123g45}", "Undefined macro g45 at");

			// octal number
			this.CompileTest("macro main{01}", 1);
			this.CompileTest("macro main{03}", 3);
			this.CompileTest("macro main{04}", 4);
			this.CompileTest("macro main{07}", 7);
			this.CompileTest("macro main{072}", 58);
			this.CompileTest("macro main{0200}", 128);
			this.CompileTest32("binary 32 macro main{017777777777}", int.MaxValue);

			this.CompileTest32("binary 32 macro main{0_1_7_77'7'777_7'77}", int.MaxValue);
			this.CompileTest32("binary 32 macro main{0'1_7_77'7'777_7'77}", int.MaxValue);

			this.CompileTest("macro main{012389}", 83, 89);
			this.CompileTest("macro main{0'12_38'9}", 83, 89);
		}


		[TestMethod]
		public void StringLiteralTest() {
			this.CompileCharTest("macro main{\"\"}", "");
			this.CompileCharTest("macro main{\"abc\"+7}", "abc7");
			this.CompileCharTest("macro main{\"abc\\td\\ne\"}", "abc\td\ne");
			this.CompileCharTest(@"macro main{""\""\\\0\a\b\f\n\r\t\v""}", "\"\\\0\a\b\f\n\r\t\v");
			this.CompileCharTest("macro main{\"a \r\nb \"}", "a \r\nb ");

			this.SyntaxErrorTest("macro main{\"abc}");
		}
	}
}
