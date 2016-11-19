using UnityEngine;

public class Maze_3d : MonoBehaviour
{
    public KMSelectable[] buttons;
    bool isActive;

    public Camera TargetCamera;
    public Material Mat;
    public Material MatWall;
    public Material MatSphere;
    public string LayerName;

    float factor = 0.74f;

    MeshFilter[] myMeshFilters;
    MeshFilter myFloor, myOuterWalls, myCeiling, mySphere, myXWall, myZWall, myTorus;
    int mLayer = 30;

    MaterialPropertyBlock myBlock;

    bool[,] vertWalls = new bool[10, 9];
    bool[,] horiWalls = new bool[10, 9];
    int[] objectives = new int[6];          //0:white 1:green 2:blue 3: yellow    Array:[color of position 0-3, col of Torus, position of goal]
    int XCam, ZCam, direction;
    Vector3 posXWall, posZWall, posSphere;

    void Awake()
    {
        //mLayer = LayerMask.NameToLayer (LayerName);
        Debug.Log("layer: " + mLayer);
    }

    void Update()
    {

        //Floor and Ceiling
        Graphics.DrawMesh(
            myFloor.mesh,
            myFloor.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            null,
            true,
            true);
        Graphics.DrawMesh(
            myCeiling.mesh,
            myCeiling.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            null,
            false,
            false);
        Graphics.DrawMesh(
            myOuterWalls.mesh,
            myOuterWalls.gameObject.transform.localToWorldMatrix,
            MatWall,
            mLayer,
            TargetCamera,
            0,
            null,
            false,
            false);


        //Walls
        for (int i = 0; i <= 9; i++)
        {
            for (int j = 0; j <= 8; j++)
            {
                if (horiWalls[i, j] == true)
                {
                    myXWall.gameObject.transform.Translate(new Vector3(-9f + 2f * i, +8f - 2f * j, 0f) * factor);
                    Graphics.DrawMesh(
                        myXWall.mesh,
                        myXWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera);
                    myXWall.transform.position = posXWall;
                    //myXWall.gameObject.transform.Translate (new Vector3 (9f-2f*i, -8+2f*j, 0f)*factor);
                }
                if (vertWalls[i, j] == true)
                {
                    myZWall.gameObject.transform.Translate(new Vector3(9f - 2f * i, 8f - 2f * j, 0f) * factor);
                    Graphics.DrawMesh(
                        myZWall.mesh,
                        myZWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera);
                    myZWall.transform.position = posZWall;
                    //myZWall.gameObject.transform.Translate (new Vector3 (-9f+2f*i,-8f+2f*j,0)*factor);
                }
            }
        }

        //Spheres
        float x = 0;
        float y = 1f;
        float z = 0;
        for (int i = 0; i <= 3; i++)
        {
            switch (objectives[i])
            {
                case 0:
                    myBlock.SetColor("_Color", Color.white);
                    break;
                case 1:
                    myBlock.SetColor("_Color", Color.green);
                    break;
                case 2:
                    myBlock.SetColor("_Color", Color.blue);
                    break;
                case 3:
                    myBlock.SetColor("_Color", Color.yellow);
                    break;
            }

            switch (i)
            {
                case 0:
                    x = -5;
                    z = 5;
                    break;
                case 1:
                    x = 5;
                    z = 5;
                    break;
                case 2:
                    x = 5;
                    z = -5;
                    break;
                case 3:
                    x = -5;
                    z = -5;
                    break;
            }

            //mySphere.gameObject.transform.Rotate (new Vector3 (15f,45f,30f)*Time.deltaTime,Space.Self);
            mySphere.gameObject.transform.Translate(new Vector3(x, y, z) * factor);
            Graphics.DrawMesh(
                mySphere.mesh,
                mySphere.gameObject.transform.localToWorldMatrix,
                MatSphere,
                mLayer,
                TargetCamera,
                0,
                myBlock,
                false,
                false);
            mySphere.transform.position = posSphere;
            //mySphere.gameObject.transform.Translate (new Vector3 (-x, -y, -z)*factor);

        }





        //Torus
        switch (objectives[4])
        {
            case 0:
                myBlock.SetColor("_Color", Color.white);
                break;
            case 1:
                myBlock.SetColor("_Color", Color.green);
                break;
            case 2:
                myBlock.SetColor("_Color", Color.blue);
                break;
            case 3:
                myBlock.SetColor("_Color", Color.yellow);
                break;
        }
        //myTorus.gameObject.transform.Translate (new Vector3 (0f, 0f, 0f)*factor);
        myTorus.gameObject.transform.Rotate(new Vector3(15f, 45f, 30f) * 2 * Time.deltaTime);
        Graphics.DrawMesh(
            myTorus.mesh,
            myTorus.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            myBlock,
            false,
            false);
        //myTorus.gameObject.transform.Translate (new Vector3 (0f, 0f, 0f)*factor);
    }

    void Start()
    {
        myMeshFilters = this.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter m in myMeshFilters)
        {
            if (m.gameObject.name == "Floor")
            {
                myFloor = m;
                Debug.Log("Floor found");
            }
            if (m.gameObject.name == "outerWalls")
            {
                myOuterWalls = m;
                Debug.Log("outerWalls found");
            }
            if (m.gameObject.name == "Ceiling")
            {
                myCeiling = m;
                Debug.Log("Ceiling found");
            }
            if (m.gameObject.name == "XWall")
            {
                myXWall = m;
                Debug.Log("XWall found");
            }
            if (m.gameObject.name == "ZWall")
            {
                myZWall = m;
                Debug.Log("ZWall found");
            }
            if (m.gameObject.name == "Sphere")
            {
                mySphere = m;
                Debug.Log("Sphere found");
            }
            if (m.gameObject.name == "Torus")
            {
                myTorus = m;
                Debug.Log("Torus found");
            }
        }

        posXWall = myXWall.transform.position;
        posZWall = myZWall.transform.position;
        posSphere = mySphere.transform.position;


        isActive = false;

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
            buttons[i].AddInteractionPunch();
        }

        myBlock = new MaterialPropertyBlock();

        XCam = (int) (Random.value * 9);    //unlikely to spawn in row 9
        ZCam = (int) (Random.value * 9);


        //XCam = 1;
        //ZCam = 1;

        TargetCamera.transform.Translate(new Vector3(8.75f - 2f * XCam, 0f, -9.4f + 2f * ZCam) * factor);

        direction = (int) (Random.value * 4);
        direction = (direction == 4) ? 3 : direction;

        //direction = 0;

        for (int i = 0; i < direction; i++)
        {
            TargetCamera.transform.Rotate(new Vector3(0f, -90f, 0f));
        }



        generateMaze();

    }

    void generateMaze()
    {

        int mazeRand = (int) (Random.value * 6);

        Debug.Log("Maze: " + mazeRand);
        switch (mazeRand)
        {
            case 0:
                generateMaze1();
                break;
            case 1:
                generateMaze2();
                break;
            case 2:
                generateMaze3();
                break;
            case 3:
                generateMaze4();
                break;
            case 4:
                generateMaze5();
                break;
            case 5:
                generateMaze6();
                break;
            default:
                generateMaze1();
                break;
        }
        //generateMaze7 ();

        //this is not specific for each maze
        //Aim position in objectiv[5]
        for (int i = 0; i <= 3; i++)
        {
            if (objectives[i] == objectives[5])
            {
                objectives[5] = i;
                break;
            }
        }

    }


    void isSolved()
    {
        GetComponent<KMBombModule>().HandlePass();
        isActive = false;
        //Debug.Log ("Pass");
    }

    void strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
        //Debug.Log ("Strike");
    }

    void OnPress(int button)
    {
        if (isActive)
        {
            switch (button)
            {
                case 0:
                    switch (objectives[5])
                    {
                        case 0:
                            if (XCam == 2 && ZCam == 7)
                                isSolved();
                            else
                                strike();
                            break;
                        case 1:
                            if (XCam == 7 && ZCam == 7)
                                isSolved();
                            else
                                strike();
                            break;
                        case 2:
                            if (XCam == 7 && ZCam == 2)
                                isSolved();
                            else
                                strike();
                            break;
                        case 3:
                            if (XCam == 2 && ZCam == 2)
                                isSolved();
                            else
                                strike();
                            break;
                    }
                    break;
                case 1:
                    if (direction == 0 && ZCam <= 8)
                        if (!horiWalls[XCam, ZCam])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, 2f) * factor);
                            ZCam++;
                            Debug.Log("move");
                        }
                    if (direction == 1 && XCam <= 8)
                        if (!vertWalls[ZCam, XCam])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, 2f) * factor);
                            Debug.Log("move");
                            XCam++;
                        }
                    if (direction == 2 && ZCam >= 1)
                        if (!horiWalls[XCam, ZCam - 1])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, 2f) * factor);
                            Debug.Log("move");
                            ZCam--;
                        }
                    if (direction == 3 && XCam >= 1)
                        if (!vertWalls[ZCam, XCam - 1])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, 2f) * factor);
                            Debug.Log("move");
                            XCam--;
                        }
                    break;
                case 2:     //down
                    if (direction == 2 && ZCam <= 8)
                        if (!horiWalls[XCam, ZCam])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, -2f) * factor);
                            ZCam++;
                            Debug.Log("move");
                        }
                    if (direction == 3 && XCam <= 8)
                        if (!vertWalls[ZCam, XCam])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, -2f) * factor);
                            Debug.Log("move");
                            XCam++;
                        }
                    if (direction == 0 && ZCam >= 1)
                        if (!horiWalls[XCam, ZCam - 1])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, -2f) * factor);
                            Debug.Log("move");
                            ZCam--;
                        }
                    if (direction == 1 && XCam >= 1)
                        if (!vertWalls[ZCam, XCam - 1])
                        {
                            TargetCamera.transform.Translate(new Vector3(0f, 0f, -2f) * factor);
                            Debug.Log("move");
                            XCam--;
                        }
                    break;
                case 3:     //left
                    direction = (4 + direction - 1) % 4;
                    TargetCamera.transform.Rotate(new Vector3(0f, 90f, 0f));
                    break;
                case 4:     //right
                    direction = (direction + 1) % 4;
                    TargetCamera.transform.Rotate(new Vector3(0f, -90f, 0f));
                    break;
                default:
                    break;
            }
            Debug.Log("Direction: " + direction);
            Debug.Log("x: " + XCam + "    Z: " + ZCam);
        }
    }

    void OnActivate()
    {
        isActive = true;
        TargetCamera.enabled = true;
    }

    void generateMaze1()
    {

        horiWalls[1, 0] = true;
        horiWalls[4, 0] = true;
        horiWalls[5, 0] = true;
        horiWalls[7, 0] = true;
        horiWalls[8, 0] = true;
        horiWalls[9, 0] = true;
        horiWalls[3, 1] = true;
        horiWalls[4, 1] = true;
        horiWalls[6, 1] = true;
        horiWalls[7, 1] = true;
        horiWalls[8, 1] = true;
        horiWalls[3, 2] = true;
        horiWalls[4, 2] = true;
        horiWalls[5, 2] = true;
        horiWalls[8, 2] = true;
        horiWalls[9, 2] = true;
        horiWalls[1, 3] = true;
        horiWalls[2, 3] = true;
        horiWalls[5, 3] = true;
        horiWalls[6, 3] = true;
        horiWalls[8, 3] = true;
        horiWalls[7, 3] = true;
        horiWalls[0, 4] = true;
        horiWalls[1, 4] = true;
        horiWalls[8, 4] = true;
        horiWalls[9, 4] = true;
        horiWalls[3, 5] = true;
        horiWalls[4, 5] = true;
        horiWalls[3, 6] = true;
        horiWalls[4, 6] = true;
        horiWalls[5, 6] = true;
        horiWalls[6, 6] = true;
        horiWalls[8, 6] = true;
        horiWalls[8, 7] = true;
        horiWalls[2, 7] = true;
        horiWalls[4, 7] = true;
        horiWalls[5, 7] = true;
        horiWalls[9, 7] = true;
        horiWalls[0, 8] = true;
        horiWalls[1, 8] = true;
        horiWalls[3, 8] = true;
        horiWalls[5, 8] = true;
        horiWalls[7, 8] = true;
        horiWalls[8, 8] = true;

        vertWalls[1, 0] = true;
        vertWalls[2, 0] = true;
        vertWalls[3, 0] = true;
        vertWalls[6, 0] = true;
        vertWalls[7, 0] = true;
        vertWalls[8, 0] = true;
        vertWalls[1, 1] = true;
        vertWalls[2, 1] = true;
        vertWalls[5, 1] = true;
        vertWalls[6, 1] = true;
        vertWalls[8, 1] = true;
        vertWalls[0, 2] = true;
        vertWalls[1, 2] = true;
        vertWalls[4, 2] = true;
        vertWalls[5, 2] = true;
        vertWalls[7, 2] = true;
        vertWalls[1, 3] = true;
        vertWalls[3, 3] = true;
        vertWalls[4, 3] = true;
        vertWalls[8, 3] = true;
        vertWalls[5, 4] = true;
        vertWalls[9, 4] = true;
        vertWalls[1, 5] = true;
        vertWalls[2, 5] = true;
        vertWalls[5, 5] = true;
        vertWalls[6, 5] = true;
        vertWalls[8, 5] = true;
        vertWalls[3, 6] = true;
        vertWalls[4, 6] = true;
        vertWalls[5, 6] = true;
        vertWalls[7, 6] = true;
        vertWalls[8, 6] = true;
        vertWalls[5, 7] = true;
        vertWalls[6, 7] = true;
        vertWalls[6, 8] = true;

        objectives[0] = 1;
        objectives[1] = 2;
        objectives[2] = 3;
        objectives[3] = 0;
        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 1;
                break;
            case 1:
                objectives[5] = 2;
                break;
            case 2:
                objectives[5] = 0;
                break;
            case 3:
                objectives[5] = 3;
                break;
        }
    }

    void generateMaze2()
    {

        horiWalls[1, 0] = true;
        horiWalls[8, 0] = true;
        horiWalls[4, 1] = true;
        horiWalls[5, 1] = true;
        horiWalls[6, 1] = true;
        horiWalls[7, 1] = true;
        horiWalls[1, 2] = true;
        horiWalls[2, 2] = true;
        horiWalls[5, 2] = true;
        horiWalls[8, 2] = true;
        horiWalls[0, 3] = true;
        horiWalls[1, 3] = true;
        horiWalls[2, 3] = true;
        horiWalls[9, 3] = true;
        horiWalls[1, 4] = true;
        horiWalls[2, 4] = true;
        horiWalls[3, 4] = true;
        horiWalls[5, 4] = true;
        horiWalls[6, 4] = true;
        horiWalls[8, 4] = true;
        horiWalls[9, 4] = true;
        horiWalls[2, 5] = true;
        horiWalls[3, 5] = true;
        horiWalls[5, 5] = true;
        horiWalls[6, 5] = true;
        horiWalls[7, 5] = true;
        horiWalls[8, 5] = true;
        horiWalls[1, 6] = true;
        horiWalls[2, 6] = true;
        horiWalls[4, 6] = true;
        horiWalls[6, 6] = true;
        horiWalls[9, 6] = true;
        horiWalls[0, 7] = true;
        horiWalls[1, 7] = true;
        horiWalls[2, 7] = true;
        horiWalls[7, 7] = true;
        horiWalls[8, 7] = true;
        horiWalls[1, 8] = true;
        horiWalls[2, 8] = true;
        horiWalls[6, 8] = true;
        horiWalls[9, 8] = true;

        vertWalls[1, 0] = true;
        vertWalls[2, 0] = true;
        vertWalls[5, 0] = true;
        vertWalls[6, 0] = true;
        vertWalls[1, 1] = true;
        vertWalls[0, 2] = true;
        vertWalls[1, 2] = true;
        vertWalls[2, 2] = true;
        vertWalls[8, 2] = true;
        vertWalls[1, 3] = true;
        vertWalls[2, 3] = true;
        vertWalls[3, 3] = true;
        vertWalls[4, 3] = true;
        vertWalls[6, 3] = true;
        vertWalls[7, 3] = true;
        vertWalls[9, 3] = true;
        vertWalls[0, 4] = true;
        vertWalls[3, 4] = true;
        vertWalls[4, 4] = true;
        vertWalls[7, 4] = true;
        vertWalls[8, 4] = true;
        vertWalls[1, 5] = true;
        vertWalls[3, 5] = true;
        vertWalls[7, 5] = true;
        vertWalls[8, 5] = true;
        vertWalls[0, 6] = true;
        vertWalls[2, 6] = true;
        vertWalls[3, 6] = true;
        vertWalls[4, 6] = true;
        vertWalls[7, 6] = true;
        vertWalls[1, 7] = true;
        vertWalls[3, 7] = true;
        vertWalls[4, 7] = true;
        vertWalls[6, 7] = true;
        vertWalls[7, 7] = true;
        vertWalls[8, 7] = true;
        vertWalls[9, 7] = true;
        vertWalls[2, 8] = true;

        objectives[0] = 1;
        objectives[1] = 2;
        objectives[2] = 0;
        objectives[3] = 3;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 1;
                break;
            case 1:
                objectives[5] = 2;
                break;
            case 2:
                objectives[5] = 3;
                break;
            case 3:
                objectives[5] = 0;
                break;
        }
    }

    void generateMaze3()
    {

        horiWalls[0, 0] = true;
        horiWalls[1, 0] = true;
        horiWalls[2, 0] = true;
        horiWalls[3, 0] = true;
        horiWalls[4, 0] = true;
        horiWalls[1, 1] = true;
        horiWalls[2, 1] = true;
        horiWalls[3, 1] = true;
        horiWalls[2, 2] = true;
        horiWalls[6, 2] = true;
        horiWalls[8, 2] = true;
        horiWalls[9, 2] = true;
        horiWalls[3, 3] = true;
        horiWalls[8, 3] = true;
        horiWalls[2, 4] = true;
        horiWalls[3, 4] = true;
        horiWalls[4, 4] = true;
        horiWalls[7, 4] = true;
        horiWalls[2, 5] = true;
        horiWalls[4, 5] = true;
        horiWalls[5, 5] = true;
        horiWalls[6, 5] = true;
        horiWalls[7, 5] = true;
        horiWalls[0, 6] = true;
        horiWalls[1, 6] = true;
        horiWalls[3, 6] = true;
        horiWalls[4, 6] = true;
        horiWalls[5, 6] = true;
        horiWalls[6, 6] = true;
        horiWalls[9, 6] = true;
        horiWalls[1, 7] = true;
        horiWalls[2, 7] = true;
        horiWalls[4, 7] = true;
        horiWalls[5, 7] = true;
        horiWalls[7, 7] = true;
        horiWalls[1, 8] = true;
        horiWalls[2, 8] = true;
        horiWalls[3, 8] = true;
        horiWalls[7, 8] = true;
        horiWalls[8, 8] = true;

        vertWalls[2, 0] = true;
        vertWalls[3, 0] = true;
        vertWalls[4, 0] = true;
        vertWalls[5, 0] = true;
        vertWalls[3, 1] = true;
        vertWalls[4, 1] = true;
        vertWalls[6, 2] = true;
        vertWalls[7, 2] = true;
        vertWalls[2, 3] = true;
        vertWalls[3, 3] = true;
        vertWalls[5, 3] = true;
        vertWalls[8, 3] = true;
        vertWalls[1, 4] = true;
        vertWalls[2, 4] = true;
        vertWalls[3, 4] = true;
        vertWalls[4, 4] = true;
        vertWalls[9, 4] = true;
        vertWalls[1, 5] = true;
        vertWalls[2, 5] = true;
        vertWalls[4, 5] = true;
        vertWalls[5, 5] = true;
        vertWalls[8, 5] = true;
        vertWalls[9, 5] = true;
        vertWalls[0, 6] = true;
        vertWalls[1, 6] = true;
        vertWalls[3, 6] = true;
        vertWalls[4, 6] = true;
        vertWalls[7, 6] = true;
        vertWalls[1, 7] = true;
        vertWalls[2, 7] = true;
        vertWalls[3, 7] = true;
        vertWalls[5, 7] = true;
        vertWalls[6, 7] = true;
        vertWalls[7, 7] = true;
        vertWalls[0, 8] = true;
        vertWalls[1, 8] = true;
        vertWalls[4, 8] = true;
        vertWalls[5, 8] = true;
        vertWalls[7, 8] = true;
        vertWalls[8, 8] = true;

        objectives[0] = 3;
        objectives[1] = 1;
        objectives[2] = 0;
        objectives[3] = 2;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 3;
                break;
            case 1:
                objectives[5] = 0;
                break;
            case 2:
                objectives[5] = 1;
                break;
            case 3:
                objectives[5] = 2;
                break;
        }
    }

    void generateMaze4()
    {

        horiWalls[1, 0] = true;
        horiWalls[2, 0] = true;
        horiWalls[3, 0] = true;
        horiWalls[4, 0] = true;
        horiWalls[5, 0] = true;
        horiWalls[8, 0] = true;
        horiWalls[2, 1] = true;
        horiWalls[3, 1] = true;
        horiWalls[4, 1] = true;
        horiWalls[8, 1] = true;
        horiWalls[9, 1] = true;
        horiWalls[0, 2] = true;
        horiWalls[1, 2] = true;
        horiWalls[3, 2] = true;
        horiWalls[4, 2] = true;
        horiWalls[6, 2] = true;
        horiWalls[7, 2] = true;
        horiWalls[1, 3] = true;
        horiWalls[4, 3] = true;
        horiWalls[6, 3] = true;
        horiWalls[8, 3] = true;
        horiWalls[0, 4] = true;
        horiWalls[2, 4] = true;
        horiWalls[3, 4] = true;
        horiWalls[5, 4] = true;
        horiWalls[8, 4] = true;
        horiWalls[1, 5] = true;
        horiWalls[3, 5] = true;
        horiWalls[4, 5] = true;
        horiWalls[5, 5] = true;
        horiWalls[9, 5] = true;
        horiWalls[2, 6] = true;
        horiWalls[3, 6] = true;
        horiWalls[7, 6] = true;
        horiWalls[8, 6] = true;
        horiWalls[0, 7] = true;
        horiWalls[3, 7] = true;
        horiWalls[6, 7] = true;
        horiWalls[7, 7] = true;
        horiWalls[9, 7] = true;
        horiWalls[1, 8] = true;
        horiWalls[4, 8] = true;
        horiWalls[5, 8] = true;
        horiWalls[6, 8] = true;
        horiWalls[8, 8] = true;

        vertWalls[1, 0] = true;
        vertWalls[6, 0] = true;
        vertWalls[7, 0] = true;
        vertWalls[2, 1] = true;
        vertWalls[4, 1] = true;
        vertWalls[5, 1] = true;
        vertWalls[7, 1] = true;
        vertWalls[8, 1] = true;
        vertWalls[3, 2] = true;
        vertWalls[8, 2] = true;
        vertWalls[9, 2] = true;
        vertWalls[2, 4] = true;
        vertWalls[4, 4] = true;
        vertWalls[6, 4] = true;
        vertWalls[7, 4] = true;
        vertWalls[1, 5] = true;
        vertWalls[2, 5] = true;
        vertWalls[5, 5] = true;
        vertWalls[6, 5] = true;
        vertWalls[3, 5] = true;
        vertWalls[0, 6] = true;
        vertWalls[1, 6] = true;
        vertWalls[4, 6] = true;
        vertWalls[5, 6] = true;
        vertWalls[7, 6] = true;
        vertWalls[9, 6] = true;
        vertWalls[1, 7] = true;
        vertWalls[5, 7] = true;
        vertWalls[6, 7] = true;
        vertWalls[8, 7] = true;
        vertWalls[2, 8] = true;
        vertWalls[3, 8] = true;


        objectives[0] = 0;
        objectives[1] = 3;
        objectives[2] = 1;
        objectives[3] = 2;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 3;
                break;
            case 1:
                objectives[5] = 1;
                break;
            case 2:
                objectives[5] = 0;
                break;
            case 3:
                objectives[5] = 2;
                break;
        }
    }

    void generateMaze5()
    {

        horiWalls[8, 0] = true;
        horiWalls[9, 0] = true;
        horiWalls[1, 1] = true;
        horiWalls[2, 1] = true;
        horiWalls[5, 1] = true;
        horiWalls[6, 1] = true;
        horiWalls[7, 1] = true;
        horiWalls[8, 1] = true;
        horiWalls[0, 2] = true;
        horiWalls[2, 2] = true;
        horiWalls[3, 2] = true;
        horiWalls[4, 2] = true;
        horiWalls[6, 2] = true;
        horiWalls[8, 2] = true;
        horiWalls[2, 3] = true;
        horiWalls[3, 3] = true;
        horiWalls[5, 3] = true;
        horiWalls[6, 3] = true;
        horiWalls[7, 3] = true;
        horiWalls[1, 4] = true;
        horiWalls[2, 4] = true;
        horiWalls[6, 4] = true;
        horiWalls[7, 4] = true;
        horiWalls[8, 4] = true;
        horiWalls[0, 5] = true;
        horiWalls[1, 5] = true;
        horiWalls[2, 5] = true;
        horiWalls[3, 5] = true;
        horiWalls[8, 5] = true;
        horiWalls[7, 5] = true;
        horiWalls[1, 6] = true;
        horiWalls[2, 6] = true;
        horiWalls[3, 6] = true;
        horiWalls[4, 6] = true;
        horiWalls[9, 6] = true;
        horiWalls[2, 7] = true;
        horiWalls[6, 7] = true;
        horiWalls[7, 7] = true;
        horiWalls[1, 8] = true;
        horiWalls[2, 8] = true;
        horiWalls[5, 8] = true;
        horiWalls[6, 8] = true;
        horiWalls[8, 8] = true;

        vertWalls[1, 0] = true;
        vertWalls[4, 0] = true;
        vertWalls[7, 0] = true;
        vertWalls[8, 0] = true;
        vertWalls[0, 1] = true;
        vertWalls[5, 1] = true;
        vertWalls[1, 2] = true;
        vertWalls[7, 2] = true;
        vertWalls[0, 3] = true;
        vertWalls[1, 3] = true;
        vertWalls[4, 3] = true;
        vertWalls[5, 3] = true;
        vertWalls[8, 3] = true;
        vertWalls[9, 3] = true;
        vertWalls[1, 4] = true;
        vertWalls[2, 4] = true;
        vertWalls[3, 4] = true;
        vertWalls[4, 4] = true;
        vertWalls[5, 4] = true;
        vertWalls[6, 4] = true;
        vertWalls[7, 4] = true;
        vertWalls[8, 4] = true;
        vertWalls[0, 5] = true;
        vertWalls[5, 5] = true;
        vertWalls[6, 5] = true;
        vertWalls[1, 6] = true;
        vertWalls[3, 6] = true;
        vertWalls[6, 6] = true;
        vertWalls[7, 7] = true;
        vertWalls[8, 7] = true;
        vertWalls[3, 8] = true;
        vertWalls[4, 8] = true;
        vertWalls[6, 8] = true;
        vertWalls[8, 8] = true;


        objectives[0] = 3;
        objectives[1] = 0;
        objectives[2] = 2;
        objectives[3] = 1;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 2;
                break;
            case 1:
                objectives[5] = 0;
                break;
            case 2:
                objectives[5] = 1;
                break;
            case 3:
                objectives[5] = 3;
                break;
        }
    }

    void generateMaze6()
    {

        horiWalls[1, 0] = true;
        horiWalls[3, 0] = true;
        horiWalls[4, 0] = true;
        horiWalls[7, 0] = true;
        horiWalls[8, 0] = true;
        horiWalls[2, 1] = true;
        horiWalls[4, 1] = true;
        horiWalls[5, 1] = true;
        horiWalls[8, 1] = true;
        horiWalls[9, 1] = true;
        horiWalls[1, 2] = true;
        horiWalls[2, 2] = true;
        horiWalls[3, 2] = true;
        horiWalls[4, 2] = true;
        horiWalls[6, 2] = true;
        horiWalls[9, 2] = true;
        horiWalls[2, 3] = true;
        horiWalls[5, 3] = true;
        horiWalls[7, 3] = true;
        horiWalls[8, 3] = true;
        horiWalls[0, 4] = true;
        horiWalls[6, 4] = true;
        horiWalls[7, 4] = true;
        horiWalls[9, 4] = true;
        horiWalls[2, 5] = true;
        horiWalls[3, 5] = true;
        horiWalls[6, 5] = true;
        horiWalls[8, 5] = true;
        horiWalls[1, 6] = true;
        horiWalls[2, 6] = true;
        horiWalls[4, 6] = true;
        horiWalls[5, 6] = true;
        horiWalls[7, 6] = true;
        horiWalls[3, 7] = true;
        horiWalls[5, 7] = true;
        horiWalls[6, 7] = true;
        horiWalls[2, 8] = true;
        horiWalls[4, 8] = true;
        horiWalls[7, 8] = true;
        horiWalls[8, 8] = true;

        vertWalls[1, 0] = true;
        vertWalls[2, 0] = true;
        vertWalls[3, 0] = true;
        vertWalls[5, 0] = true;
        vertWalls[7, 0] = true;
        vertWalls[8, 0] = true;
        vertWalls[4, 1] = true;
        vertWalls[5, 1] = true;
        vertWalls[6, 1] = true;
        vertWalls[8, 1] = true;
        vertWalls[9, 1] = true;
        vertWalls[1, 2] = true;
        vertWalls[2, 2] = true;
        vertWalls[4, 2] = true;
        vertWalls[7, 2] = true;
        vertWalls[3, 3] = true;
        vertWalls[4, 3] = true;
        vertWalls[8, 3] = true;
        vertWalls[4, 4] = true;
        vertWalls[5, 4] = true;
        vertWalls[6, 4] = true;
        vertWalls[0, 5] = true;
        vertWalls[1, 5] = true;
        vertWalls[3, 5] = true;
        vertWalls[8, 5] = true;
        vertWalls[9, 5] = true;
        vertWalls[1, 6] = true;
        vertWalls[2, 6] = true;
        vertWalls[4, 6] = true;
        vertWalls[6, 6] = true;
        vertWalls[2, 7] = true;
        vertWalls[3, 7] = true;
        vertWalls[5, 7] = true;
        vertWalls[7, 7] = true;
        vertWalls[8, 7] = true;
        vertWalls[3, 8] = true;
        vertWalls[4, 8] = true;
        vertWalls[6, 8] = true;
        vertWalls[7, 8] = true;


        objectives[0] = 2;
        objectives[1] = 3;
        objectives[2] = 0;
        objectives[3] = 1;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 2;
                break;
            case 1:
                objectives[5] = 3;
                break;
            case 2:
                objectives[5] = 1;
                break;
            case 3:
                objectives[5] = 0;
                break;
        }
    }

    void generateMaze7()
    {

        horiWalls[0, 0] = true;
        horiWalls[0, 1] = true;
        horiWalls[0, 2] = true;
        horiWalls[0, 3] = true;
        horiWalls[0, 4] = true;
        horiWalls[0, 5] = true;
        horiWalls[0, 6] = true;
        horiWalls[0, 7] = true;
        horiWalls[0, 8] = true;

        vertWalls[0, 0] = true;
        vertWalls[0, 1] = true;
        vertWalls[0, 2] = true;
        vertWalls[0, 3] = true;
        vertWalls[0, 4] = true;
        vertWalls[0, 5] = true;
        vertWalls[0, 6] = true;
        vertWalls[0, 7] = true;
        vertWalls[0, 8] = true;



        objectives[0] = 2;
        objectives[1] = 3;
        objectives[2] = 0;
        objectives[3] = 1;

        int rand = (int) (Random.value * 4);
        rand = (rand == 4) ? 3 : rand;
        objectives[4] = rand;

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (objectives[4])
        {
            case 0:
                objectives[5] = 2;
                break;
            case 1:
                objectives[5] = 3;
                break;
            case 2:
                objectives[5] = 1;
                break;
            case 3:
                objectives[5] = 0;
                break;
        }
    }

}

