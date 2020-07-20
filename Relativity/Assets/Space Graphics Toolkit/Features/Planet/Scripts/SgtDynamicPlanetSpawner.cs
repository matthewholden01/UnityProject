using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to spawn prefabs on the terrain at the specified triangle size.
	/// NOTE: The geometry at that level must generate before the prefab can spawn there.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDynamicPlanetSpawner")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Dynamic Planet Spawner")]
	public class SgtDynamicPlanetSpawner : SgtDynamicPlanetModifier
	{
		public enum ChannelType
		{
			Red,
			Green,
			Blue,
			Alpha
		}

		public enum RotateType
		{
			Randomly,
			ToSurfaceNormal,
			ToPlanetCenter
		}

		/// <summary>How big should each triangle be?
		/// NOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.</summary>
		public double TriangleSize { set { triangleSize = value; } get { return triangleSize; } } [SerializeField] private double triangleSize = 10.0;

		/// <summary>The splat texture used to control the amount of prefabs that get spawned.
		/// NOTE: This should use an Equirectangular projection.
		/// NOTE: This texture should be marked as readable.</summary>
		public Texture2D Splat { set { splat = value; } get { return splat; } } [SerializeField] private Texture2D splat;

		/// <summary>This allows you to choose which color channel from the splat texture that will be used.</summary>
		public ChannelType Channel { set { channel = value; } get { return channel; } } [SerializeField] private ChannelType channel;

		/// <summary>The prefab that will be spawned.</summary>
		public Transform Prefab { set { prefab = value; } get { return prefab; } } [SerializeField] private Transform prefab;

		/// <summary>How should the spawned prefab be rotated?</summary>
		public RotateType Rotate { set { rotate = value; } get { return rotate; } } [SerializeField] private RotateType rotate;

		/// <summary>The maximum amount of prefabs that can be spawned on the current chunk when the splat value is at maximum.</summary>
		public int Count { set { count = value; } get { return count; } } [Range(1, 100)] [SerializeField] private int count = 5;

		[System.NonSerialized]
		private Dictionary<SgtDynamicPlanetChunk, List<GameObject>> cloneLists = new Dictionary<SgtDynamicPlanetChunk, List<GameObject>>();

		protected virtual void OnEnable()
		{
			CachedPlanet.OnSplitChunk += HandleSplitChunk; // NOTE: Property
			cachedPlanet.OnMergeChunk += HandleMergeChunk;
			cachedPlanet.OnRebuild    += HandleRebuild;
		}

		protected virtual void OnDisable()
		{
			cachedPlanet.OnSplitChunk -= HandleSplitChunk;
			cachedPlanet.OnMergeChunk -= HandleMergeChunk;
			cachedPlanet.OnRebuild    -= HandleRebuild;

			HandleRebuild();
		}

		private void HandleSplitChunk(SgtDynamicPlanetChunk chunk)
		{
			if (chunk.Depth == cachedPlanet.GetDepth(triangleSize) && prefab != null)
			{
				var count = GetCount(chunk.Corner);

				if (count > 0)
				{
					var cloneList = new List<GameObject>();

					for (var i = 0; i < count; i++)
					{
						cloneList.Add(Spawn(chunk));
					}

					cloneLists.Add(chunk, cloneList);
				}
			}
		}

		private GameObject Spawn(SgtDynamicPlanetChunk chunk)
		{
			var clone        = Instantiate(prefab);
			var localPoint   = math.normalize(chunk.Cube + chunk.CubeH * UnityEngine.Random.value + chunk.CubeV * UnityEngine.Random.value);
			var sampledPoint = default(double3);

			cachedPlanet.TryGetLocalPoint(localPoint , ref sampledPoint);

			clone.transform.SetParent(null, false);
			clone.transform.position = cachedPlanet.TransformPoint(sampledPoint);

			switch (rotate)
			{
				case RotateType.Randomly:
				{
					clone.transform.rotation = UnityEngine.Random.rotation;
				}
				break;

				case RotateType.ToSurfaceNormal:
				{
					var localRight    = math.normalize(math.normalize(localPoint + chunk.CubeH) - localPoint) * chunk.CoordM * SgtDynamicPlanetChunk.Config.STEP;
					var localForward  = math.normalize(math.normalize(localPoint + chunk.CubeV) - localPoint) * chunk.CoordM * SgtDynamicPlanetChunk.Config.STEP;
					var sampledPointL = default(double3);
					var sampledPointR = default(double3);
					var sampledPointB = default(double3);
					var sampledPointF = default(double3);

					cachedPlanet.TryGetLocalPoint(localPoint - localRight  , ref sampledPointL);
					cachedPlanet.TryGetLocalPoint(localPoint + localRight  , ref sampledPointR);
					cachedPlanet.TryGetLocalPoint(localPoint - localForward, ref sampledPointB);
					cachedPlanet.TryGetLocalPoint(localPoint + localForward, ref sampledPointF);

					var vectorA = sampledPointR - sampledPointL;
					var vectorB = sampledPointF - sampledPointB;
					var normal  = math.normalize(-math.cross(vectorA, vectorB));
					var angle   = UnityEngine.Random.Range(-math.PI, math.PI);

					clone.transform.up = cachedPlanet.TransformVector(normal);
					clone.transform.Rotate(0.0f, angle, 0.0f, Space.Self);
				}
				break;

				case RotateType.ToPlanetCenter:
				{
					var normal  = math.normalize(sampledPoint);
					var angle   = UnityEngine.Random.Range(-math.PI, math.PI);

					clone.transform.up = cachedPlanet.TransformVector(normal);
					clone.transform.Rotate(0.0f, angle, 0.0f, Space.Self);
				}
				break;
			}

			return clone.gameObject;
		}

		private int GetCount(double3 point)
		{
			var weight = 1.0f;

			if (splat != null)
			{
				var uv    = SgtHelper.CartesianToPolarUV((float3)point);
				var pixel = splat.GetPixelBilinear(uv.x, uv.y);

				switch (channel)
				{
					case ChannelType.Red:   weight = pixel.r; break;
					case ChannelType.Green: weight = pixel.g; break;
					case ChannelType.Blue:  weight = pixel.b; break;
					case ChannelType.Alpha: weight = pixel.a; break;
				}
			}

			return Mathf.RoundToInt(count * weight);
		}

		private void HandleMergeChunk(SgtDynamicPlanetChunk chunk)
		{
			var cloneList = default(List<GameObject>);

			if (cloneLists.TryGetValue(chunk, out cloneList) == true)
			{
				cloneLists.Remove(chunk);

				foreach (var clone in cloneList)
				{
					Destroy(clone);
				}
			}
		}

		private void HandleRebuild()
		{
			foreach (var cloneList in cloneLists.Values)
			{
				foreach (var clone in cloneList)
				{
					Destroy(clone);
				}
			}

			cloneLists.Clear();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CustomEditor(typeof(SgtDynamicPlanetSpawner))]
	public class SgtDynamicPlanetSpawner_Editor : SgtEditor<SgtDynamicPlanetSpawner>
	{
		protected override void OnInspector()
		{
			Draw("triangleSize", "How big should each triangle be?\n\nNOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.");
			Draw("splat", "The splat texture used to control the amount of prefabs that get spawned.\n\nNOTE: This should use an Equirectangular projection.\n\nNOTE: This texture should be marked as readable.");
			Draw("channel", "This allows you to choose which color channel from the splat texture that will be used.");

			Separator();

			BeginError(Any(t => t.Prefab == null));
				Draw("prefab", "The prefab that will be spawned.");
			EndError();
			Draw("rotate", "How should the spawned prefab be rotated?");
			Draw("count", "The maximum amount of prefabs that can be spawned on the current chunk when the splat value is at maximum.");
		}
	}
}
#endif