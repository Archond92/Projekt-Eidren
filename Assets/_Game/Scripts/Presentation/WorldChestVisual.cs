using System;
using UnityEngine;

namespace Eidren.Presentation
{
	[DisallowMultipleComponent]
	public sealed class WorldChestVisual : MonoBehaviour
	{
		[SerializeField]
		private Transform lid;

		[SerializeField]
		private GameObject closedDetails;

		[SerializeField]
		private GameObject openedDetails;

		[SerializeField]
		private GameObject emptiedDetails;

		[SerializeField]
		private Vector3 openedLidEuler = new Vector3(-72f, 0f, 0f);

		[SerializeField]
		private SpriteRenderer leftHand;

		[SerializeField]
		private SpriteRenderer rightHand;

		[SerializeField]
		private GameObject lootFull;

		[SerializeField]
		private GameObject lootPartial;

		private Quaternion _closedRotation;

		private Vector3 _leftHandStart;

		private Vector3 _rightHandStart;

		public void Configure(Transform configuredLid, GameObject configuredClosed, GameObject configuredOpened, GameObject configuredEmptied, SpriteRenderer configuredLeftHand = null, SpriteRenderer configuredRightHand = null, GameObject configuredLootFull = null, GameObject configuredLootPartial = null)
		{
			lid = configuredLid;
			closedDetails = configuredClosed;
			openedDetails = configuredOpened;
			emptiedDetails = configuredEmptied;
			leftHand = configuredLeftHand;
			rightHand = configuredRightHand;
			lootFull = configuredLootFull;
			lootPartial = configuredLootPartial;
			_closedRotation = ((lid != null) ? lid.localRotation : Quaternion.identity);
			CacheHands();
			SetOpeningProgress(0f);
		}

		public void SetOpeningProgress(float normalizedProgress)
		{
			float num = Mathf.Clamp01(normalizedProgress);
			bool visible = num > 0f && num < 1f;
			AnimateHand(leftHand, _leftHandStart, num, -1f, visible);
			AnimateHand(rightHand, _rightHandStart, num, 1f, visible);
		}

		public void Apply(WorldChestVisualState status)
		{
			if (lid != null)
			{
				EnsureClosedRotation();
				lid.localRotation = ((status == WorldChestVisualState.Closed) ? _closedRotation : (_closedRotation * Quaternion.Euler(openedLidEuler)));
			}
			if (closedDetails != null)
			{
				closedDetails.SetActive(status == WorldChestVisualState.Closed);
			}
			if (openedDetails != null)
			{
				openedDetails.SetActive(status == WorldChestVisualState.Opened || status == WorldChestVisualState.PartiallyEmptied);
			}
			if (emptiedDetails != null)
			{
				emptiedDetails.SetActive(status == WorldChestVisualState.Emptied);
			}
			if (lootFull != null)
			{
				lootFull.SetActive(status == WorldChestVisualState.Opened);
			}
			if (lootPartial != null)
			{
				lootPartial.SetActive(status == WorldChestVisualState.PartiallyEmptied);
			}
			if (status != WorldChestVisualState.Closed)
			{
				SetOpeningProgress(1f);
			}
		}

		private void Awake()
		{
			if (lid != null)
			{
				_closedRotation = lid.localRotation;
			}
			CacheHands();
			SetOpeningProgress(0f);
		}

		private void EnsureClosedRotation()
		{
			// Non-serialized runtime state is normally initialized by Awake. Editor
			// captures and prefab contract tests may call Apply before Awake, where
			// Quaternion's default (0,0,0,0) is not a usable rotation.
			float magnitude = _closedRotation.x * _closedRotation.x
				+ _closedRotation.y * _closedRotation.y
				+ _closedRotation.z * _closedRotation.z
				+ _closedRotation.w * _closedRotation.w;
			if (magnitude < 0.5f)
			{
				_closedRotation = lid.localRotation;
			}
		}

		private void CacheHands()
		{
			if (leftHand != null)
			{
				_leftHandStart = leftHand.transform.localPosition;
			}
			if (rightHand != null)
			{
				_rightHandStart = rightHand.transform.localPosition;
			}
		}

		private static void AnimateHand(SpriteRenderer hand, Vector3 start, float progress, float side, bool visible)
		{
			if (!(hand == null))
			{
				hand.enabled = visible;
				float num = Mathf.Sin(progress * (float)Math.PI) * 0.2f;
				hand.transform.localPosition = start + new Vector3(side * num, progress * 0.16f, 0f);
				hand.transform.localRotation = Quaternion.Euler(58f, side * 18f, side * Mathf.Lerp(18f, 4f, progress));
			}
		}
	}
}
