using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This is the base class for any component that modifies the terrain.</summary>
	[RequireComponent(typeof(SgtDynamicPlanet))]
	public abstract class SgtDynamicPlanetModifier : MonoBehaviour
	{
		[System.NonSerialized]
		protected SgtDynamicPlanet cachedPlanet;

		[System.NonSerialized]
		private bool cachedTerrainSet;

		public SgtDynamicPlanet CachedPlanet
		{
			get
			{
				if (cachedTerrainSet == false)
				{
					cachedPlanet    = GetComponent<SgtDynamicPlanet>();
					cachedTerrainSet = true;
				}

				return cachedPlanet;
			}
		}
#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			CachedPlanet.Rebuild(); // NOTE: Property
		}
#endif
		protected virtual void OnDestroy()
		{
			CachedPlanet.Rebuild(); // NOTE: Property
		}
	}
}