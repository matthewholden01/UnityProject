using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PlanetSettings : ScriptableObject
{
    public Stats[] Planet_Variables = new Stats[10];

    [System.Serializable]
   public class Stats
    {
        public string gameObject;
        public Vector3 init_scale;
        public Vector3 currentScale;

        
    }
}
