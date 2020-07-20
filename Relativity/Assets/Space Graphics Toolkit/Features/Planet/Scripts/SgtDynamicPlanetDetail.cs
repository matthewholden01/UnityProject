using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows to displace the height of a terrain using procedural noise based on a splat map texture.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDynamicPlanetDetail")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Dynamic Planet Detail")]
	public class SgtDynamicPlanetDetail : SgtDynamicPlanetModifier, SgtDynamicPlanet.IWriteHeight
	{
		public enum ChannelType
		{
			Red,
			Green,
			Blue,
			Alpha
		}

		public enum StyleType
		{
			Simplex,
			Ridged
		}

		/// <summary>The splat texture used to control the contribution of the detail settings below.
		/// NOTE: This should use an Equirectangular projection.
		/// NOTE: This texture should be marked as readable.</summary>
		public Texture2D Splat { set { splat = value; } get { return splat; } } [SerializeField] private Texture2D splat;

		/// <summary>This allows you to choose which color channel from the splat texture that will be used.</summary>
		public ChannelType Channel { set { channel = value; } get { return channel; } } [SerializeField] private ChannelType channel;

		/// <summary>This allows you to choose the type of noise applied from the splat map.</summary>
		public StyleType Style { set { style = value; } get { return style; } } [SerializeField] private StyleType style;

		/// <summary>This allows you to control the frequency/tiling of the detail applied from the red channel of the splat texture.</summary>
		public double Frequency { set { frequency = value; } get { return frequency; } } [SerializeField] private double frequency = 100.0;

		/// <summary>This allows you to control the levels of detail applied from the red channel of the splat texture.</summary>
		public int Octaves { set { octaves = value; } get { return octaves; } } [Range(1, 15)] [SerializeField] private int octaves = 5;

		/// <summary>This allows you to control the height displacement of detail applied from the red channel of the splat texture.</summary>
		public double Strength { set { strength = value; } get { return strength; } } [Range(0.0f, 0.5f)] [SerializeField] private double strength = 0.25;

		[System.NonSerialized]
		private int2 splatSize;

		[System.NonSerialized]
		private NativeArray<float> splatData;

		protected virtual void OnEnable()
		{
			CachedPlanet.OnRebuild += Rebuild;

			Rebuild();
		}

		protected virtual void OnDisable()
		{
			cachedPlanet.OnRebuild -= Rebuild;

			Clear();
		}

		private void Clear()
		{
			if (splatData.IsCreated == true)
			{
				splatData.Dispose();
			}
		}

		private void Rebuild()
		{
			Clear();
#if UNITY_EDITOR
			SgtHelper.MakeTextureReadable(splat);
#endif
			if (splat != null)
			{
				var pixels = splat.GetPixels();
				var total  = pixels.Length;

				splatData = new NativeArray<float>(total, Allocator.Persistent);
				splatSize = new int2(splat.width, splat.height);

				switch (channel)
				{
					case ChannelType.Red:
					{
						for (var i = 0; i < total; i++)
						{
							splatData[i] = pixels[i].r;
						}
					}
					break;

					case ChannelType.Green:
					{
						for (var i = 0; i < total; i++)
						{
							splatData[i] = pixels[i].g;
						}
					}
					break;

					case ChannelType.Blue:
					{
						for (var i = 0; i < total; i++)
						{
							splatData[i] = pixels[i].b;
						}
					}
					break;

					case ChannelType.Alpha:
					{
						for (var i = 0; i < total; i++)
						{
							splatData[i] = pixels[i].a;
						}
					}
					break;
				}

				var t = (splatSize.y - 1) * splatSize.x;

				for (var x = 1; x < splatSize.x; x++)
				{
					splatData[x    ] = splatData[0];
					splatData[x + t] = splatData[t];
				}
			}
			else
			{
				splatData = new NativeArray<float>(0, Allocator.Persistent);
				splatSize = int2.zero;
			}
		}

		public bool WriteHeight(ref JobHandle handle, NativeArray<double> heights, NativeArray<double3> vectors)
		{
			if (splatData.IsCreated == true)
			{
				var job = new DetailJob();
				
				job.Heights   = heights;
				job.Vectors   = vectors;
				job.Data      = splatData;
				job.Size      = splatSize;
				job.Scale     = splatSize / new double2(math.PI * 2.0, math.PI);
				job.Style     = Style;
				job.Frequency = frequency;
				job.Octaves   = octaves;
				job.Strength  = strength;

				handle = job.Schedule(vectors.Length, math.max(1, vectors.Length / SgtDynamicPlanetChunk.Config.BATCH_SPLIT), handle);

				return true;
			}

			return false;
		}

		[BurstCompile]
		public struct DetailJob : IJobParallelFor
		{
			public NativeArray<double> Heights;

			[ReadOnly] public NativeArray<double3> Vectors;
			[ReadOnly] public NativeArray<float>   Data;
			[ReadOnly] public int2                 Size;
			[ReadOnly] public double2              Offset;
			[ReadOnly] public double2              Scale;
			[ReadOnly] public StyleType            Style;
			[ReadOnly] public double               Frequency;
			[ReadOnly] public int                  Octaves;
			[ReadOnly] public double               Strength;

			public void Execute(int index)
			{
				var vector = Vectors[index];
				var sample = Sample(vector);
				
				Heights[index] += sample * Fractal(vector) * Strength;
			}

			private double Fractal(double3 vector)
			{
				var scale = Frequency;
				var str   = 1.0;
				var value = 0.0;

				switch (Style)
				{
					case StyleType.Simplex:
					{
						for (var o = 0; o < Octaves; o++)
						{
							var sample = noise.snoise((float3)(vector * scale));

							value += sample * str;
							scale *= 2.0;
							str   *= 0.5;
						}
					}
					break;

					case StyleType.Ridged:
					{
						for (var o = 0; o < Octaves; o++)
						{
							var sample = noise.snoise((float3)(vector * scale));

							sample = 1.0f - math.abs(sample);

							value += sample * str;
							scale *= 2.0;
							str   *= 0.5;
						}
					}
					break;
				}

				return value;
			}

			private double Sample(double3 xyz)
			{
				if (Size.x <= 0 || Size.y <= 0)
				{
					return 1.0;
				}

				var x = (math.PI * 2.0 - math.atan2(xyz.x, xyz.z)) * Scale.x;
				var y = (math.asin(xyz.y) + math.PI * 0.5) * Scale.y;
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
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CustomEditor(typeof(SgtDynamicPlanetDetail))]
	public class SgtDynamicPlanetDetail_Editor : SgtEditor<SgtDynamicPlanetDetail>
	{
		protected override void OnInspector()
		{
			Draw("splat", "The splat texture used to control the contribution of the detail settings below.\n\nNOTE: This should use an Equirectangular projection.\n\nNOTE: This texture should be marked as readable.");
			Draw("channel", "This allows you to choose which color channel from the splat texture that will be used.");

			Separator();
			
			Draw("style", "This allows you to choose the type of noise applied from the splat map.");
			BeginError(Any(t => t.Frequency == 0.0));
				Draw("frequency", "This allows you to control the frequency/tiling of the detail applied from the red channel of the splat texture.");
			EndError();
			Draw("octaves", "This allows you to control the levels of detail applied from the red channel of the splat texture.");
			BeginError(Any(t => t.Strength == 0.0));
				Draw("strength", "This allows you to control the height displacement of detail applied from the red channel of the splat texture.");
			EndError();
		}
	}
}
#endif