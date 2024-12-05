using System;
using UnityEditor;
using UnityEngine;

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
#if UNITY_EDITOR
		public SceneAsset? Scene;
#endif
	}
}