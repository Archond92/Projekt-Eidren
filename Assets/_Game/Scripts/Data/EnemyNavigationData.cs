using System;

namespace Eidren.Data
{
	[Serializable]
	public struct EnemyNavigationData
	{
		public float MoveSpeed;

		public float Acceleration;

		public float AngularSpeed;

		public float AttackRange;

		public float DetectionRange;

		public float LeashRange;

		public float LeashFollowDistance;

		public float PatrolRadius;

		public float PatrolWait;

		public float AlertDuration;

		public float ReturnTolerance;

		public float NavMeshSampleDistance;

		public float AgentRadius;

		public float AgentHeight;
	}
}
