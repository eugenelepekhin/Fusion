using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceWrapper.Generator {
	internal class Message {
		public static void Error(string code, params object[] args) {
			Console.Error.WriteLine(TextMessage.ResourceManager.GetString(code, TextMessage.Culture), args);
		}
		public static void Write(string code, params object[] args) {
			Console.Out.WriteLine(TextMessage.ResourceManager.GetString(code, TextMessage.Culture), args);
		}
	}
}
