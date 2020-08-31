using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpaceGraphicsToolkit;
using TMPro;

public class LorentzTransforms : MonoBehaviour {
    public RocketSettings rocketSettings;
    public RelativitySettings relativitySettings;
    public GameObject gameObject;
    public SgtThruster thruster;
    public TextMeshProUGUI[] properText = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] relativeText = new TextMeshProUGUI[4];
    private Rigidbody rigidbody;
    private Transform rocket_trans, relativeTrans;
    private Vector3 initScale, lengthDiff, distTraveled, accel, force;
    private Vector3[] predictedPath;
    private float  relativeTime;
    private string[] myStrings = {"Proper Length:", "Time Experienced:", "Diance Traveled:"};
    private float timer = 0f;

    public float timeDilation(Vector3 Velocity, float time){
        float gamma = 1f / Mathf.Sqrt(1 - ((Mathf.Pow(Calculus.magnitude(Velocity), 2f)) / (Mathf.Pow(rocketSettings.speedOfLight, 2f))));
        return time * gamma;
    }

    public Vector3 LengthContraction(Vector3 transform, Vector3 velocity){
        float gamma = (Mathf.Sqrt(1 - (Mathf.Pow(velocity.magnitude, 2) / Mathf.Pow(rocketSettings.speedOfLight, 2))));
        Vector3 newLen = new Vector3();
        newLen = transform * gamma;

        return newLen;
    }


    private void Awake() {

        rigidbody = gameObject.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = false;
        rocket_trans = gameObject.GetComponent<Transform>();
        initScale = gameObject.GetComponent<Transform>().localScale;
    }


    private void FixedUpdate() {
        //Proper Statistics
        //Proper Time
        if(rocketSettings.startRocket == true){

        accel = rocketSettings.rocketVelocity / rocketSettings.timeToMaximumSpeed;
        force = rigidbody.mass * (accel);

            if(timer < rocketSettings.timeToMaximumSpeed){
                timer += (Time.deltaTime);
                properText[1].text = "Proper Time: " + timer + " Seconds";
            }else{
                timer += (Time.deltaTime) * 3154000f;
                properText[1].text = "Proper Time: " + timer / 31540000f + " Years";
            }
            if(timer < rocketSettings.timeToMaximumSpeed){
                distTraveled = Calculus.distanceTraveled(Vector3.zero, accel, timer);
            }else{
                distTraveled = Calculus.distanceTraveled(Vector3.zero, rocketSettings.rocketVelocity / rocketSettings.timeToMaximumSpeed, rocketSettings.timeToMaximumSpeed) + Calculus.distanceTraveled(rocketSettings.rocketVelocity, Vector3.zero, timer - rocketSettings.timeToMaximumSpeed);
            }

            properText[0].text = "Proper Length: " + Calculus.magnitude(initScale) + " Meters";

            if( LengthContraction(distTraveled, rigidbody.velocity).magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) < 0.1f){
                properText[2].text = "Proper Distance: " + LengthContraction(distTraveled, rigidbody.velocity).magnitude / 1000f + " km";
            }else{
                properText[2].text = "Proper Distance: " + LengthContraction(distTraveled, rigidbody.velocity).magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) + " lightyears";
            }
            //relative statistics
            relativitySettings.relativeTime = timeDilation(rigidbody.velocity, timer);
            relativitySettings.relativeScale = LengthContraction(initScale, rigidbody.velocity);
            relativitySettings.velocity = rigidbody.velocity;
            rocketSettings.distanceTraveled = distTraveled;
            
            relativeText[0].text = "Observed Length: " + relativitySettings.relativeScale.magnitude + " Meters";
            if(timer < rocketSettings.timeToMaximumSpeed){
                relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime + " Seconds";
            }else{
                relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime / 31540000f + " Years";
            }

            if( distTraveled.magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) < 0.1f){
                relativeText[2].text = "Observed Distance Travelled: " +  distTraveled.magnitude / 1000f + " km";
            }else{
                relativeText[2].text = "Observed Distance Travelled: " +  distTraveled.magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) + " lightyears";
            }

            if(rigidbody.velocity.magnitude < 1000f){
                relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude + " m/s";
            }else if(rigidbody.velocity.magnitude > 1000f && rigidbody.velocity.magnitude / (rocketSettings.speedOfLight) < 0.1f){
                relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude / 1000f + " km/s";
            }else{
                relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude / rocketSettings.speedOfLight + " c" ;
            }

            if(rigidbody.velocity.magnitude < rocketSettings.rocketVelocity.magnitude)
            {
                thruster.ForceMagnitude = force.magnitude;
            }else{
                accel = Vector3.zero;
                thruster.ForceMagnitude = 0f;
            }
        }
    }

    private void OnDestroy() {
        rocketSettings.rocketVelocity = Vector3.zero;
        rocketSettings.timeToMaximumSpeed = 0f;
        rocketSettings.distToTravelInLy = Vector3.zero;
        rocketSettings.startRocket = false;
        rocketSettings.distanceTraveled = Vector3.zero;
    }

}