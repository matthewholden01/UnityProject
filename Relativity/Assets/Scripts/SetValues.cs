using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetValues : MonoBehaviour
{
    public RocketSettings rocketSettings;
    public Camera mainCamera;
    public Camera SecondCamera;
    public CanvasGroup canvasGroup, canvasGroup3;
    public GameObject velocity, distance, time;

    
    public void setValues(){
        string Velocity = velocity.GetComponent<TMP_InputField>().text;
        string Distance = distance.GetComponent<TMP_InputField>().text;
        string Time = time.GetComponent<TMP_InputField>().text;


        string[] velocities = Velocity.Split(',');
        string[] distances = Distance.Split(',');
        if(velocities.Length == 3){
            rocketSettings.rocketVelocity.x = float.Parse(velocities[0].Split('(')[1]);
            rocketSettings.rocketVelocity.y = float.Parse(velocities[1]);
            rocketSettings.rocketVelocity.z = float.Parse(velocities[2].Split(')')[0]);
        }else if(rocketSettings.rocketVelocity.magnitude >= rocketSettings.speedOfLight || rocketSettings.rocketVelocity.magnitude <= 0.0f){
            velocity.GetComponent<TMP_InputField>().text = "Magnitude of Velocity must be between 0 and 3x10^8";
            return;
        }else{
            velocity.GetComponent<TMP_InputField>().text = "Please input valid velocity in format (Vx, Vy, Vz)";
            return;
        }
        if(distances.Length == 3){
            rocketSettings.distToTravelInLy.x = float.Parse(distances[0].Split('(')[1]);
            rocketSettings.distToTravelInLy.y = float.Parse(distances[1]);
            rocketSettings.distToTravelInLy.z = float.Parse(distances[2].Split(')')[0]);

            rocketSettings.timeToMaximumSpeed = float.Parse(Time);

            StartCoroutine(fadeScreen(canvasGroup));
            rocketSettings.startRocket = true;
        }else{
            distance.GetComponent<TMP_InputField>().text = "Please Input Valid Distances in format (x, y, z)";
            return;
        }
        
    }

    IEnumerator fadeScreen(CanvasGroup myGroup){
        for(float t = 0.01f; t < 5.0f;){
            t += Time.deltaTime;
            t = Mathf.Min(t, 5.0f);
            myGroup.alpha = Mathf.Lerp(1, 0, Mathf.Min(1, t / 5.0f));
            yield return null;
        }
    }

    

    private void Update() {
        if(rocketSettings.endGame){
            StartCoroutine(fadeScreen(canvasGroup3));

            rocketSettings.endGame = false;
        }
    }
}
