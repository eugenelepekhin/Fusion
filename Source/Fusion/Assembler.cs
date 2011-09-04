using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion {
	public class Assembler {

		private const string MainName = "main";
		private const string AtomicName = "atomic";
		private const string MacroName = "macro";
		private const string ErrorName = "error";
		private const string IfName = "if";
		private const string ElseName = "else";

		public TextWriter StdErr { get; private set; }
		public TextWriter StdOut { get; private set; }
		public BinaryWriter Writer { get; private set; }
		public int ErrorCount { get; private set; }
		private bool fatalError = false;
		public bool CanContinue { get { return !this.fatalError && this.ErrorCount < 10; } }
		public IEnumerable<string> SearchPath { get { yield break; } }
		public Dictionary<string, MacroDefinition> Macro { get; private set; }
		public int Address { get; private set; }

		public Assembler(TextWriter stdErr, TextWriter stdOut, BinaryWriter writer) {
			this.StdErr = stdErr;
			this.StdOut = stdOut;
			this.Writer = writer;
			this.ErrorCount = 0;
		}

		public int IncrementAddress(int delta) {
			this.Address += delta;
			return this.Address;
		}

		public int SetAddress(int address) {
			this.Address = address;
			return address;
		}

		public void Error(string message) {
			this.ErrorCount++;
			this.StdErr.WriteLine(message);
		}

		public void FatalError(string message) {
			this.fatalError = true;
			this.Error(message);
		}

		public void Parse(string file) {
			this.FirstPass(file);
			if(0 < this.ErrorCount) return;
			this.SecondPass(file);
			if(0 < this.ErrorCount) return;
			MacroDefinition main;
			if(!this.Macro.TryGetValue(Assembler.MainName, out main)) {
				this.Error(Resource.MainMissing);
				return;
			}
			if(0 < main.Parameter.Count) {
				this.Error(Resource.MainPararameters);
			}
		}

		public void Expand() {
			MacroDefinition main = this.Macro[Assembler.MainName];
			Debug.Assert(main.Parameter.Count == 0);
			Call call = new Call() {
				Name = new Token(new Position(string.Empty, 1), TokenType.Identifier, Assembler.MainName),
				Macro = main,
				Parameter = new List<Expression>(0)
			};
			this.Address = 0;
			Value value = call.Evaluate(new Context() { Assembler = this, Macro = main }, true);
			if(0 < this.ErrorCount) return;
			ListValue listValue = value as ListValue;
			if(listValue != null) {
				listValue.ResolveLabels();
			}
			value.WriteValue(this);
		}

		public void Compile(string file) {
			this.Parse(file);
			if(this.ErrorCount <= 0) {
				this.Expand();
			}
		}

		private void FirstPass(string file) {
			using(ParseStream stream = new ParseStream(this, file)) {
				Dictionary<string, MacroDefinition> macro = new Dictionary<string, MacroDefinition>();
				while(this.CanContinue) {
					Token token = stream.First();
					while(this.CanContinue && !token.IsEOS() && !token.IsIdentifier(Assembler.MacroName)) {
						this.Error(Resource.MacroExpected(token.Position.ToString()));
						token = stream.First();
					}
					if(!this.CanContinue || token.IsEOS()) break;
					Token name = stream.Next();
					if(!token.IsIdentifier()) {
						this.Error(Resource.MacroNameExpected(name.Value, name.Position.ToString()));
						continue;
					}
					if(macro.ContainsKey(name.Value)) {
						this.Error(Resource.MacroNameRedefinition(name.Value, name.Position.ToString()));
						continue;
					}
					MacroDefinition macroDefinition = new MacroDefinition(name);
					macro.Add(name.Value, macroDefinition);
					Token next = stream.Next();
					while(this.CanContinue && next.IsIdentifier()) {
						if(macroDefinition.Parameter.Any(other => other.Equals(next))) {
							this.Error(Resource.ParameterRedefinition(name.Value, next.Value, next.Position.ToString()));
						}
						if(next.IsIdentifier(Assembler.MacroName, Assembler.ErrorName, Assembler.IfName, Assembler.ElseName)) {
							this.Error(Resource.ParameterKeyword(next.Value, next.Position.ToString()));
						}
						macroDefinition.Parameter.Add(next);
						next = stream.Next();
						if(next.IsSeparator(",")) {
							next = stream.Next();
						} else {
							break;
						}
					}
					if(!next.IsSeparator("{")) {
						this.Error(Resource.BeginExpected(next.Value, next.Position.ToString()));
						continue;
					}
					int count = 1;
					Token prev = next;
					while(0 < count) {
						next = stream.Next();
						if(next.IsSeparator("}")) {
							count--;
						} else if(next.IsSeparator("{")) {
							count++;
						} else if(next.IsEOS()) {
							this.FatalError(Resource.UnexpectedEOF(next.Position.ToString()));
							break;
						} if(next.IsSeparator(":")) {
							if(prev.IsIdentifier()) {
								if(macroDefinition.Label.Any(other => other.Equals(prev))) {
									this.Error(Resource.LabelRedefined(prev.Value, macroDefinition.Name.Value, prev.Position.ToString()));
								} else {
									macroDefinition.Label.Add(prev);
								}
							} else {
								this.Error(Resource.IdentifierExpected(prev.Value, prev.Position.ToString()));
							}
						}
						prev = next;
					}
				}
				this.Macro = macro;
			}
		}

		private void SecondPass(string file) {
			using(ParseStream stream = new ParseStream(this, file)) {
				while(this.CanContinue) {
					Token token = stream.First();
					if(token.IsEOS()) break;
					if(!token.IsIdentifier(Assembler.MacroName)) {
						this.FatalError(Resource.MacroExpected(token.Position.ToString()));
						return;
					}
					Token name = stream.Next();
					if(!name.IsIdentifier() || !this.Macro.ContainsKey(name.Value)) {
						this.FatalError(Resource.FileChanged);
						return;
					}
					MacroDefinition macro = this.Macro[name.Value];
					Debug.Assert(macro.Name.Equals(name));
					for(int i = 0; i < macro.Parameter.Count; i++) {
						if(0 < i) {
							if(!stream.Next().IsSeparator(",")) {
								this.FatalError(Resource.FileChanged);
								return;
							}
						}
						Token param = stream.Next();
						if(!param.IsIdentifier(macro.Parameter[i].Value)) {
							this.FatalError(Resource.FileChanged);
							return;
						}
					}
					if(!stream.Next().IsSeparator("{")) {
						this.FatalError(Resource.FileChanged);
						return;
					}
					macro.Body = new ExpressionList() { List = new List<Expression>() };
					this.ParseExpressionList(macro, stream, macro.Body);
				}
				if(this.ErrorCount == 0 && this.Macro.Values.Any(m => m.Body == null)) {
					this.Error(Resource.InternalError);
				}
			}
		}

		private void ParseExpressionList(MacroDefinition macro, ParseStream stream, ExpressionList list) {
			Token token = stream.Next();
			while(this.CanContinue && !token.IsSeparator("}")) {
				Expression expr = this.ParseExpression(macro, stream, token);
				if(expr != null) {
					list.List.Add(expr);
				}
				token = stream.Next();
			}
		}

		private Expression ParseExpression(MacroDefinition macro, ParseStream stream, Token token) {
			if(token.IsIdentifier()) {
				Token colon = stream.Next();
				if(colon.IsSeparator(":")) {
					Debug.Assert(macro.Label.Any(other => other.Equals(token)), "unknown label");
					return new Label() { Name = token };
				} else {
					stream.Return(colon);
				}
			}
			return this.ParseOr(macro, stream, token);
		}

		private Expression ParseOr(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.ParseAnd(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				while(token.IsOperator("||")) {
					Expression right = this.ParseAnd(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression ParseAnd(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.ParseComparison(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				while(token.IsOperator("&&")) {
					Expression right = this.ParseComparison(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression ParseComparison(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.ParseAdd(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				if(token.IsComparison("<", "<=", "==", "!=", ">=", ">")) {
					Expression right = this.ParseAdd(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression ParseAdd(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.ParseMul(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				while(token.IsOperator("+", "-")) {
					Expression right = this.ParseMul(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression ParseMul(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.ParseBit(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				while(token.IsOperator("*", "/", "%")) {
					Expression right = this.ParseBit(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression ParseBit(MacroDefinition macro, ParseStream stream, Token token) {
			Expression expr = this.Primary(macro, stream, token);
			if(expr != null) {
				token = stream.Next();
				while(token.IsOperator("&", "|", "^", "<<", ">>")) {
					Expression right = this.Primary(macro, stream, stream.Next());
					if(right == null) return null;
					expr = new Binary() { Left = expr, Operation = token, Right = right };
					token = stream.Next();
				}
				stream.Return(token);
			}
			return expr;
		}

		private Expression Primary(MacroDefinition macro, ParseStream stream, Token token) {
			if(token.IsIdentifier()) {
				if(token.TextEqual(Assembler.IfName)) {
					return this.ParseIf(macro, stream, token);
				} else if(token.TextEqual(Assembler.ErrorName)) {
					return new Error() { Token = token, Text = this.ParseOr(macro, stream, stream.Next()) };
				} else if(macro.IsParameter(token)) {
					return this.ParseParameter(macro, stream, token);
				} else if(macro.IsLabel(token)) {
					return this.ParseLabel(macro, stream, token);
				} else {
					return this.ParseCall(macro, stream, token);
				}
			}
			if(token.IsSeparator("(")) {
				Expression expr = this.ParseOr(macro, stream, stream.Next());
				if(expr == null || !this.EnsureSeparator(stream.Next(), ")")) return null;
				return expr;
			}
			if(token.IsOperator("!", "-", "+", "~")) {
				Expression expr = this.Primary(macro, stream, stream.Next());
				if(expr == null) return null;
				return new Unary() { Operation = token, Operand = expr };
			}
			if(token.IsNumber() || token.IsString()) {
				return new Literal() { Value = token };
			}
			if(token.IsEOS()) {
				this.FatalError(Resource.UnexpectedEOF(token.Position.ToString()));
			} else {
				this.Error(Resource.ItemExpected("Identifier, literal, \"(\", or keyword", token.Value, token.Position.ToString()));
			}
			return null;
		}

		private Expression ParseIf(MacroDefinition macro, ParseStream stream, Token token) {
			Debug.Assert(token.IsIdentifier(Assembler.IfName));
			if(!this.EnsureSeparator(stream.Next(), "(")) return null;
			Expression condition = this.ParseOr(macro, stream, stream.Next());
			if(condition == null) return null;
			if(!this.EnsureSeparator(stream.Next(), ")")) return null;
			if(!this.EnsureSeparator(stream.Next(), "{")) return null;
			ExpressionList thenList = new ExpressionList() { List = new List<Expression>() };
			this.ParseExpressionList(macro, stream, thenList);
			if(!this.CanContinue) return null;
			ExpressionList elseList = null;
			Token elseToken = stream.Next();
			if(elseToken.IsIdentifier(Assembler.ElseName)) {
				if(!this.EnsureSeparator(stream.Next(), "{")) return null;
				elseList = new ExpressionList() { List = new List<Expression>() };
				this.ParseExpressionList(macro, stream, elseList);
				if(!this.CanContinue) return null;
			} else {
				stream.Return(elseToken);
			}
			return new If() { IfToken = token, Condition = condition, Then = thenList, Else = elseList };
		}

		private Expression ParseParameter(MacroDefinition macro, ParseStream stream, Token token) {
			Debug.Assert(token.IsIdentifier() && macro.IsParameter(token), "Token should be the name of the parameter");
			return new Parameter() { Macro = macro, ParameterName = token };
		}

		private Expression ParseLabel(MacroDefinition macro, ParseStream stream, Token token) {
			Debug.Assert(token.IsIdentifier() && macro.IsLabel(token), "Token should be label");
			return new LabelReference() { Name = token };
		}

		private Expression ParseCall(MacroDefinition macro, ParseStream stream, Token token) {
			Debug.Assert(token.IsIdentifier(), "Token should be the name of the macro to call");
			MacroDefinition call;
			if(!this.Macro.TryGetValue(token.Value, out call)) {
				this.Error(Resource.UndefinedMacro(token.Value, token.Position.ToString()));
				return null;
			}
			List<Expression> parameter = new List<Expression>(call.Parameter.Count);
			bool success = true;
			for(int i = 0; i < call.Parameter.Count && this.CanContinue; i++) {
				if(0 < i) {
					this.EnsureSeparator(stream.Next(), ",");
				}
				Expression arg = this.ParseOr(macro, stream, stream.Next());
				parameter.Add(arg);
				if(arg == null) {
					success = false;
				}
			}
			if(success && this.CanContinue) {
				return new Call() { Name = token, Macro = call, Parameter = parameter };
			}
			return null;
		}

		private bool EnsureSeparator(Token token, string text) {
			if(!token.IsSeparator(text)) {
				this.Error(Resource.ItemExpected(text, token.Value, token.Position.ToString()));
				return false;
			}
			return true;
		}
	}
}
