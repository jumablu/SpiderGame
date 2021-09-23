using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRagdollMovement : MonoBehaviour
{
    // ** public variables **
    public float movementSpeed;
    public float rotationSpeed;
    [Space(20)]
    public float maxFootDist;
    public float animationSpeed;
    public LayerMask validFootPos;
    [Space(20)]
    public Transform targetController;
    public Transform IKTargetBackL;
    public Transform IKTargetBackR;
    public Transform IKTargetFrontL;
    public Transform IKTargetFrontR;
    public Transform IKTargetMiddleL;
    public Transform IKTargetMiddleR;
    [Space(20)]
    public Transform torso;

    // ** private variables **
    Transform[] target;
    Vector3[] idealPosOffset;
    Vector3[] idealPos;
    bool[] newPosAvailable;
    Vector3[] newValidPos;
    Vector3[] currentPos;

    Vector3[] rayDirection;
    Vector3[] rayOrigin;
    RaycastHit[] rayHit;

    // Start is called before the first frame update
    void Start()
    {
        // ** Initialize variables **
        // Assign targets
        target = new Transform[6];

        target[0] = IKTargetBackL;
        target[1] = IKTargetBackR;
        target[2] = IKTargetFrontL;
        target[3] = IKTargetFrontR;
        target[4] = IKTargetMiddleL;
        target[5] = IKTargetMiddleR;

        // Calculate idealPosOffset from IkController position
        idealPosOffset = new Vector3[6];
        currentPos = new Vector3[target.Length];

        for (int i = 0; i < target.Length; i++)
        {
            idealPosOffset[i] = target[i].position - targetController.position;
            currentPos[i] = target[i].position;
        }

        // rest
        idealPos = new Vector3[target.Length];
        newPosAvailable = new bool[target.Length];
        newValidPos = new Vector3[target.Length];
        rayDirection = new Vector3[target.Length];
        rayOrigin = new Vector3[target.Length];
        rayHit = new RaycastHit[target.Length];
    }

    // Update is called once per frame
    void Update()
    {
        // ** Update newValidPos and determine which feet want to move **
        for (int i = 0; i < target.Length; i++)
        {
            // Update idealPos
            idealPos[i] = targetController.position + idealPosOffset[i];

            // Raycasts to detect new Position
            rayDirection[i] = targetController.TransformDirection(0, -1, 0);
            rayOrigin[i] = idealPos[i] - maxFootDist * rayDirection[i];

            Ray ray = new Ray(rayOrigin[i], rayDirection[i]);
            newPosAvailable[i] = Physics.Raycast(ray, out rayHit[i], 2 * maxFootDist, validFootPos);

            if (newPosAvailable[i])
            {
                newValidPos[i] = rayHit[i].point;
            }




        }

        // ** determine which feet can move **
        // ** calculate currentpos for animation **
        // ** set currentpos for feet **
    }

    private void OnDrawGizmos()
    {
        if (idealPosOffset != null)
        {
            for (int i = 0; i < target.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(idealPos[i], 0.01f);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(newValidPos[i], 0.1f);
                Debug.DrawRay(rayOrigin[i], rayDirection[i] * 2 * maxFootDist, Color.red);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(idealPos[i], maxFootDist);
            }
        }
    }
}
