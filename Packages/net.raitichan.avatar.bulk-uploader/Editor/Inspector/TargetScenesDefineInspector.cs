using System.Collections.Generic;
using net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element;
using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
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


			listView.BindProperty(this._scenesSerializedProperty);
			this._rootElement.Add(listView);

			this._rootElement.Add(new SceneListView(this._scenesSerializedProperty));


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

	internal class SceneListView : IMGUIContainer {
		private readonly SerializedObject _serializedObject;
		private readonly SerializedProperty _serializedProperty;
		
		private ReorderableList? _listView;
		private readonly Dictionary<int, SceneListElement> _sceneListElements = new Dictionary<int, SceneListElement>();

		public SceneListView(SerializedProperty serializedProperty) {
			this._serializedObject = serializedProperty.serializedObject;
			this._serializedProperty = serializedProperty;
			this.onGUIHandler = this.OnGUI;
		}

		private void OnGUI() {
			this._serializedObject.Update();
			this._listView ??= new ReorderableList(this._serializedObject, this._serializedProperty) {
				draggable = true,
				drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Scenes"),
				elementHeightCallback = ElementHeightCallback,
				drawElementCallback = DrawElementCallback,
				onAddCallback = list => Debug.Log("Add"),
				onRemoveCallback = list => Debug.Log("Remove"),
			};
			
			this._listView.DoLayoutList();
			this._serializedObject.ApplyModifiedProperties();
		}
		
		private void DrawElementCallback(Rect rect, int index, bool active, bool focused) {
			if (!this._sceneListElements.TryGetValue(index, out SceneListElement element)) {
				element = new SceneListElement(_serializedProperty.GetArrayElementAtIndex(index));
				this._sceneListElements.Add(index, element);
			}
			element.OnGUI(rect);
		}
		
		private float ElementHeightCallback(int index) {
			if (this._sceneListElements.TryGetValue(index, out SceneListElement element)) return element.GetHeight();
			return 30.0f;
		}
	}

	internal class SceneListElement {
		private const float ROOT_PADDING_TD = 5.0f;
		private const float PROPERTY_HEIGHT = 18.0f;
		
		private const float MARGIN_VERTICAL = 2.0f;
		private const float MARGIN_HORIZONTAL = 5.0f;
		
		private const float TOGGLE_WIDTH = 15.0f;
		
		private readonly SerializedProperty _serializedProperty;
		private readonly SerializedProperty _enableProperty;
		private readonly SerializedProperty _sceneProperty;

		private readonly ReorderableList? _listView;

		public SceneListElement(SerializedProperty serializedProperty) {
			this._serializedProperty = serializedProperty;
			this._enableProperty = this._serializedProperty.FindPropertyRelative(nameof(SceneDefine.Enable));
			this._sceneProperty = this._serializedProperty.FindPropertyRelative(nameof(SceneDefine.Scene));
		}

		public void OnGUI(Rect rect) {
			Rect rootRect = new(rect.x, rect.y + ROOT_PADDING_TD, rect.width, PROPERTY_HEIGHT);
			EditorGUI.PropertyField(rootRect, this._enableProperty, GUIContent.none);
			rootRect.x += TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			rootRect.width -= TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			EditorGUI.PropertyField(rootRect, this._sceneProperty, GUIContent.none);
			
			
		}
		

		public float GetHeight() {
			return 60.0f;
		}
	}
}