﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component handles a single debris object.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDebris")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Debris")]
	public class SgtDebris : MonoBehaviour
	{
		public enum StateType
		{
			Hide,
			Fade,
			Show,
		}

		/// <summary>Called when this debris is spawned (if pooling is enabled).</summary>
		public System.Action OnSpawn;

		/// <summary>Called when this debris is despawned (if pooling is enabled).</summary>
		public System.Action OnDespawn;

		/// <summary>Can this debris be pooled?</summary>
		public bool Pool;

		/// <summary>The current state of the scaling.</summary>
		public StateType State;

		/// <summary>The prefab this was instantiated from.</summary>
		public SgtDebris Prefab;

		/// <summary>This gets automatically copied when spawning debris.</summary>
		public Vector3 Scale;

		/// <summary>The cell this debris was spawned in.</summary>
		public SgtLong3 Cell;

		// The initial scale-in
		public float Show;
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtDebris))]
	public class SgtDebris_Editor : SgtEditor<SgtDebris>
	{
		protected override void OnInspector()
		{
			Draw("Pool", "Can this debris be pooled?");

			Separator();

			BeginDisabled();
				Draw("State", "The current state of the scaling.");
				Draw("Prefab", "The prefab this was instantiated from.");
				Draw("Scale", "This gets automatically copied when spawning debris.");
				Draw("Cell", "The cell this debris was spawned in.");
			EndDisabled();
		}
	}
}
#endif