using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

namespace MogulTech.Utilities
{
    public static class EditorCoroutines
    {
        public class Coroutine
        {
            public IEnumerator enumerator;
            public System.Action<bool> OnUpdate;
            public List<IEnumerator> history = new();
            public bool waitForGUI = false;
            public Action Repaint;

            public void OnGUI() => waitForGUI = false;
        }

        public class WaitForGUI
        {

        }

        static readonly List<Coroutine> coroutines = new();

        public static Coroutine Execute(IEnumerator enumerator, System.Action<bool> OnUpdate = null)
        {
            if (coroutines.Count == 0)
            {
                EditorApplication.update += Update;
            }
            var coroutine = new Coroutine { enumerator = enumerator, OnUpdate = OnUpdate };
            coroutines.Add(coroutine);
            return coroutine;
        }

        static void Update()
        {
            for (int i = 0; i < coroutines.Count; i++)
            {
                Coroutine coroutine = coroutines[i];
                if (coroutine.waitForGUI)
                {
                    coroutine.Repaint?.Invoke();
                }
                try
                {
                    bool done = !coroutine.enumerator.MoveNext();
                    if (done)
                    {
                        if (coroutine.history.Count == 0)
                        {
                            coroutines.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            done = false;
                            coroutine.enumerator = coroutine.history[^1];
                            coroutine.history.RemoveAt(coroutine.history.Count - 1);
                        }
                    }
                    else
                    {
                        if (coroutine.enumerator.Current is null)
                        {

                        }
                        else if (coroutine.enumerator.Current is IEnumerator enumerator)
                        {
                            coroutine.history.Add(coroutine.enumerator);
                            coroutine.enumerator = enumerator;
                        }
                        else if (coroutine.enumerator.Current is WaitForGUI)
                        {
                            coroutine.waitForGUI = true;
                        }
                    }
                    coroutine.OnUpdate?.Invoke(done);
                }
                catch (System.Exception error)
                {
                    UnityEngine.Debug.LogException(error);
                    coroutines.RemoveAt(i);
                    i--;
                }
            }
            if (coroutines.Count == 0) EditorApplication.update -= Update;
        }

        internal static void StopAll()
        {
            coroutines.Clear();
            EditorApplication.update -= Update;
        }
    }
}