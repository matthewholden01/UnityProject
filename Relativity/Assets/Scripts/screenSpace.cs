using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class screenSpace : MonoBehaviour
{
    public Camera camera;
    Transform target;
    Vector3 initialPos;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        target = GetComponent<Transform>();
        initialPos = camera.WorldToViewportPoint(target.position);
        target.position = camera.ViewportToWorldPoint(new Vector3(initialPos.x + 0.0001f, initialPos.y, initialPos.z));
    }
}
