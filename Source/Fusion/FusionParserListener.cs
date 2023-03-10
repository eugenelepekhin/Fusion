using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fusion {
	public class FusionParserListener : FusionParserBaseListener {
		private int mode;
		private int nameCount;

		public Assembler Assembler { get; }

		public MacroDefinition? CurrentMacro { get; private set; }

		public FusionParserListener(Assembler assembler) {
			this.Assembler = assembler;
		}

		public override void EnterMacro([NotNull] FusionParser.MacroContext context) {
			Debug.Assert(this.mode == 0);
			Debug.Assert(this.nameCount == 0);
			this.mode = 1;
		}

		public override void ExitMacroName([NotNull] FusionParser.MacroNameContext context) {
			this.nameCount++;
			if(this.nameCount == 1) {
				Debug.Assert(this.mode == 1);
				string text = context.Start.Text;
				Debug.Assert(this.Assembler.Macro.ContainsKey(text));
				this.CurrentMacro = this.Assembler.Macro[text];
				Debug.Assert(this.CurrentMacro != null);
				this.mode = 2;
			}
		}

		public override void ExitMacro([NotNull] FusionParser.MacroContext context) {
			Debug.Assert(this.mode == 2);
			Debug.Assert(this.CurrentMacro != null && this.CurrentMacro.Name.Value == context.macroName().Start.Text);
			this.CurrentMacro = null;
			this.mode = 0;
			this.nameCount = 0;
		}
	}
}
