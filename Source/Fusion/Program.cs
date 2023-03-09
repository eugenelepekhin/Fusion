using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLineParser;

namespace Fusion {
	internal class Program {
		private struct Arguments {
			private string? inputFile;
			private string? outputFile;
			private List<string>? searchPath;

			public bool Parse(string[] args) {
				string? path = null;
				List<string> arguments = new List<string>();
				bool showHelp = false;
				CommandLine commandLine = new CommandLine()
					.AddString("include", "i", "<path>", "Semicolon separated list of folders to search for includes", false, p=> path = p)
					.AddFlag("help", "?", "Show this help", false, h => showHelp = h)
				;
				string? errors = commandLine.Parse(args, a => arguments.AddRange(a));
				if(showHelp || !string.IsNullOrEmpty(errors) || arguments.Count != 2) {
					Program.Usage(errors, commandLine.Help());
					return false;
				}
				if(!File.Exists(arguments[0])) {
					Console.Error.WriteLine(Resource.FileNotFound(arguments[0]));
					Program.Usage(null, commandLine.Help());
					return false;
				}
				string? outDir = Path.GetDirectoryName(arguments[1]);
				if(!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) {
					Console.Error.WriteLine(Resource.FileNotFound(arguments[1]));
					Program.Usage(null, commandLine.Help());
					return false;
				}
				List<string> list = new List<string>();
				if(!string.IsNullOrEmpty(path)) {
					list.AddRange(path.Split(';').Where(d => !string.IsNullOrWhiteSpace(d)));
					bool notFound = false;
					foreach(string p in list) {
						if(!Directory.Exists(p)) {
							Console.Error.WriteLine(Resource.IncludeFolderNotFound(p));
							notFound = true;
						}
					}
					if(notFound) {
						return false;
					}
				}
				this.inputFile = arguments[0];
				this.outputFile = arguments[1];
				this.searchPath = list;
				return true;
			}

			public string InputFile() {
				Debug.Assert(this.inputFile != null);
				return this.inputFile;
			}

			public string OutputFile() {
				Debug.Assert(this.outputFile != null);
				return this.outputFile;
			}

			public IEnumerable<string> SearchPath() {
				Debug.Assert(this.searchPath != null);
				return this.searchPath;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		internal static int Main(string[] args) {
			Console.Out.WriteLine(Resource.AppTitle(typeof(Program).Assembly.GetName().Version!.ToString(3)));
			int returnCode = 1;
			try {
				Arguments arguments = new Arguments();
				if(arguments.Parse(args)) {
					using(MemoryStream bin = new MemoryStream(16 * 1024)) {
						using(BinaryWriter writer = new BinaryWriter(bin)) {
							Assembler assembler = new Assembler(Console.Error, Console.Out, writer, arguments.SearchPath());
							Exception? exception = Program.RunOnBigStack(() => assembler.Compile(arguments.InputFile()));
							if(exception != null) {
								if(exception is FileNotFoundException fileNotFoundException) {
									assembler.ErrorOutput.WriteLine(fileNotFoundException.Message);
								} else {
									assembler.ErrorOutput.WriteLine(exception.ToString());
								}
							} else {
								if(assembler.ErrorCount <= 0) {
									writer.Flush();
									using(FileStream file = File.Create(arguments.OutputFile())) {
										bin.WriteTo(file);
									}
								}
								if(assembler.ErrorCount == 0) {
									assembler.StandardOutput.WriteLine(Resource.SummarySuccess);
									returnCode = 0;
								} else {
									assembler.StandardOutput.WriteLine(Resource.SummaryErrors(assembler.ErrorCount));
								}
							}
						}
					}
				}
			} catch(FileNotFoundException fileNotFoundException) {
				Console.Error.WriteLine(fileNotFoundException.Message);
			} catch(Exception exception) {
				Console.Error.WriteLine(exception.ToString());
			}
			return returnCode;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		private static Exception? RunOnBigStack(Action action) {
			Exception? exception = null;
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

		private static void Usage(string? errors, string argHelp) {
			if(!string.IsNullOrEmpty(errors)) {
				Console.Error.WriteLine(errors);
			}
			Console.Out.WriteLine(Resource.Usage);
			Console.Out.WriteLine(argHelp);
		}
	}
}
