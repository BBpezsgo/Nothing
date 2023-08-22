using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Debug = UnityEngine.Debug;

namespace ProgrammingLanguage.Css
{
    public class DeclarationsContainer : IReadOnlyCollection<Declaration>, IReadOnlyList<Declaration>
    {
        readonly Declaration[] Declarations;
        public int Count => Declarations.Length;

        public static DeclarationsContainer Empty => new(new Declaration[0]);

        public Declaration this[int index] => Declarations[index];
        public Declaration this[Index index] => Declarations[index];
        public Value[] this[string property]
        {
            get
            {
                foreach (Declaration item in Declarations)
                {
                    if (!string.Equals(item.property, property, StringComparison.InvariantCulture)) continue;
                    return item.values;
                }
                return new Value[0];
            }
        }

        public DeclarationsContainer(IEnumerable<Declaration> declarations)
        {
            if (declarations is null) throw new ArgumentNullException(nameof(declarations));

            Declarations = declarations.ToArray();
        }

        public IEnumerator<Declaration> GetEnumerator() => Declarations.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Declarations.GetEnumerator();

        public Declaration[] GetDeclarations(params string[] properties)
        {
            List<Declaration> result = new();
            foreach (Declaration declaration in Declarations)
            {
                foreach (string property in properties)
                {
                    if (string.Equals(property, declaration.property, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Add(declaration);
                        break;
                    }
                }
            }
            return result.ToArray();
        }

        public Sides<Value> GetSides(string baseProperty)
        {
            Sides<Value> result = this[baseProperty].GetSides();

            result.Top = this.GetValue(baseProperty + "-top") ?? default;
            result.Left = this.GetValue(baseProperty + "-left") ?? default;
            result.Bottom = this.GetValue(baseProperty + "-bottom") ?? default;
            result.Right = this.GetValue(baseProperty + "-right") ?? default;

            return result;
        }

        public SidesInt GetSidesPx(string baseProperty)
        {
            SidesInt result = this[baseProperty].GetSidesPx();

            Value valueTop = this.GetValue(baseProperty + "-top") ?? default;
            Value valueLeft = this.GetValue(baseProperty + "-left") ?? default;
            Value valueBottom = this.GetValue(baseProperty + "-bottom") ?? default;
            Value valueRight = this.GetValue(baseProperty + "-right") ?? default;

            if (valueTop.type == Value.Type.Number && valueTop.number.Value.Unit == Unit.Pixels)
            {
                result.Top = valueTop.number.Value.Int;
            }

            if (valueLeft.type == Value.Type.Number && valueLeft.number.Value.Unit == Unit.Pixels)
            {
                result.Left = valueLeft.number.Value.Int;
            }

            if (valueBottom.type == Value.Type.Number && valueBottom.number.Value.Unit == Unit.Pixels)
            {
                result.Bottom = valueBottom.number.Value.Int;
            }

            if (valueRight.type == Value.Type.Number && valueRight.number.Value.Unit == Unit.Pixels)
            {
                result.Right = valueRight.number.Value.Int;
            }

            return result;
        }

        public Value? GetValue(string property)
        {
            Value[] values = this[property];
            if (values.Length == 0) return null;
            return values[0];
        }
        public bool TryGetValue(string property, out Value value)
        {
            Value[] values = this[property];
            if (values.Length == 0)
            {
                value = default;
                return false;
            }

            value = values[0];
            return true;
        }

        public string GetString(string property) => TryGetString(property, out string result) ? result : null;
        public bool TryGetString(string property, out string value)
        {
            Value[] values = this[property];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].type == Value.Type.Other)
                {
                    value = values[i].other;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public Number? GetNumber(string property) => TryGetNumber(property, out Number result) ? result : null;
        public bool TryGetNumber(string property, out Number number)
        {
            Value[] values = this[property];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].type == Value.Type.Number)
                {
                    number = values[i].number.Value;
                    return true;
                }
            }

            number = default;
            return false;
        }

        public Color? GetColor(string property) => TryGetColor(property, out Color result) ? result : null;
        public bool TryGetColor(string property, out Color color)
        {
            Value[] values = this[property];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].type == Value.Type.Color)
                {
                    color = values[i].color.Value;
                    return true;
                }
            }
            color = default;
            return false;
        }
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public struct Stylesheet
    {
        public List<Rule> rules;

        internal void RemoveUnsupportedProperties(string[] supportedProperties)
        {
            foreach (Rule rule in rules)
            { rule.RemoveUnsupportedProperties(supportedProperties); }
        }

        public override readonly string ToString() => $"Stylesheet {{{((rules?.Count) > 0 ? " ... " : " ")}}}";
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public class Rule
    {
        public Selector selector;
        public DeclarationsContainer declarations;

        public override string ToString() => $"{selector} {{{((declarations?.Count ?? 0) > 0 ? " ... " : " ")}}}";

        public bool TryGetProperty(string property, out Value[] values)
        {
            for (int i = 0; i < declarations.Count; i++)
            {
                if (declarations[i].property != property) continue;
                values = declarations[i].values;
                return true;
            }

            values = null;
            return false;
        }

        internal void RemoveUnsupportedProperties(string[] supportedProperties)
        {
            List<Declaration> newDeclarations = new();
            for (int i = this.declarations.Count - 1; i >= 0; i--)
            {
                if (supportedProperties.Contains(this.declarations[i].property))
                {
                    newDeclarations.Add(this.declarations[i]);
                }
                else
                {
                    Debug.Log($"[CSS]: Unsupported property \"{this.declarations[i].property}\"");
                }
            }
            this.declarations = new DeclarationsContainer(newDeclarations);
        }
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public readonly struct Selector
    {
        public readonly SimpleSelector[] Simple;
        public readonly char[] Combinators;

        public static Selector Empty => new(new SimpleSelector[0], new char[0]);

        public Selector(IEnumerable<SimpleSelector> simple, IEnumerable<char> combinators)
        {
            Simple = simple.ToArray();
            Combinators = combinators.ToArray();
        }

        public override readonly string ToString()
        {
            string result = "";

            bool notFirstOne = false;

            for (int i = 0; i < Simple.Length; i++)
            {
                if (notFirstOne) result += ", ";
                result += Simple[i].ToString();
                notFirstOne = true;
            }

            for (int i = 0; i < Combinators.Length; i++)
            {
                if (notFirstOne) result += ", ";
                result += Combinators[i].ToString();
                notFirstOne = true;
            }

            return result;
        }
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public readonly struct SimpleSelector
    {
        public enum Type
        {
            Unknown,
            TagName,
            ID,
            Class,
        }

        public readonly Type type;
        public readonly string tagName;
        public readonly string id;
        public readonly string @class;

        public readonly string elementState;

        public SimpleSelector(Type type, string value, string elementState) : this()
        {
            this.type = type;

            tagName = null;
            id = null;
            @class = null;

            switch (type)
            {
                case Type.TagName:
                    tagName = value;
                    break;
                case Type.ID:
                    id = value;
                    break;
                case Type.Class:
                    @class = value;
                    break;
                case Type.Unknown:
                default:
                    break;
            }

            this.elementState = elementState;
        }

        public override readonly string ToString()
        {
            string result = type switch
            {
                Type.TagName => tagName,
                Type.ID => $"#{id}",
                Type.Class => $".{@class}",
                _ => "???",
            };
            if (!string.IsNullOrEmpty(elementState))
            { result += $":{elementState}"; }
            return result;
        }
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public struct Declaration
    {
        public string property;
        public Value[] values;

        public override readonly string ToString() => $"{property}: {string.Join(' ', values)};";
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public readonly struct Value : IEquatable<Value>
    {
        public enum Type
        {
            Undefined,
            Color,
            Number,
            Other,
        }

        public readonly Color? color;
        public readonly Number? number;
        public readonly string other;

        public readonly Type type;

        public readonly Number NumberOrZero => IsNumber ? number.Value : new Number(0, Unit.Pixels);

        public bool IsColor => type == Type.Color;
        public bool IsNumber => type == Type.Number;
        public bool IsOther => type == Type.Other;

        public Value(Color color)
        {
            this.color = color;
            this.number = null;
            this.other = null;

            this.type = Type.Color;
        }

        public Value(Number number)
        {
            this.color = null;
            this.number = number;
            this.other = null;

            this.type = Type.Number;
        }

        /// <exception cref="ArgumentNullException"/>
        public Value(string other)
        {
            this.color = null;
            this.number = null;
            this.other = other ?? throw new ArgumentNullException(nameof(other));

            this.type = Type.Other;
        }

        public override readonly string ToString() => type switch
        {
            Type.Color => color.Value.ToString(),
            Type.Number => number.Value.ToString(),
            Type.Other => other,
            Type.Undefined => "undefined",
            _ => throw new NotImplementedException(),
        };

        public override readonly bool Equals(object obj) => obj is Value other && this.Equals(other);

        public readonly bool Equals(Value other) =>
            this.type == other.type &&
            this.color.Value == other.color.Value &&
            this.number.Value == other.number.Value &&
            this.other == other.other;

        public override readonly int GetHashCode() => type switch
        {
            Type.Color => HashCode.Combine(type, color.Value),
            Type.Number => HashCode.Combine(type, number.Value),
            Type.Other => HashCode.Combine(type, other),
            _ => HashCode.Combine(type),
        };

        public static bool operator ==(Value a, string b) => a.type == Type.Other && a.other == b;
        public static bool operator !=(Value a, string b) => !(a == b);

        public static bool operator ==(Value a, int b) => a.type == Type.Number && a.number.Value == b;
        public static bool operator !=(Value a, int b) => !(a == b);

        public static bool operator ==(Value a, float b) => a.type == Type.Number && a.number.Value == b;
        public static bool operator !=(Value a, float b) => !(a == b);

        public static bool operator ==(Value a, Color b) => a.type == Type.Color && a.color.Value == b;
        public static bool operator !=(Value a, Color b) => !(a == b);
    }

    public enum Unit
    {
        None,
        Unknown,
        /// <summary>
        /// <b>CSS:</b> "px"
        /// <br/>
        /// <br/>
        /// </summary>
        Pixels,
        /// <summary>
        /// <b>CSS:</b> "pct" or "%"
        /// <br/>
        /// <br/>
        /// </summary>
        Percentage,
        /// <summary>
        /// <b>CSS:</b> "em"
        /// <br/>
        /// <br/>
        /// Font size of the parent, in the case of typographical properties like <c>font-size</c>, and font size of the element itself, in the case of other properties like <c>width</c>.
        /// </summary>
        Em,
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public readonly struct Number : IEquatable<Number>
    {
        public readonly float Value;
        public readonly float Percentage => Value / 100f;
        public readonly int Int => (int)Math.Round(Value);
        public readonly Unit Unit;

        public Number(float value, Unit unit)
        {
            Value = value;
            Unit = unit;
        }

        public Number(int value, Unit unit)
        {
            Value = value;
            Unit = unit;
        }

        public override readonly string ToString() => $"{Value}{((Unit == Unit.None) ? "" : Unit.ToString().ToLower())}";

        public override readonly bool Equals(object obj) => obj is Number other && this.Equals(other);

        public readonly bool Equals(Number other) =>
            this.Value == other.Value &&
            this.Unit == other.Unit;

        public override readonly int GetHashCode() => HashCode.Combine(Value, Unit);

        public static bool operator ==(Number a, Number b) => a.Equals(b);
        public static bool operator !=(Number a, Number b) => !a.Equals(b);

        public static bool operator ==(Number a, int b) => a.Value == b;
        public static bool operator !=(Number a, int b) => a.Value != b;

        public static bool operator ==(Number a, float b) => a.Value == b;
        public static bool operator !=(Number a, float b) => a.Value != b;
    }

    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public readonly struct Color : IEquatable<Color>
    {
        public readonly byte R, G, B, A;

        public Color(byte r, byte g, byte b) : this((int)r, (int)g, (int)b, 255) { }
        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b) : this(r, g, b, 255) { }
        public Color(int r, int g, int b, int a)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }

        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);

        public override readonly bool Equals(object obj) => obj is Color other && this.Equals(other);

        public readonly bool Equals(Color other) =>
            this.R == other.R &&
            this.G == other.G &&
            this.B == other.B &&
            this.A == other.A;

        public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

        public override readonly string ToString() => (A == 255) ? $"rgb({R}, {G}, {B})" : $"rgba({R}, {G}, {B}, {A})";

        public static implicit operator UnityEngine.Color(Color v)
            => new(v.R / 255f, v.G / 255f, v.B / 255f, v.A / 255f);

        public static implicit operator System.Drawing.Color(Color v)
            => System.Drawing.Color.FromArgb(v.A, v.R, v.G, v.B);

        public static implicit operator Color(UnityEngine.Color v)
            => new(
                (byte)Math.Clamp(Math.Round(v.r * 255f), byte.MinValue, byte.MaxValue),
                (byte)Math.Clamp(Math.Round(v.g * 255f), byte.MinValue, byte.MaxValue),
                (byte)Math.Clamp(Math.Round(v.b * 255f), byte.MinValue, byte.MaxValue),
                (byte)Math.Clamp(Math.Round(v.a * 255f), byte.MinValue, byte.MaxValue));

        public static implicit operator Color(System.Drawing.Color v)
            => new(v.A, v.R, v.G, v.B);

        public static Color operator *(Color a, int b)
            => new(a.R * b, a.G * b, a.B * b, a.A * b);

        public static Color operator /(Color a, int b)
            => new(a.R / b, a.G / b, a.B / b, a.A / b);
    }

    public static class Extensions
    {
        public static void AddOrOverride(this List<Declaration> self, IEnumerable<Declaration> declarations)
        {
            foreach (Declaration declaration in declarations)
            {
                bool shouldAdd = true;
                int n = self.Count;
                for (int i = 0; i < n; i++)
                {
                    if (self[i].property != declaration.property) continue;
                    shouldAdd = false;

                    self[i] = new Declaration()
                    {
                        property = declaration.property,
                        values = new List<Value>(declaration.values).ToArray()
                    };
                }

                if (shouldAdd)
                { self.Add(declaration); }
            }
        }
    }

    static class PrivateExtensinos
    {
        public static bool Contains(this string value, params char[] values)
        {
            foreach (var item in values)
            {
                if (value.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Contains(this string value, params string[] values)
        {
            foreach (var item in values)
            {
                if (value.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Parser
    {
        string text;
        int i = 0;

        bool Have => i < text.Length;
        char Current => text[i];

        public Stylesheet Parse(string program)
        {
            this.text = program.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            this.i = 0;

            Stylesheet stylesheet = new()
            {
                rules = new()
            };

            while (Have)
            {
                Selector selector = ParseSelectors();
                Declaration[] declarations = ParseDeclarations();

                if (selector.Simple == null || selector.Combinators == null)
                { continue; }
                if (selector.Simple.Length == 0 && selector.Combinators.Length == 0)
                { continue; }
                if (declarations.Length == 0)
                { continue; }
                stylesheet.rules.Add(new Rule()
                {
                    selector = selector,
                    declarations = new DeclarationsContainer(declarations),
                });
            }

            return stylesheet;
        }

        #region Raw Methods

        bool SkipComment()
        {
            string prevChars = "";
            int startI = i;
            while (Have)
            {
                if (prevChars == "/*")
                {
                    ConsumeUntil("*/", false);
                    return true;
                }

                if (prevChars.Length > 1)
                {
                    i = startI;
                    return false;
                }

                prevChars += Current;
                i++;
            }
            return false;
        }

        void SkipComments()
        {
            int endlessSafe = 0;
            while (SkipComment())
            {
                if (endlessSafe++ > 50) throw new EndlessLoopException();
            }
        }

        void SkipWhitespace()
        {
            int endlessSafe = 0;
            while (Have)
            {
                if (endlessSafe++ > 200) throw new EndlessLoopException();

                if (Current == '\r' || Current == '\n' || Current == '\t' || Current == ' ')
                { i++; }
                else
                { break; }
            }
        }

        string ConsumeInBetween(char startCharacter, char stopCharacter, bool skipComment = true)
        {
            if (skipComment)
            {
                SkipComments();
                SkipWhitespace();
                SkipComments();
            }
            else
            {
                SkipWhitespace();
            }

            if (!Have)
            { return null; }

            if (Current != startCharacter)
            { return null; }

            i++;

            StringBuilder result = new();

            int endlessSafe = 0;
            int depth = 0;

            while (Have)
            {
                if (endlessSafe++ > 1000) throw new EndlessLoopException();

                if (Current == stopCharacter)
                {
                    if (depth == 0)
                    { i++; break; }
                    depth--;
                }

                if (Current == startCharacter)
                {
                    depth++;
                }

                result.Append(Current);
                i++;
                if (skipComment) SkipComments();
            }

            return result.ToString();
        }

        string ConsumeUntil(char stopBeforeThis, bool skipComment = true)
        {
            StringBuilder result = new();
            while (Have)
            {
                if (Current == stopBeforeThis)
                { break; }

                result.Append(Current);
                i++;

                if (skipComment) SkipComments();
            }
            return result.ToString();
        }

        string ConsumeUntil(string stopAfterThis, bool skipComment = true)
        {
            string result = "";
            while (Have)
            {
                if (result.EndsWith(stopAfterThis))
                { break; }

                result += Current;
                i++;

                if (skipComment) SkipComments();
            }
            return result;
        }

        #endregion

        #region Parse Methods

        Selector ParseSelectors()
        {
            SkipComments();
            string selectorRaw = ConsumeUntil('{').Trim();

            string[] selectorsRaw;
            if (selectorRaw.Contains(','))
            { selectorsRaw = selectorRaw.Split(','); }
            else
            { selectorsRaw = new string[1] { selectorRaw }; }

            List<char> combinators = new();
            List<SimpleSelector> simple = new();

            foreach (var s in selectorsRaw)
            {
                if (s.Length == 0) continue;
                var selector = s.Trim();
                if (selector.Length == 0) continue;

                if (selector == "*")
                {
                    combinators.Add('*');
                }
                else if (selector.StartsWith('#'))
                {
                    selector = selector[1..];
                    if (selector.Contains('@', '#', '.'))
                    {
                        Debug.Log("[CSS/Parser]: Complex selector not supported");
                        continue;
                    }
                    string state = string.Empty;
                    if (selector.Contains(':'))
                    {
                        state = selector.Split(':')[1];
                        selector = selector.Split(':')[0];
                    }
                    simple.Add(new SimpleSelector(SimpleSelector.Type.ID, selector, state));
                }
                else if (selector.StartsWith('.'))
                {
                    selector = selector[1..];
                    if (selector.Contains('@', '#', '.'))
                    {
                        Debug.Log($"[CSS/Parser]: Complex selector not supported");
                        continue;
                    }
                    string state = string.Empty;
                    if (selector.Contains(':'))
                    {
                        state = selector.Split(':')[1];
                        selector = selector.Split(':')[0];
                    }
                    simple.Add(new SimpleSelector(SimpleSelector.Type.Class, selector, state));
                }
                else if (selector.StartsWith('@'))
                {
                    Debug.Log("[CSS/Parser]: Advanced selector not supported");
                    return new Selector();
                }
                else
                {
                    if (selector.Contains('@', '#', '.'))
                    { Debug.Log("[CSS/Parser]: Complex selector not supported"); continue; }
                    string state = string.Empty;
                    if (selector.Contains(':'))
                    {
                        state = selector.Split(':')[1];
                        selector = selector.Split(':')[0];
                    }
                    simple.Add(new SimpleSelector(SimpleSelector.Type.TagName, selector, state));
                }
            }

            return new Selector(simple, combinators);
        }

        Declaration[] ParseDeclarations()
        {
            SkipComments();
            string contentRaw = ConsumeInBetween('{', '}');

            if (string.IsNullOrWhiteSpace(contentRaw))
            {
                Debug.Log("[CSS/Parser]: Empty rule");
                return new Declaration[] { };
            }

            if (contentRaw.Contains('{'))
            {
                Debug.LogWarning("[CSS/Parser]: Declaration segment must not have character '{'");
                return new Declaration[] { };
            }

            string[] declarationsRaw;
            if (contentRaw.Contains(';'))
            {
                declarationsRaw = contentRaw.Split(';');
            }
            else
            {
                declarationsRaw = new string[1] { contentRaw };
            }

            List<Declaration> declarations = new();

            for (int i1 = 0; i1 < declarationsRaw.Length; i1++)
            {
                string declarationRaw = declarationsRaw[i1];
                if (declarationRaw.Length == 0) continue;
                if (declarationRaw.Trim().Length == 0) continue;
                if (declarationRaw.Trim() == "}")
                {
                    if (i1 != declarationsRaw.Length - 1)
                    {
                        Debug.LogWarning($"[CSS/Parser]: Unexpected character ('{'}'}') before I finished parsing all the declarations");
                    }
                    break;
                }
                if (!declarationRaw.Contains(':'))
                {
                    Debug.LogWarning($"[CSS/Parser]: Declaration line not contains '{':'}'");
                    continue;
                }
                string variableName = declarationRaw.Split(':')[0];
                string valuesRaw = declarationRaw[(variableName.Length + 1)..].Trim();

                variableName = variableName.Trim().ToLower();

                if (valuesRaw.ToLowerInvariant().EndsWith("!important"))
                { valuesRaw = valuesRaw[..^"!important".Length]; }

                string[] valuesRawList = ParseParameters(valuesRaw);

                var values = ParseValues(valuesRawList);

                declarations.Add(new Declaration()
                {
                    property = variableName,
                    values = values,
                });
            }

            // declarations = OrganizeDeclarations(declarations);

            return declarations.ToArray();
        }

        static bool TryParseFunction(string raw, out string functionName, out string[] parameters)
        {
            functionName = null;
            parameters = null;

            if (!raw.Contains('('))
            { return false; }

            string possibleFunctionName = raw.Split('(')[0];
            for (int i = 0; i < possibleFunctionName.Length; i++)
            {
                char c = possibleFunctionName[i];
                if (char.IsLetterOrDigit(c)) continue;
                if (char.IsSymbol(c)) return false;
                if (char.IsPunctuation(c)) return false;
                if (char.IsSeparator(c)) return false;
                if (char.IsWhiteSpace(c)) return false;
                Debug.Log("Bruh");
            }

            string parametersRaw = raw[(possibleFunctionName.Length + 1)..];
            functionName = possibleFunctionName.Trim();

            parametersRaw = parametersRaw.Trim();

            if (parametersRaw.EndsWith(')'))
            { parametersRaw = parametersRaw[..^1]; }

            parameters = ParseParameters(parametersRaw);
            return true;
        }

        static string[] ParseParameters(string raw)
        {
            List<string> result = new();

            string currentParameter = "";

            bool inString = false;
            int parenthesis = 0;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (!inString && parenthesis == 0 && (c == ',' || c == ' '))
                {
                    if (currentParameter.Length > 0)
                    { result.Add(currentParameter); }
                    currentParameter = "";
                    continue;
                }

                if (c == '\"' || c == '\'')
                { inString = !inString; }

                if (c == '(')
                { parenthesis++; }

                if (c == ')')
                { parenthesis--; }

                currentParameter += c;
            }

            if (currentParameter.Length > 0)
            { result.Add(currentParameter); }

            return result.ToArray();
        }

        List<Declaration> OrganizeDeclarations(List<Declaration> declarations, int endlessSafe = 0)
        {
            if (endlessSafe > 500)
            { throw new EndlessLoopException(); }

            List<Declaration> newList = new();

            for (int i = 0; i < declarations.Count; i++)
            {
                var d = declarations[i];
                if (d.property == "background")
                {
                    for (int j = 0; j < d.values.Length; j++)
                    {
                        var v = d.values[j];
                        if (v.type == Value.Type.Color)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "background-color",
                                values = new Value[] { v },
                            });
                        }
                    }
                    continue;
                }
                else if (d.property == "margin")
                {
                    if (d.values.Length == 1)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "margin-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-right",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-bottom",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-left",
                            values = new Value[] { d.values[0] },
                        });
                    }
                    else if (d.values.Length == 2)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "margin-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-right",
                            values = new Value[] { d.values[1] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-bottom",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-left",
                            values = new Value[] { d.values[1] },
                        });
                    }
                    else if (d.values.Length == 4)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "margin-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-right",
                            values = new Value[] { d.values[1] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-bottom",
                            values = new Value[] { d.values[2] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "margin-left",
                            values = new Value[] { d.values[3] },
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[CSS/Parser]: Wrong number of values ({d.values.Length}) assigned to property \"{d.property}\"");
                    }

                    continue;
                }
                else if (d.property == "padding")
                {
                    if (d.values.Length == 1)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "padding-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-right",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-bottom",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-left",
                            values = new Value[] { d.values[0] },
                        });
                    }
                    else if (d.values.Length == 2)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "padding-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-right",
                            values = new Value[] { d.values[1] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-bottom",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-left",
                            values = new Value[] { d.values[1] },
                        });
                    }
                    else if (d.values.Length == 4)
                    {
                        newList.Add(new Declaration()
                        {
                            property = "padding-top",
                            values = new Value[] { d.values[0] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-right",
                            values = new Value[] { d.values[1] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-bottom",
                            values = new Value[] { d.values[2] },
                        });
                        newList.Add(new Declaration()
                        {
                            property = "padding-left",
                            values = new Value[] { d.values[3] },
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[CSS/Parser]: Wrong number of values ({d.values.Length}) assigned to property \"{d.property}\"");
                    }

                    continue;
                }
                else if (d.property == "border")
                {
                    for (int j = 0; j < d.values.Length; j++)
                    {
                        var v = d.values[j];
                        if (v.type == Value.Type.Number)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "border-width",
                                values = new Value[] { v },
                            });
                        }
                        else if (v.type == Value.Type.Color)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "border-color",
                                values = new Value[] { v },
                            });
                        }
                    }

                    continue;
                }
                else if (d.property == "text-shadow")
                {
                    for (int j = 0; j < d.values.Length; j++)
                    {
                        var v = d.values[j];
                        if (v.type == Value.Type.Number)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "text-shadow-ammount",
                                values = new Value[] { v },
                            });
                        }
                        else if (v.type == Value.Type.Color)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "text-shadow-color",
                                values = new Value[] { v },
                            });
                        }
                    }

                    continue;
                }
                else if (d.property == "text-outline")
                {
                    for (int j = 0; j < d.values.Length; j++)
                    {
                        var v = d.values[j];
                        if (v.type == Value.Type.Number)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "text-outline-ammount",
                                values = new Value[] { v },
                            });
                        }
                        else if (v.type == Value.Type.Color)
                        {
                            newList.Add(new Declaration()
                            {
                                property = "text-outline-color",
                                values = new Value[] { v },
                            });
                        }
                    }

                    continue;
                }

                newList.Add(d);
            }

            return newList;
        }

        Value[] ParseValues(string[] raw)
        {
            List<Value> values = new();

            foreach (var v in raw)
            {
                if (v.Length == 0) continue;
                var valueRaw = v.Trim();
                if (valueRaw.Length == 0) continue;
                var possibleValue = TryParseValue(valueRaw);
                if (possibleValue.HasValue)
                {
                    values.Add(possibleValue.Value);
                }
            }

            return values.ToArray();
        }

        Value? TryParseValue(string raw)
        {
            if (TryParseFunction(raw, out _, out _))
            {
                return null;
            }

            if (raw.StartsWith('#'))
            {
                string hexValue = raw[1..];

                Color color;

                if (hexValue.Length == 3)
                {
                    char[] digits = hexValue.ToCharArray();
                    byte r = byte.Parse(digits[0].ToString(), NumberStyles.HexNumber);
                    byte g = byte.Parse(digits[1].ToString(), NumberStyles.HexNumber);
                    byte b = byte.Parse(digits[2].ToString(), NumberStyles.HexNumber);

                    color = new Color(r + 1, g + 1, b + 1) * 16;
                }
                else if (hexValue.Length == 4)
                {
                    char[] digits = hexValue.ToCharArray();
                    byte r = byte.Parse(digits[0].ToString(), NumberStyles.HexNumber);
                    byte g = byte.Parse(digits[1].ToString(), NumberStyles.HexNumber);
                    byte b = byte.Parse(digits[2].ToString(), NumberStyles.HexNumber);
                    byte a = byte.Parse(digits[3].ToString(), NumberStyles.HexNumber);

                    color = new Color(r, g, b, a) * 16;
                }
                else if (hexValue.Length == 6)
                {
                    char[] digits = hexValue.ToCharArray();
                    byte r = byte.Parse(digits[0].ToString() + digits[1].ToString(), NumberStyles.HexNumber);
                    byte g = byte.Parse(digits[2].ToString() + digits[3].ToString(), NumberStyles.HexNumber);
                    byte b = byte.Parse(digits[4].ToString() + digits[5].ToString(), NumberStyles.HexNumber);

                    color = new(r, g, b);
                }
                else if (hexValue.Length == 8)
                {
                    char[] digits = hexValue.ToCharArray();
                    byte r = byte.Parse(digits[0].ToString() + digits[1].ToString(), NumberStyles.HexNumber);
                    byte g = byte.Parse(digits[2].ToString() + digits[3].ToString(), NumberStyles.HexNumber);
                    byte b = byte.Parse(digits[4].ToString() + digits[5].ToString(), NumberStyles.HexNumber);
                    byte a = byte.Parse(digits[6].ToString() + digits[7].ToString(), NumberStyles.HexNumber);

                    color = new Color(r, g, b, a);
                }
                else
                {
                    Debug.LogWarning("[CSS/Parser]: Invalid hex color");
                    return null;
                }

                return new Value(color);
            }

            if (TryParseNumber(raw, out var _number))
            { return new Value(_number); }

            if (TryParseIdentifier(raw, out string _identifier))
            { return new Value(_identifier); }

            if (TryParseString(raw, out string _stringValue))
            { return new Value(_stringValue); }

            Debug.LogWarning($"[CSS/Parser]: UNIMPLEMENTED");
            return null;
        }

        static bool TryParseNumber(string raw, out Number number)
        {
            number = default;

            if (string.IsNullOrWhiteSpace(raw))
            { return false; }

            string num = "";
            bool isFloat = false;
            bool isNegative = false;

            if (raw[0] == '-')
            {
                isNegative = true;
                raw = raw[1..];
            }

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (char.IsDigit(c))
                {
                    num += c;
                    continue;
                }

                if (c == '.')
                {
                    if (isFloat) return false;
                    isFloat = true;

                    num += c;
                    continue;
                }

                break;
            }

            if (raw.Length == 0)
            {
                return false;
            }

            float numberValue;

            if (isFloat)
            {
                if (!float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float @float))
                { return false; }

                if (isNegative) @float = -@float;

                numberValue = @float;
            }
            else
            {
                if (!int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out int @int))
                { return false; }

                if (isNegative) @int = -@int;

                numberValue = @int;
            }

            string possibleUnit = raw[num.Length..].Trim().ToLower();

            number = possibleUnit switch
            {
                "px" => new Number(numberValue, Unit.Pixels),
                "%" or "pct" => new Number(numberValue, Unit.Percentage),
                "em" => new Number(numberValue, Unit.Em),
                "" => new Number(numberValue, Unit.None),
                _ => new Number(numberValue, Unit.Unknown),
            };

            if (number.Unit == Unit.Unknown)
            { Debug.Log($"[CSS/Parser]: Unknown unit \"{possibleUnit}\""); }

            return true;
        }

        static bool TryParseIdentifier(string raw, out string identifier)
        {
            identifier = null;

            if (string.IsNullOrWhiteSpace(raw))
            { return false; }

            for (int i = 0; i < raw.Length; i++)
            {
                if (char.IsLetterOrDigit(raw[i]))
                { continue; }

                switch (raw[i])
                {
                    case '_': break;
                    case '-': break;
                    default: return false;
                }
            }

            identifier = raw;
            return true;
        }

        static bool TryParseString(string raw, out string value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(raw))
            { return false; }

            raw = raw.Trim();

            if (raw.StartsWith('\''))
            {
                if (!raw.EndsWith('\''))
                {
                    return false;
                }
                value = raw[1..^1];
                return true;
            }

            if (raw.StartsWith('\"'))
            {
                if (!raw.EndsWith('\"'))
                {
                    return false;
                }
                value = raw[1..^1];
                return true;
            }

            return false;
        }

        #endregion
    }
}