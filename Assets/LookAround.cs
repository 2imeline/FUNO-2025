using UnityEngine;

public class LookAround : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Settings")]
    [Range(0f, 100f)]
    [SerializeField] private float sensitivity = 50f; // 0 = none, 50 = normal, 100 = fast
    [SerializeField] private float verticalClamp = 80f;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Normalize sensitivity into a multiplier: 50 = 1.0, 100 = 2.0, 0 = 0.0
        float scaledSensitivity = sensitivity / 50f;

        float mouseX = Input.GetAxis("Mouse X") * scaledSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * scaledSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
