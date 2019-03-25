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
    public List<List<Vector2>> clockwisePaths = new List<List<Vector2> >();
    public float[,] newMap;
    public bool[,] isVisited;
    public int[,] lined;
    List<Vector3> initialDirections = new List<Vector3> { new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(0, 0, 1) };

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
        lined = new int[(int)terrain_manager.myInfo.x_high, (int)terrain_manager.myInfo.z_high];

        int m = newMap.GetLength(0);
        int n = newMap.GetLength(1);
        isVisited = new bool[m, n];
        for(int i = 0; i<m; i++) {
            for (int j = 0; j < n; j++)
                isVisited[i, j] = false;
        }
        for(int i =0; i< lined.GetLength(0); i++) {
            for (int j = 0; j< lined.GetLength(1); j++)
                lined[i, j] = 0;
        }

        friends = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject friend in friends)
        {

            List<Node> friendPath = new List<Node>();
            List<Vector2> finalPath = new List<Vector2>();

            float startX = friend.transform.position[0] ;
            float startZ = friend.transform.position[2] ;
            float centerX = terrain_manager.myInfo.get_x_pos( terrain_manager.myInfo.get_i_index(startX) );
            float centerZ = terrain_manager.myInfo.get_z_pos( terrain_manager.myInfo.get_j_index(startZ));


            if(friend.transform.name == "ArmedCar (2)") {
                startX -= 20;
                startZ += 15;
                centerX = terrain_manager.myInfo.get_x_pos(terrain_manager.myInfo.get_i_index(startX));
                centerZ = terrain_manager.myInfo.get_z_pos(terrain_manager.myInfo.get_j_index(startZ));
            }


            if (startX > centerX) {
                if (startZ > centerZ)
                    finalPath.Add(new Vector2( centerX+5f, centerZ+5f ));
                else
                    finalPath.Add(new Vector2(centerX + 5f, centerZ - 5f));
            }
            else
            {
                if (startZ > centerZ)
                    finalPath.Add(new Vector2(centerX - 5f, centerZ + 5f));
                else
                    finalPath.Add(new Vector2(centerX - 5f, centerZ - 5f));
            }

            clockwisePaths.Add(finalPath);


            Vector2 tmp = new Vector2( terrain_manager.myInfo.get_i_index(friend.transform.position[0]), terrain_manager.myInfo.get_j_index(friend.transform.position[2]));

            if (friend.transform.name == "ArmedCar (2)") {
                tmp = new Vector2(tmp[0] - 1, tmp[1]);
            }


            Node tmpNode = new Node((int)tmp[0], (int)tmp[1]);
            friendPath.Add(tmpNode);
            isVisited[ (int)tmpNode.i, (int)tmpNode.j ] = true;
                     
            Debug.Log(string.Format("initial position of {0} is: {1},{2}", friend.transform.name, tmp[0], tmp[1] ));
            allPaths.Add(friendPath);

        }
        /*
        Vector2 removed = clockwisePaths[0][0];
        clockwisePaths[0].RemoveAt(0);
        Vector2 newStart = new Vector2(removed[0]-10, removed[0]);
        clockwisePaths[0].Add(newStart);
        */      
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

        int[] gapI = { 0, 0, -1, 1 };
        int[] gapJ = { 1, -1, 0, 0 };  //up down left right  max len : 142

        //int[] gapI = { 0, 0, 1, -1 };
        //int[] gapJ = { 1, -1, 0, 0 }; //up down right left   max len: 116 

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
            int direction = 0;  // determint up, down, left, right

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
                    direction = i;
                }
                if(distance == optDis) {
                    flag = 1;
                    if(minDis > maxMinDis) {
                        maxMinDis = minDis;
                        optPos = nextPos;
                        direction = i;
                    }
                }
            }

          
            if ( flag == 1 && isVisited[(int)optPos[0], (int)optPos[1]] == false )
            {
                isVisited[(int)optPos[0], (int)optPos[1]] = true;
                Node nextNode = new Node((int)optPos[0], (int)optPos[1]);
                path.Add(nextNode);
                currentNode.son.Add(nextNode);
                nextNode.par = currentNode;
                //Debug.Log(string.Format("added position: {0}, {1}", optPos[0], optPos[1])) ;
                countValid += 1;

                // build lined matrix:
                float x1 = terrain_manager.myInfo.get_x_pos( currentNode.i );
                float z1 = terrain_manager.myInfo.get_z_pos(currentNode.j);
                float x2 = terrain_manager.myInfo.get_x_pos(nextNode.i);
                float z2 = terrain_manager.myInfo.get_z_pos(nextNode.j);

                if(direction == 0) {
                    for (int z = (int)z1; z <= z2; z++)
                        lined[(int)x1, (int)z] = 1;
                }
                if (direction == 1)
                {
                    for (int z = (int)z2; z <= z1; z++)
                        lined[(int)x1, (int)z] = 1;
                }
                if(direction == 2) {
                    for (int x = (int)x2; x <= x1; x++)
                        lined[(int)x, (int)z1] = 1;
                }
                if (direction == 3)
                {
                    for (int x = (int)x1; x <= x2; x++)
                        lined[(int)x, (int)z1] = 1;
                }

            }

            if (flag == 0) {  // if no where to go, hilling!
                //int hillRes = hilling(path);
                //countValid += hillRes;
                /*
                if(hillRes == 0) {   // if hilling faila, we expand
                    countValid += expandTree(path);
                }
                */

                countValid += expandTree(path);

            }



        }
        //Debug.Log( string.Format("The length of countValid: {0}", countValid) );
        if (countValid == 0) return;
        else
            createTree();


    }

    public int hilling(List<Node> path) {
        int flag = 0;

        for (int i = path.Count - 1; i>0 ; ) {
                Node current1 = path[i-1];
                Node current2 = path[i];
                Vector2 pos1 =  new Vector2((int)path[i-1].i, (int)path[i-1].j);
                Vector2 pos2 = new Vector2((int)path[i].i, (int)path[i].j);
                if (pos1[1] == pos2[1]) {   // same row
                    

                    // look  up
                    Vector2 up1 = new Vector2(pos1[0], pos1[1]+1);
                    Vector2 up2 = new Vector2(pos2[0], pos2[1] + 1);
                    if(checkBounds(up1) && checkBounds(up2) && flag == 0) {
                        Node n1 = new Node((int)up1[0], (int)up1[1]);
                        Node n2 = new Node((int)up2[0], (int)up2[1]);
                        path.Insert(i, n1);
                        path.Insert(i + 1, n2);
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
                        path.Insert(i, n1);
                        path.Insert(i + 1, n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)dn1[0], (int)dn1[1]] = true;
                        isVisited[(int)dn2[0], (int)dn2[1]] = true;
                        flag = 1;
                    }

                    if (flag == 1) return flag;
                    else i -= 1;
            }
                if(pos1[0] == pos2[0]) { // same col
                    
                    // look  left
                    Vector2 le1 = new Vector2(pos1[0]-1, pos1[1]);
                    Vector2 le2 = new Vector2(pos2[0]-1, pos2[1]);
                    if (checkBounds(le1) && checkBounds(le2) && flag == 0)
                    {
                        Node n1 = new Node((int)le1[0], (int)le1[1]);
                        Node n2 = new Node((int)le2[0], (int)le2[1]);
                        path.Insert(i , n1);
                        path.Insert(i + 1, n2);
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
                        path.Insert(i,n1);
                        path.Insert(i+1,n2);
                        current1.son.Remove(current2);
                        current1.son.Add(n1);
                        n1.son.Add(n2);
                        n2.son.Add(current2);
                        isVisited[(int)r1[0], (int)r1[1]] = true;
                        isVisited[(int)r2[0], (int)r2[1]] = true;
                        flag = 1;
                    }


                    if (flag == 1) return flag;
                    else i -= 1;
                }
            }
        return 0;
       

    }

    public int expandTree(List<Node> path) {
        int flag = 0;
        int[] gapI = { 0, 0, -1, 1 };
        int[] gapJ = { 1, -1, 0, 0 };

            for(int i = path.Count-1; i>=0; i--) {
                Node currentNode = path[i];
                Vector2 currentPos = new Vector2((int)path[i].i , (int)path[i].j);
                int direction = 0;
                for(int k = 0; k < 4; k++) {
                    Vector2 newPos = new Vector2(currentPos[0] + gapI[k], currentPos[1] + gapJ[k]);
                    if(checkBounds(newPos) == true) {
                        direction = k;
                        isVisited[(int)newPos[0], (int)newPos[1]] = true;
                        Node newNode = new Node((int)newPos[0], (int)newPos[1]);
                        path.Insert(i+1,newNode);
                        currentNode.son.Add(newNode);  // lower proprity
                        newNode.par = currentNode;
                    float x1 = terrain_manager.myInfo.get_x_pos(currentNode.i);
                    float z1 = terrain_manager.myInfo.get_z_pos(currentNode.j);
                    float x2 = terrain_manager.myInfo.get_x_pos(newNode.i);
                    float z2 = terrain_manager.myInfo.get_z_pos(newNode.j);

                    if (direction == 0)
                    {
                        for (int z = (int)z1; z <= z2; z++)
                            lined[(int)x1, (int)z] = 1;
                    }
                    if (direction == 1)
                    {
                        for (int z = (int)z2; z <= z1; z++)
                            lined[(int)x1, (int)z] = 1;
                    }
                    if (direction == 2)
                    {
                        for (int x = (int)x2; x <= x1; x++)
                            lined[(int)x, (int)z1] = 1;
                    }
                    if (direction == 3)
                    {
                        for (int x = (int)x1; x <= x2; x++)
                            lined[(int)x, (int)z1] = 1;
                    }

                    return 1;
                      
                    }
                }
            }


        return flag;
    }


    Vector3 nextDirection(Vector3 currentDirection, int turn) {
        Vector3 nextDirection = new Vector3();
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 down = new Vector3(0, 0, -1);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);

        if (currentDirection == up) {
            if (turn == 0)
                nextDirection = right;
            if (turn == 1)
                nextDirection = up;
            if (turn == 2)
                nextDirection = left;
        }

        if (currentDirection == down) {
            if (turn == 0)
                nextDirection = right;
            if (turn == 1)
                nextDirection = down;
            if (turn == 2)
                nextDirection = left;
        }

        if (currentDirection == left) {
            if (turn == 0)
                nextDirection = up;
            if (turn == 1)
                nextDirection = left;
            if (turn == 2)
                nextDirection = down;
        }

        if (currentDirection== right) {
            if (turn == 0)
                nextDirection = down;
            if (turn == 1)
                nextDirection = right;
            if (turn == 2)
                nextDirection = up;
        }
        return nextDirection;
    }

    public void clockwise() {
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 down = new Vector3(0, 0, -1);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);
        for (int carNumber = 0; carNumber< 3; carNumber++) {
            List<Vector2> finalPath = clockwisePaths[carNumber];
            Vector3 direction = initialDirections[carNumber];
            Vector2 startPosition = finalPath[0];
            Vector2 lastPosition = new Vector2(0, 0);
            while(lastPosition != startPosition) {
                lastPosition = finalPath[finalPath.Count-1];
                Vector3 oldDirection = direction;
                int rfl = detectLine(ref direction, lastPosition);
                if(oldDirection == up) { 
                    if(rfl == 0) {
                        Vector2 newPosition = new Vector2(lastPosition[0]+10 ,  lastPosition[1]);
                        if(checkBoundsXZ(newPosition[0], newPosition[1]) )
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1]+10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] - 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }
                if(oldDirection == down) { 
                    if(rfl == 0) {
                        Vector2 newPosition = new Vector2(lastPosition[0]-10 ,  lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1]-10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] + 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                if (oldDirection == left)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] , lastPosition[1]+10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0]-10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1]-10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                if (oldDirection == right)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] , lastPosition[1]-10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0]+10, lastPosition[1] );
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] , lastPosition[1]+10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                lastPosition = finalPath[finalPath.Count - 1];

            }
        }
        Debug.Log(string.Format("The length of 3 final path:{0}, {1}, {2}", clockwisePaths[0].Count, clockwisePaths[1].Count, clockwisePaths[2].Count ));
    }


    public void clockwise2()
    {
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 down = new Vector3(0, 0, -1);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);
        for (int carNumber = 0; carNumber < 3; carNumber++)
        {
            List<Vector2> finalPath = clockwisePaths[carNumber];
            Vector3 direction = initialDirections[carNumber];
            Vector2 startPosition = finalPath[0];
            Vector2 lastPosition = new Vector2(0, 0);
            while (lastPosition != startPosition)
            {
                lastPosition = finalPath[finalPath.Count - 1];
                Vector3 oldDirection = direction;
                int rfl = detectLine2(ref direction, lastPosition);
                if (oldDirection == up)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] - 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] + 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] + 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }
                if (oldDirection == down)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] + 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] - 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] - 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                if (oldDirection == left)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] - 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] - 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] + 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                if (oldDirection == right)
                {
                    if (rfl == 0)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] + 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 1)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0] + 10, lastPosition[1]);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                    if (rfl == 2)
                    {
                        Vector2 newPosition = new Vector2(lastPosition[0], lastPosition[1] - 10);
                        if (checkBoundsXZ(newPosition[0], newPosition[1]))
                            finalPath.Add(newPosition);
                    }
                }

                lastPosition = finalPath[finalPath.Count - 1];

            }
        }
        //Debug.Log(string.Format("The length of 3 final path:{0}, {1}, {2}", clockwisePaths[0].Count, clockwisePaths[1].Count, clockwisePaths[2].Count));
    }



    int detectLine( ref Vector3 currentDirection, Vector2 position) {
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 down = new Vector3(0, 0, -1);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);
        int flag = -1;  // 0: Right  1: up  2:left

        if(currentDirection == up) {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn<3; turn++) { 
                if(turn == 0) {
                    float rightX1 = currentX + 4f;
                    float rightZ1 = currentZ;
                    float rightX2 = currentX + 5f;
                    float rightZ2 = currentZ;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX + 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 0;
                        }
                    }

                }

                if(turn == 1) {
                    float forwardX1 = currentX;
                    float forwardZ1 = currentZ + 4f;
                    float forwardX2 = currentX;
                    float forwardZ2 = currentZ + 5f;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ+10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 1;
                        }
                    }
                }

                if(turn == 2) {
                    float leftX1 = currentX - 4f;
                    float leftZ1 = currentZ;
                    float leftX2 = currentX - 5f;
                    float leftZ2 = currentZ;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 2;
                        }
                      
                    }
                }

            }
        }

        if (currentDirection == down)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX - 4f;
                    float rightZ1 = currentZ;
                    float rightX2 = currentX - 5f;
                    float rightZ2 = currentZ;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 0;
                        }
                       
                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX;
                    float forwardZ1 = currentZ - 4f;
                    float forwardX2 = currentX;
                    float forwardZ2 = currentZ - 5f;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ-10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 1;
                        }
                   
                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX + 4f;
                    float leftZ1 = currentZ;
                    float leftX2 = currentX + 5f;
                    float leftZ2 = currentZ;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX + 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 2;
                        }
                       
                    }
                }

            }
        }

        if (currentDirection == left)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX;
                    float rightZ1 = currentZ +4f;
                    float rightX2 = currentX;
                    float rightZ2 = currentZ+5f;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ+10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 0;
                        }

                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX -4f;
                    float forwardZ1 = currentZ;
                    float forwardX2 = currentX-5f;
                    float forwardZ2 = currentZ;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 1;
                        }
                      
                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX;
                    float leftZ1 = currentZ-4f;
                    float leftX2 = currentX;
                    float leftZ2 = currentZ-5f;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ - 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 2;
                        }
                       
                    }
                }

            }
        }

        if (currentDirection == right)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX;
                    float rightZ1 = currentZ-4f;
                    float rightX2 = currentX;
                    float rightZ2 = currentZ-5f;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ - 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 0;
                        }

                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX + 4f;
                    float forwardZ1 = currentZ ;
                    float forwardX2 = currentX + 5f;
                    float forwardZ2 = currentZ ;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX+10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 1;
                        }
                
                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX;
                    float leftZ1 = currentZ+4f;
                    float leftX2 = currentX;
                    float leftZ2 = currentZ+5f;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ + 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 2;
                        }
                    
                    }
                }

            }
        }
        return flag;

    }

    int detectLine2(ref Vector3 currentDirection, Vector2 position)
    {
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 down = new Vector3(0, 0, -1);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);
        int flag = -1;  // 0: left  1: up  2:right

        if (currentDirection == up)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX - 4f;
                    float rightZ1 = currentZ;
                    float rightX2 = currentX - 5f;
                    float rightZ2 = currentZ;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 0;
                        }
                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX;
                    float forwardZ1 = currentZ + 4f;
                    float forwardX2 = currentX;
                    float forwardZ2 = currentZ + 5f;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ + 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 1;
                        }
                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX + 4f;
                    float leftZ1 = currentZ;
                    float leftX2 = currentX + 5f;
                    float leftZ2 = currentZ;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX + 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 2;
                        }

                    }
                }

            }
        }

        if (currentDirection == down)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX + 4f;
                    float rightZ1 = currentZ;
                    float rightX2 = currentX + 5f;
                    float rightZ2 = currentZ;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX + 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 0;
                        }

                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX;
                    float forwardZ1 = currentZ - 4f;
                    float forwardX2 = currentX;
                    float forwardZ2 = currentZ - 5f;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ - 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 1;
                        }

                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX - 4f;
                    float leftZ1 = currentZ;
                    float leftX2 = currentX - 5f;
                    float leftZ2 = currentZ;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 2;
                        }

                    }
                }

            }
        }

        if (currentDirection == left)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX;
                    float rightZ1 = currentZ - 4f;
                    float rightX2 = currentX;
                    float rightZ2 = currentZ - 5f;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ - 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 0;
                        }

                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX - 4f;
                    float forwardZ1 = currentZ;
                    float forwardX2 = currentX - 5f;
                    float forwardZ2 = currentZ;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX - 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = left;
                            return 1;
                        }

                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX;
                    float leftZ1 = currentZ + 4f;
                    float leftX2 = currentX;
                    float leftZ2 = currentZ + 5f;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ + 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 2;
                        }

                    }
                }

            }
        }

        if (currentDirection == right)
        {
            float currentX = position[0];
            float currentZ = position[1];
            for (int turn = 0; turn < 3; turn++)
            {
                if (turn == 0)
                {
                    float rightX1 = currentX;
                    float rightZ1 = currentZ + 4f;
                    float rightX2 = currentX;
                    float rightZ2 = currentZ + 5f;

                    if (checkBoundsXZ(rightX1, rightZ1) && checkBoundsXZ(rightX2, rightZ2) && lined[(int)rightX1, (int)rightZ1] == 0 && lined[(int)rightX2, (int)rightZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ + 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = up;
                            return 0;
                        }

                    }

                }

                if (turn == 1)
                {
                    float forwardX1 = currentX + 4f;
                    float forwardZ1 = currentZ;
                    float forwardX2 = currentX + 5f;
                    float forwardZ2 = currentZ;

                    if (checkBoundsXZ(forwardX1, forwardZ1) && checkBoundsXZ(forwardX2, forwardZ2) && lined[(int)forwardX1, (int)forwardZ1] == 0 && lined[(int)forwardX2, (int)forwardZ2] == 0)
                    {
                        float obsX = currentX + 10f;
                        float obsZ = currentZ;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = right;
                            return 1;
                        }

                    }
                }

                if (turn == 2)
                {
                    float leftX1 = currentX;
                    float leftZ1 = currentZ - 4f;
                    float leftX2 = currentX;
                    float leftZ2 = currentZ - 5f;

                    if (checkBoundsXZ(leftX1, leftZ1) && checkBoundsXZ(leftX2, leftZ2) && lined[(int)leftX1, (int)leftZ1] == 0 && lined[(int)leftX2, (int)leftZ2] == 0)
                    {
                        float obsX = currentX;
                        float obsZ = currentZ - 10f;
                        if (checkBoundsXZ(obsX, obsZ) && newMap[terrain_manager.myInfo.get_i_index(obsX), terrain_manager.myInfo.get_j_index(obsZ)] == 0f)
                        {
                            currentDirection = down;
                            return 2;
                        }

                    }
                }

            }
        }
        return flag;

    }

    bool checkBounds(Vector2 pos) {

        if (pos[0] < 0 || pos[0] >= newMap.GetLength(0) || pos[1] < 0 || pos[1] >= newMap.GetLength(1) || isVisited[(int)pos[0], (int)pos[1]] == true ||newMap[(int)pos[0], (int)pos[1]] == 1.0f)
            return false;
        else
            return true;
     }

    bool checkBoundsXZ(float x, float z) {
        if (x < terrain_manager.myInfo.x_low || x >= lined.GetLength(0) || z < terrain_manager.myInfo.z_low || z >= lined.GetLength(1) )
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
