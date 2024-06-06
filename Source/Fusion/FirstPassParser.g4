parser grammar FirstPassParser;

options {
	tokenVocab = FusionLexer;
}

fusionProgram: (binaryDeclaration | include | macro)* EOF;	// binaryDeclaration may only occurred once

binaryDeclaration: Binary outputBitsPerNumber;
outputBitsPerNumber: NumberLiteral;

include: Include filePath;
filePath: StringLiteral;

macro: Atomic? Macro macroName parameterList? Begin macroBody End;
macroName: name;
parameterList: parameterDeclaration (Comma parameterDeclaration)*;
parameterDeclaration: (parameterName indexDeclaration*) | indexDeclaration+;
parameterName: name;
indexDeclaration: OpenBox indexName (Comma indexName)* CloseBox;
indexName: name;

macroBody: exprList;
exprList: (label | expr)*;

label: labelName Colon;
labelName: name;

expr: If Open expr expr* Close Begin trueBranch=exprList End (Else Begin falseBranch=exprList End)?
	| Comma
	| name
	| Open | Close | OpenBox | CloseBox
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
