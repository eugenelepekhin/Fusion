parser grammar FirstPassParser;

options {
	tokenVocab = FusionLexer;
}

fusionProgram: (binaryDecalration | include | macro)* EOF;	// binaryDecalration may only occurred once

binaryDecalration: Binary outputBitsPerNumber;
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

expr: If Open expr expr* Close Begin trueBranch=exprList End (Else Begin falseBranch=exprList End)?
	| Comma
	| name
	| Open | Close
	| Add | Not
	| Mul
	| Add
	| BitShift
	| Compare
	| Equality
	| BitAnd
	| BitXor
	| BitOr
	| BoolAnd
	| BoolOr
	| Print expr
	| NumberLiteral
	| StringLiteral
	;

name: (Identifier | Atomic | Macro | Binary | Include);
