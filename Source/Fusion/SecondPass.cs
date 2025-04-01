using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
			Token name = new Token(TokenType.Identifier, context.macroName().Start, this.File);
			MacroDefinition? macro = this.Assembler.Macro.Select(name.Value!).FirstOrDefault(m => m.Name.SameToken(name));
			if(macro != null) {
				this.currentMacro = macro;
				macro.Body = (ExpressionList)this.Visit(context.macroBody().exprList());
			} else {
				this.Assembler.FatalError(Resource.FileChanged(this.File));
			}
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitExprList([NotNull] FusionParser.ExprListContext context) {
			ExpressionList expressionList = new ExpressionList();
			if(0 < context.ChildCount) {
				foreach(IParseTree childContext in context.children) {
					expressionList.List.Add(this.Visit(childContext));
				}
				return expressionList;
			}
			return expressionList;
		}

		public override Expression VisitLabel([NotNull] FusionParser.LabelContext context) {
			return new Label(new Token(TokenType.Identifier, context.Start, this.File));
		}

		public override Expression VisitPrint([NotNull] FusionParser.PrintContext context) {
			return new Print(new Token(TokenType.Identifier, context.Start, this.File), this.Visit(context.expr()));
		}

		public override Expression VisitIf([NotNull] FusionParser.IfContext context) {
			return new IfExpr(
				new Token(TokenType.Identifier, context.Start, this.File),
				condition: this.Visit(context.cond),
				then: (ExpressionList)this.Visit(context.trueBranch),
				@else: (context.falseBranch != null) ? ((ExpressionList)this.Visit(context.falseBranch)) : null,
				context: null
			);
		}

		public override Expression VisitCall([NotNull] FusionParser.CallContext context) {
			Token name = new Token(TokenType.Identifier, context.macroName().Start, this.File);
			Debug.Assert(!string.IsNullOrWhiteSpace(name.Value));
			List<Expression> arguments = new List<Expression>();
			StringBuilder callPattern = new StringBuilder();
			FusionParser.ArgumentsContext args = context.arguments();
			if(args != null) {
				Debug.Assert(args.argument() != null);
				bool comma = false;
				foreach(FusionParser.ArgumentContext arg in args.argument()) {
					if(comma) {
						callPattern.Append(',');
					}
					comma = true;
					FusionParser.ExprContext exprContext = arg.expr();
					if(exprContext != null) {
						arguments.Add(this.Visit(exprContext));
						callPattern.Append('$');
					}
					FusionParser.IndexExprListContext[] indexExprListContext = arg.indexExprList();
					if(indexExprListContext != null) {
						bool innerComma = false;
						foreach(FusionParser.IndexExprListContext index in indexExprListContext) {
							callPattern.Append('[');
							foreach(FusionParser.ExprContext indexExpr in index.expr()) {
								arguments.Add(this.Visit(indexExpr));
								if(innerComma) {
									callPattern.Append(',');
								}
								innerComma = true;
								callPattern.Append('$');
							}
							callPattern.Append(']');
							innerComma = false;
						}
					}
				}
			}
			MacroDefinition? macro = this.Assembler.Macro.Find(name.Value, callPattern.ToString());
			if(macro != null) {
				Debug.Assert(macro.Parameters.Count == arguments.Count);
				return new CallExpr(name, macro, arguments);
			} else {
				if(0 <= this.Assembler.Macro.MinParameters(name.Value)) {
					this.Assembler.Error(Resource.ArgumentMismatch(name.Value, name.Position));
				} else {
					this.Assembler.Error(Resource.UndefinedMacro(name.Value, name.Position));
				}
			}
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitParenExpr([NotNull] FusionParser.ParenExprContext context) {
			return this.Visit(context.expr());
		}

		public override Expression VisitUnary([NotNull] FusionParser.UnaryContext context) {
			return new Unary(new Token(TokenType.Operator, context.Start, this.File), this.Visit(context.expr()), null);
		}

		public override Expression VisitBin([NotNull] FusionParser.BinContext context) {
			return new BinaryExpr(this.Visit(context.left), new Token(TokenType.Operator, context.op, this.File), this.Visit(context.right), context: null);
		}

		public override Expression VisitLocalName([NotNull] FusionParser.LocalNameContext context) {
			Debug.Assert(this.currentMacro != null);
			Token name = new Token(TokenType.Identifier, context.Start, this.File);
			if(this.currentMacro.IsLabel(name)) {
				return new LabelReference(name, null);
			}
			if(this.currentMacro.IsParameter(name)) {
				return new Parameter(this.currentMacro, name);
			}
			this.Assembler.Error(Resource.UndefinedMacro(name.Value, name.Position));
			return new ValueExpression(VoidValue.Value);
		}

		public override Expression VisitLiteral([NotNull] FusionParser.LiteralContext context) {
			Token start = new Token((context.NumberLiteral() == null) ? TokenType.String : TokenType.Number, context.Start, this.File);
			Debug.Assert(start.IsString() || !string.IsNullOrWhiteSpace(start.Value));
			if(start.TokenType == TokenType.Number && !Token.IsValidNumber(start.Value!)) {
				this.Assembler.Error(Resource.BadNumberFormat(start.Value, start.Position));
				return new ValueExpression(VoidValue.Value);
			}
			return new Literal(start);
		}
	}
}
