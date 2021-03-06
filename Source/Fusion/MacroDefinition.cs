using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion {
	public class MacroDefinition {
		public Token Name { get; private set; }
		[SuppressMessage("Microsoft.Design", "CA1002:Do not expose generic lists")]
		public List<Token> Parameter { get; private set; }
		public IList<Token> Label { get; private set; }
		public ExpressionList Body { get; set; }
		public bool Atomic { get; set; }

		public MacroDefinition(Token name) {
			Debug.Assert(name != null);
			this.Name = name;
			this.Parameter = new List<Token>();
			this.Label = new List<Token>();
		}

		public bool IsParameter(Token name) {
			return this.Parameter.Any(t => t.Equals(name));
		}

		public bool IsLabel(Token name) {
			return this.Label.Any(t => t.Equals(name));
		}

		public void Write(TextWriter writer) {
			writer.WriteLine();
			if(this.Atomic) {
				writer.Write("atomic ");
			}
			writer.Write("macro ");
			writer.Write(this.Name.Value);
			for(int i = 0; i < this.Parameter.Count; i++) {
				if(0 < i) {
					writer.Write(",");
				}
				writer.Write(" ");
				writer.Write(this.Parameter[i].Value);
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

		public override string ToString() {
			using(StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {
				this.Write(writer);
				return writer.ToString();
			}
		}
	}
}
