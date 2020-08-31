using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a planet that has been displaced with a heightmap, and has a dynamic water level.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStaticPlanet")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Static Planet")]
	public class SgtStaticPlanet : MonoBehaviour
	{
		/// <summary>The sphere mesh used to render the planet.</summary>
		public Mesh Mesh { set { mesh = value; dirty = true; } get { return mesh; } } [SerializeField] private Mesh mesh;

		/// <summary>If you want the generated mesh to have a matching collider, you can specify it here.</summary>
		public MeshCollider MeshCollider { set { meshCollider = value; dirty = true; } get { return meshCollider; } } [SerializeField] private MeshCollider meshCollider;

		/// <summary>The radius of the planet in local space.</summary>
		public float Radius { set { radius = value; dirty = true; } get { return radius; } } [SerializeField] private float radius = 1.0f;

		/// <summary>The material used to render the planet. For best results, this should use the SGT Planet shader.</summary>
		public Material Material { set { material = value; } get { return material; } } [SerializeField] private Material material;

		/// <summary>If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.</summary>
		public SgtSharedMaterial SharedMaterial { set { sharedMaterial = value; } get { return sharedMaterial; } } [SerializeField] private SgtSharedMaterial sharedMaterial;

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

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private List<Vector3> generatedPositions = new List<Vector3>();

		[System.NonSerialized]
		private List<Vector4> generatedCoords = new List<Vector4>();

		[System.NonSerialized]
		private MaterialPropertyBlock properties;

		[System.NonSerialized]
		private bool dirty;

		/// <summary>This method causes the planet mesh to update based on the current settings. You should call this after you finish modifying them.</summary>
		[ContextMenu("Rebuild")]
		public void Rebuild()
		{
			dirty         = false;
			generatedMesh = SgtHelper.Destroy(generatedMesh);

			if (mesh != null)
			{
				generatedMesh = Instantiate(mesh);

				generatedMesh.GetVertices(generatedPositions);
				generatedMesh.GetUVs(0, generatedCoords);

				var count = generatedMesh.vertexCount;
#if UNITY_EDITOR
				SgtHelper.MakeTextureReadable(heightmap);
#endif
				for (var i = 0; i < count; i++)
				{
					var height = radius;
					var vector = generatedPositions[i].normalized;
					var coord  = generatedCoords[i];

					if (vector.y > 0.0f)
					{
						coord.z = -vector.x * 0.5f;
						coord.w = vector.z * 0.5f;
					}
					else
					{
						coord.z = vector.x * 0.5f;
						coord.w = vector.z * 0.5f;
					}

					generatedCoords[i] = coord;

					generatedPositions[i] = vector * Sample(vector);
				}

				generatedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * (radius + displacement) * 2.0f);

				generatedMesh.SetVertices(generatedPositions);
				generatedMesh.SetUVs(0, generatedCoords);

				generatedMesh.RecalculateNormals();
				generatedMesh.RecalculateTangents();

				if (meshCollider != null)
				{
					meshCollider.sharedMesh = null;
					meshCollider.sharedMesh = generatedMesh;
				}
			}
		}

		protected virtual void OnEnable()
		{
			SgtHelper.OnCalculateDistance += HandleCalculateDistance;
		}

		protected virtual void OnDisable()
		{
			SgtHelper.OnCalculateDistance -= HandleCalculateDistance;
		}

		protected virtual void LateUpdate()
		{
			if (generatedMesh == null || dirty == true)
			{
				Rebuild();
			}

			if (generatedMesh != null && material != null)
			{
				if (properties == null)
				{
					properties = new MaterialPropertyBlock();
				}

				properties.SetFloat(SgtShader._WaterLevel, waterLevel);

				Graphics.DrawMesh(generatedMesh, transform.localToWorldMatrix, material, gameObject.layer, null, 0, properties);

				if (sharedMaterial != null && sharedMaterial.Material != null)
				{
					Graphics.DrawMesh(generatedMesh, transform.localToWorldMatrix, sharedMaterial.Material, gameObject.layer, null, 0, properties);
				}
			}
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedMesh);
		}

		private void HandleCalculateDistance(Vector3 worldPosition, ref float distance)
		{
			var localPosition = transform.InverseTransformPoint(worldPosition);

			localPosition = localPosition.normalized * Sample(localPosition);

			var surfacePosition = transform.TransformPoint(localPosition);
			var thisDistance    = Vector3.Distance(worldPosition, surfacePosition);

			if (thisDistance < distance)
			{
				distance = thisDistance;
			}
		}

		private float Sample(Vector3 vector)
		{
			var final = radius;

			if (heightmap != null)
			{
				var uv   = SgtHelper.CartesianToPolarUV(vector);
				var land = heightmap.GetPixelBilinear(uv.x, uv.y).a;

				if (clampWater == true)
				{
					final += displacement * Mathf.InverseLerp(Mathf.Clamp01(waterLevel), 1.0f, land);
				}
				else
				{
					final += displacement * Mathf.Max(land, waterLevel);
				}
			}

			return final;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtStaticPlanet))]
	public class SgtStaticPlanet_Editor : SgtEditor<SgtStaticPlanet>
	{
		protected override void OnInspector()
		{
			var rebuild = false;

			BeginError(Any(t => t.Mesh == null));
				DrawDefault("mesh", ref rebuild, "The sphere mesh used to render the planet.");
			EndError();
			DrawDefault("meshCollider", ref rebuild, "If you want the generated mesh to have a matching collider, you can specify it here.");
			BeginError(Any(t => t.Radius <= 0.0f));
				DrawDefault("radius", ref rebuild, "The radius of the planet in local space.");
			EndError();

			Separator();

			BeginError(Any(t => t.Material == null));
				Draw("material", "The material used to render the planet. For best results, this should use the SGT Planet shader.");
			EndError();
			Draw("sharedMaterial", "If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.");

			Separator();

			DrawDefault("heightmap", ref rebuild, "The heightmap texture, where the height data is stored in the alpha channel.\n\nNOTE: This should use an Equirectangular projection.\n\nNOTE: This texture should be marked as readable.");
			BeginError(Any(t => t.Displacement == 0.0f));
				DrawDefault("displacement", ref rebuild, "The maximum height displacement applied to the planet mesh when the heightmap alpha value is 1.");
			EndError();
			DrawDefault("waterLevel", ref rebuild, "The current water level.\n\n0 = Radius.\n\n1 = Radius + Displacement.");
			DrawDefault("clampWater", ref rebuild, "If you enable this then the water will not rise, instead the terrain will shrink down.");

			if (rebuild == true) DirtyEach(t => t.Rebuild());
		}
	}
}
#endif