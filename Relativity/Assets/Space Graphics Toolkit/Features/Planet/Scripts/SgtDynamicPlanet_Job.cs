using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace SpaceGraphicsToolkit
{
	/// <summary>This class contains job specific methods for the <b>SgtDynamicPlanet</b> class.</summary>
	public partial class SgtDynamicPlanet
	{
		[BurstCompile]
		public struct ChunkStartJob : IJob
		{
			[ReadOnly] public int Points;

			[ReadOnly] public double3 Cube;
			[ReadOnly] public double3 CubeH;
			[ReadOnly] public double3 CubeV;

			[WriteOnly] public NativeArray<double3> Vectors;
			[WriteOnly] public NativeArray<double>  Heights;

			public void Execute()
			{
				var quads = Points - 3.0;
				var stepX = CubeH / quads;
				var stepY = CubeV / quads;
				var last  = Points * Points - 1;

				for (var y = 0; y < Points; y++)
				{
					var point = Cube + stepY * (y - 1) - stepX;

					for (var x = 0; x < Points; x++)
					{
						var i = x + y * Points;

						Vectors[i] = math.normalize(point);
						Heights[i] = 0.0;

						point += stepX;
					}
				}

				// Store center in last direction
				Vectors[last] = math.normalize(Cube + (CubeH + CubeV) * 0.5);
			}
		}

		[BurstCompile]
		public struct ScaleJob : IJobParallelFor
		{
			public NativeArray<double>  Heights;

			[ReadOnly] public double  Radius;
			[ReadOnly] public double  Displacement;
			[ReadOnly] public double  WaterLevel;
			[ReadOnly] public bool    ClampWater;

			public void Execute(int index)
			{
				var final = Radius;
				var land  = Heights[index];

				if (ClampWater == true)
				{
					final += Displacement * InverseLerp(math.saturate(WaterLevel), 1.0, land);
				}
				else
				{
					final += Displacement * math.max(land, WaterLevel);
				}

				Heights[index] = final;
			}

			private double InverseLerp(double a, double b, double value)
			{
				return a != b ? math.saturate((value - a) / (b - a)) : 0.0;
			}
		}

		[BurstCompile]
		public struct ChunkEndJob : IJob
		{
			public NativeArray<double3> Vectors;
			public NativeArray<double>  Heights;

			public NativeArray<float3> MeshPositions;
			public NativeArray<float3> MeshNormals;
			public NativeArray<float4> MeshTangents;
			public NativeArray<float4> MeshCoords0;

			[ReadOnly] public double2 Coord;
			[ReadOnly] public double  CoordM;

			public void Execute()
			{
				var corner = Vectors[SgtDynamicPlanetChunk.Config.LAST_POINT] * Heights[SgtDynamicPlanetChunk.Config.LAST_POINT];
				var coordX = new double2(SgtDynamicPlanetChunk.Config.STEP, 0.0);
				var coordY = new double2(0.0, CoordM * SgtDynamicPlanetChunk.Config.STEP);
				var offset = math.dot(Vectors[SgtDynamicPlanetChunk.Config.LAST_POINT], new double3(-1.0, 0.0, 0.0)) > 0.0f ? 0.5 : 0.0;

				for (var y = 0; y < SgtDynamicPlanetChunk.Config.VERTS; y++)
				{
					for (var x = 0; x < SgtDynamicPlanetChunk.Config.VERTS; x++)
					{
						var index   = x + y * SgtDynamicPlanetChunk.Config.VERTS;
						var indexP  = (x + 1) + (y + 1) * SgtDynamicPlanetChunk.Config.POINTS;
						var indexB  = (x + 1) + (y    ) * SgtDynamicPlanetChunk.Config.POINTS;
						var indexL  = (x    ) + (y + 1) * SgtDynamicPlanetChunk.Config.POINTS;
						var indexR  = (x + 2) + (y + 1) * SgtDynamicPlanetChunk.Config.POINTS;
						var indexT  = (x + 1) + (y + 2) * SgtDynamicPlanetChunk.Config.POINTS;
						var vector  = Vectors[indexP];
						var point   = Vectors[indexP] * Heights[indexP];
						var pointB  = Vectors[indexB] * Heights[indexB];
						var pointL  = Vectors[indexL] * Heights[indexL];
						var pointR  = Vectors[indexR] * Heights[indexR];
						var pointT  = Vectors[indexT] * Heights[indexT];
						var vectorH = pointR - pointL;
						var vectorV = pointT - pointB;
						var normal  = -math.normalize(math.cross(vectorH, vectorV));
						var tangent = math.normalize(math.cross(normal, new double3(0.0, 1.0, 0.0)));
						var coordA  = CartesianToPolarUV(Vectors[indexP], offset);
						var coordB  = new double2(vector.x, vector.z) * 0.5;

						if (vector.y > 0.0)
						{
							coordB.x = -coordB.x;
						}

						MeshPositions[index] = (float3)(point - corner);
						MeshNormals[index] = (float3)(normal);
						MeshTangents[index] = new float4((float)tangent.x, (float)tangent.y, (float)tangent.z, -1.0f);
						MeshCoords0[index] = new float4((float2)coordA, (float2)coordB);
					}
				}

				// Calculate skirt verts
				var delta  = (float3)(corner * CoordM * SgtDynamicPlanetChunk.Config.STEP);
				var outerV = SgtDynamicPlanetChunk.Config.VERTS * SgtDynamicPlanetChunk.Config.VERTS;

				WriteEdge(ref outerV, 0, 1, delta);
				WriteEdge(ref outerV, SgtDynamicPlanetChunk.Config.QUADS, SgtDynamicPlanetChunk.Config.VERTS, delta);
				WriteEdge(ref outerV, SgtDynamicPlanetChunk.Config.VERTS * SgtDynamicPlanetChunk.Config.VERTS - 1, -1, delta);
				WriteEdge(ref outerV, SgtDynamicPlanetChunk.Config.VERTS * (SgtDynamicPlanetChunk.Config.VERTS - 1), -SgtDynamicPlanetChunk.Config.VERTS, delta);
			}

			private double2 CartesianToPolarUV(double3 vector, double offset)
			{
				var u = math.atan2(vector.x, vector.z);
				var v = math.asin(vector.y);

				u = (1.75 - u / (math.PI_DBL * 2.0) - offset) % 1.0 + offset;
				v = v / math.PI_DBL + 0.5;

				return new double2(u, v);
			}

			private void WriteEdge(ref int outerV, int innerV, int innerS, float3 delta)
			{
				for (var i = 0; i < SgtDynamicPlanetChunk.Config.QUADS; i++)
				{
					MeshPositions[outerV] = MeshPositions[innerV] - delta;
					MeshNormals[outerV] = MeshNormals[innerV];
					MeshTangents[outerV] = MeshTangents[innerV];
					MeshCoords0[outerV] = MeshCoords0[innerV];

					innerV += innerS;
					outerV += 1;
				}
			}
		}

		[BurstCompile]
		public struct HeightmapJob : IJobParallelFor
		{
			public NativeArray<double> Heights;

			[ReadOnly] public NativeArray<double3> Vectors;
			[ReadOnly] public NativeArray<float>   Data;
			
			[ReadOnly] public double  WaterLevel;
			[ReadOnly] public int2    Size;
			[ReadOnly] public double2 Scale;

			public void Execute(int index)
			{
				var land   = Sample(Vectors[index]);
				var detail = math.saturate((land - WaterLevel) * 10.0);

				Heights[index] = land + detail * Heights[index];
			}

			private double Sample(double3 xyz)
			{
				var x = (math.PI * 1.5 - math.atan2(xyz.x, xyz.z)) * Scale.x; x -= 0.5;
				var y = (math.asin(xyz.y) + math.PI * 0.5) * Scale.y; y -= 0.5;
				var u = x; x = (int)x; u -= x;
				var v = y; y = (int)y; v -= y;

				var aa = Sample(x - 1.0, y - 1.0); var ba = Sample(x, y - 1.0); var ca = Sample(x + 1.0, y - 1.0); var da = Sample(x + 2.0, y - 1.0);
				var ab = Sample(x - 1.0, y      ); var bb = Sample(x, y      ); var cb = Sample(x + 1.0, y      ); var db = Sample(x + 2.0, y      );
				var ac = Sample(x - 1.0, y + 1.0); var bc = Sample(x, y + 1.0); var cc = Sample(x + 1.0, y + 1.0); var dc = Sample(x + 2.0, y + 1.0);
				var ad = Sample(x - 1.0, y + 2.0); var bd = Sample(x, y + 2.0); var cd = Sample(x + 1.0, y + 2.0); var dd = Sample(x + 2.0, y + 2.0);

				var a = Hermite(aa, ba, ca, da, u);
				var b = Hermite(ab, bb, cb, db, u);
				var c = Hermite(ac, bc, cc, dc, u);
				var d = Hermite(ad, bd, cd, dd, u);

				return Hermite(a, b, c, d, v);
			}

			private double Hermite(double a, double b, double c, double d, double t)
			{
				var tt  = t * t;
				var tt3 = tt * 3.0f;
				var ttt = t * tt;
				var ttt2 = ttt * 2.0f;
				double a0, a1, a2, a3;

				var m0 = (c - a) * 0.5f;
				var m1 = (d - b) * 0.5f;

				a0  =  ttt2 - tt3 + 1.0f;
				a1  =  ttt  - tt * 2.0f + t;
				a2  =  ttt  - tt;
				a3  = -ttt2 + tt3;

				return a0*b + a1*m0 + a2*m1 + a3*c;
			}

			private float Sample(double u, double v)
			{
				var x = (int)(u % Size.x);
				var y = (int)v;

				x = math.clamp(x, 0, Size.x - 1);
				y = math.clamp(y, 0, Size.y - 1);

				return Data[x + y * Size.x];
			}
		}

		private static NativeArray<double3> oneVectors;
		private static NativeArray<double>  oneHeights;

		private static NativeArray<double3> threeVectors;
		private static NativeArray<double>  threeHeights;

		public static NativeArray<double3> chunkVectors;
		public static NativeArray<double>  chunkHeights;

		private static NativeArray<float3> meshPositions;
		private static NativeArray<float3> meshNormals;
		private static NativeArray<float4> meshTangents;
		private static NativeArray<float4> meshCoords0;

		[System.NonSerialized]
		private int2 heightmapSize;

		[System.NonSerialized]
		private NativeArray<float> heightmapData;

		[System.NonSerialized]
		private List<IWriteHeight> cachedHeightWriters = new List<IWriteHeight>();

		[System.NonSerialized]
		private bool cachedHeightWritersSet;

		private void AllocateJobData()
		{
			oneVectors = new NativeArray<double3>(1, Allocator.Persistent);
			oneHeights = new NativeArray<double>(1, Allocator.Persistent);

			threeVectors = new NativeArray<double3>(3, Allocator.Persistent);
			threeHeights = new NativeArray<double>(3, Allocator.Persistent);

			chunkVectors = new NativeArray<double3>(SgtDynamicPlanetChunk.Config.POINTS_COUNT, Allocator.Persistent);
			chunkHeights = new NativeArray<double>(SgtDynamicPlanetChunk.Config.POINTS_COUNT, Allocator.Persistent);

			meshPositions = new NativeArray<float3>(SgtDynamicPlanetChunk.Config.VERTS_COUNT, Allocator.Persistent);
			meshNormals   = new NativeArray<float3>(SgtDynamicPlanetChunk.Config.VERTS_COUNT, Allocator.Persistent);
			meshTangents  = new NativeArray<float4>(SgtDynamicPlanetChunk.Config.VERTS_COUNT, Allocator.Persistent);
			meshCoords0   = new NativeArray<float4>(SgtDynamicPlanetChunk.Config.VERTS_COUNT, Allocator.Persistent);
		}

		private void DisposeJobData()
		{
			oneVectors.Dispose();
			oneHeights.Dispose();

			threeVectors.Dispose();
			threeHeights.Dispose();

			chunkVectors.Dispose();
			chunkHeights.Dispose();

			meshPositions.Dispose();
			meshNormals.Dispose();
			meshTangents.Dispose();
			meshCoords0.Dispose();
		}

		private List<IWriteHeight> CachedHeightWriters
		{
			get
			{
				if (cachedHeightWritersSet == false)
				{
					cachedHeightWritersSet = true;

					GetComponents(cachedHeightWriters);
				}

				return cachedHeightWriters;
			}
		}

		private void Generate(SgtDynamicPlanetChunk chunk)
		{
			var startJob = new ChunkStartJob();

			startJob.Points  = SgtDynamicPlanetChunk.Config.POINTS;
			startJob.Cube    = chunk.Cube;
			startJob.CubeH   = chunk.CubeH;
			startJob.CubeV   = chunk.CubeV;
			startJob.Vectors = chunkVectors;
			startJob.Heights = chunkHeights;

			var handle = startJob.Schedule();

			foreach (var cachedHeightWriter in CachedHeightWriters) // NOTE: Property
			{
				cachedHeightWriter.WriteHeight(ref handle, chunkHeights, chunkVectors);
			}

			if (heightmapData.IsCreated == true)
			{
				var job = new HeightmapJob();

				job.Heights    = chunkHeights;
				job.Vectors    = chunkVectors;
				job.Data       = heightmapData;
				job.WaterLevel = waterLevel;
				job.Size       = heightmapSize;
				job.Scale      = heightmapSize / new double2(math.PI * 2.0, math.PI);

				handle = job.Schedule(chunkVectors.Length, math.max(1, chunkVectors.Length / SgtDynamicPlanetChunk.Config.BATCH_SPLIT), handle);
			}

			var scaleJob = new ScaleJob();
			
			scaleJob.Heights       = chunkHeights;
			scaleJob.Radius        = radius;
			scaleJob.Displacement  = displacement;
			scaleJob.WaterLevel    = waterLevel;
			scaleJob.ClampWater    = clampWater;

			handle = scaleJob.Schedule(chunkHeights.Length, 1, handle);

			var endJob = new ChunkEndJob();
			
			endJob.Heights       = chunkHeights;
			endJob.Vectors       = chunkVectors;
			endJob.Coord         = chunk.Coord;
			endJob.CoordM        = chunk.CoordM;
			endJob.MeshPositions = meshPositions;
			endJob.MeshCoords0   = meshCoords0;
			endJob.MeshNormals   = meshNormals;
			endJob.MeshTangents  = meshTangents;

			endJob.Schedule(handle).Complete();

			chunk.Corner = chunkVectors[SgtDynamicPlanetChunk.Config.LAST_POINT] * chunkHeights[SgtDynamicPlanetChunk.Config.LAST_POINT];

			chunk.Mesh.SetVertices(ConvertNativeArray(meshPositions));
			chunk.Mesh.SetNormals(ConvertNativeArray(meshNormals));
			chunk.Mesh.SetTangents(ConvertNativeArray(meshTangents));
			chunk.Mesh.SetUVs(0, ConvertNativeArray(meshCoords0));
			chunk.Mesh.SetTriangles(SgtDynamicPlanetVisual.Config.INDICES, 0);
		}

		public float3 TransformPoint(double3 local)
		{
			var world = math.mul(new double4x4(transform.localToWorldMatrix), new double4(local, 1.0)); return new float3((float)world.x, (float)world.y, (float)world.z);
		}

		public double3 InverseTransformPoint(float3 world)
		{
			var local = math.mul(new double4x4(transform.worldToLocalMatrix), new double4(world, 1.0)); return new double3(local.x, local.y, local.z);
		}

		public float3 TransformVector(double3 local)
		{
			var world = math.mul(new double4x4(transform.localToWorldMatrix), new double4(local, 0.0)); return new float3((float)world.x, (float)world.y, (float)world.z);
		}

		public double3 InverseTransformVector(float3 world)
		{
			var local = math.mul(new double4x4(transform.worldToLocalMatrix), new double4(world, 0.0)); return new double3(local.x, local.y, local.z);
		}

		public bool TryGetWorldPoint(float3 worldPoint, ref float3 sampledPoint)
		{
			if (node != null)
			{
				var localPoint        = InverseTransformPoint(worldPoint);
				var localSampledPoint = default(double3);

				TryGetLocalPoint(localPoint, ref localSampledPoint);

				sampledPoint = TransformPoint(localSampledPoint);

				return true;
			}

			return false;
		}

		private static Vector3 GetNormal(float3 vectorA, float3 vectorB, float length)
		{
			var smallsq = length * 0.1f; smallsq *= smallsq;

			if (math.lengthsq(vectorA) < smallsq)
			{
				return vectorB;
			}

			if (math.lengthsq(vectorB) < smallsq)
			{
				return vectorA;
			}

			return -math.cross(vectorA, vectorB);
		}

		public bool TryGetWorldNormal(float3 worldPoint, float3 worldRight, float3 worldForward, ref float3 sampledNormal)
		{
			if (node != null)
			{
				var sampledPointL = default(float3);
				var sampledPointR = default(float3);
				var sampledPointB = default(float3);
				var sampledPointF = default(float3);

				if (TryGetWorldPoint(worldPoint - worldRight  , ref sampledPointL) == true &&
					TryGetWorldPoint(worldPoint + worldRight  , ref sampledPointR) == true &&
					TryGetWorldPoint(worldPoint - worldForward, ref sampledPointB) == true &&
					TryGetWorldPoint(worldPoint + worldForward, ref sampledPointF) == true)
				{
					var vectorA = sampledPointR - sampledPointL;
					var vectorB = sampledPointF - sampledPointB;

					sampledNormal = math.normalize(GetNormal(vectorA, vectorB, math.length(worldRight)));

					if (math.dot(sampledNormal, sampledPointL - (float3)transform.position) < 0.0f)
					{
						sampledNormal = -sampledNormal;
					}

					return true;
				}
			}

			return false;
		}

		public bool TryGetLocalPoint(double3 localPoint, ref double3 sampledPoint)
		{
			if (node != null)
			{
				SampleLocal(localPoint);

				sampledPoint = oneVectors[0] * oneHeights[0];

				return true;
			}

			return false;
		}

		public bool TryGetLocalHeight(double3 localPoint, ref double sampledHeight)
		{
			if (node != null)
			{
				SampleLocal(localPoint);

				sampledHeight = oneHeights[0];

				return true;
			}

			return false;
		}

		private void SampleLocal(double3 pointA, double3 pointB, double3 pointC)
		{
			threeVectors[0] = math.normalize(pointA);
			threeVectors[1] = math.normalize(pointB);
			threeVectors[2] = math.normalize(pointC);
			threeHeights[0] = 0.0;
			threeHeights[1] = 0.0;
			threeHeights[2] = 0.0;

			RunJobsNow(threeHeights, threeVectors);
		}

		private void SampleLocal(double3 point)
		{
			oneVectors[0] = math.normalize(point);
			oneHeights[0] = 0.0;

			RunJobsNow(oneHeights, oneVectors);
		}

		private void RunJobsNow(NativeArray<double> heights, NativeArray<double3> vectors)
		{
			foreach (var cachedHeightWriter in CachedHeightWriters) // NOTE: Property
			{
				var handle = default(JobHandle);

				if (cachedHeightWriter.WriteHeight(ref handle, heights, vectors) == true)
				{
					handle.Complete();
				}
			}

			if (heightmapData.IsCreated == true)
			{
				var job = new HeightmapJob();

				job.Heights    = heights;
				job.Vectors    = vectors;
				job.Data       = heightmapData;
				job.WaterLevel = waterLevel;
				job.Size       = heightmapSize;
				job.Scale      = heightmapSize / new double2(math.PI * 2.0, math.PI);

				job.Schedule(heights.Length, 1).Complete();
			}

			var scaleJob = new ScaleJob();
			
			scaleJob.Heights       = heights;
			scaleJob.Radius        = radius;
			scaleJob.Displacement  = displacement;
			scaleJob.WaterLevel    = waterLevel;
			scaleJob.ClampWater    = clampWater;

			scaleJob.Schedule(heights.Length, 1).Complete();
		}
#if UNITY_2019_3_OR_NEWER
		public static NativeArray<float2> ConvertNativeArray(NativeArray<float2> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<float3> ConvertNativeArray(NativeArray<float3> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<float4> ConvertNativeArray(NativeArray<float4> nativeArray)
		{
			return nativeArray;
		}
#else
		private static List<Vector2> tempVector2s = new List<Vector2>(1024);
		public static List<Vector2> ConvertNativeArray(NativeArray<float2> nativeArray)
		{
			tempVector2s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector2s.Add(nativeArray[i]); return tempVector2s;
		}
		private static List<Vector3> tempVector3s = new List<Vector3>(1024);
		public static List<Vector3> ConvertNativeArray(NativeArray<float3> nativeArray)
		{
			tempVector3s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector3s.Add(nativeArray[i]); return tempVector3s;
		}
		private static List<Vector4> tempVector4s = new List<Vector4>(1024);
		public static List<Vector4> ConvertNativeArray(NativeArray<float4> nativeArray)
		{
			tempVector4s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector4s.Add(nativeArray[i]); return tempVector4s;
		}
#endif
	}
}