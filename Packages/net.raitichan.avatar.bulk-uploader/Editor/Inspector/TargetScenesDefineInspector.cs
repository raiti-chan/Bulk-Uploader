using System;
using System.Collections.Generic;
using System.Linq;
using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC;
using VRC.Core;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector {
	[CustomEditor(typeof(TargetScenesDefine))]
	internal class TargetScenesDefineInspector : UnityEditor.Editor {
		private const string USS_GUID = "82d4ebb141e046149a0dc13187d1cb05";
		private VisualElement _rootElement = null!;

		private SerializedProperty _scenesSerializedProperty = null!;

		public void OnEnable() {
			this._scenesSerializedProperty = this.serializedObject.FindProperty(nameof(TargetScenesDefine.Scenes));
			
			this.ScanAvatar(false);
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
			this._rootElement.Add(new Button(this.OnScanAvatar) {
				text = "Scan AllScenes Avatar",
				style = { marginTop = 10, marginBottom = 10}
			});
			this._rootElement.Add(new SceneListView(this._scenesSerializedProperty));


			Button button = new(this.OnUpload) {
				text = "Upload",
				style = { marginTop = 10 }
			};
			this._rootElement.Add(button);
		}

		private async void OnUpload() {
			try {
				await BulkUploadProcess.StartUpload((TargetScenesDefine)this.target);
			} catch (Exception e) {
				Debug.LogException(e);
			}
		}

		private void OnScanAvatar() {
			this.ScanAvatar(true);
		}

		private void ScanAvatar(bool isLoadScene) {
			TargetScenesDefine? targetObject = this.target as TargetScenesDefine;
			if (targetObject == null) return;
			foreach (SceneDefine sceneDef in targetObject.Scenes) {
				ScanAvatar(sceneDef, isLoadScene);
			}
			AssetDatabase.SaveAssetIfDirty(targetObject);
		}

		private void ScanAvatar(SceneDefine sceneDef, bool isLoadScene) {
			if (sceneDef.Scene == null) return;
			
			bool isAdditionalLoadedScene = false;
			string scenePath = AssetDatabase.GetAssetPath(sceneDef.Scene);
			Scene scene = SceneManager.GetSceneByPath(scenePath);
			switch (scene.isLoaded) {
				case false when !isLoadScene:
					return;
				case false:
					Debug.Log($"Opening Scene: {scenePath}");
					scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
					isAdditionalLoadedScene = true;
					break;
			}

			if (!scene.isLoaded) {
				Debug.LogError($"Can not open Scene : {scenePath}");
				return;
			}
				
			PipelineManager[] pipelineManagers = scene.GetRootGameObjects()
				.SelectMany(o => o.GetComponentsInChildren<PipelineManager>())
				.Where(manager => !string.IsNullOrEmpty(manager.blueprintId))
				.ToArray();
				
			foreach (PipelineManager pipelineManager in pipelineManagers) {
				string blueprintId = pipelineManager.blueprintId;
				bool already = false;
				foreach (AvatarDefine avatarDef in sceneDef.Avatars.Where(avatarDef => avatarDef.BlueprintID == blueprintId)) {
					already = true;
					if (avatarDef.ObjectName == pipelineManager.gameObject.name) break;
					avatarDef.ObjectName = pipelineManager.gameObject.name;
					this.target.MarkDirty();
					break;
				}
				if (already) continue;
					
				sceneDef.Avatars.Add(new AvatarDefine {
					Enable = true,
					Avatar = null,
					ObjectName = pipelineManager.gameObject.name,
					BlueprintID = blueprintId
				});
				this.target.MarkDirty();
			}
			List<AvatarDefine> newAvatars = sceneDef.Avatars
				.Where(define => pipelineManagers.Select(manager => manager.blueprintId).Contains(define.BlueprintID))
				.ToList();
			if (sceneDef.Avatars.Count != newAvatars.Count) {
				sceneDef.Avatars = newAvatars;
				this.target.MarkDirty();
			}

			if (isAdditionalLoadedScene) {
				EditorSceneManager.CloseScene(scene, true);
			}
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
				elementHeightCallback = this.ElementHeightCallback,
				drawElementCallback = this.DrawElementCallback,
				onAddCallback = OnAddCallback,
				onRemoveCallback = OnRemoveCallback,
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
		
		private static void OnAddCallback(ReorderableList list) {
			int lastIndex = list.serializedProperty.arraySize;
			list.serializedProperty.InsertArrayElementAtIndex(lastIndex);
			SerializedProperty addedElement = list.serializedProperty.GetArrayElementAtIndex(lastIndex);
			addedElement.FindPropertyRelative(nameof(SceneDefine.Enable)).boolValue = true;
			addedElement.FindPropertyRelative(nameof(SceneDefine.Scene)).objectReferenceValue = null;
			addedElement.FindPropertyRelative(nameof(SceneDefine.Avatars)).ClearArray();

		}
		
		private static void OnRemoveCallback(ReorderableList list) {
			int targetIndex = list.index;
			list.serializedProperty.DeleteArrayElementAtIndex(targetIndex);
		}
		
	}

	internal class SceneListElement {
		private const float ROOT_PADDING_TD = 2.0f;
		private const float PROPERTY_HEIGHT = 18.0f;
		
		private const float MARGIN_VERTICAL = 2.0f;
		private const float MARGIN_HORIZONTAL = 5.0f;
		
		private const float TOGGLE_WIDTH = 15.0f;

		private readonly SerializedObject _serializedObject;
		private readonly SerializedProperty _enableProperty;
		private readonly SerializedProperty _sceneProperty;
		private readonly SerializedProperty _avatarsProperty;

		private ReorderableList? _listView;
		private string? _avatarName;

		public SceneListElement(SerializedProperty serializedProperty) {
			this._serializedObject = serializedProperty.serializedObject;
			this._enableProperty = serializedProperty.FindPropertyRelative(nameof(SceneDefine.Enable));
			this._sceneProperty = serializedProperty.FindPropertyRelative(nameof(SceneDefine.Scene));
			this._avatarsProperty = serializedProperty.FindPropertyRelative(nameof(SceneDefine.Avatars));
		}

		public void OnGUI(Rect rootRect) {
			Rect currentRect = new(rootRect.x, rootRect.y + ROOT_PADDING_TD, rootRect.width, PROPERTY_HEIGHT);
			
			Rect rect = currentRect;
			EditorGUI.PropertyField(rect, this._enableProperty, GUIContent.none);
			rect.x += TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			rect.width -= TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			EditorGUI.PropertyField(rect, this._sceneProperty, GUIContent.none);
			currentRect.y += currentRect.height + MARGIN_VERTICAL;

			// rect = currentRect;
			// GUI.Button(rect, "Scan Avatar");
			// currentRect.y += currentRect.height + MARGIN_HORIZONTAL;

			rect = currentRect;
			this._listView ??= new ReorderableList(this._serializedObject, this._avatarsProperty) {
				draggable = true,
				drawHeaderCallback = _rect => EditorGUI.LabelField(_rect, "Avatars"),
				elementHeightCallback = GetAvatarElementHeight,
				drawElementCallback = this.DrawAvatarElement,
				displayAdd = false,
				displayRemove = false
			};
			this._listView.DoList(rect);
		}
		
		public float GetHeight() {
			float listHeight = this._listView?.GetHeight() ?? 0;
			return PROPERTY_HEIGHT + MARGIN_VERTICAL + 
			       // PROPERTY_HEIGHT + MARGIN_VERTICAL + 
			       listHeight + ROOT_PADDING_TD * 2;
		}

		private void DrawAvatarElement(Rect rootRect, int index, bool active, bool focused) {
			Rect currentRect = new (rootRect.x, rootRect.y + ROOT_PADDING_TD, rootRect.width, PROPERTY_HEIGHT);
			
			SerializedProperty avatarProperty = this._avatarsProperty.GetArrayElementAtIndex(index);
			SerializedProperty enableProperty = avatarProperty.FindPropertyRelative(nameof(AvatarDefine.Enable));
			SerializedProperty ObjectNameProperty = avatarProperty.FindPropertyRelative(nameof(AvatarDefine.ObjectName));
			SerializedProperty blueprintIdProperty = avatarProperty.FindPropertyRelative(nameof(AvatarDefine.BlueprintID));
			
			Rect rect = currentRect;
			EditorGUI.PropertyField(rect, enableProperty, GUIContent.none);
			rect.x += TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			rect.width -= TOGGLE_WIDTH + MARGIN_HORIZONTAL;
			EditorGUI.LabelField(rect, ObjectNameProperty.stringValue);

			currentRect.y += currentRect.height + MARGIN_VERTICAL;

			rect = currentRect;
			EditorGUI.BeginDisabledGroup(true);
			EditorGUI.PropertyField(rect, blueprintIdProperty, new GUIContent("Blueprint ID"));
			EditorGUI.EndDisabledGroup();
			currentRect.y += currentRect.height + MARGIN_VERTICAL;
		}

		private static float GetAvatarElementHeight(int index) {
			return PROPERTY_HEIGHT + MARGIN_VERTICAL + 
			       PROPERTY_HEIGHT + ROOT_PADDING_TD * 2;
		}
	}
}