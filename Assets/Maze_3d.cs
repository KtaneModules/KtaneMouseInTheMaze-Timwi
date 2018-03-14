using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class Maze_3d : MonoBehaviour
{
    public KMSelectable[] buttons;
    bool isActive;

    public Camera TargetCamera;
    public Material Mat;
    public Material MatWall;
    public Material MatSphere;
    public MeshFilter Floor, OuterWalls, Ceiling, Sphere, XWall, ZWall, Torus;
    public Light Light;

    const int mLayer = 30;

    MaterialPropertyBlock myBlock;

    bool[,] vertWalls = new bool[10, 9];
    bool[,] horiWalls = new bool[10, 9];
    int[] sphereColors = new int[4];    //0:white 1:green 2:blue 3: yellow; clockwise order from top-left
    int torusColor;
    int goalPosition;
    CameraPosition curCam;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved = false;

    sealed class CameraPosition
    {
        public int X;
        public int Z;
        public int Direction;
        public Vector3 Position { get { return new Vector3(.9f - .2f * X, 0.05f, .9f - .2f * Z); } }
        public Quaternion Rotation { get { return Quaternion.Euler(0, 90f * (Direction + 2), 180); } }
    }

    private Queue<CameraPosition> _queue = new Queue<CameraPosition>();

    void Update()
    {
        if (!isActive)
            return;

        // Floor and Ceiling
        Graphics.DrawMesh(
            Floor.mesh,
            Floor.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            null,
            true,
            true);
        Graphics.DrawMesh(
            Ceiling.mesh,
            Ceiling.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            null,
            false,
            false);
        Graphics.DrawMesh(
            OuterWalls.mesh,
            OuterWalls.gameObject.transform.localToWorldMatrix,
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
                    XWall.transform.localEulerAngles = new Vector3(270, 0, 0);
                    XWall.transform.localPosition = new Vector3(.9f - .2f * i, 0f, .8f - .2f * j);
                    Graphics.DrawMesh(
                        XWall.mesh,
                        XWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera,
                        0,
                        null,
                        false,
                        false);
                    XWall.transform.localEulerAngles = new Vector3(270, 180, 0);
                    Graphics.DrawMesh(
                        XWall.mesh,
                        XWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera,
                        0,
                        null,
                        false,
                        false);
                }
                if (vertWalls[i, j] == true)
                {
                    ZWall.transform.localEulerAngles = new Vector3(270, 90, 0);
                    ZWall.transform.localPosition = new Vector3(.8f - .2f * j, 0f, .9f - .2f * i);
                    Graphics.DrawMesh(
                        ZWall.mesh,
                        ZWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera,
                        0,
                        null,
                        false,
                        false);
                    ZWall.transform.localEulerAngles = new Vector3(270, 270, 0);
                    Graphics.DrawMesh(
                        ZWall.mesh,
                        ZWall.gameObject.transform.localToWorldMatrix,
                        MatWall,
                        mLayer,
                        TargetCamera,
                        0,
                        null,
                        false,
                        false);
                }
            }
        }

        // Spheres
        float x = 0;
        float z = 0;
        for (int i = 0; i <= 3; i++)
        {
            switch (sphereColors[i])
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
                    myBlock.SetColor("_Color", new Color(1, .7f, 0));
                    break;
            }

            switch (i)
            {
                case 0:
                    x = .5f;
                    z = -.5f;
                    break;
                case 1:
                    x = -.5f;
                    z = -.5f;
                    break;
                case 2:
                    x = -.5f;
                    z = .5f;
                    break;
                case 3:
                    x = .5f;
                    z = .5f;
                    break;
            }

            Sphere.transform.localPosition = new Vector3(x, .05f, z);
            Graphics.DrawMesh(
                Sphere.mesh,
                Sphere.gameObject.transform.localToWorldMatrix,
                MatSphere,
                mLayer,
                TargetCamera,
                0,
                myBlock,
                false,
                false);
        }

        //Torus
        switch (torusColor)
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
                myBlock.SetColor("_Color", new Color(1, .5f, 0));
                break;
        }
        Torus.gameObject.transform.Rotate(new Vector3(15f, 45f, 30f) * 2 * Time.deltaTime);
        Graphics.DrawMesh(
            Torus.mesh,
            Torus.gameObject.transform.localToWorldMatrix,
            Mat,
            mLayer,
            TargetCamera,
            0,
            myBlock,
            false,
            false);
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        isActive = false;

        GetComponent<KMBombModule>().OnActivate += OnActivate;

        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }

        myBlock = new MaterialPropertyBlock();

        generateMaze();

        var x = Random.Range(0, 10);
        var z = Random.Range(0, 10);

        // Start in a random direction, but not directly facing a wall.
        var direction = Random.Range(0, 4);
        while (
            direction == 0 ? (z == 9 || horiWalls[x, z]) :
            direction == 1 ? (x == 9 || vertWalls[z, x]) :
            direction == 2 ? (z == 0 || horiWalls[x, z - 1]) :
            direction == 3 ? (x == 0 || vertWalls[z, x - 1]) : false)
            direction = (direction + 1) % 4;

        curCam = new CameraPosition { X = x, Z = z, Direction = direction };
        TargetCamera.transform.localPosition = curCam.Position;
        TargetCamera.transform.localRotation = curCam.Rotation;
        StartCoroutine(positionCamera(curCam));
    }

    void generateMaze()
    {
        int mazeRand = Random.Range(0, 6);
        Debug.LogFormat("[Mouse in the Maze #{1}] You are in the {0} maze.", "top left|middle left|bottom left|top right|middle right|bottom right".Split('|')[mazeRand], _moduleId);

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
            default:
                generateMaze6();
                break;
        }

        Debug.LogFormat("[Mouse in the Maze #{1}] Torus color: {0}", "white|green|blue|yellow".Split('|')[torusColor], _moduleId);
        Debug.LogFormat("[Mouse in the Maze #{1}] Goal sphere color: {0}", "white|green|blue|yellow".Split('|')[sphereColors[goalPosition]], _moduleId);
    }

    void solve()
    {
        GetComponent<KMBombModule>().HandlePass();
        _isSolved = true;
    }

    void strike(int x, int z)
    {
        Debug.LogFormat("[Mouse in the Maze #{0}] Strike because you pressed Submit on {1},{2} but the solution is {3},{4}.", _moduleId, curCam.X, curCam.Z, x, z);
        GetComponent<KMBombModule>().HandleStrike();
    }

    void OnPress(int button)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[button].transform);

        if (isActive && !_isSolved)
        {
            var x = curCam.X;
            var z = curCam.Z;
            var dir = curCam.Direction;

            buttons[button].AddInteractionPunch(.5f);
            switch (button)
            {
                case 0:
                    //TargetCamera.transform.localPosition = new Vector3(.07f, 2.2f, .07f);
                    //TargetCamera.transform.Rotate(new Vector3(-90, 0, 0));
                    //return;
                    switch (goalPosition)
                    {
                        case 0:
                            if (x == 2 && z == 7)
                                solve();
                            else
                                strike(2, 7);
                            break;
                        case 1:
                            if (x == 7 && z == 7)
                                solve();
                            else
                                strike(7, 7);
                            break;
                        case 2:
                            if (x == 7 && z == 2)
                                solve();
                            else
                                strike(7, 2);
                            break;
                        case 3:
                            if (x == 2 && z == 2)
                                solve();
                            else
                                strike(2, 2);
                            break;
                    }
                    break;

                // Move forward
                case 1:
                    if (dir == 0 && z <= 8 && !horiWalls[x, z])
                        z++;
                    else if (dir == 1 && x <= 8 && !vertWalls[z, x])
                        x++;
                    else if (dir == 2 && z >= 1 && !horiWalls[x, z - 1])
                        z--;
                    else if (dir == 3 && x >= 1 && !vertWalls[z, x - 1])
                        x--;
                    break;

                // Move backward
                case 2:
                    if (dir == 2 && z <= 8 && !horiWalls[x, z])
                        z++;
                    else if (dir == 3 && x <= 8 && !vertWalls[z, x])
                        x++;
                    else if (dir == 0 && z >= 1 && !horiWalls[x, z - 1])
                        z--;
                    else if (dir == 1 && x >= 1 && !vertWalls[z, x - 1])
                        x--;
                    break;

                // Turn left
                case 3:
                    dir = (dir + 3) % 4;
                    break;

                // Turn right
                case 4:
                    dir = (dir + 1) % 4;
                    break;
            }

            curCam = new CameraPosition { X = x, Z = z, Direction = dir };
            _queue.Enqueue(curCam);
        }
    }

    private IEnumerator positionCamera(CameraPosition curCamera)
    {
        const float duration = .1f;

        while (!_isSolved || _queue.Count > 0)
        {
            yield return null;

            if (_queue.Count > 0)
            {
                var newCamera = _queue.Dequeue();
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    TargetCamera.transform.localPosition = Vector3.Lerp(curCamera.Position, newCamera.Position, Mathf.Min(1, elapsed / duration));
                    TargetCamera.transform.localRotation = Quaternion.Slerp(curCamera.Rotation, newCamera.Rotation, Mathf.Min(1, elapsed / duration));
                }
                curCamera = newCamera;
            }
        }

        isActive = false;
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

        sphereColors[0] = 1;
        sphereColors[1] = 2;
        sphereColors[2] = 3;
        sphereColors[3] = 0;
        torusColor = Random.Range(0, 4);

        switch (torusColor)
        {
            case 0:
                goalPosition = 0;
                break;
            case 1:
                goalPosition = 1;
                break;
            case 2:
                goalPosition = 3;
                break;
            case 3:
                goalPosition = 2;
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

        sphereColors[0] = 1;
        sphereColors[1] = 2;
        sphereColors[2] = 0;
        sphereColors[3] = 3;
        torusColor = Random.Range(0, 4);

        switch (torusColor)
        {
            case 0:
                goalPosition = 0;
                break;
            case 1:
                goalPosition = 1;
                break;
            case 2:
                goalPosition = 3;
                break;
            case 3:
                goalPosition = 2;
                break;
        }
    }

    void generateMaze3()
    {
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

        sphereColors[0] = 3;
        sphereColors[1] = 1;
        sphereColors[2] = 0;
        sphereColors[3] = 2;
        torusColor = Random.Range(0, 4);

        switch (torusColor)
        {
            case 0:
                goalPosition = 0;
                break;
            case 1:
                goalPosition = 2;
                break;
            case 2:
                goalPosition = 1;
                break;
            case 3:
                goalPosition = 3;
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


        sphereColors[0] = 0;
        sphereColors[1] = 3;
        sphereColors[2] = 1;
        sphereColors[3] = 2;
        torusColor = Random.Range(0, 4);

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (torusColor)
        {
            case 0:
                goalPosition = 1;
                break;
            case 1:
                goalPosition = 2;
                break;
            case 2:
                goalPosition = 0;
                break;
            case 3:
                goalPosition = 3;
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


        sphereColors[0] = 3;
        sphereColors[1] = 0;
        sphereColors[2] = 2;
        sphereColors[3] = 1;
        torusColor = Random.Range(0, 4);

        //int[] aimColor = new int[4];

        //Aim color in objectives[5]  this is specific for each maze
        switch (torusColor)
        {
            case 0:
                goalPosition = 2;
                break;
            case 1:
                goalPosition = 1;
                break;
            case 2:
                goalPosition = 3;
                break;
            case 3:
                goalPosition = 0;
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
        horiWalls[1, 2] = true;
        horiWalls[2, 2] = true;
        horiWalls[3, 2] = true;
        horiWalls[4, 2] = true;
        horiWalls[6, 2] = true;
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

        sphereColors[0] = 2;
        sphereColors[1] = 3;
        sphereColors[2] = 0;
        sphereColors[3] = 1;
        torusColor = Random.Range(0, 4);

        switch (torusColor)
        {
            case 0:
                goalPosition = 0;
                break;
            case 1:
                goalPosition = 1;
                break;
            case 2:
                goalPosition = 3;
                break;
            case 3:
                goalPosition = 2;
                break;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Move with “!{0} forward back”. Turn with “!{0} left right u-turn”. Submit with “!{0} submit”. The first letter only can be used instead.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var btns = new List<KMSelectable>();
        var pieces = command.Trim().ToLowerInvariant().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < pieces.Length; i++)
        {
            switch (pieces[i])
            {
                case "forward": case "f": btns.Add(buttons[1]); break;
                case "backward": case "back": case "b": btns.Add(buttons[2]); break;
                case "left": case "l": btns.Add(buttons[3]); break;
                case "right": case "r": btns.Add(buttons[4]); break;
                case "uturn": case "u-turn": case "180": case "u": btns.Add(buttons[4]); btns.Add(buttons[4]); break;
                case "move":
                    if (i + 1 >= pieces.Length)
                        return null;
                    switch (pieces[i + 1])
                    {
                        case "forward": btns.Add(buttons[1]); break;
                        case "backward": case "back": btns.Add(buttons[2]); break;
                        default: return null;
                    }
                    i++;
                    break;

                case "turn":
                    if (i + 1 >= pieces.Length)
                        return null;
                    switch (pieces[i + 1])
                    {
                        case "left": btns.Add(buttons[3]); break;
                        case "right": btns.Add(buttons[4]); break;
                        case "around": btns.Add(buttons[4]); btns.Add(buttons[4]); break;
                        default: return null;
                    }
                    i++;
                    break;

                case "submit":
                case "s":
                    btns.Add(buttons[0]);
                    break;

                default: return null;
            }
        }

        return btns.ToArray();
    }
}

