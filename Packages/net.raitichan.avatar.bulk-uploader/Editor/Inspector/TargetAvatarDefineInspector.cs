using net.raitichan.avatar.bulk_uploader.Runtime.Component;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector {
	[CustomEditor(typeof(TargetAvatarsDefine))]
	internal class TargetAvatarDefineInspector : UnityEditor.Editor {
		private VisualElement _rootElement = null!;

		private SerializedProperty _avatarsSerializedProperty = null!;

		public void OnEnable() {
			this._avatarsSerializedProperty = this.serializedObject.FindProperty(nameof(TargetAvatarsDefine.Avatars));
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
				name = "AvatarListView",
				showAddRemoveFooter = true,
				reorderable = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				showBorder = true,
				headerTitle = "Avatars",
				showFoldoutHeader = true,
				reorderMode = ListViewReorderMode.Animated
			};

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
				style = { marginTop = 10}
			};
			this._rootElement.Add(button);
		}

		private async void OnUpload() {
			await BulkUploadProcess.StartUpload((TargetAvatarsDefine)this.target);
		}
	}

	internal class AvatarField : VisualElement {
		private readonly PropertyField _avatarPropertyField;

		private SerializedProperty _property = null!;
		private SerializedProperty _avatarSerializedProperty = null!;

		public AvatarField() {
			this._avatarPropertyField = new PropertyField {
				label = ""
			};
			this.Add(this._avatarPropertyField);
		}

		public void BindProperty(SerializedProperty prop) {
			this._property = prop;
			this._avatarSerializedProperty = this._property.FindPropertyRelative(nameof(AvatarDefine.Avatar));
			this._avatarPropertyField.BindProperty(this._avatarSerializedProperty);
		}
	}
}