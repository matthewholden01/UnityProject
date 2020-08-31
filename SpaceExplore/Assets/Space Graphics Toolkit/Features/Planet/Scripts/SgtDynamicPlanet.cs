using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to create a planet with dynamic LOD, where the surface mesh detail increases as your camera gets closer to the surface.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDynamicPlanet")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Dynamic Planet")]
	public partial class SgtDynamicPlanet : MonoBehaviour
	{
		public interface IWriteHeight
		{
			bool WriteHeight(ref JobHandle handle, NativeArray<double> heights, NativeArray<double3> vectors);
		}

		/// <summary>This stores all active and enabled <b>SgtTerrain</b> instances in the scene.</summary>
		public static LinkedList<SgtDynamicPlanet> Instances = new LinkedList<SgtDynamicPlanet>(); private LinkedListNode<SgtDynamicPlanet> node;

		/// <summary>The distance between the center and edge of the planet in local space.</summary>
		public double Radius { set { radius = value; dirty = true; } get { return radius; } } [SerializeField] private double radius = 1.0;

		/// <summary>When at the surface of the planet, how big should each triangle be?
		/// NOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.</summary>
		public double SmallestTriangleSize { set { smallestTriangleSize = value; } get { return smallestTriangleSize; } } [SerializeField] private double smallestTriangleSize = 1.0;

		/// <summary>This allows you to set the material applied to the terrain surface.</summary>
		public Material Material { set { material = value; } get { return material; } } [SerializeField] private Material material;

		/// <summary>If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.</summary>
		public SgtSharedMaterial SharedMaterial { set { sharedMaterial = value; } get { return sharedMaterial; } } [SerializeField] private SgtSharedMaterial sharedMaterial;

		/// <summary>The LOD will be based on the distance to this <b>Transform</b>.
		/// None = Main Camera.</summary>
		public Transform Observer { set { observer = value; } get { return observer; } } [SerializeField] private Transform observer;

		/// <summary>The higher this value, the more triangles will be in view.</summary>
		public double Detail { set { detail = value; } get { return detail; } } [Range(1.0f, 100.0f)] [SerializeField] private double detail = 1.0;

		/// <summary>The higher you set this, the higher the detail will appear from orbit.
		/// NOTE: The surface detail will remain the same, this only impacts distant LOD splitting.</summary>
		public double Boost { set { boost = value; } get { return boost; } } [Range(0.0f, 1.0f)] [SerializeField] private double boost = 0.0;

		/// <summary>This allows you to control the speed of the LOD generation. The higher you set this, the lower the performance.</summary>
		public int MaxSplitsPerFrame { set { maxSplitsPerFrame = value; } get { return maxSplitsPerFrame; } } [Range(1, 100)] [SerializeField] private int maxSplitsPerFrame = 10;

		/// <summary>The heightmap texture, where the height data is stored in the alpha channel.
		/// NOTE: This should use an Equirectangular projection.
		/// NOTE: This texture should be marked as readable.</summary>
		public Texture2D Heightmap { set { heightmap = value; dirty = true; } get { return heightmap; } } [SerializeField] private Texture2D heightmap;

		/// <summary>The maximum height displacement applied to the planet mesh when the heightmap alpha value is 1.</summary>
		public float Displacement { set { displacement = value; dirty = true; } get { return displacement; } } [SerializeField] private float displacement = 0.1f;

		/// <summary>The current water level.
		/// 0 = Radius.
		/// 1 = Radius + Displacement.</summary>
		public float WaterLevel { set { waterLevel = value; dirty = true; } get { return waterLevel; } } [Range(-2.0f, 2.0f)] [SerializeField] private float waterLevel;

		/// <summary>If you enable this then the water will not rise, instead the terrain will shrink down.</summary>
		public bool ClampWater { set { clampWater = value; dirty = true; } get { return clampWater; } } [SerializeField] private bool clampWater;

		/// <summary>Normals bend incorrectly on high detail planets, so it's a good idea to fade them out. This allows you to set the camera distance at which the normals begin to fade out in local space.</summary>
		public double NormalFadeRange { set { normalFadeRange = value; } get { return normalFadeRange; } } [SerializeField] private double normalFadeRange;

		public event System.Action OnRebuild;

		public event System.Action<SgtDynamicPlanetChunk> OnSplitChunk;

		public event System.Action<SgtDynamicPlanetChunk> OnMergeChunk;

		public event System.Action<SgtDynamicPlanetChunk> OnShowChunk;

		public event System.Action<SgtDynamicPlanetChunk> OnHideChunk;

		public event System.Action OnDraw;

		[System.NonSerialized]
		private double3 localPoint;

		[System.NonSerialized]
		private SgtDynamicPlanetChunk[] rootChunks = new SgtDynamicPlanetChunk[6];

		[System.NonSerialized]
		private double[] squareDistances = new double[32];

		[System.NonSerialized]
		private List<SgtDynamicPlanetChunk> testingQueue = new List<SgtDynamicPlanetChunk>();

		[System.NonSerialized]
		private List<SgtDynamicPlanetChunk> pendingQueue = new List<SgtDynamicPlanetChunk>();

		[System.NonSerialized]
		private List<SgtDynamicPlanetVisual> visuals = new List<SgtDynamicPlanetVisual>();

		[System.NonSerialized]
		private bool dirty;

		/// <summary>This method tells you the LOD level where triangles are closest to the specified triangle size.</summary>
		public int GetDepth(double triangleSize)
		{
			var size = SgtDynamicPlanetChunk.Config.QUADS * triangleSize;

			if (size > 0.0)
			{
				var depth = (int)math.log2((radius * 2) / size);

				return math.clamp(depth, 0, 32);
			}

			return 0;
		}

		private SgtDynamicPlanetVisual GetVisual(int depth)
		{
			var visual = default(SgtDynamicPlanetVisual);

			for (var i = visuals.Count - 1; i >= 0; i--)
			{
				visual = visuals[i];

				if (visual.Depth == depth && visual.Count < SgtDynamicPlanetVisual.Config.BATCH_SIZE)
				{
					return visual;
				}
			}

			visual = SgtDynamicPlanetVisual.Create(depth);

			visuals.Add(visual);

			return visual;
		}

		public void Rebuild()
		{
			dirty = false;

			if (node != null)
			{
				testingQueue.Clear();
				pendingQueue.Clear();

				for (var c = 0; c < 6; c++)
				{
					rootChunks[c].Dispose();
				}

				if (OnRebuild != null)
				{
					OnRebuild();
				}

				BuildRootChunks();
			}
		}

		protected virtual void OnEnable()
		{
			node = Instances.AddLast(this);

			BuildHeightmap();

			if (Instances.Count == 1)
			{
				AllocateJobData();
			}

			if (rootChunks[0] == null)
			{
				BuildRootChunks();
			}

			SgtHelper.OnCalculateDistance += HandleCalculateDistance;
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(node); node = null;

			ClearHeightmap();

			if (Instances.Count == 0)
			{
				DisposeJobData();
			}

			SgtHelper.OnCalculateDistance -= HandleCalculateDistance;
		}

		protected virtual void OnDestroy()
		{
			for (var c = 0; c < 6; c++)
			{
				rootChunks[c].Dispose();
			}

			if (Instances.Count == 0)
			{
				foreach (var chunk in SgtDynamicPlanetChunk.Pool)
				{
					chunk.Dispose();
				}

				SgtDynamicPlanetChunk.Pool.Clear();
			}
		}

		protected virtual void Update()
		{
			if (dirty == true)
			{
				Rebuild();
			}

			UpdateLocalPoint();
			UpdateLocalDistances();

			if (Application.isPlaying == true)
			{
				UpdateLod();
			}
		}

		protected virtual void LateUpdate()
		{
			var material2 = sharedMaterial != null ? sharedMaterial.Material : null;
			var height    = default(double);
			var bumpScale = default(float);

			if (material != null)
			{
				bumpScale = material.GetFloat(SgtShader._BumpScale);

				if (normalFadeRange > 0.0f && TryGetLocalHeight(localPoint, ref height) == true)
				{
					bumpScale *= (float)math.saturate((math.length(localPoint) - height) / normalFadeRange);
				}
			}

			for (var i = visuals.Count - 1; i >= 0; i--)
			{
				var visual = visuals[i];

				if (visual.Count > 0)
				{
					visual.Draw(transform.localToWorldMatrix, gameObject.layer, localPoint, material, material2, waterLevel, bumpScale);
				}
				else
				{
					visuals.RemoveAt(i);

					visual.Pool();
				}
			}

			if (OnDraw != null)
			{
				OnDraw.Invoke();
			}
		}

		public static SgtDynamicPlanet GetClosest(Vector3 worldPoint)
		{
			var bestPlanet     = default(SgtDynamicPlanet);
			var bestDistancesq = float.PositiveInfinity;

			foreach (var instance in Instances)
			{
				var sampledPoint = default(float3);

				if (instance.TryGetWorldPoint(worldPoint, ref sampledPoint) == true)
				{
					var distancesq = math.distancesq(worldPoint, sampledPoint);

					if (distancesq < bestDistancesq)
					{
						bestPlanet     = instance;
						bestDistancesq = distancesq;
					}
				}
			}

			return bestPlanet;
		}

		private void HandleCalculateDistance(Vector3 worldPosition, ref float distance)
		{
			var sampledPoint = default(float3);
			
			if (TryGetWorldPoint(worldPosition, ref sampledPoint) == true)
			{
				var newDistance = math.distance(worldPosition, sampledPoint);

				if (newDistance < distance)
				{
					distance = newDistance;
				}
			}
		}

		private void ClearHeightmap()
		{
			if (heightmapData.IsCreated == true)
			{
				heightmapData.Dispose();
			}
		}

		private void BuildHeightmap()
		{
#if UNITY_EDITOR
			SgtHelper.MakeTextureReadable(heightmap);
#endif
			ClearHeightmap();

			if (heightmap != null)
			{
				var pixels = heightmap.GetPixels();
				var total  = pixels.Length;

				heightmapData = new NativeArray<float>(total, Allocator.Persistent);
				heightmapSize = new int2(heightmap.width, heightmap.height);

				for (var i = 0; i < total; i++)
				{
					heightmapData[i] = pixels[i].a;
				}

				var t = (heightmapSize.y - 1) * heightmapSize.x;

				for (var x = 1; x < heightmapSize.x; x++)
				{
					heightmapData[x    ] = heightmapData[0];
					heightmapData[x + t] = heightmapData[t];
				}
			}
		}

		private void BuildRootChunks()
		{
			rootChunks[0] = BuildRootChunk(new float3( 1.0f, -1.0f, -1.0f), new float3( 0.0f,  0.0f,  2.0f), new float3( 0.0f,  2.0f,  0.0f)); // +X
			rootChunks[1] = BuildRootChunk(new float3( 1.0f,  1.0f, -1.0f), new float3( 0.0f,  0.0f,  2.0f), new float3(-2.0f,  0.0f,  0.0f)); // +Y
			rootChunks[2] = BuildRootChunk(new float3( 1.0f, -1.0f,  1.0f), new float3(-2.0f,  0.0f,  0.0f), new float3( 0.0f,  2.0f,  0.0f)); // +Z

			rootChunks[3] = BuildRootChunk(new float3(-1.0f, -1.0f,  1.0f), new float3( 0.0f,  0.0f, -2.0f), new float3( 0.0f,  2.0f,  0.0f)); // -X
			rootChunks[4] = BuildRootChunk(new float3(-1.0f, -1.0f, -1.0f), new float3( 0.0f,  0.0f,  2.0f), new float3( 2.0f,  0.0f,  0.0f)); // -Y
			rootChunks[5] = BuildRootChunk(new float3(-1.0f, -1.0f, -1.0f), new float3( 2.0f,  0.0f,  0.0f), new float3( 0.0f,  2.0f,  0.0f)); // -Z

			for (var c = 0; c < 6; c++)
			{
				var chunk = rootChunks[c];

				Generate(chunk);

				Show(chunk);
			}
		}

		private SgtDynamicPlanetChunk BuildRootChunk(float3 cube, float3 cubeH, float3 cubeV)
		{
			var rotation = quaternion.Euler(math.atan(1.0f / math.sqrt(2.0f)), 0.0f, 0.785398f);

			cube  = math.mul(rotation, cube );
			cubeH = math.mul(rotation, cubeH);
			cubeV = math.mul(rotation, cubeV);

			return SgtDynamicPlanetChunk.Create(null, cube, cubeH, cubeV, new double2(0.0, 0.0), 1.0);
		}

		private void Show(SgtDynamicPlanetChunk chunk)
		{
			if (chunk != null && chunk.Visual == null)
			{
				var visual = GetVisual(chunk.Depth);

				visual.Add(chunk);

				if (OnShowChunk != null)
				{
					OnShowChunk.Invoke(chunk);
				}
			}
		}

		private void Hide(SgtDynamicPlanetChunk chunk)
		{
			if (chunk != null && chunk.Visual != null)
			{
				chunk.Visual.Remove(chunk);

				if (OnHideChunk != null)
				{
					OnHideChunk.Invoke(chunk);
				}
			}
		}

		private void UpdateLod()
		{
			if (testingQueue.Count == 0)
			{
				if (pendingQueue.Count == 0)
				{
					testingQueue.AddRange(rootChunks);
				}
				else
				{
					testingQueue.AddRange(pendingQueue); pendingQueue.Clear();
				}
			}

			var count  = 0;
			var splits = 0;

			while (testingQueue.Count > 0 && count++ < 200 && splits < maxSplitsPerFrame)
			{
				var index = testingQueue.Count - 1;
				var chunk = testingQueue[index];

				testingQueue.RemoveAt(index);

				var squareDistance = math.lengthsq(localPoint - chunk.Corner);

				if (chunk.Split == true)
				{
					if (chunk.Children[0].Split == false && chunk.Children[1].Split == false && chunk.Children[2].Split == false && chunk.Children[3].Split == false)
					{
						if (squareDistance >= squareDistances[chunk.Depth])
						{
							HandleMerge(chunk);

							continue;
						}
					}

					testingQueue.AddRange(chunk.Children);
				}
				else if (squareDistance < squareDistances[chunk.Depth])
				{
					HandleSplit(chunk);

					splits += 1;
				}
			}
		}

		private void HandleSplit(SgtDynamicPlanetChunk chunk)
		{
			var cubeH  = chunk.CubeH * 0.5;
			var cubeV  = chunk.CubeV * 0.5;
			var coordM = chunk.CoordM * 0.5;
			var coordH = new double2(coordM, 0.0);
			var coordV = new double2(0.0, coordM);

			chunk.Split       = true;
			chunk.Children[0] = SgtDynamicPlanetChunk.Create(chunk, chunk.Cube                , cubeH, cubeV, chunk.Coord                  , coordM);
			chunk.Children[1] = SgtDynamicPlanetChunk.Create(chunk, chunk.Cube + cubeH        , cubeH, cubeV, chunk.Coord + coordH         , coordM);
			chunk.Children[2] = SgtDynamicPlanetChunk.Create(chunk, chunk.Cube + cubeV        , cubeH, cubeV, chunk.Coord + coordV         , coordM);
			chunk.Children[3] = SgtDynamicPlanetChunk.Create(chunk, chunk.Cube + cubeH + cubeV, cubeH, cubeV, chunk.Coord + coordH + coordV, coordM);

			Hide(chunk);

			for (var c = 0; c < 4; c++)
			{
				var child = chunk.Children[c];

				Generate(child);

				Show(child);
			}

			if (OnSplitChunk != null)
			{
				OnSplitChunk.Invoke(chunk);
			}
		}

		private void HandleMerge(SgtDynamicPlanetChunk chunk)
		{
			for (var c = 0; c < 4; c++)
			{
				Hide(chunk.Children[c]);

				SgtDynamicPlanetChunk.Pool.Push(chunk.Children[c]);
				
				chunk.Children[c] = null;
			}

			chunk.Split = false;

			Show(chunk);

			if (OnMergeChunk != null)
			{
				OnMergeChunk.Invoke(chunk);
			}
		}

		private void UpdateLocalPoint()
		{
			var finalObserver = observer;

			if (finalObserver == null && Camera.main != null)
			{
				finalObserver = Camera.main.transform;
			}

			if (finalObserver != null)
			{
				localPoint = new double3(transform.InverseTransformPoint(finalObserver.position));
			}
		}

		private void UpdateLocalDistances()
		{
			var distance = radius * detail;
			var maxDepth = GetDepth(smallestTriangleSize);

			for (var i = 0; i < 32; i++)
			{
				if (i < maxDepth)
				{
					squareDistances[i] = math.pow(distance, 2.0 + boost * math.pow(1.0 - i / 31.0, 3.0));
				}
				else
				{
					squareDistances[i] = float.NegativeInfinity;
				}

				distance *= 0.5;
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CustomEditor(typeof(SgtDynamicPlanet))]
	public class SgtTerrain_Editor : SgtEditor<SgtDynamicPlanet>
	{
		protected override void OnInspector()
		{
			var rebuild = false;

			BeginError(Any(t => t.Radius <= 0.0));
				DrawDefault("radius", ref rebuild, "The distance between the center and edge of the planet in local space.");
			EndError();
			BeginError(Any(t => t.SmallestTriangleSize <= 0));
				Draw("smallestTriangleSize", "When at the surface of the planet, how big should each triangle be?\n\nNOTE: This is an approximation. The final size of the triangle will depend on your planet radius, and will be a power of two.");
			EndError();

			Separator();

			BeginError(Any(t => t.Material == null));
				Draw("material", "This allows you to set the material applied to the terrain surface.");
			EndError();
			Draw("sharedMaterial", "If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.");

			Separator();

			Draw("observer", "The LOD will be based on the distance to this Transform.\n\nNone = Main Camera.");
			Draw("detail", "The higher this value, the more triangles will be in view.");
			Draw("boost", "The higher you set this, the higher the detail will appear from orbit.\n\nNOTE: The surface detail will remain the same, this only impacts distant LOD splitting.");
			Draw("maxSplitsPerFrame", "This allows you to control the speed of the LOD generation. The higher you set this, the lower the performance.");

			Separator();

			DrawDefault("heightmap", ref rebuild, "The heightmap texture, where the height data is stored in the alpha channel.\n\nNOTE: This should use an Equirectangular projection.\n\nNOTE: This texture should be marked as readable.");
			BeginError(Any(t => t.Displacement <= 0.0f));
				DrawDefault("displacement", ref rebuild, "The maximum height displacement applied to the planet mesh when the heightmap alpha value is 1.");
			EndError();
			DrawDefault("waterLevel", ref rebuild, "The current water level.\n\n0 = Radius.\n\n1 = Radius + Displacement.");
			DrawDefault("clampWater", ref rebuild, "If you enable this then the water will not rise, instead the terrain will shrink down.");

			Separator();

			Draw("normalFadeRange", "Normals bend incorrectly on high detail planets, so it's a good idea to fade them out. This allows you to set the camera distance at which the normals begin to fade out in local space.");

			if (rebuild == true)
			{
				Each(t => t.Rebuild());
			}
		}
	}
}
#endif