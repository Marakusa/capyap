using System.Windows.Input;
using CapYap.HotKeys.Models;
using SharpDX.DirectInput;

namespace CapYap.HotKeys
{
    public enum BindingAction
    {
        CaptureScreen
    }

    public class HotKeyManager : IDisposable
    {
        private readonly Dictionary<BindingAction, HotKey> hotKeys = new();
        private CancellationTokenSource? _pollingTokenSource;

        private DirectInput? _directInput;
        private SharpDX.DirectInput.Keyboard? _directInputKeyboard;

        public event Action<HotKey> HotKey_ScreenCapture;

        public event Action<bool> CtrlChanged;
        public event Action<bool> ShiftChanged;
        public event Action<bool> AltChanged;
        public event Action<bool> EscapeChanged;

        private bool _ctrlDown;
        private bool _shiftDown;
        private bool _altDown;
        private bool _escapeDown;

        public HotKeyManager()
        {
            HotKey_ScreenCapture += DummyCallback;

            CtrlChanged += (_) => { };
            ShiftChanged += (_) => { };
            AltChanged += (_) => { };
            EscapeChanged += (_) => { };

            InitializeDirectInput();
            StartPollingKeys();
        }

        #region HotKey Registration (Windows)

        public void Bind(BindingAction action, System.Windows.Input.Key k, KeyModifier keyModifiers)
        {
            HotKey hotKey = new(k, keyModifiers, GetBindingAction(action), true);
            hotKeys.Add(action, hotKey);
        }

        public void Rebind(BindingAction action, System.Windows.Input.Key k, KeyModifier keyModifiers)
        {
            if (hotKeys.ContainsKey(action))
            {
                hotKeys[action].Dispose();
                hotKeys.Remove(action);
            }

            Bind(action, k, keyModifiers);
        }

        public void Unbind(BindingAction action)
        {
            if (hotKeys.ContainsKey(action))
            {
                hotKeys[action].Unregister();
                hotKeys[action].Dispose();
                hotKeys.Remove(action);
            }
        }

        private Action<HotKey> GetBindingAction(BindingAction action)
        {
            return action switch
            {
                BindingAction.CaptureScreen => HotKey_ScreenCapture,
                _ => throw new NotImplementedException(),
            };
        }

        private void DummyCallback(HotKey obj) { }

        #endregion

        #region DirectInput Polling

        private void InitializeDirectInput()
        {
            _directInput = new DirectInput();
            _directInputKeyboard = new SharpDX.DirectInput.Keyboard(_directInput);
            _directInputKeyboard.Acquire();
        }

        private void StartPollingKeys()
        {
            _pollingTokenSource = new CancellationTokenSource();
            var token = _pollingTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    PollDirectInput();
                    await Task.Delay(10, token); // 100Hz polling
                }
            }, token);
        }

        private void PollDirectInput()
        {
            try
            {
                if (_directInputKeyboard == null)
                {
                    return;
                }

                _directInputKeyboard.Poll();
                var state = _directInputKeyboard.GetCurrentState();

                bool ctrlPressed = state.PressedKeys.Contains(SharpDX.DirectInput.Key.LeftControl) ||
                                   state.PressedKeys.Contains(SharpDX.DirectInput.Key.RightControl);
                bool shiftPressed = state.PressedKeys.Contains(SharpDX.DirectInput.Key.LeftShift) ||
                                    state.PressedKeys.Contains(SharpDX.DirectInput.Key.RightShift);
                bool altPressed = state.PressedKeys.Contains(SharpDX.DirectInput.Key.LeftAlt) ||
                                    state.PressedKeys.Contains(SharpDX.DirectInput.Key.RightAlt);
                bool escapePressed = state.PressedKeys.Contains(SharpDX.DirectInput.Key.Escape);

                // Fire events only when state changes
                if (ctrlPressed != _ctrlDown)
                {
                    _ctrlDown = ctrlPressed;
                    CtrlChanged?.Invoke(_ctrlDown);
                }

                if (shiftPressed != _shiftDown)
                {
                    _shiftDown = shiftPressed;
                    ShiftChanged?.Invoke(_shiftDown);
                }

                if (altPressed != _altDown)
                {
                    _altDown = altPressed;
                    AltChanged?.Invoke(_altDown);
                }

                if (escapePressed != _escapeDown)
                {
                    _escapeDown = escapePressed;
                    EscapeChanged?.Invoke(_escapeDown);
                }

                foreach (var kvp in hotKeys)
                {
                    var hotKey = kvp.Value;
                    if (IsPressed(state, hotKey))
                    {
                        hotKey.Action.Invoke(hotKey);
                    }
                }
            }
            catch
            {
                // Ignore DirectInput errors (device lost, etc.)
            }
        }

        private bool IsPressed(KeyboardState state, HotKey hotKey)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(hotKey.Key);
            return state.PressedKeys.Contains((SharpDX.DirectInput.Key)virtualKey);
        }

        #endregion

        public void Dispose()
        {
            _pollingTokenSource?.Cancel();

            foreach (var hotKey in hotKeys.Values)
                hotKey.Dispose();

            hotKeys.Clear();

            _directInputKeyboard?.Unacquire();
            _directInputKeyboard?.Dispose();
            _directInput?.Dispose();
        }
    }
}
