using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed;
    [Space(20)]
    public int maxStepCount;
    public List<Transform> IKController;

    List<IKFootSolver> footSolver;

    // Start is called before the first frame update
    void Start()
    {
        footSolver = new List<IKFootSolver>();
        foreach(Transform controller in IKController)
        {
            IKFootSolver solver = controller.GetComponent<IKFootSolver>();
            footSolver.Add(solver);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // <---------- Move Player ---------->
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveX, 0, moveZ);
        movement.Normalize();
        movement *= movementSpeed * Time.deltaTime;

        transform.Translate(movement);


        // <---------- Leg logic ---------->

        // Get List of legs that need to step and sort it descending
        // Get number of legs currently moving
        List<IKFootSolver> stepsNeeded = new List<IKFootSolver>();
        int legsCurrentlyMoving = 0;

        foreach (IKFootSolver solver in footSolver)
        {
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

        stepsNeeded.Sort(SortByDistance);
        stepsNeeded.Reverse();

        // Let up to the max number of legs take a step simultaniously
        for (int i = 0; i < stepsNeeded.Count && i < maxStepCount - legsCurrentlyMoving; i++)
        {
            stepsNeeded[i].stepAllowed = true;
        }
    }

    static int SortByDistance (IKFootSolver fs1, IKFootSolver fs2)
    {
        return fs1.idealPosDistance.CompareTo(fs2.idealPosDistance);
    }
}
