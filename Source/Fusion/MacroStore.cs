using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fusion {
	public sealed class MacroStore {
		private sealed class Comparer : IComparer<MacroDefinition> {
			public int Compare(MacroDefinition? x, MacroDefinition? y) {
				Debug.Assert(x != null && y != null);
				Debug.Assert(x.Name.Value == y.Name.Value && x.Name.Value != null);
				return x.Parameters.Count - y.Parameters.Count;
			}
		}

		// Macros stored in the list ordered by number of parameters.
		private readonly Dictionary<string, List<MacroDefinition>> map = new Dictionary<string, List<MacroDefinition>>();
		private readonly Comparer comparer = new Comparer();

		public bool Add(MacroDefinition macro) {
			Debug.Assert(macro.Name.Value != null);
			List<MacroDefinition>? list;
			if(!this.map.TryGetValue(macro.Name.Value, out list)) {
				list = new List<MacroDefinition>();
				this.map.Add(macro.Name.Value, list);
			}
			int index = list.BinarySearch(macro, this.comparer);
			// It is not possible to overload macro without parameters. So, check it here.
			if(index < 0 && (list.Count == 0 || macro.Parameters.Count != 0 && list[0].Parameters.Count != 0)) {
				list.Insert(~index, macro);
				return true;
			}
			return false;
		}

		public IEnumerable<MacroDefinition> Select(string name) {
			if(this.map.TryGetValue(name, out List<MacroDefinition>? list)) {
				return list;
			}
			return Enumerable.Empty<MacroDefinition>();
		}

		public int MinParameters(string name) {
			if(this.map.TryGetValue(name, out List<MacroDefinition>? list)) {
				Debug.Assert(0 < list.Count);
				return list[0].Parameters.Count;
			}
			return -1;
		}

		public MacroDefinition? Find(string name, int parameterCount) => this.Select(name).FirstOrDefault(macro => macro.Parameters.Count == parameterCount);
	}
}
