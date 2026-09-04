using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class TechnologyNodeButtonView : MonoBehaviour, ISubmitHandler, IEventSystemHandler
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image stateSurface;

		[SerializeField]
		private Text orderLabel;

		[SerializeField]
		private Text nameLabel;

		[SerializeField]
		private Text stateLabel;

		[SerializeField]
		private Image selectionFrame;

		private int _index;

		private Action<int> _select;

		public string NodeId { get; private set; } = string.Empty;

		public TechnologyNodeState State { get; private set; }

		public Button Button => button;

		public void ConfigureReferences(Button configuredButton, Image configuredStateSurface, Text configuredOrderLabel, Text configuredNameLabel, Text configuredStateLabel, Image configuredSelectionFrame)
		{
			button = configuredButton;
			stateSurface = configuredStateSurface;
			orderLabel = configuredOrderLabel;
			nameLabel = configuredNameLabel;
			stateLabel = configuredStateLabel;
			selectionFrame = configuredSelectionFrame;
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

		public void Render(TechnologyNodeDefinition node, TechnologyNodeState state, bool selected, string lockReason = "")
		{
			base.gameObject.SetActive(node != null);
			if (node != null)
			{
				NodeId = node.Id;
				State = state;
				orderLabel.text = node.SortOrder.ToString("00");
				nameLabel.text = node.DisplayName;
				Text text = stateLabel;
				if (1 == 0)
				{
				}
				string text2 = state switch
				{
					TechnologyNodeState.Unlocked => "FREIGESCHALTET", 
					TechnologyNodeState.Available => "VERFUEGBAR", 
					_ => string.IsNullOrWhiteSpace(lockReason) ? "GESPERRT" : lockReason.ToUpperInvariant(), 
				};
				if (1 == 0)
				{
				}
				text.text = text2;
				Image image = stateSurface;
				if (1 == 0)
				{
				}
				Color color = state switch
				{
					TechnologyNodeState.Unlocked => new Color(0.18f, 0.48f, 0.35f, 1f), 
					TechnologyNodeState.Available => new Color(0.58f, 0.39f, 0.12f, 1f), 
					_ => new Color(0.12f, 0.17f, 0.19f, 1f), 
				};
				if (1 == 0)
				{
				}
				image.color = color;
				selectionFrame.enabled = selected;
			}
		}

		public void OnSubmit(BaseEventData eventData)
		{
			_select?.Invoke(_index);
		}
	}
}
