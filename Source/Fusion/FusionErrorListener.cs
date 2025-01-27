using System.IO;
using Antlr4.Runtime;

namespace Fusion {
	internal sealed class FusionErrorListener : BaseErrorListener, IAntlrErrorListener<int> {
		public Assembler Assembler { get; }

		public string File { get; }

		public FusionErrorListener(Assembler assembler, string file) {
			this.Assembler = assembler;
			this.File = file;
		}

		public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
			this.Assembler.Error(Resource.SyntaxError(this.File, line, charPositionInLine + 1, msg));
		}

		public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
			this.Assembler.Error(Resource.SyntaxError(this.File, line, charPositionInLine + 1, msg));
		}
	}
}
