using System.Collections.Generic;
using UnityEngine.UIElements;
using VRC.SDKBase;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Window.Element {
	internal class SceneProgress : Box {

		private readonly Dictionary<string, AvatarProgress> _avatarProgressesDict = new();
		private readonly VisualElement _avatarProgressGroup;
		private readonly ProgressBar _progressBar;
		
		public SceneProgress(string sceneName) {
			Label label = new("Scene :" + sceneName);
			this.Add(label);
			
			this._progressBar = new ProgressBar {
				value = 0,
				title = ""
			};
			this.Add(this._progressBar);

			this._avatarProgressGroup = new VisualElement();
			this._avatarProgressGroup .AddToClassList("avatar-progress-group");
			this.Add(this._avatarProgressGroup);
		}

		public void RegisterAvatar(string blueprintId, string avatarName) {
			if (this._avatarProgressesDict.ContainsKey(blueprintId)) return;
			AvatarProgress avatarProgress = new(blueprintId, avatarName);
			this._avatarProgressGroup.Add(avatarProgress);
			this._avatarProgressesDict.Add(blueprintId, avatarProgress);
			
		}
		
		public void SetAvatarProgress(string blueprintId, string? title, float value) {
			if (this._avatarProgressesDict.TryGetValue(blueprintId, out AvatarProgress progress)) {
				progress.SetAvatarProgress(title, value);
			}
		}
		
		internal void SetAvatarError(string blueprintId) {
			if (this._avatarProgressesDict.TryGetValue(blueprintId, out AvatarProgress progress)) {
				progress.SetAvatarError();
			}
		}

		public void SetSceneProgress(string? title, float value) {
			if (title != null) {
				this._progressBar.title = title;
			}

			if (value >= 0) {
				this._progressBar.value = value;
			}
		}
		
		

	}
}