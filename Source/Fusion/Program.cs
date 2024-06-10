using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using CommandLineParser;

namespace Fusion {
	internal sealed class Program {
		private struct Arguments {
			private string? inputFile;
			private string? outputFile;
			private List<string>? searchPath;
			private OutputWriter.OutputType outputType;
			private int textRowWidth;
			private string seperator;

			public bool Parse(string[] args) {
				string? path = null;
				List<string> arguments = new List<string>();
				OutputWriter.OutputType outputType = OutputWriter.OutputType.Binary;
				int textRowWidth = 16;
				string? seperator = null;
				bool showHelp = false;
				CommandLine commandLine = new CommandLine()
					.AddString("include", "i", "<path>", "Semicolon separated list of folders to search for includes", false, p=> path = p)
					.AddEnum<OutputWriter.OutputType>("output", "o", "<output>", "Defines type of output file (default is binary file)", false,
						[
							new(OutputWriter.OutputType.Binary, "Binary", "b", " - Binary file."),
							new(OutputWriter.OutputType.TextBinary , "TextBinary", "tb", " - A text file filled with numbers written in binary code."),
							new(OutputWriter.OutputType.TextDecimal , "TextDecimal", "td", " - A text file filled with decimal numbers."),
							new(OutputWriter.OutputType.TextHexadecimal , "TextHexadecimal", "tx", " - A text file filled with hexadecimal numbers."),
						],
						"Where possible values of <output> are:",
						ot => outputType = ot
					)
					.AddInt("itemsInRow", "w", "<rowItems>", "Number of values in text file row (default is 16)", false, 1, 4096, w => textRowWidth = w)
					.AddString("separator", "s", "<separator>", "Separator of numbers in one row (default is space ' ')", false, s => seperator = s)
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
					Console.Error.WriteLine(Resource.FolderNotFound(arguments[1]));
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
				Token token = new Token(TokenType.String, new Position("", 0, 0), seperator ?? " ");
				seperator = token.Value;
				Debug.Assert(seperator != null);
				
				this.inputFile = arguments[0];
				this.outputFile = arguments[1];
				this.searchPath = list;

				this.outputType = outputType;
				this.textRowWidth = textRowWidth;
				this.seperator = seperator;
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

			public List<string> SearchPath() {
				Debug.Assert(this.searchPath != null);
				return this.searchPath;
			}

			public OutputWriter.OutputType OutputType() => this.outputType;
			public int OutputRowWidth() => this.textRowWidth;
			public string OutputSeparator() => this.seperator;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:Do not catch general exception types")]
		internal static int Main(string[] args) {
			Console.Out.WriteLine(Resource.AppTitle(typeof(Program).Assembly.GetName().Version!.ToString(3)));
			int returnCode = 1;
			#if DEBUG
				Stopwatch stopwatch = Stopwatch.StartNew();
			#endif
			try {
				Arguments arguments = new Arguments();
				if(arguments.Parse(args)) {
					using OutputWriter writer = new OutputWriter(arguments.OutputType(), arguments.OutputRowWidth(), arguments.OutputSeparator());
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
							writer.SaveFile(arguments.OutputFile());
						}
						if(assembler.ErrorCount == 0) {
							assembler.StandardOutput.WriteLine(Resource.SummarySuccess);
							returnCode = 0;
						} else {
							assembler.StandardOutput.WriteLine(Resource.SummaryErrors(assembler.ErrorCount));
						}
					}
				}
			} catch(FileNotFoundException fileNotFoundException) {
				Console.Error.WriteLine(fileNotFoundException.Message);
			} catch(Exception exception) {
				Console.Error.WriteLine(exception.ToString());
			}
			#if DEBUG
				Debug.WriteLine($"Finished in {stopwatch.Elapsed}");
			#endif
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
