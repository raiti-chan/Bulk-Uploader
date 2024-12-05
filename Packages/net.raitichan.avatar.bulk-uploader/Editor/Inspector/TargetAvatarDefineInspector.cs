using net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element;
using net.raitichan.avatar.bulk_uploader.Runtime.Component;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector {
	[CustomEditor(typeof(TargetAvatarsDefine))]
	internal class TargetAvatarDefineInspector : UnityEditor.Editor {
		private const string USS_GUID = "82d4ebb141e046149a0dc13187d1cb05";
		private VisualElement _rootElement = null!;

		private SerializedProperty _avatarsSerializedProperty = null!;

		public void OnEnable() {
			this._avatarsSerializedProperty = this.serializedObject.FindProperty(nameof(TargetAvatarsDefine.Avatars));
		}

		public override VisualElement CreateInspectorGUI() {
			string ussPath = AssetDatabase.GUIDToAssetPath(USS_GUID);
			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
			
			VisualElement root = new();
			root.styleSheets.Add(styleSheet);
			root.AddToClassList("root");
			
			this._rootElement = root;
			this.CreateGUI();
			return this._rootElement;
		}

		private void CreateGUI() {
			AdvancedListView listView = new AdvancedListView("Avatars") {
				name = "AvatarListView"
			};
			listView.makeHeader += () => new Label("Avatar");
			listView.makeItem += () => new AvatarField();
			listView.bindItem += (element, i) => {
				AvatarField avatarField = (AvatarField)element;
				SerializedProperty param = this._avatarsSerializedProperty.GetArrayElementAtIndex(i);
				avatarField.BindProperty(param);
			};
			

			listView.BindProperty(this._avatarsSerializedProperty);
			this._rootElement.Add(listView);

			Button button = new(this.OnUpload) {
				text = "Upload",
				style = { marginTop = 10 }
			};
			this._rootElement.Add(button);
		}

		private async void OnUpload() {
			await BulkUploadProcess.StartUpload((TargetAvatarsDefine)this.target);
		}
	}
}