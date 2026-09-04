using Eidren.Data;

namespace Eidren.Combat
{
	/// <summary>
	/// Darstellung der aktuell getragenen Spielerwaffe, entkoppelt von der
	/// konkreten Sprite- bzw. 3D-Umsetzung.
	/// </summary>
	public interface IPlayerWeaponPresentation
	{
		/// <summary>Zeigt die uebergebene Waffe an (null blendet alle Waffen aus).</summary>
		void Show(WeaponData weapon);

		/// <summary>Spielt die Angriffsanimation/-pose fuer die uebergebene Waffenfamilie ab.</summary>
		void PlayAttack(float duration, int comboIndex, WeaponFamily family);
	}
}
