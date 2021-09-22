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
    public float moveDuration;
    [Space(20)]
    [Header("Controllers in order front to back with L R alternating")]
    public Transform[] IKController = new Transform[6];
    public Transform torso;
    [Space(20)]
    public LayerMask validFootPos;

    IKFootSolver[] footSolver;
    bool falling;

    // Start is called before the first frame update
    void Start()
    {
        footSolver = new IKFootSolver[6];
        for (int i = 0; i < IKController.Length; i++)
        {
            // Get footSolver components
            footSolver[i] = IKController[i].GetComponent<IKFootSolver>();

            // Set IK target position to default
            IKController[i].position = footSolver[i].tipLeg.position;
        }

        falling = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Update if player falling
        if (!falling)
        {
            // Check if player in RigidBody mode
            int legIdleCount = 0;

            for (int i = 0; i < footSolver.Length; i++)
            {
                if (!footSolver[i].newPosAvailable)
                {
                    legIdleCount++;
                }
            }

            if (legIdleCount > 4)
            {
                // TODO: enable Rigidbody, disable player movement, set legs idle, !check if on ground again!
                falling = true;
            }
        }
        
        // If not falling movement logic
        if (!falling)
        {

        }









        // OLD--------------------------------------------------------------------------------------------------
        // <---------- Move Player ---------->
        // X Z plane movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Position Body relative to objects
        // Get the difference of each idealPos to newPos (if available else 0) and average them to a rotation
        

        // Ground distance should always be 0 because pivot is on null level
        float moveY = getYMovement();

        Vector3 movement = new Vector3(moveX, moveY, moveZ);
        movement *= movementSpeed * Time.deltaTime;

        transform.Translate(movement);

        // Get X and Z rotation from legs raycasts, Y rotation from mouse
        Vector3 rotatePlayer = getPlayerRotation();

        transform.Rotate(rotatePlayer);

        // TODO Rotate player relative to objects



        // <---------- Leg logic ---------->

        // Logic for all legs
        List<IKFootSolver> stepsNeeded = new List<IKFootSolver>();
        int legsCurrentlyMoving = 0;

        foreach (IKFootSolver solver in footSolver)
        {
            solver.animateFoot();

            // Pass X and Z direction movement to footsolver for raycast offset in step direction
            solver.updateNewValidPosition(new Vector3(movement.x, 0, movement.z));
            
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
        
    }

    Vector3 getPlayerRotation()
    {
        // This Function rotates the player using the footpositions
        // Correct IKFootSolver order is assumed (see Tooltip)

        // Player mouse rotation
        float rotateY = Input.GetAxis("Mouse X") * rotationSpeed * 150 * Time.deltaTime;

        // X and Z rotation based on average normal vector of validNewPos


        Vector3 playerRotation = new Vector3(0, rotateY, 0);
        return playerRotation;
    }

    float getYMovement()
    {
        float avg = 0;

        //foreach (float dist in idealPosDist)
        {
            
            //avg += dist;
        }

        avg /= 6;
        return avg;
    }

    static int SortByDistance (IKFootSolver fs1, IKFootSolver fs2)
    {
        return fs1.idealPosDistance.CompareTo(fs2.idealPosDistance);
    }
}
