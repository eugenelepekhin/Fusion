using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fusion {
	public abstract class Expression : Value {
		public override bool IsComplete { get { return false; } }

		public abstract void WriteText(TextWriter writer, int indent);

		protected static void Indent(TextWriter writer, int count) {
			for(int i = 0; i < count; i++) {
				writer.Write('\t');
			}
		}

		public override string ToString() {
			using(StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {
				this.WriteText(writer, 0);
				return writer.ToString();
			}
		}

		public abstract Value Evaluate(Context context, int address);

		public override int Size() {
			return 1;
		}

		public override Value WriteValue(Assembler assembler) {
			throw new InvalidOperationException();
		}
	}

	public class Label : Expression {
		public Token Name { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
			writer.WriteLine(":");
		}

		public override Value Evaluate(Context context, int address) {
			context.DefineLabel(this.Name, address);
			return VoidValue.Value;
		}
	}

	public class LabelReference : Expression {
		public Token Name { get; set; }
		private Context Context { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
		}

		public override Value Evaluate(Context context, int address) {
			context = this.Context ?? context;
			Debug.Assert(context != null);
			if(context.IsLabelDefined(this.Name)) {
				return new NumberValue(context.LabelValue(this.Name));
			} else {
				return new LabelReference() { Name = this.Name, Context = context };
			}
		}
	}

	public class ValueExpression : Expression {
		public Value Value { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			throw new InvalidOperationException();
		}

		public override Value Evaluate(Context context, int address) {
			return this.Value;
		}
	}

	public class Literal : Expression {
		public Token Value { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			if(this.Value.IsString()) {
				writer.Write("\"");
				foreach(char c in this.Value.Value) {
					switch(c) {
					case '"':  writer.Write("\\\""); break;
					case '\\': writer.Write("\\\\"); break;
					case '\0': writer.Write("\\0"); break;
					case '\a': writer.Write("\\a"); break;
					case '\b': writer.Write("\\b"); break;
					case '\f': writer.Write("\\f"); break;
					case '\n': writer.Write("\\n"); break;
					case '\r': writer.Write("\\r"); break;
					case '\t': writer.Write("\\t"); break;
					case '\v': writer.Write("\\v"); break;
					default:   writer.Write(c); break;
					}
				}
				writer.Write("\"");
			} else {
				writer.Write(this.Value.Value);
			}
		}

		public override Value Evaluate(Context context, int address) {
			if(this.Value.IsNumber()) {
				return new NumberValue(this.Value.Number);
			} else {
				return new StringValue(this.Value.Value);
			}
		}
	}

	public class Parameter : Expression {
		public MacroDefinition Macro { get; set; }
		public Token ParameterName { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.ParameterName.Value);
		}

		public override Value Evaluate(Context context, int address) {
			return context.Argument(this.ParameterName);
		}
	}

	public class Print : Expression {
		public Token Token { get; set; }
		public Expression Text { get; set; }

		private string message;

		public override void WriteText(TextWriter writer, int indent) {
			Expression.Indent(writer, indent);
			writer.Write(this.Token.Value);
			writer.Write(" ");
			this.Text.WriteText(writer, 0);
			writer.WriteLine();
			if(this.message != null) {
				writer.WriteLine(this.message);
			}
		}

		public override Value Evaluate(Context context, int address) {
			Value text = this.Text.Evaluate(context, 0);
			if(text.IsComplete) {
				this.message = null;
				StringValue stringValue = text.ToStringValue();
				if(stringValue != null) {
					this.message = stringValue.Value;
				} else {
					context.Assembler.Error(Resource.StringValueExpected(this.Token.Position.ToString()));
				}
				if(this.message != null) {
					if(this.Token.TextEqual(Assembler.ErrorName)) {
						context.Assembler.Error(this.message);
					} else {
						context.Assembler.StandardOutput.WriteLine(this.message);
					}
				}
			} else {
				context.Assembler.Error(Resource.IncompleteError(this.Token.Position.ToString()));
			}
			return VoidValue.Value;
		}
	}

	public class Unary : Expression {
		public Token Operation { get; set; }
		public Expression Operand { get; set; }
		private Context Context { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Operation.Value);
			writer.Write("(");
			this.Operand.WriteText(writer, 0);
			writer.Write(")");
		}

		public override Value Evaluate(Context context, int address) {
			context = this.Context ?? context;
			Debug.Assert(context != null);
			Value operand = this.Operand.Evaluate(context, 0).ToSingular();
			if(!operand.IsComplete) {
				return new Unary() { Operation = this.Operation, Operand = (Expression)operand, Context = context };
			}
			NumberValue number = operand.ToNumber();
			if(number == null) {
				context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			switch(this.Operation.Value) {
			case "!": return (number.Value == 0) ? NumberValue.True : NumberValue.False;
			case "+": return number;
			case "-": return new NumberValue(-number.Value);
			case "~": return new NumberValue(~number.Value);
			default:
				Debug.Fail("Unknown unary operator");
				throw new InvalidOperationException();
			}
		}
	}

	public class Binary : Expression {
		public Expression Left { get; set; }
		public Token Operation { get; set; }
		public Expression Right { get; set; }
		private Context Context { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			this.Left.WriteText(writer, 0);
			writer.Write(" ");
			writer.Write(this.Operation.Value);
			writer.Write(" ");
			this.Right.WriteText(writer, 0);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public override Value Evaluate(Context context, int address) {
			context = this.Context ?? context;
			Debug.Assert(context != null);
			switch(this.Operation.Value) {
			case "||": return this.BooleanOr(context);
			case "&&": return this.BooleanAnd(context);
			case "<":  return this.Compare(context, r => r < 0);
			case "<=": return this.Compare(context, r => r <= 0);
			case "==": return this.Compare(context, r => r == 0);
			case "!=": return this.Compare(context, r => r != 0);
			case ">=": return this.Compare(context, r => r >= 0);
			case ">":  return this.Compare(context, r => r > 0);
			case "+":  return this.Plus(context);
			case "-":  return this.Numeric(context, (a, b) => a - b);
			case "*":  return this.Numeric(context, (a, b) => a * b);
			case "/":  return this.Numeric(context, (a, b) => a / b);
			case "%":  return this.Numeric(context, (a, b) => a % b);
			case "&":  return this.Numeric(context, (a, b) => a & b);
			case "|":  return this.Numeric(context, (a, b) => a | b);
			case "^":  return this.Numeric(context, (a, b) => a ^ b);
			case "<<": return this.Numeric(context, (a, b) => a << b);
			case ">>": return this.Numeric(context, (a, b) => a >> b);
			default:
				Debug.Fail("Unknown unary operator");
				throw new InvalidOperationException();
			}
		}

		private Value BooleanOr(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete) {
				return new Binary() { Left = (Expression)left, Operation = this.Operation, Right = this.Right, Context = context };
			}
			NumberValue leftNumber = left.ToNumber();
			if(leftNumber != null) {
				if(leftNumber.Value != 0) {
					return leftNumber;
				}
				Value right = this.Right.Evaluate(context, 0).ToSingular();
				if(!right.IsComplete) {
					return new Binary() { Left = new ValueExpression() { Value = left }, Operation = this.Operation, Right = (Expression)right, Context = context };
				}
				NumberValue rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return rightNumber;
				}
			}
			context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
			return VoidValue.Value;
		}

		private Value BooleanAnd(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete) {
				return new Binary() { Left = (Expression)left, Operation = this.Operation, Right = this.Right, Context = context };
			}
			NumberValue leftNumber = left.ToNumber();
			if(leftNumber != null) {
				if(leftNumber.Value == 0) {
					return leftNumber;
				}
				Value right = this.Right.Evaluate(context, 0).ToSingular();
				if(!right.IsComplete) {
					return new Binary() { Left = new ValueExpression() { Value = left }, Operation = this.Operation, Right = (Expression)right, Context = context };
				}
				NumberValue rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return right;
				}
			}
			context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
			return VoidValue.Value;
		}

		private Value Compare(Context context, Func<int, bool> probe) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				return new Binary() {
					Left = left.IsComplete ? new ValueExpression() { Value = left } : (Expression)left,
					Operation = this.Operation,
					Right = right.IsComplete ? new ValueExpression() { Value = right } : (Expression)right,
					Context = context
				};
			}
			NumberValue leftNumber = left.ToNumber();
			if(leftNumber != null) {
				NumberValue rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return probe(Math.Sign((long)leftNumber.Value - (long)rightNumber.Value)) ? NumberValue.True : NumberValue.False;
				}
				context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			StringValue leftStr = left.ToStringValue();
			if(leftStr != null) {
				StringValue rightStr = right.ToStringValue();
				if(rightStr != null) {
					return probe(StringComparer.Ordinal.Compare(leftStr.Value, rightStr.Value)) ? NumberValue.True : NumberValue.False;
				}
				context.Assembler.Error(Resource.StringValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			context.Assembler.Error(Resource.StringOrNumberValueExpected(this.Operation.Position.ToString()));
			return VoidValue.Value;
		}

		private Value Plus(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				return new Binary() {
					Left = left.IsComplete ? new ValueExpression() { Value = left } : (Expression)left,
					Operation = this.Operation,
					Right = right.IsComplete ? new ValueExpression() { Value = right } : (Expression)right,
					Context = context
				};
			}
			NumberValue leftNumber = left.ToNumber();
			if(leftNumber != null) {
				NumberValue rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return new NumberValue(leftNumber.Value + rightNumber.Value);
				}
				context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			StringValue leftStr = left.ToStringValue();
			if(leftStr != null) {
				StringValue rightStr = right.ToStringValue();
				if(rightStr != null) {
					return new StringValue(leftStr.Value + rightStr.Value);
				}
				context.Assembler.Error(Resource.StringValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			context.Assembler.Error(Resource.StringOrNumberValueExpected(this.Operation.Position.ToString()));
			return VoidValue.Value;
		}

		private Value Numeric(Context context, Func<int, int, int> operation) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				return new Binary() {
					Left = left.IsComplete ? new ValueExpression() { Value = left } : (Expression)left,
					Operation = this.Operation,
					Right = right.IsComplete ? new ValueExpression() { Value = right } : (Expression)right,
					Context = context
				};
			}
			NumberValue leftNumber = left.ToNumber();
			if(leftNumber == null) {
				context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			NumberValue rightNumber = right.ToNumber();
			if(rightNumber == null) {
				context.Assembler.Error(Resource.NumberValueExpected(this.Operation.Position.ToString()));
				return VoidValue.Value;
			}
			return new NumberValue(operation(leftNumber.Value, rightNumber.Value));
		}
	}

	public class If : Expression {
		public Token IfToken { get; set; }
		public Expression Condition { get; set; }
		public ExpressionList Then { get; set; }
		public ExpressionList Else { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write("if(");
			this.Condition.WriteText(writer, 0);
			writer.WriteLine(") {");
			this.Then.WriteText(writer, indent + 1);
			if(this.Else != null) {
				writer.WriteLine();
				Expression.Indent(writer, indent);
				writer.WriteLine("} else {");
				this.Else.WriteText(writer, indent + 1);
			}
			writer.WriteLine();
			Expression.Indent(writer, indent);
			writer.WriteLine("}");
		}

		public override Value Evaluate(Context context, int address) {
			Value condition = this.Condition.Evaluate(context, 0).ToSingular();
			if(!condition.IsComplete) {
				context.Assembler.Error(Resource.IncompleteCondition(this.IfToken.Position.ToString()));
				return VoidValue.Value;
			}
			NumberValue number = condition.ToNumber();
			if(number == null) {
				context.Assembler.Error(Resource.NumberValueExpected(this.IfToken.Position.ToString()));
				return VoidValue.Value;
			}
			if(number.Value != 0) {
				return this.Then.Evaluate(context, address);
			} else if(this.Else != null) {
				return this.Else.Evaluate(context, address);
			}
			return VoidValue.Value;
		}
	}

	public class Call : Expression {
		public Token Name { get; set; }
		public MacroDefinition Macro { get; set; }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
		public List<Expression> Parameter { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
			if(0 < this.Parameter.Count) {
				for(int i = 0; i < this.Parameter.Count; i++) {
					if(0 < i) {
						writer.Write(",");
					}
					writer.Write(" ");
					this.Parameter[i].WriteText(writer, 0);
				}
			}
		}

		public override Value Evaluate(Context context, int address) {
			Debug.Assert(this.Macro.Parameter.Count == this.Parameter.Count);
			Context callContext = new Context() {
				Assembler = context.Assembler, Macro = this.Macro
			};
			foreach(Expression arg in this.Parameter) {
				callContext.AddArgument(arg.Evaluate(context, 0));
			}
			return this.Macro.Body.Evaluate(callContext, address);
		}
	}

	public class ExpressionList : Expression {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
		public List<Expression> List { get; set; }

		public override void WriteText(TextWriter writer, int indent) {
			foreach(Expression expr in this.List) {
				Expression.Indent(writer, indent);
				expr.WriteText(writer, indent);
				writer.WriteLine();
			}
		}

		public override Value Evaluate(Context context, int address) {
			ListValue list = new ListValue();
			foreach(Expression expr in this.List) {
				Value value = expr.Evaluate(context, address);
				value.Address = address;
				address += value.Size();
				list.List.Add(value);
			}
			return list;
		}

		public void WriteListing(Assembler assembler, ListValue result, int indent) {
			this.List.Zip<Expression, Value, int>(result.List, (expr, value) => {
				ExpressionList list = expr as ExpressionList;
				ListValue resultList = value as ListValue;
				if(list != null) {
					Debug.Assert(resultList != null);
					list.WriteListing(assembler, resultList, indent);
					return 0;
				}
				Call call = expr as Call;
				if(call != null) {
					Debug.Assert(resultList != null);
					call.WriteText(assembler.StandardOutput, indent);
					assembler.StandardOutput.WriteLine();
					if(resultList.List.Count > 0) {
						if(call.Macro.Atomic) {
							ExpressionList.WriteAddress(assembler, resultList.List[0].Address);
							foreach(Value v in resultList.List) {
								ExpressionList.WriteText(assembler, v);
							}
							assembler.StandardOutput.WriteLine();
						} else {
							call.Macro.Body.WriteListing(assembler, resultList, indent + 1);
						}
					}
					return 0;
				}
				expr.WriteText(assembler.StandardOutput, indent);
				assembler.StandardOutput.WriteLine();
				ExpressionList.WriteAddress(assembler, value.Address);
				ExpressionList.WriteText(assembler, value);
				assembler.StandardOutput.WriteLine();
				return 0;
			}).All(v => true);
		}

		private static void WriteAddress(Assembler assembler, int address) {
			assembler.StandardOutput.Write(">>> {0:X8} ", address);
		}

		private static void WriteText(Assembler assembler, Value value) {
			NumberValue n = value.ToNumber();
			if(n != null) {
				assembler.StandardOutput.Write("{0:X2} ", n.Value);
			} else {
				StringValue s = value.ToStringValue();
				if(s is StringValue) {
					assembler.StandardOutput.Write("{0} ", s.Value);
				} else {
					ListValue list = value as ListValue;
					if(list != null) {
						foreach(var item in list.List) {
							ExpressionList.WriteText(assembler, item);
						}
					} else {
						Debug.Assert(value is VoidValue);
					}
				}
			}
		}
	}
}
