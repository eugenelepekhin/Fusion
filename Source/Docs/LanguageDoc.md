# Fusion Language

<!--TOC-->
  - [Introduction](#introduction)
    - [Literals](#literals)
    - [Macro substitution](#macro-substitution)
    - [Binary output](#binary-output)
    - [Parameters](#parameters)
  - [Expressions](#expressions)
  - [Operators](#operators)
  - [Comments](#comments)
  - [Labels](#labels)
  - [Formal definition of the Fusion language](#formal-definition-of-the-fusion-language)
  - [Some practical examples:](#some-practical-examples)
    - [Defining and using addition assembly instruction for some hypothetical CPU](#defining-and-using-addition-assembly-instruction-for-some-hypothetical-cpu)
    - [Defining and using labels](#defining-and-using-labels)
    - [Filling up memory block with 0](#filling-up-memory-block-with-0)
  - [Command line options](#command-line-options)
<!--/TOC-->

## Introduction
A Fusion file is made up of macro definitions, include statements, or optional one definition of binary type output.
An include statement allows the reuse of previously defined macros and is structured as follows:

```
include "relativePath\file.ext"
```

A macro definition, the main element of compilation in the language, is structured as follows:

```
macro <MacroName> <ParameterList> { <MacroBody> }
```

It begins with the "macro" keyword followed by the macro's name, which should adhere to identifier rules similar to those in languages like C, C#, or VB.
The parameter list is optional and consists of comma-separated identifiers in its simplest form.
The macro body always starts with an open curly brace and ends with a closing one.
It comprises numbers, macro expansions, or statements such as "if", "print", or "error".

Every Fusion program must contain a macro called "main", which serves as the starting point for compilation and should not have any parameters.

Let's explore some examples:

```
macro main { 0 }
```

This defines the "main" macro, constituting a complete Fusion program that compiles without errors and produces a file containing one byte, with the value 0.

To test this, create a file in your text editor with the above line and save it with any extension you like (for example, Test1.asm).
Assuming you either have a copy of the Fusion.exe in current folder or added a path environment variable to a folder that contains it,
run the following command in a command window:

```
fusion Test1.asm Test1.bin
```

This will display the following output:

```
C:\FusionTest>fusion Test1.asm Test1.bin
0
    >>>>>00000000: 00
```

To view the content of the Test1.bin file, open it in a binary viewer or editor of your choice.
If you don't have one, you can use [LogicCircuit](https://logiccircuit.org/) by creating a ROM circuit and setting its data and address bit width to 8 bits each.
Then, in the ROM dialog, click the "Load…" button, select the bin file, and click "OK". You'll see the content of the file in the editor grid.
Alternatively you can generate text file with binary, decimal or hexadecimal representation of the binary output. For details see the command line options.

Another example:

```
macro main { 3 4 5 6 }
```

This will produce a 4-byte file.

### Literals

Each number in Fusion can be in one of the following forms:

-	Decimal number (e.g., 1, 3, 100, 1024)
-	Hexadecimal number (e.g., 0x1, 0xFF, 0x3c)
-	Binary number (e.g., 0b1, 0b0, 0b1010101)
-	Octal number (e.g., 0127)

You can separate digit in the numbers with underscore symbol or single quote.

Instead of number you can put a string in form:

```
"Some text you like to have here."
```

In the output it will be replaced with ASCII representation of all the characters between the quotas.
At the end it will be a 0 byte to indicate the end of the string.

### Macro substitution

Now let's look at some more complex example:

```
macro ThreeAndFive { 3 5 }
macro main { 4 ThreeAndFive 6 }
```

This two-line file will be compiled to 4 byte binary file with content: 4 3 5 6.
This is because the first macro defines two-byte output and main is calling it among its own values.
So, as you can see instead of any set of numbers it can be a macro call (or macro expansion).

Expressions are also supported in Fusion. For instance:

```
macro main { 3 + 4 }
```

This will produce one byte file with content 7.

### Binary output
By default the compiler will output each number as one byte.
If you need to output it as two byte number or 4 byte number you can specify this with "binary" keyword, folowed by number of output bits:

```
binary 16
```
or
```
binary 32
```

It is only possible to specify 8, 16, or 32 bit outputs.

### Parameters
Macros can have parameters, as shown in this example:

```
macro sum a, b { a + b }
macro main { 1 sum 2, 3 4 }
```

This will produce 3 byte output: 1, 5, 4.
Let see how it happened: main start expanding its body and the first number is 1, so it gets to the result.
Then it comes a call to sum macro. This macro defined with two parameters: a and b, so main will expect a list of two comma separated expressions.
In our example the expressions are trivial – just 2 and 3.
So now compiler will expand sum macro which is actually produce one number output which is sum of its’ parameters. This gives us 5 in the output.
Then main continue with its own body and that’s where 4 gets to output.

Fusion lets you declare parameter as index instead of just simple list of names:

```
macro load target, address[index] { target address + 4 * index }
macro main { load 3, 25[16] }
```

Here parameter "index" is defined in such way.
Inside macro you use such parameters as any other, but when this macro is called the expression passed to this parameter must be enclosed in square brackets.
Fusion allows multiple index parameters:

```
macro indexMadness one[two, three, four], [five][six, seven] {}
```

You can "overload" macros with another one with the same name if they have different number of parameters or use indices in different places:

```
macro myMacro target, source
macro myMacro target[source]
```

## Expressions

So far, we were using only + expression. Actually Fusion is following C language style of expressions so you can use:

|Symbol|Expression |
|------|---------- |
| +    |Addition   |
| -    |Subtraction|
| *    |Multiplication|
| /    |Division   |
| %    |Remainder  |
| &    |Bitwise AND|
| \|   |Bitwise OR |
| ^    |Bitwise NOT|
| <<   |Left shift |
| >>   |Right shift|
| <    |Less than  |
| \<=  |Less than or equal|
| ==   |Equal      |
| !=   |Not equal  |
| >=   |Greater than or equal|
| >    |Greater than|
| &&   |Logical AND|
| \|\| |Logical OR |

All comparisons and logical operators produce either 0 or 1 value.
You can group expression in parentheses ( and ).

## Operators

The language cannot be full without conditional operators. In Fusion it is "if" statement which comes in form:

```
if(condition) { true clause } [else { false clause }]
```

The else part is optional. The condition is an expression which is evaluated to some number.
If this number does not equal to zero then the true clause get expanded and it is equals to zero then false clause get expanded if it is present.
Let’s look at the examples:

```
macro max a, b { if(a > b) { a } else { b } }
macro main { max 1, 3 }
```

This will produce one byte output with value 3.

The last operator of the language is error statement it comes in form:

```
error "Error message"
```

When executed it will produce a compilation error and can be used to validate parameters of macro calls.
In order to help debugging of your fusion program you can use print statement. It works like error one but will not cause compilation errors.

## Comments
You can use comments in your file. To start comment put a semicolon at any place where you can have a white space.
From the semicolon to the end of the line will be ignored:

```
; Here is my first comment.
macro main
{ ; this will be ignored
	0
}
```

Like in C languages you can use multi-line comments:

```
/* Comment line 1
	Comment line 2
*/
```

## Labels
You can refer to position in the output file by defining a label in form:

```
LabelName:
```

Let look at this example:

```
macro Quote argument {
	argument
}

macro main {
	Quote 3
	Quote 4
MyLabel:
	Quote 5
	Quote MyLabel
	Quote 6
}
```

This will produce binary file with content: 3, 4, 5, 2, 6. Number 2 was the result of getting value of MyLabel which is second byte in the output.

During successful compilation Fusion will print out listing. In order to make the listing more readable you can prepend macro definition with "atomic" keyword.
This will put all the output of the macro in the listing in one line instead of breaking it up to each inner call.

## Formal definition of the Fusion language
Please refer to formal grammar in the two files: [FusionLexer.g4](../Fusion/FusionLexer.g4) and [FusionParser.g4](../Fusion/FusionParser.g4)
These two files are in the ANTLR4 syntax and used to generate the language parser.

## Some practical examples:

### Defining and using addition assembly instruction for some hypothetical CPU

```
; Define registers.
macro A { 0 }
macro B { 1 }
macro C { 2 }
macro D { 3 }

; Checks if register is valid value.
macro ValidateRegister register {
	if(!(0 <= register && register <= 3)) {
		error "Invalid register " + register
	}
}

; Defines Addition code of operation.
; This command adds register A with the provided register as a parameter.
; Code of operation is 8-bit number with high 6 bits code of addition
; and 2 low bits register number to add to A.
macro ADD register {
	; Make sure the register is a valid number
	ValidateRegister register
	; Code of ADD operation is 0b011001 concatenate it with register
; number to produce actual code of operation
	0b01100100 | register
}

; Defines halt CPU command
macro HALT {
	; code of operation for halt command is 0
	0
}

macro main {
	; sample program that adds value in register B to value in
; register A and store the result in register A, then it halts the execution
	ADD B
	HALT
}
```

### Defining and using labels

```
macro Quote argument {
	argument
}

macro main {
	Quote 3
	Quote 4
MyLabel:
	Quote 5
	Quote MyLabel
	Quote 6
}
```

### Filling up memory block with 0

```
macro Fill value, size {
	if(size > 0) {
		value
		Fill value, size - 1
	}
}

macro main {
	Fill 0, 10
}
```

## Command line options
To see command line options of the compiler just execute in command line the following command:

```
Fusion.exe /?
```

