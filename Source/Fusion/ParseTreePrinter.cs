#if DEBUG
namespace Fusion {
	using System.Globalization;
	using System.Text;
	using Antlr4.Runtime.Misc;
	using Antlr4.Runtime.Tree;

	internal sealed class ParseTreePrinter : FusionParserBaseVisitor<int> {
		private readonly StringBuilder text = new StringBuilder();
		private int indent;

		public static string Text(FusionParser.FusionProgramContext context) {
			ParseTreePrinter printer = new ParseTreePrinter();
			printer.VisitFusionProgram(context);
			return printer.text.ToString();
		}

		private ParseTreePrinter() {
		}

		private void Write(string value) => this.text.Append(value);
		private void Write(string format, params string[] arguments) => this.text.AppendFormat(CultureInfo.InvariantCulture, format, arguments);
		private void WriteLine(string value) => text.AppendLine(value);

		private void WriteLine(string format, params string[] arguments) {
			this.Write(format, arguments);
			this.text.AppendLine();
		}
		private void WriteLine() {
			this.text.AppendLine();
		}

		private void Indent(int count) {
			if(0 < count) {
				this.text.Append('\t', count);
			}
		}

		public override int VisitFusionProgram([NotNull] FusionParser.FusionProgramContext context) {
			this.text.Clear();
			this.indent = 0;

			return base.VisitFusionProgram(context);
		}

		public override int VisitBinaryDeclaration([NotNull] FusionParser.BinaryDeclarationContext context) {
			this.WriteLine("{0} {1}", context.Start.Text, context.outputBitsPerNumber().Start.Text);

			return base.VisitBinaryDeclaration(context);
		}

		public override int VisitInclude([NotNull] FusionParser.IncludeContext context) {
			this.WriteLine("{0} {1}", context.Start.Text, context.filePath().Start.Text);
			return base.VisitInclude(context);
		}

		public override int VisitMacro([NotNull] FusionParser.MacroContext context) {
			this.indent = 0;
			var atomic = context.Atomic();
			if(atomic != null) {
				this.Write("{0} ", atomic.GetText());
			}

			this.Write("{0} {1} ", context.Macro().GetText(), context.macroName().GetText());
			var args = context.parameterList();
			if(args != null) {
				bool comma = false;
				foreach(var arg in args.parameterDeclaration()) {
					if(comma) {
						this.Write(", ");
					} else {
						comma = true;
					}
					this.Write(arg.GetText());
				}
				this.Write(" ");
			}
			base.VisitMacro(context);
			this.WriteLine();
			return 0;
		}

		public override int VisitExprList([NotNull] FusionParser.ExprListContext context) {
			this.WriteLine("{");
			int old = this.indent++;
			foreach(IParseTree child in context.children) {
				this.Indent((child is FusionParser.LabelContext) ? this.indent - 1 : this.indent);
				this.Visit(child);
				this.WriteLine();
			}
			this.indent = old;
			this.Indent(this.indent);
			this.Write("}");
			return 0;
		}

		public override int VisitLabel([NotNull] FusionParser.LabelContext context) {
			this.Write("{0}:", context.Start.Text);
			return 0;
		}

		public override int VisitLiteral([NotNull] FusionParser.LiteralContext context) {
			this.Write("{0}", context.Start.Text);
			return 0;
		}

		public override int VisitLocalName([NotNull] FusionParser.LocalNameContext context) {
			this.Write("{0}", context.Start.Text);
			return 0;
		}

		public override int VisitBin([NotNull] FusionParser.BinContext context) {
			this.Write("(");
			this.Visit(context.left);
			this.Write(" {0} ", context.op.Text);
			this.Visit(context.right);
			this.Write(")");
			return 0;
		}

		public override int VisitUnary([NotNull] FusionParser.UnaryContext context) {
			this.Write("{0}(", context.Start.Text);
			this.Visit(context.expr());
			this.Write(")");
			return 0;
		}

		public override int VisitCall([NotNull] FusionParser.CallContext context) {
			this.Write("({0}", context.macroName().GetText());
			var args = context.arguments();
			if(args != null) {
				bool comma = false;
				foreach(var arg in args.argument()) {
					if(comma) {
						this.Write(",");
					} else {
						comma = true;
					}
					this.Write(" ");
					this.Visit(arg);
				}
			}
			this.Write(")");
			return 0;
		}

		public override int VisitIndexExprList([NotNull] FusionParser.IndexExprListContext context) {
			this.Write("[");
			base.VisitIndexExprList(context);
			this.Write("]");
			return 0;
		}

		public override int VisitIf([NotNull] FusionParser.IfContext context) {
			this.Write("{0}(", context.If().GetText());
			this.Visit(context.cond);
			this.Write(") ");
			this.Visit(context.trueBranch);
			var falseBranch = context.falseBranch;
			if(falseBranch != null) {
				this.Write(" {0} ", context.Else().GetText());
				this.Visit(falseBranch);
			}
			return 0;
		}

		public override int VisitPrint([NotNull] FusionParser.PrintContext context) {
			this.Write("{0} (", context.Start.Text);
			this.Visit(context.expr());
			this.Write(")");
			return 0;
		}
	}
}
#endif
