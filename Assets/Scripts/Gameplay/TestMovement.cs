using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class TestMovement : MonoBehaviour
{
    [SerializeField] private MovementVariables movementVariables;

    private float speed;

    public string comPort = "COM10";
    private int baudRate = 9600;

    private bool player1Pressed;
    private bool player2Pressed;
    private Rigidbody rb;
    private float startYAngle;
    private float currentYAngle;

    public event Action player1Press;
    public event Action player2Press;
    public event Action<float> animationSpeed;

    [HideInInspector] public bool player1Active;
    [HideInInspector] public bool player2Active;

    // CHANGED: Removed the 'saveport' ScriptableObject reference completely.
    // The live SerialPort now lives directly inside this component at runtime.
    private SerialPort activeSerialPort;

    private Thread serialThread;
    private bool isRunning;
    private float latestPot1;
    private float latestPot2;
    private string serialBuffer = "";

    private Key player1Key = Key.W;
    private Key player2Key = Key.UpArrow;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;

        startYAngle = transform.eulerAngles.y;
        currentYAngle = startYAngle;
    }

    private void Start()
    {
        StartSerial();
        StartCoroutine(ReadSerialCoroutine());
    }

    private const string Expected_ID = "Controller";

    private bool FindArduinoPort(out string foundPort)
    {
        foundPort = null;

        // CHANGED: Instead of PlayerPrefs, grab the cached port from our new central SaveSystem JSON
        string cachedPort = "";
        if (SaveSystem.Instance != null)
        {
            cachedPort = SaveSystem.Instance.data.savedPortName;
        }

        if (!string.IsNullOrEmpty(cachedPort) && TryProbePort(cachedPort))
        {
            foundPort = cachedPort;
            return true;
        }

        string[] portNames = SerialPort.GetPortNames();
        Array.Sort(portNames);
        Array.Reverse(portNames);

        foreach (string port in portNames)
        {
            if (port == cachedPort) continue;
            if (TryProbePort(port))
            {
                foundPort = port;

                // CHANGED: Instead of PlayerPrefs, save the newly discovered working port to our JSON file
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.data.savedPortName = port;
                    SaveSystem.Instance.SaveGame();
                }
                return true;
            }
        }

        return false;
    }

    private bool TryProbePort(string port)
    {
        SerialPort testPort = null;
        try
        {
            testPort = new SerialPort(port, baudRate);
            testPort.DtrEnable = true;
            testPort.ReadTimeout = 400;
            testPort.WriteTimeout = 400;
            testPort.NewLine = "\n";
            testPort.Open();

            testPort.DiscardInBuffer();
            testPort.WriteLine("ID?");

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (testPort.ReadLine().Trim() == Expected_ID)
                    {
                        testPort.Close();
                        return true;
                    }
                }
                catch (TimeoutException) { break; }
            }

            testPort.Close();
        }
        catch (Exception e)
        {
            Debug.Log($"Port {port} not the controller: {e.Message}");
            if (testPort != null && testPort.IsOpen) testPort.Close();
        }
        return false;
    }

    private void StartSerial()
    {
        // 1. FIRST CHECK: See if our SaveSystem already has a valid, known working port
        if (SaveSystem.Instance != null && !string.IsNullOrEmpty(SaveSystem.Instance.data.savedPortName))
        {
            comPort = SaveSystem.Instance.data.savedPortName;
            Debug.Log($"Fast-loading serial port from JSON cache: {comPort}");
        }
        else
        {
            // 2. FALLBACK: Only do the heavy, slow port scanning if the JSON file is totally empty
            Debug.Log("No cached port found in JSON. Starting full controller search...");
            if (!FindArduinoPort(out string detectedPort))
            {
                Debug.LogWarning("Could not find the Arduino controller on any COM port.");
                return;
            }
            comPort = detectedPort;
        }

        // 3. Open the port instantly using our confirmed string
        try
        {
            activeSerialPort = new SerialPort(comPort, baudRate);
            activeSerialPort.DtrEnable = true;
            activeSerialPort.ReadTimeout = 10;
            activeSerialPort.NewLine = "\n";
            activeSerialPort.Open();

            activeSerialPort.DiscardInBuffer();

            Debug.Log("Serial port opened cleanly on " + comPort);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not open serial port: " + e.Message);

            // Clear the bad port data so the game will try searching again on next boot
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.data.savedPortName = "";
                SaveSystem.Instance.SaveGame();
            }
        }
    }

    private IEnumerator ReadSerialCoroutine()
    {
        while (true)
        {
            // CHANGED: Reading from the script's local activeSerialPort instance
            if (activeSerialPort != null && activeSerialPort.IsOpen)
            {
                try
                {
                    string data = activeSerialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        serialBuffer += data;
                        string[] lines = serialBuffer.Split('\n');

                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            ParseLine(lines[i]);
                        }

                        serialBuffer = lines[lines.Length - 1];
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Serial error: " + e.Message);
                }
            }

            yield return null;
        }
    }

    private void ParseLine(string line)
    {
        string[] parts = line.Trim().Replace("\r", "").Split(',');

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int raw1) &&
            int.TryParse(parts[1], out int raw2))
        {
            latestPot1 = raw2 / 1023f;
            latestPot2 = raw1 / 1023f;
        }
    }

    private void ProcessInput(float val1, float val2)
    {
        if (!player1Active && val1 > movementVariables.pressThreshold)
        {
            player1Pressed = true;
            player1Active = true;
        }
        else if (player1Active && val1 < movementVariables.releaseThreshold)
        {
            player1Active = false;
        }

        if (!player2Active && val2 > movementVariables.pressThreshold)
        {
            player2Pressed = true;
            player2Active = true;
        }
        else if (player2Active && val2 < movementVariables.releaseThreshold)
        {
            player2Active = false;
        }
    }

    private void Update()
    {
        // CHANGED: Updated target check to use the build-safe instance
        if (activeSerialPort != null && activeSerialPort.IsOpen)
            ProcessInput(latestPot1, latestPot2);
        else
        {
            var keyBoard = Keyboard.current;
            float key1 = (keyBoard != null && keyBoard[player1Key].isPressed) ? 1f : 0f;
            float key2 = (keyBoard != null && keyBoard[player2Key].isPressed) ? 1f : 0f;
            ProcessInput(key1, key2);
        }
    }

    private void FixedUpdate()
    {
        if (player1Pressed)
        {
            ApplyForce(-1);
            player1Press?.Invoke();
            player1Pressed = false;
        }

        if (player2Pressed)
        {
            ApplyForce(1);
            player2Press?.Invoke();
            player2Pressed = false;
        }

        ApplyDrag();
        ClampSpeed();
    }

    private void ApplyForce(int direction)
    {
        float scaleDegrees = CalculateRotation(direction);

        currentYAngle += scaleDegrees;
        currentYAngle = Mathf.Clamp(currentYAngle, startYAngle - movementVariables.maxDegree, startYAngle + movementVariables.maxDegree);

        Vector3 euler = transform.eulerAngles;
        euler.y = currentYAngle;
        transform.eulerAngles = euler;

        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed > 0f)
            rb.linearVelocity = transform.forward * currentSpeed;

        float forceMultiplier = 1f - Mathf.Clamp01(speed / movementVariables.maxSpeed);
        rb.AddForce(transform.forward * movementVariables.force * forceMultiplier, ForceMode.Impulse);

        speed = rb.linearVelocity.magnitude;
        animationSpeed?.Invoke(speed);
    }

    private float CalculateRotation(int direction)
    {
        float offset = currentYAngle - startYAngle;
        float distanceInDirection = direction * offset;

        float threshold = movementVariables.thresholdPercentage * movementVariables.maxDegree;
        float multiplier;

        if (distanceInDirection <= threshold)
            multiplier = 1;
        else
        {
            float thresholdCal = (distanceInDirection - threshold) / (movementVariables.maxDegree - threshold);
            thresholdCal = Mathf.Clamp01(thresholdCal);

            float maxVal = 1f;
            float minVal = Mathf.Exp(-movementVariables.exponentialStrength);
            float raw = Mathf.Exp(-movementVariables.exponentialStrength * thresholdCal);
            multiplier = (raw - minVal) / (maxVal - minVal);
            multiplier = Mathf.Clamp01(multiplier);
        }

        return direction * movementVariables.degreesPerClick * multiplier;
    }

    private void ApplyDrag()
    {
        speed = rb.linearVelocity.magnitude;
        if (speed <= 0f) return;

        float speedDrop = movementVariables.deceleraionRate * Time.fixedDeltaTime;
        float newSpeed = Mathf.Max(0f, speed - speedDrop);
        rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;

        speed = newSpeed;
        animationSpeed?.Invoke(speed);
    }

    private void ClampSpeed()
    {
        if (rb.linearVelocity.magnitude > movementVariables.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * movementVariables.maxSpeed;
            speed = rb.linearVelocity.magnitude;
            animationSpeed?.Invoke(speed);
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        serialThread?.Join(500);

        // CHANGED: Properly clean up and release our local connection stream when exiting the scene/game
        if (activeSerialPort != null && activeSerialPort.IsOpen)
            activeSerialPort.Close();
    }
}