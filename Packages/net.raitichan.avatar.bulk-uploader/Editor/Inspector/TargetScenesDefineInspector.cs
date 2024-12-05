using net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element;
using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector {
	[CustomEditor(typeof(TargetScenesDefine))]
	internal class TargetScenesDefineInspector : UnityEditor.Editor {
		private const string USS_GUID = "82d4ebb141e046149a0dc13187d1cb05";
		private VisualElement _rootElement = null!;

		private SerializedProperty _scenesSerializedProperty = null!;

		public void OnEnable() {
			this._scenesSerializedProperty = this.serializedObject.FindProperty(nameof(TargetScenesDefine.Scenes));
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
			AdvancedListView listView = new("Scenes") {
				name = "ScenesListView"
			};
			listView.makeHeader += () => new Label("Scenes");
			listView.makeItem += () => new SceneField();
			listView.bindItem += (element, i) => {
				SceneField sceneField = (SceneField)element;
				SerializedProperty param = this._scenesSerializedProperty.GetArrayElementAtIndex(i);
				sceneField.BindProperty(param);
			};
			listView.fixedItemHeight = 50;


			listView.BindProperty(this._scenesSerializedProperty);
			this._rootElement.Add(listView);

			Button button = new(this.OnUpload) {
				text = "Upload",
				style = { marginTop = 10 }
			};
			this._rootElement.Add(button);
		}

		private async void OnUpload() {
			await BulkUploadProcess.StartUpload((TargetScenesDefine)this.target);
		}
	}

}