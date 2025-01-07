using System.Collections.Generic;
using net.raitichan.avatar.bulk_uploader.Editor.Window.Element;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Window {
	internal class UploadProgressWindow : EditorWindow {
		private const string USS_GUID = "e058a7e8d8ad48a98017eebc24994e26";
		private static UploadProgressWindow? _currentWindow;

		[MenuItem("Raitichan/BulkUploader/Show Progress Window")]
		public static void ShowWindowCommand() {
			ShowWindow();
		}
        
		public static UploadProgressWindow ShowWindow() {
			if (_currentWindow != null) _currentWindow.Close();
			_currentWindow = GetWindow<UploadProgressWindow>();
			_currentWindow.titleContent = new GUIContent($"Bulk Uploader Processing...");
			_currentWindow.Refresh();
			_currentWindow.Show();
			return _currentWindow;
		}

		private readonly Dictionary<string, SceneProgress> _sceneProgressesDict = new();
		private ScrollView _rootScroll = null!;

		private void CreateGUI() {
			string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
			this.rootVisualElement.styleSheets.Add(styleSheet);

			this._rootScroll = new ScrollView();
			this._rootScroll.AddToClassList("root");
			this.rootVisualElement.Add(this._rootScroll);

			VisualElement buttonGroup = new();
			buttonGroup.AddToClassList("button-group");
			this.rootVisualElement.Add(buttonGroup);
			
			buttonGroup.Add(new Button(BulkUploadProcess.CancelCurrentAvatar){text = "Skip Current Avatar"});
			buttonGroup.Add(new Button(BulkUploadProcess.CancelCurrentScene){text = "Skip Current Scene"});
			buttonGroup.Add(new Button(BulkUploadProcess.CancelAllProcess){text = "Cancel"});
			
		}

		private void Refresh() {
			this._sceneProgressesDict.Clear();
		}

		internal void RegisterScene(string sceneName) {
			if (this._sceneProgressesDict.ContainsKey(sceneName)) return;
			SceneProgress sceneProgress = new(sceneName);
			this._rootScroll.Add(sceneProgress);
			this._sceneProgressesDict.Add(sceneName, sceneProgress);
		}
		
		internal void SetSceneProgress(string sceneName, string text, float value) {
			if (this._sceneProgressesDict.TryGetValue(sceneName, out SceneProgress progress)) {
				progress.SetSceneProgress(text, value);
			}
		}

		internal void RegisterAvatar(string sceneName, string blueprintId, string avatarName) {
			if (this._sceneProgressesDict.TryGetValue(sceneName, out SceneProgress progress)) {
				progress.RegisterAvatar(blueprintId, avatarName);
			}
		}
		
		internal void SetAvatarProgress(string sceneName, string blueprintId, string text, float value) {
			if (this._sceneProgressesDict.TryGetValue(sceneName, out SceneProgress progress)) {
				progress.SetAvatarProgress(blueprintId ,text, value);
			}
		}

		internal void SetAvatarError(string sceneName, string blueprintId) {
			if (this._sceneProgressesDict.TryGetValue(sceneName, out SceneProgress progress)) {
				progress.SetAvatarError(blueprintId);
			}
		}
		
		
	}
}