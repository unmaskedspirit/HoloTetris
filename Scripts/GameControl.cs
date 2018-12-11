using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class GameControl : MonoBehaviour {

    [SerializeField]
    GameObject mainCursor;

    [SerializeField]
    float zDistance = 8.0f;

    [SerializeField]
    int levelUpLines = 5;

    AudioSource audioSource1;
    AudioSource audioSource2;

    [SerializeField]
    AudioClip lineSound;

    [SerializeField]
    AudioClip levelUpSound;

    [SerializeField]
    AudioClip gameOverSound;

    [SerializeField]
    AudioClip startMusic;

    [SerializeField]
    AudioClip backgroundSound;

    private bool exitCalibration = false;


    public static float gridWidth = 1.0f;
    public static float gridHeight = 2.0f;
    public static float gridSize = 0.1f;

    static int gridRows = (int)Mathf.Round(gridWidth / gridSize);
    static int gridCols = (int)Mathf.Round(gridHeight / gridSize);

    private float spawnX = 0.5f;
    private float spawnY = gridHeight;
    private Vector3 spawnVector;
    private Vector3 nextVector;


    public static Transform[,] grid;
    public static Transform[,] grid_2P;


    private GameObject scoreSheet;
    private GameObject multiSheet;

    private GameObject tetrisGrid;
    private GameObject tetrisCanvas;
    private GameObject gameOverSplash;
    private GameObject startGameSplash;
    private int gameScore;
    private float scoreScale = 1.0f;
    private int lineScore;
    private int levelValue;
    private string messageString;

    private float fallSpeed = 1.0f;
    private string currentTetrominoName;
    private string nextTetrominoName;

    private bool printDebugStats = false;

    private bool isMultiplayer = false;

    private int writeCount;
    private string[] mpGridLines;
    private int mpLinesRead;
    

    //Only used for MP mode
    private int[] fixedMinos;
    private int fixedMinosIndex;

    // Use this for initialization
    void Start()
    {

        //Get audio sources and play background
        audioSource1 = gameObject.AddComponent<AudioSource>();
        audioSource2 = gameObject.AddComponent<AudioSource>();
        audioSource2.loop = true;
        audioSource2.playOnAwake = true;
        audioSource2.volume = 0.3f;

        //Set game distance based on calibration
        SetGameZDistance(zDistance);

        mainCursor.transform.localScale *= 2.0f;

        //Debug only. Remove after
        
        PreloadFixedMinos();
        //Debug only. Remove after

        //Go to main menu
        MainMenu();
    }

    //Function call to set the Z distance
    public void SetGameZDistance(float zVal)
    {
        zDistance = RoundToGridSpacing(zVal);
        Vector3 cursorPosition = mainCursor.transform.position;

        //mainCursor.GetComponentInChildren<ObjectCursor>().
        mainCursor.GetComponent<HoloToolkit.Unity.InputModule.Cursor>().MinCursorDistance = RoundToGridSpacing(zDistance);
        mainCursor.GetComponent<HoloToolkit.Unity.InputModule.Cursor>().DefaultCursorDistance = RoundToGridSpacing(zDistance);
        //mainCursor.transform.localScale *= 2.0f;
        mainCursor.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, RoundToGridSpacing(zDistance));
    }

    //Function call to get the Z distance
    public float GetGameZDistance()
    {
        return zDistance;
    }

    private void RemoveAllObjects()
    {
        //Remove Game Over
        var GameOverObjects = GameObject.FindGameObjectsWithTag("GameOver");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        //Remove Tetrominos
        var tetrominoObjects = GameObject.FindGameObjectsWithTag("Tetromino");
        foreach (var item in tetrominoObjects)
        {
            Destroy(item);
        }

        //Remove Start Menu
        var StartMenuObjects = GameObject.FindGameObjectsWithTag("StartMenu");
        foreach (var item in StartMenuObjects)
        {
            Destroy(item);
        }

        var TetrominoObjects = GameObject.FindGameObjectsWithTag("NextTetromino");
        foreach (var item in TetrominoObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

    }

    //Function call for main menu
    public void MainMenu()
    {
        audioSource2.loop = true;
        audioSource2.playOnAwake = true;
        audioSource2.clip = startMusic;
        audioSource2.Play();

        //Remove Game Over
        var GameOverObjects = GameObject.FindGameObjectsWithTag("GameOver");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        //Remove Game objects
        var tetrominoObjects = GameObject.FindGameObjectsWithTag("Tetromino");
        foreach (var item in tetrominoObjects)
        {
            Destroy(item);
        }

        //Remove Start Menu
        var StartMenuObjects = GameObject.FindGameObjectsWithTag("StartMenu");
        foreach (var item in StartMenuObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        isMultiplayer = false;
        startGameSplash = (GameObject)Instantiate(Resources.Load("Prefabs/StartMenuSheet", typeof(GameObject)), new Vector3(-4.7f, -1.5f, (zDistance + 4.0f)), Quaternion.identity);
    }

    //Function call for game calibration
    public void CalibrateGame()
    {
        //exitCalibration = false;

        //Remove Game Over
        var GameOverObjects = GameObject.FindGameObjectsWithTag("GameOver");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        //Remove Start Menu
        var StartMenuObjects = GameObject.FindGameObjectsWithTag("StartMenu");
        foreach (var item in StartMenuObjects)
        {
            Destroy(item);
        }

        //Remove current Tetromino Objects
        var tetrominoObjects = GameObject.FindGameObjectsWithTag("Tetromino");
        foreach (var item in tetrominoObjects)
        {
            Destroy(item);
        }

        //Remove next Tetromino Objects
        var nextObjects = GameObject.FindGameObjectsWithTag("NextTetromino");
        foreach (var item in nextObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        CreateNewGrid();
        //Create calibration GUI
        var calibrationOptions = (GameObject)Instantiate(Resources.Load("Prefabs/CalibrationCanvas", typeof(GameObject)), new Vector3(-1.55f, 0.0f, zDistance), Quaternion.identity);
        //Create calibration tetromino
        Vector3 calibrationTetrominoPos = new Vector3(0.5f, 1.7f, zDistance);
        GameObject calibrationTetromino = (GameObject)Instantiate(Resources.Load("Prefabs/CalibrateTetromino/Tetromino_T", typeof(GameObject)), calibrationTetrominoPos, Quaternion.identity);

    }

    public void CreateNewGrid()
    {
        tetrisGrid = (GameObject)Instantiate(Resources.Load("Prefabs/Grid", typeof(GameObject)), new Vector3(0.0f, 0.0f, zDistance), Quaternion.identity);
        tetrisCanvas = (GameObject)Instantiate(Resources.Load("Prefabs/GridCanvas", typeof(GameObject)), new Vector3(-0.05f, -0.05f, zDistance), Quaternion.identity);
    }

    //Create Multiplayer game
    public void MultiplayerGame()
    {
        var GameOverObjects = GameObject.FindGameObjectsWithTag("GameOver");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        //Remove Start Menu
        var StartMenuObjects = GameObject.FindGameObjectsWithTag("StartMenu");
        foreach (var item in StartMenuObjects)
        {
            Destroy(item);
        }

        //Remove current Tetromino Objects
        var tetrominoObjects = GameObject.FindGameObjectsWithTag("Tetromino");
        foreach (var item in tetrominoObjects)
        {
            Destroy(item);
        }

        //Remove next Tetromino Objects
        var nextObjects = GameObject.FindGameObjectsWithTag("NextTetromino");
        foreach (var item in nextObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        isMultiplayer = true;
        fixedMinosIndex = 0;

        //Create multiplayer sheet
        multiSheet = (GameObject)Instantiate(Resources.Load("Prefabs/MultiplayerSheet", typeof(GameObject)), new Vector3(-1.0f, 0.5f, zDistance), Quaternion.identity);

        CreateNewGrid();
        CreateMultiplayerGrid();

        grid = new Transform[gridRows, gridCols];
        grid_2P = new Transform[gridRows, gridCols];

        gameScore = 0;
        lineScore = 0;
        levelValue = 0;
        fallSpeed = 1.0f;
        scoreScale = 1.0f;
        messageString = "";
        writeCount = 0;

        SetLevelUpdateStats();
        //Spawn location
        spawnVector = RoundVectorToGridSpacing(new Vector3(spawnX, spawnY, zDistance));

        //Read saved grid to string
        //mpGridLines = System.IO.File.ReadAllLines(@"C:\Users\mukad\Desktop\HoloTetrisGrid.txt");

        TextAsset txt = (TextAsset)Resources.Load("HoloTetrisGrid", typeof(TextAsset));
        mpGridLines = txt.text.Split('\n');

        Debug.Log("Number of lines of read grid " + mpGridLines.Length);    
        mpLinesRead = 0;

        SpawnNextMPTetromino();
    }

    public void CreateMultiplayerGrid()
    {
        tetrisGrid = (GameObject)Instantiate(Resources.Load("Prefabs/Grid", typeof(GameObject)), new Vector3(2.0f, 0.0f, zDistance), Quaternion.identity);
        tetrisCanvas = (GameObject)Instantiate(Resources.Load("Prefabs/GridCanvas", typeof(GameObject)), new Vector3(1.95f, -0.05f, zDistance), Quaternion.identity);
    }

    //Function call for starting a new game
    public void StartGame()
    {
        //Remove Game Over
        var GameOverObjects = GameObject.FindGameObjectsWithTag("GameOver");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        //Remove Start Menu
        var StartMenuObjects = GameObject.FindGameObjectsWithTag("StartMenu");
        foreach (var item in StartMenuObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        scoreSheet = (GameObject)Instantiate(Resources.Load("Prefabs/ScoreSheet", typeof(GameObject)), new Vector3(-1.0f, 0.5f, zDistance), Quaternion.identity);

        CreateNewGrid();

        isMultiplayer = false;
        gameScore = 0;
        lineScore = 0;
        levelValue = 0;
        fallSpeed = 1.0f;
        scoreScale = 1.0f;
        messageString = "";
        writeCount = 0;

        SetLevelUpdateStats();


        UpdateScoreSheet();
        grid = new Transform[gridRows, gridCols];

        audioSource2.Stop();

        audioSource2.clip = backgroundSound;
        audioSource2.loop = true;
        audioSource2.playOnAwake = true;
        audioSource2.volume = 0.3f;

        audioSource2.Play();

        //Spawn location
        spawnVector = RoundVectorToGridSpacing(new Vector3(spawnX, spawnY, zDistance));
        nextVector = RoundVectorToGridSpacing(new Vector3(1.3f, spawnY - 0.5f, zDistance));

        //currentTetrominoName = "Prefabs/" + GetRandomTetromino();
        currentTetrominoName = GetRandomTetromino(true);
        nextTetrominoName = "";


        SpawnNextTetromino();
        Debug.Log("Spawn");
    }

    //Toggle audio mute on/off
    public void MuteBackground()
    {
        audioSource2.mute = !audioSource2.mute;
    }

    private void UpdateScoreSheet()
    {

        if (isMultiplayer)
        {
            multiSheet.transform.Find("LevelText").GetComponentInChildren<Text>().text = "Level:\t" + levelValue.ToString();

            multiSheet.transform.Find("ScoreText").GetComponentInChildren<Text>().text = "Score:\t" + gameScore.ToString();
            multiSheet.transform.Find("LinesText").GetComponentInChildren<Text>().text = "Lines:\t" + lineScore.ToString();
        }

        else
        {
            scoreSheet.transform.Find("LevelText").GetComponentInChildren<Text>().text = "Level:\t" + levelValue.ToString();

            scoreSheet.transform.Find("ScoreText").GetComponentInChildren<Text>().text = "Score:\t" + gameScore.ToString();
            scoreSheet.transform.Find("LinesText").GetComponentInChildren<Text>().text = "Lines:\t" + lineScore.ToString();

            try
            {

                scoreSheet.transform.Find("MessageText").GetComponentInChildren<Text>().text = messageString;

                string statsMessage = "Score Scale: " + scoreScale.ToString() + "\n" + "Fall Speed: " + fallSpeed.ToString();

                if (printDebugStats)
                {
                    scoreSheet.transform.Find("StatsText").GetComponentInChildren<Text>().text = statsMessage;
                }
                else
                {
                    scoreSheet.transform.Find("StatsText").GetComponentInChildren<Text>().text = "";
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.Log("Caught exception");
            }
        }
    }

    public void GameOver()
    {
        //Load the game over scene

        var TetrominoObjects = GameObject.FindGameObjectsWithTag("Tetromino");
        foreach (var item in TetrominoObjects)
        {
            Destroy(item);
        }

        TetrominoObjects = GameObject.FindGameObjectsWithTag("NextTetromino");
        foreach (var item in TetrominoObjects)
        {
            Destroy(item);
        }

        //Remove MP objects
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        audioSource2.Stop();

        //Add some delay here?

        //Play game over
        audioSource1.PlayOneShot(gameOverSound, 0.7f);
        gameOverSplash = (GameObject)Instantiate(Resources.Load("Prefabs/GameOverSheet", typeof(GameObject)), new Vector3(-1.0f, 0.3f, zDistance), Quaternion.identity);
        //var ScoreSheet = GameObject.FindGameObjectsWithTag
    }

    //This function sets the fall speed and scoring rate based on the level
    private void SetLevelUpdateStats()
    {

        switch (levelValue)
        {
            case 0:
                fallSpeed = 0.8f;
                scoreScale = 1.0f;
                break;
            case 1:
                fallSpeed = 0.72f;
                scoreScale = 1.0f;
                break;
            case 2:
                fallSpeed = 0.63f;
                scoreScale = 1.20f;
                break;
            case 3:
                fallSpeed = 0.55f;
                scoreScale = 1.3f;
                break;
            case 4:
                fallSpeed = 0.47f;
                scoreScale = 1.4f;
                break;
            case 5:
                fallSpeed = 0.38f;
                scoreScale = 1.5f;
                break;
            case 6:
                fallSpeed = 0.3f;
                scoreScale = 2.0f;
                break;
            default:
                fallSpeed = 0.25f;
                scoreScale = 2.0f;
                break;
        }

    }

    public void SpawnNextMPTetromino()
    {
        spawnX = UnityEngine.Random.Range(0.2f, 0.7f);
        spawnVector = RoundVectorToGridSpacing(new Vector3(spawnX, spawnY, zDistance));

        //For MP game we spawn from a predetermined set
        currentTetrominoName = GetRandomTetromino(false, fixedMinos[fixedMinosIndex]);
        fixedMinosIndex++;
        //Wrap around the pieces
        if (fixedMinosIndex == fixedMinos.Length)
        {
            fixedMinosIndex = 0;
        }

        GameObject currentTetromino = (GameObject)Instantiate(Resources.Load("Prefabs/" + currentTetrominoName, typeof(GameObject)), spawnVector, Quaternion.identity);
        FindObjectOfType<ObjectControl>().SetFallSpeed(fallSpeed);
        FindObjectOfType<ObjectControl>().SetMPOption(isMultiplayer);
    }

    public void SpawnNextTetromino()
    {
        Debug.Log("Spawning a tetromino");

        
        Debug.Log("Current tetromino " + currentTetrominoName);
        spawnX = UnityEngine.Random.Range(0.2f, 0.7f);
        spawnVector = RoundVectorToGridSpacing(new Vector3(spawnX, spawnY, zDistance));
        GameObject currentTetromino = (GameObject)Instantiate(Resources.Load("Prefabs/" + currentTetrominoName, typeof(GameObject)), spawnVector, Quaternion.identity);
        FindObjectOfType<ObjectControl>().SetFallSpeed(fallSpeed);
        FindObjectOfType<ObjectControl>().SetMPOption(isMultiplayer);

        nextTetrominoName = GetRandomTetromino(true);

        Debug.Log("Next tetromino " + nextTetrominoName);
        //Assign current to next for the next spawn
        currentTetrominoName = nextTetrominoName;


        //First remove any next tetromino objects
        //Remove Game Over
        var GameOverObjects = GameObject.FindGameObjectsWithTag("NextTetromino");
        foreach (var item in GameOverObjects)
        {
            Destroy(item);
        }

        GameObject nextTetromino = (GameObject)Instantiate(Resources.Load("Prefabs/NextTetromino/" + nextTetrominoName, typeof(GameObject)), nextVector, Quaternion.identity);
    }

    //Get a random tetromino
    string GetRandomTetromino(bool randomize, int index = 1)
    {
        int randomTetromino;
        if (randomize)
        {
            randomTetromino = UnityEngine.Random.Range(1, 23);
        }
        else
        {
            randomTetromino = index;
        }
        

        string randomTetrominoName = "Tetromino_J";

        switch (randomTetromino)
        {
            case 1:
            case 2:
            case 3:
                randomTetrominoName = "Tetromino_I";
                break;
            case 4:
            case 5:
            case 6:
                randomTetrominoName = "Tetromino_J";
                break;
            case 7:
            case 8:
            case 9:
                randomTetrominoName = "Tetromino_L";
                break;
            case 10:
            case 11:
            case 12:
                randomTetrominoName = "Tetromino_O";
                break;
            case 13:
            case 14:
            case 15:
                randomTetrominoName = "Tetromino_S";
                break;
            case 16:
            case 17:
            case 18:
                randomTetrominoName = "Tetromino_T";
                break;
            case 19:
            case 20:
            case 21:
                randomTetrominoName = "Tetromino_Z";
                break;
            case 22:
                randomTetrominoName = "Tetromino_Bonus";
                break;

        }
        //Debug.Log("Next tetromino is " + randomTetrominoName);
        return randomTetrominoName;
    }

    public float GetGridSize()
    {
        return gridSize;
    }

    public float GetGridWidth()
    {
        return gridWidth;
    }

    //Function to return a vector that is rounded to the nearest grid space
    public Vector3 RoundVectorToGridSpacing(Vector3 pos)
    {
        Vector3 returnVector;
        returnVector.x = RoundToGridSpacing(pos.x);
        returnVector.y = RoundToGridSpacing(pos.y);
        returnVector.z = RoundToGridSpacing(pos.z);
        return returnVector;
    }

    //Function to return a float that is rounded to the nearest grid space
    public float RoundToGridSpacing(float value)
    {
        return (float)(Mathf.RoundToInt(value / gridSize)) * gridSize;
    }

    //Check if tetromoni is above grid
    public bool CheckIsAboveGrid(ObjectControl tetromino)
    {
        //Debug.Log("Checking if above grid");
        for (int x = 0; x < gridRows; ++x)
        {
            foreach (Transform mino in tetromino.transform)
            {
                Vector3 pos = RoundVectorToGridSpacing(mino.transform.position);
                if (pos.y >= gridHeight)
                {
                    return true;
                }
            }
        }
        return false;
    }

    //Check if vector is inside grid
    public bool checkIsInGrid(Vector3 pos)
    {
        //if ((pos.x - 0.0f < tolerance) || (pos.x - gridWidth> tolerance) || (pos.y - 0.0f > tolerance))
        if (pos.x < (0.0f) || pos.x >= gridWidth || pos.y < (0.0f))
        {
            //Debug.Log("X position: " + pos.x);
            //Debug.Log("Y position: " + pos.y);
            return false;

        }
        else
        {
            return true;
        }
        //return (pos.x >= 0.0f && pos.x < gridWidth && pos.y >= 0.0f);
    }

    public bool checkIsBeyondLeft(Vector3 pos)
    {
        return (pos.x < 0.0f);
    }

    public bool checkIsBeyondRight(Vector3 pos)
    {
        return (pos.x >= gridWidth);
    }

    public bool checkIsBeyondFloor(Vector3 pos)
    {
        if (pos.y < 0.0f)
        {
            return true;
        }
        return false;
    }

    //Check if the current row is full
    public bool IsFullRowAt(int y)
    {
        for (int x = 0; x < gridRows; ++x)
        {
            if (grid[x, y] == null)
            {
                return false;
            }
        }
        return true;
    }

    //Used to remove mino at location
    public void DeleteMinoAt(int y)
    {
        for (int x = 0; x < gridRows; ++x)
        {
            Destroy(grid[x, y].gameObject);
            grid[x, y] = null;
        }
    }

    //Read grid from a file and print to multiplayer grid
    public void ReadGridFromFile()
    {
        float xPos = 2.0f;
        float yPos = 0.0f;

        //First remove all MP tetrominos from scene
        var MPObjects = GameObject.FindGameObjectsWithTag("MPTetromino");
        foreach (var item in MPObjects)
        {
            Destroy(item);
        }

        for (int i=0; i<21; ++i)
        {
            if (i > 0)
            {
                //Debug.Log("Line: "+mpLinesRead + mpGridLines[mpLinesRead]);
                //We will use this information to create minos on the other grid
                string[] minos = mpGridLines[mpLinesRead].Split(' ');
                //Debug.Log("Length per line: " + minos.Length);
                xPos = 2.0f;
                for (int j=0; j < 10; ++j)
                {
                    if (minos[j] == "null")
                    {

                    }
                    else
                    {
                        Vector3 mpMinoPosition = new Vector3(RoundToGridSpacing(xPos), RoundToGridSpacing(yPos), RoundToGridSpacing(zDistance));
                        GameObject mpTetromino = (GameObject)Instantiate(Resources.Load("Prefabs/MPTetromino/Tetromino_Bonus", typeof(GameObject)), mpMinoPosition, Quaternion.identity);
                    }
                    xPos += gridSize;
                }

                yPos += gridSize;

            }
            mpLinesRead++;
            if (mpLinesRead == mpGridLines.Length)
            {
                mpLinesRead = 0;
            }
        }
    }

    //Write grid at end of every line to the file
    //Not used
    public void WriteGridToFile()
    {
        /*
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\mukad\Desktop\HoloTetrisGrid.txt", true))
        {
            file.WriteLine(writeCount.ToString());
            for (int y=0; y<gridCols; ++y)
            {
                for (int x=0; x<gridRows; ++x)
                {
                    if (grid[x,y] == null)
                    {
                        file.Write("null");
                    }
                    else
                    {
                        file.Write(grid[x, y].gameObject.name);
                    }
                    file.Write(" ");
                }
                file.WriteLine("");
            }
            //file.Write("\n");
            writeCount++;
        }
        */
    }

    public void DeleteRow()
    {
        int numLinesThisTurn = 0;
        for (int y = 0; y < gridCols; ++y)
        {
            if (IsFullRowAt(y))
            {
                DeleteMinoAt(y);

                //Update score
                numLinesThisTurn++;
                gameScore++;
                lineScore++;
                //Play line sound
                audioSource1.PlayOneShot(lineSound, 0.7f);


                //Move all rows, starting with the row above
                MoveAllRowsDown(y + 1);

                //Since a row was removed, we have to decrement y
                --y;
            }
        }

        //Only use this once to set up a MP game
        //WriteGridToFile();


        //Classic Tetris scoring applied

        int scoreThisTurn = 0;
        switch (numLinesThisTurn)
        {

            case 0:
                scoreThisTurn = 5; //soft drop score regardless of the level
                break;
            case 1:
                scoreThisTurn = (40 * (levelValue + 1));
                break;
            case 2:
                scoreThisTurn = (100 * (levelValue + 1));
                break;
            case 3:
                scoreThisTurn = (300 * (levelValue + 1));
                break;
            case 4:
                scoreThisTurn = (1200 * (levelValue + 1));
                break;
            default:
                scoreThisTurn = (7200 * (levelValue + 1));
                break;
        }


        //Update score
        gameScore += scoreThisTurn;
        messageString = "";
        //Check if level has increased
        int currentLevelValue = levelValue;
        levelValue = (int)Mathf.RoundToInt(lineScore / levelUpLines);

        if (levelValue > currentLevelValue)
        {
            //Play level up sound
            audioSource1.PlayOneShot(levelUpSound, 0.7f);
            messageString = "Level UP!\n";
        }

        //Call function to update speed and score rate
        SetLevelUpdateStats();


        if (numLinesThisTurn == 0)
        {
            messageString += "";
        }
        else if (numLinesThisTurn == 1)
        {
            messageString += "1 line!";
        }
        else
        {
            messageString += numLinesThisTurn + " lines! Multi-line bonus activated";
        }

        UpdateScoreSheet();

    }

    public void MoveRowDown(int y)
    {
        for (int x = 0; x < gridRows; ++x)
        {
            if (grid[x, y] != null)
            {
                grid[x, y - 1] = grid[x, y];
                grid[x, y] = null;

                Vector3 pos = grid[x, y - 1].transform.position;
                Debug.Log("Current mino position in grid " + pos);

                grid[x, y - 1].transform.position = new Vector3(RoundToGridSpacing(pos.x), RoundToGridSpacing(pos.y - gridSize), RoundToGridSpacing(pos.z));
            }
        }
    }

    public void MoveAllRowsDown(int y)
    {
        for (int i = y; i < gridCols; ++i)
        {
            MoveRowDown(i);
        }
    }

    //Update grid based on movement
    public void UpdateGrid(ObjectControl tetromino)
    {
        for (int y = 0; y < gridCols; ++y)
        {
            for (int x = 0; x < gridRows; ++x)
            {
                if (grid[x, y] != null)
                {
                    if (grid[x, y].parent == tetromino.transform)
                    {
                        grid[x, y] = null;
                    }
                }
            }
        }

        foreach (Transform mino in tetromino.transform)
        {
            Vector3 pos = RoundVectorToGridSpacing(mino.transform.position);
            int posXIndex = (int)Mathf.Round(pos.x / gridSize);
            int posYIndex = (int)Mathf.Round(pos.y / gridSize);

            if ((posYIndex < gridCols) && (posXIndex >= 0) && (posXIndex < gridRows))
            {
                grid[posXIndex, posYIndex] = mino;
            }
        }
    }

    public Transform GetTransformAtGrid(Vector3 position)
    {
        Vector3 pos = RoundVectorToGridSpacing(position);
        int posXIndex = (int)Mathf.Round(pos.x / gridSize);
        int posYIndex = (int)Mathf.Round(pos.y / gridSize);
        //Debug.Log("GetTransformAtGrid X " + posXIndex);
        //Debug.Log("GetTransformAtGrid Y " + posYIndex);

        if ((posYIndex > gridCols - 1) || (posXIndex < 0) || (posXIndex > gridRows - 1))
        {
            return null;
        }
        else
        {
            return grid[posXIndex, posYIndex];
        }
    }

    //Preload fixed minos for MP game
    public void PreloadFixedMinos()
    {
        fixedMinos = new int[] { 13, 10, 16, 4, 10, 13, 16, 13, 3, 3, 9, 19, 13, 22, 19, 3, 10, 4, 16, 10, 10, 4, 9, 4, 10, 9, 10, 10, 4, 10, 3, 16, 19, 10, 9, 13 };
    }
}
