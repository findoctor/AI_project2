using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI2 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        private System.Random rand = new System.Random();

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        public GameObject genObject;
        Genetic gen;

        public int number_of_gen = 50000;

        /* Driving stuff */

        public List<Vector2> pathInterm;
        public List<Vector2> path;
        public int count;

        float maxSteer;
        float angle;  // Lin lv, control
        bool crashed = false;
        int crashCounter = 0;
        bool goalReached = false;

        // used for path follow
        float x1;
        float z1;
        float x2;
        float z2;
        int nextIndex = 2;
        float backAngel = 0f;
        int target_index;
        public int pathLen;
        float heading;
        Vector3 currentForward;
        Vector3 nextForward;
        float currentSpeed = 0f;
        List<int> isVisited = new List<int>();
        List<int> backCount = new List<int>();  // move back steps for U turn
        Vector3 lastPos;
        Vector3 currentPos;

        void OnDrawGizmos()
        {


            if (transform.name.Equals("ArmedCar (1)"))
                Gizmos.color = Color.red;

            else if (transform.name.Equals("ArmedCar (2)"))
                Gizmos.color = Color.green;

            else if (transform.name.Equals("ArmedCar (3)"))
                Gizmos.color = Color.blue;


            // draw each points along the path

            for (int i = 0; i < path.Count; i++)
            {


                Vector3 p1 = new Vector3(path[i][0], 0, path[i][1]);

                Gizmos.DrawSphere(p1, 1f);
                Handles.color = Gizmos.color;
                Handles.Label(p1, i.ToString());



            }
        

        }

            private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            gen = genObject.GetComponent<Genetic>();
            maxSteer = m_Car.m_MaximumSteerAngle;

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            x1 = transform.position[0];
            z1 = transform.position[2];
            currentForward = transform.forward;


            if (transform.name == "ArmedCar (2)")
            {
                findPointsToVisit();
                Debug.Log("BestScore :" + gen.bestScore.ToString());
                pointsToVisitByCar(0);
                String tab = transform.name + " ";
                for (int i = 0; i < path.Count; i++)   // path stores the points we want to visit one by one
                {
                    tab = tab + " " + path[i].ToString();
                }
                Debug.Log(tab);
            }

            if (transform.name == "ArmedCar (3)")
            {
                pointsToVisitByCar(1);
                String tab = transform.name + " ";
                for (int i = 0; i < path.Count; i++)   // path stores the points we want to visit one by one
                {
                    tab = tab + " " + path[i].ToString();
                }
                Debug.Log(tab);
            }

            if (transform.name == "ArmedCar (1)")
            {
                pointsToVisitByCar(2);
                String tab = transform.name + " ";
                for (int i = 0; i < path.Count; i++)   // path stores the points we want to visit one by one
                {
                    tab = tab + " " + path[i].ToString();
                }
                Debug.Log(tab);
            }

            for (int i = 0; i < path.Count; i++)
                isVisited.Add(0);

            this.crashed = false;
            StartCoroutine(DidWeCrash());
            //drawLine(path);

        }

        private void pointsToVisitByCar(int numberCar)
        {
            int i;
            Vector2 point;
            float[] pos_x_z;
            for (i = 0; i < gen.orderBestScore.GetLength(0); i++)
            {
                if (gen.orderBestScore[i, 1] == numberCar)
                {
                    point = new Vector2();
                    pos_x_z = IndexToPos(gen.points[gen.orderBestScore[i, 0], 0], gen.points[gen.orderBestScore[i, 0], 1]);
                    point.x = pos_x_z[0];
                    point.y = pos_x_z[1];
                    pathInterm.Add(point);
                }
            }
            path = new List<Vector2>();
            updatePath();
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
                if (current_x + 1 < terrain_manager.myInfo.traversability.GetLength(0) && terrain_manager.myInfo.traversability[current_x + 1, current_z] == 0 && (i + DistanceWithAStar(current_x + 1, current_z, pos2_x, pos2_z) + 1) == max)
                {
                    current_x = current_x + 1;
                    if (IsInLineOfSight(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_z + 1 < terrain_manager.myInfo.traversability.GetLength(1) && terrain_manager.myInfo.traversability[current_x, current_z + 1] == 0 && (i + DistanceWithAStar(current_x, current_z + 1, pos2_x, pos2_z) + 1) == max)
                {
                    current_z = current_z + 1;
                    if (IsInLineOfSight(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_x - 1 > 0 && terrain_manager.myInfo.traversability[current_x - 1, current_z] == 0 && (i + DistanceWithAStar(current_x - 1, current_z, pos2_x, pos2_z) + 1) == max)
                {
                    current_x = current_x - 1;
                    if (IsInLineOfSight(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
                    {
                        pathInterm2.Add(previous);
                        previousInLineOfSight = new int[] { current_x, current_z };
                    }
                    previous = new int[] { current_x, current_z };
                }
                else if (current_z - 1 > 0 && terrain_manager.myInfo.traversability[current_x, current_z - 1] == 0 && (i + DistanceWithAStar(current_x, current_z - 1, pos2_x, pos2_z) + 1) == max)
                {
                    current_z = current_z - 1;
                    if (IsInLineOfSight(previousInLineOfSight[0], previousInLineOfSight[1], current_x, current_z) == false)
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

            if (IsInLineOfSight(previousInLineOfSight[0], previousInLineOfSight[1], pos2_x, pos2_z) == false)
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

        private void findPointsToVisit()
        {
            float[,] map = new float[terrain_manager.myInfo.traversability.GetLength(0), terrain_manager.myInfo.traversability.GetLength(1)];
            int i, j;
            for (i = 0; i < terrain_manager.myInfo.traversability.GetLength(0); i++)
            {
                for (j = 0; j < terrain_manager.myInfo.traversability.GetLength(1); j++)
                {
                    map[i, j] = terrain_manager.myInfo.traversability[i, j]; // 1 if obstacle or 0. And 2 if seen.
                }
            }
            int pos_x_car = PosToIndex(friends[0].transform.position)[0];
            int pos_z_car = PosToIndex(friends[0].transform.position)[1];
            map[pos_x_car, pos_z_car] = 2;
            for (i = 0; i < map.GetLength(0); i++)
            {
                for (j = 0; j < map.GetLength(1); j++)
                {
                    if (pos_x_car != i || pos_z_car != j)
                    {
                        if (map[i, j] == 0)
                        {
                            //Debug.Log(pos_x_car.ToString() + " " + pos_z_car.ToString() + " " + i.ToString() + " " + j.ToString() + IsInLineOfSight(pos_x_car, pos_z_car, i, j).ToString());
                            if (IsInLineOfSight(pos_x_car, pos_z_car, i, j) == true)
                            {
                                map[i, j] = 2;
                            }
                        }
                    }
                }
            }

            int[] pos_x_z_random;
            int[,] posToVisit = new int[2, 2];
            posToVisit[0, 0] = pos_x_car;
            posToVisit[0, 1] = pos_z_car;
            int[,] posToVisit2;
            String tab;
            while (IsAllMapCovered(map) == false)
            {
                pos_x_z_random = PickRandomUncoveredInMap(map);
                map[pos_x_z_random[0], pos_x_z_random[1]] = 2;
                for (i = 0; i < map.GetLength(0); i++)
                {
                    for (j = 0; j < map.GetLength(1); j++)
                    {
                        if (pos_x_z_random[0] != i || pos_x_z_random[1] != j)
                        {
                            if (map[i, j] == 0)
                            {
                                if (IsInLineOfSight(pos_x_z_random[0], pos_x_z_random[1], i, j) == true)
                                {
                                    map[i, j] = 2;
                                }
                            }
                        }
                    }
                }

                posToVisit[posToVisit.GetLength(0) - 1, 0] = pos_x_z_random[0];
                posToVisit[posToVisit.GetLength(0) - 1, 1] = pos_x_z_random[1];
                posToVisit2 = posToVisit;
                posToVisit = new int[posToVisit2.GetLength(0) + 1, 2];
                for (i = 0; i < posToVisit2.GetLength(0); i++)
                {
                    posToVisit[i, 0] = posToVisit2[i, 0];
                    posToVisit[i, 1] = posToVisit2[i, 1];
                }
                tab = "";
                for (i = 0; i < posToVisit.GetLength(0); i++)
                {
                    tab = tab + " (" + posToVisit[i, 0] + "," + posToVisit[i, 1] + ")";
                }

                //Debug.Log(tab + "    " + posToVisit.GetLength(0).ToString());
            }

            /* Using Problem 3 Solution */

            float[,] distance_graph = new float[posToVisit.GetLength(0) - 1, posToVisit.GetLength(0) - 1];
            for (i = 0; i < distance_graph.GetLength(0); i++)
            {
                for (j = 0; j < distance_graph.GetLength(1); j++)
                {
                    if (i == j)
                        distance_graph[i, j] = 0;
                    else
                        distance_graph[i, j] = DistanceWithAStar(posToVisit[i, 0], posToVisit[i, 1], posToVisit[j, 0], posToVisit[j, 1]);
                }
            }
            tab = "";
            for (i = 0; i < distance_graph.GetLength(0); i++)
            {
                for (j = 0; j < distance_graph.GetLength(1); j++)
                {
                    tab = tab + distance_graph[i, j].ToString() + "+";
                }
                tab = tab + "\n";
            }
            //Debug.Log(tab);

            gen.initialisation(3, distance_graph, posToVisit);
            for (i = 0; i < number_of_gen; i++)
            {
                gen.nextPopulation();
            }
            tab = "";
            //Debug.Log(i + " : bestscore = " + gen.bestScore);
            for (i = 0; i < gen.orderBestScore.GetLength(0); i++)
            {
                tab = tab + " (" + gen.orderBestScore[i, 0] + "," + gen.orderBestScore[i, 1] + ") ";
            }
            //Debug.Log(tab);
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
            String text = "";
            for (i = 0; i < tab.GetLength(0); i++)
            {
                for (j = 0; j < tab.GetLength(1); j++)
                {
                    text = text + " " + tab[i, j, 2];
                }
                text = text + "\n";
            }
            //Debug.Log(text);
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

        private int[] PickRandomUncoveredInMap(float[,] map)
        {
            int[] pos = new int[2];
            int count = 0;
            int i, j;
            for (i = 0; i < map.GetLength(0); i++)
            {
                for (j = 0; j < map.GetLength(1); j++)
                {
                    if (map[i, j] == 0)
                    {
                        count = count + 1;
                    }
                }
            }

            int[,] remainingUncovered = new int[count, 2];
            count = 0;
            for (i = 0; i < map.GetLength(0); i++)
            {
                for (j = 0; j < map.GetLength(1); j++)
                {
                    if (map[i, j] == 0)
                    {
                        remainingUncovered[count, 0] = i;
                        remainingUncovered[count, 1] = j;
                        count = count + 1;
                    }
                }
            }

            int posRandom = (int)(System.Math.Floor((double)(rand.NextDouble() * remainingUncovered.GetLength(0))));

            pos[0] = remainingUncovered[posRandom, 0];
            pos[1] = remainingUncovered[posRandom, 1];

            return pos;
        }

        private bool IsAllMapCovered(float[,] map)
        {
            bool isCovered = true;
            int i, j;
            for (i = 0; i < map.GetLength(0) && isCovered == true; i++)
            {
                for (j = 0; j < map.GetLength(1) && isCovered == true; j++)
                {
                    if (map[i, j] == 0)
                    {
                        isCovered = false;
                    }
                }
            }
            return isCovered;
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


        private void FixedUpdate()
        {
            //Debug.Log("path length: "+ path.Count);
            Debug.Log( transform.name +  ", Next index: "+ nextIndex);
            // driving stuff to put here.. points to go to are in the list path (every point is in line of sight with the previous one).
            Vector3 currentPos = transform.position;
            Vector3 nextPos = new Vector3( path[nextIndex][0], 0, path[nextIndex][1] );
            float acc = 5f;

            if (m_Car.CurrentSpeed > 20f)
                acc = 0f;

            float steeringAngel = CalculateTurnAngle(currentPos, nextPos, maxSteer, out angle);
            //decide turning direction: Vector3.SignedAngle
            float heading = Vector3.Angle(transform.forward, new Vector3(-1, 0, 0));
            if (transform.forward[2] < 0f) heading = -heading;
            float pdirection = calculate_angel(currentPos[0], currentPos[2], nextPos[0], nextPos[2]);
            float flag_turn = turnClockwise(heading, pdirection);

            if (flag_turn == 1f)
                steeringAngel = Math.Abs(steeringAngel);
            else
                steeringAngel = -Math.Abs(steeringAngel);

            if (crashed)
            {
                Debug.Log("!!!!!##################!!!!!!!!!!crashed!!!!!!!!#################!!!!!!");

                if (backAngel == 0f)
                    backAngel = steeringAngel;
                m_Car.Move(-backAngel, 0, -1f, 0);


                currentForward = transform.forward;
                currentSpeed = m_Car.CurrentSpeed;
           
            }
            else
            {
                backAngel = 0f;
                if (nextIndex == path.Count)
                    m_Car.Move(0, 0, 1f, 1f);
                else
                    m_Car.Move(steeringAngel, acc, 1f, 0f);
            }

            if (Math.Abs(transform.position[0] - nextPos[0]) < 2f && Math.Abs(transform.position[2] - nextPos[2]) < 2f)  // 6f
            {  //1f can be changed ?
                if (isVisited[nextIndex] == 0)
                {
                    Debug.Log(transform.name + "******************Hit next position******************!" + nextIndex);
                    //path[nextIndex].angel += HeadingChange;
                    isVisited[nextIndex] = 1;
                    nextIndex += 1;
                    currentForward = transform.forward;
                    currentSpeed = m_Car.CurrentSpeed;
                }
            }


        }


        IEnumerator DidWeCrash()
        {
            yield return new WaitForSeconds(2f);
            Vector3 myPosition = transform.position;
            while (!goalReached)
            {
                yield return new WaitForSeconds(1f);
                if (((myPosition - transform.position).magnitude < 0.5f && !crashed) )  // original is 0.5
                {
                    crashed = true;
                    crashCounter++;
                    yield return new WaitForSeconds(1.5f); // 0.8
                    crashed = false;
                    yield return new WaitForSeconds(1.2f);
                }
                else
                {
                    crashCounter = 0;
                }
                myPosition = transform.position;
            }
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

        void drawLine(List<Vector2> allPaths)
        {
            Color color = new Color();
            //Debug.Log(string.Format("Nums of points:{0}", path.Count));
            for (int i = 0; i < allPaths.Count-1; i++)
            {
                if (transform.name.Equals("ArmedCar (1)"))
                    color = Color.red;
                if (transform.name.Equals("ArmedCar (2)"))
                    color = Color.green;
                if (transform.name.Equals("ArmedCar (3)"))
                    color = Color.yellow;

                Vector2 n1 = allPaths[i];
                Vector2 n2 = allPaths[i+ 1];
                Vector3 p1 = new Vector3(n1[0], 0, n1[1]);
                Vector3 p2 = new Vector3(n2[0], 0, n2[1]);
                Debug.DrawLine(p1, p2, color, 10000f);



            }

        }


    }
}
