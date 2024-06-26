﻿using System.Diagnostics.CodeAnalysis;

namespace Fusion {
	[SuppressMessage("Microsoft.Naming", "CA1720: Identifiers should not contain type names")]
	public enum TokenType {
		Number,
		String,
		Identifier,
		Operator,
		Path,
	}
}
