using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelerateTowards : MonoBehaviour
{
    public GameObject targetObject;
    public float speed = 2.0f;
    private Transform target;
    // Start is called before the first frame update
    void Awake() {
        target = targetObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        float step = speed * Time.deltaTime;
        while(transform.position.magnitude != (target.position.magnitude + 100) || (transform.position.magnitude != target.position.magnitude - 100)){
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);
        }
    }
}
