using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference moveActionRef;

    public bool IsActive { get; private set; } //set by GameManager


    private Rigidbody rb;
    private Vector2 inputVector = Vector2.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetActive(bool state)
    {
        IsActive = state;

        if (!state)
        {
            inputVector = Vector2.zero; // Stop movement when inactive
        }
    }

    private void Update()
    {
        //If inactive, don't read input
        if (!IsActive) return;

        //Read input from the InputActionReference
        inputVector = moveActionRef.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        //If inactive, don't move
        if (!IsActive)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        // Convert the 2D input vector to a 3D movement direction
        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        // Standardize the movement direction to prevent faster diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Apply movement to the Rigidbody
        rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
    }

}
