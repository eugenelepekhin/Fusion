parser grammar FusionParser;

options {
	tokenVocab = FusionLexer;
}

@parser::members {
	/*
	This section is working only in the second pass of the compiler.
	The predicates field will be set before the second pass begins.
	*/
	public Predicates predicates = null;
	bool IsMacroN() => predicates.IsMacroN(CurrentToken.Text);
	bool IsMacro0() => predicates.IsMacro0(CurrentToken.Text);
	bool IsNotMacro() => predicates.IsNotMacro(CurrentToken.Text);
}

fusionProgram: (binaryDeclaration | include | macro)* EOF;	// binaryDeclaration may only occurred once

binaryDeclaration: Binary outputBitsPerNumber;
outputBitsPerNumber: NumberLiteral;

include: Include filePath;
filePath: StringLiteral;

macro: Atomic? Macro macroName parameterList? Begin macroBody End;
macroName: name;
parameterList: parameterName (Comma parameterName)*;
parameterName: name;

macroBody: exprList;
exprList: (label | expr)*;

label: labelName Colon;
labelName: name;

expr: If Open cond=expr Close Begin trueBranch=exprList End (Else Begin falseBranch=exprList End)? #If
	| {IsMacroN()}? macroName arguments	#Call
	| {IsMacro0()}? macroName			#Call
	| Open expr Close					#ParenExpr
	| (Add | Not) expr					#Unary
	| left=expr op=Mul right=expr		#Bin
	| left=expr op=Add right=expr		#Bin
	| left=expr op=BitShift right=expr	#Bin
	| left=expr op=Compare right=expr	#Bin
	| left=expr op=Equality right=expr	#Bin
	| left=expr op=BitAnd right=expr	#Bin
	| left=expr op=BitXor right=expr	#Bin
	| left=expr op=BitOr right=expr		#Bin
	| left=expr op=BoolAnd right=expr	#Bin
	| left=expr op=BoolOr right=expr	#Bin
	| Print expr						#Print
	| {IsNotMacro()}? name				#LocalName
	| NumberLiteral						#Literal
	| StringLiteral						#Literal
	;

arguments: expr (Comma expr)*;
name: (Identifier | Atomic | Macro | Binary | Include);
