using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;
using Graphics = UnityEngine.Graphics;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Window {
    public static class ToastNotification {
        public static void ShowToast(string title, string message, bool silent = false) {
            string image = string.IsNullOrEmpty(_unityIconPath) ? "" : $"<image placement='appLogoOverride' src='{_unityIconPath}'/>";
            string xml = $@"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>{title}</text>
            <text>{message}</text>
            {image}
        </binding>
    </visual>
    <audio silent='{silent}'/>
</toast>";
            show_toast(_APP_ID, xml);
        }

        private const string _APP_ID = "net.raitichan.avatar.bulk_uploader";
        private const string _REGISTRY_SUB_KEY = @"Software\Classes\AppUserModelId\" + _APP_ID;
        private const string _DISPLAY_NAME = "Bulk Uploader";

        private static string? _unityIconPath = GetUnityIcon();

        static ToastNotification() {
            Initialize();
        }

        private static void Initialize() {
            using RegistryKey? rootKey = Registry.CurrentUser.CreateSubKey(_REGISTRY_SUB_KEY);
            if (rootKey == null) return;
            rootKey.SetValue("DisplayName", _DISPLAY_NAME);

            string? iconPath = GetUnityIcon();
            if (iconPath == null) return;
            rootKey.SetValue("IconUri", iconPath);
        }

        private static string? GetUnityIcon() {
            Texture2D? icon = EditorGUIUtility.Load("d_SceneAsset Icon") as Texture2D;
            if (icon == null) return null;

            Texture2D output = new(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
            Graphics.CopyTexture(icon, output);

            string savePath = Path.Combine(Application.persistentDataPath, "unity.png").Replace('/', '\\');
            File.WriteAllBytes(savePath, output.EncodeToPNG());
            _unityIconPath = savePath;
            return savePath;
        }


        [DllImport("UnityToastNotification.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int show_toast(string appId, string xml);
    }
}