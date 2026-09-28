using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float edgeBuffer = 10f;
    public Vector2 minBounds = new Vector2(-500, -500);
    public Vector2 maxBounds = new Vector2(500, 500);

    [Header("References")]
    [SerializeField] Camera camera_UI;
    [SerializeField] Transform listenerTransform;

    private Camera cam;
    private bool isMouseInGameWindow = true;

    // Zooming
    public float zoomSpeed = 10f;
    public float minZoom = 25f;
    public float maxZoom = 50f;
    public float zoomSmoothing = 5f;
    private float targetZoom;
    public float minListenerDistance = 25f;
    public float maxListenerDistance = 50f;
    public float listenerYOffset = 50;

    // Focusing
    public Transform target;
    private bool isFocusing;

    //Singleton 
    private static CameraController _instance;

    public static CameraController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<CameraController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<CameraController>().Length > 1)
                {
                    Debug.LogWarning("More than one CameraController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("CameraController");
                    _instance = singletonObject.AddComponent<CameraController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of CameraController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // If an instance already exists, destroy this GameObject
            Destroy(gameObject);
        }
    }

    void Start()
    {
        cam = Camera.main;
        cam.transform.position = new Vector3(cam.transform.position.x, 100f, cam.transform.position.z);
        cam.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        targetZoom = cam.orthographicSize;
    }

    void Update()
    {
        if (!isFocusing)
        {
            HandleMovement();
            HandleZoom();  // Re-added zoom functionality
            UpdateAudioListenerZoom();
        }
        else
        {
            FocusCameraOnTarget();
        }
    }

    private void HandleMovement()
    {
        isMouseInGameWindow = (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width) &&
                              (Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height);

        if (!isMouseInGameWindow) return; // Prevent movement when cursor is outside game window

        Vector3 camPosition = cam.transform.position;
        bool isUsingWASD = false;

        // WASD Movement
        if (Input.GetKey(KeyCode.W)) { camPosition += new Vector3(-1, 0, 1) * moveSpeed * Time.unscaledDeltaTime; isUsingWASD = true; }
        if (Input.GetKey(KeyCode.S)) { camPosition += new Vector3(1, 0, -1) * moveSpeed * Time.unscaledDeltaTime; isUsingWASD = true; }
        if (Input.GetKey(KeyCode.A)) { camPosition += new Vector3(-1, 0, -1) * moveSpeed * Time.unscaledDeltaTime; isUsingWASD = true; }
        if (Input.GetKey(KeyCode.D)) { camPosition += new Vector3(1, 0, 1) * moveSpeed * Time.unscaledDeltaTime; isUsingWASD = true; }

        // Edge Scrolling
        if (!isUsingWASD)
        {
            if (Input.mousePosition.x <= edgeBuffer) camPosition += new Vector3(-1, 0, -1) * moveSpeed * Time.unscaledDeltaTime;
            if (Input.mousePosition.x >= Screen.width - edgeBuffer) camPosition += new Vector3(1, 0, 1) * moveSpeed * Time.unscaledDeltaTime;
            if (Input.mousePosition.y <= edgeBuffer) camPosition += new Vector3(1, 0, -1) * moveSpeed * Time.unscaledDeltaTime;
            if (Input.mousePosition.y >= Screen.height - edgeBuffer) camPosition += new Vector3(-1, 0, 1) * moveSpeed * Time.unscaledDeltaTime;
        }

        // Lock Y position to 100
        camPosition.y = 100f;

        // Apply movement bounds
        camPosition.x = Mathf.Clamp(camPosition.x, minBounds.x, maxBounds.x);
        camPosition.z = Mathf.Clamp(camPosition.z, minBounds.y, maxBounds.y);

        // Apply position
        cam.transform.position = camPosition;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomSpeed, minZoom, maxZoom);
        }
        float size = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.unscaledDeltaTime * zoomSmoothing);
        cam.orthographicSize = size;
        camera_UI.orthographicSize = size;
    }

    private void UpdateAudioListenerZoom()
    {
        // Normalize zoom (1 = zoomed in, 0 = zoomed out)
        float t = 1 - Mathf.InverseLerp(minZoom, maxZoom, cam.orthographicSize);

        // Get distance based on zoom
        float distance = Mathf.Lerp(minListenerDistance, maxListenerDistance, t);

        Vector3 cameraOffsetPos = cam.transform.position + cam.transform.up * listenerYOffset;

        // Project from the offset position along camera's forward
        Vector3 listenerPos = cameraOffsetPos + cam.transform.forward * distance;

        listenerTransform.position = listenerPos;
    }

    private void FocusCameraOnTarget()
    {
        if (target == null)
        {
            isFocusing = false;
            return;
        }

        // Calculate new camera position based on isometric angle (-45 degrees rotation)
        Vector3 targetPosition = target.position;

        // Move camera diagonally to focus on the target
        Vector3 cameraOffset = new Vector3(targetPosition.x + 60, 100f, targetPosition.z - 70);
        cam.transform.position = Vector3.Lerp(cam.transform.position, cameraOffset, (moveSpeed * Time.unscaledDeltaTime) * 0.1f);

        // Check if close enough to stop focusing
        if (Vector3.Distance(cam.transform.position, cameraOffset) < 0.5f)
        {
            isFocusing = false;
            target = null;
        }
    }

    public void FocusOnTarget(Transform newTarget)
    {
        isFocusing = true;
        target = newTarget;
    }
}
