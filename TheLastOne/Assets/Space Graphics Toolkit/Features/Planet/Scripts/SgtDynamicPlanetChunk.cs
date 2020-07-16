using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace SpaceGraphicsToolkit
{
	/// <summary>This class stores data for a terrain chunk that can be split into 4 smaller chunks.</summary>
	public class SgtDynamicPlanetChunk
	{
		public static class Config
		{
			/// <summary>The amount of rows & columns of quads in each chunk.
			/// NOTE: This should be a multiple of 2.</summary>
			public static readonly int QUADS = 16;

			/// <summary>The amount of rows & columns of vertices in each chunk.</summary>
			public static readonly int VERTS = QUADS + 1;

			/// <summary>Vertices in the main chunk surface + skirt.</summary>
			public static readonly int VERTS_COUNT = VERTS * VERTS + VERTS * 4;

			/// <summary>The amount of rows & columns of direction & height data.</summary>
			public static readonly int POINTS = VERTS + 2;

			public static readonly int POINTS_COUNT = POINTS * POINTS;

			public static readonly int BATCH_SPLIT = POINTS;

			/// <summary>The last index in the points array.</summary>
			public static readonly int LAST_POINT = POINTS * POINTS - 1;

			/// <summary>The 0..1 amount each vertex traverses across the chunk surface.</summary>
			public static readonly double STEP = 1.0 / QUADS;
		}

		public int                     Depth;
		public bool                    Split;
		public SgtDynamicPlanetVisual  Visual;
		public SgtDynamicPlanetChunk   Parent;
		public SgtDynamicPlanetChunk[] Children = new SgtDynamicPlanetChunk[4];

		public double3 Cube;
		public double3 CubeH;
		public double3 CubeV;
		public double2 Coord;
		public double  CoordM;
		public double3 Corner;
		public Mesh    Mesh = new Mesh();

		public static Stack<SgtDynamicPlanetChunk> Pool = new Stack<SgtDynamicPlanetChunk>();

		private static SgtDynamicPlanetChunk Create()
		{
			return Pool.Count > 0 ? Pool.Pop() : new SgtDynamicPlanetChunk();
		}

		public static SgtDynamicPlanetChunk Create(SgtDynamicPlanetChunk parent, double3 cube, double3 cubeH, double3 cubeV, double2 coord, double coordM)
		{
			var chunk = Create();

			chunk.Split  = false;
			chunk.Depth  = parent != null ? parent.Depth + 1 : 0;
			chunk.Parent = parent;
			chunk.Cube   = cube;
			chunk.CubeH  = cubeH;
			chunk.CubeV  = cubeV;
			chunk.Coord  = coord;
			chunk.CoordM = coordM;

			return chunk;
		}

		public void Dispose()
		{
			SgtHelper.Destroy(Mesh);

			if (Visual != null)
			{
				Visual.Remove(this);
			}

			if (Split == true)
			{
				for (var c = 0; c < 4; c++)
				{
					Children[c].Dispose();
				}
			}
		}
	}
}