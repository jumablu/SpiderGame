using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    [Space(20)]
    public int maxStepCount;
    public List<Transform> IKController;
    public Transform torso;
    [Space(20)]
    public LayerMask validFootPos;

    List<IKFootSolver> footSolver;
    Ray[] ray;
    RaycastHit[] rayInfo;
    bool[] raycastHits;
    Vector3 rayOffset;

    // Start is called before the first frame update
    void Start()
    {
        footSolver = new List<IKFootSolver>();
        foreach(Transform controller in IKController)
        {
            IKFootSolver solver = controller.GetComponent<IKFootSolver>();
            footSolver.Add(solver);
        }

        ray = new Ray[3];
        rayInfo = new RaycastHit[ray.Length];
        raycastHits = new bool[ray.Length];
        rayOffset = new Vector3(0, 1, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // <---------- Move Player ---------->
        // X Z plane movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Position Body relative to objects
        updateRaycasts();

        // Ground distance should always be 0 because pivot is on null level
        float moveY = getYMovement();

        Vector3 movement = new Vector3(moveX, moveY, moveZ);
        movement *= movementSpeed * Time.deltaTime;

        transform.Translate(movement);

        // Rotate Player around Y axis
        float rotateY = Input.GetAxis("Mouse X");
        transform.Rotate(0, rotateY * rotationSpeed * 100 * Time.deltaTime, 0);

        // TODO Rotate player relative to objects



        // <---------- Leg logic ---------->

        // Logic for all legs
        List<IKFootSolver> stepsNeeded = new List<IKFootSolver>();
        int legsCurrentlyMoving = 0;

        foreach (IKFootSolver solver in footSolver)
        {
            solver.animateFoot();

            solver.updateNewValidPosition(movement);
            
            solver.updateIdleIdeal();
            solver.updateStepNeeded();

            // Get legs that need to step
            if (solver.stepNeeded)
            {
                stepsNeeded.Add(solver);
            }

            // Get count of legs currently moving
            if (solver.makingStep)
            {
                legsCurrentlyMoving += 1;
            }
        }

        // Order Legs descending based on distance
        stepsNeeded.Sort(SortByDistance);
        stepsNeeded.Reverse();

        // Let up to the max number of legs take a step simultaniously
        // Starting with maximum distance
        for (int i = 0; i < stepsNeeded.Count && i < maxStepCount - legsCurrentlyMoving; i++)
        {
            stepsNeeded[i].updateLegPositions();
            stepsNeeded[i].startFootAnimation();
        }
    }

    private void OnDrawGizmos()
    {
        try
        {
            for (int i = 0; i < ray.Length; i++)
            {
                Debug.DrawRay(ray[i].origin, ray[i].direction.normalized * 1.5f * rayOffset.magnitude, Color.red);
            }
        }
        catch (NullReferenceException) { }
    }

    void updateRaycasts()
    {
        // Positions Player relative to ground
        // three raycasts (front middle and back) f and b are diagonal
        // ALL TODO

        ray[0] = new Ray(transform.position + rayOffset, -transform.up);
        ray[1] = new Ray(rayOffset + transform.TransformPoint(0, 0, 1.5f), -transform.up + transform.forward);
        ray[2] = new Ray(rayOffset + transform.TransformPoint(0, 0, -1), -transform.up - transform.forward);

        for(int i = 0; i < ray.Length; i++)
        {
            raycastHits[i] = Physics.Raycast(ray[i], out rayInfo[i], 1.5f * rayOffset.magnitude, validFootPos);
        }
    }

    float getYMovement()
    {
        // This function should be called after updateRaycasts()
        // Positions player relative to ground using average of the raycasthits
        Vector3 average = new Vector3(0, 0, 0);
        int hitCount = 0;

        for(int i = 0; i < rayInfo.Length; i++)
        {
            if (raycastHits[i])
            {
                average += rayInfo[i].point;
                hitCount++;
            }
        }

        if (hitCount != 0)
        {
            average /= hitCount;
        }

        Debug.Log(-(transform.position - average).y);

        // Get y distance from spiderpos to average
        return -(transform.position - average).y;
    }

    static int SortByDistance (IKFootSolver fs1, IKFootSolver fs2)
    {
        return fs1.idealPosDistance.CompareTo(fs2.idealPosDistance);
    }
}
