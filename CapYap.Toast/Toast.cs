namespace CapYap.Toast
{
    public class Toast
    {
        private readonly ToastWindow _toastWindow;

        private readonly int _closeTimeout;

        internal event EventHandler? OnClosing;

        public Toast(int closeTimeout = 5000)
        {
            _closeTimeout = closeTimeout;
            _toastWindow = new();
            _toastWindow.Closing += (_, _) =>
            {
                OnClosing?.Invoke(this, EventArgs.Empty);
            };
            _toastWindow.Show();
            ToastManager.Add(this);
        }

        public void SetWait(string text)
        {
            _toastWindow.SetStatus(true, text);
        }

        public void SetSuccess(string text, bool autoClose = true)
        {
            _toastWindow.SetStatus(false, text);
            if (autoClose)
            {
                _toastWindow.CloseIn(_closeTimeout);
            }
        }

        public void SetFail(string text, bool autoClose = true)
        {
            _toastWindow.SetStatus(false, text, true);
            if (autoClose)
            {
                _toastWindow.CloseIn(_closeTimeout);
            }
        }

        public void Close()
        {
            _toastWindow.CloseWindow();
        }

        internal void SetToastOrder(int order = 0)
        {
            _toastWindow.SetToastOrder(order);
        }

        internal ToastWindow GetWindow()
        {
            return _toastWindow;
        }
    }
}
