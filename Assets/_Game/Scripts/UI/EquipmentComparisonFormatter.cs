using Eidren.Core.Services;
using Eidren.Data;
using System.Text;

namespace Eidren.UI
{
	public static class EquipmentComparisonFormatter
	{
		public static string Format(ItemStack stack, ItemDefinition item, ContentDatabase database)
		{
			if (item == null || stack.IsEmpty)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("\n\nT").Append(item.Tier);
			if (item.MaximumDurability > 0)
			{
				stringBuilder.Append(" · Haltbarkeit ").Append(stack.Durability).Append('/')
					.Append(item.MaximumDurability);
			}
			if (item.ProtectionContribution > 0f)
			{
				stringBuilder.Append("\nSchutzbeitrag ").Append(item.ProtectionContribution.ToString("P0"));
			}
			if (item.Category == ItemCategory.Weapon && database != null && database.TryGetWeapon(item.Id, out var value))
			{
				stringBuilder.Append("\nSchaden ").Append(value.BaseDamage.ToString("0.0")).Append(" · Taumel ")
					.Append(value.BaseStaggerDamage.ToString("0.0"));
				if (!string.IsNullOrWhiteSpace(value.Identity.SignatureHint))
				{
					stringBuilder.Append("\nSignatur: ").Append(value.Identity.SignatureHint);
				}
				if (value.Identity.IsNamedVariant)
				{
					stringBuilder.Append("\nBenannte feste Variante");
				}
			}
			return stringBuilder.ToString();
		}
	}
}
