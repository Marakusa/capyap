using System.Windows.Input;
using CapYap.HotKeys.Models;

namespace CapYap.HotKeys
{
    public enum BindingAction
    {
        CaptureScreen,
        CloseCropView
    }

    public class HotKeyManager : IDisposable
    {
        private readonly Dictionary<BindingAction, HotKey> hotKeys = new();

        public event Action<HotKey> HotKey_ScreenCapture;
        public event Action<HotKey> HotKey_CloseCropView;

        public HotKeyManager()
        {
            HotKey_ScreenCapture += DummyCallback;
            HotKey_CloseCropView += DummyCallback;
        }

        public void Bind(BindingAction action, Key k, KeyModifier keyModifiers)
        {
            HotKey hotKey = new(k, keyModifiers, GetBindingAction(action), true);
            hotKeys.Add(action, hotKey);
        }

        public void Rebind(BindingAction action)
        {
            Key k = hotKeys[action].Key;
            KeyModifier keyModifiers = hotKeys[action].KeyModifiers;

            if (hotKeys.ContainsKey(action))
            {
                hotKeys[action].Dispose();
                hotKeys.Remove(action);
            }

            Bind(action, k, keyModifiers);
        }
        public void Rebind(BindingAction action, Key k, KeyModifier keyModifiers)
        {
            if (hotKeys.ContainsKey(action))
            {
                hotKeys[action].Dispose();
                hotKeys.Remove(action);
            }

            Bind(action, k, keyModifiers);
        }

        public void Dispose()
        {
            foreach (var hotKey in hotKeys)
            {
                hotKey.Value.Dispose();
            }
            hotKeys.Clear();
        }

        private Action<HotKey> GetBindingAction(BindingAction action)
        {
            switch (action)
            {
                case BindingAction.CaptureScreen:
                    return HotKey_ScreenCapture;
                case BindingAction.CloseCropView:
                    return HotKey_CloseCropView;
                default:
                    throw new NotImplementedException();
            }
        }

        private void DummyCallback(HotKey obj)
        {
            // Do nothing
        }
    }
}
