using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to spawn a prefab as a child of the current GameObject when the floating camera gets within the specified range.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingLod")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating LOD")]
	[RequireComponent(typeof(SgtFloatingObject))]
	public class SgtFloatingLod : MonoBehaviour
	{
		/// <summary>The prefab that will be spawned.</summary>
		public GameObject Prefab;

		/// <summary>If the camera is closer than this distance, then the LOD will disappear.</summary>
		public SgtLength DistanceMin;

		/// <summary>If the camera is farther than this distance, then the LOD will disappear.</summary>
		public SgtLength DistanceMax = new SgtLength(1.0, SgtLength.ScaleType.Kilometer);

		[SerializeField]
		private bool spawned;

		[SerializeField]
		private GameObject clone;

		[System.NonSerialized]
		private SgtFloatingObject cachedFloatingObject;

		/// <summary>This will be set while the LOD is within range.</summary>
		public bool Spawned
		{
			get
			{
				return spawned;
			}
		}

		/// <summary>This allows you to get the spawned prefab clone.</summary>
		public GameObject Clone
		{
			get
			{
				return clone;
			}
		}

		protected virtual void OnEnable()
		{
			cachedFloatingObject = GetComponent<SgtFloatingObject>();

			cachedFloatingObject.OnDistance += HandleDistance;
		}

		protected virtual void OnDisable()
		{
			cachedFloatingObject.OnDistance -= HandleDistance;
		}

		private void HandleDistance(double distance)
		{
			if (distance >= DistanceMin && distance <= DistanceMax)
			{
				if (spawned == false)
				{
					spawned = true;

					if (Prefab != null)
					{
						clone = Instantiate(Prefab, transform, false);
					}
				}
			}
			else if (spawned == true)
			{
				spawned = false;
				clone   = SgtHelper.Destroy(clone);
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingLod))]
	public class SgtFloatingLod_Editor : SgtEditor<SgtFloatingLod>
	{
		protected override void OnInspector()
		{
			Draw("Prefab", "The prefab that will be spawned.");
			Draw("DistanceMin", "If the camera is closer than this distance, then the LOD will disappear.");
			Draw("DistanceMax", "If the camera is farther than this distance, then the LOD will disappear.");
		}
	}
}
#endif