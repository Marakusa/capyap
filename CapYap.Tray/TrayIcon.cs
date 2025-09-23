namespace CapYap.Tray
{
    public class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;

        public event EventHandler? OnOpenClicked;
        public event EventHandler? OnCaptureClicked;
        public event EventHandler? OnOpenExternalClicked;
        public event EventHandler? OnExitClicked;

        public TrayIcon(string tooltip = "CapYap", string iconPath = "icon.ico", string version = "1.0.0")
        {
            _notifyIcon = new NotifyIcon
            {
                Text = tooltip,
                Icon = new Icon(iconPath),
                Visible = true,
                BalloonTipText = "CapYap"
            };

            // Setup context menu
            _contextMenu = new ContextMenuStrip()
            {
                ShowImageMargin = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Renderer = new ToolStripProfessionalRenderer(new ModernColorTable())
            };

            var openItem = new ToolStripMenuItem("Open CapYap");
            openItem.Click += (s, e) => OnOpenClicked?.Invoke(this, EventArgs.Empty);

            var captureItem = new ToolStripMenuItem("Take a screenshot");
            captureItem.Click += (s, e) => OnCaptureClicked?.Invoke(this, EventArgs.Empty);

            var openExtItem = new ToolStripMenuItem("Open in browser");
            openExtItem.Click += (s, e) => OnOpenExternalClicked?.Invoke(this, EventArgs.Empty);

            var exitItem = new ToolStripMenuItem("Quit");
            exitItem.Click += (s, e) => OnExitClicked?.Invoke(this, EventArgs.Empty);

            _contextMenu.Items.Add(openItem);
            _contextMenu.Items.Add(captureItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(openExtItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(new ToolStripLabel($"CapYap - {version}")
            {
                ForeColor = Color.LightGray
            });
            _contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _contextMenu;

            // Show menu on single left click
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;

            // Optional: handle double-click to open
            _notifyIcon.DoubleClick += (s, e) => OnOpenClicked?.Invoke(this, EventArgs.Empty);
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                typeof(NotifyIcon)
                    .GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(_notifyIcon, null);
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            GC.SuppressFinalize(this);
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            private readonly Color _accent = ColorTranslator.FromHtml("#5E5EFF");

            public override Color MenuItemSelected => _accent;
            public override Color MenuItemSelectedGradientBegin => _accent;
            public override Color MenuItemSelectedGradientEnd => _accent;
            public override Color MenuBorder => _accent;
            public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
        }
    }
}
