using UnityEngine;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component keeps the current <b>Transform</b> locked to the surface of the planet below it.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDynamicPlanetObject")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Dynamic Planet Object")]
	public class SgtDynamicPlanetObject : MonoBehaviour
	{
		public enum SnapType
		{
			Update,
			LateUpdate,
			FixedUpdate,
			Start
		}

		/// <summary>This allows you to specify which planet this object will snap to.
		/// None = Closest.</summary>
		public SgtDynamicPlanet Planet { set { planet = value; } get { return planet; } } [SerializeField] private SgtDynamicPlanet planet;

		/// <summary>This allows you to move the object up based on the surface normal in world space.</summary>
		public float Offset { set { offset = value; } get { return offset; } } [SerializeField] private float offset;

		/// <summary>The surface normal will be calculated using this sample radius in world space. Larger values = Smoother.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 0.1f;

		/// <summary>This allows you to control where in the game loop the object position will be snapped.</summary>
		public SnapType SnapIn { set { snapIn = value; } get { return snapIn; } } [SerializeField] private SnapType snapIn;

		[System.NonSerialized]
		private float3 delta;

		[System.NonSerialized]
		private bool deltaSet;

		protected virtual void Start()
		{
			if (snapIn == SnapType.Start)
			{
				SnapNow();
			}
		}

		protected virtual void Update()
		{
			if (snapIn == SnapType.Update)
			{
				SnapNow();
			}
		}

		protected virtual void LateUpdate()
		{
			if (snapIn == SnapType.LateUpdate)
			{
				SnapNow();
			}
		}

		protected virtual void FixedUpdate()
		{
			if (snapIn == SnapType.FixedUpdate)
			{
				SnapNow();
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (planet != null)
			{
				var worldPoint    = transform.position;
				var worldRight    = transform.right   * radius;
				var worldForward  = transform.forward * radius;
				var sampledPoint  = default(float3);
				var sampledNormal = default(float3);

				if (deltaSet == true)
				{
					worldPoint -= (Vector3)delta;
				}

				if (planet.TryGetWorldPoint(worldPoint, ref sampledPoint) == true &&
					planet.TryGetWorldNormal(worldPoint, worldRight, worldForward, ref sampledNormal) == true)
				{
					Gizmos.matrix = Matrix4x4.Rotate(Quaternion.LookRotation(worldForward, sampledNormal));
					Gizmos.DrawWireSphere(sampledPoint, radius);
				}
			}
		}
#endif

		

		/// <summary>This method updates the position and rotation of the current <b>Transform</b>.</summary>
		[ContextMenu("Snap Now")]
		private void SnapNow()
		{
			if (planet == null)
			{
				planet = SgtDynamicPlanet.GetClosest(transform.position);
			}

			if (planet != null)
			{
				var worldPoint    = transform.position;
				var worldRight    = transform.right   * radius;
				var worldForward  = transform.forward * radius;
				var sampledPoint  = default(float3);
				var sampledNormal = default(float3);

				if (deltaSet == true)
				{
					worldPoint -= (Vector3)delta;
				}

				if (planet.TryGetWorldPoint(worldPoint, ref sampledPoint) == true &&
					planet.TryGetWorldNormal(worldPoint, worldRight, worldForward, ref sampledNormal) == true)
				{
					delta    = sampledNormal * offset;
					deltaSet = true;

					transform.position = sampledPoint + delta;
					transform.rotation = Quaternion.FromToRotation(transform.up, sampledNormal) * transform.rotation;
				}
				else
				{
					deltaSet = false;
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CustomEditor(typeof(SgtDynamicPlanetObject))]
	public class SgtDynamicPlanetObject_Editor : SgtEditor<SgtDynamicPlanetObject>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Planet == null));
				Draw("planet", "This allows you to specify which planet this object will snap to.\n\nNone = Closest.");
			EndError();
			Draw("offset", "This allows you to move the object up based on the surface normal in world space.");
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", "The surface normal will be calculated using this sample radius in world space. Larger values = Smoother.");
			EndError();
			Draw("snapIn", "This allows you to control where in the game loop the object position will be snapped.");
		}
	}
}
#endif