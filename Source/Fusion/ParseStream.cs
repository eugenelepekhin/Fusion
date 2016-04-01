using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Fusion {
	public sealed class ParseStream : IDisposable {

		public Assembler Assembler { get; private set; }
		private Stack<TokenStream> include = new Stack<TokenStream>();
		private HashSet<string> parsing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private Token returned;

		private TokenStream TokenStream { get { return this.include.Peek(); } }

		public bool IsOpened { get { return this.returned != null || 0 < this.include.Count; } }

		public ParseStream(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.Open(Path.GetFullPath(file));
		}

		public void Dispose() {
			while(0 < this.include.Count) {
				TokenStream stream = this.include.Pop();
				stream.Dispose();
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public Token First() {
			if(this.returned != null) {
				return this.Returned();
			}
			Debug.Assert(this.IsOpened);
			for(;;) {
				Token token = this.TokenStream.Next();
				if(token.IsEos()) {
					this.Close();
					if(this.IsOpened) {
						continue;
					}
				} else if(token.IsIdentifier() && token.Value == "include") {
					Token file = this.TokenStream.NextPathString();
					if(file == null || !file.IsString() || string.IsNullOrWhiteSpace(file.Value)) {
						continue;
					}
					string path = this.Find(file.Value.Trim());
					if(path == null) {
						this.Assembler.FatalError(Resource.IncludeFileNotFound(file.Value, file.Position.ToString()));
						continue;
					}
					Debug.Assert(File.Exists(path)); // this actually can fail by valid reason if file was just deleted.
					this.Open(path);
					continue;
				}
				return token;
			}
		}

		public Token Next() {
			if(this.returned != null) {
				return this.Returned();
			}
			Debug.Assert(this.IsOpened);
			Token token = this.TokenStream.Next();
			if(token.IsEos()) {
				this.Close();
				this.Assembler.FatalError(Resource.UnexpectedEOF(token.Position.ToString()));
			}
			return token;
		}

		public void Return(Token token) {
			Debug.Assert(this.returned == null, "Only one returned token allowed");
			this.returned = token;
		}

		private Token Returned() {
			Debug.Assert(this.returned != null);
			Token token = this.returned;
			this.returned = null;
			return token;
		}

		private void Open(string file) {
			Debug.Assert(StringComparer.OrdinalIgnoreCase.Compare(file, Path.GetFullPath(file)) == 0, "Full path expected");
			if(this.parsing.Contains(file)) {
				//TODO: file can be included only once. Revisit this in the future.
				this.Assembler.Error(Resource.MultipleInclusions(file));
			} else {
				this.include.Push(new TokenStream(this.Assembler, file));
				this.parsing.Add(file);
			}
		}

		private void Close() {
			TokenStream stream = this.include.Pop();
			stream.Dispose();
		}

		private IEnumerable<string> SearchPath {
			get {
				Debug.Assert(this.IsOpened);
				yield return Path.GetDirectoryName(this.TokenStream.Path);
				foreach(string path in this.Assembler.SearchPath) {
					yield return path;
				}
			}
		}

		private string Find(string file) {
			if(File.Exists(file)) {
				return Path.GetFullPath(file);
			}
			foreach(string path in this.SearchPath) {
				string fullPath = Path.Combine(path, file);
				if(File.Exists(fullPath)) {
					return Path.GetFullPath(fullPath);
				}
			}
			return null;
		}
	}
}
