using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calculus 
{
    public static float integrate(float coeff, float pow, float r0, float rf){
        float finalVal = (1f / (pow + 1f)) * coeff * Mathf.Pow(rf, pow + 1f);
        float initalVal = (1f / (pow + 1f)) * coeff * Mathf.Pow(r0, pow + 1f);

        return finalVal - initalVal; 
    }

    public static float magnitude(Vector3 myVector){
        return Mathf.Sqrt((Mathf.Pow(myVector.x, 2f) + Mathf.Pow(myVector.y, 2f) + Mathf.Pow(myVector.z, 2f)));
    }

    public static Vector3 distanceTraveled(Vector3 velocity, Vector3 acceleration, float time){
        Vector3 distance = new Vector3();
        Vector3 vt = velocity * time;
        Vector3 at = .5f * acceleration * Mathf.Pow(time, 2);
        distance = vt + at;

        return distance;
    }

    public static Vector3 normalize(Vector3 myVector){
        return myVector / Mathf.Sqrt(magnitude(myVector));
    }

}
