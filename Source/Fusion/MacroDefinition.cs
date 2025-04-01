using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Fusion {
	public interface IWritable {
		void WriteListing(TextWriter writer);
	}

	public class MacroDefinition {
		public Token Name { get; }
		public IList<Token> Parameters { get; }
		public string CallPattern { get; }
		public IList<Token> Labels { get; }
		public ExpressionList Body { get; set; }
		public bool Atomic { get; }

		public MacroDefinition(Token name, bool atomic, IList<Token> parameters, string callPattern) {
			Debug.Assert(name != null);
			this.Name = name;
			this.Parameters = parameters;
			this.Labels = new List<Token>();
			this.Body = new ExpressionList();
			this.Atomic = atomic;
			this.CallPattern = callPattern;
		}

		public bool IsParameter(Token name) => this.Parameters.Any(t => t.Equals(name));
		public bool IsParameter(string name) => this.Parameters.Any(p => p.Value == name);

		public bool IsLabel(Token name) => this.Labels.Any(t => t.Equals(name));
		public bool IsLabel(string name) => this.Labels.Any(l => l.Value == name);

		public void AddLabel(Token name) {
			Debug.Assert(!this.IsLabel(name));
			this.Labels.Add(name);
		}

		public void Write(TextWriter writer) {
			writer.WriteLine();
			if(this.Atomic) {
				writer.Write("atomic ");
			}
			writer.Write("macro ");
			writer.Write(this.Name.Value);
			if(0 < this.Parameters.Count) {
				writer.Write(' ');
				this.WriteWithPattern(this.Parameters, writer);
			}
			if(this.Body != null) {
				writer.WriteLine(" {");
				foreach(Expression expr in this.Body.List) {
					writer.Write("\t");
					expr.WriteText(writer, 1);
					writer.WriteLine();
				}
				writer.WriteLine("}");
			} else {
				writer.WriteLine(" !!! No Body !!!");
			}
		}

		public void WriteWithPattern<T>(IEnumerable<T> list, TextWriter writer) where T:IWritable {
			string pattern = this.CallPattern;
			int index = 0;
			void decorate() {
				while(index < pattern.Length && pattern[index] != '$') {
					writer.Write(pattern[index]);
					if(pattern[index] == ',') {
						writer.Write(" ");
					}
					index++;
				}
				index++;
			}
			foreach(T item in list) {
				decorate();
				item.WriteListing(writer);
			}
			decorate();
		}

		#if DEBUG
			public override string ToString() {
				using(StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {
					this.Write(writer);
					return writer.ToString();
				}
			}
		#endif
	}
}
