using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class BuildingMenuItemView : MonoBehaviour, ISelectHandler, IEventSystemHandler
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text label;

		[SerializeField]
		private Image stateMarker;

		[SerializeField]
		private Text costLabel;

		[SerializeField]
		private GameObject categoryHeader;

		[SerializeField]
		private Text categoryLabel;

		private const float HeaderRowHeight = 92f;

		private int _index;

		private Action<int> _selected;

		private LayoutElement _layout;

		public Button Button => button;

		public Image Icon => icon;

		public Text CostLabel => costLabel;

		public GameObject CategoryHeader => categoryHeader;

		private void Awake()
		{
			_layout = GetComponent<LayoutElement>();
		}

		public void ConfigureReferences(Button configuredButton, Image configuredIcon, Text configuredLabel, Image configuredStateMarker, Text configuredCostLabel, GameObject configuredCategoryHeader, Text configuredCategoryLabel)
		{
			button = configuredButton;
			icon = configuredIcon;
			label = configuredLabel;
			stateMarker = configuredStateMarker;
			costLabel = configuredCostLabel;
			categoryHeader = configuredCategoryHeader;
			categoryLabel = configuredCategoryLabel;
		}

		public void Bind(int index, in BuildingMenuRow row, Action<int> selected)
		{
			_index = index;
			_selected = selected;
			label.text = row.Name;
			icon.sprite = row.Icon;
			icon.enabled = row.Icon != null;
			label.color = (row.Locked ? new Color(0.62f, 0.64f, 0.68f) : new Color(0.92f, 0.93f, 0.9f));
			if (costLabel != null)
			{
				costLabel.text = row.Cost;
				costLabel.color = (row.Locked ? new Color(0.72f, 0.63f, 0.42f) : new Color(0.72f, 0.76f, 0.78f));
			}
			ShowCategory(row.Category);
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(Select);
		}

		public void SetSelected(bool selected)
		{
			if (stateMarker != null)
			{
				stateMarker.color = (selected ? new Color(0.35f, 0.9f, 0.55f, 1f) : new Color(0.35f, 0.4f, 0.45f, 1f));
			}
		}

		private void ShowCategory(string category)
		{
			if (!(categoryHeader == null))
			{
				bool flag = !string.IsNullOrEmpty(category);
				categoryHeader.SetActive(flag);
				if (flag && categoryLabel != null)
				{
					categoryLabel.text = category;
				}
				if (_layout != null)
				{
					_layout.preferredHeight = (flag ? 92f : (-1f));
				}
			}
		}

		private void Select()
		{
			_selected?.Invoke(_index);
		}

		public void OnSelect(BaseEventData eventData)
		{
			Select();
		}
	}

	public readonly struct BuildingMenuRow
	{
		public string Name { get; }

		public Sprite Icon { get; }

		public string Cost { get; }

		public string Category { get; }

		public bool Locked { get; }

		public BuildingMenuRow(string name, Sprite icon, string cost, string category, bool locked)
		{
			Name = name ?? string.Empty;
			Icon = icon;
			Cost = cost ?? string.Empty;
			Category = category ?? string.Empty;
			Locked = locked;
		}
	}
}
