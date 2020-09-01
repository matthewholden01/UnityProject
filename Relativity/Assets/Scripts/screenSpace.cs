using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class screenSpace : MonoBehaviour
{
    public Camera camera;
    public GameObject rocket, relRocket;
    Transform target, relTarget;
    Vector3 initialPos, RelInitialPos, Origin;
    float time, timer = 0f;
    public RocketSettings rocketSettings;
    public RelativitySettings relativitySettings;

   
    

     public Vector3 LengthContraction(Vector3 transform, Vector3 velocity){
        float gamma = (Mathf.Sqrt(1 - (Mathf.Pow(velocity.magnitude, 2) / Mathf.Pow(rocketSettings.speedOfLight, 2))));
        Vector3 newLen = new Vector3();
        newLen = transform * gamma;

        return newLen;
    }

    private void Start() {
        target = rocket.GetComponent<Transform>();
        relTarget = relRocket.GetComponent<Transform>();
        initialPos = camera.WorldToViewportPoint(target.position);
        Origin = new Vector3(0.3f, 0.4f, initialPos.z);
        target.position = camera.ViewportToWorldPoint(Origin);
        relTarget.position = camera.ViewportToWorldPoint(Origin);
    }

    // Update is called once per frame
    void Update()
    {
        if(rocketSettings.startRocket == true){
             time+= Time.deltaTime;
             Vector3 endPoint = Origin + (rocketSettings.distToTravelInLy / (rocketSettings.distToTravelInLy + Vector3.Scale(Origin, rocketSettings.distToTravelInLy)).magnitude) - camera.WorldToViewportPoint(relTarget.position);
             relTarget.position = camera.ViewportToWorldPoint(Origin + ((rocketSettings.distanceTraveled / (rocketSettings.speedOfLight * 365f * 24f * 3600f)) / (rocketSettings.distToTravelInLy + Vector3.Scale(Origin, rocketSettings.distToTravelInLy)).magnitude));
             target.position = camera.ViewportToWorldPoint(LengthContraction(Origin + ((rocketSettings.distanceTraveled / (rocketSettings.speedOfLight * 365f * 24f * 3600f)) / (rocketSettings.distToTravelInLy + Vector3.Scale(Origin, rocketSettings.distToTravelInLy)).magnitude), relativitySettings.velocity));
             Quaternion rotation = Quaternion.LookRotation(endPoint,Vector3.forward);
             Quaternion relRotation = Quaternion.LookRotation(LengthContraction(endPoint, rocketSettings.rocketVelocity), Vector3.forward);
             if(time >= rocketSettings.timeToMaximumSpeed){
                relTarget.rotation = Quaternion.Slerp(relTarget.rotation, rotation, time);
                target.rotation = Quaternion.Slerp(target.rotation, relRotation, time);
                timer = (timer + Time.deltaTime) / 60f;
             }
        }
    }
}
