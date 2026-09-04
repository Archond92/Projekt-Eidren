using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Combat/Weapon")]
	public sealed class WeaponData : ScriptableObject
	{
		[SerializeField]
		private string id;

		public string DisplayName;

		public Color Accent = Color.white;

		public float BaseDamage = 10f;

		public float BaseStaggerDamage = 10f;

		public float AttackRange = 2.4f;

		public float AttackAssistAngle = 120f;

		public float BackDamageMultiplier = 1f;

		public AttackStepData[] Combo;

		public WeaponIdentityData Identity;

		[Tooltip("Optionaler Gattungsdatensatz fuer Reichweite, Assistenz und Kombo. Varianten tragen nur eigene feste Schadenswerte.")]
		public WeaponData Moveset;

		public string Id => id;

		public WeaponData ResolvedMoveset => (Moveset != null) ? Moveset : this;
	}
}
