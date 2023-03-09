using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Fusion {
	internal sealed class FusionErrorListner : BaseErrorListener, IAntlrErrorListener<int> {
		public Assembler Assembler { get; }

		public string File { get; }

		public FusionErrorListner(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.File = file;
		}

		public override void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
			this.Assembler.Error(Resource.SyntaxError(this.File, line, charPositionInLine + 1, msg));
		}

		public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
			this.Assembler.Error(Resource.SyntaxError(this.File, line, charPositionInLine + 1, msg));
		}
	}
}
