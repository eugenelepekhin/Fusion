using System.Linq;

namespace Fusion {
	public class Predicates {
		//public Assembler Assembler { get; }

		//public SecondPassListener Listener { get; }

		//public Predicates(Assembler assembler, SecondPassListener listener) {
		//	Assembler = assembler;
		//	Listener = listener;
		//}

		//private int ParamCount(string name) {
		//	string name2 = name;
		//	MacroDefinition parsingMacro = Listener.CurrentMacro;
		//	if(!parsingMacro.Parameters.Any((Token token) => token.Value == name2) && !parsingMacro.Labels.Any((Token token) => token.Value == name2) && Assembler.Macro.TryGetValue(name2, out var macro)) {
		//		return macro.Parameters.Count;
		//	}
		//	return -1;
		//}

		//public bool IsMacro0(string name) {
		//	return ParamCount(name) == 0;
		//}

		//public bool IsMacroN(string name) {
		//	return 0 < ParamCount(name);
		//}

		//public bool IsNotMacro(string name) {
		//	return ParamCount(name) < 0;
		//}
	}
}
