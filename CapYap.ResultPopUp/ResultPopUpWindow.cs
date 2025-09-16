using System.Drawing;

namespace CapYap.ResultPopUp
{
    public static class ResultPopUpWindow
    {
        private static PopUpWindow? _popUpWindow;

        public static void Show(Bitmap bitmap)
        {
            if (_popUpWindow != null)
            {
                _popUpWindow.Close();
                _popUpWindow = null;
            }

            _popUpWindow = new PopUpWindow(bitmap);
            _popUpWindow.Closed += (_, _) =>
            {
                _popUpWindow = null;
            };
            _popUpWindow.Show();
        }

        public static void Close()
        {
            if (_popUpWindow != null)
            {
                _popUpWindow.Close();
                _popUpWindow = null;
            }
        }
    }
}
