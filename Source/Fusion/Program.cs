using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Fusion {
	class Program {
		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		internal static int Main(string[] args) {
			Console.Out.WriteLine(Resource.AppTitle(typeof(Program).Assembly.GetName().Version.ToString(3)));
			int returnCode = 1;
			try {
				if(args == null || args.Length != 2) {
					Console.Error.WriteLine(Resource.Usage);
				} else if(!File.Exists(args[0])) {
					Console.Error.WriteLine(Resource.FileNotFound(args[0]));
					Console.Error.WriteLine(Resource.Usage);
				} else {
					using(MemoryStream bin = new MemoryStream(16 * 1024)) {
						using(BinaryWriter writer = new BinaryWriter(bin)) {
							Assembler assembler = new Assembler(Console.Error, Console.Out, writer);
							Exception exception = Program.RunOnBigStack(() => assembler.Compile(args[0]));
							if(exception != null) {
								if(exception is FileNotFoundException fileNotFoundException) {
									//assembler.ErrorOutput.WriteLine(fileNotFoundException.Message);
								} else {
									assembler.ErrorOutput.WriteLine(exception.ToString());
								}
								returnCode = 1;
							} else {
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
				}
			} catch(FileNotFoundException fileNotFoundException) {
				Console.Error.WriteLine(fileNotFoundException.Message);
				returnCode = 1;
			} catch(Exception exception) {
				Console.Error.WriteLine(exception.ToString());
				returnCode = 1;
			}
			return returnCode;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		private static Exception RunOnBigStack(Action action) {
			Exception exception = null;
			void Run() {
				try {
					action();
				} catch(Exception ex) {
					exception = ex;
				}
			}
			Thread thread = new Thread(new ThreadStart(Run), 1024 * 1024 * 100);
			thread.Name = "compile";
			thread.Start();
			thread.Join();
			return exception;
		}
	}
}
