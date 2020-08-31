using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace SpaceGraphicsToolkit
{
	/// <summary>This class combines many <b>SgtTerrainChunk</b> instances, and can draw them together in a single batch.</summary>
	public class SgtDynamicPlanetVisual
	{
		public static class Config
		{
			/// <summary>The amount of chunks that can be batched into one visual.
			/// NOTE: This should be at least 4.</summary>
			public static readonly int BATCH_SIZE = 50;

			/// <summary>The index data used by each mesh.</summary>
			public static readonly int[] INDICES;

			static Config()
			{
				INDICES = new int[SgtDynamicPlanetChunk.Config.QUADS * SgtDynamicPlanetChunk.Config.QUADS * 6 + SgtDynamicPlanetChunk.Config.QUADS * 4 * 6];

				var index = 0;

				for (var y = 0; y < SgtDynamicPlanetChunk.Config.QUADS; y++)
				{
					for (var x = 0; x < SgtDynamicPlanetChunk.Config.QUADS; x++)
					{
						var v = (x + y * SgtDynamicPlanetChunk.Config.VERTS);

						INDICES[index++] = v + 0;
						INDICES[index++] = v + SgtDynamicPlanetChunk.Config.VERTS;
						INDICES[index++] = v + 1;

						INDICES[index++] = v + SgtDynamicPlanetChunk.Config.VERTS + 1;
						INDICES[index++] = v + 1;
						INDICES[index++] = v + SgtDynamicPlanetChunk.Config.VERTS;
					}
				}

				var outer = 0;

				WriteEdge(ref outer, ref index, 0, 1);
				WriteEdge(ref outer, ref index, SgtDynamicPlanetChunk.Config.QUADS, SgtDynamicPlanetChunk.Config.VERTS);
				WriteEdge(ref outer, ref index, SgtDynamicPlanetChunk.Config.VERTS * SgtDynamicPlanetChunk.Config.VERTS - 1, -1);
				WriteEdge(ref outer, ref index, SgtDynamicPlanetChunk.Config.VERTS * (SgtDynamicPlanetChunk.Config.VERTS - 1), -SgtDynamicPlanetChunk.Config.VERTS);
			}

			static void WriteEdge(ref int outer, ref int index, int innerV, int innerS)
			{
				var outerV = SgtDynamicPlanetChunk.Config.VERTS * SgtDynamicPlanetChunk.Config.VERTS;
				var outerT = SgtDynamicPlanetChunk.Config.QUADS * 4;

				for (var i = 0; i < SgtDynamicPlanetChunk.Config.QUADS; i++)
				{
					var outerA = outerV + outer;
					var outerB = outerV + (outer + 1) % outerT;

					INDICES[index++] = outerA;
					INDICES[index++] = innerV;
					INDICES[index++] = outerB;

					INDICES[index++] = innerV + innerS;
					INDICES[index++] = outerB;
					INDICES[index++] = innerV;

					innerV += innerS;
					outer  += 1;
				}
			}
		}

		private bool dirty;

		private List<SgtDynamicPlanetChunk> chunks = new List<SgtDynamicPlanetChunk>();

		private Mesh mesh;

		private int depth;

		private List<CombineInstance> combines = new List<CombineInstance>();

		private int state;

		private double3 origin;

		private Matrix4x4 transform;

		private MaterialPropertyBlock properties = new MaterialPropertyBlock();

		private static Matrix4x4 translationMatrix = Matrix4x4.identity;

		private static Stack<SgtDynamicPlanetVisual> pool = new Stack<SgtDynamicPlanetVisual>();

		private static CombineInstance[][] combineArrays = new CombineInstance[SgtDynamicPlanetVisual.Config.BATCH_SIZE + 1][];

		public int Depth
		{
			get
			{
				return depth;
			}
		}

		public int Count
		{
			get
			{
				return chunks.Count;
			}
		}

		public static SgtDynamicPlanetVisual Create(int depth)
		{
			var visual = pool.Count > 0 ? pool.Pop() : new SgtDynamicPlanetVisual();

			visual.depth = depth;

			return visual;
		}

		static SgtDynamicPlanetVisual()
		{
			for (var i = 0; i <= SgtDynamicPlanetVisual.Config.BATCH_SIZE; i++)
			{
				combineArrays[i] = new CombineInstance[i];
			}
		}

		public void Pool()
		{
			properties.Clear();

			pool.Push(this);
		}

		public void Dispose()
		{
			Object.DestroyImmediate(mesh);
		}

		public void Draw(Matrix4x4 matrix, int layer, double3 localPoint, Material material, Material material2, float waterLevel, float bumpScale)
		{
			if (dirty == true)
			{
				Build();
			}

			properties.SetFloat(SgtShader._WaterLevel, waterLevel);
			properties.SetVector(SgtShader._Offset, (Vector3)(float3)origin);
			properties.SetFloat(SgtShader._BumpScale, bumpScale);

			if (mesh != null)
			{
				if (material != null)
				{
					Graphics.DrawMesh(mesh, matrix * transform, material, layer, null, 0, properties);
				}

				if (material2 != null)
				{
					Graphics.DrawMesh(mesh, matrix * transform, material2, layer, null, 0, properties);
				}
			}
		}

		private void Rebase()
		{
			state     = SgtDynamicPlanetVisual.Config.BATCH_SIZE * 2;
			origin    = GetOrigin();
			transform = Matrix4x4.Translate((float3)origin);

			for (var i = chunks.Count - 1; i >= 0; i--)
			{
				Rebase(chunks[i], i);
			}
		}

		private void Rebase(SgtDynamicPlanetChunk chunk, int index)
		{
			var combine = combines[index];

			combine.transform = Matrix4x4.Translate((float3)(chunk.Corner - origin));

			combines[index] = combine;
		}

		public void Build()
		{
			dirty = false;

			if (--state < 0)
			{
				Rebase();
			}

			var combineArray = combineArrays[combines.Count];

			for (var i = 0; i < combines.Count; i++)
			{
				combineArray[i] = combines[i];
			}

			if (mesh == null)
			{
				mesh = new Mesh();
			}

			mesh.CombineMeshes(combineArray);
		}

		public void MarkAsDirty()
		{
			dirty = true;
		}

		public void Add(SgtDynamicPlanetChunk chunk)
		{
			var index   = chunks.Count;
			var combine = new CombineInstance();

			chunk.Visual = this;

			combine.mesh = chunk.Mesh;

			chunks.Add(chunk);
			combines.Add(combine);

			if (index == 0)
			{
				Rebase();
			}
			else
			{
				Rebase(chunk, index);
			}

			dirty = true;
		}

		public void Remove(SgtDynamicPlanetChunk chunk)
		{
			for (var i = chunks.Count - 1; i >= 0; i--)
			{
				if (chunks[i] == chunk)
				{
					chunks.RemoveAt(i);
					combines.RemoveAt(i);

					break;
				}
			}

			chunk.Visual = null;

			dirty = true;
		}

		private double3 GetOrigin()
		{
			if (chunks.Count > 0)
			{
				return math.floor(chunks[0].Corner);
			}

			return default(double3);
		}
	}
}