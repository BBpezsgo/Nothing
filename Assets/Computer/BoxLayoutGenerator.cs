using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphicsElementGenerator;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BoxLayout
{
    public static class NodeUtils
    {
        public static string TakeText(LayoutBox node)
            => NodeUtils.TakeText(node.Node);
        public static string TakeText(HtmlNode node)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Element:
                    if (node.Name == "input" && node.GetAttributeValue("type", null) == "submit")
                    {
                        return System.Web.HttpUtility.HtmlDecode(node.GetAttributeValue("value", "Submit").Trim());
                    }
                    return null;
                case HtmlNodeType.Text:
                    return System.Web.HttpUtility.HtmlDecode(node.InnerText.Trim());
                case HtmlNodeType.Document:
                case HtmlNodeType.Comment:
                default:
                    return null;
            }
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class LayoutBox
    {
        public HtmlNode Node;
        public Dimensions Dimensions;
        public BoxDisplay Display;
        public List<LayoutBox> Childrens;

        public LayoutBox(HtmlNode node, Dimensions dimensions, BoxDisplay display)
        {
            Node = node;
            Dimensions = dimensions;
            Display = display;
            Childrens = new List<LayoutBox>();
        }

        string GetDebuggerDisplay() => Node.NodeType switch
        {
            HtmlNodeType.Document => $"Document {{ {Dimensions.Content.size.x}px ; {Dimensions.Content.size.y}px }}",
            HtmlNodeType.Element => $"<{Node.Name} width={Dimensions.Content.size.x}px height={Dimensions.Content.size.y}px>",
            HtmlNodeType.Comment => $"<!-- {Node.InnerText} -->",
            HtmlNodeType.Text => $"\"{Node.InnerText.Trim()}\" {{ {Dimensions.Content.size.x}px ; {Dimensions.Content.size.y}px }}",
            _ => $"Bruh",
        };
    }

    public enum BoxDisplay
    {
        Undefined,
        Block,
        InlineBlock,
        Table,
        None,
    }

    public struct CachedDimensions
    {
        public RectInt Content;
        public RectInt PaddingRect;
        public RectInt BorderRect;
        public RectInt MarginRect;

        public CachedDimensions(Dimensions dimensions)
        {
            Content = dimensions.Content;
            PaddingRect = dimensions.PaddingRect;
            BorderRect = dimensions.BorderRect;
            MarginRect = dimensions.MarginRect;
        }

        public static CachedDimensions operator +(CachedDimensions dimensions, Vector2Int offset)
        {
            dimensions.Content.position += offset;
            dimensions.PaddingRect.position += offset;
            dimensions.BorderRect.position += offset;
            dimensions.MarginRect.position += offset;
            return dimensions;
        }

        public static CachedDimensions operator -(CachedDimensions dimensions, Vector2Int offset)
        {
            dimensions.Content.position -= offset;
            dimensions.PaddingRect.position -= offset;
            dimensions.BorderRect.position -= offset;
            dimensions.MarginRect.position -= offset;
            return dimensions;
        }
    }

    public struct Dimensions
    {
        public RectInt Content;
        public SidesInt Padding;
        public SidesInt Border;
        public SidesInt Margin;
        internal int CurrentX;
        /// <summary>
        /// This is <b>not</b> the css property "max-width"!
        /// </summary>
        internal int MaxWidth;

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt PaddingRect => Content.Extend(Padding);
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt BorderRect => PaddingRect.Extend(Border);
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt MarginRect => BorderRect.Extend(Margin);

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int PaddingPosition => Content.TopLeft() - Padding.TopLeft;
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int BorderPosition => PaddingPosition - Border.TopLeft;
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int MarginPosition => BorderPosition - Margin.TopLeft;

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly SidesInt ExtraSides => Padding + Border + Margin;

        public Dimensions(RectInt content)
        {
            Content = content;
            Padding = SidesInt.Zero;
            Border = SidesInt.Zero;
            Margin = SidesInt.Zero;
            CurrentX = 0;
            MaxWidth = content.width;
        }

        public Dimensions(RectInt content, SidesInt padding, SidesInt border, SidesInt margin, int current, int maxWidth)
        {
            Content = content;
            Padding = padding;
            Border = border;
            Margin = margin;
            CurrentX = current;
            MaxWidth = maxWidth;
        }

        public Dimensions(Dimensions other)
        {
            Content = other.Content;
            Padding = other.Padding;
            Border = other.Border;
            Margin = other.Margin;
            CurrentX = other.CurrentX;
            MaxWidth = other.MaxWidth;
        }

        public static Dimensions Zero => default;

        public readonly CachedDimensions Cached => new(this);

        internal void BreakLine(ref int lineHeight)
        {
            if (CurrentX == 0) return;

            Content.height += lineHeight;
            CurrentX = 0;
            lineHeight = 0;
        }

        internal void BreakLineForce(ref int lineHeight)
        {
            Content.height += lineHeight;
            CurrentX = 0;
            lineHeight = 0;
        }
    }

    public delegate Vector2Int TextMeasurer(string text, int fontSize);

    public static class StyleUtils
    {
        public static DeclarationsContainer GetDeclarations(IEnumerable<Stylesheet> stylesheets, HtmlNode node)
        {
            List<Declaration> result = new();
            foreach (Stylesheet stylesheet in stylesheets)
            { result.AddOrOverride(stylesheet.GetMatches(node)); }
            return new DeclarationsContainer(result);
        }

        public static BoxDisplay GetDisplay(this DeclarationsContainer childStyles)
        {
            string displayValue = childStyles.GetString("display");

            return displayValue switch
            {
                null => BoxDisplay.Undefined,
                "block" => BoxDisplay.Block,
                "inline" => BoxDisplay.InlineBlock,
                "inline-block" => BoxDisplay.InlineBlock,
                "none" => BoxDisplay.None,
                _ => BoxDisplay.Undefined,
            };
        }
    }

    public class BoxLayoutGenerator
    {
        Stylesheet[] Stylesheets;

        TextMeasurer TextMeasurer;
        ImageSizeGetter ImageSizeGetter;

        int FontSize;
        float BoxScale;

        RectInt Viewport;
        HtmlDocument Document;

        Vector2Int MeasureText(string text, int fontSize) => TextMeasurer?.Invoke(text, fontSize) ?? Vector2Int.zero;
        Vector2Int MeasureText(LayoutBox node, int fontSize) => MeasureText(node.Node, fontSize);
        Vector2Int MeasureText(HtmlNode node, int fontSize)
        {
            string text = NodeUtils.TakeText(node);
            return TextMeasurer?.Invoke(text, fontSize) ?? Vector2Int.zero;
        }

        bool TryGetTextSize(LayoutBox node, int fontSize, out Vector2Int size) => TryGetTextSize(node.Node, fontSize, out size);
        bool TryGetTextSize(HtmlNode node, int fontSize, out Vector2Int size)
        {
            size = default;

            if (TextMeasurer == null) return false;

            string text = NodeUtils.TakeText(node);

            if (string.IsNullOrWhiteSpace(text)) return false;

            size = TextMeasurer.Invoke(text, fontSize);
            return true;
        }

        bool TryGetImageSize(LayoutBox node, out Vector2Int size) => TryGetImageSize(node.Node, out size);
        bool TryGetImageSize(HtmlNode node, out Vector2Int size)
        {
            size = default;

            if (node.Name != "img")
            { return false; }

            int width = node.GetAttributeValue("width", -1);
            int height = node.GetAttributeValue("height", -1);

            if (width >= 0 && height >= 0)
            {
                size = new Vector2Int(Mathf.RoundToInt(width * BoxScale), Mathf.RoundToInt(height * BoxScale));
                return true;
            }

            string url = node.GetAttributeValue("src", null);

            bool success = ImageSizeGetter?.Invoke(url, out size) ?? false;

            if (success)
            {
                size = Vector2Int.RoundToInt(size.ToFloat() * BoxScale);
                return true;
            }

            return false;
        }

        public static LayoutBox LayoutDocument(HtmlDocument document, IEnumerable<Stylesheet> stylesheets, RectInt area, TextMeasurer textMeasurer, ImageSizeGetter imageSizeGetter)
        {
            BoxLayoutGenerator generator = new()
            {
                TextMeasurer = textMeasurer,
                ImageSizeGetter = imageSizeGetter,
                Stylesheets = stylesheets.ToArray(),
                FontSize = 8,
                BoxScale = .5f,
                Viewport = new RectInt(area.xMin, area.yMin, area.width, area.height),
                Document = document,
            };

            Dimensions rootDimensions = new(new RectInt(area.xMin, area.yMin, area.width, 0));
            LayoutBox root = new(document.DocumentNode, Dimensions.Zero, BoxDisplay.Block);
            generator.Layout(root, rootDimensions);
            return root;
        }

        void Layout(LayoutBox node, Dimensions dimensions)
        {
            switch (node.Display)
            {
                case BoxDisplay.Block:
                    LayoutBlock(node, dimensions);
                    break;
                case BoxDisplay.InlineBlock:
                    LayoutInline(node, dimensions);
                    break;
                case BoxDisplay.Table:
                    LayoutTable(node, dimensions);
                    break;
                case BoxDisplay.Undefined:
                case BoxDisplay.None:
                default:
                    throw new NotImplementedException();
            }
        }

        void LayoutBlock(LayoutBox node, Dimensions dimensions)
        {
            CalculateBlockWidth(node, dimensions);
            CalculateBlockPosition(node, dimensions);
            LayoutChildren(node);
            CalculateHeight(node, dimensions);
        }

        void LayoutInline(LayoutBox node, Dimensions dimensions)
        {
            CalculateInlineWidth(node, dimensions);
            CalculateInlinePosition(node, dimensions);
            LayoutChildren(node);
            CalculateHeight(node, dimensions);
            if (node.Dimensions.CurrentX > 0)
            { node.Dimensions.Content.width += node.Dimensions.CurrentX; }
        }

        void CalculateInlineWidth(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            d.Content.width = GetAbsoluteWidth(node, dimensions);

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                d.Content.width = textSize.x;
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                d.Content.width = imageSize.x;
            }
            else if (node.Node.GetAttributeValue("size", -1) >= 0)
            {
                int widthCharacters = node.Node.GetAttributeValue("size", -1);
                string space = new(' ', widthCharacters);
                d.Content.width = MeasureText(space, FontSize).x;
            }

            d.MaxWidth = dimensions.MaxWidth;

            d.Padding.SetHorizontal(style.GetSidesPx("padding") * BoxScale, Mathf.RoundToInt);
            d.Border.SetHorizontal(style.GetSidesPx("border") * BoxScale, Mathf.RoundToInt);
            d.Margin.SetHorizontal(style.GetSidesPx("margin") * BoxScale, Mathf.RoundToInt);
        }

        void CalculateInlinePosition(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            d.Padding.SetVertical(style.GetSidesPx("padding") * BoxScale, Mathf.RoundToInt);
            d.Border.SetVertical(style.GetSidesPx("border") * BoxScale, Mathf.RoundToInt);
            d.Margin.SetVertical(style.GetSidesPx("margin") * BoxScale, Mathf.RoundToInt);

            d.Content.x = dimensions.Content.x + dimensions.CurrentX;
            d.Content.y = dimensions.Content.height + dimensions.Content.y;
        }

        int GetAbsoluteWidth(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            if (!style.TryGetNumber("width", out Number number))
            { return 0; }

            switch (number.Unit)
            {
                case Unit.Pixels:
                    return Mathf.RoundToInt(number.Value * BoxScale);
                case Unit.Percentage:
                    return Mathf.RoundToInt(number.Percentage * dimensions.Content.width);
                case Unit.Em:
                    return Mathf.RoundToInt(number.Value * FontSize);
                case Unit.None:
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Value without unit for \"width\"");
                    return 0;
                case Unit.Unknown:
                default:
                    Debug.LogWarning($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unsupported \"width\" unit \"{number.Unit}\"");
                    return number.Int;
            }
        }

        void CalculateBlockWidth(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            Value width = new("auto");

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                width = new Value(new Number(textSize.x, Unit.Pixels));
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                width = new Value(new Number(imageSize.x, Unit.Pixels));
            }
            else if (node.Node.GetAttributeValue("size", -1) >= 0)
            {
                int widthCharacters = node.Node.GetAttributeValue("size", -1);
                string space = new(' ', widthCharacters);
                width = new Value(new Number(MeasureText(space, FontSize).x, Unit.Pixels));
            }

            if (style.TryGetNumber("width", out Number widthNumber))
            {
                width = new Value(new Number(ConvertToPixels(dimensions, widthNumber), Unit.Pixels));
            }

            ref Dimensions d = ref node.Dimensions;

            d.Padding.SetHorizontal(style.GetSidesPx("padding") * BoxScale, Mathf.RoundToInt);
            d.Border.SetHorizontal(style.GetSidesPx("border") * BoxScale, Mathf.RoundToInt);
            Sides<Value> margin = style.GetSides("margin");
            Sides<int> marginNumbers = margin.ToPixels();

            int widthPixels = ConvertToPixels(dimensions, width.NumberOrZero);

            int total = widthPixels + d.BorderRect.width + marginNumbers.Left + marginNumbers.Right;

            if (total > dimensions.Content.width)
            {
                if (margin.Left == "auto")
                { margin.Left = new Value(new Number(0, Unit.Pixels)); }

                if (margin.Right == "auto")
                { margin.Right = new Value(new Number(0, Unit.Pixels)); }
            }

            int underflow = dimensions.Content.width - total;

            if (width == "auto")
            {
                if (margin.Left == "auto")
                { margin.Left = new Value(new Number(0, Unit.Pixels)); }
                if (margin.Right == "auto")
                { margin.Right = new Value(new Number(0, Unit.Pixels)); }

                if (underflow >= 0)
                {
                    // Expand width to fill the underflow.
                    d.Content.width = underflow;
                }
                else
                {
                    // Width can't be negative. Adjust the right margin instead.
                    d.Content.width = 0;
                    margin.Right = new Value(new Number(ConvertToPixels(dimensions, margin.Right.NumberOrZero) + underflow, Unit.Pixels));
                }
            }
            else if (margin.Left.IsNumber && margin.Right.IsNumber)
            {
                margin.Right = new Value(new Number(ConvertToPixels(dimensions, margin.Right.number.Value) + underflow, Unit.Pixels));
            }
            else if (margin.Left.IsNumber && margin.Right == "auto")
            {
                margin.Right = new Value(new Number(underflow, Unit.Pixels));
            }
            else if (margin.Left == "auto" && margin.Right.IsNumber)
            {
                margin.Left = new Value(new Number(underflow, Unit.Pixels));
            }
            else if (margin.Left == "auto" && margin.Right == "auto")
            {
                margin.Left = new Value(new Number(Mathf.RoundToInt(underflow / 2f), Unit.Pixels));
                margin.Right = new Value(new Number(Mathf.RoundToInt(underflow / 2f), Unit.Pixels));
            }
            d.Margin.SetHorizontal(margin.ToPixels());

            d.MaxWidth = dimensions.MaxWidth;

            /*
            if (width == 0)
            {
                if (underflow >= 0)
                {
                    d.Content.width = underflow;
                    d.Margin.Right = margin.Right;
                }
                else
                {
                    d.Margin.Right = margin.Right + underflow;
                    d.Content.width = width;
                }
                d.Margin.Left = margin.Left;
            }
            else
            {
                d.Content.width = width;
            }
            */
        }

        int ConvertToPixels(Dimensions containingBox, Number number)
        {
            return number.Unit switch
            {
                Unit.Percentage => Mathf.RoundToInt(number.Percentage * containingBox.Content.width),
                Unit.Em => Mathf.RoundToInt(number.Value * FontSize),
                Unit.Pixels => number.Int,
                _ => number.Int,
            };
        }

        void CalculateBlockPosition(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            ref Dimensions d = ref node.Dimensions;

            d.Margin.SetVertical((style.GetSidesPx("margin") * BoxScale).ToInt());
            d.Border.SetVertical((style.GetSidesPx("border") * BoxScale).ToInt());
            d.Padding.SetVertical((style.GetSidesPx("padding") * BoxScale).ToInt());

            d.Content.x = dimensions.Content.x + d.ExtraSides.Left;
            d.Content.y = dimensions.Content.yMax + d.ExtraSides.Top;
        }

        void CalculateHeight(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                d.Content.height = textSize.y;
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                d.Content.height = imageSize.x;
            }

            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            if (style.TryGetNumber("height", out Number number))
            {
                switch (number.Unit)
                {
                    case Unit.Pixels:
                        d.Content.height = (int)(number.Value * BoxScale);
                        break;
                    case Unit.Em:
                        d.Content.height = Mathf.RoundToInt(number.Value * FontSize);
                        break;
                    case Unit.Percentage:
                        // d.Content.height = Mathf.RoundToInt(number.Percentage * dimensions.Content.height);
                        break;
                    case Unit.None:
                        Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Value without unit for \"height\"");
                        break;
                    case Unit.Unknown:
                    default:
                        Debug.LogWarning($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unsupported \"height\" unit \"{number.Unit}\"");
                        break;
                }
            }
        }

        void LayoutChildren(LayoutBox node)
        {
            List<HtmlNode> childs = new();

            foreach (HtmlNode child in node.Node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Comment)
                { continue; }

                if (child is HtmlTextNode textNode)
                { childs.AddRange(BreakText(textNode, node.Dimensions.MaxWidth)); }
                else
                { childs.Add(child); }
            }

            int lineHeight = 0;
            bool prevIsBlock = true;

            foreach (HtmlNode child in childs)
            {
                if (child.NodeType == HtmlNodeType.Comment)
                { continue; }

                DeclarationsContainer childStyles = StyleUtils.GetDeclarations(Stylesheets, child);
                BoxDisplay childDisplay = childStyles.GetDisplay();

                if (childDisplay == BoxDisplay.Undefined)
                {
                    childDisplay = child.NodeType switch
                    {
                        HtmlNodeType.Document => BoxDisplay.Block,
                        HtmlNodeType.Text => BoxDisplay.InlineBlock,
                        HtmlNodeType.Element => child.Name switch
                        {
                            "table" => BoxDisplay.Table,
                            _ => childDisplay,
                        },
                        HtmlNodeType.Comment => BoxDisplay.None,
                        _ => childDisplay,
                    };
                }

                if (childDisplay == BoxDisplay.None)
                { continue; }

                if (childDisplay == BoxDisplay.Undefined)
                {
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: No display specified for element \"<{child.Name}>\"");
                    childDisplay = BoxDisplay.Block;
                }

                bool isBlock = (childDisplay == BoxDisplay.Block || childDisplay == BoxDisplay.Table);

                LayoutBox childBox = new(child, new Dimensions(Dimensions.Zero)
                {
                    MaxWidth = Mathf.Max(0, node.Dimensions.Content.width - node.Dimensions.CurrentX),
                }, childDisplay);
                node.Childrens.Add(childBox);

                if (isBlock)
                { node.Dimensions.BreakLineForce(ref lineHeight); }

                Layout(childBox, node.Dimensions);
                node.Dimensions.CurrentX += childBox.Dimensions.MarginRect.width;

                lineHeight = Mathf.Max(lineHeight, childBox.Dimensions.MarginRect.height);

                if (isBlock)
                {
                    node.Dimensions.BreakLineForce(ref lineHeight);
                }
                else
                {
                    if (node.Dimensions.CurrentX > node.Dimensions.Content.width)
                    {
                        node.Dimensions.BreakLine(ref lineHeight);

                        Layout(childBox, node.Dimensions);
                        node.Dimensions.CurrentX += childBox.Dimensions.MarginRect.width;

                        lineHeight = Mathf.Max(lineHeight, childBox.Dimensions.MarginRect.height);
                    }
                }

                prevIsBlock = isBlock;
            }

            if (lineHeight > 0)
            {
                node.Dimensions.BreakLineForce(ref lineHeight);
                node.Dimensions.Content.width += node.Dimensions.CurrentX;
            }
        }

        void LayoutTable(LayoutBox node, Dimensions dimensions)
        {
            List<List<HtmlNode>> table = new();
            foreach (HtmlNode row in node.Node.ChildNodes)
            {
                if (row.NodeType == HtmlNodeType.Comment) continue;

                if (row.Name != "tr")
                {
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unexpected table element as row: <{row.Name}>");
                    continue;
                }

                List<HtmlNode> rowContent = new();
                foreach (HtmlNode col in row.ChildNodes)
                {
                    if (col.NodeType == HtmlNodeType.Comment) continue;

                    if (col.Name != "td" && col.Name != "th")
                    {
                        Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unexpected table element as cell: <{row.Name}>");
                        continue;
                    }

                    rowContent.Add(col);
                }
                table.Add(rowContent);
            }

            int cellSpacing = 4;

            int columns = 0;

            foreach (List<HtmlNode> row in table)
            {
                columns = Mathf.Max(row.Count, columns);
            }

            foreach (List<HtmlNode> row in table)
            {
                for (int i = row.Count; i < columns; i++)
                {
                    row.Add(Document.CreateElement("td"));
                }
            }

            CalculateBlockWidth(node, dimensions);
            CalculateBlockPosition(node, dimensions);

            ref Dimensions d = ref node.Dimensions;

            int[] columnWidths = new int[columns];
            for (int i = 0; i < columnWidths.Length; i++)
            {
                columnWidths[i] = (d.Content.width - (cellSpacing * (columns - 1))) / columns;
            }

            Dimensions dCopy = node.Dimensions;

            for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                List<HtmlNode> row = table[rowIndex];

                int rowHeight = 0;

                for (int cellIndex = 0; cellIndex < row.Count; cellIndex++)
                {
                    LayoutBox newBox = new(row[cellIndex], new Dimensions(Dimensions.Zero)
                    {
                        MaxWidth = Mathf.Max(0, dCopy.Content.width - dCopy.CurrentX),
                    }, BoxDisplay.InlineBlock);
                    Layout(newBox, dCopy);
                    rowHeight = Mathf.Max(rowHeight, newBox.Dimensions.MarginRect.height);

                    dCopy.CurrentX += newBox.Dimensions.MarginRect.width;
                    dCopy.CurrentX += cellSpacing;

                    columnWidths[cellIndex] = Mathf.Max(columnWidths[cellIndex], newBox.Dimensions.MarginRect.width);
                }

                dCopy.Content.width = Mathf.Max(dCopy.CurrentX, dCopy.Content.width);
                dCopy.CurrentX = 0;
                dCopy.Content.height += rowHeight;
                dCopy.Content.height += cellSpacing;
            }

            for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                List<HtmlNode> row = table[rowIndex];

                int rowHeight = 0;

                for (int cellIndex = 0; cellIndex < row.Count; cellIndex++)
                {
                    LayoutBox newBox = new(row[cellIndex], new Dimensions(Dimensions.Zero)
                    {
                        MaxWidth = columnWidths[cellIndex],
                    }, BoxDisplay.InlineBlock);
                    node.Childrens.Add(newBox);
                    Layout(newBox, d);
                    rowHeight = Mathf.Max(rowHeight, newBox.Dimensions.MarginRect.height);

                    d.CurrentX += columnWidths[cellIndex];
                    d.CurrentX += cellSpacing;
                }

                d.Content.width = Mathf.Max(d.CurrentX, d.Content.width);
                d.CurrentX = 0;
                d.Content.height += rowHeight;
                d.Content.height += cellSpacing;
            }
        }

        HtmlTextNode[] BreakText(HtmlTextNode node, int maxWidth)
        {
            if (node == null) return null;

            List<HtmlTextNode> result = new();

            List<string> remaingWords = new(node.Text.Split(' ', '\n', '\r', '\t'));
            string currentText = "";

            int endlessSafe = 0;
            while (remaingWords.Count > 0)
            {
                string word = remaingWords[0];
                remaingWords.RemoveAt(0);

                if (string.IsNullOrWhiteSpace(word))
                { continue; }

                string space = "";
                if (currentText.Length > 0)
                {
                    space = " ";
                }

                int width = MeasureText(currentText + space + word, FontSize).x;

                if (width > maxWidth)
                {
                    if (!string.IsNullOrWhiteSpace(currentText))
                    { result.Add(Document.CreateTextNode(currentText)); }
                    currentText = word;
                }
                else
                {
                    currentText += space + word;
                }

                if (endlessSafe++ > 50)
                { break; }
            }

            if (!string.IsNullOrWhiteSpace(currentText))
            {
                result.Add(Document.CreateTextNode(currentText));
                currentText = "";
            }

            return result.ToArray();
        }
    }
}
