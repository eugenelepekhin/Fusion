using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Fusion {
	public class FirstPass : FirstPassParserBaseVisitor<int> {
		private MacroDefinition? currentMacro;

		private bool binDefined;

		public Assembler Assembler { get; }

		public string SourceFile { get; }

		public FirstPass(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.SourceFile = file;
		}

		public override int VisitBinaryDecalration([NotNull] FirstPassParser.BinaryDecalrationContext context) {
			IToken outputBitsPerNumber = context.outputBitsPerNumber().Start;
			Token width = new Token(TokenType.Number, outputBitsPerNumber, this.SourceFile);
			if(binDefined) {
				this.Assembler.Error(Resource.BinaryTypeRedefined(width.Position));
				return 1;
			}
			binDefined = true;
			int binWidht = width.Number;
			if(binWidht == 8 || binWidht == 16 || binWidht == 32) {
				this.Assembler.SetBinaryType(binWidht);
			} else {
				this.Assembler.Error(Resource.BinaryTypeExpected(width.Value, width.Position));
			}
			return 0;
		}

		public override int VisitInclude([NotNull] FirstPassParser.IncludeContext context) {
			Token token = new Token(TokenType.String, context.filePath().Start, this.SourceFile);
			Debug.Assert(!string.IsNullOrWhiteSpace(token.Value));
			string filePath = token.Value;
			if(!File.Exists(filePath) && !Path.IsPathRooted(filePath)) {
				foreach(string item in this.Assembler.SearchPath) {
					string file = Path.Combine(item, filePath);
					if(File.Exists(file)) {
						filePath = Path.GetFullPath(file);
						break;
					}
				}
			}
			if(File.Exists(filePath)) {
				this.Assembler.FirstPassParse(filePath);
				return 0;
			}
			this.Assembler.FatalError(Resource.IncludeFileNotFound(filePath, token.Position));
			return 1;
		}

		public override int VisitMacro([NotNull] FirstPassParser.MacroContext context) {
			Token name = new Token(TokenType.Identifier, context.macroName().Start, this.SourceFile);
			Debug.Assert(!string.IsNullOrWhiteSpace(name.Value));
			bool atomic = context.Atomic() != null;
			FirstPassParser.ParameterListContext parameterListContext = context.parameterList();
			List<Token>? parameters = null;
			if(parameterListContext != null) {
				parameters = context.parameterList().parameterName().Select(pnc => new Token(TokenType.Identifier, pnc.Start, this.SourceFile)).ToList();
				HashSet<string> paramNames = new HashSet<string>();
				foreach(Token param in parameters) {
					Debug.Assert(!string.IsNullOrWhiteSpace(param.Value));
					if(!paramNames.Add(param.Value)) {
						this.Assembler.Error(Resource.ParameterRedefinition(name.Value, param.Value, param.Position));
					}
					if(param.IsIdentifier(Assembler.PrintName, Assembler.ErrorName, Assembler.IfName, Assembler.ElseName)) {
						this.Assembler.Error(Resource.ParameterKeyword(param.Value, param.Position));
					}
				}
			}
			FirstPassParser.ExprListContext body = context.macroBody().exprList();
			this.currentMacro = new MacroDefinition(name, atomic, parameters ?? new List<Token>());
			if(this.Assembler.Macro.ContainsKey(name.Value)) {
				this.Assembler.Error(Resource.MacroNameRedefinition(name.Value, name.Position));
			} else {
				this.Assembler.Macro.Add(name.Value, this.currentMacro);
			}
			return base.Visit(body);
		}

		public override int VisitLabel([NotNull] FirstPassParser.LabelContext context) {
			Debug.Assert(this.currentMacro != null);
			Token name = new Token(TokenType.Identifier, context.labelName().Start, this.SourceFile);
			if(this.currentMacro.Labels.Any((Token t) => t.Value == name.Value)) {
				this.Assembler.Error(Resource.LabelRedefined(name.Value, this.currentMacro.Name.Value, name.Position));
				return 1;
			}
			if(this.currentMacro.Parameters.Any((Token arg) => arg.Value == name.Value)) {
				this.Assembler.Error(Resource.LabelHidesParameter(name.Value, this.currentMacro.Name.Value, name.Position));
				return 1;
			}
			this.currentMacro.Labels.Add(name);
			return 0;
		}
	}
}
