using System;
using System.Diagnostics.CodeAnalysis;

namespace Fusion {
	[SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
	public struct Position {
		public string File { get; }
		public int Line { get; }

		public Position(string file, int line) {
			this.File = file;
			this.Line = line;
		}

		public override string ToString() {
			return Resource.PositionText(this.File, this.Line);
		}
	}
}
