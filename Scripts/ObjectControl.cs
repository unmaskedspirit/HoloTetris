using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;


public class ObjectControl : MonoBehaviour, IInputHandler, IFocusable
{
    /// <summary>
    /// Event triggered when dragging starts.
    /// </summary>
    public event Action StartedDragging;

    /// <summary>
    /// Event triggered when dragging stops.
    /// </summary>
    public event Action StoppedDragging;

    //Colors for tetromino
    private Color[] childColors = new Color[4];

    //Booleans for state controls
    private bool isRotating = false;
    private bool isDragging = false;
    private bool isEnabled = false;

    //Variables used for debug
    private int clickCount = 0;

    //Used for moving
    public Transform HostTransform;
    private Rigidbody hostRigidbody;
    private Vector3 initialDraggingPosition;
    private Vector3 currentDraggingPosition;
    private Vector3 draggingPosition;
    private Vector3 handDraggingPosition;
    private IInputSource currentInputSource;
    private uint currentInputSourceId;
    private bool hostRigidbodyWasKinematic;

    private float zPosition;
    private float clickStartTime;


    [SerializeField]
    bool allowDragging = true;
    [SerializeField]
    bool xDraggingEnabled = true;
    [SerializeField]
    bool yDraggingEnabled = true;
    [SerializeField]
    bool yUpDraggingEnabled = true;
    [SerializeField]
    bool zDraggingEnabled = false;

    [Tooltip("Scale by which hand movement in z is multiplied to move the dragged object.")]
    public float DistanceScale = 50f;

    [Tooltip("Controls the speed at which the object will interpolate toward the desired position")]
    [Range(0.01f, 1.0f)]
    public float PositionLerpSpeed = 0.25f;

    
    [SerializeField]
    bool allowRotating = true;
    [SerializeField]
    bool allowRotate90 = true;

    [SerializeField]
    float clickSecondsThreshold = 0.30f;

    //used for dropping
    [SerializeField]
    bool allowFreeFall = true;

    [SerializeField]
    float fallSpeed = 1.0f;
    private float previousFallTime;

    //Other variables
    private float gridSize;

    private bool isMultiplayer;

    // Use this for initialization
    void Start()
    {
        EnableTetromino();
        //Get host transform and positon
        if (HostTransform == null)
        {
            HostTransform = transform;
        }
        hostRigidbody = HostTransform.GetComponent<Rigidbody>();

        //Get position
        initialDraggingPosition = GetHostPosition();
        zPosition = FindObjectOfType<GameControl>().GetGameZDistance();

        gridSize = FindObjectOfType<GameControl>().GetGridSize(); 
        
        GetChildColors();
        previousFallTime = 0.0f;
    }

    public void SetMPOption(bool isMP)
    {
        isMultiplayer = isMP;
    }

    //Set fall speed from outside
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
    }

    public void EnableTetromino()
    {
        isEnabled = true;
    }

    public void DisableTetromino()
    {
        isEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (isEnabled)
        {

            if (allowDragging && isDragging)
            {
                UpdateDragging();
            }

            //Drop object if not currently engaged
            else if (!isRotating && !isDragging && allowFreeFall && (Time.time - previousFallTime >= fallSpeed))
            {
                Vector3 newHostPosition = GetHostPosition();
                newHostPosition.y = RoundToGridSpacing(newHostPosition.y - gridSize);
                SetHostPosition(newHostPosition);
                //previousFallTime = Time.time;
                //Hit the floor so have to fix this and disable
                //Then spawn next tetromino

                if (!CheckIsValidPosition())
                {
                    while (!CheckIsValidPosition())
                    {
                        newHostPosition.y = RoundToGridSpacing(newHostPosition.y + gridSize);
                        SetHostPosition(newHostPosition);
                    }
                    DisableTetromino();
                    //Change color back to default
                    ChangeChildColors(false);
                    //Piece is disabled so we have to check if it is a full row or not
                    FindObjectOfType<GameControl>().DeleteRow();
                    
                    if (FindObjectOfType<GameControl>().CheckIsAboveGrid(this))
                    {
                        FindObjectOfType<GameControl>().GameOver();
                    }
                    else
                    {
                        Debug.Log("Spawn new");
                        if (isMultiplayer)
                        {
                            FindObjectOfType<GameControl>().SpawnNextMPTetromino();
                            FindObjectOfType<GameControl>().ReadGridFromFile();
                        }
                        else
                        {
                            FindObjectOfType<GameControl>().SpawnNextTetromino();
                        }
                        
                    }
                    
                }
                else
                {
                    previousFallTime = Time.time;

                    FindObjectOfType<GameControl>().UpdateGrid(this);
                }
            }
        }
    }

    //Change color when focused
    public void OnFocusEnter()
    {
        if (isEnabled)
        {
            ChangeChildColors(true);
        }
    }

    //Revert color when unfocused
    public void OnFocusExit()
    {
        if (isEnabled)
        {
            ChangeChildColors(false);
        }
    }

    //Get original colors
    public void GetChildColors()
    {
        int colorIndex = 0;
        foreach (Transform child in this.transform)
        {
            childColors[colorIndex] = child.transform.GetComponent<Renderer>().material.color;
            colorIndex++;
        }
    }

    //Change color based on state
    public void ChangeChildColors(bool changeToWhite)
    {
        int colorIndex = 0;

        foreach (Transform child in this.transform)
        {
            if (changeToWhite)
            {
                childColors[colorIndex] = child.transform.GetComponent<Renderer>().material.color;
                //Change metallic effect
                //child.transform.GetComponent<Renderer>().material.SetFloat("_Metallic", 0.0f);
                child.transform.GetComponent<Renderer>().material.color = new Color32(240, 220, 182, 255);
            }
            else
            {
                //child.transform.GetComponent<Renderer>().material.SetFloat("_Metallic", 1.0f);
                child.transform.GetComponent<Renderer>().material.color = childColors[colorIndex];

            }
            colorIndex++;
        }
    }

    //Get current position of the object
    private Vector3 GetHostPosition()
    {
        if (hostRigidbody == null)
        {
            return HostTransform.position;

        }
        else
        {
            return hostRigidbody.position;

        }
    }

    //Set position of object
    private void SetHostPosition(Vector3 position)
    {
        if (hostRigidbody == null)
        {
            HostTransform.position = position;

        }
        else
        {
            hostRigidbody.position = position;

        }

    }

    //Snap object to grid
    private void SnapBodyToGrid(bool forceToInitial)
    {
        if (forceToInitial)
        {
            SetHostPosition(initialDraggingPosition);
        }
        else
        {
            Vector3 snappedPosition = Vector3.zero;
            Vector3 bodyPosition = GetHostPosition();
            bool snapOK = true;

            snappedPosition.x = RoundToGridSpacing(bodyPosition.x);
            snappedPosition.y = RoundToGridSpacing(bodyPosition.y);
            snappedPosition.z = RoundToGridSpacing(bodyPosition.z);
            SetHostPosition(snappedPosition);

            //If final position is not within game grid, figure out where's the best place to keep it
            if (!CheckIsValidPosition())
            {
                //We can only move object closer to its starting position
                Debug.Log("out of grid");
                snapOK = false;

                float initialXPosition = RoundToGridSpacing(initialDraggingPosition.x);
                float initialYPosition = RoundToGridSpacing(initialDraggingPosition.y);

                if (snappedPosition.x <= initialXPosition)
                {
                    Debug.Log("Moving north west");
                    Debug.Log("Current Snapped X Position " + snappedPosition.x);
                    Debug.Log("Current Snapped Y Position " + snappedPosition.y);
                    Debug.Log("Initial X position " + initialXPosition);
                    Debug.Log("Initial Y Position " + initialYPosition);

                    int xDistance = (int)Mathf.Round((initialXPosition - snappedPosition.x) / gridSize);
                    int yDistance = (int)Mathf.Round((initialYPosition - snappedPosition.y) / gridSize);
                    //Debug.Log("X Distance " + xDistance);
                    //Debug.Log("Y Distance " + yDistance);
                    Vector3 tryPosition = Vector3.zero;

                    while (snapOK == false)
                    {
                        if (xDistance == 0)
                        {
                            for (float y = snappedPosition.y; y <= initialYPosition; y += gridSize)
                            {
                                tryPosition = new Vector3(snappedPosition.x, RoundToGridSpacing(y), snappedPosition.z);
                                SetHostPosition(tryPosition);
                                if (CheckIsValidPosition())
                                {
                                    Debug.Log("We good. North west return. X = 0");
                                    snapOK = true;
                                    break;
                                }
                            }
                        }
                        else if (yDistance == 0)
                        {
                            for (float x = snappedPosition.x; x <= initialXPosition; x += gridSize)
                            {
                                tryPosition = new Vector3(RoundToGridSpacing(x), snappedPosition.y, snappedPosition.z);
                                SetHostPosition(tryPosition);
                                if (CheckIsValidPosition())
                                {
                                    Debug.Log("We good. North west return. Y = 0");
                                    snapOK = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int firstCount = 0;
                            int secondCount = 0;
                            if (xDistance > yDistance)
                            {
                                //Move the x till it equals y distance
                                for (firstCount = 0; firstCount <= xDistance - yDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x + ((firstCount + secondCount) * gridSize)), RoundToGridSpacing(snappedPosition.y + (secondCount * gridSize)), snappedPosition.z);
                                    //tryPosition.x = RoundToGridSpacing(snappedPosition.x + (count*gridSize));
                                    //tryPosition.y = RoundToGridSpacing(tryPosition.y);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North west return. X > Y");
                                        snapOK = true;
                                        break;
                                    }
                                }
                                //x distance has equaled y distance but object still doesnt fit
                                if (snapOK == false)
                                {
                                    firstCount--;
                                    for (secondCount = 0; secondCount <= yDistance; secondCount++)
                                    {
                                        tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x + ((firstCount + secondCount) * gridSize)), RoundToGridSpacing(snappedPosition.y + (secondCount * gridSize)), snappedPosition.z);
                                        SetHostPosition(tryPosition);
                                        if (CheckIsValidPosition())
                                        {
                                            Debug.Log("We good. North west return. X > Y");
                                            snapOK = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (yDistance > xDistance)
                            {
                                //Move the x till it equals y distance
                                for (firstCount = 0; firstCount <= yDistance - xDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x + (secondCount * gridSize)), RoundToGridSpacing(snappedPosition.y + ((firstCount + secondCount) * gridSize)), snappedPosition.z);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North west return. Y > X");
                                        snapOK = true;
                                        break;
                                    }
                                }
                                //y distance has equaled x distance but object still doesnt fit
                                if (snapOK == false)
                                {
                                    firstCount--;
                                    for (secondCount = 0; secondCount <= xDistance; secondCount++)
                                    {
                                        tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x + (secondCount * gridSize)), RoundToGridSpacing(snappedPosition.y + ((firstCount + secondCount) * gridSize)), snappedPosition.z);
                                        SetHostPosition(tryPosition);
                                        if (CheckIsValidPosition())
                                        {
                                            Debug.Log("We good. North west return. Y > X");
                                            snapOK = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            //x distance equals y distance
                            else
                            {
                                //Move the x and y distance equally
                                for (firstCount = 0; firstCount <= yDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x + (firstCount * gridSize)), RoundToGridSpacing(snappedPosition.y + (firstCount * gridSize)), snappedPosition.z);
                                    //tryPosition.x = RoundToGridSpacing(snappedPosition.x + (count*gridSize));
                                    //tryPosition.y = RoundToGridSpacing(tryPosition.y);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North west return. X = Y");
                                        snapOK = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                else
                {
                    Debug.Log("Moving north east");
                    Debug.Log("Current Snapped X Position " + snappedPosition.x);
                    Debug.Log("Current Snapped Y Position " + snappedPosition.y);
                    Debug.Log("Initial X position " + initialXPosition);
                    Debug.Log("Initial Y Position " + initialYPosition);

                    int xDistance = (int)Mathf.Round((snappedPosition.x - initialXPosition) / gridSize);
                    int yDistance = (int)Mathf.Round((initialYPosition - snappedPosition.y) / gridSize);
                    Debug.Log("X Distance " + xDistance);
                    Debug.Log("Y Distance " + yDistance);
                    Vector3 tryPosition = Vector3.zero;

                    while (snapOK == false)
                    {
                        if (xDistance == 0)
                        {
                            for (float y = snappedPosition.y; y <= initialYPosition; y += gridSize)
                            {
                                tryPosition = new Vector3(snappedPosition.x, RoundToGridSpacing(y), snappedPosition.z);
                                SetHostPosition(tryPosition);
                                if (CheckIsValidPosition())
                                {
                                    Debug.Log("We good. North east return. X = 0");
                                    snapOK = true;
                                    break;
                                }
                            }
                        }
                        else if (yDistance == 0)
                        {
                            for (float x = snappedPosition.x; x >= initialXPosition; x -= gridSize)
                            {
                                tryPosition = new Vector3(RoundToGridSpacing(x), snappedPosition.y, snappedPosition.z);
                                SetHostPosition(tryPosition);
                                if (CheckIsValidPosition())
                                {
                                    Debug.Log("We good. North east return. Y = 0");
                                    snapOK = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int firstCount = 0;
                            int secondCount = 0;

                            if (xDistance > yDistance)
                            {
                                //Move the x till it equals y distance
                                for (firstCount = 0; firstCount <= xDistance - yDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x - ((firstCount + secondCount) * gridSize)), RoundToGridSpacing(snappedPosition.y + (secondCount * gridSize)), snappedPosition.z);
                                    //tryPosition.x = RoundToGridSpacing(snappedPosition.x + (count*gridSize));
                                    //tryPosition.y = RoundToGridSpacing(tryPosition.y);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North east return. X > Y");
                                        snapOK = true;
                                        break;
                                    }
                                }
                                //x distance has equaled y distance but object still doesnt fit
                                if (snapOK == false)
                                {
                                    firstCount--;
                                    for (secondCount = 0; secondCount <= yDistance; secondCount++)
                                    {
                                        tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x - ((firstCount + secondCount) * gridSize)), RoundToGridSpacing(snappedPosition.y + (secondCount * gridSize)), snappedPosition.z);
                                        SetHostPosition(tryPosition);
                                        if (CheckIsValidPosition())
                                        {
                                            Debug.Log("We good. North east return. X > Y");
                                            snapOK = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (yDistance > xDistance)
                            {
                                //Move the x till it equals y distance
                                for (firstCount = 0; firstCount <= yDistance - xDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x - (secondCount * gridSize)), RoundToGridSpacing(snappedPosition.y + ((firstCount + secondCount) * gridSize)), snappedPosition.z);
                                    //tryPosition.x = RoundToGridSpacing(snappedPosition.x + (count*gridSize));
                                    //tryPosition.y = RoundToGridSpacing(tryPosition.y);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North east return. Y > X");
                                        snapOK = true;
                                        break;
                                    }
                                }
                                //y distance has equaled x distance but object still doesnt fit
                                if (snapOK == false)
                                {
                                    firstCount--;
                                    for (secondCount = 0; secondCount <= xDistance; secondCount++)
                                    {
                                        tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x - (secondCount * gridSize)), RoundToGridSpacing(snappedPosition.y + ((firstCount + secondCount) * gridSize)), snappedPosition.z);
                                        SetHostPosition(tryPosition);
                                        if (CheckIsValidPosition())
                                        {
                                            Debug.Log("We good. North east return. Y > X");
                                            snapOK = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            //x distance equals y distance
                            else
                            {
                                //Move the x and y distance equally
                                for (firstCount = 0; firstCount <= yDistance; firstCount++)
                                {
                                    tryPosition = new Vector3(RoundToGridSpacing(snappedPosition.x - (firstCount * gridSize)), RoundToGridSpacing(snappedPosition.y + (firstCount * gridSize)), snappedPosition.z);
                                    //tryPosition.x = RoundToGridSpacing(snappedPosition.x + (count*gridSize));
                                    //tryPosition.y = RoundToGridSpacing(tryPosition.y);
                                    SetHostPosition(tryPosition);
                                    if (CheckIsValidPosition())
                                    {
                                        Debug.Log("We good. North east return. Y = X");
                                        snapOK = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public float RoundToGridSpacing(float value)
    {
        return FindObjectOfType<GameControl>().RoundToGridSpacing(value);
    }

    //Function to return a vector that is rounded to the nearest grid space
    public Vector3 RoundVectorToGridSpacing(Vector3 pos)
    {
        return FindObjectOfType<GameControl>().RoundVectorToGridSpacing(pos);
    }

    private Vector3 LimitDragPosition(Vector3 dragPosition)
    {
        Vector3 draggingPosition = dragPosition;

        //We will bound the motion to within the grid
        if (draggingPosition.x < 0.0f)
        {
            draggingPosition.x = RoundToGridSpacing(0.0f);
            Debug.Log("Way out in X");
        }
        else if (draggingPosition.x > FindObjectOfType<GameControl>().GetGridWidth())
        {
            draggingPosition.x = RoundToGridSpacing(FindObjectOfType<GameControl>().GetGridWidth());
            Debug.Log("Way out in X");
        }

        if (draggingPosition.y < 0.0f)
        {
            draggingPosition.y = RoundToGridSpacing(0.0f);
            Debug.Log("Way out in Y");
        }
        
        return draggingPosition;
    }

    //Apply position on object based on action
    private Vector3 DetermineDragPosition(Vector3 dragPosition)
    {
        if (!xDraggingEnabled)
        {
            dragPosition.x = GetHostPosition().x;
        }

        if (!yDraggingEnabled)
        {
            dragPosition.y = GetHostPosition().y;
        }
        else if (!yUpDraggingEnabled)
        {
            //Dont allow object to move up
            if (dragPosition.y > GetHostPosition().y)
            {
                dragPosition.y = GetHostPosition().y;
            }
        }

        if (!zDraggingEnabled)
        {
            dragPosition.z = zPosition;
        }

        //Apply intermediate position on object
        //Don't have to scale to grid while drag is in progress
        //Dont have to check if valid position while drag is in progress
        return dragPosition;
    }

    //Rotate the object
    private void RotateObject()
    {
        if (isEnabled && allowRotating)
        {
            isRotating = true;
            clickCount++;
            Debug.Log("Object clicked " + clickCount.ToString() + " number of times");

            if (allowRotate90)
            {
                this.transform.Rotate(0.0f, 0.0f, 90.0f);

                //Rotation put a mino out of the grid, so undo

                if (!CheckIsValidPosition())
                {
                    this.transform.Rotate(0.0f, 0.0f, -90.0f);
                }

            }
            else
            {
                if (this.transform.rotation.eulerAngles.z >= 90)
                {
                    this.transform.Rotate(0.0f, 0.0f, -90.0f);
                    //Rotation put a mino out of the grid, so undo

                    if (!CheckIsValidPosition())
                    {
                        this.transform.Rotate(0.0f, 0.0f, 90.0f);
                    }

                }
                else
                {
                    this.transform.Rotate(0.0f, 0.0f, +90.0f);

                    if (!CheckIsValidPosition())
                    {
                        this.transform.Rotate(0.0f, 0.0f, -90.0f);
                    }

                }
            }
        }

        SnapBodyToGrid(false);
        isRotating = false;
        FindObjectOfType<GameControl>().UpdateGrid(this);
    }

    //Function to check position of mino within game grid
    private bool CheckIsValidPosition()
    {
        int minoCount = 0;
        foreach (Transform mino in this.transform)
        {
            minoCount++;
            Vector3 pos = RoundVectorToGridSpacing(mino.transform.position);
            //Debug.Log("Position of child " + minoCount + " is: " + pos);

            if (FindObjectOfType<GameControl>().checkIsInGrid(pos))
            {
                //Debug.Log("This child is inside grid");
            }
            else
            {
                //Debug.Log(message: "Child " + minoCount + " is NOT inside grid with position " + pos);
                //Debug.Log("Position of child " + minoCount + " is: " + pos.ToString());
                return false;
            }

            if (FindObjectOfType<GameControl>().GetTransformAtGrid(pos) != null && FindObjectOfType<GameControl>().GetTransformAtGrid(pos).parent != this.transform)
            {
                //Debug.Log("Warning. Collision with another tetromino");
                return false;
            }
            
        }
        return true;
    }

    //Function call for OnInputDown
    public void OnInputDown(InputEventData eventData)
    {
        //Debug.Log("On Input Down");
        if (isDragging)
        {
            // We're already handling drag input, so we can't start a new drag operation.
            return;
        }

        if (!isEnabled)
        {
            return;
        }

        //Get start time of input down
        clickStartTime = Time.time;

        InteractionSourceInfo sourceKind;
        eventData.InputSource.TryGetSourceKind(eventData.SourceId, out sourceKind);
        if (sourceKind != InteractionSourceInfo.Hand)
        {
            if (!eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.GripPosition))
            {
                // The input source must provide grip positional data for this script to be usable
                return;
            }
        }

        eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

        currentInputSource = eventData.InputSource;
        currentInputSourceId = eventData.SourceId;

        //Get start position
        initialDraggingPosition = GetHostPosition();
        StartDragging(initialDraggingPosition);

    }

    //Function call when input is released
    public void OnInputUp(InputEventData eventData)
    {
        //Debug.Log("On Input Up");

        if (!isEnabled)
        {
            return;
        }

        if (currentInputSource != null &&
                eventData.SourceId == currentInputSourceId)
        {
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

            //TRY
            //StopDragging();
        }

        //Get click duration and rotate object if click was short enough or object didnt move
        float distanceMoved = Vector3.Distance(GetHostPosition(), initialDraggingPosition);

        if (distanceMoved < gridSize)
        {
            float clickDuration = Time.time - clickStartTime;
            int minutes = ((int)clickDuration / 60);
            float seconds = (float)(clickDuration % 60);
            //Debug.Log("Time between click is " + minutes.ToString() + " minutes and " + seconds.ToString()+" seconds");
            if ((float)minutes == 0 && seconds < clickSecondsThreshold)
            {
                StopDragging(true);
                RotateObject();

            }
        }
        else
        {
            StopDragging(false);
        }
        
    }

    //On stop drag, release input and snap to grid
    public void StopDragging(bool forceToInitial)
    {
        if (!isDragging)
        {
            return;
        }

        // Remove self as a modal input handler
        InputManager.Instance.PopModalInputHandler();

        isDragging = false;
        currentInputSource = null;
        currentInputSourceId = 0;
        if (hostRigidbody != null)
        {
            hostRigidbody.isKinematic = hostRigidbodyWasKinematic;
        }
        StoppedDragging.RaiseEvent();

        //Now we snap the object to a fixed grid
        SnapBodyToGrid(forceToInitial);
        FindObjectOfType<GameControl>().UpdateGrid(this);
    }

    //Called when dragging starts
    //Save initial hand position
    public void StartDragging(Vector3 initialDraggingPosition)
    {
        if (!isEnabled || !allowDragging || isDragging)
        {
            return;
        }
        // Add self as a modal input handler, to get all inputs during the manipulation
        InputManager.Instance.PushModalInputHandler(gameObject);

        isDragging = true;
        if (hostRigidbody != null)
        {
            hostRigidbodyWasKinematic = hostRigidbody.isKinematic;
            hostRigidbody.isKinematic = true;
        }
        Vector3 inputPosition = Vector3.zero;
#if UNITY_2017_2_OR_NEWER
        InteractionSourceInfo sourceKind;
        currentInputSource.TryGetSourceKind(currentInputSourceId, out sourceKind);
        switch (sourceKind)
        {
            case InteractionSourceInfo.Hand:
                currentInputSource.TryGetGripPosition(currentInputSourceId, out inputPosition);
                break;
            case InteractionSourceInfo.Controller:
                currentInputSource.TryGetPointerPosition(currentInputSourceId, out inputPosition);
                break;
        }
#else
            currentInputSource.TryGetPointerPosition(currentInputSourceId, out inputPosition);
#endif
        Debug.Log("Hand reference position " + inputPosition.ToString());
        
        draggingPosition = initialDraggingPosition;
        handDraggingPosition = inputPosition;
        
        StartedDragging.RaiseEvent();
    }

    //Update dragging
    private void UpdateDragging()
    {
        
        Vector3 inputPosition = Vector3.zero;
#if UNITY_2017_2_OR_NEWER
        InteractionSourceInfo sourceKind;
        currentInputSource.TryGetSourceKind(currentInputSourceId, out sourceKind);
        switch (sourceKind)
        {
            case InteractionSourceInfo.Hand:
                currentInputSource.TryGetGripPosition(currentInputSourceId, out inputPosition);
                break;
            case InteractionSourceInfo.Controller:
                currentInputSource.TryGetPointerPosition(currentInputSourceId, out inputPosition);
                break;
        }
#else
            currentInputSource.TryGetPointerPosition(currentInputSourceId, out inputPosition);
#endif

        
        Vector3 newHandDirection = inputPosition;
        Vector3 handDistance = (newHandDirection - handDraggingPosition);
        handDraggingPosition = newHandDirection; //Assign for next pass

        //Make sure object doesn't move out of grid
        Vector3 draggedPosition = LimitDragPosition(HostTransform.position + (handDistance * DistanceScale));
        
        Vector3 newPosition = Vector3.Slerp(HostTransform.position, draggedPosition, PositionLerpSpeed);

        // Determine and set final position
        SetHostPosition(DetermineDragPosition(newPosition));

    }

    public void OnSourceDetected(SourceStateEventData eventData)
    {
        // Nothing to do
    }

    public void OnSourceLost(SourceStateEventData eventData)
    {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
        {
            StopDragging(true);
        }
    }

}
