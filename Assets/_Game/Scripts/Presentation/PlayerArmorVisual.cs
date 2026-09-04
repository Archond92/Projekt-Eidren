using UnityEngine;

namespace Eidren.Presentation
{
	public sealed class PlayerArmorVisual : MonoBehaviour
	{
		[SerializeField]
		private GameObject headRoot;

		[SerializeField]
		private GameObject chestRoot;

		[SerializeField]
		private GameObject handsRoot;

		[SerializeField]
		private GameObject legsRoot;

		public GameObject HeadRoot => headRoot;

		public GameObject ChestRoot => chestRoot;

		public GameObject HandsRoot => handsRoot;

		public GameObject LegsRoot => legsRoot;

		public void Configure(GameObject head, GameObject chest, GameObject hands, GameObject legs)
		{
			headRoot = head;
			chestRoot = chest;
			handsRoot = hands;
			legsRoot = legs;
			ShowEmpty();
		}

		public void Show(bool head, bool chest, bool hands, bool legs)
		{
			SetVisible(headRoot, head);
			SetVisible(chestRoot, chest);
			SetVisible(handsRoot, hands);
			SetVisible(legsRoot, legs);
		}

		public void ShowEmpty()
		{
			SetVisible(headRoot, visible: false);
			SetVisible(chestRoot, visible: false);
			SetVisible(handsRoot, visible: false);
			SetVisible(legsRoot, visible: false);
		}

		private static void SetVisible(GameObject target, bool visible)
		{
			if (target != null && target.activeSelf != visible)
			{
				target.SetActive(visible);
			}
		}
	}
}
