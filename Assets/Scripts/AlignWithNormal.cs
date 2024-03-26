using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignWithNormal : MonoBehaviour
{
    RaycastHit hit;
    Vector3 theRay;

    public LayerMask terainMask;

    void FixedUpdate()
    {
        Align();
    }
  
    private void Align()
    {
        theRay = -transform.up;

        if (Physics.Raycast(new Vector3(transform.position.x, transform.position.y, transform.position.z),
                theRay, out hit, 20, terainMask))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation (transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation,slopeRotation * transform.rotation,5*Time.deltaTime);
        }
    }
}
