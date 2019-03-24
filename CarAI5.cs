using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI5 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;
        private Boolean enemiesDetermined = false;

        public GameObject[] friends;
        public GameObject[] enemies;

        public int[,] map_point;
        public List<int[]> border_point;
        public int[,,] map_border_point;
        public int[] state_enemies;

        public List<float[]> path_intermediate;
        public List<Vector2> path;
        public List<Vector2> pathInterm;
        public int count;

        // Yuhao Lin
        Vector3 leaderPos;
        Vector3 leaderRotation;

        Vector3 nextPos;  // nextPos of two followers
        int nextIndex = 0;
        float angel;  //for use of computing steer angel
        float maxSteer;
        List<int> isVisited = new List<int>();

        void OnDrawGizmos()
        {

            Gizmos.color = Color.red;
            for (int i = 0; i < path.Count; i++)
            {

                Vector3 p1 = new Vector3(path[i][0], 0, path[i][1]);
                Gizmos.DrawSphere(p1, 1f);
                Handles.color = Gizmos.color;
                Handles.Label(p1, i.ToString());
                Gizmos.DrawSphere(nextPos, 1.5f);
            }

        }

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

           
            enemies = GameObject.FindGameObjectsWithTag("Enemy");


            findPointsToVisit();
            maxSteer = m_Car.m_MaximumSteerAngle;
            if (transform.name == "ArmedCar (3)") {
                leaderPos = transform.position;
                leaderRotation = transform.forward;
            }

        }


        private void FixedUpdate()
        {
            float acc = 0.5f;
            /* Adjst speed */
            if (m_Car.CurrentSpeed > 10f)  // 10 speed and 0.04 start time.
                acc = 0f;
            while (enemiesDetermined == false)
            {
                enemies = GameObject.FindGameObjectsWithTag("Enemy");
                if (enemies.Length > 0)
                {
                    if (transform.name == "ArmedCar (4)")
                    {
                        findPointsToVisit();
                        for (int i = 0; i < path.Count; i++)
                            isVisited.Add(0);
                    }

                    if (transform.name == "ArmedCar (3)")
                    {
                        findPointsToVisit();
                        for (int i = 0; i < path.Count; i++)
                            isVisited.Add(0);
                    }

                    if (transform.name == "ArmedCar (2)")
                    {
                        findPointsToVisit();
                        for (int i = 0; i < path.Count; i++)
                            isVisited.Add(0);
                    }
                    enemiesDetermined = true;
                }
            }

            // If it is leader car, follow the points in PATH
            if (transform.name.Equals("ArmedCar (3)")) {
                leaderPos = transform.position;
                leaderRotation = transform.forward;
                nextPos = new Vector3(path[nextIndex][0], 0f, path[nextIndex][1]);

                Debug.Log("Leader, current position: " + transform.position[0] + ", " + transform.position[2]);
            }
            else
            {
                leaderPos = friends[2].transform.position;
                leaderRotation = friends[2].transform.forward;
                Debug.Log("Leader position input to follower: " + leaderPos[0] + ", " + leaderPos[2]);
                nextPos = ComputeNextPos(transform.name, leaderPos, leaderRotation);
                /*
                if (Time.time > 5f && Vector3.Distance(transform.position, nextPos) > 2f)
                    acc = 0.2f;
                else if (Time.time > 5f && Vector3.Distance(transform.position, nextPos) > 0.5f)
                    acc = 0.1f;
                */
                nextPos = new Vector3(path[nextIndex][0], 0f, path[nextIndex][1]);
                Debug.Log(transform.name + ", next position: "+ nextPos[0] +", "+ nextPos[2]);

            }
            /* compute steering angel */
            float steeringAngel = CalculateTurnAngle(transform.position, nextPos, maxSteer, out angel);
            float heading = Vector3.Angle(transform.forward, new Vector3(-1, 0, 0));
            if (transform.forward[2] < 0f) heading = -heading;
            float pdirection = calculate_angel(transform.position[0], transform.position[2], nextPos[0], nextPos[2]);
            float flag_turn = turnClockwise(heading, pdirection);
            if (flag_turn == 1f)
                steeringAngel = Math.Abs(steeringAngel);
            else
                steeringAngel = -Math.Abs(steeringAngel);

            //Debug.Log("Steering angel is: "+ steeringAngel);

            /* Drive */
            if(transform.name == "ArmedCar (3)")
                m_Car.Move(steeringAngel, acc, 1f, 0f);
            else if(Time.time > 0.04f)
                m_Car.Move(steeringAngel, acc, 1f, 0f);

            /* when hit the next point*/
            if (Math.Abs(transform.position[0] - nextPos[0]) < 6f && Math.Abs(transform.position[2] - nextPos[2]) < 6f)  // 6f
            {  
                if (isVisited[nextIndex] == 0)
                {
                    Debug.Log(transform.name + "******************Hit next position******************!" + nextIndex);
                    isVisited[nextIndex] = 1;
                    nextIndex += 1;
                  
                }
            }

        }
            




        public Vector3 ComputeNextPos(string ID, Vector3 leaderPos, Vector3 rotation)
        {
            Vector3 newPos = new Vector3(leaderPos[0], leaderPos[1], leaderPos[2]);
            Vector3 newRotation;
            float GapDis = 5f;
            if (ID.Equals("ArmedCar (4)"))
            {
                newRotation = Quaternion.AngleAxis(-90, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * GapDis);
            }
            if (ID.Equals("ArmedCar (2)"))
            {
                newRotation = Quaternion.AngleAxis(90, Vector3.up) * rotation;
                newPos = leaderPos + (newRotation.normalized * GapDis);
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

        private int[] PosToIndex(Vector3 elem)
        {
            int x, z;
            float x_low = terrain_manager.myInfo.x_low;
            int x_N = terrain_manager.myInfo.x_N;
            float z_low = terrain_manager.myInfo.z_low;
            int z_N = terrain_manager.myInfo.z_N;

            x = (int)Math.Floor((elem.x - x_low) / x_N);
            z = (int)Math.Floor((elem.z - z_low) / z_N);

            return new int[] { x, z };
        }

        private float[] IndexToPos(int pos_x, int pos_z)
        {
            float x, z;
            float x_low = terrain_manager.myInfo.x_low;
            int x_N = terrain_manager.myInfo.x_N;
            float z_low = terrain_manager.myInfo.z_low;
            int z_N = terrain_manager.myInfo.z_N;

            x = (pos_x * x_N) + x_low + x_N / 2;
            z = (pos_z * z_N) + z_low + z_N / 2;

            return new float[] { x, z };
        }

        private void findPointsToVisit()
        {
            path_intermediate = new List<float[]>();

            /* Initialize state turrets */

            state_enemies = new int[enemies.Length]; // 1 if removed

            /* Create map to know how many turrets are in line with each case */

            map_point = new int[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1)];
            int i, j, k;
            int[] pos;
            for (i = 0; i < enemies.Length; i++)
            {
                pos = PosToIndex(enemies[i].transform.position);
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(0); j++)
                {
                    for (k = 0; k < terrain_manager.myInfo.traversability.GetLength(1); k++)
                    {
                        if (terrain_manager.myInfo.traversability[j, k] == 1)
                        {
                            map_point[j, k] = -1;
                        }
                        else
                        {
                            if (IsInLineOfSight(j, k, pos[0], pos[1]))
                            {
                                map_point[j, k] = map_point[j, k] + 1;
                            }
                        }
                    }
                }
            }

            pos = PosToIndex(friends[0].transform.position);
            while (all_enemies_down() == false)
            {
                /* Create list of cases in border with the first zone */

                findPointBorder(pos[0], pos[1]);

                /* Choose best point */
                int distance = DistanceWithAStar(border_point[0][0], border_point[0][1], pos[0], pos[1]);
                int nb_point = map_point[border_point[0][0], border_point[0][1]];
                int pos_x_point = border_point[0][0];
                int pos_z_point = border_point[0][1];
                for (i = 1; i < border_point.Count; i++)
                {
                    if (map_point[border_point[i][0], border_point[i][1]] < nb_point)
                    {
                        distance = DistanceWithAStar(border_point[i][0], border_point[i][1], pos[0], pos[1]);
                        nb_point = map_point[border_point[i][0], border_point[i][1]];
                        pos_x_point = border_point[i][0];
                        pos_z_point = border_point[i][1];
                    }
                    else if (map_point[border_point[i][0], border_point[i][1]] == nb_point)
                    {
                        if (DistanceWithAStar(border_point[i][0], border_point[i][1], pos[0], pos[1]) < distance)
                        {
                            distance = DistanceWithAStar(border_point[i][0], border_point[i][1], pos[0], pos[1]);
                            nb_point = map_point[border_point[i][0], border_point[i][1]];
                            pos_x_point = border_point[i][0];
                            pos_z_point = border_point[i][1];
                        }
                    }
                }

                cleanMap(pos_x_point, pos_z_point);

                pos = new int[] { pos_x_point, pos_z_point };
                path_intermediate.Add(IndexToPos(pos[0], pos[1]));

                Debug.Log("Zone to reach : x = " + IndexToPos(pos[0], pos[1])[0] + " +/- 10 && z = " + IndexToPos(pos[0], pos[1])[1] + " +/- 10");
            }
            path_intermediateToPathInterm();
            updatePath();
        }

        private void path_intermediateToPathInterm()
        {
            pathInterm = new List<Vector2>();
            Vector2 elem;
            for (int i = 0; i < path_intermediate.Count; i++)
            {
                elem = new Vector2();
                elem.x = path_intermediate[i][0];
                elem.y = path_intermediate[i][1];
                pathInterm.Add(elem);
            }
        }

        private int[] PosToIndex2(Vector2 elem)
        {
            int x, z;
            float x_low = terrain_manager.myInfo.x_low;
            int x_N = terrain_manager.myInfo.x_N;
            float z_low = terrain_manager.myInfo.z_low;
            int z_N = terrain_manager.myInfo.z_N;

            x = (int)Math.Floor((elem.x - x_low) / x_N);
            z = (int)Math.Floor((elem.y - z_low) / z_N);

            return new int[] { x, z };
        }

        public Vector2[] findPath(Vector2 start, Vector2 goal)
        {
            List<int[]> pathInterm2 = new List<int[]>();
            Vector2[] pathVec;
            int pos1_x, pos1_z, pos2_x, pos2_z, current_x, current_z;
            pos1_x = PosToIndex2(start)[0];
            pos1_z = PosToIndex2(start)[1];
            pos2_x = PosToIndex2(goal)[0];
            pos2_z = PosToIndex2(goal)[1];

            int[] previous = new int[] { pos1_x, pos1_z };
            int[] previousInLineOfSight = new int[] { pos1_x, pos1_z };
            current_x = pos1_x;
            current_z = pos1_z;
            int max = DistanceWithAStar(pos1_x, pos1_z, pos2_x, pos2_z);
            for (int i = 0; i < max - 1; i++)
            {
                if (current_x + 1 < terrain_manager.myInfo.traversability.GetLength(0) && terrain_manager.myInfo.traversability[current_x + 1, current_z] == 0 && map_point[current_x + 1, current_z] == 0 && (i + DistanceWithAStar(current_x + 1, current_z, pos2_x, pos2_z) + 1) == max)
                {
                    current_x = current_x + 1;
                    if (IsInLineOfSight3(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_z + 1 < terrain_manager.myInfo.traversability.GetLength(1) && terrain_manager.myInfo.traversability[current_x, current_z + 1] == 0 && map_point[current_x, current_z + 1] == 0 && (i + DistanceWithAStar(current_x, current_z + 1, pos2_x, pos2_z) + 1) == max)
                {
                    current_z = current_z + 1;
                    if (IsInLineOfSight3(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_x - 1 > 0 && terrain_manager.myInfo.traversability[current_x - 1, current_z] == 0 && map_point[current_x - 1, current_z] == 0 && (i + DistanceWithAStar(current_x - 1, current_z, pos2_x, pos2_z) + 1) == max)
                {
                    current_x = current_x - 1;
                    if (IsInLineOfSight3(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_z - 1 > 0 && terrain_manager.myInfo.traversability[current_x, current_z - 1] == 0 && map_point[current_x, current_z - 1] == 0 && (i + DistanceWithAStar(current_x, current_z - 1, pos2_x, pos2_z) + 1) == max)
                {
                    current_z = current_z - 1;
                    if (IsInLineOfSight3(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else
                {
                    Debug.Log("PROBLEM PROBLEM PROBLEM");
                }
            }

            if (IsInLineOfSight3(previousInLineOfSight[0], previousInLineOfSight[1], pos2_x, pos2_z) == false)
            {
                pathInterm2.Add(previous);
            }

            pathInterm2.Add(new int[] { pos2_x, pos2_z });

            pathVec = pathInterm2ToPath(pathInterm2);

            String tab = "Start : " + start.ToString() + " Goal : " + goal.ToString() + "\n";
            for (int i = 0; i < pathVec.Length; i++)
            {
                tab = tab + " " + pathVec[i].ToString();
            }
            Debug.Log(tab);

            return pathVec;
        }

        private Vector2[] pathInterm2ToPath(List<int[]> pathInterm2)
        {
            Vector2 point;
            Vector2[] pathVec = new Vector2[pathInterm2.Count];
            for (int i = 0; i < pathInterm2.Count; i++)
            {
                point = new Vector2();
                point.x = IndexToPos(pathInterm2[i][0], pathInterm2[i][1])[0];
                point.y = IndexToPos(pathInterm2[i][0], pathInterm2[i][1])[1];
                pathVec[i] = point;
            }
            return pathVec;
        }

        private void updatePath()
        {
            Vector2 current = new Vector2();
            current.x = transform.position.x;
            current.y = transform.position.z;
            Vector2 next;
            Vector2[] pathAStar;
            for (int i = 0; i < pathInterm.Count; i++)
            {
                next = pathInterm[i];
                pathAStar = findPath(current, next);
                Debug.Log("Length : " + pathAStar.Length.ToString());

                String tab = "Start : " + current.ToString() + " Goal : " + next.ToString() + "\n";
                for (int j = 0; j < pathAStar.Length; j++)
                {
                    tab = tab + " " + pathAStar[j].ToString();
                    path.Add(pathAStar[j]);
                    count = count + 1;
                }
                Debug.Log(count.ToString());

                current = next;
            }
        }

        private bool all_enemies_down()
        {
            bool test = true;
            int i;
            for (i = 0; i < state_enemies.Length && test == true; i++)
            {
                if (state_enemies[i] == 0)
                {
                    test = false;
                }
            }
            return test;
        }

        private void cleanMap(int pos_x, int pos_z)
        {
            int i, j, k;
            int[] pos;
            for (i = 0; i < enemies.Length; i++)
            {
                pos = PosToIndex(enemies[i].transform.position);
                if (IsInLineOfSight(pos_x, pos_z, pos[0], pos[1]) && state_enemies[i] == 0)
                {
                    for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(0); j++)
                    {
                        for (k = 0; k < terrain_manager.myInfo.traversability.GetLength(1); k++)
                        {
                            if (terrain_manager.myInfo.traversability[j, k] == 0)
                            {
                                if (IsInLineOfSight(j, k, pos[0], pos[1]))
                                {
                                    map_point[j, k] = map_point[j, k] - 1;
                                }
                            }
                        }
                    }
                    state_enemies[i] = 1; // turret down
                }
            }
        }

        private void findPointBorder(int pos_x, int pos_z)
        {
            border_point = new List<int[]>();
            map_border_point = new int[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1), 2];
            int j, k;
            for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(0); j++)
            {
                for (k = 0; k < terrain_manager.myInfo.traversability.GetLength(1); k++)
                {
                    map_border_point[j, k, 0] = map_point[j, k];
                }
            }

            findPointBorderRecursively(pos_x, pos_z, map_point[pos_x, pos_z]);
        }

        private void findPointBorderRecursively(int pos_x, int pos_z, int nb_point)
        {
            map_border_point[pos_x, pos_z, 1] = 1;

            int x, z;

            x = pos_x - 1;
            z = pos_z;
            if (x > 0)
            {
                if (map_border_point[x, z, 0] != -1)
                {
                    if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] != nb_point)
                    {
                        border_point.Add(new int[] { x, z });
                        map_border_point[x, z, 1] = 1;
                    }
                    else if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] == nb_point)
                    {
                        findPointBorderRecursively(x, z, nb_point);
                    }
                }
            }

            x = pos_x;
            z = pos_z + 1;
            if (z < map_border_point.GetLength(1))
            {
                if (map_border_point[x, z, 0] != -1)
                {
                    if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] != nb_point)
                    {
                        border_point.Add(new int[] { x, z, map_border_point[x, z, 0] });
                        map_border_point[x, z, 1] = 1;
                    }
                    else if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] == nb_point)
                    {
                        findPointBorderRecursively(x, z, nb_point);
                    }
                }
            }

            x = pos_x + 1;
            z = pos_z;
            if (x < map_border_point.GetLength(0))
            {
                if (map_border_point[x, z, 0] != -1)
                {
                    if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] != nb_point)
                    {
                        border_point.Add(new int[] { x, z, map_border_point[x, z, 0] });
                        map_border_point[x, z, 1] = 1;
                    }
                    else if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] == nb_point)
                    {
                        findPointBorderRecursively(x, z, nb_point);
                    }
                }
            }

            x = pos_x;
            z = pos_z - 1;
            if (z > 0)
            {
                if (map_border_point[x, z, 0] != -1)
                {
                    if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] != nb_point)
                    {
                        border_point.Add(new int[] { x, z, map_border_point[x, z, 0] });
                        map_border_point[x, z, 1] = 1;
                    }
                    else if (map_border_point[x, z, 1] == 0 && map_border_point[x, z, 0] == nb_point)
                    {
                        findPointBorderRecursively(x, z, nb_point);
                    }
                }
            }
        }

        private bool IsInLineOfSight(int pos1_x, int pos1_z, int pos2_x, int pos2_z)
        {
            bool isInLineOfSight = true;
            float[,] tab = new float[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1)];
            int i, j;
            for (i = 0; i < terrain_manager.myInfo.traversability.GetLength(0); i++)
            {
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(1); j++)
                {
                    tab[i, j] = terrain_manager.myInfo.traversability[i, j]; // 1 if obstacle or 0.
                }
            }

            int diffX = Math.Abs(pos1_x - pos2_x);
            int diffZ = Math.Abs(pos1_z - pos2_z);
            double m1, m2, m3;
            double p1, p2, p3;
            double zA1, zA2, zA3, zB1, zB2, zB3, xA1, xA2, xA3, xB1, xB2, xB3;
            double step = 0.1;
            int numberStepInOne = (int)(1.0 / step);

            if (pos1_x == pos2_x && pos1_z < pos2_z) // 2 is up
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z + i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x == pos2_x && pos1_z > pos2_z) // 2 is down
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z - i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z == pos2_z) // 2 is right
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x + i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z == pos2_z) // 2 is left
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x - i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z < pos2_z) // 2 is up right
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z < pos2_z) // 2 is up left
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z > pos2_z) // 2 is down right
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z > pos2_z) // 2 is down left
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }

            return isInLineOfSight;
        }

        private bool IsInLineOfSight3(int pos1_x, int pos1_z, int pos2_x, int pos2_z)
        {
            bool isInLineOfSight = true;
            float[,] tab = new float[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1)];
            int i, j;
            for (i = 0; i < terrain_manager.myInfo.traversability.GetLength(0); i++)
            {
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(1); j++)
                {
                    tab[i, j] = terrain_manager.myInfo.traversability[i, j]; // 1 if obstacle or 0.
                    if (map_point[i, j] != 0)
                    {
                        tab[i, j] = 1;
                    }
                }
            }

            int diffX = Math.Abs(pos1_x - pos2_x);
            int diffZ = Math.Abs(pos1_z - pos2_z);
            double m1, m2, m3;
            double p1, p2, p3;
            double zA1, zA2, zA3, zB1, zB2, zB3, xA1, xA2, xA3, xB1, xB2, xB3;
            double step = 0.1;
            int numberStepInOne = (int)(1.0 / step);

            if (pos1_x == pos2_x && pos1_z < pos2_z) // 2 is up
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z + i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x == pos2_x && pos1_z > pos2_z) // 2 is down
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z - i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z == pos2_z) // 2 is right
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x + i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z == pos2_z) // 2 is left
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x - i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z < pos2_z) // 2 is up right
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z < pos2_z) // 2 is up left
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z > pos2_z) // 2 is down right
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z > pos2_z) // 2 is down left
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }

            return isInLineOfSight;
        }

        private bool IsInLineOfSight2(int pos1_x, int pos1_z, int pos2_x, int pos2_z) // Special for problem 5
        {
            bool isInLineOfSight = true;
            bool isInLineOfSightTop = true;
            bool isInLineOfSightBot = true;
            bool isInLineOfSightMid = true;
            float[,] tab = new float[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1)];
            int i, j;
            for (i = 0; i < terrain_manager.myInfo.traversability.GetLength(0); i++)
            {
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(1); j++)
                {
                    tab[i, j] = terrain_manager.myInfo.traversability[i, j]; // 1 if obstacle or 0.
                }
            }

            int diffX = Math.Abs(pos1_x - pos2_x);
            int diffZ = Math.Abs(pos1_z - pos2_z);
            double m1, m2, m3;
            double p1, p2, p3;
            double zA1, zA2, zA3, zB1, zB2, zB3, xA1, xA2, xA3, xB1, xB2, xB3;
            double step = 0.1;
            int numberStepInOne = (int)(1.0 / step);

            if (pos1_x == pos2_x && pos1_z < pos2_z) // 2 is up
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z + i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x == pos2_x && pos1_z > pos2_z) // 2 is down
            {
                for (i = 0; i < diffZ && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x, pos1_z - i] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z == pos2_z) // 2 is right
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x + i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z == pos2_z) // 2 is left
            {
                for (i = 0; i < diffX && isInLineOfSight == true; i++)
                {
                    if (tab[pos1_x - i, pos1_z] == 1)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z < pos2_z) // 2 is up right
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSightTop = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSightBot = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSightMid = false;
                    }
                    if (isInLineOfSightBot == false && isInLineOfSightMid == false && isInLineOfSightTop == false)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z < pos2_z) // 2 is up left
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSightTop = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSightBot = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSightMid = false;
                    }
                    if (isInLineOfSightBot == false && isInLineOfSightMid == false && isInLineOfSightTop == false)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x < pos2_x && pos1_z > pos2_z) // 2 is down right
            {
                zA1 = pos1_z;
                xA1 = pos1_x;
                zA2 = pos1_z + 1;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z;
                xB1 = pos2_x;
                zB2 = pos2_z + 1;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 + i * step)), (int)(Math.Floor(m1 * (xA1 + i * step) + p1))] == 1)
                    {
                        isInLineOfSightTop = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 + i * step)), (int)(Math.Floor(m2 * (xA2 + i * step) + p2))] == 1)
                    {
                        isInLineOfSightBot = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 + i * step)), (int)(Math.Floor(m3 * (xA3 + i * step) + p3))] == 1)
                    {
                        isInLineOfSightMid = false;
                    }
                    if (isInLineOfSightBot == false && isInLineOfSightMid == false && isInLineOfSightTop == false)
                    {
                        isInLineOfSight = false;
                    }
                }
            }
            else if (pos1_x > pos2_x && pos1_z > pos2_z) // 2 is down left
            {
                zA1 = pos1_z + 1;
                xA1 = pos1_x;
                zA2 = pos1_z;
                xA2 = pos1_x + 1;
                zA3 = pos1_z + 0.5;
                xA3 = pos1_x + 0.5;

                zB1 = pos2_z + 1;
                xB1 = pos2_x;
                zB2 = pos2_z;
                xB2 = pos2_x + 1;
                zB3 = pos2_z + 0.5;
                xB3 = pos2_x + 0.5;

                m1 = (zB1 - zA1) / (xB1 - xA1);
                m2 = (zB2 - zA2) / (xB2 - xA2);
                m3 = (zB3 - zA3) / (xB3 - xA3);
                p1 = zA1 - m1 * xA1;
                p2 = zA2 - m2 * xA2;
                p3 = zA3 - m3 * xA3;

                for (i = 1; i < (diffX * numberStepInOne) - 1 && isInLineOfSight == true; i++)
                {
                    if (tab[(int)(Math.Floor(xA1 - i * step)), (int)(Math.Floor(m1 * (xA1 - i * step) + p1))] == 1)
                    {
                        isInLineOfSightTop = false;
                    }
                    if (tab[(int)(Math.Floor(xA2 - i * step)), (int)(Math.Floor(m2 * (xA2 - i * step) + p2))] == 1)
                    {
                        isInLineOfSightBot = false;
                    }
                    if (tab[(int)(Math.Floor(xA3 - i * step)), (int)(Math.Floor(m3 * (xA3 - i * step) + p3))] == 1)
                    {
                        isInLineOfSightMid = false;
                    }
                    if (isInLineOfSightBot == false && isInLineOfSightMid == false && isInLineOfSightTop == false)
                    {
                        isInLineOfSight = false;
                    }
                }
            }

            return isInLineOfSight;
        }

        private int DistanceWithAStar(int pos1_x, int pos1_z, int pos2_x, int pos2_z)
        {
            float[,,] tab = new float[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1), 5];
            int i, j;
            for (i = 0; i < terrain_manager.myInfo.traversability.GetLength(0); i++)
            {
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(1); j++)
                {
                    tab[i, j, 1] = terrain_manager.myInfo.traversability[i, j]; // 1 if obstacle or 0.
                    tab[i, j, 2] = 0; // Value for A*
                    tab[i, j, 3] = 0; // 2 if already observed by A*, 1 if neighbor of observed case, else 0.
                    tab[i, j, 4] = 0; // Distance found by A*
                }
            }

            // First compute pos1 to pos2
            tab[pos1_x, pos1_z, 3] = 2;
            if (pos1_x - 1 > 0)
            {
                if (tab[pos1_x - 1, pos1_z, 1] == 0)
                {
                    tab[pos1_x - 1, pos1_z, 2] = 1 + DistanceCaseByCase(pos1_x - 1, pos1_z, pos2_x, pos2_z);
                    tab[pos1_x - 1, pos1_z, 3] = 1;
                    tab[pos1_x - 1, pos1_z, 4] = 1;
                }
            }
            if (pos1_x + 1 < tab.GetLength(0))
            {
                if (tab[pos1_x + 1, pos1_z, 1] == 0)
                {
                    tab[pos1_x + 1, pos1_z, 2] = 1 + DistanceCaseByCase(pos1_x + 1, pos1_z, pos2_x, pos2_z);
                    tab[pos1_x + 1, pos1_z, 3] = 1;
                    tab[pos1_x + 1, pos1_z, 4] = 1;
                }
            }
            if (pos1_z - 1 > 0)
            {
                if (tab[pos1_x, pos1_z - 1, 1] == 0)
                {
                    tab[pos1_x, pos1_z - 1, 2] = 1 + DistanceCaseByCase(pos1_x, pos1_z - 1, pos2_x, pos2_z);
                    tab[pos1_x, pos1_z - 1, 3] = 1;
                    tab[pos1_x, pos1_z - 1, 4] = 1;
                }
            }
            if (pos1_z + 1 < tab.GetLength(1))
            {
                if (tab[pos1_x, pos1_z + 1, 1] == 0)
                {
                    tab[pos1_x, pos1_z + 1, 2] = 1 + DistanceCaseByCase(pos1_x, pos1_z + 1, pos2_x, pos2_z);
                    tab[pos1_x, pos1_z + 1, 3] = 1;
                    tab[pos1_x, pos1_z + 1, 4] = 1;
                }
            }

            // Other computes
            int[] previous;
            int pos_x;
            int pos_z;
            int distance;
            Boolean foundPath = false;
            if (tab[pos2_x, pos2_z, 3] == 1)
            {
                foundPath = true;
            }
            while (foundPath == false)
            {
                previous = PosBestCaseForAStar(tab);
                pos_x = previous[0];
                pos_z = previous[1];
                distance = previous[2];
                tab[pos_x, pos_z, 3] = 2;
                if (pos_x - 1 > 0)
                {
                    if (tab[pos_x - 1, pos_z, 1] == 0 && tab[pos_x - 1, pos_z, 3] == 0)
                    {
                        tab[pos_x - 1, pos_z, 2] = distance + 1 + DistanceCaseByCase(pos_x - 1, pos_z, pos2_x, pos2_z);
                        tab[pos_x - 1, pos_z, 3] = 1;
                        tab[pos_x - 1, pos_z, 4] = distance + 1;
                    }
                }
                if (pos_x + 1 < tab.GetLength(0))
                {
                    if (tab[pos_x + 1, pos_z, 1] == 0 && tab[pos_x + 1, pos_z, 3] == 0)
                    {
                        tab[pos_x + 1, pos_z, 2] = distance + 1 + DistanceCaseByCase(pos_x + 1, pos_z, pos2_x, pos2_z);
                        tab[pos_x + 1, pos_z, 3] = 1;
                        tab[pos_x + 1, pos_z, 4] = distance + 1;
                    }
                }
                if (pos_z - 1 > 0)
                {
                    if (tab[pos_x, pos_z - 1, 1] == 0 && tab[pos_x, pos_z - 1, 3] == 0)
                    {
                        tab[pos_x, pos_z - 1, 2] = distance + 1 + DistanceCaseByCase(pos_x, pos_z - 1, pos2_x, pos2_z);
                        tab[pos_x, pos_z - 1, 3] = 1;
                        tab[pos_x, pos_z - 1, 4] = distance + 1;
                    }
                }
                if (pos_z + 1 < tab.GetLength(1))
                {
                    if (tab[pos_x, pos_z + 1, 1] == 0 && tab[pos_x, pos_z + 1, 3] == 0)
                    {
                        tab[pos_x, pos_z + 1, 2] = distance + 1 + DistanceCaseByCase(pos_x, pos_z + 1, pos2_x, pos2_z);
                        tab[pos_x, pos_z + 1, 3] = 1;
                        tab[pos_x, pos_z + 1, 4] = distance + 1;
                    }
                }

                if (tab[pos2_x, pos2_z, 3] == 1)
                {
                    foundPath = true;
                }
            }

            return (int)tab[pos2_x, pos2_z, 4];
        }

        private int[] PosBestCaseForAStar(float[,,] tab)
        {
            int pos_x = -1;
            int pos_z = -1;
            float distance = -1;
            float value = -1;
            int i, j;
            for (i = 0; i < 20; i++)
            {
                for (j = 0; j < 20; j++)
                {
                    if (tab[i, j, 3] == 1) // 1 if neighbor of observed case
                    {
                        if (value == -1)
                        {
                            value = tab[i, j, 2];
                            pos_x = i;
                            pos_z = j;
                            distance = tab[i, j, 4];
                        }
                        else
                        {
                            if (value > tab[i, j, 2])
                            {
                                value = tab[i, j, 2];
                                pos_x = i;
                                pos_z = j;
                                distance = tab[i, j, 4];
                            }
                        }
                    }
                }
            }
            return new int[] { pos_x, pos_z, (int)distance };
        }

        private int DistanceCaseByCase(int pos1_x, int pos1_z, int pos2_x, int pos2_z)
        {
            int value;
            if (pos1_x > pos2_x)
            {
                value = pos1_x - pos2_x;
            }
            else
            {
                value = pos2_x - pos1_x;
            }
            if (pos1_z > pos2_z)
            {
                value = value + pos1_z - pos2_z;
            }
            else
            {
                value = value + pos2_z - pos1_z;
            }

            return value;
        }
    }
}
