using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Random = UnityEngine.Random;

public class MouseInTheMazeModule : MonoBehaviour
{
    public KMSelectable[] buttons;
    public KMSelectable module;
    public Camera TargetCamera;
    public Material Mat;
    public Material MatWall;
    public Material MatSphere;
    public MeshFilter Floor, OuterWalls, Ceiling, Sphere, XWall, ZWall, Torus;
    public Light Light;
    public KMRuleSeedable RuleSeedable;

    private const int mLayer = 30;

    MaterialPropertyBlock _myBlock;
    bool[,] _vertWalls = new bool[10, 9];
    bool[,] _horiWalls = new bool[10, 9];
    int[] _sphereColors = new int[4];    //0=white, 1=green, 2=blue, 3=yellow; clockwise order from top-left
    int _torusColor;
    int _goalPosition;
    int[] _goalFromTorusColor;
    CameraPosition _curCam;
    bool _isSelected;

    private bool _isActive;
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved = false;
    private bool _tpMazeTemporarilyDisabled = false;

    sealed class CameraPosition : IEquatable<CameraPosition>
    {
        public int X { get; private set; }
        public int Z { get; private set; }
        public int Direction { get; private set; }
        public Vector3 Position { get { return new Vector3(.9f - .2f * X, 0.05f, .9f - .2f * Z); } }
        public Quaternion Rotation { get { return Quaternion.Euler(0, 90f * (Direction + 2), 180); } }
        public CameraPosition(int x, int z, int dir) { X = x; Z = z; Direction = dir; }

        public CameraPosition MoveForward(bool[,] horiWalls, bool[,] vertWalls)
        {
            return
                (Direction == 0 && Z <= 8 && !horiWalls[X, Z]) ? new CameraPosition(X, Z + 1, Direction) :
                (Direction == 1 && X <= 8 && !vertWalls[Z, X]) ? new CameraPosition(X + 1, Z, Direction) :
                (Direction == 2 && Z >= 1 && !horiWalls[X, Z - 1]) ? new CameraPosition(X, Z - 1, Direction) :
                (Direction == 3 && X >= 1 && !vertWalls[Z, X - 1]) ? new CameraPosition(X - 1, Z, Direction) : null;
        }

        public CameraPosition MoveBackward(bool[,] horiWalls, bool[,] vertWalls)
        {
            return
                (Direction == 2 && Z <= 8 && !horiWalls[X, Z]) ? new CameraPosition(X, Z + 1, Direction) :
                (Direction == 3 && X <= 8 && !vertWalls[Z, X]) ? new CameraPosition(X + 1, Z, Direction) :
                (Direction == 0 && Z >= 1 && !horiWalls[X, Z - 1]) ? new CameraPosition(X, Z - 1, Direction) :
                (Direction == 1 && X >= 1 && !vertWalls[Z, X - 1]) ? new CameraPosition(X - 1, Z, Direction) : null;
        }

        public CameraPosition TurnLeft() { return new CameraPosition(X, Z, (Direction + 3) % 4); }
        public CameraPosition TurnRight() { return new CameraPosition(X, Z, (Direction + 1) % 4); }

        public bool Equals(CameraPosition other)
        {
            return other != null && other.X == X && other.Z == Z && other.Direction == Direction;
        }
        public override int GetHashCode()
        {
            return X + 31 * Z + 71 * Direction;
        }
        public override bool Equals(object obj)
        {
            return obj is CameraPosition && Equals((CameraPosition) obj);
        }

        public bool IsGoal(int goalPosition)
        {
            return new[] { 2, 7, 7, 2 }[goalPosition] == X && new[] { 7, 7, 2, 2 }[goalPosition] == Z; ;
        }
    }

    private readonly Queue<CameraPosition> _queue = new Queue<CameraPosition>();

    void Update()
    {
        if (!_isActive || _tpMazeTemporarilyDisabled)
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
                if (_horiWalls[i, j] == true)
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
                if (_vertWalls[i, j] == true)
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
            switch (_sphereColors[i])
            {
                case 0:
                    _myBlock.SetColor("_Color", Color.white);
                    break;
                case 1:
                    _myBlock.SetColor("_Color", Color.green);
                    break;
                case 2:
                    _myBlock.SetColor("_Color", Color.blue);
                    break;
                case 3:
                    _myBlock.SetColor("_Color", new Color(1, .7f, 0));
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
                _myBlock,
                false,
                false);
        }

        //Torus
        switch (_torusColor)
        {
            case 0:
                _myBlock.SetColor("_Color", Color.white);
                break;
            case 1:
                _myBlock.SetColor("_Color", Color.green);
                break;
            case 2:
                _myBlock.SetColor("_Color", Color.blue);
                break;
            case 3:
                _myBlock.SetColor("_Color", new Color(1, .5f, 0));
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
            _myBlock,
            false,
            false);

        if (_isSelected)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Keypad8))
                buttons[1].OnInteract();
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Keypad2))
                buttons[2].OnInteract();
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Keypad4))
                buttons[3].OnInteract();
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Keypad6))
                buttons[4].OnInteract();
            else if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                buttons[0].OnInteract();
        }
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isActive = false;

        GetComponent<KMBombModule>().OnActivate += OnActivate;
        module.OnFocus += delegate { _isSelected = true; };
        module.OnDefocus += delegate { _isSelected = false; };

        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }

        _myBlock = new MaterialPropertyBlock();

        // Start of rule-seeded code
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[Mouse in the Maze #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);
        var generatedHWalls = new List<bool[,]>();
        var generatedVWalls = new List<bool[,]>();
        var generatedGoalFromTorus = new List<int[]>();
        var generatedSphereColors = new List<int[]>();
        if (rnd.Seed != 1)
        {
            // Generate 6 random mazes and then remove 10–12 walls from each
            for (var mazeIx = 0; mazeIx < 6; mazeIx++)
            {
                var hWalls = new bool[10, 9];
                var vWalls = new bool[10, 9];
                for (var i = 0; i < 10; i++)
                    for (var j = 0; j < 9; j++)
                    {
                        hWalls[i, j] = true;
                        vWalls[i, j] = true;
                    }

                var todo = Enumerable.Range(0, 100).ToList();
                var visited = new List<int>();

                var startingSquare = rnd.Next(0, 100);
                visited.Add(startingSquare);
                todo.RemoveAt(startingSquare);

                while (todo.Count > 0)
                {
                    // Pick a random square from “visited”
                    var randomSquareIx = rnd.Next(0, visited.Count);
                    var sq = visited[randomSquareIx];
                    var validNeighbors = new List<int>();
                    if (sq % 10 != 0 && todo.Contains(sq - 1))
                        validNeighbors.Add(sq - 1);
                    if (sq % 10 != 9 && todo.Contains(sq + 1))
                        validNeighbors.Add(sq + 1);
                    if (sq / 10 != 0 && todo.Contains(sq - 10))
                        validNeighbors.Add(sq - 10);
                    if (sq / 10 != 9 && todo.Contains(sq + 10))
                        validNeighbors.Add(sq + 10);

                    if (validNeighbors.Count == 0)
                    {
                        visited.RemoveAt(randomSquareIx);
                        continue;
                    }

                    var neighIx = rnd.Next(0, validNeighbors.Count);
                    var neigh = validNeighbors[neighIx];
                    if (sq % 10 == neigh % 10)
                        hWalls[sq % 10, Math.Min(sq / 10, neigh / 10)] = false;
                    else
                        vWalls[sq / 10, Math.Min(sq % 10, neigh % 10)] = false;
                    visited.Add(neigh);
                    todo.Remove(neigh);
                }

                // Remove between 2–4 random walls to create some loops
                var numWallsToRemove = rnd.Next(2, 5);
                while (numWallsToRemove > 0)
                {
                    var hv = rnd.Next(0, 2) != 0;
                    var i = rnd.Next(0, 10);
                    var j = rnd.Next(0, 9);
                    if (hv ? hWalls[i, j] : vWalls[i, j])
                    {
                        if (hv)
                            hWalls[i, j] = false;
                        else
                            vWalls[i, j] = false;
                        numWallsToRemove--;
                    }
                }

                generatedHWalls.Add(hWalls);
                generatedVWalls.Add(vWalls);
                generatedGoalFromTorus.Add(rnd.ShuffleFisherYates(Enumerable.Range(0, 4).ToArray()));
                generatedSphereColors.Add(rnd.ShuffleFisherYates(Enumerable.Range(0, 4).ToArray()));

                // This is redundant for the C# side, but the manual side does this to randomize the order of the torus colors
                rnd.ShuffleFisherYates(Enumerable.Range(0, 4).ToArray());
            }
        }
        // End of rule-seed code

        generateMaze(generatedHWalls, generatedVWalls, generatedGoalFromTorus, generatedSphereColors);

        var x = Random.Range(0, 10);
        var z = Random.Range(0, 10);

        // Start in a random direction, but not directly facing a wall.
        var direction = Random.Range(0, 4);
        while (
            direction == 0 ? (z == 9 || _horiWalls[x, z]) :
            direction == 1 ? (x == 9 || _vertWalls[z, x]) :
            direction == 2 ? (z == 0 || _horiWalls[x, z - 1]) :
            direction == 3 ? (x == 0 || _vertWalls[z, x - 1]) : false)
            direction = (direction + 1) % 4;

        _curCam = new CameraPosition(x, z, direction);
        TargetCamera.transform.localPosition = _curCam.Position;
        TargetCamera.transform.localRotation = _curCam.Rotation;
        StartCoroutine(positionCamera(_curCam));
    }

    void generateMaze(List<bool[,]> hWalls, List<bool[,]> vWalls, List<int[]> goalFromTorus, List<int[]> sphColors)
    {
        int mazeRand = Random.Range(0, 6);
        Debug.LogFormat("[Mouse in the Maze #{1}] You are in the {0} maze.", "top left|top right|middle left|middle right|bottom left|bottom right".Split('|')[mazeRand], _moduleId);

        if (hWalls.Count > 0)   // rule seeded mazes
        {
            _horiWalls = hWalls[mazeRand];
            _vertWalls = vWalls[mazeRand];
            _goalFromTorusColor = goalFromTorus[mazeRand];
            _sphereColors = sphColors[mazeRand];
        }
        else
        {
            // Default rule seed (original mazes)
            switch (mazeRand)
            {
                case 0: generateMaze1(); break;
                case 1: generateMaze2(); break;
                case 2: generateMaze3(); break;
                case 3: generateMaze4(); break;
                case 4: generateMaze5(); break;
                default: generateMaze6(); break;
            }
        }

        _torusColor = Random.Range(0, 4);
        _goalPosition = _goalFromTorusColor[_torusColor];

        Debug.LogFormat("[Mouse in the Maze #{1}] Torus color: {0}", "white|green|blue|yellow".Split('|')[_torusColor], _moduleId);
        Debug.LogFormat("[Mouse in the Maze #{1}] Goal sphere color: {0}", "white|green|blue|yellow".Split('|')[_sphereColors[_goalPosition]], _moduleId);
    }

    void solve()
    {
        GetComponent<KMBombModule>().HandlePass();
        _isSolved = true;
    }

    void strike()
    {
        Debug.LogFormat("[Mouse in the Maze #{0}] Strike because you pressed Submit on {1},{2} but the solution is {3},{4}.", _moduleId, _curCam.X, _curCam.Z, new[] { 2, 7, 7, 2 }[_goalPosition], new[] { 7, 7, 2, 2 }[_goalPosition]);
        GetComponent<KMBombModule>().HandleStrike();
    }

    void OnPress(int button)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[button].transform);

        if (_isActive && !_isSolved)
        {
            CameraPosition newCam = null;

            buttons[button].AddInteractionPunch(.5f);
            switch (button)
            {
                case 0:
                    if (_curCam.IsGoal(_goalPosition))
                        solve();
                    else
                        strike();
                    break;

                case 1: newCam = _curCam.MoveForward(_horiWalls, _vertWalls); break;
                case 2: newCam = _curCam.MoveBackward(_horiWalls, _vertWalls); break;
                case 3: newCam = _curCam.TurnLeft(); break;
                case 4: newCam = _curCam.TurnRight(); break;
            }

            if (newCam != null)
            {
                _queue.Enqueue(newCam);
                _curCam = newCam;
            }
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

        _isActive = false;
    }

    void OnActivate()
    {
        _isActive = true;
        TargetCamera.enabled = true;
    }

    void generateMaze1()
    {
        _horiWalls[1, 0] = true;
        _horiWalls[4, 0] = true;
        _horiWalls[5, 0] = true;
        _horiWalls[7, 0] = true;
        _horiWalls[8, 0] = true;
        _horiWalls[9, 0] = true;
        _horiWalls[3, 1] = true;
        _horiWalls[4, 1] = true;
        _horiWalls[6, 1] = true;
        _horiWalls[7, 1] = true;
        _horiWalls[8, 1] = true;
        _horiWalls[3, 2] = true;
        _horiWalls[4, 2] = true;
        _horiWalls[5, 2] = true;
        _horiWalls[8, 2] = true;
        _horiWalls[9, 2] = true;
        _horiWalls[1, 3] = true;
        _horiWalls[2, 3] = true;
        _horiWalls[5, 3] = true;
        _horiWalls[6, 3] = true;
        _horiWalls[8, 3] = true;
        _horiWalls[7, 3] = true;
        _horiWalls[0, 4] = true;
        _horiWalls[1, 4] = true;
        _horiWalls[8, 4] = true;
        _horiWalls[9, 4] = true;
        _horiWalls[3, 5] = true;
        _horiWalls[4, 5] = true;
        _horiWalls[3, 6] = true;
        _horiWalls[4, 6] = true;
        _horiWalls[5, 6] = true;
        _horiWalls[6, 6] = true;
        _horiWalls[8, 6] = true;
        _horiWalls[8, 7] = true;
        _horiWalls[2, 7] = true;
        _horiWalls[4, 7] = true;
        _horiWalls[5, 7] = true;
        _horiWalls[9, 7] = true;
        _horiWalls[0, 8] = true;
        _horiWalls[1, 8] = true;
        _horiWalls[3, 8] = true;
        _horiWalls[5, 8] = true;
        _horiWalls[7, 8] = true;
        _horiWalls[8, 8] = true;

        _vertWalls[1, 0] = true;
        _vertWalls[2, 0] = true;
        _vertWalls[3, 0] = true;
        _vertWalls[6, 0] = true;
        _vertWalls[7, 0] = true;
        _vertWalls[8, 0] = true;
        _vertWalls[1, 1] = true;
        _vertWalls[2, 1] = true;
        _vertWalls[5, 1] = true;
        _vertWalls[6, 1] = true;
        _vertWalls[8, 1] = true;
        _vertWalls[0, 2] = true;
        _vertWalls[1, 2] = true;
        _vertWalls[4, 2] = true;
        _vertWalls[5, 2] = true;
        _vertWalls[7, 2] = true;
        _vertWalls[1, 3] = true;
        _vertWalls[3, 3] = true;
        _vertWalls[4, 3] = true;
        _vertWalls[8, 3] = true;
        _vertWalls[5, 4] = true;
        _vertWalls[9, 4] = true;
        _vertWalls[1, 5] = true;
        _vertWalls[2, 5] = true;
        _vertWalls[5, 5] = true;
        _vertWalls[6, 5] = true;
        _vertWalls[8, 5] = true;
        _vertWalls[3, 6] = true;
        _vertWalls[4, 6] = true;
        _vertWalls[5, 6] = true;
        _vertWalls[7, 6] = true;
        _vertWalls[8, 6] = true;
        _vertWalls[5, 7] = true;
        _vertWalls[6, 7] = true;
        _vertWalls[6, 8] = true;

        _sphereColors = new int[] { 1, 2, 3, 0 };
        _goalFromTorusColor = new int[] { 0, 1, 3, 2 };
    }

    void generateMaze3()
    {
        _horiWalls[1, 0] = true;
        _horiWalls[8, 0] = true;
        _horiWalls[4, 1] = true;
        _horiWalls[5, 1] = true;
        _horiWalls[6, 1] = true;
        _horiWalls[7, 1] = true;
        _horiWalls[1, 2] = true;
        _horiWalls[2, 2] = true;
        _horiWalls[5, 2] = true;
        _horiWalls[8, 2] = true;
        _horiWalls[0, 3] = true;
        _horiWalls[1, 3] = true;
        _horiWalls[2, 3] = true;
        _horiWalls[9, 3] = true;
        _horiWalls[1, 4] = true;
        _horiWalls[2, 4] = true;
        _horiWalls[3, 4] = true;
        _horiWalls[5, 4] = true;
        _horiWalls[6, 4] = true;
        _horiWalls[8, 4] = true;
        _horiWalls[9, 4] = true;
        _horiWalls[2, 5] = true;
        _horiWalls[3, 5] = true;
        _horiWalls[5, 5] = true;
        _horiWalls[6, 5] = true;
        _horiWalls[7, 5] = true;
        _horiWalls[8, 5] = true;
        _horiWalls[1, 6] = true;
        _horiWalls[2, 6] = true;
        _horiWalls[4, 6] = true;
        _horiWalls[6, 6] = true;
        _horiWalls[9, 6] = true;
        _horiWalls[0, 7] = true;
        _horiWalls[1, 7] = true;
        _horiWalls[2, 7] = true;
        _horiWalls[7, 7] = true;
        _horiWalls[8, 7] = true;
        _horiWalls[1, 8] = true;
        _horiWalls[2, 8] = true;
        _horiWalls[6, 8] = true;
        _horiWalls[9, 8] = true;

        _vertWalls[1, 0] = true;
        _vertWalls[2, 0] = true;
        _vertWalls[5, 0] = true;
        _vertWalls[6, 0] = true;
        _vertWalls[1, 1] = true;
        _vertWalls[0, 2] = true;
        _vertWalls[1, 2] = true;
        _vertWalls[2, 2] = true;
        _vertWalls[8, 2] = true;
        _vertWalls[1, 3] = true;
        _vertWalls[2, 3] = true;
        _vertWalls[3, 3] = true;
        _vertWalls[4, 3] = true;
        _vertWalls[6, 3] = true;
        _vertWalls[7, 3] = true;
        _vertWalls[9, 3] = true;
        _vertWalls[0, 4] = true;
        _vertWalls[3, 4] = true;
        _vertWalls[4, 4] = true;
        _vertWalls[7, 4] = true;
        _vertWalls[8, 4] = true;
        _vertWalls[1, 5] = true;
        _vertWalls[3, 5] = true;
        _vertWalls[7, 5] = true;
        _vertWalls[8, 5] = true;
        _vertWalls[0, 6] = true;
        _vertWalls[2, 6] = true;
        _vertWalls[3, 6] = true;
        _vertWalls[4, 6] = true;
        _vertWalls[7, 6] = true;
        _vertWalls[1, 7] = true;
        _vertWalls[3, 7] = true;
        _vertWalls[4, 7] = true;
        _vertWalls[6, 7] = true;
        _vertWalls[7, 7] = true;
        _vertWalls[8, 7] = true;
        _vertWalls[9, 7] = true;
        _vertWalls[2, 8] = true;

        _sphereColors = new int[] { 1, 2, 0, 3 };
        _goalFromTorusColor = new int[] { 0, 1, 3, 2 };
    }

    void generateMaze5()
    {
        _horiWalls[1, 0] = true;
        _horiWalls[2, 0] = true;
        _horiWalls[3, 0] = true;
        _horiWalls[4, 0] = true;
        _horiWalls[1, 1] = true;
        _horiWalls[2, 1] = true;
        _horiWalls[3, 1] = true;
        _horiWalls[2, 2] = true;
        _horiWalls[6, 2] = true;
        _horiWalls[8, 2] = true;
        _horiWalls[9, 2] = true;
        _horiWalls[3, 3] = true;
        _horiWalls[8, 3] = true;
        _horiWalls[2, 4] = true;
        _horiWalls[3, 4] = true;
        _horiWalls[4, 4] = true;
        _horiWalls[7, 4] = true;
        _horiWalls[2, 5] = true;
        _horiWalls[4, 5] = true;
        _horiWalls[5, 5] = true;
        _horiWalls[6, 5] = true;
        _horiWalls[7, 5] = true;
        _horiWalls[0, 6] = true;
        _horiWalls[1, 6] = true;
        _horiWalls[3, 6] = true;
        _horiWalls[4, 6] = true;
        _horiWalls[5, 6] = true;
        _horiWalls[6, 6] = true;
        _horiWalls[9, 6] = true;
        _horiWalls[1, 7] = true;
        _horiWalls[2, 7] = true;
        _horiWalls[4, 7] = true;
        _horiWalls[5, 7] = true;
        _horiWalls[7, 7] = true;
        _horiWalls[1, 8] = true;
        _horiWalls[2, 8] = true;
        _horiWalls[3, 8] = true;
        _horiWalls[7, 8] = true;
        _horiWalls[8, 8] = true;

        _vertWalls[2, 0] = true;
        _vertWalls[3, 0] = true;
        _vertWalls[4, 0] = true;
        _vertWalls[5, 0] = true;
        _vertWalls[3, 1] = true;
        _vertWalls[4, 1] = true;
        _vertWalls[6, 2] = true;
        _vertWalls[7, 2] = true;
        _vertWalls[2, 3] = true;
        _vertWalls[3, 3] = true;
        _vertWalls[5, 3] = true;
        _vertWalls[8, 3] = true;
        _vertWalls[1, 4] = true;
        _vertWalls[2, 4] = true;
        _vertWalls[3, 4] = true;
        _vertWalls[4, 4] = true;
        _vertWalls[9, 4] = true;
        _vertWalls[1, 5] = true;
        _vertWalls[2, 5] = true;
        _vertWalls[4, 5] = true;
        _vertWalls[5, 5] = true;
        _vertWalls[8, 5] = true;
        _vertWalls[9, 5] = true;
        _vertWalls[0, 6] = true;
        _vertWalls[1, 6] = true;
        _vertWalls[3, 6] = true;
        _vertWalls[4, 6] = true;
        _vertWalls[7, 6] = true;
        _vertWalls[1, 7] = true;
        _vertWalls[2, 7] = true;
        _vertWalls[3, 7] = true;
        _vertWalls[5, 7] = true;
        _vertWalls[6, 7] = true;
        _vertWalls[7, 7] = true;
        _vertWalls[0, 8] = true;
        _vertWalls[1, 8] = true;
        _vertWalls[4, 8] = true;
        _vertWalls[5, 8] = true;
        _vertWalls[7, 8] = true;
        _vertWalls[8, 8] = true;

        _sphereColors = new int[] { 3, 1, 0, 2 };
        _goalFromTorusColor = new int[] { 0, 2, 1, 3 };
    }

    void generateMaze2()
    {
        _horiWalls[1, 0] = true;
        _horiWalls[2, 0] = true;
        _horiWalls[3, 0] = true;
        _horiWalls[4, 0] = true;
        _horiWalls[5, 0] = true;
        _horiWalls[8, 0] = true;
        _horiWalls[2, 1] = true;
        _horiWalls[3, 1] = true;
        _horiWalls[4, 1] = true;
        _horiWalls[8, 1] = true;
        _horiWalls[9, 1] = true;
        _horiWalls[0, 2] = true;
        _horiWalls[1, 2] = true;
        _horiWalls[3, 2] = true;
        _horiWalls[4, 2] = true;
        _horiWalls[6, 2] = true;
        _horiWalls[7, 2] = true;
        _horiWalls[1, 3] = true;
        _horiWalls[4, 3] = true;
        _horiWalls[6, 3] = true;
        _horiWalls[8, 3] = true;
        _horiWalls[0, 4] = true;
        _horiWalls[2, 4] = true;
        _horiWalls[3, 4] = true;
        _horiWalls[5, 4] = true;
        _horiWalls[8, 4] = true;
        _horiWalls[1, 5] = true;
        _horiWalls[3, 5] = true;
        _horiWalls[4, 5] = true;
        _horiWalls[5, 5] = true;
        _horiWalls[9, 5] = true;
        _horiWalls[2, 6] = true;
        _horiWalls[3, 6] = true;
        _horiWalls[7, 6] = true;
        _horiWalls[8, 6] = true;
        _horiWalls[0, 7] = true;
        _horiWalls[3, 7] = true;
        _horiWalls[6, 7] = true;
        _horiWalls[7, 7] = true;
        _horiWalls[9, 7] = true;
        _horiWalls[1, 8] = true;
        _horiWalls[4, 8] = true;
        _horiWalls[5, 8] = true;
        _horiWalls[6, 8] = true;
        _horiWalls[8, 8] = true;

        _vertWalls[1, 0] = true;
        _vertWalls[6, 0] = true;
        _vertWalls[7, 0] = true;
        _vertWalls[2, 1] = true;
        _vertWalls[4, 1] = true;
        _vertWalls[5, 1] = true;
        _vertWalls[7, 1] = true;
        _vertWalls[8, 1] = true;
        _vertWalls[3, 2] = true;
        _vertWalls[8, 2] = true;
        _vertWalls[9, 2] = true;
        _vertWalls[2, 4] = true;
        _vertWalls[4, 4] = true;
        _vertWalls[6, 4] = true;
        _vertWalls[7, 4] = true;
        _vertWalls[1, 5] = true;
        _vertWalls[2, 5] = true;
        _vertWalls[5, 5] = true;
        _vertWalls[6, 5] = true;
        _vertWalls[3, 5] = true;
        _vertWalls[0, 6] = true;
        _vertWalls[1, 6] = true;
        _vertWalls[4, 6] = true;
        _vertWalls[5, 6] = true;
        _vertWalls[7, 6] = true;
        _vertWalls[9, 6] = true;
        _vertWalls[1, 7] = true;
        _vertWalls[5, 7] = true;
        _vertWalls[6, 7] = true;
        _vertWalls[8, 7] = true;
        _vertWalls[2, 8] = true;
        _vertWalls[3, 8] = true;

        _sphereColors = new int[] { 0, 3, 1, 2 };
        _goalFromTorusColor = new int[] { 1, 2, 0, 3 };
    }

    void generateMaze4()
    {
        _horiWalls[8, 0] = true;
        _horiWalls[9, 0] = true;
        _horiWalls[1, 1] = true;
        _horiWalls[2, 1] = true;
        _horiWalls[5, 1] = true;
        _horiWalls[6, 1] = true;
        _horiWalls[7, 1] = true;
        _horiWalls[8, 1] = true;
        _horiWalls[0, 2] = true;
        _horiWalls[2, 2] = true;
        _horiWalls[3, 2] = true;
        _horiWalls[4, 2] = true;
        _horiWalls[6, 2] = true;
        _horiWalls[8, 2] = true;
        _horiWalls[2, 3] = true;
        _horiWalls[3, 3] = true;
        _horiWalls[5, 3] = true;
        _horiWalls[6, 3] = true;
        _horiWalls[7, 3] = true;
        _horiWalls[1, 4] = true;
        _horiWalls[2, 4] = true;
        _horiWalls[6, 4] = true;
        _horiWalls[7, 4] = true;
        _horiWalls[8, 4] = true;
        _horiWalls[0, 5] = true;
        _horiWalls[1, 5] = true;
        _horiWalls[2, 5] = true;
        _horiWalls[3, 5] = true;
        _horiWalls[8, 5] = true;
        _horiWalls[7, 5] = true;
        _horiWalls[1, 6] = true;
        _horiWalls[2, 6] = true;
        _horiWalls[3, 6] = true;
        _horiWalls[4, 6] = true;
        _horiWalls[9, 6] = true;
        _horiWalls[2, 7] = true;
        _horiWalls[6, 7] = true;
        _horiWalls[7, 7] = true;
        _horiWalls[1, 8] = true;
        _horiWalls[2, 8] = true;
        _horiWalls[5, 8] = true;
        _horiWalls[6, 8] = true;
        _horiWalls[8, 8] = true;

        _vertWalls[1, 0] = true;
        _vertWalls[4, 0] = true;
        _vertWalls[7, 0] = true;
        _vertWalls[8, 0] = true;
        _vertWalls[0, 1] = true;
        _vertWalls[5, 1] = true;
        _vertWalls[1, 2] = true;
        _vertWalls[7, 2] = true;
        _vertWalls[0, 3] = true;
        _vertWalls[1, 3] = true;
        _vertWalls[4, 3] = true;
        _vertWalls[5, 3] = true;
        _vertWalls[8, 3] = true;
        _vertWalls[9, 3] = true;
        _vertWalls[1, 4] = true;
        _vertWalls[2, 4] = true;
        _vertWalls[3, 4] = true;
        _vertWalls[4, 4] = true;
        _vertWalls[5, 4] = true;
        _vertWalls[6, 4] = true;
        _vertWalls[7, 4] = true;
        _vertWalls[8, 4] = true;
        _vertWalls[0, 5] = true;
        _vertWalls[5, 5] = true;
        _vertWalls[6, 5] = true;
        _vertWalls[1, 6] = true;
        _vertWalls[3, 6] = true;
        _vertWalls[6, 6] = true;
        _vertWalls[7, 7] = true;
        _vertWalls[8, 7] = true;
        _vertWalls[3, 8] = true;
        _vertWalls[4, 8] = true;
        _vertWalls[6, 8] = true;
        _vertWalls[8, 8] = true;

        _sphereColors = new int[] { 3, 0, 2, 1 };
        _goalFromTorusColor = new int[] { 2, 1, 3, 0 };
    }

    void generateMaze6()
    {
        _horiWalls[1, 0] = true;
        _horiWalls[3, 0] = true;
        _horiWalls[4, 0] = true;
        _horiWalls[7, 0] = true;
        _horiWalls[8, 0] = true;
        _horiWalls[2, 1] = true;
        _horiWalls[4, 1] = true;
        _horiWalls[5, 1] = true;
        _horiWalls[8, 1] = true;
        _horiWalls[1, 2] = true;
        _horiWalls[2, 2] = true;
        _horiWalls[3, 2] = true;
        _horiWalls[4, 2] = true;
        _horiWalls[6, 2] = true;
        _horiWalls[2, 3] = true;
        _horiWalls[5, 3] = true;
        _horiWalls[7, 3] = true;
        _horiWalls[8, 3] = true;
        _horiWalls[0, 4] = true;
        _horiWalls[6, 4] = true;
        _horiWalls[7, 4] = true;
        _horiWalls[9, 4] = true;
        _horiWalls[2, 5] = true;
        _horiWalls[3, 5] = true;
        _horiWalls[6, 5] = true;
        _horiWalls[8, 5] = true;
        _horiWalls[1, 6] = true;
        _horiWalls[2, 6] = true;
        _horiWalls[4, 6] = true;
        _horiWalls[5, 6] = true;
        _horiWalls[7, 6] = true;
        _horiWalls[3, 7] = true;
        _horiWalls[5, 7] = true;
        _horiWalls[6, 7] = true;
        _horiWalls[2, 8] = true;
        _horiWalls[4, 8] = true;
        _horiWalls[7, 8] = true;
        _horiWalls[8, 8] = true;

        _vertWalls[1, 0] = true;
        _vertWalls[2, 0] = true;
        _vertWalls[3, 0] = true;
        _vertWalls[5, 0] = true;
        _vertWalls[7, 0] = true;
        _vertWalls[8, 0] = true;
        _vertWalls[4, 1] = true;
        _vertWalls[5, 1] = true;
        _vertWalls[6, 1] = true;
        _vertWalls[8, 1] = true;
        _vertWalls[9, 1] = true;
        _vertWalls[1, 2] = true;
        _vertWalls[2, 2] = true;
        _vertWalls[4, 2] = true;
        _vertWalls[7, 2] = true;
        _vertWalls[3, 3] = true;
        _vertWalls[4, 3] = true;
        _vertWalls[8, 3] = true;
        _vertWalls[4, 4] = true;
        _vertWalls[5, 4] = true;
        _vertWalls[6, 4] = true;
        _vertWalls[0, 5] = true;
        _vertWalls[1, 5] = true;
        _vertWalls[3, 5] = true;
        _vertWalls[8, 5] = true;
        _vertWalls[9, 5] = true;
        _vertWalls[1, 6] = true;
        _vertWalls[2, 6] = true;
        _vertWalls[4, 6] = true;
        _vertWalls[6, 6] = true;
        _vertWalls[2, 7] = true;
        _vertWalls[3, 7] = true;
        _vertWalls[5, 7] = true;
        _vertWalls[7, 7] = true;
        _vertWalls[8, 7] = true;
        _vertWalls[3, 8] = true;
        _vertWalls[4, 8] = true;
        _vertWalls[6, 8] = true;
        _vertWalls[7, 8] = true;

        _sphereColors = new int[] { 2, 3, 0, 1 };
        _goalFromTorusColor = new int[] { 0, 1, 3, 2 };
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"!{0} f[orward] b[ack] l[eft] r[ight] u[-turn] | !{0} submit | !{0} enablemaze/disablemaze [to sidestep visual glitches with other modules]";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*disablemaze\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            _tpMazeTemporarilyDisabled = true;
            return new KMSelectable[0];
        }
        if (Regex.IsMatch(command, @"^\s*enablemaze\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            _tpMazeTemporarilyDisabled = false;
            return new KMSelectable[0];
        }

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

    struct CamQueueItem
    {
        public CameraPosition OldPos;
        public CameraPosition NewPos;
        public int Button;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_isSolved)
            yield break;

        _tpMazeTemporarilyDisabled = false;
        yield return null;

        // 1=forward, 2=backward, 3=turn left, 4=turn right
        var q = new Queue<CamQueueItem>();
        var already = new Dictionary<CameraPosition, CamQueueItem>();
        q.Enqueue(new CamQueueItem { OldPos = null, NewPos = _curCam, Button = 0 });
        CameraPosition goalCam = null;
        while (q.Count > 0)
        {
            var item = q.Dequeue();
            if (already.ContainsKey(item.NewPos))
                continue;
            already[item.NewPos] = item;
            if (item.NewPos.IsGoal(_goalPosition))
            {
                goalCam = item.NewPos;
                break;
            }

            var options = new[] { item.NewPos.MoveForward(_horiWalls, _vertWalls), item.NewPos.MoveBackward(_horiWalls, _vertWalls), item.NewPos.TurnLeft(), item.NewPos.TurnRight() };
            for (var i = 0; i < options.Length; i++)
                if (options[i] != null)
                    q.Enqueue(new CamQueueItem { OldPos = item.NewPos, NewPos = options[i], Button = i + 1 });
        }

        var btns = new List<int>();
        var pos = goalCam;
        while (pos != null)
        {
            btns.Add(already[pos].Button);
            pos = already[pos].OldPos;
        }
        for (int i = btns.Count - 2; i >= 0; i--)
        {
            buttons[btns[i]].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        while (_queue.Count > 0)
            yield return true;
        yield return new WaitForSeconds(.1f);
        buttons[0].OnInteract();
    }
}

