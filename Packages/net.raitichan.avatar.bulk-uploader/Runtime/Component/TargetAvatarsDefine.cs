﻿using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Runtime.Component {
	public class TargetAvatarsDefine : MonoBehaviour {
		public AvatarDefine[] Avatars = null!;

#if UNITY_EDITOR
		[MenuItem("Raitichan/BulkUploader/CreateTargetAvatarDefine")]
		public static void CreateTargetAvatarDefineObject() {
			GameObject gameObject = new() {
				name = "TargetAvatarDefine"
			};

			gameObject.AddComponent<TargetAvatarsDefine>();

			Undo.RegisterCreatedObjectUndo(gameObject, "Create TargetAvatarDefine Object");
		}
#endif
	}
}