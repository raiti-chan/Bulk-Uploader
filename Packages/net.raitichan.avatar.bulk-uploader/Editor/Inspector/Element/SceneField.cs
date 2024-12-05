using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element {
	internal class SceneField : VisualElement {
		private readonly Toggle _enableToggle;
		private readonly PropertyField _scenePropertyField;

		private SerializedProperty _property = null!;
		private SerializedProperty _enableSerializedProperty = null!;
		private SerializedProperty _sceneSerializedProperty = null!;

		public SceneField() {
			this._enableToggle = new Toggle() {
				label = ""
			};
			this.Add(this._enableToggle);
			
			this._scenePropertyField = new PropertyField {
				label = ""
			};
			this.Add(this._scenePropertyField);
		}

		public void BindProperty(SerializedProperty prop) {
			this._property = prop;
			this._enableSerializedProperty = this._property.FindPropertyRelative(nameof(AvatarDefine.Enable));
			this._enableToggle.BindProperty(this._enableSerializedProperty);
			this._sceneSerializedProperty = this._property.FindPropertyRelative(nameof(SceneDefine.Scene));
			this._scenePropertyField.BindProperty(this._sceneSerializedProperty);
		}
	}
}