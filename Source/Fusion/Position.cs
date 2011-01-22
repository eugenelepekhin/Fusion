using System;

namespace Fusion {
	public struct Position {
		private string file;
		public string File { get { return this.file; } }
		private int line;
		public int Line { get { return this.line; } }

		public Position(string file, int line) {
			this.file = file;
			this.line = line;
		}

		public override string ToString() {
			return Resource.PositionText(this.file, this.line);
		}
	}
}
