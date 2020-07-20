using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to add colliders to a terrain at the specified triangle size.
	/// NOTE: The geometry at that level must generate before the collider can appear there.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDynamicPlanetCollider")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Dynamic Planet Collider")]
	public class SgtDynamicPlanetCollider : SgtDynamicPlanetModifier
	{
		/// <summary>How big should each triangle be?
		/// NOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.</summary>
		public double TriangleSize { set { triangleSize = value; } get { return triangleSize; } } [SerializeField] private double triangleSize = 10.0;

		/// <summary>The GameObject layer the colliders will use.</summary>
		public int Layer { set { layer = value; } get { return layer; } } [SerializeField] private int layer;

		/// <summary>The physics material the colliders will use.</summary>
		public PhysicMaterial Material { set { material = value; } get { return material; } } [SerializeField] private PhysicMaterial material;

		[System.NonSerialized]
		private Dictionary<SgtDynamicPlanetChunk, GameObject> clones = new Dictionary<SgtDynamicPlanetChunk, GameObject>();

		protected virtual void OnEnable()
		{
			CachedPlanet.OnSplitChunk += HandleSplitChunk; // NOTE: Property
			cachedPlanet.OnMergeChunk += HandleMergeChunk;
			CachedPlanet.OnRebuild    += HandleRebuild;
		}

		protected virtual void OnDisable()
		{
			cachedPlanet.OnSplitChunk -= HandleSplitChunk;
			cachedPlanet.OnMergeChunk -= HandleMergeChunk;
			CachedPlanet.OnRebuild    -= HandleRebuild;

			HandleRebuild();
		}

		private void HandleSplitChunk(SgtDynamicPlanetChunk chunk)
		{
			if (chunk.Depth == cachedPlanet.GetDepth(triangleSize))
			{
				var clone    = new GameObject("Collider");
				var collider = clone.AddComponent<MeshCollider>();

				clone.layer = layer;

				collider.sharedMaterial = material;
				collider.sharedMesh     = chunk.Mesh;

				clone.transform.SetParent(transform, false);
				clone.transform.localPosition = (float3)chunk.Corner;

				clones.Add(chunk, clone);
			}
		}

		private void HandleMergeChunk(SgtDynamicPlanetChunk chunk)
		{
			var clone = default(GameObject);

			if (clones.TryGetValue(chunk, out clone) == true)
			{
				clones.Remove(chunk);

				Destroy(clone);
			}
		}

		private void HandleRebuild()
		{
			foreach (var clone in clones)
			{
				Destroy(clone.Value);
			}

			clones.Clear();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CustomEditor(typeof(SgtDynamicPlanetCollider))]
	public class SgtDynamicPlanetCollider_Editor : SgtEditor<SgtDynamicPlanetCollider>
	{
		protected override void OnInspector()
		{
			Draw("triangleSize", "How big should each triangle be?\n\nNOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.");
			DrawLayer("layer", "The GameObject layer the colliders will use.");
			Draw("material", "The physics material the colliders will use.");
		}
	}
}
#endif