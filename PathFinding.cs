using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathFinding : MonoBehaviour
{

    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;

    public GameObject[] friends;
    public GameObject[] enemies;


    int D = 10;
    public List<List<Node>> allPaths = new List<List<Node>>();
    public float[,] newMap;
    public bool[,] isVisited;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void initialize() {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        newMap = terrain_manager.myInfo.traversability;

        int m = newMap.GetLength(0);
        int n = newMap.GetLength(1);
        isVisited = new bool[m, n];
        for(int i = 0; i<m; i++) {
            for (int j = 0; j < n; j++)
                isVisited[i, j] = false;
        }

        friends = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject friend in friends)
        {

            List<Node> friendPath = new List<Node>();
            Vector2 tmp = new Vector2( terrain_manager.myInfo.get_i_index(friend.transform.position[0]), terrain_manager.myInfo.get_j_index(friend.transform.position[2]));
            Node tmpNode = new Node((int)tmp[0], (int)tmp[1]);
            friendPath.Add(tmpNode);
            Debug.Log(string.Format("initial position of {0} is: {1},{2}", friend.transform.name, tmp[0], tmp[1] ));
            allPaths.Add(friendPath);

        }
    }


    Vector2 getNewPositions(Vector3 iniRobotsPosition, int D)
    {
        Vector2 newPos = new Vector2(0, 0);
        int m = get_m_index(iniRobotsPosition[0], D);
        int n = get_n_index(iniRobotsPosition[2], D);
        newPos[0] = m;
        newPos[1] = n;

        return newPos;
    }
    // function: transform original map into : 2D * 2D, index starts from (0,0), indexed by m,n
    float[,] transformMap(float[,] originalMap, int D)
    {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        int m = (int)(terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low) / (2 * D);
        int n = (int)(terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low) / (2 * D);
        float[,] newMap = new float[m, n];

        float startX = terrain_manager.myInfo.x_low + D;
        float startZ = terrain_manager.myInfo.z_low + D;
        int starti = terrain_manager.myInfo.get_i_index(startX);
        int startj = terrain_manager.myInfo.get_j_index(startZ);
        if (originalMap[starti, startj] == 1.0f)
            newMap[0, 0] = 1.0f;
        else
            newMap[0, 0] = 0.0f;

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == 0 && j == 0) continue;
                // detect if there is obstacle in new grid *************???????********* WRONG!
                float newX = startX + i * 2 * D;
                float newZ = startZ + j * 2 * D;
                int nexti = terrain_manager.myInfo.get_i_index(newX);
                int nextj = terrain_manager.myInfo.get_j_index(newZ);
                if (originalMap[nexti, nextj] == 1.0f)
                    newMap[i, j] = 1.0f;
                else
                    newMap[i, j] = 0.0f;
            }
        }
        return newMap;
    }

    // ******** create spanning tree *************
    public void createTree()
    {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


        // note that both arrays will have holes when objects are destroyed
        // but for initial planning they should work
        friends = GameObject.FindGameObjectsWithTag("Player");
        enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float[,] originalMap = terrain_manager.myInfo.traversability;
        //float[,] newMap = transformMap(originalMap, D);
        float[,] newMap = terrain_manager.myInfo.traversability;
        int rows = newMap.GetLength(0);
        int cols = newMap.GetLength(1);

        //int[] gapI = { 0, 0, -1, 1 };
        //int[] gapJ = { 1, -1, 0, 0 };  //up down left right  max len : 142

        int[] gapI = { 0, 0, 1, -1 };
        int[] gapJ = { 1, -1, 0, 0 }; //up down right left   max len: 116 

        //int[] gapI = { 1, 0, 0, -1 };
        //int[] gapJ = { 0, 1, -1, 0 };
        int countValid = 0;   // if any car succed in expanding its path, add 1

        foreach (List<Node> path in allPaths) {
            Node currentNode = path[path.Count - 1];
            Vector2 startPos = new Vector2 (path[path.Count - 1].i, path[path.Count - 1].j);
            //Debug.Log(string.Format("start position: {0}, {1}", startPos[0], startPos[1]));
            int optDis = -1;
            int maxMinDis = 0; 
            Vector2 optPos = new Vector2(0, 0);

            int flag = 0;

            for (int i = 0; i < 4; i++)
            {
                int minDis = 0xffffff;
                int distance = 0;

                Vector2 nextPos = new Vector2(startPos[0] + gapI[i], startPos[1] + gapJ[i]);
                // check bounds
                if (nextPos[0] < 0 || nextPos[0] >= rows || nextPos[1] < 0 || nextPos[0] >= cols || isVisited[(int)nextPos[0], (int)nextPos[1]] == true || newMap[(int)nextPos[0], (int)nextPos[1]] == 1.0f)
                    continue;
                foreach (List<Node> friendPath in allPaths)
                {

                    Vector2 lastPos = new Vector2(friendPath[friendPath.Count - 1].i, friendPath[friendPath.Count - 1].j);
                    int diff = ((int)Math.Abs(lastPos[0] - nextPos[0]) + (int)Math.Abs(lastPos[1] - nextPos[1]));
                    distance += diff;
                    if(diff < minDis)
                        minDis = diff;
                }

                if (distance > optDis)
                {
                    flag = 1;
                    optDis = distance;
                    optPos = nextPos;
                    if (minDis > maxMinDis)
                        maxMinDis = minDis;
                }
                if(distance == optDis) {
                    flag = 1;
                    if(minDis > maxMinDis) {
                        maxMinDis = minDis;
                        optPos = nextPos;
                    }
                }
            }

          
            if ( flag == 1 && isVisited[(int)optPos[0], (int)optPos[1]] == false )
            {
                isVisited[(int)optPos[0], (int)optPos[1]] = true;
                Node nextNode = new Node((int)optPos[0], (int)optPos[1]);
                path.Add(nextNode);
                currentNode.son.Add(nextNode);
                //Debug.Log(string.Format("added position: {0}, {1}", optPos[0], optPos[1])) ;
                countValid += 1;
            }
       
        }
        if (countValid == 0) return;
        else
            createTree();


    }

    public void hilling() {
        int oldCount = 0;
        int newCount = 0;
        foreach (List<Node> path in allPaths)
            oldCount += path.Count;


        foreach (List<Node> path in allPaths) { 
          for(int i = 0; i< path.Count-1; ) {
                Node current1 = path[i];
                Node current2 = path[i + 1];
                Vector2 pos1 =  new Vector2((int)path[i].i, (int)path[i].j);
                Vector2 pos2 = new Vector2((int)path[i+1].i, (int)path[i+1].j);
                if (pos1[1] == pos2[1]) {   // same row
                    int flag = 0;

                    // look  up
                    Vector2 up1 = new Vector2(pos1[0], pos1[1]+1);
                    Vector2 up2 = new Vector2(pos2[0], pos2[1] + 1);
                    if(checkBounds(up1) && checkBounds(up2) && flag == 0) {
                        Node n1 = new Node((int)up1[0], (int)up1[1]);
                        Node n2 = new Node((int)up2[0], (int)up2[1]);
                        path.Insert(i + 1, n1);
                        path.Insert(i + 2, n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)up1[0], (int)up1[1]] = true;
                        isVisited[(int)up2[0], (int)up2[1]] = true;
                        flag = 1;
                    }

                    //look down
                    Vector2 dn1 = new Vector2(pos1[0], pos1[1] - 1);
                    Vector2 dn2 = new Vector2(pos2[0], pos2[1] - 1);
                    if (checkBounds(dn1) && checkBounds(dn2) && flag == 0)
                    {
                        Node n1 = new Node((int)dn1[0], (int)dn1[1]);
                        Node n2 = new Node((int)dn2[0], (int)dn2[1]);
                        path.Insert(i + 1, n1);
                        path.Insert(i + 2, n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)dn1[0], (int)dn1[1]] = true;
                        isVisited[(int)dn2[0], (int)dn2[1]] = true;
                        flag = 1;
                    }

                    if (flag == 1) i += 4;
                    else i += 1;
                }
                if(pos1[0] == pos2[0]) { // same col
                    int flag = 0;

                    // look  left
                    Vector2 le1 = new Vector2(pos1[0]-1, pos1[1]);
                    Vector2 le2 = new Vector2(pos2[0]-1, pos2[1]);
                    if (checkBounds(le1) && checkBounds(le2) && flag == 0)
                    {
                        Node n1 = new Node((int)le1[0], (int)le1[1]);
                        Node n2 = new Node((int)le2[0], (int)le2[1]);
                        path.Insert(i + 1, n1);
                        path.Insert(i + 2, n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)le1[0], (int)le1[1]] = true;
                        isVisited[(int)le2[0], (int)le2[1]] = true;
                        flag = 1;
                    }

                    //look down
                    Vector2 r1 = new Vector2(pos1[0] +1, pos1[1]);
                    Vector2 r2 = new Vector2(pos2[0] +1, pos2[1]);
                    if (checkBounds(r1) && checkBounds(r2) && flag == 0)
                    {
                        Node n1 = new Node((int)r1[0], (int)r1[1]);
                        Node n2 = new Node((int)r2[0], (int)r2[1]);
                        path.Insert(i+1,n1);
                        path.Insert(i+2,n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)r1[0], (int)r1[1]] = true;
                        isVisited[(int)r2[0], (int)r2[1]] = true;
                        flag = 1;
                    }


                    if (flag == 1) i += 4;
                    else i += 1;
                }
            }
        }
        foreach (List<Node> path in allPaths)
            newCount += path.Count;

        if (oldCount == newCount) return;
        else
            hilling();
            //return;

    }

    public void expandTree() {
        int flag = 0;
        int[] gapI = { 0, 0, -1, 1 };
        int[] gapJ = { 1, -1, 0, 0 };
        foreach (List<Node> path in allPaths) { 
            for(int i = 0; i<path.Count; ) {
                Node currentNode = path[i];
                Vector2 currentPos = new Vector2((int)path[i].i , (int)path[i].j);
                for(int k = 0; k < 4; k++) {
                    Vector2 newPos = new Vector2(currentPos[0] + gapI[k], currentPos[1] + gapJ[k]);
                    if(checkBounds(newPos) == true) {
                        isVisited[(int)newPos[0], (int)newPos[1]] = true;
                        Node newNode = new Node((int)newPos[0], (int)newPos[1]);
                        path.Insert(i+1,newNode);
                        currentNode.son.Add(newNode);  // lower proprity
                        flag = 1;
                      
                    }
                }
                if (flag == 1) i += 2;
                else i += 1;
            }
        }

        if (flag == 0) return;
        else
            expandTree();
    }

    bool checkBounds(Vector2 pos) {

        if (pos[0] < 0 || pos[0] >= newMap.GetLength(0) || pos[1] < 0 || pos[1] >= newMap.GetLength(1) || isVisited[(int)pos[0], (int)pos[1]] == true ||newMap[(int)pos[0], (int)pos[1]] == 1.0f)
            return false;
        else
            return true;
     }

    int get_m_index(float x, int D)
    {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        return (int)Math.Floor((x - terrain_manager.myInfo.x_low) / (2 * D));
    }

    int get_n_index(float z, int D)
    {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        return (int)Math.Floor((z - terrain_manager.myInfo.z_low) / (2 * D));
    }





}
