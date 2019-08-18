using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {

	/// <summary>
	///This is a test class for Assembler.Compile
	///</summary>
	[TestClass()]
	public class CompilerTest {
		public TestContext TestContext { get; set; }

		private byte[] Compile(string text, out int errorCount, out string errors) {
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "AssemblerCompileTest.asm");
			File.WriteAllText(file, text);
			StringBuilder output = new StringBuilder();
			using(MemoryStream stream = new MemoryStream(16 * 1024)) {
				using(BinaryWriter writer = new BinaryWriter(stream)) {
					Assembler assembler = AssemblerFactory.Create(this.TestContext, writer, output);
					assembler.Compile(file);
					errorCount = assembler.ErrorCount;
					errors = output.ToString();
					if(assembler.ErrorCount <= 0) {
						writer.Flush();
						return stream.ToArray();
					}
				}
			}
			return null;
		}
		private byte[] Compile(string text, out int errorCount) {
			return this.Compile(text, out errorCount, out _);
		}

		private void CompileTest(string text, params byte[] expected) {
			int errorCount;
			byte[] actual = this.Compile(text, out errorCount);
			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(expected.Length, actual.Length);
			for(int i = 0; i < expected.Length; i++) {
				Assert.AreEqual(expected[i], actual[i]);
			}
		}

		private void CompileTest16(string text, params int[] expected) {
			int errorCount;
			byte[] actual = this.Compile(text, out errorCount);
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

		private void CompileTest32(string text, params int[] expected) {
			int errorCount;
			byte[] actual = this.Compile(text, out errorCount);
			Assert.AreEqual(0, errorCount);
			Assert.AreEqual(expected.Length * 4, actual.Length);
			int i = 0;
			using(MemoryStream stream = new MemoryStream(actual)) {
				using(BinaryReader reader = new BinaryReader(stream)) {
					foreach(int expectedItem in expected) {
						Assert.AreEqual(reader.ReadInt32(), expectedItem);
						//Assert.AreEqual(reader.ReadInt32(), actual[i++]);
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

		private void CompileErrorsTest(string text, string errorFragment) {
			string errors = this.CompileErrors(text);
			StringAssert.Matches(errors, new Regex(errorFragment, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				"Expected error fragment ({0}) not found in error message: {1}", errorFragment, errors
			);
		}

		/// <summary>
		///A test for Compile
		///</summary>
		[TestMethod()]
		public void AssemblerCompileTest() {
			// basic expression test
			this.CompileTest("macro main{1}", 1);
			this.CompileTest("macro main{1 2 0xb 0b10100101}", 1, 2, 0xb, 0xa5);
			this.CompileTest("macro main{2 a: 3 a}", 2, 3, 1);
			this.CompileTest("macro main{\"abc\"}", (byte)'a', (byte)'b', (byte)'c', 0);
			this.CompileTest("macro main{a 5 a 7} macro a p{p}", 5, 7);
			this.CompileTest("macro main{!3 !0 (+4) (-3) (~5)}", 0, 1, 4, (byte)((-3) & 0xFF), (byte)((~5) & 0xFF));
			this.CompileTest("macro main{(0||0) (5||0) (0||6) (4||7)}", 0, 1, 1, 1);
			this.CompileTest("macro main{(0&&0) (5&&0) (0&&6) (4&&7)}", 0, 0, 0, 1);
			this.CompileTest("macro main{(0<0) (-1<0) (0<1) (-3<-5) (6<3) (4<7)}", 0, 1, 1, 0, 0, 1);
			this.CompileTest("macro main{(0>0) (-1>0) (0>1) (-3>-5) (6>3) (4>7)}", 0, 0, 0, 1, 1, 0);
			this.CompileTest("macro main{(0<=0) (-1<=0) (0<=1) (-3<=-5) (6<=3) (4<=7) (2<=2)}", 1, 1, 1, 0, 0, 1, 1);
			this.CompileTest("macro main{(0>=0) (-1>=0) (0>=1) (-3>=-5) (6>=3) (4>=7) (2>=2)}", 1, 0, 0, 1, 1, 0, 1);
			this.CompileTest("macro main{(0==0) (1==1) (-2==-2) (3==4) (\"abc\"==\"abc\") (\"abc\"==\"def\")}", 1, 1, 1, 0, 1, 0);
			this.CompileTest("macro main{(0!=0) (1!=1) (-2!=-2) (3!=4) (\"abc\"!=\"abc\") (\"abc\"!=\"def\")}", 0, 0, 0, 1, 0, 1);

			this.CompileTest("macro main{(1+2) (-3+-4) (\"abc\"+\"def\")}", 3, (-7 & 0xFF), (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', 0);
			this.CompileTest("macro main{(\"abc\"+(3+4)+\"def\")}", (byte)'a', (byte)'b', (byte)'c', (byte)'7', (byte)'d', (byte)'e', (byte)'f', 0);
			this.CompileTest("macro main{5-2}", 3);
			this.CompileTest("macro main{4*2}", 8);
			this.CompileTest("macro main{7/2}", 3);
			this.CompileTest("macro main{7%2}", 1);
			this.CompileTest("macro main{5&3}", 1);
			this.CompileTest("macro main{5|3}", 7);
			this.CompileTest("macro main{5^3}", 6);
			this.CompileTest("macro main{5<<2}", 20);
			this.CompileTest("macro main{20>>2}", 5);

			this.CompileTest("macro main{if(1){5}else{6}}", 5);
			this.CompileTest("macro main{if(0){5}else{6}}", 6);
			this.CompileTest("macro main{if(0){5}}");
			this.CompileTest("macro main{if(3){5}}", 5);

			//test calls
			this.CompileTest("macro main{a 5 b 7} macro a p{p} macro b d{d*2}", 5, 14);
			this.CompileTest("macro main{1 a} macro a{2 3}", 1, 2, 3);
			// recursive one
			this.CompileTest("macro main{9 a 5 9} macro a n{if(0 < n){n a n - 1}}", 9, 5, 4, 3, 2, 1, 9);
			// parameter hides macro
			this.CompileTest("macro main{a b 5 a} macro a{3} macro b a{a}", 3, 5, 3);
			// label hides macro
			this.CompileTest("macro main{a 1 a: 5 a 3} macro a{8}", 2, 1, 5, 2, 3);

			//test for address pointer moves
			this.CompileTest("macro main{l1:a l2:a 0 l2 l1} macro a{5}", 5, 5, 0, 1, 0);
			this.CompileTest("macro main{l1:if(1<a){3}else{4}l2:a l2 l1} macro a{5}", 3, 5, 1, 0);
			this.CompileTest("macro main{l1:if(a>2*a){3}else{4}l2:a l2 l1} macro a{5}", 4, 5, 1, 0);
			this.CompileTest("macro main{\"ab\" l1:if(a>2*a){3}else{4}l2:a l2 l1} macro a{5}", 'a' & 0xFF, 'b' & 0xFF, 0, 4, 5, 4, 3);

			// passing labels to macro
			this.CompileTest("macro main{6 a:7 m a,b b:5} macro m a,b{a b}", 6, 7, 1, 4, 5);
			this.CompileTest("macro main{6 a:4 m a,b b:5} macro m a,b{n a,b 2 3} macro n a,b{7 a b 9}", 6, 4, 7, 1, 8, 9, 2, 3, 5);
			this.CompileTest("macro main{6 a b+100 b:5} macro a b{c b+10} macro c b{b}", 6, 112, 5);
			this.CompileTest("macro main{6 a:4 m a,b+100 b:5} macro m a,b{n a,10+b 2 3} macro n a,b{7 a o b+100 9} macro o p{10+p}", 6, 4, 7, 1, 228, 9, 2, 3, 5);
			this.CompileTest("macro main{0 a:1 m a,b,c b:2 c:3} macro m a,b,c{n a+10,b+c+20,c-a+30} macro n a,b,c{a+100 b c+200-b}", 0, 1, 111, 31, 204, 2, 3);

			// unresolved labels in expressions
			this.CompileTest("macro main{0 (-a) 2 a:3}", 0, (byte)(-3 & 0xFF), 2, 3);
			this.CompileTest("macro main{0 m -a 2 a:3} macro m x{x}", 0, (byte)(-3 & 0xFF), 2, 3);

			this.CompileTest("macro main{0 !a 2 a:3}", 0, 0, 2, 3);
			this.CompileTest("macro main{0 m !a 2 a:3} macro m x{x}", 0, 0, 2, 3);

			this.CompileTest("macro main{0 ~a 2 a:3}", 0, (byte)(~3 & 0xFF), 2, 3);
			this.CompileTest("macro main{0 m ~a 2 a:3} macro m x{x}", 0, (byte)(~3 & 0xFF), 2, 3);

			this.CompileTest("macro main{0 a || 0 2 a:3}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 m a || 0 2 a:3} macro m x{x}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 0 || a 2 a:3}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 m 0 || a 2 a:3} macro m x{x}", 0, 1, 2, 3);

			this.CompileTest("macro main{0 a && 10 2 a:3}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 m a && 10 2 a:3} macro m x{x}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 10 && a 2 a:3}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 m 10 && a 2 a:3} macro m x{x}", 0, 1, 2, 3);

			this.CompileTest("macro main{0 a < 10 2 a:3}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 m a < 10 2 a:3} macro m x{x}", 0, 1, 2, 3);
			this.CompileTest("macro main{0 10 < a 2 a:3}", 0, 0, 2, 3);
			this.CompileTest("macro main{0 m 10 < a 2 a:3} macro m x{x}", 0, 0, 2, 3);

			this.CompileTest("macro main{0 a < b 2 a:3 b:4}", 0, 1, 2, 3, 4);
			this.CompileTest("macro main{0 m a < b 2 a:3 b:4} macro m x{x}", 0, 1, 2, 3, 4);
			this.CompileTest("macro main{0 b < a 2 a:3 b:4}", 0, 0, 2, 3, 4);
			this.CompileTest("macro main{0 m b < a 2 a:3 b:4} macro m x{x}", 0, 0, 2, 3, 4);

			this.CompileTest("macro main{0 a > b 2 a:3 b:4}", 0, 0, 2, 3, 4);
			this.CompileTest("macro main{0 m a > b 2 a:3 b:4} macro m x{x}", 0, 0, 2, 3, 4);
			this.CompileTest("macro main{0 b > a 2 a:3 b:4}", 0, 1, 2, 3, 4);
			this.CompileTest("macro main{0 m b > a 2 a:3 b:4} macro m x{x}", 0, 1, 2, 3, 4);

			this.CompileTest("macro main{0 a + 10 2 a:3}", 0, 13, 2, 3);
			this.CompileTest("macro main{0 m a + 10 2 a:3} macro m x{x}", 0, 13, 2, 3);
			this.CompileTest("macro main{0 10 + a 2 a:3}", 0, 13, 2, 3);
			this.CompileTest("macro main{0 m 10 + a 2 a:3} macro m x{x}", 0, 13, 2, 3);
			this.CompileTest("macro main{0 a + b 2 a:3 b:4}", 0, 7, 2, 3, 4);
			this.CompileTest("macro main{0 m a + b 2 a:3 b:4} macro m x{x}", 0, 7, 2, 3, 4);
			this.CompileTest("macro main{0 b + a 2 a:3 b:4}", 0, 7, 2, 3, 4);
			this.CompileTest("macro main{0 m b + a 2 a:3 b:4} macro m x{x}", 0, 7, 2, 3, 4);

			this.CompileTest("macro main{0 a * 10 2 a:3}", 0, 30, 2, 3);
			this.CompileTest("macro main{0 m a & 10 2 a:3} macro m x{x}", 0, 2, 2, 3);
			this.CompileTest("macro main{0 10 / a 2 a:3}", 0, 3, 2, 3);
			this.CompileTest("macro main{0 m 10 | a 2 a:3} macro m x{x}", 0, 11, 2, 3);
			this.CompileTest("macro main{0 a - b 2 a:3 b:4}", 0, (byte)(-1 & 0xFF), 2, 3, 4);
			this.CompileTest("macro main{0 m a << b 2 a:3 b:4} macro m x{x}", 0, 0x30, 2, 3, 4);
			this.CompileTest("macro main{0 b & a 2 a:3 b:4}", 0, 0, 2, 3, 4);
			this.CompileTest("macro main{0 m b | a 2 a:3 b:4} macro m x{x}", 0, 7, 2, 3, 4);

			this.CompileTest("macro main{0 a:1 2 3 b:4 5 b-a}", 0, 1, 2, 3, 4, 5, 3);
			this.CompileTest("macro main{0 a:1 2 3 b:4 5 m b,a} macro m max, min{max-min+10}", 0, 1, 2, 3, 4, 5, 13);
			this.CompileTest("macro main{m b,a 0 a:1 2 3 b:4 5} macro m max, min{max-min+10}", 13, 0, 1, 2, 3, 4, 5);
		}

		/// <summary>
		///A test for Compilation errors
		///</summary>
		[TestMethod()]
		public void AssemblerCompileErrorTest() {
			this.CompileErrorsTest("macro a x{if(0<x){x}}macro main{1 a m m:2}", "Condition is incomplete value");
		}

		/// <summary>
		///A test for investigation of Compile errors
		///</summary>
		[TestMethod()]
		public void CompileSpecialCaseTest() {
			this.CompileTest("macro main{6 a:4 m a,b+100 b:5} macro m a,b{n a,10+b 2 3} macro n a,b{7 a o b+100 9} macro o p{10+p}", 6, 4, 7, 1, 228, 9, 2, 3, 5);
			this.CompileTest("macro main{6 a b+100 b:5} macro a b{c b+10} macro c b{b}", 6, 112, 5);

			// concatenation of defined labels is allowed
			this.CompileTest("macro main{1 2 3 a: 4 5 6 \"abc\"+a 7 8}", 1, 2, 3, 4, 5, 6, (byte)'a', (byte)'b', (byte)'c', (byte)'3', (byte)'\0', 7, 8);
			// concatenation of undefined labels does not allowed
			int errorCount;
			byte[] actual = this.Compile("macro main{1 \"abc\"+a 2 3 a: 4 5}", out errorCount);
			Assert.IsTrue(0 < errorCount);

			// if statement resulting in void value should be ignored to not affect arithmetic
			this.CompileTest("macro c a {if(a<0){error \"hello\"}a}macro main{1+c 3}", 4);
			this.CompileTest("macro c a {if(a<0){error \"world\"}a*3}macro main{4+c 5}", 19);
			// also check for sublist
			this.CompileTest("macro c a{if(a<0){error\"world\"}a*3}macro b a{if(a==1){c 10}else{if(a==2){c 20}else{error\"error\"}}}macro main{4+b 2}", 64);
			this.CompileTest("macro c a{if(a<0){error\"world\"}a*3}macro b a{if(a==2){c 10}else{if(a==1){error\"error\"}else{c a}}}macro main{4+b 4}", 16);
		}

		/// <summary>
		///A test for compiling to 16 and 32 binaries
		///</summary>
		[TestMethod()]
		public void CompileBinaryTypesTest() {
			this.CompileTest("binary 8 macro main{1 2 0xAB}", 1, 2, 0xAB);
			this.CompileTest("binary 8 macro main{1 2 0xCD a: 3 4 5 a}", 1, 2, 0xCD, 3, 4, 5, 3);
			this.CompileTest("binary 8 macro main{1 2 0xAD a: 3 4 5 a + 0x80}", 1, 2, 0xAD, 3, 4, 5, 0x83);
			this.CompileTest("binary 8 macro main{1 2 a 0xAD a: 3 4 5 a 6}", 1, 2, 4, 0xAD, 3, 4, 5, 4, 6 );

			this.CompileTest16("binary 16 macro main{1 2 0xABCD}", 1, 2, 0xABCD);
			this.CompileTest16("binary 16 macro main{1 2 0xABCD a: 3 4 5 a}", 1, 2, 0xABCD, 3, 4, 5, 3);
			this.CompileTest16("binary 16 macro main{1 2 0xABCD a: 3 4 5 a + 0x8000}", 1, 2, 0xABCD, 3, 4, 5, 0x8003);
			this.CompileTest16("binary 16 macro main{1 2 a 0xADBC a: 3 4 5 a 6}", 1, 2, 4, 0xADBC, 3, 4, 5, 4, 6 );

			this.CompileTest32("binary 32 macro main{1 2 0xABCD}", 1, 2, 0xABCD);
			this.CompileTest32("binary 32 macro main{1 2 0xABCD a: 3 4 5 a}", 1, 2, 0xABCD, 3, 4, 5, 3);
			this.CompileTest32("binary 32 macro main{1 2 0xABCD a: 3 4 5 a + 0x70000000}", 1, 2, 0xABCD, 3, 4, 5, 0x70000003);
			this.CompileTest32("binary 32 macro main{1 2 a 0x1234ADBC a: 3 4 5 a 6}", 1, 2, 4, 0x1234ADBC, 3, 4, 5, 4, 6 );
		}
	}
}
