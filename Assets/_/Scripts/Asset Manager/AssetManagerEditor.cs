using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AssetManager
{
    [System.Serializable]
    public class AssetManagerEditor
    {
#if UNITY_EDITOR

        readonly string AssetsPath = "C:\\Users\\bazsi\\Desktop\\Nothing Assets\\";
        internal void OnGUI(Rect position)
        {
            EditorGUI.TextField(position, AssetsPath);
        }

        internal float GetPropertyHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }
#endif
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(AssetManagerEditor))]
    public class AssetManagerEditorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field == null)
            {
                return base.GetPropertyHeight(property, label);
            }
            var instance = (AssetManagerEditor)field.GetValue(targetObject);
            return instance.GetPropertyHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.LabelField(position, "null");
                EditorGUI.EndProperty();
                return;
            }
            var instance = (AssetManagerEditor)field.GetValue(targetObject);
            EditorGUI.BeginProperty(position, label, property);
            instance.OnGUI(position);
            EditorGUI.EndProperty();
        }
    }

#endif
}