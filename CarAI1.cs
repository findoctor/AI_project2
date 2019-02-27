using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI1 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        Control control;

        // used for path follow
        float x1;
        float z1;
        float x2;
        float z2;
        int target_index;
        public int pathLen;
        float heading;
        Vector3 currentForward;
        Vector3 nextForward;
        float currentSpeed = 0f;
        List<int> isVisited = new List<int>();
        Vector3 lastPos;
        Vector3 currentPos;

        // from Lin lv
        bool crashed = false;
        int crashCounter = 0;
        bool goalReached = false;

        int firstIndex = 0;
        int nextIndex = 1;


        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject pathPlanner;
        PathFinding pathManager;

        public GameObject[] friends;
        public GameObject[] enemies;

        List<List<Node>> allPaths = new List<List<Node>>();
        List<List<Vector2>> clockwisePaths = new List<List<Vector2>>();
        List<Vector2> path;
        int[] hit = { 0, 0, 0 };
        float backAngel = 0f;

        void OnDrawGizmos()
        {
            /*
            Gizmos.color = Color.red;
            for (int k = 0;k<3; k++) {
                Gizmos.DrawSphere( new Vector3( clockwisePaths[k][1][0], 0, clockwisePaths[k][1][1]), 2f);
            }
            */


            // draw each points along the path

            for (int i = 0; i < clockwisePaths.Count; i++)
            {
                if(i == 0)
                    Gizmos.color = Color.red;
                else if (i == 1)
                    Gizmos.color = Color.green;
                else if (i == 2)
                    Gizmos.color = Color.blue;

                for (int k = 0; k < clockwisePaths[i].Count; k++)
                {
                    //if (k == 0)
                        //Gizmos.color = Color.red;
                    Vector2 n1 = clockwisePaths[i][k];
                  
                    Vector3 p1 = new Vector3(n1[0], 0, n1[1]);

                    Gizmos.DrawSphere(p1, 1f);

                }

            }




        }




        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            pathManager = pathPlanner.GetComponent<PathFinding>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            // Plan your path here

            if(transform.name == "ArmedCar (1)") {
                pathManager.initialize();
                pathManager.createTree();
                pathManager.clockwise2();
            }


            this.allPaths =  pathManager.allPaths;
            this.clockwisePaths = pathManager.clockwisePaths;

            if (transform.name == "ArmedCar")
            {
                path = clockwisePaths[1];
                heading = -90f;
                x1 = transform.position[0];
                z1 = transform.position[2];
                currentForward = new Vector3(0,0,-1);
            }

            if (transform.name == "ArmedCar (1)")
            {
                path = clockwisePaths[0];
                heading = 90f;
                x1 = transform.position[0];
                z1 = transform.position[2];
                currentForward = new Vector3(0, 0, 1);
            }

            if (transform.name == "ArmedCar (2)")
            {
                path = clockwisePaths[2];
                heading = 90f;
                x1 = transform.position[0];
                z1 = transform.position[2];
                currentForward = new Vector3(0, 0, 1);
            }

            drawLine(allPaths);
            drawLine2(clockwisePaths);
            //Debug.Log(string.Format("The length of 3 final path:{0}, {1}, {2}", clockwisePaths[0].Count, clockwisePaths[1].Count, clockwisePaths[2].Count));

            //Debug.Log(allPaths[0].Count);
            //Debug.Log(allPaths[1].Count);
            //Debug.Log(allPaths[2].Count);


            //Debug.Log(newMap[newRobotsPosition[0], newRobotsPosition[1]]);


            //currentForward = transform.forward;
            nextForward = transform.forward;
            control = new Control();
            for (int i = 0; i < path.Count; i++)
                isVisited.Add(0);

            this.crashed = false;
            StartCoroutine(DidWeCrash());


        }




        private void FixedUpdate()
        {
            float newSteer = 0f;

            Debug.Log(transform.name + " , path length is "+ path.Count);

            Debug.Log(transform.name + " , next index is " +  nextIndex);
            // Execute your path here
            // ...

            x2 = path[nextIndex][0];
            z2 = path[nextIndex][1];
            float acc = 2f;

            float current_angel = Vector3.Angle(currentForward, new Vector3(1, 0, 0));  // should not update transform!! ??? 
            List<float> res = control.getSteeringAngel(x1, z1, x2, z2, current_angel, currentSpeed);
            float steeringAngel = res[0] * 180f / (float)Math.PI;
            float HeadingChange = res[1];


            //decide turning direction: Vector3.SignedAngle
            float heading = Vector3.Angle(currentForward, new Vector3(-1, 0, 0));
            if (transform.forward[2] < 0f) heading = -heading;
            float pdirection = calculate_angel(x1, z1, x2, z2);
            float flag_turn = turnClockwise(heading, pdirection);

            if (flag_turn == 1f)
                steeringAngel = Math.Abs(steeringAngel);
            else
                steeringAngel = -Math.Abs(steeringAngel);

            //if (Math.Abs(steeringAngel) > 15f && currentSpeed > 3f && hit[0]+hit[1]+hit[2] > 10)
            //acc = -50;
            /* 
             * if maxSpeed == 15, acc = -1, 3 cars remained, cant make sharp turn to hit next target.
             * 
             * 
             */

            if (m_Car.CurrentSpeed > 15f)  
                acc = 0f;

            if (steeringAngel > 25)  //set brake to -1 when sharp turn
            {
                newSteer = 25;

                if (m_Car.CurrentSpeed > 3f)
                    acc = -10f;   // from -1
            }

            else if (steeringAngel < -25)
            {
                newSteer = -25;

                if (m_Car.CurrentSpeed > 3f)
                    acc = -10f;
            }

            else if (steeringAngel > 15f)
            {
                newSteer = 25f;
                if (m_Car.CurrentSpeed > 3f)
                    acc = -1;
            }
            else if (steeringAngel < -15f)
            {
                newSteer = -25f;
                if (m_Car.CurrentSpeed > 3f)
                    acc = -1;
            }

            else
                newSteer = steeringAngel;

            if (m_Car.CurrentSpeed < 2f) {
                acc = 1f;
            }


           
      
            float updatedHeading = Vector3.Angle(transform.forward, new Vector3(-1, 0, 0));
            if (transform.forward[2] < 0f) updatedHeading = -updatedHeading;
            float updatedPdirection = calculate_angel(transform.position[0], transform.position[2], x2, z2);
            //Debug.Log("updated heading:" + updatedHeading);
            //Debug.Log("updated pdirection:" + updatedPdirection);
            if (Math.Abs(updatedHeading - updatedPdirection) < 5f)   // changed from 5.
                newSteer = 0f;


            //Debug.Log("Steering angel:" + steeringAngel);
            //Debug.Log("Accleration:" + acc);

            // when to stop
            if(transform.name == "ArmedCar") { 
                if(crashed && hit[1] > 5) {
                    Debug.Log("!!!!!##################!!!!!!!!!!crashed!!!!!!!!#################!!!!!!");
                    if (backAngel == 0f)
                        backAngel = newSteer;
                    m_Car.Move(-backAngel, 0, -1f, 0);

                    currentForward = transform.forward;
                    currentSpeed = m_Car.CurrentSpeed;
                    x1 = transform.position[0];
                    z1 = transform.position[2];
                }
                else
                {
                    backAngel = 0f;
                    if (hit[1] == path.Count - 1)
                        m_Car.Move(0, 0, 1f, 1f);
                    else
                        m_Car.Move(newSteer / 25, acc, 1f, 0f);
                }
            }
            else if(transform.name == "ArmedCar (1)")
            {
                if (crashed) {
                    Debug.Log("!!!!!##################!!!!!!!!!!crashed!!!!!!!!#################!!!!!!");

                    m_Car.Move(-newSteer, 0, -1f, 0);


                    currentForward = transform.forward;
                    currentSpeed = m_Car.CurrentSpeed;
                    x1 = transform.position[0];
                    z1 = transform.position[2];
                }
                else
                {
                    if (hit[0] == path.Count - 1)
                        m_Car.Move(0, 0, 1f, 1f);
                    else
                        m_Car.Move(newSteer / 25, acc, 1f, 0f);
                }
            }

            else if (transform.name == "ArmedCar (2)")
            {
                if (crashed)
                {
                    Debug.Log("!!!!!##################!!!!!!!!!!crashed!!!!!!!!#################!!!!!!");
                    m_Car.Move(-newSteer, 0, -1f, 0);  // second changed from 0

                    currentForward = transform.forward;
                    currentSpeed = m_Car.CurrentSpeed;
                    x1 = transform.position[0];
                    z1 = transform.position[2];
                }
                else
                {
                    if (hit[2] == path.Count - 1)
                        m_Car.Move(0, 0, 1f, 1f);
                    else
                        m_Car.Move(newSteer / 25, acc, 1f, 0f);
                }
            }



            /*
            if (crashed && hit[1] > 5)
            {
                Debug.Log("!!!!!##################!!!!!!!!!!crashed!!!!!!!!#################!!!!!!");
                m_Car.Move(-steeringAngel, 0, -1f, 0);

                currentForward = transform.forward;
                currentSpeed = m_Car.CurrentSpeed;
                x1 = transform.position[0];
                z1 = transform.position[2];

            }
            else
            {
                if (hit[0] == path.Count - 1)
                    m_Car.Move(0, 0, 1f, 1f);
                else
                    m_Car.Move(steeringAngel / 25, acc, 1f, 0f);
            }
            */






            //Debug.Log("current heading:" + path[firstIndex].angel * 180f / Math.PI);
            // when hit the next point
            if (Math.Abs(transform.position[0] - x2) < 6f && Math.Abs(transform.position[2] - z2) < 6f)  // 6f
            {  //1f can be changed ?
                if (isVisited[nextIndex] == 0)
                {
                    Debug.Log(transform.name + "******************Hit next position******************!" + nextIndex);
                    if (transform.name == "ArmedCar (1)")
                        hit[0]++;
                    if (transform.name == "ArmedCar")
                        hit[1]++;
                    if (transform.name == "ArmedCar (2)")
                        hit[2]++;
                    //path[nextIndex].angel += HeadingChange;
                    x1 = transform.position[0];
                    z1 = transform.position[2];
                    isVisited[nextIndex] = 1;
                    nextIndex += 1;
                    currentForward = transform.forward;
                    currentSpeed = m_Car.CurrentSpeed;
                }
            }





                /*
                Vector3 avg_pos = Vector3.zero;

                foreach (GameObject friend in friends)
                {
                    avg_pos += friend.transform.position;
                }
                avg_pos = avg_pos / friends.Length;
                Vector3 direction = (avg_pos - transform.position).normalized;

                bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
                bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

                float steering = 0f;
                float acceleration = 0;

                if (is_to_the_right && is_to_the_front)
                {
                    steering = 1f;
                    acceleration = 1f;
                }
                else if (is_to_the_right && !is_to_the_front)
                {
                    steering = -1f;
                    acceleration = -1f;
                }
                else if (!is_to_the_right && is_to_the_front)
                {
                    steering = -1f;
                    acceleration = 1f;
                }
                else if (!is_to_the_right && !is_to_the_front)
                {
                    steering = 1f;
                    acceleration = -1f;
                }

                // this is how you access information about the terrain
                int i = terrain_manager.myInfo.get_i_index(transform.position.x);
                int j = terrain_manager.myInfo.get_j_index(transform.position.z);
                float grid_center_x = terrain_manager.myInfo.get_x_pos(i);
                float grid_center_z = terrain_manager.myInfo.get_z_pos(j);

                Debug.DrawLine(transform.position, new Vector3(grid_center_x, 0f, grid_center_z));


                // this is how you control the car
                Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
                m_Car.Move(steering, acceleration, acceleration, 0f);
                //m_Car.Move(0f, -1f, 1f, 0f);
                */

            }

        IEnumerator DidWeCrash()
        {
            yield return new WaitForSeconds(2f);
            Vector3 myPosition = transform.position;
            while (!goalReached)
            {
                yield return new WaitForSeconds(1f);
                if ((myPosition - transform.position).magnitude < 0.5f && !crashed)  // original is 0.5
                {
                    crashed = true;
                    crashCounter++;
                    yield return new WaitForSeconds(0.5f);
                    crashed = false;
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    crashCounter = 0;
                }
                myPosition = transform.position;
            }
        }


        float turnClockwise(float heading, float pdirection)
        {
            float flag = 1f;
            if (heading > 0 && pdirection > 0)
            {
                if (heading > pdirection)
                {
                    //Debug.Log("---------1");
                    flag = -1f;
                }
                else
                {
                   //Debug.Log("---------2");
                    flag = 1f;
                }

            }
            else if (heading < 0 && pdirection < 0)
            {
                if (heading < pdirection)
                {
                    //Debug.Log("---------3");
                    flag = 1f;
                }

                else
                {
                    //Debug.Log("---------4");
                    flag = -1f;

                }

            }
            else if (heading > 0 && pdirection <= 0)
            {
                if (Math.Abs(heading - pdirection) > 180)
                {

                    //Debug.Log("---------5");
                    flag = 1f;
                }
                else
                {
                    //Debug.Log("---------6");
                    flag = -1f;
                }
            }
            else if (heading <= 0 && pdirection > 0)
            {
                if (Math.Abs(pdirection - heading) < 180)
                {
                    //Debug.Log("---------7");
                    flag = 1f;
                }
                else
                {
                    //Debug.Log("---------8");
                    flag = -1f;
                }
            }
            return flag;
        }

        float calculate_angel(float x1, float z1, float x2, float z2)
        {
            float angel = (float)Math.Atan2(z2 - z1, x1 - x2);
            angel = 180f * angel / (float)Math.PI;
            //if (z2 < z1)
            //angel -= 180f;
            return angel;
        }

        void drawLine(List<List<Node>> allPaths)
        {
            Color color = new Color();
            //Debug.Log(string.Format("Nums of points:{0}", path.Count));
            for (int i = 0; i < allPaths.Count; i++)
            {
                if (i == 0)
                    color = Color.red;
                if (i == 1)
                    color = Color.green;
                if (i == 2)
                    color = Color.blue;
                for (int k = 0; k < allPaths[i].Count; k++)
                {
                    Node currentNode = allPaths[i][k];
                    Vector3 currentPos = new Vector3( terrain_manager.myInfo.get_x_pos( (int)allPaths[i][k].i), 0, terrain_manager.myInfo.get_z_pos((int)allPaths[i][k].j ));
                    foreach(Node sonNode in currentNode.son) {
                        Vector3 nextPos = new Vector3(terrain_manager.myInfo.get_x_pos((int)sonNode.i), 0, terrain_manager.myInfo.get_z_pos((int)sonNode.j));
                        Debug.DrawLine(currentPos, nextPos, color, 10000f);
                    }


                    //Debug.Log(string.Format("drawing point: {0},{1}", currentPos[0], currentPos[2]));
                    //Debug.Log(string.Format("drawing point: {0},{1}", nextPos[0], nextPos[2]));

                }

            }

        }

        void drawLine2(List<List<Vector2>> allPaths)
        {
            Color color = new Color();
            //Debug.Log(string.Format("Nums of points:{0}", path.Count));
            for (int i = 0; i < allPaths.Count; i++)
            {
                if (i == 0)
                    color = Color.yellow;
                if (i == 1)
                    color = Color.yellow;
                if (i == 2)
                    color = Color.yellow;
                for (int k = 0; k < allPaths[i].Count-1; k++)
                {
                    Vector2 n1 = allPaths[i][k];
                    Vector2 n2 = allPaths[i][k + 1];
                    Vector3 p1 = new Vector3(n1[0], 0, n1[1]);
                    Vector3 p2 = new Vector3(n2[0], 0, n2[1]);
                    Debug.DrawLine(p1, p2, color, 10000f);

                }

            }

        }


    }
}
