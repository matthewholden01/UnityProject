using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RocketSettings", menuName = "RelativityProject/RocketSettings", order = 0)]
public class RocketSettings : ScriptableObject {
    public Vector3 rocketVelocity;
    public float timeToMaximumSpeed;
    public Vector3 distToTravelInLy;
    public float speedOfLight = 300000000f;
    public Vector3 distanceTraveled;
    public bool startRocket = false;
}