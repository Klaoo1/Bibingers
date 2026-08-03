using Unity.Netcode;
using UnityEngine;

public class LampController : NetworkBehaviour {
    public float speed = 10f;
    public float rotationSpeed = 720f;
    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.freezeRotation = true; 
    }

    void FixedUpdate() { 
        if (!IsOwner || !IsSpawned) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 moveDirection = new Vector3(x, 0, z).normalized;

        if (moveDirection.magnitude >= 0.1f) {
            // 1. ROTATION (This part is working!)
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 2. MOVEMENT (The Force Method)
            // This calculates where the lamp should be and FORCES it to go there
            Vector3 targetPosition = rb.position + moveDirection * speed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);

            Debug.Log("Forcing movement to: " + targetPosition);
        }
    }
}