using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform torso;
    public Transform player;
    public float maxDist;
    public float minDist;
    public float targetYOffset;
    public float rayAngle;
    public LayerMask playerMask;

    RaycastHit rayHit;

    // Start is called before the first frame update
    void Start()
    {
        rayHit = new RaycastHit();
    }

    // LateUpdate is called after Update()
    void LateUpdate()
    {
        Vector3 rayDirection = Quaternion.Euler(rayAngle, 0, 0) * new Vector3(0, 0, -1);

        Ray ray = new Ray(torso.position, player.TransformDirection(rayDirection));

        if (Physics.Raycast(ray, out rayHit, maxDist, ~playerMask))
        {
            if (rayHit.distance > minDist)
            {
                transform.position = rayHit.point;
            }
            else
            {
                transform.position = torso.position + player.TransformDirection(rayDirection) * minDist;
            }
        }
        else
        {
            transform.position = torso.position + player.TransformDirection(rayDirection) * maxDist;
        }

        transform.LookAt(torso.position + player.TransformDirection(0, targetYOffset, 0));

        // DEBUG
        Debug.DrawRay(torso.position, player.TransformDirection(rayDirection) * maxDist, Color.yellow);
    }
}
