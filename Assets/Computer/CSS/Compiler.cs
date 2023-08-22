using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ProgrammingLanguage.Css
{
    public static class SidesExtensions
    {
        public static Sides<Number> ToNumbers(this Sides<Value> v) => v.ToNumbers(new Number(0, Unit.None));
        public static Sides<Number> ToNumbers(this Sides<Value> v, Number @default) => v.ToAny(value =>
        {
            if (value.type != Value.Type.Number) return @default;
            return value.number.Value;
        });

        public static Sides<Color> ToColors(this Sides<Value> v) => v.ToColors(new Color(0, 0, 0));
        public static Sides<Color> ToColors(this Sides<Value> v, Color @default) => v.ToAny(value =>
        {
            if (value.type != Value.Type.Color) return @default;
            return value.color.Value;
        });

        public static Sides<string> ToOthers(this Sides<Value> v) => v.ToOthers(null);
        public static Sides<string> ToOthers(this Sides<Value> v, string @default) => v.ToAny(value =>
        {
            if (value.type != Value.Type.Other) return @default;
            return value.other;
        });

        public static Sides<int> ToPixels(this Sides<Value> v) => v.ToPixels(0);
        public static Sides<int> ToPixels(this Sides<Value> v, int @default) => v.ToAny(value =>
        {
            if (value.type != Value.Type.Number) return @default;
            return value.number.Value.Unit switch
            {
                Unit.Pixels => value.number.Value.Int,
                _ => @default,
            };
        });

        public static SidesInt ToInt(this Sides<int> v) => new(v.Top, v.Left, v.Bottom, v.Right);
        public static SidesInt ToInt(this Sides<float> v) => new(v.Top, v.Left, v.Bottom, v.Right);
        public static Sides<T2> ToAny<T1, T2>(this Sides<T1> v, Func<T1, T2> converter)
            where T1 : IEquatable<T1> where T2 : IEquatable<T2>
            => new(converter.Invoke(v.Top), converter.Invoke(v.Left), converter.Invoke(v.Bottom), converter.Invoke(v.Right));

        public static RectInt Extend(this RectInt rect, SidesInt sides)
        {
            rect.yMin -= sides.Top;
            rect.xMin -= sides.Left;
            rect.yMax += sides.Bottom;
            rect.xMax += sides.Right;
            return rect;
        }

        public static RectInt Extend(this RectInt rect, Sides<int> sides)
        {
            rect.yMin -= sides.Top;
            rect.xMin -= sides.Left;
            rect.yMax += sides.Bottom;
            rect.xMax += sides.Right;
            return rect;
        }

        public static Rect Extend(this Rect rect, Sides<float> sides)
        {
            rect.yMin -= sides.Top;
            rect.xMin -= sides.Left;
            rect.yMax += sides.Bottom;
            rect.xMax += sides.Right;
            return rect;
        }

        public static Rect Extend(this Rect rect, SidesInt sides)
        {
            rect.yMin -= sides.Top;
            rect.xMin -= sides.Left;
            rect.yMax += sides.Bottom;
            rect.xMax += sides.Right;
            return rect;
        }

        public static Rect Extend(this Rect rect, Sides<int> sides)
        {
            rect.yMin -= sides.Top;
            rect.xMin -= sides.Left;
            rect.yMax += sides.Bottom;
            rect.xMax += sides.Right;
            return rect;
        }

        public static Vector2Int TopLeft(this Sides<int> v) => new(v.Left, v.Top);
        public static Vector2 TopLeft(this Sides<float> v) => new(v.Left, v.Top);
        public static ValueTuple<T, T> TopLeft<T>(this Sides<T> v)
            where T : IEquatable<T>
            => new(v.Left, v.Top);
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct SidesInt
    {
        public int Top;
        public int Left;
        public int Bottom;
        public int Right;

        public static SidesInt Zero => new(0);
        public static SidesInt One => new(1);

        public Vector2Int TopLeft
        {
            readonly get => new(Left, Top);
            set
            {
                Left = value.x;
                Top = value.y;
            }
        }

        public readonly int Width => Left + Right;
        public readonly int Height => Top + Bottom;

        public readonly Vector2Int Size => new(Width, Height);

        public SidesInt(int top, int left, int bottom, int right)
        {
            Top = top;
            Left = left;
            Bottom = bottom;
            Right = right;
        }

        public SidesInt(int vertical, int horizontal)
        {
            Top = vertical;
            Left = horizontal;
            Bottom = vertical;
            Right = horizontal;
        }

        public SidesInt(int all)
        {
            Top = all;
            Left = all;
            Bottom = all;
            Right = all;
        }

        public SidesInt(float top, float left, float bottom, float right)
        {
            Top = Mathf.RoundToInt(top);
            Left = Mathf.RoundToInt(left);
            Bottom = Mathf.RoundToInt(bottom);
            Right = Mathf.RoundToInt(right);
        }

        public SidesInt(float vertical, float horizontal)
        {
            Top = Mathf.RoundToInt(vertical);
            Left = Mathf.RoundToInt(horizontal);
            Bottom = Mathf.RoundToInt(vertical);
            Right = Mathf.RoundToInt(horizontal);
        }

        public SidesInt(float all)
        {
            Top = Mathf.RoundToInt(all);
            Left = Mathf.RoundToInt(all);
            Bottom = Mathf.RoundToInt(all);
            Right = Mathf.RoundToInt(all);
        }

        public SidesInt Extend(SidesInt other)
        {
            Top = Mathf.Max(Top, other.Top);
            Left = Mathf.Max(Left, other.Left);
            Bottom = Mathf.Max(Bottom, other.Bottom);
            Right = Mathf.Max(Right, other.Right);
            return this;
        }

        public SidesInt SetHorizontal(int value)
        {
            this.Left = value;
            this.Right = value;
            return this;
        }

        public SidesInt SetVertical(int value)
        {
            this.Top = value;
            this.Bottom = value;
            return this;
        }

        public SidesInt SetHorizontal(SidesInt value)
        {
            this.Left = value.Left;
            this.Right = value.Right;
            return this;
        }

        public SidesInt SetVertical(SidesInt value)
        {
            this.Top = value.Left;
            this.Bottom = value.Right;
            return this;
        }

        public SidesInt SetHorizontal(Sides<int> value)
        {
            this.Left = value.Left;
            this.Right = value.Right;
            return this;
        }

        public SidesInt SetVertical(Sides<int> value)
        {
            this.Top = value.Left;
            this.Bottom = value.Right;
            return this;
        }

        public SidesInt SetHorizontal<T>(Sides<T> value, Sides<T>.Converter<T, int> converter) where T : IEquatable<T>
        {
            this.Left = converter.Invoke(value.Left);
            this.Right = converter.Invoke(value.Right);
            return this;
        }

        public SidesInt SetVertical<T>(Sides<T> value, Sides<T>.Converter<T, int> converter) where T : IEquatable<T>
        {
            this.Top = converter.Invoke(value.Left);
            this.Bottom = converter.Invoke(value.Right);
            return this;
        }

        public static implicit operator Sides<int>(SidesInt v) => new(v.Top, v.Left, v.Bottom, v.Right);
        public static implicit operator Sides<float>(SidesInt v) => new(v.Top, v.Left, v.Bottom, v.Right);

        readonly string GetDebuggerDisplay()
        {
            if (Top == Bottom && Left == Right)
            {
                if (Top == Left)
                { return $"{Top}"; }
                return $"{Top} {Left}";
            }
            return $"{Top} {Right} {Bottom} {Left}";
        }

        public override readonly string ToString() => $"{nameof(SidesInt)} {{ Top: {Top}, Right: {Right}, Bottom: {Bottom}, Left: {Left} }}";

        public static Sides<float> operator *(SidesInt a, float b)
            => new(a.Top * b, a.Left * b, a.Bottom * b, a.Right * b);

        public static SidesInt operator *(SidesInt a, int b)
            => new(a.Top * b, a.Left * b, a.Bottom * b, a.Right * b);

        public static SidesInt operator +(SidesInt a, SidesInt b)
            => new(a.Top + b.Top, a.Left + b.Left, a.Bottom + b.Bottom, a.Right + b.Right);
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct Sides<T> where T : IEquatable<T>
    {
        public T Top;
        public T Left;
        public T Bottom;
        public T Right;

        public Sides(T top, T left, T bottom, T right)
        {
            Top = top;
            Left = left;
            Bottom = bottom;
            Right = right;
        }

        public Sides(T vertical, T horizontal)
        {
            Top = vertical;
            Left = horizontal;
            Bottom = vertical;
            Right = horizontal;
        }

        public Sides(T all)
        {
            Top = all;
            Left = all;
            Bottom = all;
            Right = all;
        }

        public Sides<T> SetHorizontal(T value)
        {
            this.Left = value;
            this.Right = value;
            return this;
        }

        public Sides<T> SetVertical(T value)
        {
            this.Top = value;
            this.Bottom = value;
            return this;
        }

        public Sides<T> SetHorizontal(Sides<T> value)
        {
            this.Left = value.Left;
            this.Right = value.Right;
            return this;
        }

        public Sides<T> SetVertical(Sides<T> value)
        {
            this.Top = value.Left;
            this.Bottom = value.Right;
            return this;
        }

        public delegate TDestination Converter<TSource, TDestination>(TSource value);

        public Sides<T> SetHorizontal<T2>(Sides<T2> value, Converter<T2, T> converter) where T2 : IEquatable<T2>, IConvertible
        {
            this.Left = converter.Invoke(value.Left);
            this.Right = converter.Invoke(value.Right);
            return this;
        }

        public Sides<T> SetVertical<T2>(Sides<T2> value, Converter<T2, T> converter) where T2 : IEquatable<T2>, IConvertible
        {
            this.Top = converter.Invoke(value.Left);
            this.Bottom = converter.Invoke(value.Right);
            return this;
        }

        readonly string GetDebuggerDisplay()
        {
            if ((IEquatable<T>)Top == (IEquatable<T>)Bottom && (IEquatable<T>)Left == (IEquatable<T>)Right)
            {
                if ((IEquatable<T>)Top == (IEquatable<T>)Left)
                { return $"{Top}"; }
                return $"{Top} {Left}";
            }
            return $"{Top} {Right} {Bottom} {Left}";
        }

        public override readonly string ToString() => $"{nameof(SidesInt)} {{ Top: {Top}, Right: {Right}, Bottom: {Bottom}, Left: {Left} }}";
    }

    public static class Compiler
    {
        public static Sides<Value> GetSides(this Value[] values)
            => TryGetSides(values, out Sides<Value> result) ? result : default;
        public static bool TryGetSides(this Value[] values, out Sides<Value> sides)
        {
            if (values.Length == 1)
            {
                sides = new Sides<Value>(values[0]);
                return true;
            }

            if (values.Length == 2)
            {
                sides = new Sides<Value>(values[0], values[1]);
                return true;
            }

            if (values.Length == 4)
            {
                sides = new Sides<Value>(values[0], values[3], values[2], values[1]);
                return true;
            }

            sides = default;
            return false;
        }

        public static SidesInt GetSidesPx(this Value[] values)
        {
            SidesInt result = SidesInt.Zero;

            if (values.Length == 1)
            {
                if (values[0].type == Value.Type.Number && values[0].number.Value.Unit == Unit.Pixels)
                {
                    result = new SidesInt(values[0].number.Value.Int);
                }
            }
            else if (values.Length == 2)
            {
                if (values[0].type == Value.Type.Number && values[0].number.Value.Unit == Unit.Pixels)
                {
                    result.Top = values[0].number.Value.Int;
                    result.Bottom = values[0].number.Value.Int;
                }
                if (values[1].type == Value.Type.Number && values[1].number.Value.Unit == Unit.Pixels)
                {
                    result.Right = values[1].number.Value.Int;
                    result.Left = values[1].number.Value.Int;
                }
            }
            else if (values.Length == 4)
            {
                if (values[0].type == Value.Type.Number && values[0].number.Value.Unit == Unit.Pixels)
                {
                    result.Top = values[0].number.Value.Int;
                }
                if (values[1].type == Value.Type.Number && values[1].number.Value.Unit == Unit.Pixels)
                {
                    result.Right = values[1].number.Value.Int;
                }
                if (values[2].type == Value.Type.Number && values[2].number.Value.Unit == Unit.Pixels)
                {
                    result.Bottom = values[2].number.Value.Int;
                }
                if (values[3].type == Value.Type.Number && values[3].number.Value.Unit == Unit.Pixels)
                {
                    result.Left = values[3].number.Value.Int;
                }
            }

            return result;
        }

        public static bool IsMatch(this Selector selector, HtmlAgilityPack.HtmlNode node)
        {
            foreach (char combinator in selector.Combinators)
            {
                if (combinator == '*')
                { return true; }
            }

            return selector.Simple.IsMatch(node);
        }

        public static bool IsMatch(this SimpleSelector[] selector, HtmlAgilityPack.HtmlNode node)
        {
            for (int i = 0; i < selector.Length; i++)
            {
                SimpleSelector subselector = selector[i];

                bool match1 = subselector.type switch
                {
                    SimpleSelector.Type.TagName => node.Name == subselector.tagName,
                    SimpleSelector.Type.ID => node.GetAttributeValue("id", null) == subselector.id,
                    SimpleSelector.Type.Class => node.HasClass(subselector.@class),
                    _ => false,
                };

                if (!match1) return false;

                if (!string.IsNullOrWhiteSpace(subselector.elementState))
                { return false; }
            }

            return true;
        }

        public static Declaration[] GetMatches(this Stylesheet stylesheet, HtmlAgilityPack.HtmlNode node)
        {
            List<Declaration> result = new();
            foreach (var rule in stylesheet.rules)
            {
                if (rule.selector.IsMatch(node))
                {
                    result.AddOrOverride(rule.declarations);
                }
            }
            return result.ToArray();
        }
    }
}