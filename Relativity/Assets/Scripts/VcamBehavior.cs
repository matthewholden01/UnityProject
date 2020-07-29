using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class VcamBehavior : MonoBehaviour
{
    public CinemachineVirtualCameraBase vcam;
    public GameObject gameObject;
    private Transform pos;
    // Start is called before the first frame update
    void Awake() {
        pos = gameObject.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        //vcam.LookAt = pos;
    }
}
