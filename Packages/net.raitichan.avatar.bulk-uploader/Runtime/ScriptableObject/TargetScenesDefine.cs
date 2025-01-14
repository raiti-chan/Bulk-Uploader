﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject {
#if UNITY_EDITOR
	[CreateAssetMenu(menuName = "Bulk Upload TargetScenesDefine")]
#endif
	public class TargetScenesDefine : UnityEngine.ScriptableObject {
		public SceneDefine[] Scenes = null!;
	}

	[Serializable]
	public class SceneDefine {
		public bool Enable;
#if UNITY_EDITOR
		public SceneAsset? Scene;
#endif
		
		public List<AvatarDefine> Avatars = null!;
	}
	
	[Serializable]
	public class AvatarDefine {
		public bool Enable;
		public VRC_AvatarDescriptor? Avatar;
		public string? ObjectName;
		public string? BlueprintID;
	}
}