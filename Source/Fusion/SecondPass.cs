using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fusion {
	public class SecondPass : FusionParserBaseVisitor<Expression> {
		private MacroDefinition? currentMacro;

		public Assembler Assembler { get; }

		public string File { get; }

		public SecondPass(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.File = file;
		}

		public override Expression VisitMacro([NotNull] FusionParser.MacroContext context) {
			string name = context.macroName().GetText();
			if(this.Assembler.Macro.TryGetValue(name, out MacroDefinition? macro)) {
				Debug.Assert(macro != null);
				this.currentMacro = macro;
				macro.Body = (ExpressionList)Visit(context.macroBody().exprList());
			} else {
				this.Assembler.FatalError(Resource.FileChanged(this.File));
			}
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitExprList([NotNull] FusionParser.ExprListContext context) {
			ExpressionList expressionList = new ExpressionList();
			if(0 < context.ChildCount) {
				foreach(IParseTree childContext in context.children) {
					expressionList.List.Add(Visit(childContext));
				}
				return expressionList;
			}
			return expressionList;
		}

		public override Expression VisitLabel([NotNull] FusionParser.LabelContext context) {
			return new Label(new Token(TokenType.Identifier, context.Start, this.File));
		}

		public override Expression VisitPrint([NotNull] FusionParser.PrintContext context) {
			return new Print(new Token(TokenType.Identifier, context.Start, this.File), Visit(context.expr()));
		}

		public override Expression VisitIf([NotNull] FusionParser.IfContext context) {
			return new IfExpr(new Token(TokenType.Identifier, context.Start, this.File), then: (ExpressionList)Visit(context.trueBranch), @else: (context.falseBranch != null) ? ((ExpressionList)Visit(context.falseBranch)) : null, condition: Visit(context.cond), context: null);
		}

		public override Expression VisitCall([NotNull] FusionParser.CallContext context) {
			Token name = new Token(TokenType.Identifier, context.macroName().Start, this.File);
			Debug.Assert(!string.IsNullOrWhiteSpace(name.Value));
			if(this.Assembler.Macro.TryGetValue(name.Value, out var macro)) {
				FusionParser.ArgumentsContext args = context.arguments();
				List<Expression> arguments = ((args != null) ? (from e in args.expr()
																select Visit(e)).ToList() : new List<Expression>());
				if(arguments.Count == macro.Parameters.Count) {
					return new CallExpr(name, macro, arguments);
				}
				this.Assembler.Error(Resource.ArgumentCount(arguments.Count, macro.Parameters.Count, macro.Name.Value));
			} else {
				this.Assembler.Error(Resource.UndefinedMacro(name.Value, name.Position));
			}
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitParenExpr([NotNull] FusionParser.ParenExprContext context) {
			return Visit(context.expr());
		}

		public override Expression VisitUnary([NotNull] FusionParser.UnaryContext context) {
			return new Unary(new Token(TokenType.Operator, context.Start, this.File), Visit(context.expr()), null);
		}

		public override Expression VisitBin([NotNull] FusionParser.BinContext context) {
			return new BinaryExpr(Visit(context.left), right: Visit(context.right), operation: new Token(TokenType.Operator, context.op, this.File), context: null);
		}

		public override Expression VisitLocalName([NotNull] FusionParser.LocalNameContext context) {
			Token name = new Token(TokenType.Identifier, context.Start, this.File);
			if(this.currentMacro!.IsLabel(name)) {
				return new LabelReference(name, null);
			}
			if(this.currentMacro!.IsParameter(name)) {
				return new Parameter(this.currentMacro, name);
			}
			this.Assembler.Error(Resource.UndefinedMacro(name.Value, name.Position));
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitLiteral([NotNull] FusionParser.LiteralContext context) {
			Token start = new Token((context.NumberLiteral() == null) ? TokenType.String : TokenType.Number, context.Start, this.File);
			Debug.Assert(!string.IsNullOrWhiteSpace(start.Value));
			if(start.TokenType == TokenType.Number && !Token.IsValidNumber(start.Value)) {
				this.Assembler.Error(Resource.BadNumberFormat(start.Value, start.Position));
				return new ValueExpression(VoidValue.Value);
			}
			return new Literal(start);
		}
	}
}
