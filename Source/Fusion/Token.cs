using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Fusion {
	
	public class Token : IEquatable<Token> {
		public TokenType TokenType { get; }
		public Position Position { get; }
		public string? Value { get; }

		public Token(TokenType tokenType, IToken node, string file) {
			this.TokenType = tokenType;
			this.Position = new Position(file, node.Line, node.Column + 1);
			this.Value = node.Text;
			if(this.TokenType == TokenType.String) {
				StringBuilder text = new StringBuilder(this.Value);
				if(0 < text.Length && text[0] == '"') {
					text.Remove(0, 1);
				}
				if(0 < text.Length && text[text.Length - 1] == '"') {
					text.Remove(text.Length - 1, 1);
				}
				if(0 < text.Length) {
					text.Replace("\\\"", "\"");
					text.Replace("\\\\", "\\");
					text.Replace("\\0", "\0");
					text.Replace("\\a", "\a");
					text.Replace("\\b", "\b");
					text.Replace("\\f", "\f");
					text.Replace("\\n", "\n");
					text.Replace("\\r", "\r");
					text.Replace("\\t", "\t");
					text.Replace("\\v", "\v");
				}
				this.Value = text.ToString();
			}
		}
		public Token(TokenType tokenType, ITerminalNode node, string file) : this(tokenType, node.Symbol, file) {
		}

		public bool TextEqual(string? data) { return StringComparer.Ordinal.Compare(this.Value, data) == 0; }
		public bool TextEqual(params string[] oneOf) { return oneOf.Contains(this.Value, StringComparer.Ordinal); }

		public bool IsNumber() { return this.TokenType == Fusion.TokenType.Number; }
		public bool IsString() { return this.TokenType == Fusion.TokenType.String; }
		public bool IsIdentifier() { return this.TokenType == Fusion.TokenType.Identifier; }
		public bool IsIdentifier(string data) { return this.IsIdentifier() && this.TextEqual(data); }
		public bool IsIdentifier(params string[] oneOf) { return this.IsIdentifier() && this.TextEqual(oneOf); }
		public bool IsSeparator() { return this.TokenType == Fusion.TokenType.Separator; }
		public bool IsSeparator(string data) { return this.IsSeparator() && this.TextEqual(data); }
		public bool IsOperator() { return this.TokenType == Fusion.TokenType.Operator; }
		public bool IsOperator(string data) { return this.IsOperator() && this.TextEqual(data); }
		public bool IsOperator(params string[] oneOf) { return this.IsOperator() && this.TextEqual(oneOf); }
		public bool IsComparison() { return this.TokenType == Fusion.TokenType.Comparison; }
		public bool IsComparison(string data) { return this.IsComparison() && this.TextEqual(data); }
		public bool IsComparison(params string[] oneOf) { return this.IsComparison() && this.TextEqual(oneOf); }
		public bool IsError() { return this.TokenType == Fusion.TokenType.Error; }

		public bool Equals(Token? other) {
			return other != null && this.TokenType == other.TokenType && this.TextEqual(other.Value);
		}

		public override bool Equals(object? obj) => base.Equals(obj);
		public override int GetHashCode() => base.GetHashCode();

		public int Number {
			get {
				Debug.Assert(this.IsNumber());
				Debug.Assert(this.Value != null);
				if(1 < this.Value.Length && this.Value[0] == '0') {
					switch(this.Value[1]) {
					case 'x':
					case 'X':
						return Token.ParseHex(this.Value);
					case 'b':
					case 'B':
						return Token.ParseBinary(this.Value);
					}
				}
				if(Value[0] == '0') {
					return (int)Token.ParseOcta(Value);
				}
				return (int)Token.ParseDecimal(this.Value);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsNumberDecorator(char c) => c == '_' || c == '\'';

		private static long ParseDecimal(string text) {
			long value = 0L;
			foreach(char c in text) {
				if(!Token.IsNumberDecorator(c)) {
					Debug.Assert('0' <= c && c <= '9');
					value = value * 10 + (c - '0');
				}
			}
			return value;
		}

		private static int ParseHex(string text) {
			int value = 0;
			for(int i = 2; i < text.Length; i++) {
				char c = text[i];
				if(!Token.IsNumberDecorator(c)) {
					if('0' <= c && c <= '9') {
						value = value * 16 + c - '0';
					} else if('a' <= c && c <= 'f') {
						value = value * 16 + c - 'a' + 10;
					} else {
						Debug.Assert('A' <= c && c <= 'F');
						value = value * 16 + c - 'A' + 10;
					}
				}
			}
			return value;
		}

		private static int ParseBinary(string text) {
			int value = 0;
			for(int i = 2; i < text.Length; i++) {
				char c = text[i];
				if(!Token.IsNumberDecorator(c)) {
					value = value * 2 + text[i] - '0';
				}
			}
			return value;
		}

		private static long ParseOcta(string text) {
			long value = 0L;
			for(int i = 1; i < text.Length; i++) {
				char c = text[i];
				if(!Token.IsNumberDecorator(c)) {
					value = value * 8 + (c - '0');
				}
			}
			return value;
		}

		public static bool IsValidNumber(string value) {
			bool between(int min, int max) {
				int len = value.Where((char c) => !IsNumberDecorator(c)).Count();
				return min <= len && len <= max;
			}

			if(!string.IsNullOrEmpty(value)) {
				if(value.StartsWith("0x", ignoreCase: true, CultureInfo.InvariantCulture)) {
					return between(3, 10);
				}
				if(value.StartsWith("0b", ignoreCase: true, CultureInfo.InvariantCulture)) {
					return between(3, 34);
				}
				if(value.StartsWith("0", ignoreCase: true, CultureInfo.InvariantCulture)) {
					return between(1, 12) && Token.ParseOcta(value) <= int.MaxValue;
				}
				return between(1, 10) && Token.ParseDecimal(value) <= int.MaxValue;
			}
			return false;
		}

		#if DEBUG
			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture, "{0} \"{1}\" @ line {2}", this.TokenType, this.Value, this.Position.Line);
			}
		#endif
	}
}
