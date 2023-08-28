using System;
using System.Collections.Generic;
using BoxLayout;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using UnityEngine;
using Color = UnityEngine.Color;

namespace GraphicsElementGenerator
{
    public delegate bool ImageSizeGetter(string url, out Vector2Int size);

    [System.Diagnostics.DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class Element
    {
        public readonly CachedDimensions Dimensions;

        protected Element(LayoutBox box)
        {
            Dimensions = box.Dimensions.Cached;
        }

        public abstract ElementKind Kind { get; }

        string GetDebuggerDisplay() => ToString();

        public override string ToString() => Kind.ToString();
    }

    public class ElementLabel : Element
    {
        public string Text;
        public Color Color;
        public string Link;
        public ushort LinkID;

        public ElementLabel(LayoutBox box) : base(box)
        {
        }

        public override ElementKind Kind => ElementKind.Text;

        public sealed override string ToString() => $"{base.ToString()} \"{Text}\"";
    }

    public abstract class ElementWithID : Element
    {
        public ushort ID;

        protected ElementWithID(LayoutBox box) : base(box)
        {
        }

        public override string ToString() => $"{base.ToString()}#{ID}";
    }

    public abstract class ElementFocusable : ElementWithID
    {
        protected ElementFocusable(LayoutBox box) : base(box)
        {
        }
    }

    public class ElementButton : ElementFocusable
    {
        public string Text;
        public override ElementKind Kind => ElementKind.Button;
        public ElementForm Form;

        public ElementButton(LayoutBox box) : base(box)
        {
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Text: \"{Text}\" }}";
    }

    public class ElementImage : Element
    {
        public string Url;
        public byte ImageID;

        public ElementImage(LayoutBox box) : base(box)
        {
        }

        public override ElementKind Kind => ElementKind.Image;

        public sealed override string ToString() => $"{base.ToString()} {{ Url: \"{Url}\" ImageID: {ImageID} }}";
    }

    public class ElementTextField : ElementFocusable
    {
        public string Name;
        internal InGameComputer.TextInputField Manager;
        public override ElementKind Kind => ElementKind.InputText;
        public ElementForm Form;

        public ElementTextField(LayoutBox box) : base(box)
        {
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Name: \"{Name}\" Value: \"{Manager?.Buffer}\" }}";
    }

    public class ElementSelect : ElementFocusable
    {
        public (string Value, string Label)[] Values;
        public int SelectedIndex;

        public ElementSelect(LayoutBox box) : base(box)
        {
        }

        public (string Value, string Label)? Selected => (SelectedIndex < 0 || SelectedIndex > Values.Length) ? null : Values[SelectedIndex];
        public string Label => (SelectedIndex < 0 || SelectedIndex > Values.Length) ? null : Values[SelectedIndex].Label;

        public override ElementKind Kind => ElementKind.Select;

        public sealed override string ToString() => $"{base.ToString()} {{ SelectedIndex: {SelectedIndex} }}";
    }

    public class ElementForm : ElementWithID
    {
        public override ElementKind Kind => ElementKind.Form;
        public bool Submitted;
        public bool ShouldSubmit;
        internal string Method;
        internal string Target;

        public ElementForm(LayoutBox box) : base(box)
        {
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Method: \"{Method}\" Target: \"{Target}\" }}";
    }

    public enum ElementKind
    {
        Text,
        Button,
        InputText,
        Form,
        Image,
        Select,
    }

    readonly struct GeneratorUtils
    {
        public static readonly string[] SupportedTags = new string[]
        {
            "div",
            "p",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "b",
            "u",
            "html",
            "body",
            "a",
            "center",
            "br",
            "span",
        };

        public static string ConvertHtmlText(string text)
        {
            if (text is null) return null;
            return System.Web.HttpUtility.HtmlDecode(text);
        }

        public static Color FixColor(Color color)
        {
            return color;
            /*
            if (color.grayscale >= .5f) return color;
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp(v, .5f, 1f);
            return Color.HSVToRGB(h, s, v);
            */
        }

    }

    public class Generator
    {
        public class NeedThisImage
        {
            public string Url;
            public byte ID;
            public Vector2Int DownloadedSize;
        }

        public readonly List<NeedThisImage> NeedTheseImages;
        public readonly List<ElementForm> Forms;
        public readonly List<Stylesheet> Stylesheets;
        public readonly List<Element> Elements;

        TextMeasurer MeasureText;
        ImageSizeGetter GetImageSize;

        ushort ElementIDCounter;
        byte ImageIDCounter;
        ushort LinkIDCounter;

        public RectInt PageArea;
        Vector2Int overflow;

        public Vector2Int Overflow => overflow;
        public int OverflowX => overflow.x;
        public int OverflowY => overflow.y;

        public Vector2Int PageSize => overflow + PageArea.size;
        public int PageWidth => PageSize.x;
        public int PageHeight => PageSize.y;

        readonly Stack<string> LinkStack;
        readonly Stack<ElementForm> FormStack;
        readonly Stack<Declaration[]> StyleStack;

        static readonly string[] inheritableStyleProperties = new string[]
        {
            "color",
        };

        public Generator()
        {
            LinkStack = new Stack<string>();
            FormStack = new Stack<ElementForm>();
            StyleStack = new Stack<Declaration[]>();

            NeedTheseImages = new List<NeedThisImage>();

            Stylesheets = new List<Stylesheet>();

            Forms = new List<ElementForm>();
            Elements = new List<Element>();

            ElementIDCounter = 0;

            PageArea = default;
            overflow = Vector2Int.zero;

            MeasureText = null;
        }

        public void Reset()
        {
            LinkStack.Clear();
            FormStack.Clear();
            StyleStack.Clear();

            NeedTheseImages.Clear();

            Stylesheets.Clear();

            Forms.Clear();
            Elements.Clear();

            ElementIDCounter = 0;
            ImageIDCounter = 0;
            LinkIDCounter = 0;

            PageArea = default;
            overflow = Vector2Int.zero;

            MeasureText = null;
        }

        NeedThisImage GetOrCreateImage(string url)
        {
            foreach (var image in NeedTheseImages)
            {
                if (string.Equals(image.Url, url, StringComparison.InvariantCulture))
                {
                    return image;
                }
            }

            ImageIDCounter++;
            NeedThisImage newImage = new()
            {
                Url = url,
                ID = ImageIDCounter,
                DownloadedSize = Vector2Int.zero,
            };
            NeedTheseImages.Add(newImage);
            return newImage;
        }

        public ElementWithID GetElementByID(ushort id)
        {
            foreach (var element in Elements)
            {
                if (element is ElementWithID elementFocusable && elementFocusable.ID == id)
                {
                    return elementFocusable;
                }
            }
            return null;
        }
        public Element[] GetFormElements(ElementForm form)
        {
            List<Element> result = new();
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i] is ElementButton button)
                {
                    if (button.Form.ID == form.ID)
                    {
                        result.Add(button);
                        continue;
                    }
                }
                else if (Elements[i] is ElementTextField textField)
                {
                    if (textField.Form.ID == form.ID)
                    {
                        result.Add(textField);
                        continue;
                    }
                }
            }
            return result.ToArray();
        }

        public void GenerateLayout(HtmlDocument document, TextMeasurer textMeasurer, ImageSizeGetter imageSizeGetter)
        {
            MeasureText = textMeasurer;
            ElementIDCounter = 1;
            Elements.Clear();
            Forms.Clear();
            overflow = Vector2Int.zero;

            LayoutBox root = BoxLayoutGenerator.LayoutDocument(document, Stylesheets, PageArea, textMeasurer, imageSizeGetter);

            overflow = root.Dimensions.MarginRect.size - PageArea.size;

            GenerateElement(root, DeclarationsContainer.Empty);
        }

        void GenerateElement(LayoutBox layoutBox, DeclarationsContainer currentStyles)
        {
            List<Declaration> collectedStyles = new(currentStyles);

            foreach (Stylesheet stylesheet in Stylesheets)
            { collectedStyles.AddRange(stylesheet.GetMatches(layoutBox.Node)); }

            DeclarationsContainer newStyles = new(collectedStyles);

            Action after;

            switch (layoutBox.Node.NodeType)
            {
                case HtmlNodeType.Document:
                    GenerateElementForChilds(layoutBox, newStyles);
                    return;
                case HtmlNodeType.Element:
                    bool shouldBreak = GenerateElementForElement(layoutBox, newStyles, out after);
                    if (shouldBreak) return;
                    break;
                case HtmlNodeType.Comment:
                    return;
                case HtmlNodeType.Text:
                    GenerateElementForText(layoutBox, newStyles);
                    return;
                default:
                    return;
            }

            GenerateElementForChilds(layoutBox, newStyles);

            after?.Invoke();
        }

        void GenerateElementForChilds(LayoutBox layoutBox, DeclarationsContainer styles)
        {
            Declaration[] inheritingStyles = styles.GetDeclarations(inheritableStyleProperties);

            foreach (LayoutBox child in layoutBox.Childrens)
            { GenerateElement(child, new DeclarationsContainer(inheritingStyles)); }
        }

        void GenerateElementForText(LayoutBox layoutBox, DeclarationsContainer style)
        {
            if (layoutBox.Node.NodeType != HtmlNodeType.Text) return;

            string text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.InnerText);
            if (string.IsNullOrWhiteSpace(text)) return;

            string link = null;
            if (LinkStack.Count > 0)
            { link = LinkStack.Peek(); }
            Color defaultColor = Color.white;

            if (!string.IsNullOrWhiteSpace(link))
            { defaultColor = Color.blue; }

            ElementLabel element = new(layoutBox)
            {
                Text = text,
                Color = GeneratorUtils.FixColor(style.GetColor("color") ?? defaultColor),
            };
            if (!string.IsNullOrWhiteSpace(link))
            {
                element.LinkID = LinkIDCounter;
                element.Link = link;
            }
            Elements.Add(element);
        }

        bool GenerateElementForElement(LayoutBox layoutBox, DeclarationsContainer styles, out Action after)
        {
            after = null;

            if (layoutBox.Node.NodeType != HtmlNodeType.Element) return true;

            if (layoutBox.Node.Name == "button")
            {
                Elements.Add(new ElementButton(layoutBox)
                {
                    Text = layoutBox.Node.InnerText,
                    ID = ElementIDCounter++,
                });

                return true;
            }

            if (layoutBox.Node.Name == "img")
            {
                string src = layoutBox.Node.GetAttributeValue("src", string.Empty);
                NeedThisImage image = GetOrCreateImage(src);

                int width = layoutBox.Node.GetAttributeValue("width", image.DownloadedSize.x);
                int height = layoutBox.Node.GetAttributeValue("height", image.DownloadedSize.y);

                var d = layoutBox.Dimensions;
                d.Content.width = width;
                d.Content.height = height;

                Elements.Add(new ElementImage(layoutBox)
                {
                    Url = src,
                    ImageID = image.ID,
                });

                return true;
            }

            if (layoutBox.Node.Name == "input")
            {
                string inputType = layoutBox.Node.GetAttributeValue("type", null);
                if (inputType == "text" || string.IsNullOrWhiteSpace(inputType))
                {
                    string text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.GetAttributeValue("value", ""));

                    Elements.Add(new ElementTextField(layoutBox)
                    {
                        Manager = new InGameComputer.TextInputField(text),
                        ID = ElementIDCounter++,
                        Form = FormStack.PeekOrDefault(),
                        Name = layoutBox.Node.GetAttributeValue("name", string.Empty),
                    });

                    return true;
                }

                if (inputType == "submit")
                {
                    string text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.GetAttributeValue("value", "Submit"));

                    Elements.Add(new ElementButton(layoutBox)
                    {
                        Text = text,
                        ID = ElementIDCounter++,
                        Form = FormStack.PeekOrDefault(),
                    });

                    return true;
                }

                Debug.Log($"Unknown input type \"{inputType}\"");
                return true;
            }

            if (layoutBox.Node.Name == "select")
            {
                List<(string Value, string Label)> values = new();

                int longest = 16;

                foreach (HtmlNode child in layoutBox.Node.ChildNodes)
                {
                    if (child.Name != "option") continue;
                    string value = child.GetAttributeValue("value", null);
                    string label = GeneratorUtils.ConvertHtmlText(child.InnerText).Trim();

                    if (string.IsNullOrWhiteSpace(label)) continue;
                    values.Add((value, label));

                    longest = Mathf.Max(longest, MeasureText.Invoke(label, 8).x);
                }

                Elements.Add(new ElementSelect(layoutBox)
                {
                    Values = values.ToArray(),
                    SelectedIndex = 0,
                    ID = ElementIDCounter++,
                });

                return true;
            }

            if (layoutBox.Node.Name == "form")
            {
                ElementForm newForm = new(layoutBox)
                {
                    ID = ElementIDCounter++,
                    Method = layoutBox.Node.GetAttributeValue("method", "POST"),
                    Target = layoutBox.Node.GetAttributeValue("target", "./"),
                };
                Elements.Add(newForm);
                Forms.Add(newForm);

                FormStack.Push(newForm);

                after = new Action(() => FormStack.Pop());

                return false;
            }

            if (layoutBox.Node.Name == "a")
            {
                LinkIDCounter++;
                LinkStack.Push(layoutBox.Node.GetAttributeValue("href", null));
                after = new Action(() => LinkStack.Pop());
            }

            if (!GeneratorUtils.SupportedTags.Contains(layoutBox.Node.Name, StringComparison.InvariantCulture))
            { Debug.Log($"[{nameof(GraphicsElementGenerator)}/{nameof(Generator)}]: Unknown tag \"{layoutBox.Node.Name}\""); }

            return false;
        }
    }
}
