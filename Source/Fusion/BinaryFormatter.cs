using System;
using System.IO;

namespace Fusion {
	public abstract class BinaryFormatter {
		public abstract int CellSize { get; }
		protected BinaryWriter Writer { get; private set; }
		public long	Position { get { return this.Writer.BaseStream.Position; } }

		protected BinaryFormatter(BinaryWriter writer) {
			this.Writer = writer;
		}

		public abstract string Write(char value);
		public abstract string Write(int value);
	}

	public class BinaryFormatter8 : BinaryFormatter {
		public override int CellSize { get { return 8; } }

		public BinaryFormatter8(BinaryWriter writer) : base(writer) {
		}

		public override string Write(char value) {
			this.Writer.Write((byte)(value & 0xFF));
			return null;
		}

		public override string Write(int value) {
			string error = null;
			if(0xFF < Math.Abs(value)) {
				error = Resource.IncorrectNumber(value, this.Position, 0xFF);
				value = 0xFF;
			}
			this.Writer.Write((byte)value);
			return error;
		}
	}

	public class BinaryFormatter16 : BinaryFormatter {
		public override int CellSize { get { return 16; } }

		public BinaryFormatter16(BinaryWriter writer) : base(writer) {
		}

		public override string Write(char value) {
			this.Writer.Write((short)(value & 0xFF));
			return null;
		}

		public override string Write(int value) {
			string error = null;
			if(0xFFFF < Math.Abs(value)) {
				error = Resource.IncorrectNumber(value, this.Position, 0xFFFF);
				value = 0xFFFF;
			}
			this.Writer.Write((short)value);
			return error;
		}
	}

	public class BinaryFormatter32 : BinaryFormatter {
		public override int CellSize { get { return 32; } }

		public BinaryFormatter32(BinaryWriter writer) : base(writer) {
		}

		public override string Write(char value) {
			this.Writer.Write((int)(value & 0xFF));
			return null;
		}

		public override string Write(int value) {
			this.Writer.Write(value);
			return null;
		}
	}
}
