using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float rotationSpeed = 12.0f;
    [SerializeField] private float gravity = 20.0f;
    [SerializeField] private bool moveRelativeToCamera = true;
    [SerializeField] private bool faceMoveDirection = true;

    [Header("Animation")]
    [SerializeField] private float animationDampTime = 0.1f;

    private CharacterController controller;
    private Animator animator;
    private Camera mainCamera;
    private float verticalVelocity;

    // Animator.StringToHash 可以避免每帧用字符串查找参数，适合 Update 里反复设置动画参数。
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (animator != null)
        {
            // 位移由 PlayerController 代码控制，不使用动画自带的 Root Motion 推动角色。
            animator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 moveDirection = GetWorldMoveDirection(moveInput);
        bool isRunning = IsRunInputHeld() && moveDirection != Vector3.zero;

        RotateToMoveDirection(moveDirection);
        MovePlayer(moveDirection, isRunning);
        UpdateAnimation(moveInput, moveDirection, isRunning);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // GetAxisRaw 按键时会直接得到 -1/0/1，手感更干脆，适合动作 RPG。
        // 同时按两个方向键时，向量长度会大于 1；归一化后斜向移动不会比直线移动更快。
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        return input;
    }

    private bool IsRunInputHeld()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    private void MovePlayer(Vector3 moveDirection, bool isRunning)
    {
        ApplyGravity();

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetWorldMoveDirection(Vector2 moveInput)
    {
        if (moveInput == Vector2.zero)
        {
            return Vector3.zero;
        }

        if (!moveRelativeToCamera || mainCamera == null)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y);
        }

        // 俯角 RPG 的镜头通常是斜着看的。
        // 这里把键盘输入的“屏幕上下左右”，转换成地面上的真实移动方向。
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
    }

    private void RotateToMoveDirection(Vector3 moveDirection)
    {
        if (!faceMoveDirection || moveDirection == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        // 只改角色 Y 轴朝向，不影响上下倾斜；这样角色会转身跑，而不是面朝前侧移。
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            // 给一个很小的向下速度，让 CharacterController 稳定贴在地面上。
            verticalVelocity = -1f;
            return;
        }

        verticalVelocity -= gravity * Time.deltaTime;
    }

    private void UpdateAnimation(Vector2 moveInput, Vector3 moveDirection, bool isRunning)
    {
        if (animator == null)
        {
            return;
        }

        Vector2 animationDirection = GetAnimationInput(moveInput, moveDirection, isRunning);

        // 第三个参数是阻尼时间，让动画参数平滑变化，避免方向切换时太生硬。
        animator.SetFloat(HorizontalHash, animationDirection.x, animationDampTime, Time.deltaTime);
        animator.SetFloat(VerticalHash, animationDirection.y, animationDampTime, Time.deltaTime);
    }

    private Vector2 GetAnimationInput(Vector2 moveInput, Vector3 moveDirection, bool isRunning)
    {
        if (moveDirection == Vector3.zero)
        {
            return Vector2.zero;
        }

        if (faceMoveDirection)
        {
            // 角色已经转向移动方向了，所以动画只需要播放“向前跑/走”。
            // 如果这里继续传 Horizontal=-1/1，Animator 就会播放侧移动画，看起来像 A/D 平移。
            // 当前混合树是 2D Freeform Directional：
            // Idle 放在 (0,0)，Walk Forward 建议放在 (0,0.5)，Run Forward 建议放在 (0,1)。
            return new Vector2(0f, isRunning ? 1f : 0.5f);
        }

        Vector2 fourDirectionInput = GetFourDirectionAnimationInput(moveInput);
        float animationSpeed = isRunning ? 1f : 0.5f;
        return fourDirectionInput * animationSpeed;
    }

    private static Vector2 GetFourDirectionAnimationInput(Vector2 moveInput)
    {
        if (moveInput == Vector2.zero)
        {
            return Vector2.zero;
        }

        // 当前只有四方向动画，斜向输入时取更明显的那个轴来决定播放横向还是纵向动画。
        // 例如同时按 W+D 时，如果 D 更明显就播右移动画，否则播前移动画。
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            return new Vector2(Mathf.Sign(moveInput.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(moveInput.y));
    }
}
