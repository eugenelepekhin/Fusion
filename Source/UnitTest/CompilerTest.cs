﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest {
	[TestClass]
	public class CompilerTest : CompileTestBase {
		[TestMethod]
		public void BasicExprTest() {
			this.CompileTest("macro main{1 2 0xb 0b10100101}", 1, 2, 0xb, 0b10100101);
			this.CompileTest("macro main{2 a: 3 a}", 2, 3, 1);
			this.CompileTest("macro main{a 5 a 7} macro a p{p}", 5, 7);
			this.CompileTest("macro main{a 5 a b} macro a p{p} macro b{20 30}", 5, 20, 30);
			this.CompileTest("macro main{a 3 a f 5} macro a p{p} macro f n{if(0<n){f n-1 n}}", 3, 1, 2, 3, 4, 5);
		}

		[TestMethod]
		public void ArithmeticExprTest() {
			this.CompileTest("macro main{!3 !0 (+4) (-3) (~5)}", 0, 1, 4, (byte)((-3) & 0xFF), (byte)((~5) & 0xFF));

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

			this.CompileTest("macro main{(0||0) (5||0) (0||6) (4||7)}", 0, 1, 1, 1);
			this.CompileTest("macro main{(0&&0) (5&&0) (0&&6) (4&&7)}", 0, 0, 0, 1);
			this.CompileTest("macro main{(0<0) (-1<0) (0<1) (-3<-5) (6<3) (4<7)}", 0, 1, 1, 0, 0, 1);
			this.CompileTest("macro main{(0>0) (-1>0) (0>1) (-3>-5) (6>3) (4>7)}", 0, 0, 0, 1, 1, 0);
			this.CompileTest("macro main{(0<=0) (-1<=0) (0<=1) (-3<=-5) (6<=3) (4<=7) (2<=2)}", 1, 1, 1, 0, 0, 1, 1);
			this.CompileTest("macro main{(0>=0) (-1>=0) (0>=1) (-3>=-5) (6>=3) (4>=7) (2>=2)}", 1, 0, 0, 1, 1, 0, 1);
			this.CompileTest("macro main{(0==0) (1==1) (-2==-2) (3==4) (\"abc\"==\"abc\") (\"abc\"==\"def\")}", 1, 1, 1, 0, 1, 0);
			this.CompileTest("macro main{(0!=0) (1!=1) (-2!=-2) (3!=4) (\"abc\"!=\"abc\") (\"abc\"!=\"def\")}", 0, 0, 0, 1, 0, 1);
		}

		[TestMethod]
		public void ExprPrecedenceTest() {
			this.CompileTest("macro main{(2+3)*4}", 20);
			this.CompileTest("macro main{2+3*4}", 14);
			this.CompileTest("macro a{7} macro main{a+a*a}", 7+7*7);
			this.CompileTest("macro main{-2+3}", 1);
			this.CompileTest("macro main{+-+3}", (byte)((-3) & 0xFF));
			this.CompileTest("macro add a,b{a+b} macro main{add 1,2+3}", 1 + 2 + 3);
			this.CompileTest("macro dub a{a*2} macro main{dub 2+3 (dub 2)+3}", 10, 7);

			this.CompileTest("macro main{1 << 2 + 3 0b10'000'000 >> 2 + 3}", 1 << 2 + 3, 0b10_000_000 >> 2 + 3);
			this.CompileTest("macro main{1 < 1 << 2}", 1 < 1 << 2 ? 1 : 0);
			this.CompileTest("macro main{0 <= 1 != 1 0 <= (1 != 1)}", 0, 1);
			this.CompileTest("macro main{0 < 3 & 2  0 < (3 & 2)}", 0, 1);

			this.CompileTest("macro main{2 < 3 && 2 2 < (3 && 2)}", 1, 0);
		}

		[TestMethod]
		public void IfExprTest() {
			this.CompileTest("macro main{if(1){5}else{6}}", 5);
			this.CompileTest("macro main{if(0){5}else{6}}", 6);
			this.CompileTest("macro main{if(0){5}}");
			this.CompileTest("macro main{if(3){5}}", 5);
			this.CompileTest("macro m n{if(n<3){2}else{3}} macro main{1 (m a) a:3}", 1, 2, 3);

			this.CompileTest("macro quote n{n} macro abs n{if(0<=n){quote n}else{quote -n}} macro main{1 abs 2 abs -5 3}", 1, 2, 5, 3);
		}

		[TestMethod]
		public void PrintTest() {
			this.CompileTest("macro m v{print \"hello \"+v} macro main{m 1}");
			this.CompileErrorsTest("macro m v{error \"hello \"+v} macro main{m 1}", "hello 1");
		}

		[TestMethod]
		public void CallTest() {
			this.CompileTest("macro main{a 5 b 7} macro a p{p} macro b d{d*2}", 5, 14);
			this.CompileTest("macro main{1 a} macro a{2 3}", 1, 2, 3);
			// recursive one
			this.CompileTest("macro main{9 a 5 9} macro a n{if(0 < n){n a n - 1}}", 9, 5, 4, 3, 2, 1, 9);
			// parameter hides macro
			this.CompileTest("macro main{a b 5 a} macro a{3} macro b a{a}", 3, 5, 3);
			// label hides macro
			this.CompileTest("macro main{a 1 a: 5 a 3} macro a{8}", 2, 1, 5, 2, 3);

			// mix of parameters and macro calls
			this.CompileTest("macro main{test 1, 2, 3} macro test a,b,c{sum b,mul a,sum b,c} macro mul a,b{a*b} macro sum a,b{a+b}", 7);

			this.CompileTest("macro a p{p+1} macro b{3} macro c p{p*2} macro d p{p+10} macro f p{p+4} macro g{5} macro main{a b c d f g}", 4, 19 * 2);

			// allowed keyword as macro name
			this.CompileTest("macro atomic p{p+1} macro main{atomic 3}", 4);
			this.CompileTest("macro macro p{p+2} macro main{macro 4}", 6);
			this.CompileTest("macro binary p{p+3} macro main{binary 5}", 8);
			this.CompileTest("macro include p{p+4} macro main{include 6}", 10);

			// allowed keyword as parameter name
			this.CompileTest("macro foo atomic{atomic+1} macro main{foo 10}", 11);
			this.CompileTest("macro foo macro{macro+2} macro main{foo 11}", 13);
			this.CompileTest("macro foo binary{binary+3} macro main{foo 12}", 15);
			this.CompileTest("macro foo include{include+4} macro main{foo 13}", 17);

			this.CompileErrorsTest("macro foo a, b{a+b} macro main{foo 1}", "Actual arguments does not match any macro foo declarations at");
			this.CompileErrorsTest("macro foo a, b{a+b} macro main{foo 1, 2, 3}", "Actual arguments does not match any macro foo declarations at");

			// test for overload macros
			this.CompileTest("macro main{a 1,2 a 3,4,5} macro a b, c{b c} macro a b, c, d{b c d}", 1, 2, 3, 4, 5);
			this.CompileErrorsTest("macro foo a, b{a+b} macro foo a, b, c{a+b+c} macro main{foo 1}", "Actual arguments does not match any macro foo declarations at");
			this.CompileErrorsTest("macro foo {1} macro main{bar 2}", "Undefined macro bar at");
			this.CompileErrorsTest("macro a b[c] {b c} macro a b[c]{b c} macro main{a 5[6]}", "Macro a redefined at");

			// macro without parameters can't be overloaded.
			this.CompileErrorsTest("macro a{1} macro a b{b} macro main{a a 5}", "Macro a redefined at");
			this.CompileErrorsTest("macro a{1} macro a [b]{b} macro main{a a[5]}", "Macro a redefined at");

			// test for index calls
			this.CompileTest("macro a b[c]{b c} macro main{a 5[6]}", 5, 6);
			this.CompileErrorsTest("macro a b[c]{b c} macro main{a 5,6}", "Actual arguments does not match any macro a declarations at");
			this.CompileTest("macro a b[c,d][e,f,g],h{b c d e f g h} macro main{a 5[6,7][8,9,0],1}", 5, 6, 7, 8, 9, 0, 1);
			this.CompileErrorsTest("macro a b[c,d][e,f,g],h{b c d e f g h} macro main{a 5[6,7][8,9,0],[1]}", "Actual arguments does not match any macro a declarations at");
			this.CompileTest("macro a [c,d][e,f,g],h{c d e f g h} macro main{a [6,7][8,9,0],1}", 6, 7, 8, 9, 0, 1);
			// test index overload and index expressions
			this.CompileTest("macro a b{b} macro a[b]{b+1} macro main{a 3 a 4 a[5] a[8*4]}", 3, 4, 6, 33);

			string overloadA1 = (
				"macro a b,c  {b+c+1}" +
				"macro a b[c] {b+c+2}" +
				"macro a b,[c]{b+c+3}" +
				"macro a [b],c{b+c+4}" +
				"macro a [b,c]{b+c+5}" +
				"macro main{a 1,2 a 3[4] a 5,[6] a[7],8 a[9,10]}"
			);
			this.CompileTest(overloadA1, 4, 9, 14, 19, 24);

			string overloadA2 = (
				"macro a a,b,c{a+b+c+1}" +
				"macro a [a],b,c{a+b+c+2}" +
				"macro a [a,b],c{a+b+c+3}" +
				"macro a [a,b,c]{a+b+c+4}" +
				"macro main{" +
				"a 1,2,3 " +
				"a [4],5,6 " +
				"a [7,8],9 " +
				"a [10,11,12]"+
				"}"
			);
			this.CompileTest(overloadA2, 7,17,27,37);

			this.CompileErrorsTest("macro a b[b]{1} macro main{a 5[6]}", "Macro a already contains parameter b");
			this.CompileErrorsTest("macro a a[b],b{1} macro main{a 5[6],7}", "Macro a already contains parameter b");
			this.CompileErrorsTest("macro a a[],b{1} macro main{a 5[],7}", "Syntax error: missing {'atomic', 'macro', 'binary', 'include', Identifier} at ']'");
			this.CompileErrorsTest("macro a a[b],c{1} macro main{a 5[],7}", "Syntax error: mismatched input ']'");
		}

		[TestMethod]
		public void LabelTest() {
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

			this.CompileTest("atomic macro atomicFoo n{if(n<3){11}else{12}} macro main{1 atomicFoo 2 3}", 1, 11, 3);

			// label keyword
			this.CompileTest("macro main{1 2 3 binary: 4 5 binary 6}", 1, 2, 3, 4, 5, 3, 6);
			this.CompileTest("macro main{1 2 3 main: 4 5 main 6}", 1, 2, 3, 4, 5, 3, 6);
			this.CompileTest("macro main{1 2 3 atomic: 4 5 atomic 6}", 1, 2, 3, 4, 5, 3, 6);
			this.CompileTest("macro main{1 2 3 atomic: 4 5 atomic 6}", 1, 2, 3, 4, 5, 3, 6);
			this.CompileTest("macro main{1 2 3 macro: 4 5 macro 6}", 1, 2, 3, 4, 5, 3, 6);
		}


		/// <summary>
		///A test for Compilation errors
		///</summary>
		[TestMethod()]
		public void AssemblerCompileErrorTest() {
			this.CompileErrorsTest("macro hello {1}", "Macro \"main\" is missing");
			this.CompileErrorsTest("macro main arg {1 arg}", "Macro main should not have any parameters");

			this.CompileErrorsTest("binary 8 binary 8 macro main{1}", "Binary type already defined at");
			this.CompileErrorsTest("binary 16 binary 8 macro main{1}", "Binary type already defined at");
			this.CompileErrorsTest("binary 13 macro main{1}", "Expected binary format type 8, 16, or 32 instead of 13 at");

			this.SyntaxErrorTest("atomic atomic main{1}");
			this.SyntaxErrorTest("macro {1}");
			this.SyntaxErrorTest("macro 3{2} macro main{1 3}");
			this.CompileErrorsTest("macro a{1} macro a{2} macro main{10 a 20}", "Macro a redefined at");

			this.SyntaxErrorTest("macro print a{33 a} macro main{1 print 2 3}");
			this.SyntaxErrorTest("macro error a{33 a} macro main{1 error 2 3}");
			this.SyntaxErrorTest("macro if a{33 a} macro main{1 if 2 3}");
			this.SyntaxErrorTest("macro else a{33 a} macro main{1 else 2 3}");

			this.CompileErrorsTest("macro foo a, a{2} macro main{1 foo 2, 3 4}", "Macro foo already contains parameter a at");
			//this.CompileErrorsTest("macro foo macro{33} macro main{1 foo 2 3}", "Name of parameter can not be a keyword \"macro\" at");
			this.SyntaxErrorTest("macro foo print{33} macro main{1 foo 2 3}");
			this.SyntaxErrorTest("macro foo error{33} macro main{1 foo 2 3}");
			this.SyntaxErrorTest("macro foo if{33} macro main{1 foo 2 3}");
			this.SyntaxErrorTest("macro foo else{33} macro main{1 foo 2 3}");

			this.SyntaxErrorTest("macro main{");
			this.SyntaxErrorTest("macro main{(");

			this.CompileErrorsTest("macro main{1 foo 2 3}", "Undefined macro foo at");

			this.CompileErrorsTest("macro a x{if(0<x){x}}macro main{1 a m m:2}", "Condition is incomplete value");
			this.CompileErrorsTest("macro main{2345678901}", "Bad format of number:");

			this.CompileErrorsTest("macro two{1 2} macro main{print two}", "String value expected:");
			this.CompileErrorsTest("macro a{error\"hello world\"}\nmacro main\n{a}", @"hello world:\s*in macro a at");
			this.CompileErrorsTest("macro neg a{-a}\nmacro main{neg \"string\"}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(a || b){1}else{2}}\nmacro main{toBool 0, two}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(a && b){1}else{2}}\nmacro main{toBool 1, two}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(a != b){1}else{2}}\nmacro main{toBool 1, two}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(b != a){1}else{2}}\nmacro main{toBool 1, two}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(b != a){1}else{2}}\nmacro main{toBool \"hello\", two}", @"Single number value expected:");
			this.CompileErrorsTest("macro two{1 2} macro toBool a, b{if(a != b){1}else{2}}\nmacro main{toBool \"hello\", two}", @"Single number value expected:");

			this.CompileErrorsTest("macro main{1 a:2 3 a:4 5}", "Label a redefined in macro main at");
			this.CompileErrorsTest("macro main{1 print\"hello \" + label 2 label:3}", "String concatenation is incomplete value. Only already defined labels can be used in string concatenation");
			this.CompileErrorsTest("macro main{1 print label 2 label:3}", "Inconclusive error message");
			this.CompileErrorsTest("macro two{1 2} macro main{1 if(0 || two){error 3} 2 label:4}", "Single number value expected:");

			this.CompileErrorsTest("macro main{300}", @"Attempt to write too big number \(300\)");
			this.CompileErrorsTest("binary 16 macro main{70000}", @"Attempt to write too big number \(70000\)");

			this.CompileErrorsTest("macro foo a{if(a<3){1}else{2 3}} macro main{1 foo a 2 a:3 4}", "Condition is incomplete value. Only already defined labels can be used in condition:");

			// label keyword
			this.SyntaxErrorTest("macro main{1 2 print: 3 4 print 5}");
			this.SyntaxErrorTest("macro main{1 2 error: 3 4 error 5}");
			this.SyntaxErrorTest("macro main{1 2 if: 3 4 if 5}");
			this.SyntaxErrorTest("macro main{1 2 else: 3 4 else 5}");

			// label hides parameter
			this.CompileErrorsTest("macro a b{b 1 b: 2} macro main{a 8}", "Label b hides parameter in macro a at");
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
			byte[]? actual = this.Compile("macro main{1 \"abc\"+a 2 3 a: 4 5}", out errorCount);
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

		[TestMethod]
		public void CompileTextOutputTest() {
			void test(Fusion.OutputWriter.OutputType outputType, int rowWidth, string input, params string[] expected) {
				string? output = this.CompileToText(input, outputType, rowWidth, out int errorCount, out string errors);
				Assert.AreEqual(0, errorCount);
				Assert.IsNotNull(output);
				string[] actual = output.Split([" ", "\r\n"], StringSplitOptions.RemoveEmptyEntries);
				this.AreEqual(expected, actual);
			}

			test(Fusion.OutputWriter.OutputType.TextDecimal, 16, "macro main{1 2}", "1", "2");
			test(Fusion.OutputWriter.OutputType.TextDecimal, 2, "macro main{1 2 3}", "1", "2", "3");
			test(Fusion.OutputWriter.OutputType.TextBinary, 2, "macro main{2 3 5 7}", "00000010", "00000011", "00000101", "00000111");
			test(Fusion.OutputWriter.OutputType.TextHexadecimal, 2, "macro main{2 3 0xa5 0xc7}", "02", "03", "A5", "C7");
		}

		[TestMethod]
		public void CompileListingTest() {
			string unspace(string input) => Regex.Replace(input, @"\s", "");
			void test(string text, string[] expected) {
				Assert.IsNotNull(this.TestContext);
				Debug.WriteLine("Checking listing of:");
				Debug.WriteLine(text);

				string listing = unspace(this.CompileListing(text));
				foreach(string line in expected) {
					StringAssert.Contains(listing, unspace(line));
				}
			}
			test(
				"macro argsCall x{if(x<5){args 1[2,3]}else{args 4[5,6]}}" +
				"macro args a[b,c]{a b c}" +
				"macro main{argsCall 5}",
				["args 1[2, 3]", "args 4[5, 6]"]
			);
			test(
				"macro a b{b}" +
				"macro call{a 5}" +
				"macro main{call}",
				["a 5"]
			);
			test(
				"macro a [b]{b}" +
				"macro call{a [5]}" +
				"macro main{call}",
				["a [5]"]
			);
			test(
				"macro a b[c]{b c}" +
				"macro call{a 3[5]}" +
				"macro main{call}",
				["a 3[5]"]
			);
			test(
				"macro a [b][c]{b c}" +
				"macro call{a [3][5]}" +
				"macro main{call}",
				["a [3][5]"]
			);
			test(
				"macro a [b,c][d]{b c d}" +
				"macro call{a [3,4][5]}" +
				"macro main{call}",
				["a [3,4][5]"]
			);
			test(
				"macro a [b,c],[d]{b c d}" +
				"macro call{a [3,4],[5]}" +
				"macro main{call}",
				["a [3,4],[5]"]
			);
			//Check the right overload is used and listed
			test(
				"macro a [b,c],[d]{b+c+d}" +
				"macro a [b,c],d{b c d}" +
				"macro call{a [3,4],5}" +
				"macro main{call}",
				["a [3,4],5"]
			);
			test(
				"macro a [b,c],[d]{b+c+d}" +
				"macro a [b,c],d{b c d}" +
				"macro call{a [3,4],[5]}" +
				"macro main{call}",
				["a [3,4],[5]"]
			);
		}
	}
}
