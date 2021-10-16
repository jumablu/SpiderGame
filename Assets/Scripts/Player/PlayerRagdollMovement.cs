using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * This class is attatched to a spider ragdoll mesh with rig.
 * It is used to controll the movement of the foot targets linkes via a 
 * fixed joint in order to move the player.
 * 
 * TODO:
 * - Move and rotate the targetController according to ground.
 * - Handle Legs in air / without newValidPos.
 * - handle multiple raycasts.
 * 
 * Current workarounds:
 * -variable float[] idealPosDist is made ststic for sorting. This could 
 * lead to issues if there is more than one GameObject with the 
 * PlayerRagdollMovement script attatched.
 * */
public class PlayerRagdollMovement : MonoBehaviour
{
    // ** public variables **
    public float movementSpeed;
    public float rotationSpeed;
    [Space(20)]
    public float maxFootDist;
    public float animationSpeed;
    public float stepHeight;
    public float stepTilt;
    public int maxStepAllowed;
    public LayerMask validFootPos;
    public LayerMask playerBody;
    [Space(20)]
    public Transform targetController;
    public Transform IKTargetBackL;
    public Transform IKTargetBackR;
    public Transform IKTargetFrontL;
    public Transform IKTargetFrontR;
    public Transform IKTargetMiddleL;
    public Transform IKTargetMiddleR;
    [Space(20)]
    public Transform head;
    public Transform torso;
    public Transform tailEnd;

    // ** private variables **
    // foot position & calculation
    Transform[] target;
    Vector3[] idealPosOffset;
    Vector3[] idealPos;
    static float[] idealPosDist;
    bool[] newPosAvailable;
    Vector3[] newValidPos;
    Vector3[] currentPos;

    // foot raycast variables
    Vector3[] rayDirection;
    Vector3[] rayOrigin;
    RaycastHit[] rayHit;

    // Controller movement raycast variables
    Ray[] moveYRay;
    RaycastHit[] moveYRayHit;
    Vector3[] moveYRayDir;

    // animation variables
    bool[] footMoving;
    bool[] footIdle;
    float[] lerp;
    Vector3[] startPos;
    Vector3[] endPos;
    Vector3[] newPos;
    Vector3[] oldPos;
    Vector3[] circleCenter;
    float[] circleRadius;

    // -- DEBUG VARIABLES --
    // -- DEBUG VARIABLES END --

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

        // foot position & calculation rest
        idealPos = new Vector3[target.Length];
        idealPosDist = new float[target.Length];
        newPosAvailable = new bool[target.Length];
        newValidPos = new Vector3[target.Length];

        // raycast variables
        rayDirection = new Vector3[target.Length];
        rayOrigin = new Vector3[target.Length];
        rayHit = new RaycastHit[target.Length];

        // Controller movement raycast variables
        moveYRay = new Ray[3];
        moveYRayHit = new RaycastHit[3];
        moveYRayDir = new Vector3[3];

        // animation variables
        footMoving = new bool[target.Length];
        footIdle = new bool[target.Length];
        lerp = new float[target.Length];
        startPos = new Vector3[target.Length];
        endPos = new Vector3[target.Length];
        newPos = new Vector3[target.Length];
        oldPos = new Vector3[target.Length];
        circleCenter = new Vector3[target.Length];
        circleRadius = new float[target.Length];

        // -- DEBUG INIT --
        // -- DEBUG INIT END --
    }

    // Update is called once per frame
    void Update()
    {
        // ---------- Handle Player Input and movement ----------
        // ** Movement **
        // get X Z plane movement
        float moveX = Input.GetAxis("Horizontal") * movementSpeed;
        float moveZ = Input.GetAxis("Vertical") * movementSpeed;

        // get Y movement
        float moveY = 0;
        // TODO

        // Apply transforms
        Vector3 movement = new Vector3(moveX, moveY, moveZ) * Time.deltaTime;
        targetController.Translate(movement);

        // ** Rotation **
        // Y rotation via mouse input
        float rotateY = Input.GetAxis("Mouse X") * rotationSpeed * 100 * Time.deltaTime;

        // X and Z rotation from average normal vector of raycasts
        Vector3 avgNormal = new Vector3(0, 0, 0);
        float rotateX = 0;
        float rotateZ = 0;
        // TODO

        // Apply rotation
        Vector3 rotate = new Vector3(rotateX, rotateY, rotateZ);
        targetController.Rotate(rotate);



        // ---------- Calculate leg positions / animations ----------
        // ** Update newValidPos, determine which feet need to move and count feet currently moving **
        List<int> stepNeededIndex = new List<int>();
        int feetCurrentlyMoving = 0;

        for (int i = 0; i < target.Length; i++)
        {
            // Update idealPos
            idealPos[i] = targetController.position + targetController.TransformDirection(idealPosOffset[i]);

            // Update if foot needs to step
            idealPosDist[i] = Vector3.Distance(currentPos[i], idealPos[i]);
            if (idealPosDist[i] > maxFootDist && !footMoving[i])
            {
                stepNeededIndex.Add(i);
            }

            // Count the number of feet currently moving
            if (footMoving[i])
            {
                feetCurrentlyMoving++;
            }
        }

        // ** determine which feet can move **
        List<int> stepAllowedIndex = new List<int>();
        if (stepNeededIndex.Count > maxStepAllowed - feetCurrentlyMoving)
        {
            // First move feet that are furthest from their idealPos -> Sort by distance descending
            stepNeededIndex.Sort(SortIndexByDistance);
            stepNeededIndex.Reverse();

            for (int i = 0; i < maxStepAllowed - feetCurrentlyMoving; i++)
            {
                stepAllowedIndex.Add(stepNeededIndex[i]);
            }
        }
        else
        {
            stepAllowedIndex = stepNeededIndex;
        }

        // ** Initialize new foot movement **
        foreach (int i in stepAllowedIndex)
        {
            // Animation variables
            footMoving[i] = true;
            lerp[i] = 0;

            // Calculate starting position on Sphere (maxfootDist in direction of currentpos)
            startPos[i] = idealPos[i] + (currentPos[i] - idealPos[i]).normalized * maxFootDist;
            //currentPos[i] = startPos[i]; //?

            // Calculate ideal end position on Sphere (maxfootDist in movement direction)
            endPos[i] = idealPos[i] + targetController.TransformDirection(movement.x, 0, movement.z).normalized * maxFootDist;

            Debug.Log(Vector3.Distance(endPos[i], idealPos[i]));

            newPos[i] = startPos[i];
            oldPos[i] = startPos[i];

            // Get the circles center and radius: Plane through start & endPos with the normal facing to idealPos
            Vector3 normal = Vector3.Cross(startPos[i] - endPos[i], targetController.TransformDirection(0, 1, 0));
            Plane plane = new Plane(normal, startPos[i]);

            circleCenter[i] = plane.ClosestPointOnPlane(idealPos[i]);
            circleRadius[i] = (circleCenter[i] - startPos[i]).magnitude;
        }

        // ** Update currentPos and move feet to currentPos **
        for (int i = 0; i < target.Length; i++)
        {
            if (footMoving[i])
            {
                // Calculate newPos for next frame: sin in up direction, cos in movement direction
                newPos[i] = startPos[i];

                newPos[i] += stepHeight * Mathf.Sin(lerp[i] * 2 * Mathf.PI) * targetController.TransformDirection(0, 1, 0);
                newPos[i] += circleRadius[i] * Mathf.Cos(lerp[i] * 2 * Mathf.PI) * (startPos[i] - endPos[i]).normalized;

                // Raycast from oldPos to newPos to detect if foot hit a valid position
                Ray ray = new Ray(oldPos[i], newPos[i] - oldPos[i]);
                RaycastHit info = new RaycastHit();


                if (Physics.Raycast(ray, out info, (newPos[i] - oldPos[i]).magnitude, validFootPos))
                {
                    currentPos[i] = info.point;
                    footMoving[i] = false;
                }
                else
                {
                    lerp[i] += Time.deltaTime * animationSpeed;
                    oldPos[i] = newPos[i];
                    currentPos[i] = newPos[i];
                }

                if (lerp[i] >= 1)
                {
                    footMoving[i] = false;
                }
            }

            target[i].position = currentPos[i];
        }

        // -- DEBUGGING --

        // -- DEBUGGING END ---
    }

    private void OnDrawGizmos()
    {
        if (idealPosOffset != null && idealPosOffset.Length != 0)
        {
            for (int i = 0; i < target.Length; i++)
            {
                // idealPos
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(idealPos[i], 0.01f);

                // leg Raycast
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(newValidPos[i], 0.1f);
                Gizmos.DrawSphere(rayOrigin[i], 0.02f);
                Debug.DrawRay(rayOrigin[i], rayDirection[i] * 2 * maxFootDist, Color.red);

                // leg validpos
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(idealPos[i], maxFootDist);

                // circle
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(circleCenter[i], circleRadius[i]);

                // Debug
                Gizmos.DrawSphere(startPos[i], 0.01f);
                Gizmos.DrawSphere(endPos[i], 0.01f);
            }

            // targetController raycasts
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(head.position, 0.01f);
            Debug.DrawRay(head.position, moveYRayDir[0].normalized * 2 * maxFootDist, Color.magenta);
            Gizmos.DrawSphere(torso.position, 0.01f);
            Debug.DrawRay(torso.position, moveYRayDir[1].normalized * 2 * maxFootDist, Color.magenta);
            Gizmos.DrawSphere(tailEnd.position, 0.01f);
            Debug.DrawRay(tailEnd.position, moveYRayDir[2].normalized * 2 * maxFootDist, Color.magenta);

            for (int i = 0; i < moveYRay.Length; i++)
            {
                Gizmos.DrawWireSphere(moveYRayHit[i].point, 0.1f);
            }
        }
    }

    static int SortIndexByDistance(int i1, int i2)
    {
        return idealPosDist[i1].CompareTo(idealPosDist[i2]);
    }
}
