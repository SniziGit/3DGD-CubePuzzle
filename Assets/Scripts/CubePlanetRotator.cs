using UnityEngine;

public class CubePlanetRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float smoothFactor = 10f;
    [SerializeField] private float resetSmoothFactor = 8f; // Speed of reset rotation
    [SerializeField] private bool invertVertical = false;
    [SerializeField] private bool invertHorizontal = false;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = -10f;
    [SerializeField] private float maxZoom = 10f;

    private Vector3 currentRotation;
    private Vector3 rotationVelocity;
    private bool isRotating = false;
    private bool isResetting = false;
    private Quaternion initialRotation;
    private Vector2 lastMousePosition;
    private Camera mainCamera;
    private Vector3 initialPosition;

    private void Start()
    {
        mainCamera = Camera.main;
        currentRotation = transform.rotation.eulerAngles;
        initialRotation = transform.rotation;
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Check for reset key
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartReset();
        }

        // Handle zoom input
        HandleZoom();

        if (isResetting)
        {
            HandleReset();
        }
        else
        {
            HandleRotationInput();
            ApplyRotation();
        }
    }

    private void HandleRotationInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (isRotating)
        {
            // Get mouse movement delta from Input instead of calculating it ourselves
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Apply inversion based on settings
            float xRotation = mouseY * (invertVertical ? 1 : -1);
            float yRotation = mouseX * (invertHorizontal ? -1 : 1);

            // Calculate rotation delta
            Vector3 rotationDelta = new Vector3(xRotation, yRotation, 0);

            // Apply rotation with delta time for consistent speed
            currentRotation += rotationDelta * Time.deltaTime * 60f; // 60 to maintain similar speed to before

            // Keep angles between 0-360 for consistency
            currentRotation.x = currentRotation.x % 360f;
            currentRotation.y = currentRotation.y % 360f;
        }
    }

    private void ApplyRotation()
    {
        // Apply rotation with smoothing
        Quaternion targetRotation = Quaternion.Euler(currentRotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothFactor * Time.deltaTime);
    }

    private void StartReset()
    {
        isResetting = true;
    }

    private void HandleReset()
    {
        // Smoothly interpolate rotation back to initial rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, resetSmoothFactor * Time.deltaTime);
        
        // Smoothly interpolate position back to initial position
        transform.position = Vector3.Lerp(transform.position, initialPosition, resetSmoothFactor * Time.deltaTime);

        // Check if we're close enough to the target rotation to stop resetting
        if (Quaternion.Angle(transform.rotation, initialRotation) < 0.1f)
        {
            isResetting = false;
            transform.rotation = initialRotation; // Snap to exact rotation
            transform.position = initialPosition; // Snap to exact position
            currentRotation = initialRotation.eulerAngles;
        }
    }
    
    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            Vector3 currentPosition = transform.position;
            float zoomAmount = scrollInput * zoomSpeed;
            
            // Apply zoom to Z axis
            currentPosition.z += zoomAmount;
            
            // Clamp the zoom to min/max values
            currentPosition.z = Mathf.Clamp(currentPosition.z, minZoom, maxZoom);
            
            transform.position = currentPosition;
        }
    }
}
