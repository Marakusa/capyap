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

        public HotKeyManager()
        {
            HotKey_ScreenCapture += DummyCallback;

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
