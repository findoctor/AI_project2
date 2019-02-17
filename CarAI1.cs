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

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject pathPlanner;
        PathFinding pathManager;

        public GameObject[] friends;
        public GameObject[] enemies;

        List<List<Vector2>> allPaths = new List<List<Vector2>>();
        void OnDrawGizmos()
        {
            Color color = new Color();
            //Debug.Log(string.Format("Nums of points:{0}", path.Count));
            for (int i = 0; i < allPaths.Count; i++)
            {
                if (i == 0)
                    Gizmos.color = Color.red;
                if (i == 1)
                    Gizmos.color = Color.green;
                if (i == 2)
                    Gizmos.color = Color.blue;
                // draw last position
                Vector3 currentPos = new Vector3(terrain_manager.myInfo.get_x_pos((int)allPaths[i][allPaths[i].Count-1][0]), 0, terrain_manager.myInfo.get_z_pos((int)allPaths[i][allPaths[i].Count - 1][1]));
                Gizmos.DrawSphere(currentPos, 1f);
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
                pathManager.hilling();
            }


            this.allPaths =  pathManager.allPaths;
            drawLine(allPaths);
            


            Debug.Log(string.Format("lastPosition: {0},{1}", allPaths[0][allPaths[0].Count - 1][0], allPaths[0][allPaths[0].Count - 1][1] ) );
            Debug.Log(string.Format("lastPosition: {0},{1}", allPaths[1][allPaths[1].Count - 1][0], allPaths[1][allPaths[1].Count - 1][1]) );
            Debug.Log(string.Format("lastPosition: {0},{1}", allPaths[2][allPaths[2].Count - 1][0], allPaths[2][allPaths[2].Count - 1][1]) );


            Debug.Log(allPaths[0].Count);
            Debug.Log(allPaths[1].Count);
            Debug.Log(allPaths[2].Count);


            //Debug.Log(newMap[newRobotsPosition[0], newRobotsPosition[1]]);

        }


        private void FixedUpdate()
        {


            // Execute your path here
            // ...

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


        }

        void drawLine(List<List<Vector2>> allPaths)
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
                for (int k = 0; k < allPaths[i].Count - 1; k++)
                {
                    Vector3 currentPos = new Vector3( terrain_manager.myInfo.get_x_pos( (int)allPaths[i][k][0]), 0, terrain_manager.myInfo.get_z_pos((int)allPaths[i][k][1] ));
                    Vector3 nextPos = new Vector3( terrain_manager.myInfo.get_x_pos((int)allPaths[i][k+1][0]), 0, terrain_manager.myInfo.get_z_pos((int)allPaths[i][k+1][1]));
                    //Debug.Log(string.Format("drawing point: {0},{1}", currentPos[0], currentPos[2]));
                    //Debug.Log(string.Format("drawing point: {0},{1}", nextPos[0], nextPos[2]));
                    Debug.DrawLine(currentPos, nextPos, color, 10000f);
                }

            }

        }


    }
}
