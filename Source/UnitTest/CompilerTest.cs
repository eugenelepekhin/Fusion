using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {

	/// <summary>
	///This is a test class for Assembler.Compile
	///</summary>
	[TestClass()]
	public class CompilerTest {

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

		private void CompileTest(string text, params byte[] expected) {
			string file = Path.Combine(this.TestContext.TestDeploymentDir, "AssemblerCompileTest.asm");
			File.WriteAllText(file, text);
			byte[] actual = null;
			using(MemoryStream stream = new MemoryStream(16 * 1024)) {
				using(BinaryWriter writer = new BinaryWriter(stream)) {
					Assembler assembler = AssemblerFactory.Create(this.TestContext, writer);
					assembler.Compile(file);
					Assert.AreEqual(0, assembler.ErrorCount);
					if(assembler.ErrorCount <= 0) {
						writer.Flush();
						actual = stream.ToArray();
					}
				}
			}
			Assert.AreEqual(expected.Length, actual.Length);
			for(int i = 0; i < expected.Length; i++) {
				Assert.AreEqual(expected[i], actual[i]);
			}
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
			this.CompileTest("macro main{(0||0) (5||0) (0||6) (4||7)}", 0, 5, 6, 4);
			this.CompileTest("macro main{(0&&0) (5&&0) (0&&6) (4&&7)}", 0, 0, 0, 7);
			this.CompileTest("macro main{(0<0) (-1<0) (0<1) (-3<-5) (6<3) (4<7)}", 0, 1, 1, 0, 0, 1);
			this.CompileTest("macro main{(0>0) (-1>0) (0>1) (-3>-5) (6>3) (4>7)}", 0, 0, 0, 1, 1, 0);
			this.CompileTest("macro main{(0<=0) (-1<=0) (0<=1) (-3<=-5) (6<=3) (4<=7) (2<=2)}", 1, 1, 1, 0, 0, 1, 1);
			this.CompileTest("macro main{(0>=0) (-1>=0) (0>=1) (-3>=-5) (6>=3) (4>=7) (2>=2)}", 1, 0, 0, 1, 1, 0, 1);
			this.CompileTest("macro main{(0==0) (1==1) (-2==-2) (3==4) (\"abc\"==\"abc\") (\"abc\"==\"def\")}", 1, 1, 1, 0, 1, 0);
			this.CompileTest("macro main{(0!=0) (1!=1) (-2!=-2) (3!=4) (\"abc\"!=\"abc\") (\"abc\"!=\"def\")}", 0, 0, 0, 1, 0, 1);

			this.CompileTest("macro main{(1+2) (-3+-4) (\"abc\"+\"def\")}", 3, (-7 & 0xFF), (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', 0);
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

			this.CompileTest("macro main{0 a || 0 2 a:3}", 0, 3, 2, 3);
			this.CompileTest("macro main{0 m a || 0 2 a:3} macro m x{x}", 0, 3, 2, 3);
			this.CompileTest("macro main{0 0 || a 2 a:3}", 0, 3, 2, 3);
			this.CompileTest("macro main{0 m 0 || a 2 a:3} macro m x{x}", 0, 3, 2, 3);

			this.CompileTest("macro main{0 a && 10 2 a:3}", 0, 10, 2, 3);
			this.CompileTest("macro main{0 m a && 10 2 a:3} macro m x{x}", 0, 10, 2, 3);
			this.CompileTest("macro main{0 10 && a 2 a:3}", 0, 3, 2, 3);
			this.CompileTest("macro main{0 m 10 && a 2 a:3} macro m x{x}", 0, 3, 2, 3);

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
		///A test for investigation of Compile errors
		///</summary>
		[TestMethod()]
		public void CompileSpecialCase() {
			this.CompileTest("macro main{6 a:4 m a,b+100 b:5} macro m a,b{n a,10+b 2 3} macro n a,b{7 a o b+100 9} macro o p{10+p}", 6, 4, 7, 1, 228, 9, 2, 3, 5);
			this.CompileTest("macro main{6 a b+100 b:5} macro a b{c b+10} macro c b{b}", 6, 112, 5);
		}
	}
}
