using System.Diagnostics;

namespace Fusion {
	public class Predicates {
		public Assembler Assembler { get; }

		public FusionParserListener Listener { get; }

		public Predicates(Assembler assembler, FusionParserListener listener) {
			this.Assembler = assembler;
			this.Listener = listener;
		}

		private int ParamCount(string name) {
			MacroDefinition? parsingMacro = this.Listener.CurrentMacro;
			Debug.Assert(parsingMacro != null);
			if(!parsingMacro.IsParameter(name) && !parsingMacro.IsLabel(name)) {
				return this.Assembler.Macro.MinParameters(name);
			}
			return -1;
		}

		public bool IsMacro0(string name) {
			return 0 == this.ParamCount(name);
		}

		public bool IsMacroN(string name) {
			return 0 < this.ParamCount(name);
		}

		public bool IsNotMacro(string name) {
			return this.ParamCount(name) < 0;
		}
	}
}
