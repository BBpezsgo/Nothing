using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Components;
using Game.Managers;
using InputUtils;
using Unity.Netcode;
using UnityEngine;

namespace InGameComputer
{
    public class Computer : NetworkBehaviour, INeedDirectWorldCursor, IDamagable
    {
        [SerializeField] float HP;
        [SerializeField] GameObject DestroyEffect;

        [SerializeField, ReadOnly] internal OperatingSystem OperatingSystem;

        [SerializeField, ReadOnly] public MouseEventSystem MouseEventSystem;
        [SerializeField, ReadOnly] public KeyboardEventSystem KeyboardEventSystem;

        [SerializeField, ReadOnly] CameraLockable ViewCamera;

        [SerializeField] GameObject MonitorObject;
        [SerializeField, ReadOnly] internal Monitor Monitor;
        [SerializeField, ReadOnly] internal VideoCard VideoCard;

        [SerializeField] Transform MonitorJoint;

        [SerializeField] internal TextAsset[] Assets;
        [SerializeField] Texture2D Cursor;
        [SerializeField] string Autorun = "web https://google.com/";

        PriorityKey KeyEsc;

        public int CursorPriority => 1;

        public bool IsControlling => CameraController.Instance.LockOn == ViewCamera;

        void Start()
        {
            WorldCursorManager.Instance.Register(this);

            KeyEsc = new PriorityKey(KeyCode.Escape, 1, () => IsControlling);
            KeyEsc.OnDown += OnKeyEsc;

            VideoCard = MonitorObject.GetComponentInChildren<VideoCard>(false);
            Monitor = MonitorObject.GetComponentInChildren<Monitor>(false);
            ViewCamera = MonitorObject.GetComponentInChildren<CameraLockable>(false);

            MouseEventSystem = new MouseEventSystem(Monitor);
            KeyboardEventSystem = new KeyboardEventSystem(this);
            OperatingSystem = new OperatingSystem(this, Autorun)
            {
                Cursor = Cursor,
            };

            VideoCard.AttachHardware(this);

            if (MonitorJoint != null && MonitorObject.TryGetComponentInChildren(out JointThingy joint))
            {
                joint.Target = MonitorJoint;
                joint.Refresh();
            }
        }

        void OnKeyEsc()
        {
            CameraController.Instance.TryOverrideLock(null, CameraLockable.Priorities.ComputerView);
        }

        void Update()
        {
            OperatingSystem.Tick();
        }

        void FixedUpdate()
        {
            OperatingSystem.Update();
        }

        public bool OnWorldCursor(Vector3 worldPosition)
        {
            if (!Input.GetKey(KeyCode.LeftControl)) return false;
            if (IsControlling) return false;
            CameraController.Instance.TryOverrideLock(ViewCamera, CameraLockable.Priorities.ComputerView);
            return true;
        }

        public void Damage(float ammount)
        {
            HP -= ammount;
            if (HP <= 0)
            { Explode(); }
        }

        void Explode()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (DestroyEffect != null)
            { GameObject.Instantiate(DestroyEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            GameObject.Destroy(gameObject);
        }
    }

    [Serializable]
    public class MouseEventSystem
    {
        public delegate void MouseDownEvent(Vector2 position);
        public delegate void MouseUpEvent(Vector2 position, Vector2 pressedPosition);

        [System.Diagnostics.DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        [Serializable]
        public class ComputerMouseEventSubsystem
        {
            enum MouseStage
            {
                None,
                Down,
                DownCaptured,
                Hold,
                Up,
                UpCaptured,
            }

            [SerializeField, ReadOnly] int MouseButton;

            [SerializeField, ReadOnly] Monitor ComputerScreen;

            [SerializeField, ReadOnly] MouseStage Stage;

            public event MouseDownEvent OnDown;
            public event MouseUpEvent OnUp;

            public bool IsDown => Stage == MouseStage.Down;
            public bool IsHold => Stage == MouseStage.Hold;
            public bool IsUp => Stage == MouseStage.Up;

            public Vector2 PressedAt { get; private set; }

            public ComputerMouseEventSubsystem(int mouseButton, Monitor computerScreen)
            {
                MouseButton = mouseButton;
                ComputerScreen = computerScreen;
            }

            public void InvokeEvents()
            {
                if (!ComputerScreen.IsMouseOnScreen)
                { return; }

                switch (Stage)
                {
                    case MouseStage.Down:
                        OnDown?.Invoke(PressedAt);
                        break;
                    case MouseStage.Up:
                        OnUp?.Invoke(ComputerScreen.CapturedMousePosition, PressedAt);
                        break;
                    default:
                        break;
                }
            }

            public void Tick()
            {
                if (!ComputerScreen.IsMouseOnScreen)
                {
                    Stage = MouseStage.None;
                    return;
                }

                if (Input.GetMouseButtonDown(MouseButton) &&
                    (Stage == MouseStage.None))
                {
                    Stage = MouseStage.Down;
                    PressedAt = ComputerScreen.CapturedMousePosition;
                }

                if (Input.GetMouseButton(MouseButton) &&
                    (Stage == MouseStage.DownCaptured || Stage == MouseStage.Hold))
                {
                    Stage = MouseStage.Hold;
                }

                if (Input.GetMouseButtonUp(MouseButton) &&
                    (Stage == MouseStage.DownCaptured || Stage == MouseStage.Hold))
                {
                    Stage = MouseStage.Up;
                }
            }

            public void Step()
            {
                if (Stage == MouseStage.Down)
                { Stage = MouseStage.DownCaptured; }
                else if (Stage == MouseStage.Up)
                { Stage = MouseStage.UpCaptured; }
                else if (Stage == MouseStage.UpCaptured)
                { Stage = MouseStage.None; }
                else if (Stage == MouseStage.DownCaptured)
                { Stage = MouseStage.Up; }
            }

            private string GetDebuggerDisplay()
            {
                string buttonName = MouseButton switch
                {
                    0 => "Left",
                    1 => "Right",
                    _ => "Unknown",
                };
                return $"{buttonName} button {{ Stage: {Stage}, PressedAt: {PressedAt} }}";
            }
        }

        [SerializeField, ReadOnly] Monitor ComputerScreen;

        [SerializeField, ReadOnly] public ComputerMouseEventSubsystem LeftMouse;
        [SerializeField, ReadOnly] public ComputerMouseEventSubsystem RightMouse;

        enum MouseCaptureStage
        {
            OffScreen,
            Entered,
            OnScreen,
            Left,
        }

        [SerializeField, ReadOnly] MouseCaptureStage CaptureStage;

        public Vector2 ScrollDelta;

        public MouseEventSystem(Monitor computerScreen)
        {
            ComputerScreen = computerScreen;
            LeftMouse = new ComputerMouseEventSubsystem(MouseButton.Left, computerScreen);
            RightMouse = new ComputerMouseEventSubsystem(MouseButton.Right, computerScreen);
        }

        public void Tick()
        {
            ComputerScreen.CaptureMousePosition();
            LeftMouse.Tick();
            RightMouse.Tick();

            if (ComputerScreen.IsMouseOnScreen)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    ScrollDelta.x += Input.mouseScrollDelta.y;
                }
                else
                {
                    ScrollDelta.y += Input.mouseScrollDelta.y;
                }
            }
        }

        public void InvokeEvents()
        {
            LeftMouse.InvokeEvents();
            RightMouse.InvokeEvents();
        }

        public void Step()
        {
            if (ComputerScreen.IsMouseOnScreen)
            {
                CaptureStage = CaptureStage switch
                {
                    MouseCaptureStage.OffScreen => MouseCaptureStage.Entered,
                    MouseCaptureStage.Entered => MouseCaptureStage.OnScreen,
                    MouseCaptureStage.Left => MouseCaptureStage.Entered,
                    _ => CaptureStage,
                };
            }
            else
            {
                CaptureStage = CaptureStage switch
                {
                    MouseCaptureStage.Entered => MouseCaptureStage.Left,
                    MouseCaptureStage.Left => MouseCaptureStage.OffScreen,
                    MouseCaptureStage.OnScreen => MouseCaptureStage.Left,
                    _ => CaptureStage,
                };
            }

            if (CaptureStage == MouseCaptureStage.Entered)
            {
                Cursor.visible = false;
            }
            else if (CaptureStage == MouseCaptureStage.Left)
            {
                Cursor.visible = true;
            }

            LeftMouse.Step();
            RightMouse.Step();
            ScrollDelta = Vector2.zero;
        }

        public bool IsButtonDown(int button) => button switch
        {
            MouseButton.Left => LeftMouse.IsDown,
            MouseButton.Right => RightMouse.IsDown,
            _ => throw new System.Exception($"Unknown button {button}"),
        };

        public bool IsButtonHold(int button) => button switch
        {
            MouseButton.Left => LeftMouse.IsHold,
            MouseButton.Right => RightMouse.IsHold,
            _ => throw new System.Exception($"Unknown button {button}"),
        };

        public bool IsButtonUp(int button) => button switch
        {
            MouseButton.Left => LeftMouse.IsUp,
            MouseButton.Right => RightMouse.IsUp,
            _ => throw new System.Exception($"Unknown button {button}"),
        };

        public Vector2 PressedAt(int button) => button switch
        {
            MouseButton.Left => LeftMouse.PressedAt,
            MouseButton.Right => RightMouse.PressedAt,
            _ => throw new System.Exception($"Unknown button {button}"),
        };
    }

    [Serializable]
    public class KeyboardEventSystem
    {
        public delegate void KeyboardEvent(KeyCode key);

        static readonly KeyCode[] KeyCodes = Enum.GetValues(typeof(KeyCode)).OfType<object>().Select(o => (KeyCode)o).ToArray();

        static readonly KeyCode[] OkayKeyCodes = new KeyCode[]
        {
            KeyCode.Backspace,
            KeyCode.Delete,
            KeyCode.Tab,
            KeyCode.Clear,
            KeyCode.Return,
            KeyCode.Pause,
            KeyCode.Escape,
            KeyCode.Space,
            KeyCode.Keypad0,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6,
            KeyCode.Keypad7,
            KeyCode.Keypad8,
            KeyCode.Keypad9,
            KeyCode.KeypadPeriod,
            KeyCode.KeypadDivide,
            KeyCode.KeypadMultiply,
            KeyCode.KeypadMinus,
            KeyCode.KeypadPlus,
            KeyCode.KeypadEnter,
            KeyCode.KeypadEquals,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.RightArrow,
            KeyCode.LeftArrow,
            KeyCode.Insert,
            KeyCode.Home,
            KeyCode.End,
            KeyCode.PageUp,
            KeyCode.PageDown,
            KeyCode.F1,
            KeyCode.F2,
            KeyCode.F3,
            KeyCode.F4,
            KeyCode.F5,
            KeyCode.F6,
            KeyCode.F7,
            KeyCode.F8,
            KeyCode.F9,
            KeyCode.F10,
            KeyCode.F11,
            KeyCode.F12,
            KeyCode.F13,
            KeyCode.F14,
            KeyCode.F15,
            KeyCode.Alpha0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
            KeyCode.Exclaim,
            KeyCode.DoubleQuote,
            KeyCode.Hash,
            KeyCode.Dollar,
            KeyCode.Percent,
            KeyCode.Ampersand,
            KeyCode.Quote,
            KeyCode.LeftParen,
            KeyCode.RightParen,
            KeyCode.Asterisk,
            KeyCode.Plus,
            KeyCode.Comma,
            KeyCode.Minus,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.Colon,
            KeyCode.Semicolon,
            KeyCode.Less,
            KeyCode.Equals,
            KeyCode.Greater,
            KeyCode.Question,
            KeyCode.At,
            KeyCode.LeftBracket,
            KeyCode.Backslash,
            KeyCode.RightBracket,
            KeyCode.Caret,
            KeyCode.Underscore,
            KeyCode.BackQuote,
            KeyCode.A,
            KeyCode.B,
            KeyCode.C,
            KeyCode.D,
            KeyCode.E,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.I,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.M,
            KeyCode.N,
            KeyCode.O,
            KeyCode.P,
            KeyCode.Q,
            KeyCode.R,
            KeyCode.S,
            KeyCode.T,
            KeyCode.U,
            KeyCode.V,
            KeyCode.W,
            KeyCode.X,
            KeyCode.Y,
            KeyCode.Z,
            KeyCode.LeftCurlyBracket,
            KeyCode.Pipe,
            KeyCode.RightCurlyBracket,
            KeyCode.Tilde,
            KeyCode.Numlock,
            KeyCode.CapsLock,
            KeyCode.ScrollLock,
            KeyCode.RightShift,
            KeyCode.LeftShift,
            KeyCode.RightControl,
            KeyCode.LeftControl,
            KeyCode.RightAlt,
            KeyCode.LeftAlt,
            KeyCode.LeftMeta,
            KeyCode.LeftCommand,
            KeyCode.LeftApple,
            KeyCode.LeftWindows,
            KeyCode.RightMeta,
            KeyCode.RightCommand,
            KeyCode.RightApple,
            KeyCode.RightWindows,
            KeyCode.AltGr,
            KeyCode.Help,
            KeyCode.Print,
            KeyCode.SysReq,
            KeyCode.Break,
            KeyCode.Menu,
        };

        readonly KeyStage[] KeyState;
        readonly Computer Computer;

        enum KeyStage : byte
        {
            None,
            Down,
            DownCaptured,
            Hold,
            Up,
            UpCaptured,
        }

        public event KeyboardEvent OnDown;

        public KeyboardEventSystem(Computer computer)
        {
            Computer = computer;
            KeyState = new KeyStage[KeyCodes.Length];
        }

        public void Tick()
        {

        }

        public void Step()
        {
            if (!Computer.IsControlling) return;

            for (int i = 0; i < KeyCodes.Length; i++)
            {
                KeyCode code = KeyCodes[i];
                KeyStage state = KeyState[i];
                bool isDown = Input.GetKey(code) || Input.GetKeyDown(code);

                if (isDown)
                {
                    if (state == KeyStage.Down)
                    {
                        OnDown?.Invoke(code);
                    }

                    KeyState[i] = state switch
                    {
                        KeyStage.None => KeyStage.Down,
                        KeyStage.Down => KeyStage.DownCaptured,
                        KeyStage.DownCaptured => KeyStage.Hold,
                        KeyStage.Hold => KeyStage.Hold,
                        KeyStage.Up => KeyStage.Down,
                        KeyStage.UpCaptured => KeyStage.Down,
                        _ => state,
                    };
                }
                else
                {
                    KeyState[i] = state switch
                    {
                        KeyStage.None => KeyStage.None,
                        KeyStage.Down => KeyStage.Up,
                        KeyStage.DownCaptured => KeyStage.Up,
                        KeyStage.Hold => KeyStage.Up,
                        KeyStage.Up => KeyStage.UpCaptured,
                        KeyStage.UpCaptured => KeyStage.None,
                        _ => state,
                    };
                }
            }
        }

        public bool IsDown(KeyCode code)
        {
            KeyStage state = KeyState[Array.IndexOf(KeyCodes, code)];
            return state == KeyStage.Down;
        }

        public bool IsHold(KeyCode code)
        {
            KeyStage state = KeyState[Array.IndexOf(KeyCodes, code)];
            return state == KeyStage.Hold;
        }

        public bool IsUp(KeyCode code)
        {
            KeyStage state = KeyState[Array.IndexOf(KeyCodes, code)];
            return state == KeyStage.Up;
        }
    }

    internal readonly struct ProfilerMarkers
    {
        internal static readonly Unity.Profiling.ProfilerMarker Computer = new("Computer");
        internal static readonly Unity.Profiling.ProfilerMarker IO = new("Computer.IO");
    }

    static class KeyboardUtils
    {
        public static char? KeyToChar(KeyCode code) => code switch
        {
            KeyCode.None => null,
            KeyCode.Backspace => null,
            KeyCode.Delete => null,
            KeyCode.Tab => '\t',
            KeyCode.Clear => null,
            KeyCode.Return => '\n',
            KeyCode.Pause => null,
            KeyCode.Escape => null,
            KeyCode.Space => ' ',
            KeyCode.Keypad0 => '0',
            KeyCode.Keypad1 => '1',
            KeyCode.Keypad2 => '2',
            KeyCode.Keypad3 => '3',
            KeyCode.Keypad4 => '4',
            KeyCode.Keypad5 => '5',
            KeyCode.Keypad6 => '6',
            KeyCode.Keypad7 => '7',
            KeyCode.Keypad8 => '8',
            KeyCode.Keypad9 => '9',
            KeyCode.KeypadPeriod => '.',
            KeyCode.KeypadDivide => '/',
            KeyCode.KeypadMultiply => '*',
            KeyCode.KeypadMinus => '-',
            KeyCode.KeypadPlus => '+',
            KeyCode.KeypadEnter => '\r',
            KeyCode.KeypadEquals => '=',
            KeyCode.Alpha0 => '0',
            KeyCode.Alpha1 => '1',
            KeyCode.Alpha2 => '2',
            KeyCode.Alpha3 => '3',
            KeyCode.Alpha4 => '4',
            KeyCode.Alpha5 => '5',
            KeyCode.Alpha6 => '6',
            KeyCode.Alpha7 => '7',
            KeyCode.Alpha8 => '8',
            KeyCode.Alpha9 => '9',
            KeyCode.Exclaim => '!',
            KeyCode.DoubleQuote => '"',
            KeyCode.Hash => '#',
            KeyCode.Dollar => '$',
            KeyCode.Percent => '%',
            KeyCode.Ampersand => '&',
            KeyCode.Quote => '\'',
            KeyCode.LeftParen => '(',
            KeyCode.RightParen => ')',
            KeyCode.Asterisk => '*',
            KeyCode.Plus => '+',
            KeyCode.Comma => ',',
            KeyCode.Minus => '-',
            KeyCode.Period => '.',
            KeyCode.Slash => '/',
            KeyCode.Colon => ':',
            KeyCode.Semicolon => ';',
            KeyCode.Less => '<',
            KeyCode.Equals => '=',
            KeyCode.Greater => '>',
            KeyCode.Question => '?',
            KeyCode.At => '@',
            KeyCode.LeftBracket => '[',
            KeyCode.Backslash => '\\',
            KeyCode.RightBracket => ']',
            KeyCode.Caret => '^',
            KeyCode.Underscore => '_',
            KeyCode.BackQuote => '`',
            KeyCode.A => 'a',
            KeyCode.B => 'b',
            KeyCode.C => 'c',
            KeyCode.D => 'd',
            KeyCode.E => 'e',
            KeyCode.F => 'f',
            KeyCode.G => 'g',
            KeyCode.H => 'h',
            KeyCode.I => 'i',
            KeyCode.J => 'j',
            KeyCode.K => 'k',
            KeyCode.L => 'l',
            KeyCode.M => 'm',
            KeyCode.N => 'n',
            KeyCode.O => 'o',
            KeyCode.P => 'p',
            KeyCode.Q => 'q',
            KeyCode.R => 'r',
            KeyCode.S => 's',
            KeyCode.T => 't',
            KeyCode.U => 'u',
            KeyCode.V => 'v',
            KeyCode.W => 'w',
            KeyCode.X => 'x',
            KeyCode.Y => 'y',
            KeyCode.Z => 'z',
            KeyCode.LeftCurlyBracket => '{',
            KeyCode.Pipe => '|',
            KeyCode.RightCurlyBracket => '}',
            KeyCode.Tilde => '~',
            _ => null,
        };
    }

    class TextInputField
    {
        public string Buffer;
        public int CursorPosition;

        public TextInputField()
        {
            Buffer = string.Empty;
            CursorPosition = 0;
        }

        public TextInputField(string value)
        {
            Buffer = value;
            CursorPosition = value?.Length ?? 0;
        }

        public void FeedKey(KeyCode key)
        {
            if (key == KeyCode.Backspace)
            {
                if (Buffer.Length > 0)
                {
                    if (CursorPosition == 0)
                    {

                    }
                    else if (CursorPosition == Buffer.Length)
                    {
                        Buffer = Buffer[..^1];
                    }
                    else
                    {
                        Buffer = Buffer[..(CursorPosition - 1)] + Buffer[(CursorPosition)..];
                    }
                }
                MoveCursor(-1);
                return;
            }

            if (key == KeyCode.Delete)
            {
                if (Buffer.Length > 0)
                {
                    if (CursorPosition == 0)
                    {
                        Buffer = Buffer[1..];
                    }
                    else if (CursorPosition == Buffer.Length)
                    {

                    }
                    else
                    {
                        Buffer = Buffer[..(CursorPosition)] + Buffer[(CursorPosition + 1)..];
                    }
                }
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            { return; }

            if (key == KeyCode.LeftArrow)
            {
                MoveCursor(-1);
                return;
            }

            if (key == KeyCode.RightArrow)
            {
                MoveCursor(1);
                return;
            }

            char? possibleChar = KeyboardUtils.KeyToChar(key);

            if (possibleChar.HasValue)
            {
                char @char = possibleChar.Value;

                if (CursorPosition == 0)
                { Buffer = @char + Buffer; }
                else if (CursorPosition == Buffer.Length)
                { Buffer += @char; }
                else
                { Buffer = Buffer.Insert(CursorPosition, @char.ToString()); }

                MoveCursor(1);
                return;
            }
        }

        void MoveCursor(int offset)
        { CursorPosition = Mathf.Clamp(CursorPosition + offset, 0, Buffer.Length); }

        public void Clear()
        {
            Buffer = string.Empty;
            CursorPosition = 0;
        }

        internal void SetCursor(int clickedCharacter)
        { CursorPosition = Mathf.Clamp(clickedCharacter, 0, Buffer.Length); }
    }

    [Serializable]
    public abstract class Program
    {
        protected readonly OperatingSystem Computer;
        protected VideoCard VideoCard => Computer.Hardware.VideoCard;
        public RectInt Rect
        {
            get => Layout.TotalSpace;
            set => Layout.TotalSpace = value;
        }
        protected Layout Layout;
        protected readonly string[] Arguments;

        public abstract string ID { get; }

        public Program(OperatingSystem computer, string[] arguments)
        {
            this.Computer = computer;
            this.Layout = new Layout(new RectInt(0, 0, 4, 4), 8);
            this.Arguments = arguments ?? new string[0];
        }

        public virtual void Draw() { }

        public virtual void Tick() { }

        public virtual void Start() { }

        protected void DrawText(string text, int fontSize, Color color)
        {
            VideoCard.DrawText(text, Layout.GetNextRect(VideoCard.TextSize(text, fontSize).x), fontSize, color);
        }

        public virtual void Destroy() { }
    }

    public class ProgramTerminal : Program
    {
        readonly List<string> Lines;
        readonly TextInputField InputField;

        public override string ID => "cmd";

        Blinker Blinker;

        int FontSize = 8;
        int LineSpacing = 1;

        public ProgramTerminal(OperatingSystem computer, string[] arguments) : base(computer, arguments)
        {
            computer.Keyboard.OnDown += OnKeyDown;
            computer.Mouse.LeftMouse.OnUp += OnClick;

            InputField = new TextInputField();
            Lines = new List<string>();

            Blinker = new();
        }

        public override void Start()
        {
            if (Arguments.Length > 0)
            { Submit(string.Join(' ', Arguments)); }
        }

        void OnClick(Vector2 position, Vector2 pressedPosition)
        {
            if (InputField.Buffer.Length == 0) return;

            Vector2Int screenPosition = Computer.TransformPoint(position);
            Vector2Int pressedScreenPosition = Computer.TransformPoint(pressedPosition);

            if ((screenPosition - pressedScreenPosition).magnitude > 2) return;

            int characterWidth = Computer.Hardware.VideoCard.SelectedFont.CharacterSize(FontSize).x;

            int avaliableLines = Rect.height / FontSize - 1;

            int startLineIndex = Mathf.Max(0, Lines.Count - avaliableLines);

            int visibleLines = Lines.Count - startLineIndex;
            int linesHeight = visibleLines * FontSize * LineSpacing;

            RectInt inputRect = new(new Vector2Int(Rect.x, Rect.y + linesHeight), new Vector2Int(Rect.width, FontSize));

            if (!inputRect.Contains(pressedScreenPosition)) return;

            int clickedCharacter = (pressedScreenPosition.x - inputRect.x) / characterWidth;
            InputField.SetCursor(clickedCharacter);
        }

        void OnKeyDown(KeyCode key)
        {
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                string input = InputField.Buffer;
                InputField.Clear();
                Submit(input);
                return;
            }

            InputField.FeedKey(key);
        }

        static (string Command, string[] Arguments) Parse(string input)
        {

            string[] parts = input.Split(' ', '\t');
            if (parts.Length == 0) return (null, null);

            string command = parts[0];
            List<string> parameters = new();
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i])) continue;
                parameters.Add(parts[i]);
            }

            return (command, parameters.ToArray());
        }

        void Submit(string input)
        {
            var parsed = Parse(input);
            if (parsed.Command == null) return;

            Lines.Add(input);

            switch (parsed.Command)
            {
                case "web":
                    {
                        if (parsed.Arguments.Length < 1) break;
                        Computer.StartProgram("web", parsed.Arguments);
                        break;
                    }
                default:
                    break;
            }
        }

        public override void Draw()
        {
            int avaliableLines = Rect.height / FontSize - 1;

            int characterWidth = Computer.Hardware.VideoCard.SelectedFont.CharacterSize(FontSize).x;

            int startLineIndex = Mathf.Max(0, Lines.Count - avaliableLines);

            int visibleLines = Lines.Count - startLineIndex;
            int linesHeight = visibleLines * FontSize * LineSpacing;

            for (int i = startLineIndex; i < Lines.Count; i++)
            {
                string line = Lines[i];
                Computer.Hardware.VideoCard.DrawText(line, new RectInt(Rect.x, Rect.y + ((i - startLineIndex) * FontSize * LineSpacing), Rect.width, FontSize), FontSize, Color.white);
            }

            Vector2Int inputPosition = new(Rect.x, Rect.y + linesHeight);
            Computer.Hardware.VideoCard.DrawText(InputField.Buffer, inputPosition, FontSize, Color.white);

            if (Blinker.IsOn)
            { Computer.Hardware.VideoCard.DrawLine(new Vector2Int(inputPosition.x + (characterWidth * InputField.CursorPosition), inputPosition.y + FontSize - 1), new Vector2Int(inputPosition.x + (characterWidth * InputField.CursorPosition) + characterWidth, inputPosition.y + FontSize - 1), Color.white); }
        }

        public override void Tick()
        {
            Blinker.Tick();
        }
    }

    public class Layout
    {
        int x;
        int y;
        public RectInt TotalSpace;
        public int LineHeight;

        public int X
        {
            get => x;
        }
        public int Y
        {
            get => y;
        }
        public Vector2Int Position => new(x, y);

        public Layout(RectInt totalSpace, int lineHeight)
        {
            TotalSpace = totalSpace;
            LineHeight = lineHeight;
            x = 0;
            y = 0;
        }

        /// <summary>
        /// Finishes the current line
        /// </summary>
        public void Break()
        {
            x = 0;
            y += LineHeight;
        }

        /// <summary>
        /// Finishes the current line if it has any content (<see cref="X"/> &gt; 0)
        /// </summary>
        public void Return()
        {
            if (x == 0) return;
            x = 0;
            y += LineHeight;
        }

        public void Push(int width)
        {
            x += width;
            if (x >= TotalSpace.width)
            {
                x = 0;
                y += LineHeight;
            }
        }

        public void Reset()
        {
            x = 0;
            y = 0;
        }

        public RectInt GetNextRect(int widthNeed)
        {
            if (x > 0 && x + widthNeed > TotalSpace.width)
            {
                x = 0;
                y += LineHeight;
            }
            RectInt result = new(x, y, widthNeed, LineHeight);
            x += widthNeed;
            return result;
        }
    }

    public struct Blinker
    {
        int CurrentTime;

        public void Tick()
        {
            CurrentTime++;
        }

        public readonly bool IsOn => CurrentTime % 14 < 7;
    }

    /*
    public class PageGenerator
    {
        [System.Diagnostics.DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public abstract class Element
        {
            public Vector2Int ContentPosition;
            public Vector2Int ContentSize;
            public SidesInt Padding;
            public SidesInt Border;
            public SidesInt Margin;

            public RectInt ContentRect;
            public RectInt PaddingRect;
            public RectInt BorderRect;
            public RectInt MarginRect;

            public Vector2Int PaddingPosition;
            public Vector2Int BorderPosition;
            public Vector2Int MarginPosition;

            public abstract ElementKind Kind { get; }

            public void RecalculateRectangles()
            {
                ContentRect = new(ContentPosition, ContentSize);
                PaddingRect = ContentRect.Extend(Padding);
                BorderRect = PaddingRect.Extend(Border);
                MarginRect = BorderRect.Extend(Margin);

                PaddingPosition = ContentPosition - Padding.TopLeft;
                BorderPosition = PaddingPosition - Border.TopLeft;
                MarginPosition = BorderPosition - Margin.TopLeft;
            }

            string GetDebuggerDisplay() => ToString();

            public override string ToString() => Kind.ToString();
        }

        public class ElementLabel : Element
        {
            public string Text;
            public Color Color;
            public string Link;

            public override ElementKind Kind => ElementKind.Text;

            public sealed override string ToString() => $"{base.ToString()} \"{Text}\"";
        }

        public abstract class ElementWithID : Element
        {
            public byte ID;

            public override string ToString() => $"{base.ToString()}#{ID}";
        }

        public abstract class ElementFocusable : ElementWithID
        {

        }

        public class ElementButton : ElementFocusable
        {
            public string Text;
            public override ElementKind Kind => ElementKind.Button;
            public ElementForm Form;

            public sealed override string ToString() => $"{base.ToString()} {{ Text: \"{Text}\" }}";
        }

        public class ElementImage : Element
        {
            public string Url;
            public byte ImageID;
            public override ElementKind Kind => ElementKind.Image;

            public sealed override string ToString() => $"{base.ToString()} {{ Url: \"{Url}\" ImageID: {ImageID} }}";
        }

        public class ElementTextField : ElementFocusable
        {
            public string Name;
            internal TextInputField Manager;
            public override ElementKind Kind => ElementKind.InputText;
            public ElementForm Form;

            public sealed override string ToString() => $"{base.ToString()} {{ Name: \"{Name}\" Value: \"{Manager?.Buffer}\" }}";
        }

        public class ElementSelect : ElementFocusable
        {
            public (string Value, string Label)[] Values;
            public int SelectedIndex;
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

        public class NeedThisImage
        {
            public string Url;
            public byte ID;
            public Vector2Int DownloadedSize;
        }

        public delegate Vector2Int TextMeasurer(string text, int fontSize);

        public readonly List<NeedThisImage> NeedTheseImages;
        public readonly List<ElementForm> Forms;
        public readonly List<Stylesheet> Stylesheets;
        public readonly List<Element> Elements;
        (int LineHeight, List<Element> Elements) CurrentLine;
        TextMeasurer MeasureText;

        byte ElementIDCounter;
        byte ImageIDCounter;

        Vector2Int CurrentPosition;

        public RectInt PageArea;
        public int OverflowX;
        public int OverflowY => CurrentPosition.y;

        class BoxElement
        {
            public RectInt ContentRect;
            public int OverflowX;
        }

        readonly Stack<string> CurrentLink;
        readonly Stack<BoxElement> CurrentBox;

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

        public ElementWithID GetElementByID(byte id)
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

        public void GenerateLayout(HtmlDocument document, TextMeasurer textMeasurer)
        {
            MeasureText = textMeasurer;
            ElementIDCounter = 1;
            CurrentPosition = Vector2Int.zero;
            Elements.Clear();
            Forms.Clear();
            CurrentBox.Clear();
            OverflowX = PageArea.width;
            var doc = document.DocumentNode;
            GenerateLayout(doc, new Declaration[0]);
            FinishLine();
        }

        static string ConvertHtmlText(string text)
        {
            if (text is null) return null;
            return System.Web.HttpUtility.HtmlDecode(text);
        }

        static readonly string[] inheritableStyleProperties = new string[]
        {
            "color",
        };

        public PageGenerator()
        {
            CurrentLink = new Stack<string>();
            NeedTheseImages = new List<NeedThisImage>();
            Forms = new List<ElementForm>();
            Stylesheets = new List<Stylesheet>();
            Elements = new List<Element>();
            CurrentLine = (0, new List<Element>());
            CurrentBox = new Stack<BoxElement>();

            ElementIDCounter = 0;
            CurrentPosition = Vector2Int.zero;

            PageArea = default;
            OverflowX = 0;

            MeasureText = null;
        }

        public void Reset()
        {
            CurrentLink.Clear();
            NeedTheseImages.Clear();
            Forms.Clear();
            Stylesheets.Clear();
            CurrentBox.Clear();
            Elements.Clear();
            CurrentLine = (0, new List<Element>());

            ElementIDCounter = 0;
            CurrentPosition = Vector2Int.zero;

            PageArea = default;
            OverflowX = 0;

            MeasureText = null;
        }

        static readonly string[] SimpleTags = new string[]
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

        void GenerateLayout(HtmlNode node, Declaration[] currentStyles, ElementForm currentForm = null)
        {
            if (node.NodeType == HtmlNodeType.Comment) return;

            List<Declaration> newStyles = new(currentStyles);

            foreach (Stylesheet stylesheet in Stylesheets)
            { newStyles.AddRange(Compiler.GetMatches(stylesheet, node)); }

            if (node.NodeType == HtmlNodeType.Text)
            {
                string text = ConvertHtmlText(node.InnerText);
                if (string.IsNullOrWhiteSpace(text)) return;

                string link = null;
                if (CurrentLink.Count > 0)
                { link = CurrentLink.Peek(); }
                Color defaultColor = Color.white;

                if (!string.IsNullOrWhiteSpace(link))
                { defaultColor = Color.blue; }

                PushElement(new ElementLabel()
                {
                    ContentPosition = CurrentPosition,
                    ContentSize = MeasureText.Invoke(text, 8),
                    Text = text,
                    Color = FixColor(newStyles.GetColor("color", defaultColor)),
                    Link = link,
                });
                return;
            }

            SidesInt padding;
            SidesInt border;
            SidesInt margin;
            Value displayValue;
            RectInt contentRect;

            if (node.NodeType == HtmlNodeType.Element)
            {
                padding = Compiler.GetSidesPx(newStyles, "padding");
                border = SidesInt.Zero; // GetSideValues(newStyles, "border-width");
                margin = Compiler.GetSidesPx(newStyles, "margin");
                displayValue = newStyles.GetValue("display", Value.Null);

                if (displayValue == "block")
                { ForceFinishLine(); }
                else if (displayValue == "none")
                { return; }

                if (node.Name == "button")
                {
                    contentRect = new RectInt(CurrentPosition, MeasureText.Invoke(ConvertHtmlText(node.InnerText), 8));
                    PushElement(new ElementButton()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ContentPosition = contentRect.position,
                        ContentSize = contentRect.size,
                        Text = node.InnerText,
                        ID = ElementIDCounter++,
                    });
                    return;
                }
                else if (node.Name == "img")
                {
                    string src = node.GetAttributeValue("src", string.Empty);
                    NeedThisImage image = GetOrCreateImage(src);

                    int width = node.GetAttributeValue("width", image.DownloadedSize.x);
                    int height = node.GetAttributeValue("height", image.DownloadedSize.y);

                    contentRect = new RectInt(CurrentPosition, new Vector2Int(width, height));

                    PushElement(new ElementImage()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ContentPosition = contentRect.position,
                        ContentSize = contentRect.size,
                        Url = src,
                        ImageID = image.ID,
                    });
                    return;
                }
                else if (node.Name == "input" && node.GetAttributeValue("type", null) == "submit")
                {
                    string text = ConvertHtmlText(node.GetAttributeValue("value", "Submit"));
                    contentRect = new RectInt(CurrentPosition, MeasureText.Invoke(text, 8));
                    PushElement(new ElementButton()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ContentPosition = contentRect.position,
                        ContentSize = contentRect.size,
                        Text = text,
                        ID = ElementIDCounter++,
                        Form = currentForm,
                    });
                    return;
                }
                else if (node.Name == "input" && node.GetAttributeValue("type", null) == "text")
                {
                    string text = ConvertHtmlText(node.GetAttributeValue("value", ""));
                    contentRect = new RectInt(CurrentPosition, new Vector2Int(50, 8));
                    PushElement(new ElementTextField()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ContentPosition = contentRect.position,
                        ContentSize = contentRect.size,
                        Manager = new TextInputField(text),
                        ID = ElementIDCounter++,
                        Form = currentForm,
                        Name = node.GetAttributeValue("name", string.Empty),
                    });
                    return;
                }
                else if (node.Name == "form")
                {
                    contentRect = new RectInt(CurrentPosition, new Vector2Int(PageArea.width, 0));
                    ElementForm newForm = new()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ID = ElementIDCounter++,
                        Method = node.GetAttributeValue("method", "POST"),
                        Target = node.GetAttributeValue("target", "./"),
                    };
                    currentForm = newForm;
                    Elements.Add(newForm);
                    Forms.Add(newForm);
                }
                else if (node.Name == "select")
                {
                    List<(string Value, string Label)> values = new();

                    int longest = 16;

                    foreach (HtmlNode child in node.ChildNodes)
                    {
                        if (child.Name != "option") continue;
                        string value = child.GetAttributeValue("value", null);
                        string label = ConvertHtmlText(child.InnerText).Trim();

                        if (string.IsNullOrWhiteSpace(label)) continue;
                        values.Add((value, label));

                        longest = Mathf.Max(longest, MeasureText.Invoke(label, 8).x);
                    }

                    contentRect = new RectInt(CurrentPosition, new Vector2Int(longest, 10));

                    PushElement(new ElementSelect()
                    {
                        Padding = padding,
                        Border = border,
                        Margin = margin,
                        ContentPosition = contentRect.position,
                        ContentSize = contentRect.size,
                        Values = values.ToArray(),
                        SelectedIndex = 0,
                        ID = ElementIDCounter++,
                    });

                    return;
                }
                else if (SimpleTags.Contains(node.Name, StringComparison.InvariantCulture))
                {
                    contentRect = new RectInt(CurrentPosition, Vector2Int.zero);
                }
                else
                {
                    contentRect = new RectInt(CurrentPosition, Vector2Int.zero);
                    Debug.Log($"[{nameof(PageGenerator)}]: Unknown tag \"{node.Name}\"");
                }
            }
            else if (node.NodeType == HtmlNodeType.Document)
            {
                contentRect = PageArea;
            }
            else
            {
                Debug.LogWarning($"[{nameof(PageGenerator)}]: Unimplemented node type: {node.NodeType}");
                return;
            }

            List<Declaration> inheritableStyles = new();
            foreach (Declaration item in newStyles)
            {
                foreach (var inheritableStyleProperty in inheritableStyleProperties)
                {
                    if (string.Equals(inheritableStyleProperty, item.property, StringComparison.InvariantCultureIgnoreCase))
                    {
                        inheritableStyles.Add(item);
                        break;
                    }
                }
            }

            if (node.Name == "a")
            { CurrentLink.Push(node.GetAttributeValue("href", null)); }

            if (node.NodeType == HtmlNodeType.Element)
            {
                CurrentBox.Push(new BoxElement()
                {
                    ContentRect = contentRect,
                    OverflowX = 0,
                });
            }

            foreach (HtmlNode child in node.ChildNodes)
            {
                GenerateLayout(child, inheritableStyles.ToArray(), currentForm);
            }

            if (node.NodeType == HtmlNodeType.Element)
            {
                BoxElement box = CurrentBox.Pop();
            }

            if (node.Name == "a")
            { CurrentLink.Pop(); }
        }

        static Color FixColor(Color color)
        {
            if (color.grayscale >= .5f) return color;
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp(v, .5f, 1f);
            return Color.HSVToRGB(h, s, v);
        }

        void FinishLine()
        {
            if (CurrentPosition.x == 0) return;
            ForceFinishLine();
        }

        void ForceFinishLine()
        {
            CurrentPosition.y += CurrentLine.LineHeight;

            foreach (Element element in CurrentLine.Elements)
            {
                element.ContentPosition.y = CurrentPosition.y;
                element.RecalculateRectangles();
            }

            CurrentPosition.x = 0;

            CurrentLine = (0, new List<Element>());
        }

        /// <summary>
        ///   This will handle the following:
        ///   <list type="bullet">
        ///     <item>
        ///       <paramref name="element"/> . Position
        ///     </item>
        ///     <item>
        ///       <see langword="this"/> . OverflowX 
        ///     </item>
        ///     <item>
        ///       <see langword="this"/> . CurrentPosition . X
        ///     </item>
        ///     <item>
        ///       <see langword="this"/> . CurrentLine
        ///     </item>
        ///   </list>
        /// </summary>
        void PushElement(Element element)
        {
            if (CurrentBox.Peek().ContentRect.xMax >= element.PaddingPosition.x)
            {
                CurrentBox.Peek().OverflowX = element.PaddingPosition.x - CurrentBox.Peek().ContentRect.xMax;
            }

            element.RecalculateRectangles();

            if (CurrentPosition.x + element.PaddingRect.width > PageArea.width)
            {
                FinishLine();
                OverflowX = Mathf.Max(OverflowX, element.PaddingRect.width);
            }

            element.ContentPosition = CurrentPosition;
            element.RecalculateRectangles();

            Elements.Add(element);

            CurrentPosition.x += element.PaddingRect.width;

            element.RecalculateRectangles();

            CurrentLine.LineHeight = Mathf.Max(CurrentLine.LineHeight, element.PaddingRect.height);
            CurrentLine.Elements.Add(element);

            element.RecalculateRectangles();
        }
    }
    */
}