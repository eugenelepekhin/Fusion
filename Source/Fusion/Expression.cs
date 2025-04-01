using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Fusion {
	public abstract class Expression : Value, IWritable {
		public override bool IsComplete { get { return false; } }

		public abstract void WriteText(TextWriter writer, int indent);

		protected static void Indent(TextWriter writer, int count) {
			for(int i = 0; i < count; i++) {
				writer.Write('\t');
			}
		}

		public abstract Value Evaluate(Context context, int address);

		public override int Size(Context context) {
			return 1;
		}

		public override Value WriteValue(Assembler assembler) {
			throw new InvalidOperationException();
		}

		public virtual void WriteListing(TextWriter writer) => this.WriteText(writer, 0);

		#if DEBUG
			public override string ToString() {
				using(StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {
					this.WriteText(writer, 0);
					return writer.ToString();
				}
			}
		#endif
	}

	public class Label : Expression {
		public Token Name { get; }

		public Label(Token name) {
			this.Name = name;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
			writer.Write(":");
		}

		public override Value Evaluate(Context context, int address) {
			context.DefineLabel(this.Name, address);
			return VoidValue.Value;
		}

		public override int Size(Context context) => 0;
	}

	public class LabelReference : Expression {
		public Token Name { get; }
		private Context? Context { get; }

		public LabelReference(Token name, Context? context) {
			this.Name = name;
			this.Context = context;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
		}

		public override Value Evaluate(Context context, int address) {
			context = this.Context ?? context;
			Debug.Assert(context != null);
			if(context.IsLabelDefined(this.Name)) {
				return new  NumberValue(context, this.Name, context.LabelValue(this.Name));
			} else {
				return new LabelReference(this.Name, context);
			}
		}
	}

	public class ValueExpression : Expression {
		public Value Value { get; }

		public ValueExpression(Value value) {
			this.Value = value;
		}

		public override void WriteText(TextWriter writer, int indent) {
			throw new InvalidOperationException();
		}

		public override Value Evaluate(Context context, int address) {
			return this.Value;
		}

		public override int Size(Context context) => this.Value.Size(context);
	}

	public class Literal : Expression {
		public Token Value { get; }

		public Literal(Token value) {
			this.Value = value;
		}

		public override void WriteText(TextWriter writer, int indent) {
			Debug.Assert(this.Value.Value != null);
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
				return new NumberValue(context, this.Value, this.Value.Number);
			} else {
				Debug.Assert(this.Value.Value != null);
				return new StringValue(this.Value.Value);
			}
		}

		public override int Size(Context context) {
			if(this.Value.IsNumber()) {
				return 1;
			} else {
				Debug.Assert(this.Value.Value != null);
				return this.Value.Value.Length + 1;
			}
		}
	}

	public class Parameter : Expression {
		public MacroDefinition Macro { get; }
		public Token ParameterName { get; }

		public Parameter(MacroDefinition macro, Token parameterName) {
			this.Macro = macro;
			this.ParameterName = parameterName;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.ParameterName.Value);
		}

		public override Value Evaluate(Context context, int address) {
			Value value = context.Argument(this.ParameterName);
			if(!value.IsComplete) {
				if(value is Expression expression) {
					return expression.Evaluate(context, address);
				}
			}
			return value;
		}
	}

	public class Print : Expression {
		public Token Token { get; }
		public Expression Text { get; }

		private string? message;

		public Print(Token token, Expression text) {
			this.Token = token;
			this.Text = text;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Token.Value);
			writer.Write(" ");
			this.Text.WriteText(writer, 0);
			if(this.message != null) {
				writer.WriteLine();
				writer.WriteLine(this.message);
			}
		}

		public override Value Evaluate(Context context, int address) {
			Value text = this.Text.Evaluate(context, 0);
			if(text.IsComplete) {
				this.message = null;
				StringValue? stringValue = text.ToStringValue();
				if(stringValue != null) {
					this.message = stringValue.Value;
				} else {
					context.Assembler.Error(Resource.StringValueExpected(context.PositionStack(this.Token)));
				}
				if(this.message != null) {
					if(this.Token.TextEqual(Assembler.ErrorName)) {
						context.Assembler.Error(Resource.MessageOnStack(this.message, context.PositionStack(this.Token)));
					} else {
						context.Assembler.StandardOutput.WriteLine(Resource.MessageOnStack(this.message, context.PositionStack(this.Token)));
					}
				}
			} else {
				context.Assembler.Error(Resource.IncompleteError(context.PositionStack(this.Token)));
			}
			return VoidValue.Value;
		}

		public override int Size(Context context) => 0;
	}

	public class Unary : Expression {
		public Token Operation { get; }
		public Expression Operand { get; }
		private Context? Context { get; }

		public Unary(Token operation, Expression operand, Context? context) {
			this.Operation = operation;
			this.Operand = operand;
			this.Context = context;
		}

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
				return new Unary(this.Operation, (Expression)operand, context);
			}
			NumberValue? number = operand.ToNumber();
			if(number == null) {
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			switch(this.Operation.Value) {
			case "!": return new NumberValue(context, this.Operation, number.Value == 0);
			case "+": return number;
			case "-": return new NumberValue(context, this.Operation, -number.Value);
			case "~": return new NumberValue(context, this.Operation, ~number.Value);
			default:
				Debug.Fail("Unknown unary operator");
				throw new InvalidOperationException();
			}
		}
	}

	public class ToBoolean : Expression {
		public Expression Operand { get; }
		public Token PositionToken { get; }

		public ToBoolean(Expression operand, Token positionToken) {
			this.Operand = operand;
			this.PositionToken = positionToken;
		}

		public override void WriteText(TextWriter writer, int indent) {
			throw new InvalidOperationException();
		}

		public override Value Evaluate(Context context, int address) {
			Value value = this.Operand.Evaluate(context, address).ToSingular();
			if(!value.IsComplete) {
				return new ToBoolean((Expression)value, this.PositionToken);
			}
			NumberValue? numberValue = value.ToNumber();
			if(numberValue != null) {
				return numberValue.ToBoolean();
			}
			context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.PositionToken)));
			return VoidValue.Value;
		}
	}

	public class BinaryExpr : Expression {
		public Expression Left { get; }
		public Token Operation { get; }
		public Expression Right { get; }
		private Context? Context { get; }

		public BinaryExpr(Expression left, Token operation, Expression right, Context? context) {
			this.Left = left;
			this.Operation = operation;
			this.Right = right;
			this.Context = context;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write("(");
			this.Left.WriteText(writer, 0);
			writer.Write(" ");
			writer.Write(this.Operation.Value);
			writer.Write(" ");
			this.Right.WriteText(writer, 0);
			writer.Write(")");
		}

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
				Debug.Fail("Unknown binary operator");
				throw new InvalidOperationException();
			}
		}

		private Value BooleanOr(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete) {
				return new BinaryExpr((Expression)left, this.Operation, this.Right, context);
			}
			NumberValue? leftNumber = left.ToNumber();
			if(leftNumber != null) {
				if(leftNumber.Value != 0) {
					return leftNumber.ToBoolean();
				}
				Value right = this.Right.Evaluate(context, 0).ToSingular();
				if(!right.IsComplete) {
					//return new Binary() { Left = new ValueExpression() { Value = left }, Operation = this.Operation, Right = (Expression)right, Context = context };
					return new ToBoolean((Expression)right, this.Operation);
				}
				NumberValue? rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return rightNumber.ToBoolean();
				}
			}
			context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
			return VoidValue.Value;
		}

		private Value BooleanAnd(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete) {
				return new BinaryExpr((Expression)left, this.Operation, this.Right, context);
			}
			NumberValue? leftNumber = left.ToNumber();
			if(leftNumber != null) {
				if(leftNumber.Value == 0) {
					return leftNumber.ToBoolean();
				}
				Value right = this.Right.Evaluate(context, 0).ToSingular();
				if(!right.IsComplete) {
					//return new Binary() { Left = new ValueExpression() { Value = left }, Operation = this.Operation, Right = (Expression)right, Context = context };
					return new ToBoolean((Expression)right, this.Operation);
				}
				NumberValue? rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return rightNumber.ToBoolean();
				}
			}
			context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
			return VoidValue.Value;
		}

		private Value Compare(Context context, Func<int, bool> probe) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				return new BinaryExpr(
					left.IsComplete ? new ValueExpression(left) : (Expression)left,
					this.Operation,
					right.IsComplete ? new ValueExpression(right) : (Expression)right,
					context
				);
			}
			NumberValue? leftNumber = left.ToNumber();
			if(leftNumber != null) {
				NumberValue? rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return new NumberValue(context, this.Operation, probe(Math.Sign((long)leftNumber.Value - (long)rightNumber.Value)));
				}
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			StringValue? leftStr = left.ToStringValue();
			if(leftStr != null) {
				StringValue? rightStr = right.ToStringValue();
				if(rightStr != null) {
					return new NumberValue(context, this.Operation, probe(StringComparer.Ordinal.Compare(leftStr.Value, rightStr.Value)));
				}
				context.Assembler.Error(Resource.StringValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			context.Assembler.Error(Resource.StringOrNumberValueExpected(context.PositionStack(this.Operation)));
			return VoidValue.Value;
		}

		private Value Plus(Context context) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				if((left.IsComplete && left is StringValue) || (right.IsComplete && right is StringValue)) {
					context.Assembler.Error(Resource.IncompleteString(context.PositionStack(this.Operation)));
				}
				return new BinaryExpr(
					left.IsComplete ? new ValueExpression(left) : (Expression)left,
					this.Operation,
					right.IsComplete ? new ValueExpression(right) : (Expression)right,
					context
				);
			}
			NumberValue? leftNumber = left.ToNumber();
			if(leftNumber != null) {
				NumberValue? rightNumber = right.ToNumber();
				if(rightNumber != null) {
					return new NumberValue(context, this.Operation, leftNumber.Value + rightNumber.Value);
				}
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			StringValue? leftStr = left.ToStringValue();
			if(leftStr != null) {
				StringValue? rightStr = right.ToStringValue();
				if(rightStr != null) {
					return new StringValue(leftStr.Value + rightStr.Value);
				}
				context.Assembler.Error(Resource.StringValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			context.Assembler.Error(Resource.StringOrNumberValueExpected(context.PositionStack(this.Operation)));
			return VoidValue.Value;
		}

		private Value Numeric(Context context, Func<int, int, int> operation) {
			Value left = this.Left.Evaluate(context, 0).ToSingular();
			Value right = this.Right.Evaluate(context, 0).ToSingular();
			if(!left.IsComplete || !right.IsComplete) {
				return new BinaryExpr(left.IsComplete ? new ValueExpression(left) : (Expression)left, this.Operation, right.IsComplete ? new ValueExpression(right) : (Expression)right, context);
			}
			NumberValue? leftNumber = left.ToNumber();
			if(leftNumber == null) {
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			NumberValue? rightNumber = right.ToNumber();
			if(rightNumber == null) {
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.Operation)));
				return VoidValue.Value;
			}
			return new NumberValue(context, this.Operation, operation(leftNumber.Value, rightNumber.Value));
		}
	}

	public class IfExpr : Expression {
		public Token IfToken { get; }
		public Expression Condition { get; }
		public ExpressionList Then { get; }
		public ExpressionList? Else { get; }
		private Context? Context {get; }

		public IfExpr(Token ifToken, Expression condition, ExpressionList then, ExpressionList? @else, Context? context) {
			this.IfToken = ifToken;
			this.Condition = condition;
			this.Then = then;
			this.Else = @else;
			this.Context = context;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write("if(");
			this.Condition.WriteText(writer, 0);
			writer.WriteLine(") {");
			this.Then.WriteText(writer, indent + 1);
			if(this.Else != null) {
				Expression.Indent(writer, indent);
				writer.WriteLine("} else {");
				this.Else.WriteText(writer, indent + 1);
			}
			Expression.Indent(writer, indent);
			writer.WriteLine("}");
		}

		public override Value Evaluate(Context context, int address) {
			context = this.Context ?? context;
			Debug.Assert(context != null);
			Value condition = this.Condition.Evaluate(context, 0).ToSingular();
			if(!condition.IsComplete) {
				Debug.Assert(this.Context == null);
				int thenSize = this.Then.Size(context);
				int elseSize = (this.Else != null) ? this.Else.Size(context) : 0;
				if(thenSize != elseSize) {
					context.Assembler.Error(Resource.IncompleteCondition(context.PositionStack(this.IfToken)));
					return VoidValue.Value;
				}
				Debug.Assert(condition is Expression);
				return new IfExpr(this.IfToken, (Expression)condition, this.Then, this.Else, context);
			}
			NumberValue? number = condition.ToNumber();
			if(number == null) {
				context.Assembler.Error(Resource.NumberValueExpected(context.PositionStack(this.IfToken)));
				return VoidValue.Value;
			}
			if(number.Value != 0) {
				return this.Then.Evaluate(context, address);
			} else if(this.Else != null) {
				return this.Else.Evaluate(context, address);
			}
			return VoidValue.Value;
		}

		public override int Size(Context context) {
			if(this.Else == null) {
				return this.Then.Size(context);
			}
			Value condition = this.Condition.Evaluate(context, 0).ToSingular();
			if(condition.IsComplete) {
				NumberValue? number = condition.ToNumber();
				if(number != null) {
					if(number.Value != 0) {
						return this.Then.Size(context);
					} else {
						return this.Else.Size(context);
					}
				} else {
					return 0; // ignore now. this will cause an error later.
				}
			} else {
				int size = this.Then.Size(context);
				if(size != this.Else.Size(context)) {
					context.Assembler.Error(Resource.IncompleteCondition(context.PositionStack(this.IfToken)));
				}
				return size;
			}
		}
	}

	public class CallExpr : Expression {
		public Token Name { get; }
		public MacroDefinition Macro { get; }
		public IList<Expression> Argument { get; }

		public CallExpr(Token name, MacroDefinition macro, IList<Expression> argument) {
			this.Name = name;
			this.Macro = macro;
			this.Argument = argument;
		}

		public override void WriteText(TextWriter writer, int indent) {
			writer.Write(this.Name.Value);
			if(0 < this.Argument.Count) {
				writer.Write(' ');
				this.Macro.WriteWithPattern(this.Argument, writer);
			}
		}

		public override Value Evaluate(Context context, int address) {
			Debug.Assert(this.Macro.Parameters.Count == this.Argument.Count);
			Context callContext = new Context(context, this.Macro, this);
			foreach(Expression arg in this.Argument) {
				callContext.AddArgument(arg.Evaluate(context, 0));
			}
			return this.Macro.Body.Evaluate(callContext, address);
		}

		public override int Size(Context context) {
			Debug.Assert(this.Macro.Parameters.Count == this.Argument.Count);
			Context callContext = new Context(context, this.Macro, this);
			foreach(Expression arg in this.Argument) {
				callContext.AddArgument(arg.Evaluate(context, 0));
			}
			return this.Macro.Body.Size(callContext);
		}
	}

	public class ExpressionList : Expression {
		public IList<Expression> List { get; } = new List<Expression>();

		public override void WriteText(TextWriter writer, int indent) {
			foreach(Expression expr in this.List) {
				Expression.Indent(writer, (expr is Label) ? (indent - 1) : indent);
				expr.WriteText(writer, indent);
				writer.WriteLine();
			}
		}

		public override Value Evaluate(Context context, int address) {
			ListValue list = new ListValue {
				Address = address
			};
			foreach(Expression expr in this.List) {
				Value value = expr.Evaluate(context, address);
				value.Address = address;
				address += value.Size(context);
				list.List.Add(value);
			}
			return list;
		}

		public void WriteListing(Assembler assembler, ListValue result, int indent) {
			bool all = this.List.Zip<Expression, Value, int>(result.List, (expr, value) => {
				ListValue? resultList = value as ListValue;
				if(expr is ExpressionList list) {
					Debug.Assert(resultList != null);
					list.WriteListing(assembler, resultList, indent);
					return 0;
				}
				if(expr is not Label) {
					Expression.Indent(assembler.StandardOutput, indent);
				}
				if(expr is CallExpr call) {
					Debug.Assert(resultList != null);
					call.WriteText(assembler.StandardOutput, indent);
					assembler.StandardOutput.WriteLine();
					if(resultList.List.Count > 0) {
						if(call.Macro.Atomic) {
							ExpressionList.WriteAddress(assembler, resultList.Address);
							foreach(Value v in resultList.List) {
								ExpressionList.WriteText(assembler, v);
							}
							assembler.StandardOutput.WriteLine();
						} else {
							call.Macro.Body.WriteListing(assembler, resultList, indent);
						}
					}
					return 0;
				}
				expr.WriteText(assembler.StandardOutput, indent);
				assembler.StandardOutput.WriteLine();
				if(!(value is VoidValue)) {
					ExpressionList.WriteAddress(assembler, value.Address);
				}
				ExpressionList.WriteText(assembler, value);
				assembler.StandardOutput.WriteLine();
				return 0;
			}).All(v => true);
		}

		private static void WriteAddress(Assembler assembler, int address) {
			assembler.StandardOutput.Write(">>> {0:X8} ", address);
		}

		private static void WriteText(Assembler assembler, Value value) {
			NumberValue? n = value.ToNumber();
			if(n != null) {
				int cellSize = assembler.BinaryFormatter.CellSize;
				int result = (cellSize == 32) ? n.Value : n.Value & ((1 << cellSize) - 1);
				string format = string.Format(CultureInfo.InvariantCulture, "{{0:X{0}}} ", cellSize / 4);
				assembler.StandardOutput.Write(format, result);
			} else {
				StringValue? s = value.ToStringValue();
				if(s is StringValue) {
					assembler.StandardOutput.Write("{0} ", s.Value);
				} else {
					if(value is ListValue list) {
						foreach(var item in list.List) {
							ExpressionList.WriteText(assembler, item);
						}
					} else {
						Debug.Assert(value is VoidValue);
					}
				}
			}
		}

		public override int Size(Context context) => this.List.Sum(expr => expr.Size(context));
	}
}
