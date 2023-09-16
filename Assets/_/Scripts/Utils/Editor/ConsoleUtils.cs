using System;
using System.Collections.Generic;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

public class ConsoleUtils
{
    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorGUI.hyperLinkClicked += EditorHyperLinkClicked;
    }

    static void EditorHyperLinkClicked(EditorWindow sender, HyperLinkClickedEventArgs args)
    {
        Dictionary<string, string> data = args.hyperLinkData;

        if (data.TryGetValue("href", out string href))
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out Uri uriResult))
            {
                if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                {
                    string url = uriResult.ToString();
                    Application.OpenURL(url);
                    return;
                }
                else if (uriResult.Scheme == Uri.UriSchemeFile)
                {

                }
                else if (uriResult.Scheme == "object")
                {
                    if (int.TryParse(uriResult.Host, out int instanceId))
                    {
                        EditorGUIUtility.PingObject(instanceId);
                        Selection.activeInstanceID = instanceId;
                        return;
                    }
                }
                else
                {

                }
            }

            if (File.Exists(href))
            {
                if (data.TryGetValue("line", out string _line) && int.TryParse(_line, out int line))
                {
                    bool success;

                    if (data.TryGetValue("col", out string _column) && int.TryParse(_column, out int column))
                    { success = CodeEditor.CurrentEditor.OpenProject(href, line, column); }
                    else
                    { success = CodeEditor.CurrentEditor.OpenProject(href, line); }

                    if (success) return;
                }
            }
        }

        {
            if (data.TryGetValue("object", out string _instanceId) && int.TryParse(_instanceId, out int instanceId))
            {
                EditorGUIUtility.PingObject(instanceId);
                Selection.activeInstanceID = instanceId;
                return;
            }
        }
    }
}
