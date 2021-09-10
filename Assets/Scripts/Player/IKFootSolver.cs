using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    public Transform spider;
    public Transform root;
    public Transform torso;
    public Transform upperLeg;
    public Transform lowerLeg;
    public float safeRadius;
    public float stepHeight;
    public float stepSpeed;
    public LayerMask validFootPos;

    // Foot position variables
    public bool stepNeeded;
    Vector3 oldPosition;
    Vector3 currentPosition;
    Vector3 newPosition;
    Vector3 newValidPosition;
    bool newPosAvailable;
    Vector3 idealPositionOffset;
    Vector3 idealPosition;
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
    Vector3 rayOffset;
    Vector3 rayOrigin;
    Vector3 rayDirection;

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

        idealPositionOffset = transform.position - torso.position;
        idealPosDistance = 0;
        idlePositionOffset = idealPositionOffset / 2;
        legIdle = false;

        initialTorsoRotation = torso.rotation;

        rayOffset = new Vector3(0, safeRadius, 0);
    }

    // Every calculated Position needs to be rotated when the torso rotates.
    // Update is called once per frame
    void Update()
    {
        // Leave foot on current position
        transform.position = currentPosition;

        updateNewvalidPosition();

        // Move leg if current pos leaves safe area
        // safe area = max distance from idealPos
        idlePosition = applyTorsoRotation(torso.position + idlePositionOffset);
        idealPosition = applyTorsoRotation(torso.position + idealPositionOffset);
        idealPosDistance = Vector3.Distance(idealPosition, currentPosition);
        stepNeeded = !makingStep && (legIdle || idealPosDistance > safeRadius);
        if (stepNeeded && stepAllowed)
        {
            updateLegPositions();
            startFootAnimation();
        }

        animateFoot();
    }

    private void OnDrawGizmos()
    {
        // new Pos = red, safe area = green, ideal position = yellow
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(newValidPosition, 0.1f);
        Debug.DrawRay(rayOrigin, rayDirection.normalized * 2 * safeRadius, Color.red);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(idealPosition, safeRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(idealPosition, 0.1f);
    }

    private void updateNewvalidPosition()
    {
        // Raycast to detect if new ideal position is available
        rayOrigin = applyTorsoRotation(torso.position + idealPositionOffset + rayOffset);
        rayDirection = torso.TransformDirection(0, 0, -1);
        rayOffset = new Vector3(0, safeRadius, 0);
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit info;
        newPosAvailable = Physics.Raycast(ray, out info, 2 * safeRadius, validFootPos);

        if (newPosAvailable)
        {
            newValidPosition = info.point;
        }
    }

    private void updateLegPositions()
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

    private void startFootAnimation()
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

    private void animateFoot()
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
    }

    // Rotate point around the current torso rotation and then around
    // the inverse initial rotation to get the difference.
    private Vector3 applyTorsoRotation(Vector3 point)
    {
        point = rotateAroundPivot(point, torso.position, Quaternion.Inverse(initialTorsoRotation));
        point = rotateAroundPivot(point, torso.position, torso.rotation);
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
