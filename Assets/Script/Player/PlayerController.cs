using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private Player playerInput;
    private Vector3 movedirection;
    private Vector3 movevelocity;

    [Header("Aim")]
    [SerializeField] private LayerMask aimLayer;
    private Vector3 lookingDirection;
    public Transform aim;

    private float verticalVelocity;
    private float GRAVITY = 9.8f;
    private CharacterController characterController;

    private Vector2 moveInput;
    private Vector2 aimImput;


    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void Awake()
    {
        playerInput = new Player();

        playerInput.PlayerMove.Move.performed += context => moveInput = context.ReadValue<Vector2>();
        playerInput.PlayerMove.Move.canceled += context => moveInput = Vector2.zero;


        playerInput.PlayerMove.Aim.performed += context => aimImput = context.ReadValue<Vector2>();
        playerInput.PlayerMove.Aim.canceled += context => aimImput = Vector2.zero;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
        ApplyMovement();

        ApplyAim();
    }

    private void ApplyAim()
    {
        Ray ray = Camera.main.ScreenPointToRay(aimImput);

        if(Physics.Raycast(ray,out var hitInfo,Mathf.Infinity,aimLayer))
        {
            lookingDirection = hitInfo.point - transform.position;
            lookingDirection.y = 0f;
            lookingDirection.Normalize();
            transform.forward = lookingDirection;

            aim.position = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity -= GRAVITY * Time.deltaTime;
        }
    }
    
    private void ApplyMovement()
    {
        movedirection = new Vector3(moveInput.x, 0f, moveInput.y);
        movevelocity = movedirection * walkSpeed;
        movevelocity.y = verticalVelocity;

        characterController.Move(movevelocity * Time.deltaTime);
    }

    private void OnDisable()
    {
        playerInput.Disable();
        moveInput = Vector2.zero;
        aimImput = Vector2.zero;
    }


}
