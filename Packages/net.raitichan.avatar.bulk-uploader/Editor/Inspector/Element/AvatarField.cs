using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element {
	internal class AvatarField : VisualElement {
		private readonly Toggle _enableToggle;
		private readonly PropertyField _avatarPropertyField;

		private SerializedProperty _property = null!;
		private SerializedProperty _enableSerializedProperty = null!;
		private SerializedProperty _avatarSerializedProperty = null!;

		public AvatarField() {
			this._enableToggle = new Toggle() {
				label = ""
			};
			this.Add(this._enableToggle);
			
			this._avatarPropertyField = new PropertyField {
				label = ""
			};
			this.Add(this._avatarPropertyField);
		}

		public void BindProperty(SerializedProperty prop) {
			this._property = prop;
			this._enableSerializedProperty = this._property.FindPropertyRelative(nameof(AvatarDefine.Enable));
			this._enableToggle.BindProperty(this._enableSerializedProperty);
			this._avatarSerializedProperty = this._property.FindPropertyRelative(nameof(AvatarDefine.Avatar));
			this._avatarPropertyField.BindProperty(this._avatarSerializedProperty);
		}
	}
}