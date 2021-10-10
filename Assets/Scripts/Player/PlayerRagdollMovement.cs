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
    Vector3 moveYRayDir;

    // animation variables
    bool[] footMoving;
    float[] lerp;
    Vector3[] oldPos;
    Vector3[] newPos;

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
        moveYRayDir = new Vector3();

        // animation variables
        footMoving = new bool[target.Length];
        lerp = new float[target.Length];
        oldPos = new Vector3[target.Length];
        newPos = new Vector3[target.Length];

        // -- DEBUG INIT --
        // -- DEBUG INIT END --
    }

    // Update is called once per frame
    void Update()
    {
        // ---------- Calculate leg positions / animations ----------
        // ** Update newValidPos, determine which feet need to move and count feet currently moving **
        List<int> stepNeededIndex = new List<int>();
        int feetCurrentlyMoving = 0;

        for (int i = 0; i < target.Length; i++)
        {
            // Update idealPos
            idealPos[i] = targetController.position + targetController.TransformDirection(idealPosOffset[i]);

            // Raycasts to detect new Position
            rayDirection[i] = targetController.TransformDirection(0, -1, 0);

            rayOrigin[i] = idealPos[i] - maxFootDist * rayDirection[i];

            Ray ray = new Ray(rayOrigin[i], rayDirection[i]);
            newPosAvailable[i] = Physics.Raycast(ray, out rayHit[i], 2 * maxFootDist, validFootPos);

            if (newPosAvailable[i])
            {
                newValidPos[i] = rayHit[i].point;
            }

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

        // ** Start foot movement (animation or ragdoll) **
        foreach (int i in stepAllowedIndex)
        {
            // Start animation if newPosAvailable
            if (newPosAvailable[i])
            {
                // set oldPos, newPos, lerp (linear interpolation), and the state of moving
                oldPos[i] = currentPos[i];
                newPos[i] = newValidPos[i];
                lerp[i] = 0;
                footMoving[i] = true;
            }
        }

        // ** Update currentPos and move feet to currentPos **
        for (int i = 0; i < target.Length; i++)
        {
            if (footMoving[i])
            {
                // Update currentPos
                currentPos[i] = Vector3.Lerp(oldPos[i], newPos[i], lerp[i]);
                currentPos[i] += targetController.TransformDirection(new Vector3(0, Mathf.Sin(lerp[i] * Mathf.PI) * stepHeight, 0));

                // increment lerp
                lerp[i] += Time.deltaTime * animationSpeed;

                // Check if step ended
                if (lerp[i] >= 1)
                {
                    footMoving[i] = false;
                }
            }

            target[i].position = currentPos[i];
        }


        // ---------- Move & rotate IK Controller / player ----------
        // ** Movement **
        // get X Z plane movement
        float moveX = Input.GetAxis("Horizontal") * movementSpeed;
        float moveZ = Input.GetAxis("Vertical") * movementSpeed;

        // get Y movement, avg of raycast from head, torso, tailEnd
        float moveY = 0;
        int rayHitCount = 0;

        moveYRay = new Ray[3];
        moveYRayHit = new RaycastHit[3];

        moveYRayDir = targetController.TransformDirection(0, -1, 0);
        moveYRay[0] = new Ray(head.position, moveYRayDir);
        moveYRay[0] = new Ray(torso.position, moveYRayDir);
        moveYRay[0] = new Ray(tailEnd.position, moveYRayDir);

        for (int i = 0; i < moveYRay.Length; i++)
        {
            if (Physics.Raycast(moveYRay[i], out moveYRayHit[i], 2 * maxFootDist, validFootPos))
            {
                rayHitCount++;
                moveY += moveYRayHit[i].distance;
            }
        }

        if (rayHitCount > 0)
        {
            moveY /= rayHitCount;
            moveY -= maxFootDist;
            moveY -= targetController.position.y;
        }
        else
        {
            moveY = 0;
        }

        // Apply transforms
        Vector3 movement = new Vector3(moveX, moveY, moveZ) * Time.deltaTime;
        targetController.Translate(movement);

        // ** Rotation **
        // Y rotation via mouse input
        float rotateY = Input.GetAxis("Mouse X") * rotationSpeed;

        // X and Z rotation from average normal vector of raycasts
        Vector3 avgNormal = new Vector3(0, 0, 0);
        rayHitCount = 0;

        for (int i = 0; i < target.Length; i++)
        {
            if (newPosAvailable[i])
            {
                rayHitCount++;
                avgNormal += rayHit[i].normal;
            }
        }

        avgNormal /= rayHitCount;

        // Apply rotation
        Vector3 rotate = new Vector3(0, rotateY, 0);
        targetController.Rotate(rotate);

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
            }

            // targetController raycasts
            Gizmos.color = Color.magenta;
            Debug.DrawRay(head.position, moveYRayDir.normalized * 2 * maxFootDist, Color.magenta);
            Debug.DrawRay(torso.position, moveYRayDir.normalized * 2 * maxFootDist, Color.magenta);
            Debug.DrawRay(tailEnd.position, moveYRayDir.normalized * 2 * maxFootDist, Color.magenta);

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
