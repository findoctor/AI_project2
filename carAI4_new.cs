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

        public GameObject speedRecoder_game_object;
        SpeedRecoder speedRecoder;

        public GameObject[] friends;
        public GameObject[] enemies;

        Control control;
        public const float GapDis = 18f;   // 12 is workable
        string parent;
        //string formation = "Linear";
        string formation = "Square";
        string[] nameList = { "ArmedCar (5)", "ArmedCar (3)", "ArmedCar (2)", "ArmedCar (4)" };
        float[] distanceToLeader = { 3*GapDis, 4*GapDis, GapDis, 2*GapDis };
        //float[] waitTime = { 1.0f, 1.1f, 0.5f, 0.7f };  for gap = 12
        float[] waitTime = { 1.5f, 1.7f, 1.0f, 1.2f };
        int carNum = 4;
        float maxSteer;
        float angle;  // Lin lv, control

        // compute the speed of each car
        Vector3 lastPosition;
        Vector3 currentPosition;

        // to compute the speed of the leader car
        Vector3 speed_lastPos, speed_currentPos;
        float currentSpeed;
        Dictionary<string, float> speed_dict;
        Vector3 nextPos = new Vector3(80,0,80);

        void OnDrawGizmos()
        {
            // Draw a semitransparent blue cube at the transforms position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(nextPos, 2f);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            trajectory_manager = trajectory_game_object.GetComponent<TrajectoryLogger>();
            control = new Control();
            speedRecoder = speedRecoder_game_object.GetComponent<SpeedRecoder>();

            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            setPar(transform.name, formation);
            speed_lastPos = friends[0].transform.position;
            lastPosition = transform.position;
            currentSpeed = 0f;
            if(transform.name == "ArmedCar (2)") { 
                foreach (GameObject f in friends){
                    speedRecoder.speed_dictionary.Add(f.transform.name, 0f);
                }
                this.speed_dict = speedRecoder.speed_dictionary;
            }
            else
                this.speed_dict = speedRecoder.speed_dictionary;


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            maxSteer = m_Car.m_MaximumSteerAngle;


            // Plan your path here
            // ...
        }


        private void FixedUpdate()
        {
            print("Frame time: " + Time.fixedDeltaTime);
            speed_currentPos = friends[0].transform.position;
            currentPosition = transform.position;
            currentSpeed = Vector3.Distance(speed_currentPos, speed_lastPos) / Time.fixedDeltaTime;
            speed_dict["ReplayCar (2)"] = currentSpeed;
            speed_dict[transform.name] = Vector3.Distance(lastPosition, currentPosition) / Time.fixedDeltaTime;
            Vector3 leaderPos = friends[0].transform.position;
            //Vector3 leaderRotation = friends[0].transform.rotation.eulerAngles;
            Vector3 leaderRotation = friends[0].transform.forward;
            Vector3 currentPos = transform.position;

            string carName = transform.name;
            float acc = 1f;
            for (int i = 0; i < carNum; i++)
            {
                if (carName.Equals(nameList[i]))
                {
                    string ID = transform.name;
                    nextPos = ComputeNextPos(ID, leaderPos, leaderRotation, formation);

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

                    float catchUp = Vector3.Distance(transform.position, nextPos); // distance to target
                    float follwerSpeed = speed_dict[ID];
                    float leaderSpeed = speed_dict[parent];
                    float moveDis = follwerSpeed* Time.fixedDeltaTime + 0.5f*acc* Time.fixedDeltaTime* Time.fixedDeltaTime; // increase speed
                    float moveDis2 = follwerSpeed * Time.fixedDeltaTime - 0.5f * acc * Time.fixedDeltaTime * Time.fixedDeltaTime; // decrease speed


                    if (follwerSpeed < leaderSpeed && Time.time > 3f) {
                        if (moveDis >= catchUp)
                            acc = 0.2f;   // 0f
                        else
                            acc = 1f;
                    }
                    if (follwerSpeed >= leaderSpeed && Time.time > 3f)
                    {
                        //if (follwerSpeed > leaderSpeed + 5f)
                        // acc = -1f;

                        if (catchUp - GapDis > 10f && follwerSpeed - leaderSpeed > 5f)
                            acc = -0.3f;   //2f
                        if (catchUp - GapDis > 5f && catchUp - GapDis < 10f  && follwerSpeed - leaderSpeed > 2f && follwerSpeed - leaderSpeed < 5f)
                            acc = -0.6f;     //3f
                        if (catchUp - GapDis < 3f)
                            if (follwerSpeed - leaderSpeed < 1f)
                                acc = 0f;
                            else
                                acc = -1f;   // -5f
                    }


                    /*
                    if (speed_dict[ID] > speed_dict["ReplayCar (2)"] + 1f ) {
                        if (Vector3.Distance(transform.position, leaderPos) > GapDis + 3f)
                            acc = 0f;
                        else
                            acc = -1f;
                    }


                    else if (Time.time > 5f && Vector3.Distance(transform.position, leaderPos) <= GapDis + 1f)
                    {
                        
                        if (speed_dict[ID] > speed_dict[parent])
                            acc = -10f;
                        else acc = 0f;
                    }
                    else if (Time.time > 5f && Vector3.Distance(transform.position, leaderPos) > GapDis+1f ) //&& Math.Abs(leader_angel - current_angel) < 2f)
                    {
                        acc = 1f;
                    }
                    */
      
                    Debug.Log("ReplayCar, speed:"+ speed_dict["ReplayCar (2)"]);
                    Debug.Log(ID + ", acc:" + acc);
                    Debug.Log(ID + ", speed:" + speed_dict[ID]);
                    Debug.Log(ID + ", catch up distance: " + catchUp);
                    Debug.Log(ID + ", speed up distance: " + moveDis);
                    Debug.Log(ID + ", speed down distance: " + moveDis2);

                    if (Time.time > waitTime[i])
                        m_Car.Move(steeringAngel, acc, 1f, 0f);
                    //m_Car.Move(steeringAngel / 25, acc, 1f, 0f);
                }
            }
            speed_lastPos = friends[0].transform.position;
            lastPosition = transform.position;

            // Execute your path here
            // ...




            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            //m_Car.Move(steering, acceleration, acceleration, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);


        }

        public bool isObstacle(Vector3 position) {
            int i = terrain_manager.myInfo.get_i_index(position[0]);
            int j = terrain_manager.myInfo.get_i_index(position[2]);
            int[] gapi = {-1,-1,-1,0,0,0,1,1,1 };
            int[] gapj = { -1,0,1,-1,0,1,-1,0,1 };
            for(int k = 0; k < 8; k++) {
                int newi = i + gapi[k];
                int newj = j + gapj[k];
                if(terrain_manager.myInfo.traversability[newi, newj] == 1) {
                    return true;
                }
            }


            return false;
        }

        void setPar(string id, string formation) { 
            if(formation == "Linear") {
                if (id.Equals("ArmedCar (2)"))
                    parent = "ReplayCar (2)";
                if (id.Equals("ArmedCar (4)"))
                    parent = "ArmedCar (2)";
                if (id.Equals("ArmedCar (5)"))
                    parent = "ArmedCar (4)";
                if (id.Equals("ArmedCar (3)"))
                    parent = "ArmedCar (5)";
            }

            if (formation.Equals("Square")) {
                if (id.Equals("ArmedCar (2)"))
                    parent = "ReplayCar (2)";
                if (id.Equals("ArmedCar (4)"))
                    parent = "ReplayCar (2)";
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

            // set for obstacle avoid
            float[] scaleDown = { -10, -8f, -6f, -4f, -2f };

            if(formation == "Linear") {
                newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * GapDis);
            }
            if(formation == "Square") { 
                if(ID.Equals("ArmedCar (2)")) {
                    newRotation = Quaternion.AngleAxis(-150, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized * GapDis);
                    int i = 0;
                    if(isObstacle(newPos)) {
                        newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                        newPos = leaderPos + (newRotation.normalized * GapDis);
                    }
                }
                if (ID.Equals("ArmedCar (4)"))
                {
                    newRotation = Quaternion.AngleAxis(150, Vector3.up) * rotation;
                    newPos = leaderPos + (newRotation.normalized * GapDis);
                    int i = 0;
                    if(isObstacle(newPos))
                    {
                        newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                        newPos = leaderPos + (newRotation.normalized * GapDis);
                    }
                }
                if (ID.Equals("ArmedCar (5)"))
                {
                    newRotation = Quaternion.AngleAxis(-150, Vector3.up) * rotation;
                    newPos = leaderPos + 2f* (newRotation.normalized* GapDis);
                    int i = 0;
                    if(isObstacle(newPos))
                    {
                        newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                        newPos = leaderPos + 2f * (newRotation.normalized * GapDis);
                    }
                }
                if (ID.Equals("ArmedCar (3)"))
                {
                    newRotation = Quaternion.AngleAxis(150, Vector3.up) * rotation;
                    newPos = leaderPos + 2f * (newRotation.normalized * GapDis);
                    int i = 0;
                    if(isObstacle(newPos))
                    {
                        newRotation = Quaternion.AngleAxis(180, Vector3.up) * rotation;
                        newPos = leaderPos + 2f * (newRotation.normalized * GapDis);
                    }
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
