using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class screenSpace : MonoBehaviour
{
    public Camera camera;
    public GameObject rocket, relRocket, star;
    Transform target, relTarget, starTrans;
    Vector3 initialPos, RelInitialPos, Origin;
    float timer = 0f;
    public RocketSettings rocketSettings;
    public RelativitySettings relativitySettings;

   
    public Vector3 normalizePointToScreenSpace(Vector3 distTrav, Vector3 origin){
        Vector3 finalPoint = new Vector3();
        Vector3 unitVector = distTrav / (rocketSettings.distToTravelInLy * rocketSettings.speedOfLight * 365f * 24f * 3600f).magnitude;
        finalPoint = origin + Vector3.Scale(unitVector, new Vector3(0.2f, 0.5f, origin.z));
        return finalPoint;
    }   

     public Vector3 LengthContraction(Vector3 transform, Vector3 velocity){
        float gamma = (Mathf.Sqrt(1 - (Mathf.Pow(velocity.magnitude, 2) / Mathf.Pow(rocketSettings.speedOfLight, 2))));
        Vector3 newLen = new Vector3();
        newLen = transform * gamma;

        return newLen;
    }

    private void Start() {
        starTrans = star.GetComponent<Transform>();
        target = rocket.GetComponent<Transform>();
        initialPos = camera.WorldToViewportPoint(target.position);
        Origin = new Vector3(0.2f, 0.5f, initialPos.z);
        target.position = camera.ViewportToWorldPoint(Origin);
        relTarget = relRocket.GetComponent<Transform>();
        relTarget.position = camera.ViewportToWorldPoint(Origin);

    }

    // Update is called once per frame
    void Update()
    {
        if(rocketSettings.startRocket == true){
            initialPos = camera.WorldToViewportPoint(target.position);
            RelInitialPos = camera.WorldToViewportPoint(relTarget.position);
            target.position = camera.ViewportToWorldPoint(normalizePointToScreenSpace(LengthContraction(rocketSettings.distanceTraveled, relativitySettings.velocity), Origin));
            relTarget.position = camera.ViewportToWorldPoint(normalizePointToScreenSpace(rocketSettings.distanceTraveled, Origin));
        }
    }
}
