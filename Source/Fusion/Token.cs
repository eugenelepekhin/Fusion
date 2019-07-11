using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Fusion {
	
	public class Token : IEquatable<Token> {
		public Position Position { get; private set; }
		public TokenType TokenType { get; private set; }
		public string Value { get; private set; }

		public Token(Position position, TokenType tokenType, string value) {
			Debug.Assert(!string.IsNullOrEmpty(value) || tokenType == Fusion.TokenType.Eos);
			this.Position = position;
			this.TokenType = tokenType;
			this.Value = value;
		}

		public bool TextEqual(string data) { return StringComparer.Ordinal.Compare(this.Value, data) == 0; }
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
		public bool IsEos() { return this.TokenType == Fusion.TokenType.Eos; }
		public bool IsError() { return this.TokenType == Fusion.TokenType.Error; }

		public bool Equals(Token other) {
			return this.TokenType == other.TokenType && this.TextEqual(other.Value);
		}

		public override bool Equals(object obj) => base.Equals(obj);
		public override int GetHashCode() => base.GetHashCode();

		public int Number {
			get {
				Debug.Assert(this.IsNumber());
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
				return Token.ParseDecimal(this.Value);
			}
		}

		private static int ParseDecimal(string text) {
			return int.Parse(text, CultureInfo.InvariantCulture);
		}

		private static int ParseHex(string text) {
			int value = 0;
			for(int i = 2; i < text.Length; i++) {
				char c = text[i];
				if('0' <= c && c <= '9') {
					value = value * 16 + c - '0';
				} else if('a' <= c && c <= 'f') {
					value = value * 16 + c - 'a' + 10;
				} else {
					Debug.Assert('A' <= c && c <= 'F');
					value = value * 16 + c - 'A' + 10;
				}
			}
			return value;
		}

		private static int ParseBinary(string text) {
			int value = 0;
			for(int i = 2; i < text.Length; i++) {
				value = value * 2 + text[i] - '0';
			}
			return value;
		}

		#if DEBUG
			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture, "{0} \"{1}\" @ line {2}", this.TokenType, this.Value, this.Position.Line);
			}
		#endif
	}
}
