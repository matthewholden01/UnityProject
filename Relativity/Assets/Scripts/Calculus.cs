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

    public static Vector3 distanceTraveled(Vector3 velocity, Vector3 acceleration, float initial, float final){
        float v0tx = integrate(velocity.x, 0f, initial, final);
        float atx = integrate(acceleration.x, 1f, initial, final);
        float v0ty = integrate(velocity.y, 0f, initial, final);
        float aty = integrate(acceleration.y, 1f, initial, final);
        float v0tz = integrate(velocity.z, 0f, initial, final);
        float atz = integrate(acceleration.z, 1f, initial, final);

        return new Vector3(v0tx + atx, v0ty + aty, v0tz + atz);
    }

}
