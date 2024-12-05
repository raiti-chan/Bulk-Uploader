using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector {
	[CustomEditor(typeof(TargetScenesDefine))]
	internal class TargetScenesDefineInspector : UnityEditor.Editor {
		private VisualElement _rootElement = null!;

		private SerializedProperty _scenesSerializedProperty = null!;

		public void OnEnable() {
			this._scenesSerializedProperty = this.serializedObject.FindProperty(nameof(TargetScenesDefine.Scenes));
		}

		public override VisualElement CreateInspectorGUI() {
			VisualElement root = new();
			this._rootElement = root;
			this.CreateGUI();
			return this._rootElement;
		}

		private void CreateGUI() {
			ListView listView = new() {
				focusable = true,
				name = "ScenesListView",
				showAddRemoveFooter = true,
				reorderable = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				showBorder = true,
				headerTitle = "Scenes",
				showFoldoutHeader = true,
				reorderMode = ListViewReorderMode.Animated
			};

			listView.makeItem += () => new SceneField();
			listView.bindItem += (element, i) => {
				SceneField sceneField = (SceneField)element;
				SerializedProperty param = this._scenesSerializedProperty.GetArrayElementAtIndex(i);
				sceneField.BindProperty(param);
			};
			
			
			listView.BindProperty(this._scenesSerializedProperty);
			this._rootElement.Add(listView);
			
			Button button = new(this.OnUpload) {
				text = "Upload",
				style = { marginTop = 10}
			};
			this._rootElement.Add(button);
		}

		private async void OnUpload() {
			await BulkUploadProcess.StartUpload((TargetScenesDefine)this.target);
		}
	}

	internal class SceneField : VisualElement {
		private readonly PropertyField _scenePropertyField;
		
		private SerializedProperty _property = null!;
		private SerializedProperty _sceneSerializedProperty = null!;
		
		public SceneField() {
			this._scenePropertyField = new PropertyField {
				label = ""
			};
			this.Add(this._scenePropertyField);
		}

		public void BindProperty(SerializedProperty prop) {
			this._property = prop;
			this._sceneSerializedProperty = this._property.FindPropertyRelative(nameof(SceneDefine.Scene));
			this._scenePropertyField.BindProperty(this._sceneSerializedProperty);
		}
	}
}