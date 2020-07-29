using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpaceGraphicsToolkit;
using TMPro;

public class LorentzTransforms : MonoBehaviour {
    public RocketSettings rocketSettings;
    public RelativitySettings relativitySettings;
    public GameObject gameObject, relativeRocket;
    public SgtThruster thruster;
    public TextMeshProUGUI[] properText = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] relativeText = new TextMeshProUGUI[4];
    private Rigidbody rigidbody;
    private Transform rocket_trans, relativeTrans;
    private Vector3 initScale, lengthDiff, distTraveled, accel, force;
    private Vector3[] predictedPath;
    private float  relativeTime;
     private string[] myStrings = {"Proper Length:", "Time Experienced:", "Diance Traveled:"};

    private int t = 0;
    private float timer = 0f;


    public float timeToTravel(Vector3 distance){
        float totalDist = Mathf.Sqrt((distance.x * distance.x) + (distance.y * distance.y) + (distance.z * distance.z));
        float totalSpeed = rocketSettings.rocketVelocity.magnitude;
        return totalDist / totalSpeed;
    }

    public float timeDilation(Vector3 Velocity, float time){
        float gamma = 1f / Mathf.Sqrt(1 - ((Mathf.Pow(Calculus.magnitude(Velocity), 2f)) / (Mathf.Pow(rocketSettings.speedOfLight, 2f))));
        return time * gamma;
    }

    public Vector3 LengthContraction(Vector3 transform, Vector3 velocity){
        float x = transform.x * Mathf.Sqrt(1f - (Mathf.Pow(velocity.x,2f)/Mathf.Pow(rocketSettings.speedOfLight, 2f)));
        float y = transform.y * Mathf.Sqrt(1f - (Mathf.Pow(velocity.y,2f)/Mathf.Pow(rocketSettings.speedOfLight, 2f)));
        float z = transform.z * Mathf.Sqrt(1f - (Mathf.Pow(velocity.z,2f)/Mathf.Pow(rocketSettings.speedOfLight, 2f)));

        return new Vector3(x, y, z);
    }

    public float LorentzDistance(Vector3 dist, Vector3 velocity, float time){
        float gamma = 1f / Mathf.Sqrt(1 - ((Mathf.Pow(velocity.magnitude, 2f)) / (Mathf.Pow(rocketSettings.speedOfLight, 2f))));
        float rhs1 = dist.magnitude - (gamma * velocity.magnitude * time);
        float rhs2 = (gamma - 1) / Mathf.Pow(Calculus.magnitude(velocity), 2f);
        float rhs3 = Vector3.Dot(velocity, dist) * Calculus.magnitude(velocity);

        return rhs1 + rhs2 * rhs3;
    }


    private void Awake() {
        //Proper Rocket Properties
        rigidbody = gameObject.GetComponent<Rigidbody>();
        rocket_trans = gameObject.GetComponent<Transform>();
        initScale = gameObject.GetComponent<Transform>().localScale;
        accel = rocketSettings.rocketVelocity / rocketSettings.timeToMaximumSpeed;
        force = rigidbody.mass * (accel);

        //Relative Rocket Properties
        relativeTrans = relativeRocket.GetComponent<Transform>();
    }

    private void Start() {
        relativeTrans.localScale = LengthContraction(initScale, rocketSettings.rocketVelocity);
    }

    private void FixedUpdate() {
        //Proper Statistics
        //Proper Time
        if(timer < 60f){
            timer += (Time.deltaTime);
            properText[1].text = "Proper Time: " + timer + " Seconds";
        }else if(timer > 60f && timer < 3600f){
            timer += (Time.deltaTime) * 60f;
            properText[1].text = "Proper Time: " + timer / 60f + " Minutes";
        }else if(timer > 3600f && timer < 86400f){
            timer += (Time.deltaTime) * 3600f;
            properText[1].text = "Proper Time: " + timer / 3600f + " Hours";
        }else if(timer > 86400f && timer < 3154000f){
            timer += (Time.deltaTime) * 86400f;
            properText[1].text = "Proper Time: " + timer / 86400f + " Days";
        }else{
            timer += (Time.deltaTime) * 3154000f;
            properText[1].text = "Proper Time: " + timer / 31540000f + " Years";
        }
        distTraveled = Calculus.distanceTraveled(rigidbody.velocity, accel, 0f, timer);
        properText[0].text = "Proper Length: " + Calculus.magnitude(initScale) + " Meters";
        //Proper Distance Traveled
        if( LengthContraction(distTraveled, rigidbody.velocity).magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) < 0.1f){
            properText[2].text = "Proper Distance: " + LengthContraction(distTraveled, rigidbody.velocity).magnitude / 1000f + " km";
        }else{
            properText[2].text = "Proper Distance: " + LengthContraction(distTraveled, rigidbody.velocity).magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) + " lightyears";
        }
        //relative statistics
        Debug.Log(LorentzDistance(distTraveled, rigidbody.velocity, timer));

        relativitySettings.relativeTime = timeDilation(rigidbody.velocity, timer);
        relativitySettings.relativeScale = LengthContraction(initScale, rigidbody.velocity);
        relativitySettings.velocity = rigidbody.velocity;

        
        relativeText[0].text = "Observed Length: " + relativitySettings.relativeScale.magnitude + " Meters";
        if(timer < 60f){
            relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime + " Seconds";
        }else if(timer > 60f && timer < 3600f){
            relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime / 60f + " Minutes";
        }else if(timer > 3600f && timer < 86400f){
            timer += (Time.deltaTime) * 3600f;
            relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime / 3600f + " Hours";
        }else if(timer > 86400f && timer < 3154000f){
            relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime/ 86400f + " Days";
        }else{
            relativeText[1].text = "Observers Time: " + relativitySettings.relativeTime / 31540000f + " Years";
        }

        if( Calculus.magnitude(distTraveled) / (rocketSettings.speedOfLight * 365f * 24f * 3600f) < 0.1f){
            relativeText[2].text = "Observed Distance Travelled: " +  distTraveled.magnitude / 1000f + " km";
        }else{
            relativeText[2].text = "Observed Distance Travelled: " +  distTraveled.magnitude / (rocketSettings.speedOfLight * 365f * 24f * 3600f) + " lightyears";
        }
        if(rigidbody.velocity.magnitude < 1000f){
            relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude + " m/s";
        }else if(rigidbody.velocity.magnitude > 1000f && rigidbody.velocity.magnitude < (rocketSettings.speedOfLight * .1f)){
            relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude / 1000f + " km/s";
        }else{
            relativeText[3].text = "Observed Velocity: " + rigidbody.velocity.magnitude / rocketSettings.speedOfLight + " c" ;
        }

        if(rigidbody.velocity.magnitude < rocketSettings.rocketVelocity.magnitude)
        {
            thruster.ForceMagnitude = force.magnitude * timer;
        }else{
            accel = Vector3.zero;
            thruster.ForceMagnitude = 0f;
        }
    }

}