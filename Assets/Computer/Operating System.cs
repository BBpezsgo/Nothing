using System;
using System.Collections.Generic;
using UnityEngine;

namespace InGameComputer
{
    public class OperatingSystem
    {
        [SerializeField, ReadOnly] internal Computer Hardware;
        [SerializeField, ReadOnly] internal Texture Cursor;

        [SerializeField, ReadOnly, NonReorderable] List<ProgramData> Programs = new();
        [SerializeField, ReadOnly, NonReorderable] List<Program> RunningPrograms = new();

        [Serializable]
        public class ProgramData
        {
            [SerializeField, ReadOnly] public readonly ProgramInstantiator Instantiator;
            [SerializeField, ReadOnly] public readonly string ID;
            [SerializeField, ReadOnly] public readonly Type Type;

            ProgramData(ProgramInstantiator instantiator, string id, Type type)
            {
                Instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
                ID = id ?? throw new ArgumentNullException(nameof(id));
                Type = type ?? throw new ArgumentNullException(nameof(type));
            }

            public static ProgramData Create<T>(ProgramInstantiator<T> instantiator, string id) where T : Program
            {
                return new((p0, p1) => instantiator.Invoke(p0, p1), id, typeof(T));
            }
        }

        public delegate Program ProgramInstantiator(OperatingSystem operatingSystem, string[] arguments);
        public delegate T ProgramInstantiator<T>(OperatingSystem operatingSystem, string[] arguments) where T : Program;

        internal Vector2Int ScreenSize
        {
            get
            {
                if (Hardware.VideoCard != null)
                { _screenSize = Hardware.VideoCard.ScreenSize; }
                return _screenSize;
            }
        }
        Vector2Int _screenSize;

        public Vector2Int TransformPoint(Vector2 point) => Vector2Int.RoundToInt(point * ScreenSize);

        public Vector2Int TransformedMousePosition => TransformPoint(Hardware.Monitor.CapturedMousePosition);

        public KeyboardEventSystem Keyboard => Hardware.KeyboardEventSystem;
        public MouseEventSystem Mouse => Hardware.MouseEventSystem;

        public bool IsPointerOnRect(RectInt rect)
        {
            if (Mouse.IsButtonDown(MouseButton.Left))
            { return rect.Contains(TransformedMousePosition); }
            else if (Mouse.IsButtonHold(MouseButton.Left))
            { return rect.Contains(TransformPoint(Mouse.PressedAt(MouseButton.Left))); }
            else if (Mouse.IsButtonUp(MouseButton.Left))
            { return rect.Contains(TransformPoint(Mouse.PressedAt(MouseButton.Left))); }
            else
            { return rect.Contains(TransformedMousePosition); }
        }

        public OperatingSystem(Computer computer, string autorun = null)
        {
            Hardware = computer;

            /*
            Programs.Add(ProgramData.Create((p0, p1) => new ProgramWebBrowser(p0, p1)
            {
                Rect = new(0, 0, ScreenSize.x, ScreenSize.y),
            }, "web"));
            */
            Programs.Add(ProgramData.Create((p0, p1) => new ProgramTerminal(p0, p1)
            {
                Rect = new(0, 0, ScreenSize.x, ScreenSize.y),
            }, "cmd"));

            StartProgram(GetProgram<ProgramTerminal>(), autorun);
        }

        public void StartProgram(ProgramData program, params string[] arguments)
        {
            foreach (Program runningProgram in RunningPrograms)
            { runningProgram.Destroy(); }
            RunningPrograms.Clear();

            Program programInstance = program.Instantiator.Invoke(this, arguments);
            RunningPrograms.Add(programInstance);
            programInstance.Start();
        }

        public void StartProgram(string id, params string[] arguments) => StartProgram(GetProgram(id), arguments);
        public void StartProgram<T>(params string[] arguments) where T : Program => StartProgram(GetProgram<T>(), arguments);

        public ProgramData GetProgram(string id) => TryGetProgram(id, out ProgramData program) ? program : null;
        public bool TryGetProgram(string id, out ProgramData program)
        {
            foreach (var _program in Programs)
            {
                if (_program.ID != id) continue;
                program = _program;
                return true;
            }
            program = null;
            return false;
        }

        public ProgramData GetProgram<T>() where T : Program => TryGetProgram<T>(out ProgramData program) ? program : default;
        public bool TryGetProgram<T>(out ProgramData program) where T : Program
        {
            foreach (var _program in Programs)
            {
                if (_program.Type != typeof(T)) continue;
                program = _program;
                return true;
            }
            program = default;
            return false;
        }

        public void Update()
        {
            using (ProfilerMarkers.Computer.Auto())
            {
                Mouse.InvokeEvents();

                foreach (Program program in RunningPrograms)
                { program.Tick(); }

                if (Hardware.VideoCard != null)
                {
                    using (Hardware.VideoCard.Render())
                    {
                        Hardware.VideoCard.Clear();

                        foreach (Program program in RunningPrograms)
                        { program.Draw(); }

                        Hardware.VideoCard.DrawTexture(new RectInt(TransformedMousePosition, new Vector2Int(8, 8)), Cursor, Color.white);
                    }
                }
            }

            using (ProfilerMarkers.IO.Auto())
            {
                Mouse.Step();
                Keyboard.Step();
            }
        }

        public void Tick()
        {
            using (ProfilerMarkers.IO.Auto())
            {
                Mouse.Tick();
                Keyboard.Tick();
            }
        }
    }
}
