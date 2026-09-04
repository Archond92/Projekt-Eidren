using Eidren.Data;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CraftingRecipeButtonView : MonoBehaviour, ISubmitHandler, IEventSystemHandler
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text label;

		[SerializeField]
		private Text stateLabel;

		[SerializeField]
		private Image selectionFrame;

		[SerializeField]
		private CanvasGroup canvasGroup;

		private int _index;

		private Action<int> _select;

		public Button Button => button;

		public float Opacity => (canvasGroup != null) ? canvasGroup.alpha : 1f;

		public void ConfigureReferences(Button configuredButton, Image configuredIcon, Text configuredLabel, Text configuredStateLabel, Image configuredSelectionFrame, CanvasGroup configuredCanvasGroup)
		{
			button = configuredButton;
			icon = configuredIcon;
			label = configuredLabel;
			stateLabel = configuredStateLabel;
			selectionFrame = configuredSelectionFrame;
			canvasGroup = configuredCanvasGroup;
		}

		public void Bind(int index, Action<int> select)
		{
			_index = index;
			_select = select;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate
			{
				_select?.Invoke(_index);
			});
		}

		public void Render(CraftingRecipeDefinition recipe, bool selected, string state, bool locked)
		{
			base.gameObject.SetActive(recipe != null);
			if (!(recipe == null))
			{
				label.text = recipe.DisplayName;
				icon.sprite = recipe.Icon;
				icon.enabled = recipe.Icon != null;
				stateLabel.text = state ?? string.Empty;
				selectionFrame.enabled = selected;
				if (canvasGroup != null)
				{
					canvasGroup.alpha = (locked ? 0.42f : 1f);
				}
				stateLabel.color = (locked ? new Color(0.58f, 0.61f, 0.61f, 1f) : new Color(0.95f, 0.64f, 0.17f, 1f));
			}
		}

		public void OnSubmit(BaseEventData eventData)
		{
			_select?.Invoke(_index);
		}
	}
}
