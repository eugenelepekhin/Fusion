using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Fusion {
	public static class Program {
		//Debug: "E:\Projects\Fusion\Test.asm" "E:\Projects\Fusion\Test.bin"
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		public static int Main(string[] args) {
			int returnCode = 1;
			try {
				if(args == null || args.Length != 2) {
					Console.Error.WriteLine(Resource.Usage);
				} else {
					using(MemoryStream bin = new MemoryStream(16 * 1024)) {
						using(BinaryWriter writer = new BinaryWriter(bin)) {
							Assembler assembler = new Assembler(Console.Error, Console.Out, writer);
							Program.RunOnBigStack(() => assembler.Compile(args[0]));
							//assembler.Compile(args[0]);
							if(assembler.ErrorCount <= 0) {
								returnCode = 0;
								writer.Flush();
								using(FileStream file = File.Create(args[1])) {
									bin.WriteTo(file);
								}
							}
							if(assembler.ErrorCount == 0) {
								assembler.StandardOutput.WriteLine(Resource.SummarySuccess);
							} else {
								assembler.StandardOutput.WriteLine(Resource.SummaryErrors(assembler.ErrorCount));
							}
						}
					}
				}
			} catch(Exception exception) {
				Console.Error.WriteLine(exception.ToString());
				returnCode = 1;
			}
			return returnCode;
		}

		private static void RunOnBigStack(Action action) {
			Thread thread = new Thread(new ThreadStart(action), 1024 * 1024 * 100);
			thread.Name = "compile";
			thread.Start();
			thread.Join();
		}
	}
}
