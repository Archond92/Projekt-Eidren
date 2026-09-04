using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class InteractionSystemTests
{
	private sealed class TestInteractable : IInteractable
	{
		public string InteractionId { get; }

		public InteractionType Type => InteractionType.WorldObject;

		public string DisplayText => "TEST";

		public Sprite Icon => null;

		public float InteractionRange => 5f;

		public Eidren.Interaction.InteractionMode Mode { get; }

		public float HoldDuration { get; }

		public int Priority { get; }

		public Vector3 InteractionPosition => InteractionObject.transform.position;

		public GameObject InteractionObject { get; }

		public int BeginCount { get; private set; }

		public int CancelCount { get; private set; }

		public int CompleteCount { get; private set; }

		public TestInteractable(GameObject owner, string id, Eidren.Interaction.InteractionMode mode, float duration, int priority)
		{
			InteractionObject = owner;
			InteractionId = id;
			Mode = mode;
			HoldDuration = duration;
			Priority = priority;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			blockedReason = string.Empty;
			return true;
		}

		public void BeginInteraction(in InteractionContext context)
		{
			BeginCount++;
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			CancelCount++;
		}

		public void CompleteInteraction(in InteractionContext context)
		{
			CompleteCount++;
		}

		bool IInteractable.CanInteract(in InteractionContext context, out string blockedReason)
		{
			return CanInteract(in context, out blockedReason);
		}

		void IInteractable.BeginInteraction(in InteractionContext context)
		{
			BeginInteraction(in context);
		}

		void IInteractable.UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			UpdateInteraction(in context, normalizedProgress);
		}

		void IInteractable.CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			CancelInteraction(in context, reason);
		}

		void IInteractable.CompleteInteraction(in InteractionContext context)
		{
			CompleteInteraction(in context);
		}
	}

	private GameObject _actor;

	private InteractionContext _context;

	[SetUp]
	public void SetUp()
	{
		_actor = new GameObject("InteractionTest_Actor");
		_actor.transform.forward = Vector3.forward;
		_context = new InteractionContext(_actor, _actor.transform, Vector3.forward);
	}

	[TearDown]
	public void TearDown()
	{
		if (_actor != null)
		{
			UnityEngine.Object.DestroyImmediate(_actor);
		}
		GameObject[] array = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (GameObject target in array)
		{
			if (target != null && target.name.StartsWith("InteractionTest_", StringComparison.Ordinal))
			{
				UnityEngine.Object.DestroyImmediate(target);
			}
		}
	}

	[Test]
	public void InstantInteraction_CompletesExactlyOnce()
	{
		TestInteractable target = CreateTarget("instant", Vector3.forward, Eidren.Interaction.InteractionMode.Instant);
		InteractionSession interactionSession = new InteractionSession();
		Assert.That<bool>(interactionSession.Begin(target, in _context), (IResolveConstraint)(object)Is.True);
		interactionSession.Tick(10f, inputHeld: true, in _context);
		Assert.That<int>(target.BeginCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<InteractionState>(interactionSession.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Completed));
	}

	[Test]
	public void HoldInteraction_RequiresConfiguredDuration()
	{
		TestInteractable target = CreateTarget("hold", Vector3.forward, Eidren.Interaction.InteractionMode.Hold, 2.25f);
		InteractionSession interactionSession = new InteractionSession();
		Assert.That<bool>(interactionSession.Begin(target, in _context), (IResolveConstraint)(object)Is.True);
		interactionSession.Tick(2.24f, inputHeld: true, in _context);
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
		interactionSession.Tick(0.01f, inputHeld: true, in _context);
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<float>(interactionSession.Progress, (IResolveConstraint)(object)Is.EqualTo((object)1f));
	}

	[Test]
	public void InputRelease_CancelsHold()
	{
		AssertCancelledByTick(InteractionCancelReason.InputReleased, held: false, moveOutOfRange: false);
	}

	[Test]
	public void TimedInteraction_ContinuesAfterInputRelease()
	{
		TestInteractable target = CreateTarget("timed", Vector3.forward, Eidren.Interaction.InteractionMode.Timed);
		InteractionSession interactionSession = new InteractionSession();
		Assert.That<bool>(interactionSession.Begin(target, in _context), (IResolveConstraint)(object)Is.True);
		interactionSession.Tick(0.45f, inputHeld: false, in _context);
		Assert.That<bool>(interactionSession.IsActive, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(interactionSession.Progress, (IResolveConstraint)(object)Is.EqualTo((object)0.45f).Within((object)0.001f));
		interactionSession.Tick(0.55f, inputHeld: false, in _context);
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<InteractionState>(interactionSession.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Completed));
	}

	[Test]
	public void DistanceLoss_CancelsHold()
	{
		AssertCancelledByTick(InteractionCancelReason.OutOfRange, held: true, moveOutOfRange: true);
	}

	[TestCase(InteractionCancelReason.AttackStarted)]
	[TestCase(InteractionCancelReason.DodgeStarted)]
	[TestCase(InteractionCancelReason.PlayerDamaged)]
	[TestCase(InteractionCancelReason.PlayerDied)]
	[TestCase(InteractionCancelReason.SceneTransition)]
	[TestCase(InteractionCancelReason.InputDisabled)]
	public void ExternalPlayerEvents_CancelHold(InteractionCancelReason reason)
	{
		TestInteractable target = CreateTarget($"cancel.{reason}", Vector3.forward);
		InteractionSession interactionSession = new InteractionSession();
		interactionSession.Begin(target, in _context);
		interactionSession.Tick(0.3f, inputHeld: true, in _context);
		interactionSession.Cancel(in _context, reason);
		Assert.That<InteractionState>(interactionSession.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Cancelled));
		Assert.That<InteractionCancelReason>(interactionSession.LastCancelReason, (IResolveConstraint)(object)Is.EqualTo((object)reason));
		Assert.That<int>(target.CancelCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void TargetDeactivation_CancelsWithoutCompletion()
	{
		TestInteractable target = CreateTarget("disabled", Vector3.forward);
		InteractionSession interactionSession = new InteractionSession();
		interactionSession.Begin(target, in _context);
		target.InteractionObject.SetActive(value: false);
		interactionSession.Tick(10f, inputHeld: true, in _context);
		Assert.That<InteractionState>(interactionSession.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Cancelled));
		Assert.That<InteractionCancelReason>(interactionSession.LastCancelReason, (IResolveConstraint)(object)Is.EqualTo((object)InteractionCancelReason.TargetUnavailable));
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void TargetDestruction_CancelsWithoutExceptionOrCompletion()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		TestInteractable target = CreateTarget("destroyed", Vector3.forward);
		InteractionSession session = new InteractionSession();
		session.Begin(target, in _context);
		UnityEngine.Object.DestroyImmediate(target.InteractionObject);
		Assert.DoesNotThrow((TestDelegate)delegate
		{
			session.Tick(10f, inputHeld: true, in _context);
		});
		Assert.That<InteractionState>(session.State, (IResolveConstraint)(object)Is.EqualTo((object)InteractionState.Cancelled));
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void CompletionNeverOccursAfterCancellation()
	{
		TestInteractable target = CreateTarget("no.late.complete", Vector3.forward);
		InteractionSession interactionSession = new InteractionSession();
		interactionSession.Begin(target, in _context);
		interactionSession.Tick(0.4f, inputHeld: true, in _context);
		interactionSession.Cancel(in _context, InteractionCancelReason.AttackStarted);
		interactionSession.Tick(20f, inputHeld: true, in _context);
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void HigherPriorityTargetWins()
	{
		// Gleich weit entfernt und gleich gut anvisiert — hier und nur hier
		// entscheidet der Rang.
		TestInteractable low = CreateTarget("low", new Vector3(-1f, 0f, 1.7320508f), Eidren.Interaction.InteractionMode.Hold, 1f, 1);
		TestInteractable high = CreateTarget("high", new Vector3(1f, 0f, 1.7320508f), Eidren.Interaction.InteractionMode.Hold, 1f, 5);
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { low, high }, 2, in _context, null, 0.1f, 0.3f, out var available, out var _), (IResolveConstraint)(object)Is.SameAs((object)high));
		Assert.That<bool>(available, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void CloseResourceBeatsDistantContainer()
	{
		// Der Glutruinen-Fehlschlag vom 15.08.2026: Eine bewachte Kiste (Rang 88)
		// stand in Reichweite neben dem Kupfervorkommen (Rang 10) und zog das Ziel
		// an sich, obwohl der Spieler direkt vor dem Erz stand. Das Erz war aus
		// dieser Richtung nicht mehr abbaubar.
		TestInteractable vein = CreateTarget("resource.copper_vein", new Vector3(0f, 0f, 1.5f), Eidren.Interaction.InteractionMode.Hold, 1f, 10);
		TestInteractable chest = CreateTarget("world_chest.guarded", new Vector3(0f, 0f, 2.6f), Eidren.Interaction.InteractionMode.Hold, 1f, 88);
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { vein, chest }, 2, in _context, null, 0.1f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)vein));
	}

	[Test]
	public void SelectionDoesNotDependOnCandidateOrder()
	{
		// Physics.OverlapSphereNonAlloc sichert keine Reihenfolge zu. Jede
		// Permutation derselben Kandidaten muss dasselbe Ziel liefern.
		TestInteractable vein = CreateTarget("a_resource", new Vector3(0f, 0f, 1.5f), Eidren.Interaction.InteractionMode.Hold, 1f, 10);
		TestInteractable chest = CreateTarget("b_chest", new Vector3(0f, 0f, 2.2f), Eidren.Interaction.InteractionMode.Hold, 1f, 88);
		TestInteractable bench = CreateTarget("c_bench", new Vector3(0.9f, 0f, 1.9f), Eidren.Interaction.InteractionMode.Hold, 1f, 80);
		IInteractable expected = InteractionTargetSelector.SelectBest(new IInteractable[3] { vein, chest, bench }, 3, in _context, null, 0.1f, 0.3f, out var _, out var _);
		IInteractable[][] permutations = new IInteractable[5][]
		{
			new IInteractable[3] { vein, bench, chest },
			new IInteractable[3] { chest, vein, bench },
			new IInteractable[3] { chest, bench, vein },
			new IInteractable[3] { bench, vein, chest },
			new IInteractable[3] { bench, chest, vein }
		};
		foreach (IInteractable[] permutation in permutations)
		{
			Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(permutation, 3, in _context, null, 0.1f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)expected), "Die Auswahl haengt an der Reihenfolge der Kandidaten.", Array.Empty<object>());
		}
	}

	[Test]
	public void EqualCandidatesFallBackToInstanceId()
	{
		// Deckungsgleiche Ziele mit gleichem Rang: Die Kennung entscheidet, damit
		// auch dieser Grenzfall reproduzierbar bleibt.
		TestInteractable second = CreateTarget("zzz", new Vector3(0f, 0f, 2f), Eidren.Interaction.InteractionMode.Hold, 1f, 20);
		TestInteractable first = CreateTarget("aaa", new Vector3(0f, 0f, 2f), Eidren.Interaction.InteractionMode.Hold, 1f, 20);
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { second, first }, 2, in _context, null, 0.1f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)first));
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { first, second }, 2, in _context, null, 0.1f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)first));
	}

	[Test]
	public void BestTargetChangesWhenAdvantageIsDecisive()
	{
		TestInteractable current = CreateTarget("current", new Vector3(1.4f, 0f, 2f));
		TestInteractable forward = CreateTarget("forward", new Vector3(0f, 0f, 1.2f));
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { current, forward }, 2, in _context, current, 0.1f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)forward));
	}

	[Test]
	public void HysteresisKeepsCurrentTargetForSmallAdvantage()
	{
		TestInteractable current = CreateTarget("current", new Vector3(0.12f, 0f, 2f));
		TestInteractable contender = CreateTarget("contender", new Vector3(0f, 0f, 1.85f));
		Assert.That<IInteractable>(InteractionTargetSelector.SelectBest(new IInteractable[2] { current, contender }, 2, in _context, current, 0.12f, 0.3f, out var _, out var _), (IResolveConstraint)(object)Is.SameAs((object)current));
	}

	[Test]
	public void ResourceNode_Uses225SecondsAndMinesOnce()
	{
		ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/CopperVein.asset");
		Assert.That<ResourceNodeDefinition>(definition, (IResolveConstraint)(object)Is.Not.Null, "Build Resource Collection V0.1 before running tests.", Array.Empty<object>());
		ContentDatabase database = new GameObject("InteractionTest_ContentDatabase").AddComponent<ContentDatabase>();
		ItemDefinition pickaxe = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/Pickaxe.asset");
		database.ConfigureItems(new ItemDefinition[2] { definition.OutputItem, pickaxe });
		PlayerInventory inventory = new PlayerInventory(database);
		inventory.Add("pickaxe", 1);
		_context = new InteractionContext(_actor, _actor.transform, Vector3.forward, inventory);
		GameObject gameObject = new GameObject("InteractionTest_Resource");
		SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
		ResourceNode resource = gameObject.AddComponent<ResourceNode>();
		resource.Configure(definition, "resource.test", null, null, trigger, null);
		int mined = 0;
		resource.Mined += delegate
		{
			mined++;
		};
		InteractionSession interactionSession = new InteractionSession();
		Assert.That<float>(resource.HoldDuration, (IResolveConstraint)(object)Is.EqualTo((object)2.25f));
		Assert.That<Eidren.Interaction.InteractionMode>(resource.Mode, (IResolveConstraint)(object)Is.EqualTo((object)Eidren.Interaction.InteractionMode.Timed));
		interactionSession.Begin(resource, in _context);
		interactionSession.Tick(2.24f, inputHeld: false, in _context);
		Assert.That<int>(mined, (IResolveConstraint)(object)Is.Zero);
		interactionSession.Tick(0.01f, inputHeld: false, in _context);
		interactionSession.Tick(10f, inputHeld: true, in _context);
		Assert.That<bool>(resource.IsMined, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(mined, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	private void AssertCancelledByTick(InteractionCancelReason expected, bool held, bool moveOutOfRange)
	{
		TestInteractable target = CreateTarget($"cancel.{expected}", Vector3.forward);
		InteractionSession interactionSession = new InteractionSession();
		interactionSession.Begin(target, in _context);
		interactionSession.Tick(0.2f, inputHeld: true, in _context);
		if (moveOutOfRange)
		{
			_actor.transform.position = Vector3.back * 20f;
		}
		interactionSession.Tick(0.2f, held, in _context);
		Assert.That<InteractionCancelReason>(interactionSession.LastCancelReason, (IResolveConstraint)(object)Is.EqualTo((object)expected));
		Assert.That<int>(target.CancelCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(target.CompleteCount, (IResolveConstraint)(object)Is.Zero);
	}

	private static TestInteractable CreateTarget(string id, Vector3 position, Eidren.Interaction.InteractionMode mode = Eidren.Interaction.InteractionMode.Hold, float duration = 1f, int priority = 0)
	{
		GameObject gameObject = new GameObject("InteractionTest_" + id);
		gameObject.transform.position = position;
		return new TestInteractable(gameObject, id, mode, duration, priority);
	}
}
}
