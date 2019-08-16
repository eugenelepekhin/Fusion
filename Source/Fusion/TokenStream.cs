using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fusion {
	public sealed class TokenStream : IDisposable {
		public Assembler Assembler { get; set; }
		public string Path { get; private set; }
		private readonly StreamReader reader;
		private int line = 1;

		public TokenStream(Assembler assembler, string file) {
			if(!File.Exists(file)) {
				string error = Resource.FileNotFound(file);
				assembler.FatalError(error);
				throw new FileNotFoundException(error);
			}
			this.Assembler = assembler;
			this.Path = file;
			this.reader = File.OpenText(file);
		}

		public void Dispose() {
			this.reader.Dispose();
		}

		public Token Next() {
			while(this.Assembler.CanContinue) {
				int c = this.Skip();
				if(c == -1) {
					break; // return new Token(new Position(this.Path, this.line), TokenType.Eos, null);
				}
				if(TokenStream.IsDecimalDigit(c)) {
					return this.Number(c);
				}
				if(TokenStream.IsLetter(c)) {
					return this.Identifier(c);
				}
				if(c == '"') {
					return this.String();
				}
				if(TokenStream.IsSeparator(c)) {
					return this.Separator(c);
				}
				this.Assembler.Error(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, new Position(this.Path, this.line).ToString()));
				this.SkipLine();
			}
			return new Token(new Position(this.Path, this.line), TokenType.Eos, null);
		}

		public Token NextPathString() {
			int c = this.Skip();
			Position p = new Position(this.Path, this.line);
			if(c != '"') {
				this.Assembler.FatalError(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, p.ToString()));
				this.Assembler.Error(Resource.IncludeFileMissing(p.ToString()));
				return null;
			}
			HashSet<char> invalid = new HashSet<char>(System.IO.Path.GetInvalidPathChars()) {
				'\n', // just make sure these chars are included
				'\r'
			};
			StringBuilder text = new StringBuilder(128);
			this.reader.Read();
			for(;;) {
				c = this.reader.Read();
				switch(c) {
				case '"':
					return new Token(p, TokenType.String, text.ToString());
				case -1:
					this.Assembler.FatalError(Resource.UnexpectedEOF(p.ToString()));
					this.Assembler.Error(Resource.IncludeFileMissing(p.ToString()));
					return null;
				default:
					if(invalid.Contains((char)c)) {
						this.Assembler.FatalError(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, p.ToString()));
						this.Assembler.Error(Resource.IncludeFileMissing(p.ToString()));
						return null;
					}
					text.Append((char)c);
					break;
				}
			}
		}

		private static char MakePrintable(char c) {
			if(char.IsSymbol(c)) {
				return c;
			}
			return ' ';
		}

		private void SkipLine() {
			int c;
			do {
				this.reader.Read();
				c = this.reader.Peek();
			} while(c != -1 && c != '\n');
		}

		private static bool IsWhiteSpace(int c) { return c != -1 && char.IsWhiteSpace((char)c); }
		private static bool IsBinaryDidit(int c) { return c == '0' || c == '1'; }
		private static bool IsDecimalDigit(int c) { return '0' <= c && c <= '9'; }
		private static bool IsHexadecimalDigit(int c) { return TokenStream.IsDecimalDigit(c) || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F'); }
		private static bool IsLetter(int c) { return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || c == '_'; }
		private static bool IsSeparator(int c) { return 0 <= "(){},:<>!=+-*/%&|^~".IndexOf((char)c); }

		private int Skip() {
			int c = this.reader.Peek();
			for(;;) {
				while(TokenStream.IsWhiteSpace(c)) {
					if(c == '\n') {
						this.line++;
					}
					this.reader.Read();
					c = this.reader.Peek();
				}
				if(c == ';') {
					do {
						this.reader.Read();
						c = this.reader.Peek();
						if(c == -1) {
							return c;
						}
					} while(c != '\n');
				} else {
					return c;
				}
			}
		}
	
		private Token Number(int c) {
			StringBuilder text = new StringBuilder(34);
			text.Append((char)c);
			Func<int, bool> valid;
			int minLenght = 1;
			int maxLenght = 10;
			this.reader.Read();
			if((char)c == '0') {
				switch(this.reader.Peek()) {
				case 'x':
				case 'X':
					text.Append((char)this.reader.Read());
					valid = TokenStream.IsHexadecimalDigit;
					minLenght = 3;
					break;
				case 'b':
				case 'B':
					text.Append((char)this.reader.Read());
					valid = TokenStream.IsBinaryDidit;
					minLenght = 3;
					maxLenght = 34;
					break;
				default:
					valid = TokenStream.IsDecimalDigit;
					break;
				}
			} else {
				valid = TokenStream.IsDecimalDigit;
			}
			c = this.reader.Peek();
			Position p = new Position(this.Path, this.line);
			while(valid(c)) {
				text.Append((char)this.reader.Read());
				c = this.reader.Peek();
				if(maxLenght < text.Length) {
					this.Assembler.Error(Resource.BadNumberFormat(text.ToString(), p.ToString()));
					this.SkipLine();
					return new Token(p, TokenType.Error, text.ToString());
				}
			}
			if(TokenStream.IsLetter(c) || text.Length < minLenght) {
				this.Assembler.Error(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, p.ToString()));
				this.SkipLine();
				return new Token(p, TokenType.Error, text.ToString());
			}
			//TODO: still possible to have bad number here. Validate the parsed number.
			return new Token(p, TokenType.Number, text.ToString());
		}

		private Token String() {
			StringBuilder text = new StringBuilder(64);
			Position p = new Position(this.Path, this.line);
			this.reader.Read();
			for(;;) {
				int c = this.reader.Read();
				switch(c) {
				case '"':
					return new Token(p, TokenType.String, text.ToString());
				case '\\':
					c = this.reader.Read();
					switch(c) {
					case '"':
					case '\\':
						text.Append((char)c);
						break;
					case '0':
						text.Append('\0');
						break;
					case 'a':
						text.Append('\a');
						break;
					case 'b':
						text.Append('\b');
						break;
					case 'f':
						text.Append('\f');
						break;
					case 'n':
						text.Append('\n');
						break;
					case 'r':
						text.Append('\r');
						break;
					case 't':
						text.Append('\t');
						break;
					case 'v':
						text.Append('\v');
						break;
					//case 'u':
					//case 'U':
					//case 'x':
					default:
						this.Assembler.Error(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, p.ToString()));
						this.SkipLine();
						return new Token(p, TokenType.Error, text.ToString());
					}
					break;
				case '\n':
					this.Assembler.Error(Resource.UnexpectedChar(TokenStream.MakePrintable((char)c), c, p.ToString()));
					this.SkipLine();
					return new Token(p, TokenType.Error, text.ToString());
				case -1:
					this.Assembler.Error(Resource.UnexpectedEOF(p.ToString()));
					return new Token(p, TokenType.Error, text.ToString());
				default:
					text.Append((char)c);
					break;
				}
			}
		}

		private Token Identifier(int c) {
			StringBuilder text = new StringBuilder(64);
			while(TokenStream.IsLetter(c) || TokenStream.IsDecimalDigit(c)) {
				text.Append((char)c);
				this.reader.Read();
				c = this.reader.Peek();
			}
			return new Token(new Position(this.Path, this.line), TokenType.Identifier, text.ToString());
		}

		private Token Separator(int c) {
			Position p = new Position(this.Path, this.line);
			int c1;
			switch(c) {
			case '<':
			case '>':
				this.reader.Read();
				c1 = this.reader.Peek();
				if(c1 == c) {
					this.reader.Read();
					return new Token(p, TokenType.Operator, new string(new char[] {(char)c, (char)c1}));
				} else if(c1 == '=') {
					this.reader.Read();
					return new Token(p, TokenType.Comparison, new string(new char[] {(char)c, (char)c1}));
				} else {
					return new Token(p, TokenType.Comparison, new string((char)c, 1));
				}
			case '!':
			case '=':
				this.reader.Read();
				c1 = this.reader.Peek();
				if(c1 == '=') {
					this.reader.Read();
					return new Token(p, TokenType.Comparison, new string(new char[] {(char)c, (char)c1}));
				} else {
					return new Token(p, TokenType.Operator, new string((char)c, 1));
				}
			case '(':
			case ')':
			case '{':
			case '}':
			case ',':
			case ':':
				this.reader.Read();
				return new Token(p, TokenType.Separator, new string((char)c, 1));
			case '+':
			case '-':
			case '*':
			case '/':
			case '%':
			case '^':
			case '~':
				this.reader.Read();
				return new Token(p, TokenType.Operator, new string((char)c, 1));
			case '&':
			case '|':
				this.reader.Read();
				c1 = this.reader.Peek();
				if(c1 == c) {
					this.reader.Read();
					return new Token(p, TokenType.Operator, new string(new char[] {(char)c, (char)c1}));
				} else {
					return new Token(p, TokenType.Operator, new string((char)c, 1));
				}
			default:
				throw new InvalidOperationException();
			}
		}
	}
}
