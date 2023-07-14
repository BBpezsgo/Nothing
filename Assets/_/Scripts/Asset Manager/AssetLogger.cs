using System;
using System.Diagnostics;

namespace AssetManager
{
    public static class AssetLogger
    {
        public static string Path;
        public const string EOL = "\r\n";

        public static void LogWarning(params string[] v) => LogWarning(string.Join(' ', v));
        public static void LogWarning(string v)
        {
            LogFile("[Warning] " + v);
            UnityEngine.Debug.LogWarning(v);
        }

        public static void LogError(params string[] v) => LogError(string.Join(' ', v));
        public static void LogError(string v)
        {
            LogFile("[Error] " + v);
            UnityEngine.Debug.LogError(v);
        }
        public static void LogError(Exception v)
        {
            LogFile("[Exeption] " + v.ToString());
            UnityEngine.Debug.LogException(v);
        }

        public static void LogFile(string v)
        {
            try
            {
#if !UNITY_EDITOR
            System.IO.File.AppendAllText(Path, v + EOL + GetStack());
#endif
            }
            catch (Exception exception)
            { UnityEngine.Debug.LogException(exception); }
        }

        static string GetStack()
        {
            StackTrace stackTrace = new(3, true);
            StackFrame[] frames = stackTrace.GetFrames();
            string result = "";
            for (int i = 0; i < frames.Length; i++)
            {
                StackFrame frame = frames[i];
                string line = " ";
                if (frame.HasMethod())
                {
                    var method = frame.GetMethod();
                    line += $" {method.Name}";
                }
                if (frame.HasSource())
                {
                    line += $" at {frame.GetFileName()}:{frame.GetFileLineNumber()}:{frame.GetFileColumnNumber()}";
                }
                if (line == " ")
                {
                    continue;
                }
                result += line + EOL;
            }
            return result;
        }
    }
}
