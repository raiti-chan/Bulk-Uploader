using System.Runtime.InteropServices;

namespace net.raitichan.avatar.bulk_uploader.Editor {
    public static class Notification {
        
        public static void PlayNotification() {
            PlaySound("SystemNotification", System.IntPtr.Zero, _SND_ALIAS | _SND_ASYNC);
        }

        public static void PlayHand() {
            PlaySound("SystemHand", System.IntPtr.Zero, _SND_ALIAS | _SND_ASYNC);
        }

        private const uint _SND_ALIAS = 0x00010000;
        private const uint _SND_ASYNC = 0x0001;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool PlaySound(string pszSound, System.IntPtr hWnd, uint fdwSound);

    }
}