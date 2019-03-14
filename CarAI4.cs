using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI4 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject trajectory_game_object;
        TrajectoryLogger trajectory_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        Control control;
        public const float GapDis = 5f;
        string parent;
        string formation = "Linear";
        //string formation = "Square";
        string[] nameList = { "ArmedCar (5)", "ArmedCar (3)", "ArmedCar (2)", "ArmedCar (4)" };
        float[] distanceToLeader = { 3*GapDis, 4*GapDis, GapDis, 2*GapDis };
        float[] waitTime = { 1.5f, 2f, 0.5f, 1f };
        int carNum = 4;
        float maxSteer;
        float angle;  // Lin lv, control


        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            trajectory_manager = trajectory_game_object.GetComponent<TrajectoryLogger>();
            control = new Control();

            setPar(transform.name, formation);

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            maxSteer = m_Car.m_MaximumSteerAngle;


            // Plan your path here
            // ...
        }


        private void FixedUpdate()
        {
            Vector3 leaderPos = friends[0].transform.position;
            //Vector3 leaderRotation = friends[0].transform.rotation.eulerAngles;
            Vector3 leaderRotation = friends[0].transform.forward;
            Vector3 currentPos = transform.position;

            //get the position and rotatio of parent car
            foreach(GameObject friend in friends) {
                if (friend.transform.name.Equals(parent)) {
                    leaderPos = friend.transform.position;
                    leaderRotation = friend.transform.forward;
                }
            }

            string carName = transform.name;
            float acc = 3f;
            //float currentSpeed = 5f;   // ???
            for (int i = 0; i < carNum; i++)
            {
                if (carName.Equals(nameList[i]))
                {
                    string ID = transform.name;
                    Vector3 nextPos = ComputeNextPos(ID, leaderPos, leaderRotation, formation);

                    Debug.Log( ID + ", next position:"+ nextPos[0] + " " + nextPos[2] );
                    Debug.Log( "Leader" + ", next position:" + leaderPos[0] + " " + leaderPos[2]);

                    float leader_angel = Vector3.Angle(leaderPos, new Vector3(1, 0, 0));
                    float current_angel = Vector3.Angle(transform.forward, new Vector3(1, 0, 0));  // should not update transform!! ??? 
                    /*
                    List<float> res = control.getSteeringAngel(currentPos[0], currentPos[2], nextPos[0], nextPos[2], current_angel, currentSpeed);
                    float steeringAngel = res[0] * 180f / (float)Math.PI;
                    float HeadingChange = res[1];
                    */
                    float steeringAngel = CalculateTurnAngle(transform.position ,nextPos, maxSteer, out angle);                   

                    //decide turning direction: Vector3.SignedAngle
                    float heading = Vector3.Angle(transform.forward, new Vector3(-1, 0, 0));
                    if (transform.forward[2] < 0f) heading = -heading;
                    float pdirection = calculate_angel(currentPos[0], currentPos[2], nextPos[0], nextPos[2]);
                    float flag_turn = turnClockwise(heading, pdirection);

                    if (flag_turn == 1f)
                        steeringAngel = Math.Abs(steeringAngel);
                    else
                        steeringAngel = -Math.Abs(steeringAngel);
                    /*
                    if (steeringAngel > Math.Abs(maxSteer))  //set brake to -1 when sharp turn
                    {
                        steeringAngel = Math.Abs(maxSteer);
                    }

                    else if (steeringAngel < -Math.Abs(maxSteer))
                    {
                        steeringAngel = -Math.Abs(maxSteer);
                    }
                    */

                    Debug.Log(ID + ", steering:" + steeringAngel);
                    /*
                    if (m_Car.CurrentSpeed > 40f)
                        acc = 0f;
                    if (Time.time > 5f && Vector3.Distance(transform.position, leaderPos) < GapDis && Math.Abs(leader_angel-current_angel) < 2f) 
                    {
                        acc = 0f;
                    }
                    if(Time.time > 5f && Vector3.Distance(transform.position, leaderPos) < 0.5*GapDis )
                    {
                        acc = -3f;
                    }
                    */


                    if (Time.time > 5f && Vector3.Distance(transform.position, leaderPos) < GapDis)
                    {
                        acc = -3f;
                    }
                    if (Time.time > 5f && Vector3.Distance(transform.position, leaderPos) > GapDis + 3f && Math.Abs(leader_angel - current_angel) < 2f)
                    {
                        acc = 2f;
                    }
                    else if(m_Car.CurrentSpeed > 40f)
                        acc = 0f;

                    if (Time.time > waitTime[i])
                        m_Car.Move(steeringAngel, acc, 1f, 0f);
                    //m_Car.Move(steeringAngel / 25, acc, 1f, 0f);
                }
            }

            

            // Execute your path here
            // ...




            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            //m_Car.Move(steering, acceleration, acceleration, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);


        }

        void setPar(string id, string formation) { 
            if(formation == "Linear") {
                if (id.Equals("ArmedCar (2)"))
                    parent = "ReplayCar";
                if (id.Equals("ArmedCar (4)"))
                    parent = "ArmedCar (2)";
                if (id.Equals("ArmedCar (5)"))
                    parent = "ArmedCar (4)";
                if (id.Equals("ArmedCar (3)"))
                    parent = "ArmedCar (5)";
            }

            if (formation.Equals("Square")) {
                if (id.Equals("ArmedCar (2)"))
                    parent = "ReplayCar";
                if (id.Equals("ArmedCar (4)"))
                    parent = "ReplayCar";
                if (id.Equals("ArmedCar (5)"))
                    parent = "ArmedCar (2)";
                if (id.Equals("ArmedCar (3)"))
                    parent = "ArmedCar (4)";
            }
        }

        /*
         * 
         *  vector = Quaternion.AngleAxis(-45, Vector3.up) * vector; // 0, -45, +45, 90 (up, left, right, down)
         *  endPoint = startPoint + (vector.normalized * length);   
         */
        public Vector3 ComputeNextPos(string ID, Vector3 leaderPos, Vector3 rotation) {
            Vector3 newPos = new Vector3(leaderPos[0], leaderPos[1], leaderPos[2]);
            Vector3 newRotation;
            if(ID == nameList[0]) 
            {   // down
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * distanceToLeader[0]);
            }
            else if (ID == nameList[1])
            {  // down *2
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * distanceToLeader[1]);
            }
            else if (ID == nameList[2])
            {  // left
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * distanceToLeader[2]);
            }
            else if (ID == nameList[3])
            {  // right
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * distanceToLeader[3]);
            }
            return newPos;
        }

        public Vector3 ComputeNextPos(string ID, Vector3 leaderPos, Vector3 rotation, string formation)
        {
            Vector3 newPos = new Vector3(leaderPos[0], leaderPos[1], leaderPos[2]);
            Vector3 newRotation;

            if(formation == "Linear") {
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * GapDis);
            }
            if(formation == "Square") { 
                if(ID.Equals("ArmedCar (2)")) {
                    newRotation = Quaternion.AngleAxis(-90, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized * GapDis);
                }
                if (ID.Equals("ArmedCar (4)"))
                {
                    newRotation = Quaternion.AngleAxis(90, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized * GapDis);
                }
                if (ID.Equals("ArmedCar (5)"))
                {
                    newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized* GapDis);
                }
                if (ID.Equals("ArmedCar (3)"))
                {
                    newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized * GapDis);
                }

            }


            return newPos;
        }

        float CalculateTurnAngle(Vector3 from, Vector3 to, float maximumSteerAngle, out float angle)
        {
            Vector3 direction = to - from;
            angle = Vector3.Angle(direction, transform.forward);
            if (Vector3.Cross(direction, transform.forward).y > 0)
            {
                angle = -angle;
            }
            return Mathf.Clamp(angle, (-1) * maximumSteerAngle, maximumSteerAngle) / maximumSteerAngle;
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


    }
}
