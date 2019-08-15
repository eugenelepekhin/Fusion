﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion {
	public abstract class Value {

		public int Address { get; set; }
		public virtual bool IsComplete { get { return true; } }

		public abstract int Size(Context context);
		public abstract Value WriteValue(Assembler assembler);

		public NumberValue ToNumber() {
			if(this.ToSingular() is NumberValue number) {
				return number;
			}
			return null;
		}

		public StringValue ToStringValue() {
			if(this.ToSingular() is StringValue str) {
				return str;
			}
			if(this.ToSingular() is NumberValue num) {
				return new StringValue(num.Value.ToString("X", CultureInfo.InvariantCulture));
			}
			return null;
		}

		public Value ToSingular() {
			if(this is ListValue list && list != null && 0 < list.List.Count) {
				List<Value> values = list.List.Where(value => !(value is VoidValue)).ToList();
				if(values.Count == 1) {
					return values[0];
				}
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
		public static readonly NumberValue False = new NumberValue(0);
		public static readonly NumberValue True = new NumberValue(1);
		public NumberValue(int value) { this.Value = value; }
		public int Value { get; private set; }
		public override int Size(Context context) { return 1; }
		public override Value WriteValue(Assembler assembler) {
			int value = this.Value;
			string error = assembler.BinaryFormatter.Write(value);
			if(error != null) {
				assembler.Error(error);
			}
			return this;
		}
		#if DEBUG
			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture, "NumberValue(0x{0:X})", this.Value);
			}
		#endif
	}

	public class LabelValue : NumberValue {
		public static readonly LabelValue Label = new LabelValue();
		private LabelValue() : base(0) {}
		public override Value WriteValue(Assembler assembler) {
			assembler.Error(Resource.IncorrectValue("Label", assembler.BinaryFormatter.Position));
			assembler.BinaryFormatter.Write(0xFF);
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
		public override int Size(Context context) { return this.Value.Length + 1; }
		public override Value WriteValue(Assembler assembler) {
			if(this.Value != null) {
				string error = null;
				string message;
				foreach(char c in this.Value) {
					message = assembler.BinaryFormatter.Write(c);
					error = error ?? message;
				}
				message = assembler.BinaryFormatter.Write((char)0);
				error = error ?? message;
				if(error != null) {
					assembler.Error(error);
				}
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
		public List<Value> List { get; } = new List<Value>();
		public override int Size(Context context) { return this.List.Sum(v => v.Size(context)); }
		public override Value WriteValue(Assembler assembler) {
			foreach(Value value in this.List) {
				value.WriteValue(assembler);
			}
			return this;
		}
		public void ResolveLabels() {
			for(int i = 0; i < this.List.Count; i++) {
				if(this.List[i] is ListValue listValue) {
					listValue.ResolveLabels();
				} else {
					if(this.List[i] is Expression expression) {
						this.List[i] = expression.Evaluate(null, 0);
						Debug.Assert(this.List[i].IsComplete, "Expression expected to be evaluated");
						this.List[i].Address = expression.Address;
					}
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
