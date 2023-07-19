using UnityEngine;

using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LinkAttribute : PropertyAttribute
{ public LinkAttribute() { } }

[Serializable]
public class UnityTimeSpan : UnityEngine.Object
{
    public TimeSpan v;

    public UnityTimeSpan(TimeSpan v) => this.v = v;

    public bool IsEmpty => this.v.Ticks == 0;

    public override bool Equals(object other) => v.Equals(other);
    public override int GetHashCode() => v.GetHashCode();
    public override string ToString() => v.ToString();

    public static implicit operator TimeSpan(UnityTimeSpan v) => v.v;
    public static implicit operator UnityTimeSpan(TimeSpan v) => new(v);
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ButtonAttribute : PropertyAttribute
{
    public string MethodName { get; }
    public string Label { get; }
    public bool WorksInEditor { get; }
    public bool WorksInPlaytime { get; }
    public ButtonAttribute(string methodName, bool worksInEditor, bool worksInPlaytime)
    {
        MethodName = methodName;
        WorksInEditor = worksInEditor;
        WorksInPlaytime = worksInPlaytime;
        Label = methodName + "()";
    }
    public ButtonAttribute(string methodName, bool worksInEditor, bool worksInPlaytime, string label)
    {
        MethodName = methodName;
        WorksInEditor = worksInEditor;
        WorksInPlaytime = worksInPlaytime;
        Label = label;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ThumbnailAttribute : PropertyAttribute
{
    public string RenderFieldName;

    public ThumbnailAttribute(string v)
    {
        this.RenderFieldName = v;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SuffixAttribute : PropertyAttribute
{
    public string suffix;
    public SuffixAttribute(string suffix) { this.suffix = suffix; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LabelAttribute : PropertyAttribute
{
    public string text;
    public LabelAttribute(string text) { this.text = text; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ProgressBarAttribute : PropertyAttribute
{
    public enum PropertyLabelPosition
    {
        None,
        Left,
        Inside,
    }

    public readonly float Minimum;

    public readonly float Maximum;
    public readonly string MaximumFieldName;

    public PropertyLabelPosition LabelPosition = PropertyLabelPosition.Left;
    public bool ShowPercent = true;
    public bool CanEdit;

    public ProgressBarAttribute(float minimum, float maximum, bool canEdit = true)
    {
        this.Minimum = minimum;
        this.Maximum = maximum;
        this.MaximumFieldName = null;
        this.CanEdit = canEdit;
    }
    public ProgressBarAttribute(float minimum, string maximum, bool canEdit = true)
    {
        this.Minimum = minimum;
        this.Maximum = float.MaxValue;
        this.MaximumFieldName = maximum;
        this.CanEdit = canEdit;
    }
    public ProgressBarAttribute(float maximum, bool canEdit = true)
    {
        this.Minimum = 0f;
        this.Maximum = maximum;
        this.MaximumFieldName = null;
        this.CanEdit = canEdit;
    }
    public ProgressBarAttribute(string maximum, bool canEdit = true)
    {
        this.Minimum = 0f;
        this.Maximum = float.MaxValue;
        this.MaximumFieldName = maximum;
        this.CanEdit = canEdit;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class IfAttribute : PropertyAttribute
{
    public readonly string IfFieldName;
    public IfAttribute(string ifFieldName) => this.IfFieldName = ifFieldName;
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class PlaceholderAttribute : PropertyAttribute
{
    public readonly string Placeholder;
    public PlaceholderAttribute(string placeholder) => this.Placeholder = placeholder;
}

public class ReadOnlyAttribute : PropertyAttribute { }

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MinMaxAttribute : PropertyAttribute
{
    public readonly float Minimum;
    public readonly float Maximum;

    public MinMaxAttribute(float minimum, float maximum)
    {
        this.Minimum = minimum;
        this.Maximum = maximum;
    }
}

public class EditorOnlyAttribute : PropertyAttribute { }

[Serializable]
class MessageBox
{
    public enum MessageType
    {
        None,
        Info,
        Warning,
        Error
    }

    internal MessageType Type;
    internal string Message;
    internal bool Visible;

    internal void Show(string message, MessageType type)
    {
        Visible = true;
        Message = message;
        Type = type;
    }

    internal void Hide()
    {
        Visible = false;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MessageBoxAttribute : PropertyAttribute { }

namespace InspectorDrawers
{
#if UNITY_EDITOR
    using UnityEditor;

    [CustomPropertyDrawer(typeof(LinkAttribute))]
    public class LinkDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.type != "string")
            {
                EditorGUI.HelpBox(position, "[Link] is only valid on string fields!", MessageType.Error);
                return;
            }

            string link = property.stringValue;

            var linkRect = EditorGUI.PrefixLabel(position, label);

            if (link.Length == 0)
            {
                EditorGUI.LabelField(linkRect, "[link]", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState() { textColor = Color.gray } });
                return;
            }

            string finalText = link;
            Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(finalText));

            if (textSize.x > linkRect.width)
            {
                var dddWidth = GUI.skin.label.CalcSize(new GUIContent("...")).x;
                for (int i = finalText.Length; i >= 0; i--)
                {
                    if (finalText.Length <= 5)
                    { break; }
                    textSize = GUI.skin.label.CalcSize(new GUIContent(finalText));
                    if (linkRect.width >= textSize.x + dddWidth)
                    { break; }
                    finalText = finalText[..^1];
                }
                finalText += "...";
            }

            if (EditorGUI.LinkButton(linkRect, finalText))
            {
                System.Diagnostics.Process.Start(link);
            }
        }
    }

    [CustomPropertyDrawer(typeof(UnityTimeSpan))]
    public class TimeSpanPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, label);
            GUI.enabled = false;
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(targetObject) as UnityTimeSpan;
                EditorGUI.TextField(rect, value.v.ToString());
            }
            else
            {
                EditorGUI.TextField(rect, "null");
            }
            GUI.enabled = true;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ButtonAttribute;
            var methodName = attr.MethodName;
            var target = property.serializedObject.targetObject;
            var type = target.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (method == null)
            {
                GUI.Label(position, "Method could not be found.");
                return;
            }
            if (method.GetParameters().Length > 0)
            {
                GUI.Label(position, "Method cannot have parameters.");
                return;
            }
            bool enabledSaved = GUI.enabled;
            if ((!attr.WorksInPlaytime && Application.isPlaying) || (!attr.WorksInEditor && !Application.isPlaying))
            {
                GUI.enabled = false;
            }
            if (GUI.Button(position, attr.Label))
            {
                method.Invoke(target, null);
            }
            GUI.enabled = enabledSaved;
        }
    }

    [CustomPropertyDrawer(typeof(ThumbnailAttribute))]
    public class ThumbnailDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.currentViewWidth;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ThumbnailAttribute attribute = base.attribute as ThumbnailAttribute;

            var value = property.objectReferenceValue;

            if (!string.IsNullOrWhiteSpace(attribute.RenderFieldName))
            {
                var field = property.serializedObject.FindProperty(attribute.RenderFieldName);
                if (field != null)
                { value = field.objectReferenceValue; }
            }

            if (value is Texture2D texture)
            {
                EditorGUI.DrawPreviewTexture(position, texture, null, ScaleMode.ScaleToFit, 0f, -1f, UnityEngine.Rendering.ColorWriteMask.All, 0f);
                return;
            }

            if (value is RenderTexture renderTexture)
            {
                EditorGUI.DrawPreviewTexture(position, renderTexture, null, ScaleMode.ScaleToFit, 0f, -1f, UnityEngine.Rendering.ColorWriteMask.All, 0f);
                return;
            }
        }
    }

    [CustomPropertyDrawer(typeof(SuffixAttribute))]
    public class SuffixPropertyDrawer : PropertyDrawer
    {
        bool CheckProperty(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            { return true; }
            if (property.propertyType == SerializedPropertyType.Integer)
            { return true; }
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            if (CheckProperty(property))
            {
                SuffixAttribute attribute = base.attribute as SuffixAttribute;
                var suffixDimensions = GUI.skin.label.CalcSize(new GUIContent(attribute.suffix));

                bool savedEditorEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.LabelField(new Rect(position.x + (position.width - suffixDimensions.x), position.y, position.width, position.height), attribute.suffix);
                GUI.enabled = savedEditorEnabled;
            }
            else
            {
                Debug.LogWarning("Invalid property type " + property.propertyType.ToString());
            }
        }
    }

    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                LabelAttribute attribute = base.attribute as LabelAttribute;
                if (!IsItBloodyArrayTho(property))
                {
                    label.text = attribute.text;
                }
                else
                {
                    Debug.LogWarning($"{typeof(LabelAttribute).Name}(\"{attribute.text}\") doesn't support arrays ");
                }
                EditorGUI.PropertyField(position, property, label);
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        bool IsItBloodyArrayTho(SerializedProperty property)
        {
            string path = property.propertyPath;
            int idot = path.IndexOf('.');
            if (idot == -1) return false;
            string propName = path[..idot];
            SerializedProperty p = property.serializedObject.FindProperty(propName);
            return p.isArray;
            //CREDITS: https://answers.unity.com/questions/603882/serializedproperty-isnt-being-detected-as-an-array.html
        }
    }

    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarAttributeDrawer : PropertyDrawer
    {
        bool foldout = false;

        static float Maximum(SerializedProperty property, ProgressBarAttribute attribute)
        {
            if (attribute.MaximumFieldName != null)
            {
                UnityEngine.Object targetObject = property.serializedObject.targetObject;
                Type targetObjectClassType = targetObject.GetType();
                FieldInfo field = targetObjectClassType.GetField(attribute.MaximumFieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null) return 0f;

                object value = field.GetValue(targetObject);
                if (value is float @float) return @float;
                if (value is int @int) return (float)@int;
            }
            return attribute.Maximum;
        }
        static float Minimum(SerializedProperty property, ProgressBarAttribute attribute)
        {
            return attribute.Minimum;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!foldout) return EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight * 2f + 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Float &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.HelpBox(position, "[ProgressBar] can be applied only to type float/int !", MessageType.Warning);
                return;
            }

            ProgressBarAttribute progressBarAttribute = (ProgressBarAttribute)attribute;

            string labelText = property.displayName;
            string barText;
            float value;

            static string FormatValue(float value, bool percent)
            {
                if (percent) return Math.Round(value * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
                else return (Math.Round(value * 100f) / 100f).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            var topPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            {
                var max = Maximum(property, progressBarAttribute);
                var min = Minimum(property, progressBarAttribute);

                if (max == 0)
                {
                    value = 0f;
                    barText = FormatValue(value, progressBarAttribute.ShowPercent);
                }
                else if (min >= max)
                {
                    Rect rect = EditorGUI.PrefixLabel(topPosition, label);
                    EditorGUI.HelpBox(rect, $"min >= max", MessageType.Error);
                    return;
                }
                else
                {
                    float interestingMax = max - min;
                    float interestingValue = (property.propertyType == SerializedPropertyType.Integer ? (float)property.intValue : property.floatValue) - min;

                    if (interestingMax == 0f)
                    {
                        value = 0f;
                        barText = FormatValue(value, progressBarAttribute.ShowPercent);
                    }
                    else
                    {
                        value = interestingValue / interestingMax;
                        barText = FormatValue(value, progressBarAttribute.ShowPercent);
                    }
                }
            }

            float barLabelWidth = GUI.skin.label.CalcSize(new GUIContent(barText)).x;
            float barEmptyLabelWidth = GUI.skin.label.CalcSize(new GUIContent("")).x;

            bool labelDrawn = false;

            switch (progressBarAttribute.LabelPosition)
            {
                case ProgressBarAttribute.PropertyLabelPosition.Left:
                    {
                        Rect labelRect = new(topPosition.x, topPosition.y, EditorGUIUtility.labelWidth + 1f, topPosition.height);
                        if (progressBarAttribute.CanEdit)
                        {
                            foldout = EditorGUI.Foldout(labelRect, foldout, labelText, true);
                        }
                        else
                        {
                            EditorGUI.LabelField(labelRect, labelText);
                        }

                        labelDrawn = true;

                        if (topPosition.width - labelRect.width - barLabelWidth > 1f)
                        {
                            Rect barRect = new(labelRect.width + topPosition.x, topPosition.y, topPosition.width - labelRect.width, topPosition.height);
                            EditorGUI.ProgressBar(barRect, value, barText);
                        }
                        else if (topPosition.width - labelRect.width - barEmptyLabelWidth > 1f)
                        {
                            Rect barRect = new(labelRect.width + topPosition.x, topPosition.y, topPosition.width - labelRect.width, topPosition.height);
                            EditorGUI.ProgressBar(barRect, value, string.Empty);
                        }
                        break;
                    }
                case ProgressBarAttribute.PropertyLabelPosition.Inside:
                    {
                        topPosition = EditorGUI.IndentedRect(topPosition);

                        if (progressBarAttribute.CanEdit)
                        {
                            foldout = EditorGUI.Foldout(topPosition, foldout, GUIContent.none);
                        }

                        if (topPosition.width - barLabelWidth > 1f)
                        {
                            EditorGUI.ProgressBar(topPosition, value, labelText + ": " + barText);
                        }
                        else if (topPosition.width - barEmptyLabelWidth > 1f)
                        {
                            EditorGUI.ProgressBar(topPosition, value, string.Empty);
                        }
                        break;
                    }
                case ProgressBarAttribute.PropertyLabelPosition.None:
                default:
                    {
                        topPosition = EditorGUI.IndentedRect(topPosition);

                        if (topPosition.width - barLabelWidth > 1f)
                        {
                            EditorGUI.ProgressBar(topPosition, value, barText);
                        }
                        else if (topPosition.width - barEmptyLabelWidth > 1f)
                        {
                            EditorGUI.ProgressBar(topPosition, value, string.Empty);
                        }
                        break;
                    }
            }

            if (foldout && progressBarAttribute.CanEdit)
            {
                if (labelDrawn)
                { EditorGUI.PropertyField(new Rect(position.x, position.y + 2f + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight), property, true); }
                else
                { EditorGUI.PropertyField(new Rect(position.x, position.y + 2f + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight), property, label, true); }
            }
        }
    }

    [CustomPropertyDrawer(typeof(IfAttribute))]
    public class IfAttributeDrawer : PropertyDrawer
    {
        bool Visible(SerializedProperty property)
        {
            IfAttribute _attribute = (IfAttribute)attribute;
            if (property.name == _attribute.IfFieldName) return true;

            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            Type targetObjectClassType = targetObject.GetType();
            FieldInfo field = targetObjectClassType.GetField(_attribute.IfFieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(targetObject);
                if (value == null) return true;
                if (value is bool @bool) return @bool;
            }

            return true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!Visible(property)) return 0f;
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!Visible(property)) return;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(PlaceholderAttribute))]
    public class PlaceholderAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            if (property.propertyType == SerializedPropertyType.String)
            {
                string placeholder = ((PlaceholderAttribute)attribute).Placeholder;
                if (string.IsNullOrEmpty(property.stringValue))
                {
                    Rect pos = new(position); //GUILayoutUtility.GetLastRect());
                    pos.x += EditorGUIUtility.labelWidth;
                    GUIStyle style = new()
                    {
                        alignment = TextAnchor.UpperLeft,
                        padding = new RectOffset(3, 0, 2, 0),
                        fontStyle = FontStyle.Italic,
                        normal = { textColor = Color.grey },
                    };
                    EditorGUI.LabelField(pos, placeholder, style);
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    [CustomPropertyDrawer(typeof(EditorOnlyAttribute))]
    public class EditorOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = !Application.isPlaying;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }

    [CustomPropertyDrawer(typeof(MinMaxAttribute))]
    public class MinMaxAttributeDrawer : PropertyDrawer
    {
        bool foldout = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!foldout) return EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight * 3f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.HelpBox(position, $"[{nameof(MinMaxAttributeDrawer)}] can be applied only to type {nameof(Vector2)} !", MessageType.Error);
                return;
            }

            MinMaxAttribute attribute = (MinMaxAttribute)this.attribute;

            Rect foldoutPosition = new(position.x, position.y, 4f, EditorGUIUtility.singleLineHeight);
            foldout = EditorGUI.Foldout(foldoutPosition, foldout, GUIContent.none, true);

            Rect topPosition = new(foldoutPosition.xMax, position.y, position.width - foldoutPosition.width, EditorGUIUtility.singleLineHeight);
            var v = property.vector2Value;
            float minValue = Mathf.Clamp(Mathf.Min(v.x, v.y), attribute.Minimum, attribute.Maximum);
            float maxValue = Mathf.Clamp(Mathf.Max(v.x, v.y), attribute.Minimum, attribute.Maximum);
            EditorGUI.MinMaxSlider(topPosition, ref minValue, ref maxValue, attribute.Minimum, attribute.Maximum);
            property.vector2Value = new Vector2(minValue, maxValue);

            /*
            string minText = Math.Round(minValue, 2).ToString();
            string maxText = Math.Round(maxValue, 2).ToString();

            float maxPercent = maxValue / attribute.Maximum;
            float minPercent = minValue / attribute.Maximum;

            var minDimensions = GUI.skin.label.CalcSize(new GUIContent(minText));
            var maxDimensions = GUI.skin.label.CalcSize(new GUIContent(maxText));

            Rect minValueLabel = new(topPosition.x + minPercent * topPosition.width, topPosition.y, topPosition.width, topPosition.height);
            Rect maxValueLabel = new(topPosition.x + maxPercent * topPosition.width - maxDimensions.x, topPosition.y, topPosition.width, topPosition.height);

            EditorGUI.DropShadowLabel(minValueLabel, minText, new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleLeft });
            EditorGUI.DropShadowLabel(maxValueLabel, maxText, new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleLeft });
            */

            if (foldout)
            { EditorGUI.PropertyField(new Rect(position.x, position.y + 2f + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight), property, label, true); }
        }
    }
    [CustomPropertyDrawer(typeof(MessageBox))]
    public class MessageBoxDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            MessageBox value = Value(property);
            if (value == null) return EditorGUIUtility.singleLineHeight;
            return value.Visible ? EditorGUIUtility.singleLineHeight : 0f;
        }

        MessageBox Value(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) return field.GetValue(targetObject) as MessageBox;
            return null;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MessageBox value = Value(property);

            if (value == null)
            {
                EditorGUI.HelpBox(position, "null", (MessageType)MessageType.None);
                return;
            }
            if (!value.Visible) return;

            EditorGUI.HelpBox(position, value.Message, (MessageType)value.Type);
        }
    }

#endif
}
