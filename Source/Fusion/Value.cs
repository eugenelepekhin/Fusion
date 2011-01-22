using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion {
	public abstract class Value {
		public abstract int Size();
		public abstract Value WriteValue(Assembler assembler);

		public NumberValue ToNumber() {
			NumberValue number = this.ToSingular() as NumberValue;
			if(number != null) {
				return number;
			}
			return null;
		}

		public StringValue ToStringValue() {
			StringValue str = this.ToSingular() as StringValue;
			if(str != null) {
				return str;
			}
			return null;
		}

		public Value ToSingular() {
			ListValue list = this as ListValue;
			if(list != null && list.List.Count == 1) {
				return list.List[0];
			}
			return this;
		}

		public ListValue ToList() {
			ListValue list = this as ListValue;
			if(list != null) {
				return list;
			}
			list = new ListValue();
			list.List.Add(this);
			return list;
		}

		public bool IsComplete { get { return !(this is Expression); } }
	}

	public class VoidValue : Value {
		public static readonly VoidValue Value = new VoidValue();
		private VoidValue() {}
		public override int Size() { return 0; }
		public override Value WriteValue(Assembler assembler) {
			assembler.Error(Resource.IncorrectValue("Void", assembler.Writer.BaseStream.Position));
			assembler.Writer.Write((byte)0xFF);
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return "VoidValue";
			}
		#endif
	}

	public class NumberValue : Value {
		public static readonly NumberValue False = new NumberValue(0);
		public static readonly NumberValue True = new NumberValue(1);
		public NumberValue(int value) { this.Value = value; }
		public int Value { get; private set; }
		public override int Size() { return 1; }
		public override Value WriteValue(Assembler assembler) {
			int value = this.Value;
			if(0xFF < Math.Abs(value)) {
				assembler.Error(Resource.IncorrectNumber(this.Value, assembler.Writer.BaseStream.Position));
				value = 0xFF;
			}
			assembler.Writer.Write((byte)value);
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return string.Format("NumberValue(0x{0:X})", this.Value);
			}
		#endif
	}

	public class LabelValue : NumberValue {
		public static readonly LabelValue Label = new LabelValue();
		private LabelValue() : base(0) {}
		public override Value WriteValue(Assembler assembler) {
			assembler.Error(Resource.IncorrectValue("Label", assembler.Writer.BaseStream.Position));
			assembler.Writer.Write((byte)0xFF);
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return "LabelValue unresolved";
			}
		#endif
	}

	public class StringValue : Value {
		public StringValue(string value) { this.Value = value; }
		public string Value { get; private set; }
		public override int Size() { return this.Value.Length + 1; }
		public override Value WriteValue(Assembler assembler) {
			if(this.Value != null) {
				foreach(char c in this.Value) {
					assembler.Writer.Write((byte)(c & 0xFF));
				}
				assembler.Writer.Write((byte)0);
			}
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return string.Format("StringValue({0})", this.Value);
			}
		#endif
	}

	public class ListValue : Value {
		private List<Value> list = new List<Value>();
		public List<Value> List { get { return this.list; } }
		public override int Size() { return 0; }
		public override Value WriteValue(Assembler assembler) {
			foreach(Value value in this.list) {
				value.WriteValue(assembler);
			}
			return this;
		}
		public void ResolveLabels() {
			for(int i = 0; i < this.List.Count; i++) {
				ListValue listValue = this.List[i] as ListValue;
				if(listValue != null) {
					listValue.ResolveLabels();
				} else {
					Expression expression = this.List[i] as Expression;
					if(expression != null) {
						this.List[i] = expression.Evaluate();
						Debug.Assert(!(this.List[i] is Expression));
					}
				}
			}
		}

		#if DEBUG
			public override string ToString() {
				StringBuilder text = new StringBuilder();
				text.Append("ListValue(");
				bool first = true;
				foreach(Value value in this.list) {
					if(!first) {
						text.Append(" ");
					} else {
						first = false;
					}
					text.Append(value.ToString());
				}
				text.Append(")");
				return text.ToString();
			}
		#endif
	}
}
