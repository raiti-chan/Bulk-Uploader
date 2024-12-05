using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Window.Element {
	internal class AvatarProgress : Box {
		
		private readonly ProgressBar _progressBar;
		
		
		public AvatarProgress(string avatarName) {
			Label label = new("Avatar : " + avatarName);
			this.Add(label);

			this._progressBar = new ProgressBar {
				value = 0,
				title = ""
			};
			this.Add(this._progressBar);
		}
		
		public void SetAvatarProgress(string? title, float value) {
			if (title != null) {
				this._progressBar.title = title;
			}

			if (value >= 0) {
				this._progressBar.value = value;
			}
		}

		public void SetAvatarError() {
			this.AddToClassList("error");
		}
		
		
	}
}