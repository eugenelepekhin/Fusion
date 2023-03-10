using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Fusion {
	public abstract class Value {

		public int Address { get; set; }
		public virtual bool IsComplete { get { return true; } }

		public abstract int Size(Context context);
		public abstract Value WriteValue(Assembler assembler);

		public NumberValue? ToNumber() {
			if(this.ToSingular() is NumberValue number) {
				return number;
			}
			return null;
		}

		public StringValue? ToStringValue() {
			Value value = this.ToSingular();
			if(value is StringValue str) {
				return str;
			}
			if(value is NumberValue num) {
				return new StringValue(num.Value.ToString("X", CultureInfo.InvariantCulture));
			}
			return null;
		}

		public virtual Value ToSingular() {
			return this;
		}

		public ListValue ToList() {
			ListValue? list = this as ListValue;
			if(list != null) {
				return list;
			}
			list = new ListValue();
			list.List.Add(this);
			return list;
		}
	}

	public class VoidValue : Value {
		public static readonly VoidValue Value = new VoidValue();
		private VoidValue() {}
		public override int Size(Context context) { return 0; }
		public override Value WriteValue(Assembler assembler) {
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return "VoidValue";
			}
		#endif
	}

	public class NumberValue : Value {
		public Context Context { get; }
		public Token Token { get; }
		public int Value { get; }
		public NumberValue(Context context, Token token, int value) {
			this.Context = context;
			this.Token = token;
			this.Value = value;
		}
		public NumberValue(Context context, Token token, bool value) : this(context, token, value ? 1 : 0) {
		}

		public override int Size(Context context) { return 1; }
		public override Value WriteValue(Assembler assembler) {
			int value = this.Value;
			string? error = assembler.BinaryFormatter.Write(value);
			if(error != null) {
				Debug.Assert(this.Context != null && this.Token != null);
				assembler.Error(Resource.MessageOnStack(error, this.Context.PositionStack(this.Token)));
			}
			return this;
		}

		public NumberValue ToBoolean() => new NumberValue(this.Context, this.Token, this.Value != 0);

		#if DEBUG
			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture, "NumberValue(0x{0:X})", this.Value);
			}
		#endif
	}

	//public class LabelValue : NumberValue {
	//	public static readonly LabelValue Label = new LabelValue();
	//	private LabelValue() : base(0) {}
	//	public override Value WriteValue(Assembler assembler) {
	//		assembler.Error(Resource.IncorrectValue("Label", assembler.BinaryFormatter.Position));
	//		assembler.BinaryFormatter.Write(0xFF);
	//		return this;
	//	}
	//	#if DEBUG
	//		public override string ToString() {
	//			return "LabelValue unresolved";
	//		}
	//	#endif
	//}

	public class StringValue : Value {
		public string Value { get; }
		public StringValue(string value) { this.Value = value; }
		public override int Size(Context context) { return this.Value.Length + 1; }
		public override Value WriteValue(Assembler assembler) {
			// TODO: can it be null here?
			if(this.Value != null) {
				string? error = null;
				string? message;
				foreach(char c in this.Value) {
					message = assembler.BinaryFormatter.Write(c);
					error = error ?? message;
				}
				message = assembler.BinaryFormatter.Write((char)0);
				error = error ?? message;
				Debug.Assert(error == null);
				//if(error != null) {
				//	assembler.Error(error);
				//}
			}
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture, "StringValue({0})", this.Value);
			}
		#endif
	}

	public class ListValue : Value {
		public IList<Value> List { get; } = new List<Value>();

		private int size = -1;

		public override int Size(Context context) {
			if(this.size < 0) {
				this.size = this.List.Sum(v => v.Size(context));
			#if DEBUG
			} else {
				Debug.Assert(this.size == this.List.Sum(v => v.Size(context)));
			#endif
			}
			return this.size;
		}
		public override Value WriteValue(Assembler assembler) {
			foreach(Value value in this.List) {
				value.WriteValue(assembler);
			}
			return this;
		}

		public override Value ToSingular() {
			List<Value> list = new List<Value>(2);
			bool extract(IList<Value> values) {
				foreach(Value value in values) {
					if(value is ListValue listValue) {
						if(extract(listValue.List)) {
							return true;
						}
					} else if(!(value is VoidValue)) {
						list.Add(value);
						if(1 < list.Count) {
							return true;
						}
					}
				}
				return false;
			}

			extract(this.List);

			if(1 == list.Count) {
				return list[0];
			}

			return this;
		}

		public void ResolveLabels(Context context) {
			for(int i = 0; i < this.List.Count; i++) {
				if(this.List[i] is ListValue listValue) {
					listValue.ResolveLabels(context);
				} else if(this.List[i] is Expression expression) {
					this.List[i] = expression.Evaluate(context, 0);
					Debug.Assert(this.List[i].IsComplete, "Expression expected to be evaluated");
					this.List[i].Address = expression.Address;
				}
			}
		}

		#if DEBUG
			public override string ToString() {
				StringBuilder text = new StringBuilder();
				text.Append("ListValue(");
				bool first = true;
				foreach(Value value in this.List) {
					if(!first) {
						text.Append(' ');
					} else {
						first = false;
					}
					text.Append(value.ToString());
				}
				text.Append(')');
				return text.ToString();
			}
		#endif
	}
}
