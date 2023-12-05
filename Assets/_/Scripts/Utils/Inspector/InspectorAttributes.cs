using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LinkAttribute : PropertyAttribute
{ public LinkAttribute() { } }

[Serializable]
public class InspectorTimeSpan : UnityEngine.Object, IEquatable<TimeSpan>, IEquatable<InspectorTimeSpan>
{
    TimeSpan v;

    public InspectorTimeSpan(TimeSpan v) => this.v = v;

    public override bool Equals(object other) => v.Equals(other);

    public bool Equals(InspectorTimeSpan other) => other != null && v.Equals(other.v);
    public bool Equals(TimeSpan other) => v.Equals(other);

    public override int GetHashCode() => v.GetHashCode();
    public override string ToString() => v.ToString();

    public static implicit operator TimeSpan(InspectorTimeSpan v) => v.v;
    public static implicit operator InspectorTimeSpan(TimeSpan v) => new(v);

    public static bool operator !=(InspectorTimeSpan a, InspectorTimeSpan b) => !(a == b);
    public static bool operator ==(InspectorTimeSpan a, InspectorTimeSpan b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b);
    }
    public static bool operator !=(InspectorTimeSpan a, TimeSpan b) => !(a == b);
    public static bool operator ==(InspectorTimeSpan a, TimeSpan b)
    {
        if (a == null) return false;
        return a.v.Equals(b);
    }
    public static bool operator !=(TimeSpan a, InspectorTimeSpan b) => !(a == b);
    public static bool operator ==(TimeSpan a, InspectorTimeSpan b)
    {
        if (b == null) return false;
        return a.Equals(b.v);
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TimeSpanAttribute : PropertyAttribute
{
    public TimeSpanAttribute()
    { }
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

    public ThumbnailAttribute(string rendererFieldName)
    {
        this.RenderFieldName = rendererFieldName;
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
class InspectorMessageBox
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
