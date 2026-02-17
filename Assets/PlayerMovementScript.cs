using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public Camera playerCamera;

    [Header("Configurations")]
    public float walkSpeed;
    public float runSpeed;

    

    void Start()
    {
        // You can initialize rb here if not assigned in Inspector
     
    }

    void Update()
    {
        // For input detection if needed
    }

    void FixedUpdate()
    {
        Vector3 newVelocity = Vector3.up * rb.linearVelocity.y;

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

        rb.linearVelocity = newVelocity;
    }
}

