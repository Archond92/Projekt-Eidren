using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Player;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class MapExitRuleTests
{
	[Test]
	public void NorthExit_UsesPositiveWorldZ()
	{
		AssertDirection(Vector3.forward, MapSideFlags.North, MapExitDirection.North);
	}

	[Test]
	public void EastExit_UsesPositiveWorldX()
	{
		AssertDirection(Vector3.right, MapSideFlags.East, MapExitDirection.East);
	}

	[Test]
	public void SouthExit_UsesNegativeWorldZ()
	{
		AssertDirection(Vector3.back, MapSideFlags.South, MapExitDirection.South);
	}

	[Test]
	public void WestExit_UsesNegativeWorldX()
	{
		AssertDirection(Vector3.left, MapSideFlags.West, MapExitDirection.West);
	}

	[Test]
	public void NorthEastCorner_HorizontalDominanceChoosesEast()
	{
		AssertDirection(new Vector3(0.9f, 0f, 0.4f), MapSideFlags.North | MapSideFlags.East, MapExitDirection.East);
	}

	[Test]
	public void SouthEastCorner_VerticalDominanceChoosesSouth()
	{
		AssertDirection(new Vector3(0.4f, 0f, -0.9f), MapSideFlags.East | MapSideFlags.South, MapExitDirection.South);
	}

	[Test]
	public void SouthWestCorner_HorizontalDominanceChoosesWest()
	{
		AssertDirection(new Vector3(-0.9f, 0f, -0.4f), MapSideFlags.South | MapSideFlags.West, MapExitDirection.West);
	}

	[Test]
	public void NorthWestCorner_VerticalDominanceChoosesNorth()
	{
		AssertDirection(new Vector3(-0.4f, 0f, 0.9f), MapSideFlags.North | MapSideFlags.West, MapExitDirection.North);
	}

	[Test]
	public void NearZeroMovement_UsesLastValidWorldDirection()
	{
		Assert.That<MapExitDirection>(MapExitDirectionResolver.Resolve(Vector3.zero, Vector3.left, MapSideFlags.North | MapSideFlags.West), (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.West));
	}

	[Test]
	public void ReturningFullyInside_CancelsCountdown()
	{
		MapExitCountdown mapExitCountdown = NewCountdown();
		Assert.That<bool>(mapExitCountdown.Tick(0.2f, fullyInsideSafeArea: true, participantAlive: true), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(mapExitCountdown.IsRunning, (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void Reentering_StartsCleanCountdown()
	{
		MapExitCountdown mapExitCountdown = NewCountdown();
		mapExitCountdown.Tick(0.2f, fullyInsideSafeArea: true, participantAlive: true);
		Assert.That<bool>(mapExitCountdown.Begin("home.exit.north", MapExitDirection.North, "WorldMap", 0.4f), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(mapExitCountdown.Remaining, (IResolveConstraint)(object)Is.EqualTo((object)0.4f));
		Assert.That<float>(mapExitCountdown.Progress, (IResolveConstraint)(object)Is.EqualTo((object)0f));
	}

	[Test]
	public void PlayerDeath_CancelsWithoutConfirmation()
	{
		MapExitCountdown mapExitCountdown = NewCountdown();
		Assert.That<bool>(mapExitCountdown.Tick(1f, fullyInsideSafeArea: false, participantAlive: false), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(mapExitCountdown.IsRunning, (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void ConfirmedExit_IsRecordedInGameSession()
	{
		GameObject gameObject = new GameObject("Session_Test");
		GameSession gameSession = gameObject.AddComponent<GameSession>();
		gameSession.RecordConfirmedExit("zone_greenwood", MapExitDirection.South, "WorldMap");
		Assert.That<string>(gameSession.PreviousZoneId, (IResolveConstraint)(object)Is.EqualTo((object)"zone_greenwood"));
		Assert.That<MapExitDirection>(gameSession.LastExitDirection, (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.South));
		Assert.That<string>(gameSession.RequestedTargetScene, (IResolveConstraint)(object)Is.EqualTo((object)"WorldMap"));
		Assert.That<AreaStatus>(gameSession.LastValidAreaStatus, (IResolveConstraint)(object)Is.EqualTo((object)AreaStatus.TransitionConfirmed));
		UnityEngine.Object.DestroyImmediate(gameObject);
	}

	[Test]
	public void RectangleBoundary_LeavesConfiguredExitSideOpen()
	{
		PlayerMovementBoundary boundary = PlayerMovementBoundary.Rectangle(Vector3.zero, new Vector2(20f, 20f), MapSideFlags.East);
		Assert.That<float>(boundary.Constrain(new Vector3(14f, 0f, 0f)).x, (IResolveConstraint)(object)Is.EqualTo((object)14f));
		Assert.That<float>(boundary.Constrain(new Vector3(0f, 0f, 14f)).z, (IResolveConstraint)(object)Is.EqualTo((object)10f));
	}

	[Test]
	public void PrototypeCircleConfiguration_RemainsClosed()
	{
		Vector3 vector = PlayerMovementBoundary.Circle(Vector3.zero, 10f).Constrain(new Vector3(20f, 0f, 0f));
		Assert.That<float>(vector.x, (IResolveConstraint)(object)Is.EqualTo((object)10f));
		Assert.That<float>(vector.z, (IResolveConstraint)(object)Is.EqualTo((object)0f));
	}

	[Test]
	public void SingleLongFrame_DoesNotConfirmExit()
	{
		MapExitCountdown mapExitCountdown = NewCountdown();
		Assert.That<bool>(mapExitCountdown.Tick(2f, fullyInsideSafeArea: false, participantAlive: true), (IResolveConstraint)(object)Is.False, "Ein einzelner langer Frame darf den Szenenwechsel nicht ausloesen.", Array.Empty<object>());
		Assert.That<bool>(mapExitCountdown.IsRunning, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void LongFrameThenReturn_CancelsInsteadOfConfirming()
	{
		MapExitCountdown mapExitCountdown = NewCountdown();
		mapExitCountdown.Tick(2f, fullyInsideSafeArea: false, participantAlive: true);
		Assert.That<bool>(mapExitCountdown.Tick(0.016f, fullyInsideSafeArea: true, participantAlive: true), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(mapExitCountdown.IsRunning, (IResolveConstraint)(object)Is.False, "Rueckkehr in die Sicherheitszone muss auch nach einem langen Frame noch abbrechen koennen.", Array.Empty<object>());
	}

	[Test]
	public void ArbitrarilyLongFrames_StillRequireSeveralTicks()
	{
		MapExitCountdown countdown = NewCountdown();
		int ticks = 0;
		while (!countdown.Tick(5f, fullyInsideSafeArea: false, participantAlive: true))
		{
			ticks++;
			Assert.That<int>(ticks, (IResolveConstraint)(object)Is.LessThan((object)50), "Countdown loest nie aus.", Array.Empty<object>());
		}
		ticks++;
		Assert.That<int>(ticks, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)4), "Die Delta-Klemmung muss den Wechsel ueber mehrere Frames strecken, egal wie lang ein einzelner Frame ist.", Array.Empty<object>());
	}

	[Test]
	public void NormalFrames_StillConfirmAfterDuration()
	{
		MapExitCountdown countdown = NewCountdown();
		bool confirmed = false;
		for (int i = 0; i < 120; i++)
		{
			if (confirmed)
			{
				break;
			}
			confirmed = countdown.Tick(0.016f, fullyInsideSafeArea: false, participantAlive: true);
		}
		Assert.That<bool>(confirmed, (IResolveConstraint)(object)Is.True, "Bei normalen Frames muss der Wechsel weiterhin ausloesen.", Array.Empty<object>());
		Assert.That<bool>(countdown.IsRunning, (IResolveConstraint)(object)Is.False);
	}

	private static MapExitCountdown NewCountdown()
	{
		MapExitCountdown mapExitCountdown = new MapExitCountdown();
		Assert.That<bool>(mapExitCountdown.Begin("home.exit.north", MapExitDirection.North, "WorldMap", 0.4f), (IResolveConstraint)(object)Is.True);
		return mapExitCountdown;
	}

	private static void AssertDirection(Vector3 movement, MapSideFlags available, MapExitDirection expected)
	{
		Assert.That<MapExitDirection>(MapExitDirectionResolver.Resolve(movement, Vector3.forward, available), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}
}
}
