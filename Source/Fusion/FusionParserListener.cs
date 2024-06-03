using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fusion {
	public class FusionParserListener : FusionParserBaseListener {
		private int mode;
		private int nameCount;

		public Assembler Assembler { get; }
		public string SourceFile { get; }

		public MacroDefinition? CurrentMacro { get; private set; }

		public FusionParserListener(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.SourceFile = file;
		}

		public override void EnterMacro([NotNull] FusionParser.MacroContext context) {
			Debug.Assert(this.mode == 0);
			Debug.Assert(this.nameCount == 0);
			this.mode = 1;
		}

		public override void ExitMacroName([NotNull] FusionParser.MacroNameContext context) {
			this.nameCount++;
			if(this.nameCount == 1) { // First occurred name is current macro name. All other are just calls to other macros.
				Debug.Assert(this.mode == 1);
				Token name = new Token(TokenType.Identifier, context.Start, this.SourceFile);
				Debug.Assert(name.Value != null);
				// Find macro with the same location as original.
				this.CurrentMacro = this.Assembler.Macro.Select(name.Value).First(m => m.Name.SameToken(name));
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
