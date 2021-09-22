using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    public Transform upperLeg;
    public Transform lowerLeg;
    public Transform tipLeg;
    public float safeRadius;
    public float stepHeight;
    public float stepSpeed;
    public LayerMask validFootPos;

    // Foot position variables
    public bool stepNeeded;
    Vector3 oldPosition;
    Vector3 currentPosition;
    Vector3 newPosition;
    public Vector3 newValidPosition;
    public bool newPosAvailable;
    Vector3 idealPositionOffset;
    public Vector3 idealPosition;
    public float idealPosDistance;
    Vector3 idlePositionOffset;
    Vector3 idlePosition;
    bool legIdle;
    Quaternion initialTorsoRotation;

    // Animation variables
    float lerp;
    public bool stepAllowed;
    public bool makingStep;

    // Raycast variables
    Vector3[] rayOrigin;
    Vector3[] rayDirection;

    // Start is called before the first frame update
    void Start()
    {
        stepNeeded = false;
        oldPosition = transform.position;
        currentPosition = transform.position;
        newPosition = transform.position;
        newValidPosition = transform.position;
        newPosAvailable = false;

        lerp = 1;
        stepAllowed = false;
        makingStep = false;

        //idealPositionOffset = transform.position - torso.position;
        idealPosDistance = 0;
        idlePositionOffset = idealPositionOffset / 2;
        legIdle = false;

        //initialTorsoRotation = torso.rotation;

        rayDirection = new Vector3[1];
        rayDirection[0] = new Vector3(0, 0, 0);
        //rayDirection[1] = new Vector3(0, 0, 0);
        //rayDirection[2] = new Vector3(0, 0, 0);

        rayOrigin = new Vector3[rayDirection.Length];
    }

    // Every calculated Position needs to be rotated when the torso rotates.
    // Update is called once per frame
    void Update()
    {
        // Leave foot on current position
        //transform.position = currentPosition;

    }

    private void OnDrawGizmos()
    {
        // new Pos = red, safe area = green, ideal position = yellow
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(newValidPosition, 0.1f);

        // Draw all raycasts
        if (rayDirection != null)
        {
            for (int i = 0; i < rayDirection.Length; i++)
            {
                Debug.DrawRay(rayOrigin[i], rayDirection[i].normalized * 2 * safeRadius, Color.red);
                Gizmos.DrawSphere(rayOrigin[i], 0.1f);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(idealPosition, safeRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(idealPosition, 0.1f);
    }

    public void updateNewValidPosition(Vector3 movementDirection)
    {
        // Raycast to detect if new ideal position is available
        // Multiple raycasts will be made and the closest one will be chosen
        // All Raycasts are offset by 0.75*safeRadius in movement direction to step ahead

        Vector3 movementDirectionOffset = movementDirection.normalized * 0.75f * safeRadius;

        Ray[] ray = new Ray[rayDirection.Length];
        RaycastHit[] info = new RaycastHit[rayDirection.Length];
        bool[] rayCastHitGround = new bool[rayDirection.Length];
        int closestIndex = -1;

        newPosAvailable = false;

        //rayDirection[0] = torso.up;
        //rayDirection[1] = torso.right;
        //rayDirection[2] = torso.forward;

        //TODO
        //rayDirection[0] = -torso.forward;

        for (int i = 0; i < rayDirection.Length; i++)
        {
            // Update Ray direction
            // from torso through idealpos, two perpendicular to it
            //TODO

            // Calculate rayOrigin based on rayDirection and safeRadius
            //rayOrigin[i] = applyTorsoRotation(torso.position + idealPositionOffset + movementDirectionOffset) + -safeRadius * rayDirection[i];

            ray[i] = new Ray(rayOrigin[i], rayDirection[i]);
            rayCastHitGround[i] = Physics.Raycast(ray[i], out info[i], 2 * safeRadius, validFootPos);
            newPosAvailable |= rayCastHitGround[i];

            // Get raycast with smallest distance to origin
            if (rayCastHitGround[i] && (closestIndex == -1 || info[i].distance < info[closestIndex].distance))
            {
                closestIndex = i;
            }
        }

        // Update the newValidPosition if available
        if (newPosAvailable)
        {
            newValidPosition = info[closestIndex].point;
        }
    }

    public void updateIdleIdeal()
    {
        //idlePosition = applyTorsoRotation(torso.position + idlePositionOffset);
        //idealPosition = applyTorsoRotation(torso.position + idealPositionOffset);
    }

    public void updateStepNeeded()
    {
        idealPosDistance = Vector3.Distance(idealPosition, currentPosition);
        stepNeeded = !makingStep && (legIdle || idealPosDistance > safeRadius);
    }

    public void updateLegPositions()
    {
        if (newPosAvailable)
        {
            // Set new target position for foot
            legIdle = false;
            newPosition = newValidPosition;
        }
        else
        {
            // Set idle position for foot
            //legIdle = true;
            newPosition = idlePosition;
        }
    }

    public void startFootAnimation()
    {
        // Start animation variables
        if (lerp >= 1 && (!makingStep && !legIdle))
        {
            lerp = 0;
            oldPosition = currentPosition;
            makingStep = true;
            stepAllowed = false;
            stepNeeded = false;
        }
    }

    public void animateFoot()
    {
        // if leg already in idle position update transform to stay near torso
        if (!makingStep && legIdle)
        {
            transform.position = idlePosition;
        }

        // Animate foot frane by frame if allowed and new position determined
        if (lerp < 1)
        {
            Vector3 footPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            footPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = footPosition;
            lerp += Time.deltaTime * stepSpeed;
        }
        else
        {
            if (newPosition == idlePosition)
            {
                legIdle = true;
            }
            oldPosition = newPosition;
            makingStep = false;
        }

        transform.position = currentPosition;
    }

    // Rotate point around the current torso rotation and then around
    // the inverse initial rotation to get the difference.
    private Vector3 applyTorsoRotation(Vector3 point)
    {
        //point = rotateAroundPivot(point, torso.position, Quaternion.Inverse(initialTorsoRotation));
        //point = rotateAroundPivot(point, torso.position, torso.rotation);
        return point;
    }

    private Vector3 applyTorsoRotationAroundIdealPos(Vector3 point)
    {
        point = rotateAroundPivot(point, idealPosition, Quaternion.Inverse(initialTorsoRotation));
        //point = rotateAroundPivot(point, idealPosition, torso.rotation);
        return point;
    }

    // Rotate a point around a pivot using a Quaternion
    private Vector3 rotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion angles)
    {
        Vector3 dir = point - pivot;
        dir = angles * dir;
        point = dir + pivot;
        return point;
    }
}
