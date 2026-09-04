using Eidren.Data;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CraftingIngredientView : MonoBehaviour
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text label;

		[SerializeField]
		private Text amountLabel;

		public void ConfigureReferences(Image configuredIcon, Text configuredLabel, Text configuredAmountLabel)
		{
			icon = configuredIcon;
			label = configuredLabel;
			amountLabel = configuredAmountLabel;
		}

		public void Render(ItemDefinition item, int current, int required)
		{
			base.gameObject.SetActive(item != null);
			if (!(item == null))
			{
				icon.sprite = item.Icon;
				icon.enabled = item.Icon != null;
				label.text = item.DisplayName;
				amountLabel.text = $"{current} / {required}";
				amountLabel.color = ((current >= required) ? new Color(0.58f, 0.88f, 0.62f) : new Color(0.96f, 0.48f, 0.39f));
			}
		}
	}
}
