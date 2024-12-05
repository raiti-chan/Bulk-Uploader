using System;
using UnityEngine.UIElements;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.Inspector.Element {
	public class AdvancedListView : ListView {
		
		private readonly Box _headerBox;

		private Func<VisualElement>? _makeHeader;
		public Func<VisualElement>? makeHeader {
			get => this._makeHeader;
			set {
				if (value == this._makeHeader) return;
				this._makeHeader = value;
				this.RebuildHeader();
			}
		}
		
		public AdvancedListView(string title) {
			this.focusable = true;
			this.showAddRemoveFooter = true;
			this.reorderable = true;
			this.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
			this.showBorder = true;
			this.headerTitle = title;
			this.showFoldoutHeader = false;
			this.reorderMode = ListViewReorderMode.Animated;
			
			this._headerBox = new Box();
			this._headerBox.AddToClassList("header");
			this.hierarchy.Insert(0, this._headerBox);
		}

		private void RebuildHeader() {
			this._headerBox.Clear();
			VisualElement? headerContent = this.makeHeader?.Invoke();
			if (headerContent == null) return;
			this._headerBox.Add(headerContent);
		}
		
	}
}