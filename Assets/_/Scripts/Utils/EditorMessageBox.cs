using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class MessageBoxAttribute : PropertyAttribute
{

}

[System.Serializable]
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
#if UNITY_EDITOR
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
            EditorGUI.HelpBox(position, "null", (UnityEditor.MessageType)MessageType.None);
            return;
        }
        if (!value.Visible) return;

        EditorGUI.HelpBox(position, value.Message, (UnityEditor.MessageType)value.Type);
    }
}
#endif
