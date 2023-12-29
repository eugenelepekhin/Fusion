using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Fusion {
	public class Context {
		public Assembler Assembler { get; }
		public MacroDefinition Macro { get; }
		public Context? Parent { get; }
		public CallExpr? Call { get; }

		private List<Value>? argument;
		private Dictionary<string, int>? label;

		public Context(Assembler assembler, MacroDefinition macro) {
			this.Assembler = assembler;
			this.Macro = macro;
		}

		public Context(Context parent, MacroDefinition macro, CallExpr call) :this(parent.Assembler, macro) {
			this.Parent = parent;
			this.Call = call;
		}

		public void AddArgument(Value value) {
			if(this.argument == null) {
				this.argument = new List<Value>();
			}
			this.argument.Add(value);
			Debug.Assert(this.argument.Count <= this.Macro.Parameters.Count, "tool many actual arguments");
		}

		public Value Argument(Token name) {
			Debug.Assert(this.argument != null, "the macro was called without parameters");
			for(int i = 0; i < this.Macro.Parameters.Count; i++) {
				if(this.Macro.Parameters[i].Value == name.Value) {
					return this.argument[i];
				}
			}
			return VoidValue.Value;
		}

		public void DefineLabel(Token labelName, int value) {
			if(this.label == null) {
				this.label = new Dictionary<string, int>();
			}
			Debug.Assert(this.Macro.IsLabel(labelName), "label name expected");
			Debug.Assert(!string.IsNullOrWhiteSpace(labelName.Value));
			if(this.label.TryGetValue(labelName.Value, out int currentValue)) {
				if(currentValue != value) {
					this.Assembler.Error(Resource.LabelRedefined(labelName.Value, this.Macro.Name.Value, labelName.Position));
				}
			} else {
				this.label.Add(labelName.Value, value);
			}
		}

		public bool IsLabelDefined(Token labelName) {
			return this.label != null && !string.IsNullOrWhiteSpace(labelName.Value) && this.label.ContainsKey(labelName.Value);
		}

		public int LabelValue(Token labelName) {
			Debug.Assert(this.IsLabelDefined(labelName));
			Debug.Assert(!string.IsNullOrWhiteSpace(labelName.Value));
			Debug.Assert(this.label != null);
			return this.label[labelName.Value];
		}

		public string PositionStack(Token token) {
			StringBuilder text = new StringBuilder();
			Context? context = this;
			Token? name = token;
			while(context != null && name != null) {
				text.AppendLine(Resource.UserErrorPosition(context.Macro.Name.Value, name.Position));
				name = context.Call?.Name;
				context = context.Parent;
			}
			return text.ToString();
		}

		#if DEBUG
			public override string ToString() {
				string append(string text, string value) => string.IsNullOrEmpty(text) ? value : text + ", " + value;
				return string.Format(CultureInfo.InvariantCulture,
					"{{macro:{0} ({1})}}",
					this.Macro.Name.Value,
					(this.argument == null) ? "" : this.argument.Aggregate("", (x, y) => append(x, y.ToString()!))
				);
			}
		#endif
	}
}
