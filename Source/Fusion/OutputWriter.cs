using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Fusion {
	public sealed class OutputWriter : IDisposable {
		public enum OutputType {
			Binary,
			TextBinary,
			TextDecimal,
			TextHexadecimal,
		}

		private abstract class Writer : IDisposable {
			public abstract void Dispose();

			public abstract long Position { get; }

			public abstract void Write(byte value);
			public abstract void Write(short value);
			public abstract void Write(int value);
			public abstract void SaveFile(string file);
			public abstract byte[] ToArray();
		}

		private sealed class BinaryOutputWriter : Writer, IDisposable {
			private readonly MemoryStream stream;
			private readonly BinaryWriter writer;

			public BinaryOutputWriter() {
				this.stream = new MemoryStream(16 * 1024);
				this.writer = new BinaryWriter(this.stream);
			}

			public override void Dispose() {
				this.writer.Dispose();
				this.stream.Dispose();
			}

			public override long Position => this.stream.Position;

			public override void Write(byte value) {
				this.writer.Write(value);
			}

			public override void Write(short value) {
				this.writer.Write(value);
			}

			public override void Write(int value) {
				this.writer.Write(value);
			}

			public override void SaveFile(string file) {
				this.writer.Flush();
				this.stream.Flush();
				using(FileStream fileStream = File.Create(file)) {
					this.stream.WriteTo(fileStream);
				}
			}
			public override byte[] ToArray() => this.stream.ToArray();
		}

		private sealed class TextBinaryWriter : Writer, IDisposable {
			private readonly StringBuilder text = new StringBuilder();
			private readonly CompositeFormat byteFormat;
			private readonly CompositeFormat shortFormat;
			private readonly CompositeFormat intFormat;
			private readonly string separator;
			private readonly int rowWidth;
			private int item;

			public TextBinaryWriter(string byteFormat, string shortFormat, string intFormat, int rowWidth, string separator) {
				this.byteFormat = CompositeFormat.Parse(byteFormat);
				this.shortFormat = CompositeFormat.Parse(shortFormat);
				this.intFormat = CompositeFormat.Parse(intFormat);
				this.rowWidth = rowWidth;
				this.separator = separator;
			}

			public override void Dispose() {}

			public override long Position => this.text.Length;

			private void WriteSeparator() {
				if(this.rowWidth <= this.item) {
					if(!string.IsNullOrWhiteSpace(this.separator)) {
						this.text.Append(this.separator);
					}
					this.text.AppendLine();
					this.item = 0;
				}
				if(0 < this.item) {
					this.text.Append(this.separator);
				}
				this.item++;
			}

			public override void Write(byte value) {
				this.WriteSeparator();
				this.text.AppendFormat(CultureInfo.InvariantCulture, this.byteFormat, value);
			}

			public override void Write(short value) {
				this.WriteSeparator();
				this.text.AppendFormat(CultureInfo.InvariantCulture, this.shortFormat, value);
			}

			public override void Write(int value) {
				this.WriteSeparator();
				this.text.AppendFormat(CultureInfo.InvariantCulture, this.intFormat, value);
			}

			public override void SaveFile(string file) {
				File.WriteAllText(file, this.text.ToString());
			}
			public override byte[] ToArray() => Encoding.UTF8.GetBytes(this.text.ToString());
		}

		private readonly Writer writer;

		public OutputWriter(OutputType outputType, int rowWidth, string separator) {
			switch(outputType) {
			case OutputType.Binary:
				this.writer = new BinaryOutputWriter();
				break;
			case OutputType.TextBinary:
				this.writer = new TextBinaryWriter("{0:b8}", "{0:b16}", "{0:b32}", rowWidth, separator);
				break;
			case OutputType.TextDecimal:
				this.writer = new TextBinaryWriter("{0}", "{0}", "{0}", rowWidth, separator);
				break;
			case OutputType.TextHexadecimal:
				this.writer = new TextBinaryWriter("{0:X2}", "{0:X4}", "{0:X8}", rowWidth, separator);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(outputType));
			}
		}

		public void Dispose() {
			this.writer.Dispose();
		}

		public long Position => this.writer.Position;
		public void Write(byte value) => this.writer.Write(value);
		public void Write(short value) => this.writer.Write(value);
		public void Write(int value) => this.writer.Write(value);

		public void SaveFile(string file) => this.writer.SaveFile(file);
		public byte[] ToArray() => this.writer.ToArray();
	}
}
