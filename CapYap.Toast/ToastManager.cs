
namespace CapYap.Toast
{
    public static class ToastManager
    {
        private static List<Toast> _toasts = new();

        internal static void Add(Toast toast)
        {
            toast.OnClosing += ToastRemoved;
            _toasts.Add(toast);
            UpdateToastOrders();
        }

        private static void ToastRemoved(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            Toast toast = (Toast)sender;
            _toasts.Remove(toast);
            UpdateToastOrders();
        }

        private static void UpdateToastOrders()
        {
            for (int index = 0; index < _toasts.Count; index++)
            {
                var toast = _toasts[index];
                toast.SetToastOrder(_toasts.Count - index - 1);
            }
        }

        public static void HideAllToasts()
        {
            foreach (var toast in _toasts)
            {
                toast.GetWindow().Topmost = false;
            }
        }

        public static void ShowAllToasts()
        {
            foreach (var toast in _toasts)
            {
                toast.GetWindow().Topmost = true;
            }
        }
    }
}
