using UnityEngine;
using System;
using System.Collections;


public class CableComponent : MonoBehaviour
{
	#region Class members

	[SerializeField] private Transform endPoint;
	[SerializeField] private Material cableMaterial;

	// Cable config
	[Tooltip("0이면 시작~끝 거리를 자동 계산")]
	[SerializeField] private float cableLength = 0f;
	[SerializeField] private int totalSegments = 12;
	[SerializeField] private float segmentsPerUnit = 2f;
	private int segments = 0;
	[SerializeField] private float cableWidth = 0.1f;

	// Solver config
	[SerializeField] private int verletIterations = 3;
	[SerializeField] private int solverIterations = 20;

	//[Range(0,3)]
	[SerializeField] private float stiffness = 1f;

	[Header("Collision")]
	[SerializeField] private LayerMask collisionMask = ~0; // 기본: 모든 레이어
	[SerializeField] private float collisionRadius = 0.02f;

	private LineRenderer line;
	private CableParticle[] points;
	private bool initialized = false;

	public CableParticle[] Points => points;
	public int Segments => segments;
	public Transform EndPoint => endPoint;
	public float CableLength => cableLength;

	#endregion


	#region Initial setup

	void Start()
	{
		if (endPoint == null)
		{
			Debug.LogError($"[CableComponent] '{gameObject.name}' — EndPoint is not set. Please assign End Point in Inspector.", this);
			return;
		}

		// cableLength가 0이면 실제 거리로 자동 계산
		if (cableLength <= 0f)
			cableLength = Vector3.Distance(transform.position, endPoint.position);

		InitCableParticles();
		InitLineRenderer();
		initialized = true;
	}

	void InitCableParticles()
	{
		if (totalSegments > 0)
			segments = totalSegments;
		else
			segments = Mathf.CeilToInt(cableLength * segmentsPerUnit);

		Vector3 cableDirection = (endPoint.position - transform.position).normalized;
		float initialSegmentLength = cableLength / segments;
		points = new CableParticle[segments + 1];

		for (int pointIdx = 0; pointIdx <= segments; pointIdx++)
		{
			Vector3 initialPosition = transform.position + (cableDirection * (initialSegmentLength * pointIdx));
			points[pointIdx] = new CableParticle(initialPosition);
		}

		CableParticle start = points[0];
		CableParticle end = points[segments];
		start.Bind(this.transform);
		end.Bind(endPoint.transform);
	}

	void InitLineRenderer()
	{
		line = this.gameObject.GetComponent<LineRenderer>();
		if (line == null) line = this.gameObject.AddComponent<LineRenderer>();
		line.startWidth = cableWidth;
		line.endWidth = cableWidth;
		line.positionCount = segments + 1;
		line.material = cableMaterial;
		line.GetComponent<Renderer>().enabled = true;
	}

	#endregion


	#region Render Pass

	void Update()
	{
		if (!initialized) return;
		RenderCable();
	}

	void RenderCable()
	{
		for (int pointIdx = 0; pointIdx < segments + 1; pointIdx++)
		{
			line.SetPosition(pointIdx, points[pointIdx].Position);
		}
	}

	#endregion


	#region Verlet integration & solver pass

	void FixedUpdate()
	{
		if (!initialized) return;

		// 1. Verlet 이동
		VerletIntegrate();

		// 2. 거리 제약 + 충돌 처리 반복 (인터리브)
		for (int i = 0; i < solverIterations; i++)
		{
			SolveDistanceConstraintPass();
			SolveCollisions();
		}
	}

	void VerletIntegrate()
	{
		Vector3 gravityDisplacement = Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;

		foreach (CableParticle particle in points)
		{
			if (!particle.IsFree()) continue;

			Vector3 prevPos = particle.Position;
			particle.UpdateVerlet(gravityDisplacement);

			// SphereCast — 이동 경로에 콜라이더 있으면 이전 위치로 복귀
			Vector3 dir = particle.Position - prevPos;
			float dist = dir.magnitude;
			if (dist > 0.0001f)
			{
				if (Physics.SphereCast(prevPos, collisionRadius, dir.normalized, out RaycastHit hit, dist, collisionMask))
				{
					if (!hit.collider.isTrigger && !(hit.collider is MeshCollider mc && !mc.convex))
						particle.Position = prevPos;
				}
			}
		}
	}

	void SolveDistanceConstraintPass()
	{
		float segmentLength = cableLength / segments;

		// 앞→뒤
		for (int i = 0; i < segments; i++)
			SolveDistanceConstraint(points[i], points[i + 1], segmentLength);

		// 뒤→앞
		for (int i = segments - 1; i >= 0; i--)
			SolveDistanceConstraint(points[i], points[i + 1], segmentLength);
	}


	void SolveCollisions()
	{
		foreach (CableParticle particle in points)
		{
			if (!particle.IsFree()) continue;

			Collider[] hits = Physics.OverlapSphere(particle.Position, collisionRadius, collisionMask);
			foreach (Collider hit in hits)
			{
				if (hit.isTrigger) continue;
				// 케이블 자신의 콜라이더 무시
				if (hit.transform == this.transform || hit.transform.IsChildOf(this.transform)) continue;
				if (endPoint != null && (hit.transform == endPoint || hit.transform.IsChildOf(endPoint))) continue;
				// Non-Convex MeshCollider 무시
				if (hit is MeshCollider mc && !mc.convex) continue;

				Vector3 closest = hit.ClosestPoint(particle.Position);
				Vector3 pushDir = particle.Position - closest;
				float dist = pushDir.magnitude;

				if (dist < 0.0001f)
				{
					pushDir = Vector3.up;
					dist = 0f;
				}

				float penetration = collisionRadius - dist;
				if (penetration > 0f)
					particle.Position += pushDir.normalized * penetration;
			}
		}
	}

	#endregion


	#region Solver Constraints

void SolveDistanceConstraint(CableParticle particleA, CableParticle particleB, float segmentLength)
	{
		Vector3 delta = particleB.Position - particleA.Position;
		float currentDistance = delta.magnitude;
		float errorFactor = (currentDistance - segmentLength) / currentDistance;

		if (particleA.IsFree() && particleB.IsFree())
		{
			particleA.Position += errorFactor * 0.5f * delta;
			particleB.Position -= errorFactor * 0.5f * delta;
		}
		else if (particleA.IsFree())
		{
			particleA.Position += errorFactor * delta;
		}
		else if (particleB.IsFree())
		{
			particleB.Position -= errorFactor * delta;
		}
	}

	#endregion
}
