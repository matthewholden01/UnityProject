using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RelativitySettings", menuName = "RelativityProject/RelativitySettings", order = 0)]
public class RelativitySettings : ScriptableObject {
    public Vector3 relativeScale;
    public float relativeTime;
    public Vector3 relativeDistance;
    public Vector3 velocity;
}