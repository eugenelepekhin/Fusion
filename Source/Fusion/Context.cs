using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Fusion {
	public class Context {
		public Assembler Assembler { get; set; }
		public MacroDefinition Macro { get; set; }
		private List<Value> argument;
		private Dictionary<string, int> label;

		public void AddArgument(Value value) {
			if(this.argument == null) {
				this.argument = new List<Value>();
			}
			this.argument.Add(value);
			Debug.Assert(this.argument.Count <= this.Macro.Parameter.Count, "tool many actual arguments");
		}

		public Value Argument(Token name) {
			Debug.Assert(this.argument != null, "the macro was called without parameters");
			int index = this.Macro.Parameter.FindIndex(t => t.Equals(name));
			Debug.Assert(0 <= index && index < this.Macro.Parameter.Count, "Unknown argument");
			return this.argument[index];
		}

		public void DefineLabel(Token labelName, int value) {
			if(this.label == null) {
				this.label = new Dictionary<string, int>();
			}
			Debug.Assert(this.Macro.IsLabel(labelName), "label name expected");
			if(this.label.ContainsKey(labelName.Value)) {
				if(this.label[labelName.Value] != value) {
					this.Assembler.Error(Resource.LabelRedefined(labelName.Value, this.Macro.Name.Value, labelName.Position.ToString()));
				}
			} else {
				this.label.Add(labelName.Value, value);
			}
		}

		public bool IsLabelDefined(Token labelName) {
			return this.label != null && this.label.ContainsKey(labelName.Value);
		}

		public int LabelValue(Token labelName) {
			Debug.Assert(this.IsLabelDefined(labelName));
			return this.label[labelName.Value];
		}

		#if DEBUG
			public override string ToString() {
				string append(string text, string value) => string.IsNullOrEmpty(text) ? value : text + ", " + value;
				return string.Format(CultureInfo.InvariantCulture,
					"{{macro:{0} ({1})}}",
					this.Macro.Name.Value,
					(this.argument == null) ? "" : this.argument.Aggregate("", (x, y) => append(x, y.ToString()))
				);
			}
		#endif
	}
}
