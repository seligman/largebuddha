using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ScottsUtils.Equation
{
    public delegate string ValueEvaluator(string value);

    public class EquationException : Exception
    {
        public EquationException(string error)
            : base(error)
        {
        }
    }

    public class Equation<NumType>
    {
        public bool ThrowExceptions { get; set; }

        public Equation()
        {
            ThrowExceptions = false;
        }

        const string c_AllNumbers = "0123456789.,";
        const string c_AllOperands = "+-/*()^!=|<>&%";
        const string c_AllOperandsContinue = "<>=";
        const string c_AllVariables = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        Random m_Random = null;

        enum Operands
        {
            Number,
            Multiply,
            Divide,
            Add,
            Subtract,
            Power,
            Variable,
            Abs,
            Acos,
            Asin,
            Atan,
            Ceiling,
            Cos,
            Cosh,
            Exp,
            Floor,
            Int,
            Log,
            Sign,
            Sin,
            Sinh,
            Sqrt,
            Sqr,
            Tan,
            Tanh,
            Truncate,
            Factorial,
            Random,
            Pi,
            GreaterThan,
            LessThan,
            GreaterThanEquals,
            LessThanEquals,
            Equals,
            NotEquals,
            And,
            Or,
            Modulus,
            Not,
            Reciproc,
            Invalid
        }

        class Value
        {
            internal bool IsNumber = false;
            internal NumType Number;
            internal int Level = 0;
            internal string Variable = "";
            internal Operands Operand = Operands.Invalid;

            public override string ToString()
            {
                return Number.ToString() +
                    " (Level: " + Level.ToString() + ", " +
                    "Operand: " + Operand.ToString() + ", " +
                    "Variable: " + Variable + ")";
            }

            internal void SetNumber(NumType value)
            {
                Number = value;
                SetNumber();
            }

            internal void SetNumber()
            {
                Operand = Operands.Number;
                IsNumber = true;
            }

            internal void SetOperand(Operands value)
            {
                Operand = value;
                IsNumber = false;
            }

            internal Value(NumType Number, int Level)
            {
                this.IsNumber = true;
                this.Number = Number;
                this.Operand = Operands.Number;
                this.Level = Level;
            }

            internal Value(NumType Zero, Operands Operand, int Level)
            {
                this.IsNumber = false;
                this.Number = Zero;
                this.Operand = Operand;
                this.Level = Level;
            }

            internal Value(Equation<NumType> parent, ref string errorMessage, Operands Operand, int Level)
            {
                this.IsNumber = false;
                this.Number = parent.InternalFromDouble(ref errorMessage, 0);
                this.Operand = Operand;
                this.Level = Level;
            }
        }

        static class OperandStr
        {
            internal const string OpenParen = "(";
            internal const string CloseParen = ")";
            internal const string Abs = "ABS";
            internal const string Multiply = "*";
            internal const string Divide = "/";
            internal const string Modulus = "%";
            internal const string Add = "+";
            internal const string Subtract = "-";
            internal const string Power = "^";
            internal const string Acos = "ACOS";
            internal const string Asin = "ASIN";
            internal const string Atan = "ATAN";
            internal const string Ceiling = "CEILING";
            internal const string Cos = "COS";
            internal const string Cosh = "COSH";
            internal const string Exp = "EXP";
            internal const string Floor = "FLOOR";
            internal const string Int = "INT";
            internal const string Log = "LOG";
            internal const string Sign = "SIGN";
            internal const string Sin = "SIN";
            internal const string Sinh = "SINH";
            internal const string Sqrt = "SQRT";
            internal const string Sqr = "SQR";
            internal const string Tan = "TAN";
            internal const string Tanh = "TANH";
            internal const string Truncate = "TRUNCATE";
            internal const string Random = "RAND";
            internal const string Pi = "PI";
            internal const string Factorial = "!";
            internal const string EqualsStr = "=";
            internal const string NotEquals = "!=";
            internal const string LessThan = "<";
            internal const string GreaterThan = ">";
            internal const string LessThanEquals = "<=";
            internal const string GreaterThanEquals = ">=";
            internal const string And = "&";
            internal const string Or = "|";
            internal const string Not = "NOT";
            internal const string Reciproc = "RECIPROC";

            internal static string[] GetAllWords()
            {
                Type cur = typeof(OperandStr);

                List<string> ret = new List<string>();

                foreach (MemberInfo mi in cur.GetMembers(BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (mi.MemberType == MemberTypes.Field &&
                        ((FieldInfo)mi).FieldType == typeof(string))
                    {
                        string val = (string)(((FieldInfo)mi).GetValue(null));
                        if (val.Length != 1)
                        {
                            ret.Add(val);
                        }
                    }
                }

                return ret.ToArray();
            }

            internal static string[] GetAllChars()
            {
                Type cur = typeof(OperandStr);

                List<string> ret = new List<string>();

                foreach (MemberInfo mi in cur.GetMembers(BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (mi.MemberType == MemberTypes.Field &&
                        ((FieldInfo)mi).FieldType == typeof(string))
                    {
                        string val = (string)(((FieldInfo)mi).GetValue(null));
                        if (val.Length == 1)
                        {
                            ret.Add(val);
                        }
                    }
                }

                return ret.ToArray();
            }
        }

        static string BuildRe(string[] vals)
        {
            StringBuilder re = new StringBuilder();
            bool first = true;

            re.Append("(");
            foreach (string oper in vals)
            {
                if (!first)
                {
                    re.Append("|");
                }
                first = false;
                if (oper.Length == 1)
                {
                    re.Append("\\");
                }
                re.Append(oper);
            }
            re.Append(")");

            return re.ToString();
        }

        Random Random
        {
            get
            {
                if (m_Random == null)
                {
                    m_Random = new Random();
                }

                return m_Random;
            }
        }

        public void SeedRandom(int value)
        {
            m_Random = new Random(value);
        }

        static Equation<NumType> m_static = null;

        public static Equation<NumType> Static
        {
            get
            {
                if (m_static == null)
                {
                    m_static = new Equation<NumType>();
                }

                return m_static;
            }
        }

        public bool TryEvaluate(string expression, out NumType output)
        {
            string error = null;

            output = Evaluate(expression, ref error, (Dictionary<string, NumType>)null);

            if (error == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string Evaluate(string equation, ValueEvaluator eval)
        {
            string ret = equation;

            LinkedList<string> chars = new LinkedList<string>();
            LinkedList<string> words = new LinkedList<string>();

            ret = Regex.Replace(ret,
                BuildRe(OperandStr.GetAllChars()),
                delegate(Match match)
                {
                    chars.AddLast(match.Groups[0].Value);
                    return "\x01";
                },
                RegexOptions.IgnoreCase);

            ret = Regex.Replace(ret,
                "([^a-z]|^)" + BuildRe(OperandStr.GetAllWords()) + "([^a-z]|$)",
                delegate(Match match)
                {
                    words.AddLast(match.Groups[2].Value);
                    return match.Groups[1].Value + "\x02" + match.Groups[3].Value;
                },
                RegexOptions.IgnoreCase);

            if ((chars.Count + words.Count) > 0)
            {
                ret = Regex.Replace(ret,
                    "[^\x01\x02]+",
                    delegate(Match match)
                    {
                        return eval(match.Groups[0].Value);
                    });

                ret = Regex.Replace(ret, "\x01",
                    delegate(Match match)
                    {
                        string temp = chars.First.Value;
                        chars.RemoveFirst();
                        return temp;
                    });

                ret = Regex.Replace(ret, "\x02",
                    delegate(Match match)
                    {
                        string temp = words.First.Value;
                        words.RemoveFirst();
                        return temp;
                    });

                return ret;
            }
            else
            {
                return eval(equation);
            }
        }

        public NumType Evaluate(string expression)
        {
            string error = null;

            return Evaluate(expression, ref error, (Dictionary<string, NumType>)null, true);
        }

        public NumType Evaluate(string expression, bool stripCommas)
        {
            string error = null;

            return Evaluate(expression, ref error, (Dictionary<string, NumType>)null, stripCommas);
        }

        public NumType Evaluate(string expression, params NumType[] variables)
        {
            string error = null;

            return Evaluate(expression, ref error, new List<NumType>(variables), true);
        }

        public NumType Evaluate(string expression, List<NumType> variables)
        {
            string error = null;

            return Evaluate(expression, ref error, variables, true);
        }

        public NumType Evaluate(string expression, Dictionary<string, NumType> variables)
        {
            string error = null;

            return Evaluate(expression, ref error, variables, true);
        }

        public NumType Evaluate(string expression, bool stripCommas, params NumType[] variables)
        {
            string error = null;

            return Evaluate(expression, ref error, new List<NumType>(variables), stripCommas);
        }

        public NumType Evaluate(string expression, bool stripCommas, List<NumType> variables)
        {
            string error = null;

            return Evaluate(expression, ref error, variables, stripCommas);
        }

        public NumType Evaluate(string expression, bool stripCommas, Dictionary<string, NumType> variables)
        {
            string error = null;

            return Evaluate(expression, ref error, variables, stripCommas);
        }

        public NumType Evaluate(string expression, ref string errorMessage, List<NumType> variables)
        {
            return Evaluate(expression, ref errorMessage, variables, true);
        }

        public NumType Evaluate(string expression, ref string errorMessage, Dictionary<string, NumType> variables)
        {
            return Evaluate(expression, ref errorMessage, variables, true);
        }

        public NumType Evaluate(string expression, ref string errorMessage, List<NumType> variables, bool stripCommas)
        {
            return Evaluate(expression, ref errorMessage, ListToDict(variables), stripCommas);
        }

        Dictionary<string, NumType> ListToDict(List<NumType> list)
        {
            if (list == null)
            {
                return null;
            }
            else
            {
                Dictionary<string, NumType> ret = new Dictionary<string, NumType>();

                for (int i = 0; i < list.Count; i++)
                {
                    ret.Add((('a') + i).ToString(), list[i]);
                }

                return ret;
            }
        }

        bool NormalizeDict(Dictionary<string, NumType> values, Dictionary<string, NumType> dest, ref string errorMessage)
        {
            Dictionary<string, NumType> ret = new Dictionary<string, NumType>();

            foreach (var kvp in values)
            {
                if (dest.ContainsKey(kvp.Key.ToUpper()))
                {
                    errorMessage = "The variable \"" + kvp.Key + "\" was used more than once.";
                    return false;
                }

                dest.Add(kvp.Key.ToUpper(), kvp.Value);
            }

            return true;
        }

        public NumType Evaluate(string expression, ref string errorMessage, Dictionary<string, NumType> variables, bool stripCommas)
        {
            int count = 0;

            LinkedList<Value> args = new LinkedList<Value>();
            int level = 0;
            int maxLevel = 0;

            expression = expression.ToUpper();

            if (!BreakupTokens(expression, ref count, args, stripCommas, ref level, ref maxLevel, ref errorMessage))
            {
                return InternalFromDouble(ref errorMessage, 0);
            }

            if (level < 0)
            {
                InternalError(ref errorMessage, "Unmatched number of parentheses");
                return InternalFromDouble(ref errorMessage, 0);
            }

            if (variables != null)
            {
                Dictionary<string, NumType> normalized = new Dictionary<string, NumType>();
                if (!NormalizeDict(variables, normalized, ref errorMessage))
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }

                if (!SubstituteVariables(args, normalized, ref errorMessage))
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            if (count == 0)
            {
                return InternalFromDouble(ref errorMessage, 0);
            }
            else if (count == 1)
            {
                if (args.First.Value.IsNumber)
                {
                    return args.First.Value.Number;
                }
                else if (
                    args.First.Value.Operand != Operands.Random &&
                    args.First.Value.Operand != Operands.Pi)
                {
                    InternalError(ref errorMessage, "Invalid expression");
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            level = maxLevel + 1;

            for (LinkedListNode<Value> cur = args.First; cur != null; cur = cur.Next)
            {
                if (cur.Value.Operand == Operands.Variable)
                {
                    InternalError(ref errorMessage, "Undefined \"" + cur.Value.Variable + "\" variable.");
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            do
            {
                level--;
                if (level < 0)
                {
                    InternalError(ref errorMessage, "Parse error");
                    return InternalFromDouble(ref errorMessage, 0);
                }
                count = 0;

                if (!ProcessOperands(args, level, ref errorMessage))
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }

                count = RemoveInvalidOperands(count, args, level);

            } while (count != 1);

            if (args.First.Value.IsNumber)
            {
                return args.First.Value.Number;
            }
            else
            {
                InternalError(ref errorMessage, "Invalid expression");
                return InternalFromDouble(ref errorMessage, 0);
            }
        }

        void InternalError(ref string errorMessage, string message)
        {
            if (ThrowExceptions)
            {
                throw new EquationException(message);
            }
            else
            {
                errorMessage = message;
            }
        }

        static bool SubstituteVariables(LinkedList<Value> args, Dictionary<string, NumType> variables, ref string errorMessage)
        {
            for (LinkedListNode<Value> cur = args.First; cur != null; cur = cur.Next)
            {
                if (cur.Value.Operand == Operands.Variable &&
                    cur.Value.Variable != null &&
                    cur.Value.Variable.Length >= 1)
                {
                    if (variables.ContainsKey(cur.Value.Variable.ToUpper()))
                    {
                        cur.Value.SetNumber(variables[cur.Value.Variable.ToUpper()]);
                    }
                    else
                    {
                        errorMessage = "Unable to find variable \"" + cur.Value.Variable.ToUpper() + "\"";
                        return false;
                    }
                }
            }
            return true;
        }

        static int RemoveInvalidOperands(int count, LinkedList<Value> args, int level)
        {
            LinkedList<LinkedListNode<Value>> toRemove =
                new LinkedList<LinkedListNode<Value>>();

            for (LinkedListNode<Value> cur = args.First; cur != null; cur = cur.Next)
            {
                if (cur.Value.Level == level)
                {
                    cur.Value.Level = (level - 1);
                }

                if (cur.Value.Operand == Operands.Invalid)
                {
                    toRemove.AddLast(cur);
                }
                else
                {
                    count++;
                }
            }

            for (LinkedListNode<LinkedListNode<Value>> cur = toRemove.First;
                cur != null; cur = cur.Next)
            {
                args.Remove(cur.Value);
            }

            return count;
        }

        bool ProcessOperands(LinkedList<Value> args, int level, ref string errorMessage)
        {
            for (int pass = 1; pass <= 5; pass++)
            {
                List<Operands> validOpers = new List<Operands>();
                switch (pass)
                {
                    case 1:
                        validOpers.Add(Operands.Factorial);
                        validOpers.Add(Operands.Abs);
                        validOpers.Add(Operands.Acos);
                        validOpers.Add(Operands.Asin);
                        validOpers.Add(Operands.Atan);
                        validOpers.Add(Operands.Ceiling);
                        validOpers.Add(Operands.Cos);
                        validOpers.Add(Operands.Cosh);
                        validOpers.Add(Operands.Exp);
                        validOpers.Add(Operands.Floor);
                        validOpers.Add(Operands.Int);
                        validOpers.Add(Operands.Log);
                        validOpers.Add(Operands.Sign);
                        validOpers.Add(Operands.Sin);
                        validOpers.Add(Operands.Sinh);
                        validOpers.Add(Operands.Sqrt);
                        validOpers.Add(Operands.Sqr);
                        validOpers.Add(Operands.Tan);
                        validOpers.Add(Operands.Tanh);
                        validOpers.Add(Operands.Truncate);
                        validOpers.Add(Operands.Random);
                        validOpers.Add(Operands.Pi);
                        validOpers.Add(Operands.Reciproc);
                        break;
                    case 2:
                        validOpers.Add(Operands.Multiply);
                        validOpers.Add(Operands.Divide);
                        validOpers.Add(Operands.Modulus);
                        validOpers.Add(Operands.Power);
                        break;
                    case 3:
                        validOpers.Add(Operands.Subtract);
                        validOpers.Add(Operands.Add);
                        break;
                    case 4:
                        validOpers.Add(Operands.Equals);
                        validOpers.Add(Operands.NotEquals);
                        validOpers.Add(Operands.GreaterThan);
                        validOpers.Add(Operands.LessThan);
                        validOpers.Add(Operands.GreaterThanEquals);
                        validOpers.Add(Operands.LessThanEquals);
                        break;
                    case 5:
                        validOpers.Add(Operands.And);
                        validOpers.Add(Operands.Or);
                        validOpers.Add(Operands.Not);
                        break;
                }

                for (LinkedListNode<Value> cur = args.First; cur != null; cur = cur.Next)
                {
                    if (!cur.Value.IsNumber && cur.Value.Level == level &&
                        validOpers.Contains(cur.Value.Operand))
                    {
                        bool needNext = false;
                        bool needPrev = false;

                        LinkedListNode<Value> prev = GetNextNumber(cur, false);
                        LinkedListNode<Value> next = GetNextNumber(cur, true);

                        switch (cur.Value.Operand)
                        {
                            case Operands.Pi:
                            case Operands.Random:
                                // I don't need either
                                break;
                            case Operands.Equals:
                            case Operands.NotEquals:
                            case Operands.LessThan:
                            case Operands.GreaterThan:
                            case Operands.LessThanEquals:
                            case Operands.GreaterThanEquals:
                            case Operands.And:
                            case Operands.Or:
                            case Operands.Multiply:
                            case Operands.Divide:
                            case Operands.Modulus:
                            case Operands.Add:
                            case Operands.Power:
                                needPrev = true;
                                needNext = true;
                                break;
                            case Operands.Subtract:
                            case Operands.Abs:
                            case Operands.Acos:
                            case Operands.Asin:
                            case Operands.Atan:
                            case Operands.Ceiling:
                            case Operands.Cos:
                            case Operands.Cosh:
                            case Operands.Exp:
                            case Operands.Floor:
                            case Operands.Int:
                            case Operands.Log:
                            case Operands.Sign:
                            case Operands.Sin:
                            case Operands.Sinh:
                            case Operands.Sqrt:
                            case Operands.Sqr:
                            case Operands.Tan:
                            case Operands.Tanh:
                            case Operands.Truncate:
                            case Operands.Not:
                            case Operands.Reciproc:
                                needNext = true;
                                break;
                            case Operands.Factorial:
                                needPrev = true;
                                break;
                            default:
                                InternalError(ref errorMessage, "Internal error");
                                return false;
                        }

                        if (needNext && next == null)
                        {
                            InternalError(ref errorMessage, "Operand missing right value");
                            return false;
                        }

                        if (needPrev && prev == null)
                        {
                            InternalError(ref errorMessage, "Operand missing left value");
                            return false;
                        }

                        switch (cur.Value.Operand)
                        {
                            case Operands.Factorial:
                                cur.Value.Number = InternalFromDouble(ref errorMessage,
                                    Factorial(InternalToNumDouble(ref errorMessage, prev.Value.Number)));
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Multiply:
                                cur.Value.Number = (NumType)InternalMul(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Equals:
                                cur.Value.Number = (NumType)InternalEqu(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.NotEquals:
                                cur.Value.Number = (NumType)InternalNeq(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.LessThan:
                                cur.Value.Number = (NumType)InternalLT(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.GreaterThan:
                                cur.Value.Number = (NumType)InternalGT(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.LessThanEquals:
                                cur.Value.Number = (NumType)InternalLTE(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.GreaterThanEquals:
                                cur.Value.Number = (NumType)InternalGTE(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.And:
                                cur.Value.Number = (NumType)InternalAnd(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Or:
                                cur.Value.Number = (NumType)InternalOr(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Divide:
                                cur.Value.Number = (NumType)InternalDiv(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Reciproc:
                                cur.Value.Number = (NumType)InternalDiv(ref errorMessage, InternalFromDouble(ref errorMessage, 1), next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Modulus:
                                cur.Value.Number = (NumType)InternalMod(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Add:
                                cur.Value.Number = (NumType)InternalAdd(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Subtract:
                                if (prev == null)
                                {
                                    cur.Value.Number = (NumType)InternalMul(ref errorMessage, next.Value.Number,
                                        InternalFromDouble(ref errorMessage, -1));
                                    if (errorMessage != null)
                                    {
                                        return false;
                                    }
                                    cur.Value.SetNumber();
                                }
                                else
                                {
                                    needPrev = true;
                                    cur.Value.Number = (NumType)InternalSub(ref errorMessage, prev.Value.Number, next.Value.Number);
                                    if (errorMessage != null)
                                    {
                                        return false;
                                    }
                                    cur.Value.SetNumber();
                                }
                                break;
                            case Operands.Power:
                                cur.Value.Number = InternalPow(ref errorMessage, prev.Value.Number, next.Value.Number);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Abs:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Abs", Math.Abs, Math.Abs, Math.Abs, Math.Abs, null, null, Math.Abs);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Acos:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Acos", Math.Acos, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Asin:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Asin", Math.Asin, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Atan:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Atan", Math.Atan, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Ceiling:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Ceiling", Math.Ceiling, null, null, null, null, null, Math.Ceiling);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Cos:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Cos", Math.Cos, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Cosh:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Cosh", Math.Cosh, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Exp:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Exp", Math.Exp, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Floor:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Floor", Math.Floor, null, null, null, null, null, Math.Floor);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Log:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Log", Math.Log, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Pi:
                                cur.Value.Number = InternalFromDouble(ref errorMessage, 3.141592653589);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Random:
                                cur.Value.Number = InternalFromDouble(ref errorMessage, Random.NextDouble());
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Sign:
                                cur.Value.Number = InternalOperInt(ref errorMessage, next.Value.Number, "Sign", Math.Sign, Math.Sign, Math.Sign, Math.Sign, null, null, Math.Sign);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Sin:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Sin", Math.Sin, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Not:
                                cur.Value.Number = (NumType)InternalEqu(ref errorMessage, next.Value.Number,
                                    InternalFromDouble(ref errorMessage, 0));
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Sinh:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Sinh", Math.Sinh, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Sqr:
                            case Operands.Sqrt:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Sqrt", Math.Sqrt, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Tan:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Tan", Math.Tan, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Tanh:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Tanh", Math.Tanh, null, null, null, null, null, null);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            case Operands.Int:
                            case Operands.Truncate:
                                cur.Value.Number = InternalOper(ref errorMessage, next.Value.Number, "Truncate", Math.Truncate, null, null, null, null, null, Math.Truncate);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                cur.Value.SetNumber();
                                break;
                            default:
                                InternalError(ref errorMessage, "Internal error");
                                return false;
                        }

                        if (prev != null && needPrev)
                        {
                            prev.Value.SetOperand(Operands.Invalid);
                        }
                        if (next != null && needNext)
                        {
                            next.Value.SetOperand(Operands.Invalid);
                        }
                    }
                }
            }

            return true;
        }

        static double Factorial(double value)
        {
            double ret = 1;
            value = Math.Floor(value);

            while (value > 1)
            {
                ret *= value;
                value--;
            }

            return ret;
        }

        static LinkedListNode<Value> GetNextNumber(LinkedListNode<Value> start, bool forward)
        {
            int level = start.Value.Level;

            while (true)
            {
                if (forward)
                {
                    start = start.Next;
                }
                else
                {
                    start = start.Previous;
                }

                if (start == null)
                {
                    return null;
                }

                if (start.Value.Level != level)
                {
                    return null;
                }

                if (start.Value.IsNumber)
                {
                    return start;
                }
            }
        }

        bool BreakupTokens(string expression, ref int count, LinkedList<Value> args, bool stripCommas, ref int level, ref int maxLevel, ref string errorMessage)
        {
            int start = 0;

            bool inOperand = false;
            bool inValue = false;
            bool inVariable = false;

            for (int pos = 0; pos <= expression.Length; pos++)
            {
                bool value = false;
                bool operand = false;
                bool variable = false;
                bool operandContinue = false;

                if (pos != expression.Length)
                {
                    if (c_AllNumbers.IndexOf(expression[pos].ToString()) >= 0)
                    {
                        value = true;
                    }
                    else if (!stripCommas && expression[pos] == '_')
                    {
                        value = true;
                    }
                    else if (c_AllOperands.IndexOf(expression[pos].ToString()) >= 0)
                    {
                        operand = true;
                        if (c_AllOperandsContinue.IndexOf(expression[pos].ToString()) >= 0)
                        {
                            operandContinue = true;
                        }
                    }
                    else if (c_AllVariables.IndexOf(expression[pos].ToString()) >= 0)
                    {
                        variable = true;
                    }
                }

                int parseStart = 0;
                int parseEnd = 0;
                bool parseValue = false;
                bool parseOperand = false;
                bool parseVariable = false;

                if (value)
                {
                    if (inOperand || inVariable)
                    {
                        parseStart = start;
                        parseEnd = pos;
                        parseOperand = inOperand;
                        parseValue = inValue;
                        parseVariable = inVariable;
                        inOperand = false;
                        inVariable = false;
                        start = pos;
                    }
                    if (!inValue)
                    {
                        start = pos;
                        inValue = true;
                    }
                }
                else if (variable)
                {
                    if (inOperand || inValue)
                    {
                        parseStart = start;
                        parseEnd = pos;
                        parseOperand = inOperand;
                        parseValue = inValue;
                        parseVariable = inVariable;
                        inOperand = false;
                        inValue = false;
                        start = pos;
                    }
                    if (!inVariable)
                    {
                        start = pos;
                        inVariable = true;
                    }
                }
                else if (operand)
                {
                    if ((!operandContinue && (inVariable || inValue || inOperand)) ||
                        (operandContinue && (inVariable || inValue)))
                    {
                        parseStart = start;
                        parseEnd = pos;
                        parseOperand = inOperand;
                        parseValue = inValue;
                        parseVariable = inVariable;
                        inValue = false;
                        inOperand = false;
                        inVariable = false;
                        start = pos;
                    }
                    if (!inOperand)
                    {
                        start = pos;
                        inOperand = true;
                    }
                }
                else if (inOperand || inValue || inVariable)
                {
                    parseStart = start;
                    parseEnd = pos;
                    parseOperand = inOperand;
                    parseValue = inValue;
                    parseVariable = inVariable;
                    inValue = false;
                    inOperand = false;
                    inVariable = false;
                    start = pos;
                }

                if (parseValue || parseVariable || parseOperand)
                {
                    string work = expression.Substring(parseStart, parseEnd - parseStart);

                    Value cur = null;
                    if (parseValue)
                    {
                        object parsed;

                        if (stripCommas)
                        {
                            if (InternalTryParse(typeof(NumType), work.Replace(",", ""), out parsed))
                            {
                                cur = new Value((NumType)parsed, level);
                            }
                            else
                            {
                                InternalError(ref errorMessage, "Unable to parse number");
                                return false;
                            }
                        }
                        else
                        {
                            if (InternalTryParse(typeof(NumType), work.Replace("_", "-"), out parsed))
                            {
                                cur = new Value((NumType)parsed, level);
                            }
                            else
                            {
                                InternalError(ref errorMessage, "Unable to parse number");
                                return false;
                            }
                        }
                    }
                    else if (parseOperand || parseVariable)
                    {
                        switch (work.ToUpper())
                        {
                            case OperandStr.EqualsStr:
                                cur = new Value(this, ref errorMessage, Operands.Equals, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.NotEquals:
                                cur = new Value(this, ref errorMessage, Operands.NotEquals, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.LessThan:
                                cur = new Value(this, ref errorMessage, Operands.LessThan, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.GreaterThan:
                                cur = new Value(this, ref errorMessage, Operands.GreaterThan, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.LessThanEquals:
                                cur = new Value(this, ref errorMessage, Operands.LessThanEquals, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.GreaterThanEquals:
                                cur = new Value(this, ref errorMessage, Operands.GreaterThanEquals, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.And:
                                cur = new Value(this, ref errorMessage, Operands.And, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Or:
                                cur = new Value(this, ref errorMessage, Operands.Or, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Abs:
                                cur = new Value(this, ref errorMessage, Operands.Abs, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Acos:
                                cur = new Value(this, ref errorMessage, Operands.Acos, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Asin:
                                cur = new Value(this, ref errorMessage, Operands.Asin, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Atan:
                                cur = new Value(this, ref errorMessage, Operands.Atan, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Ceiling:
                                cur = new Value(this, ref errorMessage, Operands.Ceiling, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Cos:
                                cur = new Value(this, ref errorMessage, Operands.Cos, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Cosh:
                                cur = new Value(this, ref errorMessage, Operands.Cosh, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Exp:
                                cur = new Value(this, ref errorMessage, Operands.Exp, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Factorial:
                                cur = new Value(this, ref errorMessage, Operands.Factorial, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Floor:
                                cur = new Value(this, ref errorMessage, Operands.Floor, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Int:
                                cur = new Value(this, ref errorMessage, Operands.Int, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Log:
                                cur = new Value(this, ref errorMessage, Operands.Log, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Pi:
                                cur = new Value(this, ref errorMessage, Operands.Pi, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Random:
                                cur = new Value(this, ref errorMessage, Operands.Random, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Sign:
                                cur = new Value(this, ref errorMessage, Operands.Sign, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Sin:
                                cur = new Value(this, ref errorMessage, Operands.Sin, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Not:
                                cur = new Value(this, ref errorMessage, Operands.Not, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Sinh:
                                cur = new Value(this, ref errorMessage, Operands.Sinh, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Sqrt:
                                cur = new Value(this, ref errorMessage, Operands.Sqrt, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Reciproc:
                                cur = new Value(this, ref errorMessage, Operands.Reciproc, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Sqr:
                                cur = new Value(this, ref errorMessage, Operands.Sqr, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Tan:
                                cur = new Value(this, ref errorMessage, Operands.Tan, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Tanh:
                                cur = new Value(this, ref errorMessage, Operands.Tanh, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Truncate:
                                cur = new Value(this, ref errorMessage, Operands.Truncate, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Add:
                                cur = new Value(this, ref errorMessage, Operands.Add, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Subtract:
                                cur = new Value(this, ref errorMessage, Operands.Subtract, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Divide:
                                cur = new Value(this, ref errorMessage, Operands.Divide, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Modulus:
                                cur = new Value(this, ref errorMessage, Operands.Modulus, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Multiply:
                                cur = new Value(this, ref errorMessage, Operands.Multiply, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.Power:
                                cur = new Value(this, ref errorMessage, Operands.Power, level);
                                if (errorMessage != null)
                                {
                                    return false;
                                }
                                break;
                            case OperandStr.OpenParen:
                                level++;
                                if (level > maxLevel)
                                {
                                    maxLevel = level;
                                }
                                break;
                            case OperandStr.CloseParen:
                                level--;
                                break;

                            default:
                                if (work.Length >= 1)
                                {
                                    cur = new Value(this, ref errorMessage, Operands.Variable, level);

                                    if (errorMessage != null)
                                    {
                                        return false;
                                    }

                                    cur.Variable = work.ToUpper();
                                }
                                else
                                {
                                    InternalError(ref errorMessage, "Unknown variable \"" + work + "\"");
                                    return false;
                                }
                                break;
                        }
                    }

                    if (cur != null)
                    {
                        count++;
                        args.AddLast(cur);
                    }
                }
            }

            return true;
        }

        NumType InternalPow(ref string errorMessage, object num1, object num2)
        {
            if (typeof(NumType) == typeof(double))
            {
                return (NumType)(object)Math.Pow((double)num1, (double)num2);
            }
            else if (typeof(NumType) == typeof(float))
            {
                return (NumType)(object)Math.Pow((double)(float)num1, (double)(float)num2);
            }
            else if (typeof(NumType) == typeof(int))
            {
                return (NumType)(object)Math.Pow((double)(int)num1, (double)(int)num2);
            }
            else if (typeof(NumType) == typeof(long))
            {
                return (NumType)(object)Math.Pow((double)(long)num1, (double)(long)num2);
            }
            else if (typeof(NumType) == typeof(uint))
            {
                return (NumType)(object)Math.Pow((double)(uint)num1, (double)(uint)num2);
            }
            else if (typeof(NumType) == typeof(ulong))
            {
                return (NumType)(object)Math.Pow((double)(ulong)num1, (double)(ulong)num2);
            }
            else if (typeof(NumType) == typeof(decimal))
            {
                return (NumType)(object)Math.Pow((double)(decimal)num1, (double)(decimal)num2);
            }
            else
            {
                MethodInfo mi = typeof(NumType).GetMethod("Pow",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase,
                    null,
                    new Type[] { typeof(NumType), typeof(NumType) },
                    null);

                if (mi == null)
                {
                    return InternalFromDouble(ref errorMessage, Math.Pow(
                        InternalToNumDouble(ref errorMessage, num1),
                        InternalToNumDouble(ref errorMessage, num2)));
                }
                else
                {
                    return (NumType)(object)mi.Invoke(null, new object[] { num1, num2 });
                }
            }
        }

        static bool InternalTryParse(Type type, string value, out object result)
        {
            MethodInfo mi = null;

            mi = type.GetMethod("TryParse", new Type[] { typeof(string), type.MakeByRefType() });

            if (mi != null)
            {
                object[] methodParams = new object[] { value, null };
                bool ret = (bool)mi.Invoke(null, methodParams);
                result = methodParams[1];
                return ret;
            }

            mi = type.GetMethod("Parse", new Type[] { typeof(string) });

            if (mi != null)
            {
                try
                {
                    result = mi.Invoke(null, new object[] { value });
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            mi = type.GetMethod("op_Implicit", new Type[] { typeof(double) });

            if (mi != null)
            {
                double temp = 0;
                if (double.TryParse(value, out temp))
                {
                    try
                    {
                        result = mi.Invoke(null, new object[] { temp });
                        return true;
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
            }

            result = null;
            return false;
        }

        delegate double OperDouble(double value);
        delegate float OperFloat(float value);
        delegate int OperInt(int value);
        delegate long OperLong(long value);
        delegate uint OperUInt(uint value);
        delegate ulong OperULong(ulong value);
        delegate decimal OperDecimal(decimal value);

        NumType InternalOper(ref string errorMessage, NumType value, string oper, OperDouble operDouble, OperFloat operFloat, OperInt operInt, OperLong operLong, OperUInt operUInt, OperULong operULong, OperDecimal operDecimal)
        {
            if (typeof(NumType) == typeof(double) && operDouble != null)
            {
                return (NumType)(object)operDouble((double)(object)value);
            }
            else if (typeof(NumType) == typeof(float) && operFloat != null)
            {
                return (NumType)(object)operFloat((float)(object)value);
            }
            else if (typeof(NumType) == typeof(int) && operInt != null)
            {
                return (NumType)(object)operInt((int)(object)value);
            }
            else if (typeof(NumType) == typeof(long) && operLong != null)
            {
                return (NumType)(object)operLong((long)(object)value);
            }
            else if (typeof(NumType) == typeof(uint) && operUInt != null)
            {
                return (NumType)(object)operUInt((uint)(object)value);
            }
            else if (typeof(NumType) == typeof(ulong) && operULong != null)
            {
                return (NumType)(object)operULong((ulong)(object)value);
            }
            else if (typeof(NumType) == typeof(decimal) && operDecimal != null)
            {
                return (NumType)(object)operDecimal((decimal)(object)value);
            }
            else
            {
                MethodInfo mi = typeof(NumType).GetMethod(oper,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase,
                    null,
                    new Type[] { typeof(NumType) },
                    null);

                if (mi == null)
                {
                    mi = typeof(NumType).GetMethod("get_" + oper,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase,
                        null,
                        new Type[] { },
                        null);

                    if (mi == null)
                    {
                        return InternalFromDouble(ref errorMessage, operDouble(InternalToNumDouble(ref errorMessage, value)));
                    }
                    else
                    {
                        object ret = mi.Invoke(value, new object[] { });

                        if (ret.GetType() == typeof(NumType))
                        {
                            return (NumType)ret;
                        }
                        else
                        {
                            return InternalFromUnknown(ref errorMessage, ret);
                        }
                    }
                }
                else
                {
                    return (NumType)(object)mi.Invoke(null, new object[] { value });
                }
            }
        }

        delegate int OperDoubleInt(double value);
        delegate int OperFloatInt(float value);
        delegate int OperIntInt(int value);
        delegate int OperLongInt(long value);
        delegate int OperUIntInt(uint value);
        delegate int OperULongInt(ulong value);
        delegate int OperDecimalInt(decimal value);

        NumType InternalOperInt(ref string errorMessage, NumType value, string oper, OperDoubleInt operDouble, OperFloatInt operFloat, OperIntInt operInt, OperLongInt operLong, OperUIntInt operUInt, OperULongInt operULong, OperDecimalInt operDecimal)
        {
            if (typeof(NumType) == typeof(double) && operDouble != null)
            {
                return (NumType)(object)(double)operDouble((double)(object)value);
            }
            else if (typeof(NumType) == typeof(float) && operFloat != null)
            {
                return (NumType)(object)(double)operFloat((float)(object)value);
            }
            else if (typeof(NumType) == typeof(int) && operInt != null)
            {
                return (NumType)(object)operInt((int)(object)value);
            }
            else if (typeof(NumType) == typeof(long) && operLong != null)
            {
                return (NumType)(object)operLong((long)(object)value);
            }
            else if (typeof(NumType) == typeof(uint) && operUInt != null)
            {
                return (NumType)(object)operUInt((uint)(object)value);
            }
            else if (typeof(NumType) == typeof(ulong) && operULong != null)
            {
                return (NumType)(object)operULong((ulong)(object)value);
            }
            else if (typeof(NumType) == typeof(decimal) && operDecimal != null)
            {
                return (NumType)(object)operDecimal((decimal)(object)value);
            }
            else
            {
                MethodInfo mi = typeof(NumType).GetMethod(oper,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase,
                    null,
                    new Type[] { typeof(NumType) },
                    null);

                if (mi == null)
                {
                    mi = typeof(NumType).GetMethod("get_" + oper,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase,
                        null,
                        new Type[] { },
                        null);

                    if (mi == null)
                    {
                        return InternalFromDouble(ref errorMessage, (double)(int)operDouble(
                            InternalToNumDouble(ref errorMessage, value)));
                    }
                    else
                    {
                        object ret = mi.Invoke(value, new object[] { });

                        if (ret.GetType() == typeof(NumType))
                        {
                            return (NumType)ret;
                        }
                        else
                        {
                            return InternalFromUnknown(ref errorMessage, ret);
                        }
                    }
                }
                else
                {
                    return (NumType)(object)mi.Invoke(null, new object[] { value });
                }
            }
        }

        object InternalEqu(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;

                if (num1 is double)
                {
                    ret = ((double)num1) == ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) == ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) == ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) == ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) == ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) == ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) == ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Equality", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with equality");
            return null;
        }

        object InternalNeq(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;

                if (num1 is double)
                {
                    ret = ((double)num1) != ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) != ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) != ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) != ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) != ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) != ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) != ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Inequality", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with inequality");
            return null;
        }

        object InternalLT(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;
                if (num1 is double)
                {
                    ret = ((double)num1) < ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) < ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) < ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) < ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) < ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) < ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) < ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_LessThan", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with less than");
            return null;
        }

        object InternalGT(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;
                if (num1 is double)
                {
                    ret = ((double)num1) > ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) > ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) > ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) > ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) > ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) > ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) > ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_GreaterThan", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with greater than");
            return default(NumType);
        }

        object InternalLTE(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;
                if (num1 is double)
                {
                    ret = ((double)num1) <= ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) <= ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) <= ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) <= ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) <= ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) <= ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) <= ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_LessThanOrEqual", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with less than or equal");
            return null;
        }

        object InternalGTE(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                bool ret = false;
                if (num1 is double)
                {
                    ret = ((double)num1) >= ((double)num2);
                }
                else if (num1 is float)
                {
                    ret = ((float)num1) >= ((float)num2);
                }
                else if (num1 is int)
                {
                    ret = ((int)num1) >= ((int)num2);
                }
                else if (num1 is long)
                {
                    ret = ((long)num1) >= ((long)num2);
                }
                else if (num1 is uint)
                {
                    ret = ((uint)num1) >= ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    ret = ((ulong)num1) >= ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    ret = ((decimal)num1) >= ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_GreaterThanOrEqual", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        ret = (bool)mi.Invoke(null, new object[] { num1, num2 });
                    }
                }

                if (ret)
                {
                    return InternalFromDouble(ref errorMessage, 1);
                }
                else
                {
                    return InternalFromDouble(ref errorMessage, 0);
                }
            }

            InternalError(ref errorMessage, "Error with greater than or equal");
            return null;
        }

        object InternalAnd(ref string errorMessage, object num1, object num2)
        {
            num1 = InternalNeq(ref errorMessage, num1, InternalFromDouble(ref errorMessage, 0));
            if (errorMessage != null)
            {
                return null;
            }
            num2 = InternalNeq(ref errorMessage, num2, InternalFromDouble(ref errorMessage, 0));
            if (errorMessage != null)
            {
                return null;
            }

            if (InternalToNumDouble(ref errorMessage, num1) == 1 &&
                InternalToNumDouble(ref errorMessage, num2) == 1)
            {
                return InternalFromDouble(ref errorMessage, 1);
            }
            else
            {
                return InternalFromDouble(ref errorMessage, 0);
            }
        }

        object InternalOr(ref string errorMessage, object num1, object num2)
        {
            num1 = InternalNeq(ref errorMessage, num1, InternalFromDouble(ref errorMessage, 0));
            if (errorMessage != null)
            {
                return null;
            }
            num2 = InternalNeq(ref errorMessage, num2, InternalFromDouble(ref errorMessage, 0));
            if (errorMessage != null)
            {
                return null;
            }

            if (InternalToNumDouble(ref errorMessage, num1) == 1 ||
                InternalToNumDouble(ref errorMessage, num2) == 1)
            {
                return InternalFromDouble(ref errorMessage, 1);
            }
            else
            {
                return InternalFromDouble(ref errorMessage, 0);
            }
        }

        object InternalAdd(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                if (num1 is double)
                {
                    return ((double)num1) + ((double)num2);
                }
                else if (num1 is float)
                {
                    return ((float)num1) + ((float)num2);
                }
                else if (num1 is int)
                {
                    return ((int)num1) + ((int)num2);
                }
                else if (num1 is long)
                {
                    return ((long)num1) + ((long)num2);
                }
                else if (num1 is uint)
                {
                    return ((uint)num1) + ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    return ((ulong)num1) + ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    return ((decimal)num1) + ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Addition", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        return mi.Invoke(null, new object[] { num1, num2 });
                    }
                }
            }

            InternalError(ref errorMessage, "Error with addition");
            return null;
        }

        object InternalSub(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                if (num1 is double)
                {
                    return ((double)num1) - ((double)num2);
                }
                else if (num1 is float)
                {
                    return ((float)num1) - ((float)num2);
                }
                else if (num1 is int)
                {
                    return ((int)num1) - ((int)num2);
                }
                else if (num1 is long)
                {
                    return ((long)num1) - ((long)num2);
                }
                else if (num1 is uint)
                {
                    return ((uint)num1) - ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    return ((ulong)num1) - ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    return ((decimal)num1) - ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Subtraction", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        return mi.Invoke(null, new object[] { num1, num2 });
                    }
                }
            }

            InternalError(ref errorMessage, "Error with subtraction");
            return null;
        }

        object InternalMul(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                if (num1 is double)
                {
                    return ((double)num1) * ((double)num2);
                }
                else if (num1 is float)
                {
                    return ((float)num1) * ((float)num2);
                }
                else if (num1 is int)
                {
                    return ((int)num1) * ((int)num2);
                }
                else if (num1 is long)
                {
                    return ((long)num1) * ((long)num2);
                }
                else if (num1 is uint)
                {
                    return ((uint)num1) * ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    return ((ulong)num1) * ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    return ((decimal)num1) * ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Multiply", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        return mi.Invoke(null, new object[] { num1, num2 });
                    }
                }
            }

            InternalError(ref errorMessage, "Error with multiply");
            return null;
        }

        object InternalDiv(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                if (num1 is double)
                {
                    return ((double)num1) / ((double)num2);
                }
                else if (num1 is float)
                {
                    return ((float)num1) / ((float)num2);
                }
                else if (num1 is int)
                {
                    return ((int)num1) / ((int)num2);
                }
                else if (num1 is long)
                {
                    return ((long)num1) / ((long)num2);
                }
                else if (num1 is uint)
                {
                    return ((uint)num1) / ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    return ((ulong)num1) / ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    return ((decimal)num1) / ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Division", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        return mi.Invoke(null, new object[] { num1, num2 });
                    }
                }
            }

            InternalError(ref errorMessage, "Error with division");
            return null;
        }

        object InternalMod(ref string errorMessage, object num1, object num2)
        {
            if (num1.GetType() == num2.GetType())
            {
                if (num1 is double)
                {
                    return ((double)num1) % ((double)num2);
                }
                else if (num1 is float)
                {
                    return ((float)num1) % ((float)num2);
                }
                else if (num1 is int)
                {
                    return ((int)num1) % ((int)num2);
                }
                else if (num1 is long)
                {
                    return ((long)num1) % ((long)num2);
                }
                else if (num1 is uint)
                {
                    return ((uint)num1) % ((uint)num2);
                }
                else if (num1 is ulong)
                {
                    return ((ulong)num1) % ((ulong)num2);
                }
                else if (num1 is decimal)
                {
                    return ((decimal)num1) % ((decimal)num2);
                }
                else
                {
                    MethodInfo mi = num1.GetType().GetMethod("op_Modulus", new Type[] { num1.GetType(), num2.GetType() });
                    if (mi != null)
                    {
                        return mi.Invoke(null, new object[] { num1, num2 });
                    }
                }
            }

            InternalError(ref errorMessage, "Error with modulus");
            return null;
        }

        double InternalToNumDouble(ref string errorMessage, object value)
        {
            if (value is double)
            {
                return (double)value;
            }
            else if (value is float)
            {
                return (double)((float)value);
            }
            else if (value is int)
            {
                return (double)((int)value);
            }
            else if (value is long)
            {
                return (double)((long)value);
            }
            else if (value is uint)
            {
                return (double)((uint)value);
            }
            else if (value is ulong)
            {
                return (double)((ulong)value);
            }
            else if (value is decimal)
            {
                return (double)((decimal)value);
            }
            else
            {
                MethodInfo mi = value.GetType().GetMethod("get_DoubleValue", new Type[] { });
                if (mi != null)
                {
                    return (double)mi.Invoke(value, null);
                }
            }

            InternalError(ref errorMessage, "Error with conversion to double");
            return 0;
        }

        NumType InternalFromDouble(ref string errorMessage, double value)
        {
            if (typeof(NumType) == typeof(double))
            {
                return (NumType)(object)value;
            }
            else if (typeof(NumType) == typeof(float))
            {
                return (NumType)(object)(float)value;
            }
            else if (typeof(NumType) == typeof(int))
            {
                return (NumType)(object)(int)value;
            }
            else if (typeof(NumType) == typeof(long))
            {
                return (NumType)(object)(long)value;
            }
            else if (typeof(NumType) == typeof(uint))
            {
                return (NumType)(object)(uint)value;
            }
            else if (typeof(NumType) == typeof(ulong))
            {
                return (NumType)(object)(ulong)value;
            }
            else if (typeof(NumType) == typeof(decimal))
            {
                return (NumType)(object)(decimal)value;
            }
            else
            {
                MethodInfo mi = typeof(NumType).GetMethod("op_Implicit", new Type[] { typeof(double) });
                if (mi != null)
                {
                    return (NumType)(object)mi.Invoke(null, new object[] { value });
                }
            }

            InternalError(ref errorMessage, "Error with implicit conversion");
            return default(NumType);
        }

        NumType InternalFromUnknown(ref string errorMessage, object value)
        {
            MethodInfo mi = typeof(NumType).GetMethod("op_Implicit", new Type[] { value.GetType() });

            if (mi != null)
            {
                return (NumType)(object)mi.Invoke(null, new object[] { value });
            }

            InternalError(ref errorMessage, "Error with implicit conversion");
            return default(NumType);
        }
    }

    #region Test Cases
#if EQUATION_TESTER
    static class EquationTester
    {
        static void DNScriptEntryPoint()
        {
            RunTest();
        }

        static void Verify(string expr, double expected, ref int errors, StringBuilder sb)
        {
            try
            {
                Equation<double> eq = new Equation<double>();
                eq.SeedRandom(1);
                double result = eq.Evaluate(expr);

                VerifyCompare<double>(expr, expected, ref errors, sb, result);
            }
            catch (Exception ex)
            {
                errors++;
                sb.Append("ERROR: ");
                sb.Append(expr);
                sb.Append(" triggered ");
                sb.Append(ex.Message);
            }

            sb.AppendLine("");
        }

        static void VerifyOther<T>(string expr, T expected, ref int errors, StringBuilder sb)
        {
            try
            {
                sb.Append(typeof(T).Name);
                sb.Append(": ");

                Equation<T> eq = new Equation<T>();
                T result = eq.Evaluate(expr);

                VerifyCompare<T>(expr, expected, ref errors, sb, result);
            }
            catch (Exception ex)
            {
                errors++;
                sb.Append("ERROR: ");
                sb.Append(expr);
                sb.Append(" triggered ");
                sb.Append(ex.Message);
            }

            sb.AppendLine("");
        }

        static void VerifyVar(string expr, double expected, ref int errors, StringBuilder sb)
        {
            try
            {
                Equation<double> eq = new Equation<double>();
                double result = eq.Evaluate(expr, 2, 3, 4);

                VerifyCompare<double>(expr, expected, ref errors, sb, result);
            }
            catch (Exception ex)
            {
                errors++;
                sb.Append("ERROR: ");
                sb.Append(expr);
                sb.Append(" triggered ");
                sb.Append(ex.Message);
            }

            sb.AppendLine("");
        }

        static void VerifyCallback(string expr, double expected, ref int errors, StringBuilder sb)
        {
            try
            {
                Equation<double> eq = new Equation<double>();
                string parsed = eq.Evaluate(expr, delegate(string part)
                {
                    switch (part.Trim().ToLower())
                    {
                        case "one":
                            return "1";
                        case "two":
                            return "2";
                        case "three":
                            return "3";
                        case "four":
                            return "4";
                    }
                    return part;
                });

                double result = eq.Evaluate(parsed);

                VerifyCompare<double>(expr, expected, ref errors, sb, result);
            }
            catch (Exception ex)
            {
                errors++;
                sb.Append("ERROR: ");
                sb.Append(expr);
                sb.Append(" triggered ");
                sb.Append(ex.Message);
            }

            sb.AppendLine("");
        }

        static void VerifyCompare<T>(string expr, T expected, ref int errors, StringBuilder sb, T result)
        {
            bool same = false;

            if (expected is double)
            {
                if (double.IsNaN((double)(object)result) && double.IsNaN((double)(object)expected))
                {
                    same = true;
                }
                else if (double.IsInfinity((double)(object)result) && double.IsInfinity((double)(object)expected))
                {
                    same = true;
                }
                else if (!double.IsNaN((double)(object)result) && !double.IsNaN((double)(object)expected) &&
                        !double.IsInfinity((double)(object)result) && !double.IsInfinity((double)(object)expected))
                {
                    if (Math.Abs((double)(object)result - (double)(object)result) < 0.00001)
                    {
                        same = true;
                    }
                }
            }
            else if (expected is float)
            {
                if ((float)(object)expected == (float)(object)result)
                {
                    same = true;
                }
            }
            else if (expected is int)
            {
                if ((int)(object)expected == (int)(object)result)
                {
                    same = true;
                }
            }
            else if (expected is uint)
            {
                if ((uint)(object)expected == (uint)(object)result)
                {
                    same = true;
                }
            }
            else if (expected is long)
            {
                if ((long)(object)expected == (long)(object)result)
                {
                    same = true;
                }
            }
            else if (expected is ulong)
            {
                if ((ulong)(object)expected == (ulong)(object)result)
                {
                    same = true;
                }
            }
            else if (expected is decimal)
            {
                if ((decimal)(object)expected == (decimal)(object)result)
                {
                    same = true;
                }
            }

            if (!same)
            {
                sb.Append("ERROR: ");
                errors++;

                sb.Append(expr);
                sb.Append(" = ");
                sb.Append(result.ToString());

                sb.Append(" (Expected: ");
                sb.Append(expected.ToString());
                sb.Append(")");
            }
            else
            {
                sb.Append(expr);
                sb.Append(" = ");
                sb.Append(result.ToString());
            }
        }

        public static void RunTest()
        {
            StringBuilder sb = new StringBuilder();
            int errors = 0;
            sb.AppendLine("  Running tests:");

            Verify("123", 123, ref errors, sb);
            Verify("1 / 0", double.PositiveInfinity, ref errors, sb);
            Verify("5 * 6", 30, ref errors, sb);
            Verify("12 / 3", 4, ref errors, sb);
            Verify("52 + 12", 64, ref errors, sb);
            Verify("90 - 30", 60, ref errors, sb);
            Verify("3 ^ 4", 81, ref errors, sb);
            VerifyVar("A + B + C", 9, ref errors, sb);
            Verify("Abs(5 - 10)", 5, ref errors, sb);
            Verify("Acos(110.5)", Math.Acos(110.5), ref errors, sb);
            Verify("Asin(0.5)", Math.Asin(0.5), ref errors, sb);
            Verify("Atan(12)", Math.Atan(12), ref errors, sb);
            Verify("Ceiling(13 / 2)", 7, ref errors, sb);
            Verify("Cos(12)", Math.Cos(12), ref errors, sb);
            Verify("Cosh(12)", Math.Cosh(12), ref errors, sb);
            Verify("Exp(12)", Math.Exp(12), ref errors, sb);
            Verify("Floor(13 / 2)", 6, ref errors, sb);
            Verify("Int(13 / 2)", 6, ref errors, sb);
            Verify("Log(12)", Math.Log(12), ref errors, sb);
            Verify("Sign(12)", 1, ref errors, sb);
            Verify("Sin(12)", Math.Sin(12), ref errors, sb);
            Verify("Sinh(12)", Math.Sinh(12), ref errors, sb);
            Verify("Sqrt(12)", Math.Sqrt(12), ref errors, sb);
            Verify("Tan(12)", Math.Tan(12), ref errors, sb);
            Verify("Tanh(12)", Math.Tanh(12), ref errors, sb);
            Verify("Truncate(13 / 2)", 6, ref errors, sb);
            Verify("56 % 10", 6, ref errors, sb);
            Verify("5!", 120, ref errors, sb);
            Verify("INT(RAND * 10)", 2, ref errors, sb);
            Verify("PI", 3.141592653589, ref errors, sb);
            Verify("12 > 34", 0, ref errors, sb);
            Verify("12 < 34", 1, ref errors, sb);
            Verify("12 >= 34", 0, ref errors, sb);
            Verify("12 <= 34", 1, ref errors, sb);
            Verify("12 = 34", 0, ref errors, sb);
            Verify("12 != 34", 1, ref errors, sb);
            Verify("1 & 0", 0, ref errors, sb);
            Verify("1 | 0", 1, ref errors, sb);
            Verify("2 + 3 * 4", 16, ref errors, sb);
            Verify("(2 + 3) * 4", 20, ref errors, sb);
            Verify("Reciproc(.25)", 4, ref errors, sb);
            VerifyOther<int>("6! + 1 / 3", 720, ref errors, sb);
            VerifyOther<uint>("6! + 1 / 3", 720, ref errors, sb);
            VerifyOther<long>("6! + 1 / 3", 720, ref errors, sb);
            VerifyOther<ulong>("6! + 1 / 3", 720, ref errors, sb);
            VerifyOther<float>("6! + 1 / 3", 720.3333333f, ref errors, sb);
            VerifyOther<decimal>("26! + 1 / 3", 403291461126606000000000000.33m, ref errors, sb);
            VerifyCallback("(two + three) * four", 20, ref errors, sb);

            sb.AppendLine();

            if (errors == 0)
            {
                sb.AppendLine("Everything checks out.");
            }
            else
            {
                sb.AppendLine("There were errors!");
            }

            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            form.Text = "Equation Eval Test";
            form.Size = new System.Drawing.Size(500, 300);

            System.Windows.Forms.TextBox text = new System.Windows.Forms.TextBox();
            text.Text = sb.ToString();
            text.Multiline = true;
            text.WordWrap = false;
            text.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            text.Dock = System.Windows.Forms.DockStyle.Fill;
            text.Font = new System.Drawing.Font(
                "Courier New",
                9.75F,
                System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point,
                0);
            text.ReadOnly = true;

            form.Controls.Add(text);

            form.Load += new EventHandler(delegate(object sender, EventArgs e)
            {
                ((System.Windows.Forms.Form)sender).BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate()
                {
                    text.Select(text.Text.Length, 0);
                    text.ScrollToCaret();
                }));
            });

            if (System.Windows.Forms.Application.MessageLoop)
            {
                form.ShowDialog();
            }
            else
            {
                System.Windows.Forms.Application.Run(form);
            }
        }
    }
#endif
    #endregion
}
