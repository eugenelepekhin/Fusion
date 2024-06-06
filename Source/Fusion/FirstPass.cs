using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

		public override int VisitBinaryDeclaration([NotNull] FirstPassParser.BinaryDeclarationContext context) {
			IToken outputBitsPerNumber = context.outputBitsPerNumber().Start;
			Debug.Assert(outputBitsPerNumber != null);
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
			List<Token> parameters = new List<Token>();
			StringBuilder callPattern = new StringBuilder();
			if(parameterListContext != null) {
				Debug.Assert(parameterListContext.parameterDeclaration() != null);
				HashSet<string> paramNames = new HashSet<string>();
				bool comma = false;
				foreach(var parameterContext in parameterListContext.parameterDeclaration()) {
					if(comma) {
						callPattern.Append(',');
					}
					comma = true;
					FirstPassParser.ParameterNameContext parameterNameContext = parameterContext.parameterName();
					if(parameterNameContext != null) {
						Token parameterName = new Token(TokenType.Identifier, parameterNameContext.Start, this.SourceFile);
						if(!paramNames.Add(parameterName.Value!)) {
							this.Assembler.Error(Resource.ParameterRedefinition(name.Value, parameterName.Value, parameterName.Position));
						} else {
							parameters.Add(parameterName);
							callPattern.Append('$');
						}
					}
					FirstPassParser.IndexDeclarationContext[] indexContext = parameterContext.indexDeclaration();
					if(indexContext != null && 0 < indexContext.Length) {
						foreach(FirstPassParser.IndexDeclarationContext index in indexContext) {
							callPattern.Append('[');
							bool indexComma = false;
							foreach(FirstPassParser.IndexNameContext indexNameContext in index.indexName()) {
								Token indexName = new Token(TokenType.Identifier, indexNameContext.Start, this.SourceFile);
								if(!paramNames.Add(indexName.Value!)) {
									this.Assembler.Error(Resource.ParameterRedefinition(name.Value, indexName.Value, indexName.Position));
								} else {
									parameters.Add(indexName);
									if(indexComma) {
										callPattern.Append(',');
									}
									indexComma = true;
									callPattern.Append('$');
								}
							}
							callPattern.Append(']');
						}
					}
				}
			}
			FirstPassParser.ExprListContext body = context.macroBody().exprList();
			this.currentMacro = new MacroDefinition(name, atomic, parameters, callPattern.ToString());
			if(!this.Assembler.Macro.Add(this.currentMacro)) {
				this.Assembler.Error(Resource.MacroNameRedefinition(name.Value, name.Position));
			}
			return base.Visit(body);
		}

		public override int VisitLabel([NotNull] FirstPassParser.LabelContext context) {
			Debug.Assert(this.currentMacro != null);
			Token name = new Token(TokenType.Identifier, context.labelName().Start, this.SourceFile);
			if(this.currentMacro.IsLabel(name)) {
				this.Assembler.Error(Resource.LabelRedefined(name.Value, this.currentMacro.Name.Value, name.Position));
				return 1;
			}
			if(this.currentMacro.IsParameter(name)) {
				this.Assembler.Error(Resource.LabelHidesParameter(name.Value, this.currentMacro.Name.Value, name.Position));
				return 1;
			}
			this.currentMacro.AddLabel(name);
			return 0;
		}
	}
}
