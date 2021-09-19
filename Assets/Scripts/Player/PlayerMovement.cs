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
    [Tooltip("Controllers in order front to back with L R ")]
    public List<Transform> IKController;
    public Transform torso;
    [Space(20)]
    public LayerMask validFootPos;

    List<IKFootSolver> footSolver;
    float[] idealPosDist;

    // Start is called before the first frame update
    void Start()
    {
        footSolver = new List<IKFootSolver>();
        foreach(Transform controller in IKController)
        {
            IKFootSolver solver = controller.GetComponent<IKFootSolver>();
            footSolver.Add(solver);
        }

        idealPosDist = new float[6];
    }

    // Update is called once per frame
    void Update()
    {
        // <---------- Move Player ---------->
        // X Z plane movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Position Body relative to objects
        // Get the difference of each idealPos to newPos (if available else 0) and average them to a rotation
        idealPosDist = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (footSolver[i].newPosAvailable)
            {
                idealPosDist[i] = Vector3.Distance(footSolver[i].idealPosition, footSolver[i].newValidPosition);
                
                // If newPos below ideal pos (greater distance from torso) it should be negative
                if (Vector3.Distance(footSolver[i].newValidPosition, torso.position) > Vector3.Distance(footSolver[i].idealPosition, torso.position))
                {
                    idealPosDist[i] *= -1;
                }
            }
            else
            {
                idealPosDist[i] = 0;
            }
        }

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
        
    }

    Vector3 getPlayerRotation()
    {
        // This Function rotates the player using the footpositions
        // Correct IKFootSolver order is assumed (see Tooltip)

        // Player mouse rotation
        float rotateY = Input.GetAxis("Mouse X") * rotationSpeed * 150 * Time.deltaTime;

        // Z Rotation (average L and R and get difference)
        float avgL = 0;
        float avgR = 0;

        for (int i = 0; i < 6; i++)
        {
            if (i % 2 == 0)
            {
                avgL += idealPosDist[i];
            }
            else
            {
                avgR += idealPosDist[i];
            }
        }

        avgL /= 3;
        avgR /= 3;

        Debug.Log("LR " + (avgL - avgR));

        // X Rotation (average front and back and get difference)
        float avgF = (idealPosDist[0] + idealPosDist[1]) / 2;
        float avgB = (idealPosDist[4] + idealPosDist[5]) / 2;

        Debug.Log("FB " + (avgF - avgB));


        Vector3 playerRotation = new Vector3(avgB - avgF, rotateY, avgR - avgL);
        return playerRotation;
    }

    float getYMovement()
    {
        float avg = 0;

        foreach (float dist in idealPosDist)
        {
            
            avg += dist;
        }

        avg /= 6;
        return avg;
    }

    static int SortByDistance (IKFootSolver fs1, IKFootSolver fs2)
    {
        return fs1.idealPosDistance.CompareTo(fs2.idealPosDistance);
    }
}
