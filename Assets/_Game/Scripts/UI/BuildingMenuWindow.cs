using Eidren.Core.Services;
using Eidren.Data;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class BuildingMenuWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Text detailText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private BuildingMenuItemView[] itemViews = Array.Empty<BuildingMenuItemView>();

		[SerializeField]
		private Button placeButton;

		[SerializeField]
		private Button demolishButton;

		[SerializeField]
		private Button moveButton;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private BuildingPlacementBar placementBar;

		private readonly List<BuildingCatalogEntry> _entries = new List<BuildingCatalogEntry>();

		private BuildingCostDefinition[] _visible = Array.Empty<BuildingCostDefinition>();

		private ContentDatabase _content;

		private IMaterialStock _materials;

		private TechnologyUnlockService _technology;

		private int _selectedIndex;

		private float _nextStockRefresh;

		private readonly StringBuilder _text = new StringBuilder();

		private readonly StringBuilder _costText = new StringBuilder();

		public bool IsOpen { get; private set; }

		public bool IsPlacementMode { get; private set; }

		public int SelectedIndex => _selectedIndex;

		public IReadOnlyList<BuildingCostDefinition> VisibleBuildings => _visible;

		public IReadOnlyList<BuildingMenuItemView> ItemViews => itemViews;

		public BuildingPlacementBar PlacementBar => placementBar;

		public event Action<string> PlaceRequested;

		public event Action DemolishRequested;

		public event Action MoveRequested;

		public event Action Closed;

		public void ConfigureReferences(GameObject configuredPanelRoot, Text configuredDetailText, Text configuredFeedbackText, BuildingMenuItemView[] configuredItemViews, Button configuredPlaceButton, Button configuredDemolishButton, Button configuredCloseButton, BuildingPlacementBar configuredPlacementBar)
		{
			placementBar = configuredPlacementBar;
			panelRoot = configuredPanelRoot;
			detailText = configuredDetailText;
			feedbackText = configuredFeedbackText;
			itemViews = configuredItemViews;
			placeButton = configuredPlaceButton;
			demolishButton = configuredDemolishButton;
			closeButton = configuredCloseButton;
		}

		public void Initialize(ContentDatabase content, TechnologyUnlockService technology, IMaterialStock materials)
		{
			ValidateReferences();
			_content = content ?? throw new ArgumentNullException("content");
			_materials = materials ?? throw new ArgumentNullException("materials");
			if (_technology != null)
			{
				_technology.Changed -= RebuildVisible;
			}
			_technology = technology ?? throw new ArgumentNullException("technology");
			_technology.Changed += RebuildVisible;
			RebuildVisible();
			BindButtons();
			panelRoot.SetActive(value: false);
			Refresh();
		}

		private void RebuildVisible()
		{
			string b = ((_selectedIndex >= 0 && _selectedIndex < _visible.Length) ? _visible[_selectedIndex].Id : string.Empty);
			BuildingCatalog.Rebuild(_content, _technology, _entries);
			_entries.RemoveAll((BuildingCatalogEntry entry) => !entry.IsUnlocked);
			_visible = new BuildingCostDefinition[_entries.Count];
			for (int num = 0; num < _entries.Count; num++)
			{
				_visible[num] = _entries[num].Building;
			}
			for (int num2 = 0; num2 < itemViews.Length; num2++)
			{
				itemViews[num2].gameObject.SetActive(num2 < _visible.Length);
			}
			_selectedIndex = 0;
			for (int num3 = 0; num3 < _visible.Length; num3++)
			{
				if (string.Equals(_visible[num3].Id, b, StringComparison.Ordinal))
				{
					_selectedIndex = num3;
				}
			}
			if (IsOpen)
			{
				Refresh();
			}
		}

		private void BindButtons()
		{
			placeButton.onClick.RemoveAllListeners();
			demolishButton.onClick.RemoveAllListeners();
			moveButton.onClick.RemoveAllListeners();
			closeButton.onClick.RemoveAllListeners();
			placeButton.onClick.AddListener(RequestPlacement);
			demolishButton.onClick.AddListener(delegate
			{
				this.DemolishRequested?.Invoke();
			});
			moveButton.onClick.AddListener(delegate
			{
				this.MoveRequested?.Invoke();
			});
			closeButton.onClick.AddListener(Close);
		}

		private void Update()
		{
			if (IsOpen && !(Time.unscaledTime < _nextStockRefresh))
			{
				_nextStockRefresh = Time.unscaledTime + 0.15f;
				Refresh();
			}
		}

		public void Open()
		{
			IsOpen = true;
			IsPlacementMode = false;
			panelRoot.SetActive(value: true);
			feedbackText.text = string.Empty;
			Refresh();
			FocusSelected();
		}

		public void Close()
		{
			if (IsOpen)
			{
				IsOpen = false;
				IsPlacementMode = false;
				panelRoot.SetActive(value: false);
				placementBar?.SetVisible(visible: false);
				this.Closed?.Invoke();
			}
		}

		public void SetPlacementMode(bool active)
		{
			if (IsOpen)
			{
				IsPlacementMode = active;
				panelRoot.SetActive(!active);
				placementBar?.SetVisible(active);
			}
		}

		public void MoveSelection(int delta)
		{
			if (_visible.Length != 0 && delta != 0)
			{
				_selectedIndex = (_selectedIndex + Math.Sign(delta) + _visible.Length) % _visible.Length;
				Refresh();
				FocusSelected();
			}
		}

		public void ActivateSelected()
		{
			RequestPlacement();
		}

		public void SetFeedback(string message, bool success = false)
		{
			feedbackText.text = message ?? string.Empty;
			feedbackText.color = (success ? new Color(0.45f, 0.95f, 0.6f) : new Color(1f, 0.55f, 0.45f));
		}

		private void Select(int index)
		{
			if (index >= 0 && index < _visible.Length)
			{
				_selectedIndex = index;
				ScrollSelectedIntoView(index);
				Refresh();
			}
		}

		// F31-012: Der Katalog scrollt horizontal; Tastatur- und Gamepad-
		// Auswahl muss die gewaehlte Karte in den maskierten Viewport holen.
		private void ScrollSelectedIntoView(int index)
		{
			if (itemViews.Length == 0 || _visible.Length <= 1)
			{
				return;
			}
			ScrollRect scroll = itemViews[0].GetComponentInParent<ScrollRect>();
			if (scroll != null)
			{
				scroll.horizontalNormalizedPosition = Mathf.Clamp01(index / (float)(_visible.Length - 1));
			}
		}

		private void RequestPlacement()
		{
			if (_selectedIndex >= 0 && _selectedIndex < _visible.Length)
			{
				this.PlaceRequested?.Invoke(_visible[_selectedIndex].Id);
			}
		}

		private void ValidateReferences()
		{
			if (panelRoot == null || detailText == null || feedbackText == null || itemViews == null || itemViews.Length < 11 || placeButton == null || demolishButton == null || moveButton == null || closeButton == null)
			{
				throw new InvalidOperationException("BuildingMenuWindow prefab references are incomplete.");
			}
		}

		private void OnDestroy()
		{
			if (_technology != null)
			{
				_technology.Changed -= RebuildVisible;
			}
		}

		private void FocusSelected()
		{
			if (_selectedIndex >= 0 && _selectedIndex < _visible.Length && _selectedIndex < itemViews.Length)
			{
				EventSystem.current?.SetSelectedGameObject(itemViews[_selectedIndex].gameObject);
			}
		}

		private void Refresh()
		{
			for (int i = 0; i < itemViews.Length; i++)
			{
				if (i < _entries.Count)
				{
					itemViews[i].Bind(i, RowFor(i), Select);
					itemViews[i].SetSelected(i == _selectedIndex);
				}
			}
			bool flag = _selectedIndex >= 0 && _selectedIndex < _visible.Length;
			placeButton.interactable = flag;
			detailText.text = (flag ? BuildDetails(_entries[_selectedIndex]) : "Kein Bauplan vorhanden.");
		}

		private BuildingMenuRow RowFor(int index)
		{
			BuildingCatalogEntry entry = _entries[index];
			// Der Katalog ist nach Kategorie sortiert: Die Ueberschrift traegt
			// jeweils nur die erste Zeile einer Kategorie als Gruppeneroeffnung.
			bool opensCategory = index == 0 || _entries[index - 1].Category != entry.Category;
			string category = (opensCategory ? CategoryText(entry.Category) : string.Empty);
			return new BuildingMenuRow(entry.Building.DisplayName, entry.Building.Icon, entry.IsUnlocked ? CostText(entry.Building) : RequirementText(in entry), category, !entry.IsUnlocked);
		}

		private string CostText(BuildingCostDefinition building)
		{
			_costText.Clear();
			for (int i = 0; i < building.Cost.Count; i++)
			{
				if (i > 0)
				{
					_costText.Append("  ·  ");
				}
				CraftingIngredient craftingIngredient = building.Cost[i];
				_costText.Append(CompactCostEntry(NameOf(craftingIngredient.ItemId), craftingIngredient.Amount, _materials.GetTotalAmount(craftingIngredient.ItemId)));
			}
			return _costText.ToString();
		}

		private static string RequirementText(in BuildingCatalogEntry entry)
		{
			if (!string.IsNullOrEmpty(entry.Requirement))
			{
				return "Benötigt: " + entry.Requirement;
			}
			return "Noch nicht freigeschaltet";
		}

		private string BuildDetails(in BuildingCatalogEntry entry)
		{
			_text.Clear();
			_text.Append(entry.Building.DisplayName);
			_text.Append(" · ");
			_text.Append(CategoryText(entry.Category));
			_text.Append('\n');
			_text.Append("Kosten:");
			for (int index = 0; index < entry.Building.Cost.Count; index++)
			{
				CraftingIngredient ingredient = entry.Building.Cost[index];
				_text.Append('\n');
				_text.Append("  ");
				_text.Append(DetailCostLine(NameOf(ingredient.ItemId), ingredient.Amount, _materials.GetTotalAmount(ingredient.ItemId)));
			}
			if (!entry.IsUnlocked)
			{
				_text.Append('\n');
				_text.Append(RequirementText(in entry));
				return _text.ToString();
			}
			if (entry.Building.TryGetLevel(1, out var footprint, out var _))
			{
				_text.Append("\nRaster: ");
				_text.Append(footprint.Width);
				_text.Append(" × ");
				_text.Append(footprint.Depth);
			}
			return _text.ToString();
		}

		private string NameOf(string itemId)
		{
			if (!_content.TryGetItem(itemId, out var value))
			{
				return itemId;
			}
			return value.DisplayName;
		}

		public static string CategoryText(BuildingCategory category)
		{
			return category switch
			{
				BuildingCategory.Structure => "Struktur",
				BuildingCategory.Workshops => "Werkstätten",
				BuildingCategory.Supply => "Versorgung",
				BuildingCategory.Farming => "Landwirtschaft",
				_ => "Sonstiges",
			};
		}

		/// <summary>
		/// Kompakte Kostenangabe für die Katalogkarte. Eine Fehlmenge wird
		/// zusätzlich im Text genannt, damit sie nicht allein an der Farbe hängt.
		/// </summary>
		public static string CompactCostEntry(string itemName, int required, int available)
		{
			string entry = $"{itemName} {required}/{available}";
			return (available < required) ? $"{entry} (fehlt {required - available})" : entry;
		}

		/// <summary>
		/// Ausführliche Kostenzeile für den Detailbereich: Bedarf und Bestand
		/// sind benannt, die Reihenfolge der Zahlen muss nicht bekannt sein.
		/// </summary>
		public static string DetailCostLine(string itemName, int required, int available)
		{
			string line = $"{itemName}: {required} benötigt · {available} vorhanden";
			return (available < required) ? $"{line} · es fehlen {required - available}" : line;
		}
	}
}
