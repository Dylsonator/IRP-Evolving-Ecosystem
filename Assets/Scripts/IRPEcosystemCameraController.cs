using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple free camera for observing the ecosystem and selecting fish.
/// Controls:
/// RMB + mouse = look, WASD = move, Q/E = down/up, Shift = fast, Ctrl = slow,
/// left click = select fish, F = focus selected fish, Esc = unlock cursor.
/// </summary>
public class IRPEcosystemCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 18f;
    public float FastMultiplier = 3.2f;
    public float SlowMultiplier = 0.35f;
    public float LookSensitivity = 0.12f;
    public bool RequireRightMouseForLook = true;
    public bool LockCursorWhileLooking = true;

    [Header("Selection")]
    public FishSelectionDebugPanel SelectionPanel;
    public Camera ControlledCamera;
    public LayerMask SelectionMask = ~0;
    public float RaycastDistance = 600f;
    public float SphereCastRadius = 0.85f;
    public float FocusDistance = 12f;
    public float FocusHeight = 4f;
    public float FocusLerpSpeed = 8f;
    [Tooltip("When enabled, pressing F toggles a soft follow camera on the selected fish instead of only jumping once.")]
    public bool ToggleFollowWithF = true;
    public float FollowDistance = 14f;
    public float FollowHeight = 5f;
    public float FollowLerpSpeed = 5.5f;

    private float yaw;
    private float pitch;
    private MarineCreatureAgent selectedFish;
    private bool hasFocusTarget;
    private bool isFollowingSelected;
    private Vector3 focusTargetPosition;

    private void Awake()
    {
        if (ControlledCamera == null)
        {
            ControlledCamera = GetComponent<Camera>();
        }

        if (ControlledCamera == null)
        {
            ControlledCamera = Camera.main;
        }

        if (SelectionPanel == null)
        {
            SelectionPanel = FindFirstObjectByType<FishSelectionDebugPanel>();
        }

        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalisePitch(euler.x);
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleSelection();
        HandleFocus();
        HandleCursorRelease();
    }

    private void HandleLook()
    {
        bool shouldLook = !RequireRightMouseForLook || IsRightMouseHeld();
        if (!shouldLook)
        {
            if (LockCursorWhileLooking && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        if (LockCursorWhileLooking)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector2 delta = GetMouseDelta();
        yaw += delta.x * LookSensitivity;
        pitch -= delta.y * LookSensitivity;
        pitch = Mathf.Clamp(pitch, -88f, 88f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;
        if (GetKey(KeyCode.W)) input += Vector3.forward;
        if (GetKey(KeyCode.S)) input += Vector3.back;
        if (GetKey(KeyCode.D)) input += Vector3.right;
        if (GetKey(KeyCode.A)) input += Vector3.left;
        if (GetKey(KeyCode.E)) input += Vector3.up;
        if (GetKey(KeyCode.Q)) input += Vector3.down;

        if (input.sqrMagnitude <= 0.001f)
        {
            return;
        }

        hasFocusTarget = false;
        isFollowingSelected = false;
        float speed = MoveSpeed;
        if (GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift)) speed *= FastMultiplier;
        if (GetKey(KeyCode.LeftControl) || GetKey(KeyCode.RightControl)) speed *= SlowMultiplier;
        Vector3 worldMove = transform.TransformDirection(input.normalized) * speed * Time.unscaledDeltaTime;
        transform.position += worldMove;
    }

    private void HandleSelection()
    {
        if (!WasLeftMousePressedThisFrame())
        {
            return;
        }

        Camera cam = ControlledCamera != null ? ControlledCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(GetMousePosition());
        MarineCreatureAgent found = null;
        if (Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, SelectionMask, QueryTriggerInteraction.Collide))
        {
            found = hit.collider.GetComponentInParent<MarineCreatureAgent>();
            if (found == null)
            {
                CreatureHurtbox hurtbox = hit.collider.GetComponentInParent<CreatureHurtbox>();
                found = hurtbox != null ? hurtbox.Owner : null;
            }
        }

        if (found == null && Physics.SphereCast(ray, SphereCastRadius, out RaycastHit sphereHit, RaycastDistance, SelectionMask, QueryTriggerInteraction.Collide))
        {
            found = sphereHit.collider.GetComponentInParent<MarineCreatureAgent>();
            if (found == null)
            {
                CreatureHurtbox hurtbox = sphereHit.collider.GetComponentInParent<CreatureHurtbox>();
                found = hurtbox != null ? hurtbox.Owner : null;
            }
        }

        if (found != null)
        {
            selectedFish = found;
            if (SelectionPanel == null)
            {
                SelectionPanel = FindFirstObjectByType<FishSelectionDebugPanel>();
            }

            if (SelectionPanel != null)
            {
                SelectionPanel.SelectedFish = found;
                SelectionPanel.ShowPanel = true;
            }
        }
    }

    private void HandleFocus()
    {
        if (GetKeyDown(KeyCode.F) && selectedFish != null)
        {
            if (ToggleFollowWithF)
            {
                isFollowingSelected = !isFollowingSelected;
                hasFocusTarget = false;
            }
            else
            {
                Vector3 back = -transform.forward;
                focusTargetPosition = selectedFish.transform.position + back * FocusDistance + Vector3.up * FocusHeight;
                hasFocusTarget = true;
            }
        }

        if (isFollowingSelected)
        {
            if (selectedFish == null)
            {
                isFollowingSelected = false;
                return;
            }

            Vector3 back = -transform.forward;
            Vector3 wanted = selectedFish.transform.position + back * FollowDistance + Vector3.up * FollowHeight;
            transform.position = Vector3.Lerp(transform.position, wanted, Mathf.Clamp01(FollowLerpSpeed * Time.unscaledDeltaTime));
            Vector3 lookDirection = selectedFish.transform.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Mathf.Clamp01(FollowLerpSpeed * Time.unscaledDeltaTime));
                Vector3 euler = transform.rotation.eulerAngles;
                yaw = euler.y;
                pitch = NormalisePitch(euler.x);
            }
            return;
        }

        if (hasFocusTarget)
        {
            transform.position = Vector3.Lerp(transform.position, focusTargetPosition, Mathf.Clamp01(FocusLerpSpeed * Time.unscaledDeltaTime));
            if ((transform.position - focusTargetPosition).sqrMagnitude < 0.05f)
            {
                hasFocusTarget = false;
            }
        }
    }

    private void HandleCursorRelease()
    {
        if (!GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        switch (key)
        {
            case KeyCode.W: return keyboard.wKey.isPressed;
            case KeyCode.A: return keyboard.aKey.isPressed;
            case KeyCode.S: return keyboard.sKey.isPressed;
            case KeyCode.D: return keyboard.dKey.isPressed;
            case KeyCode.Q: return keyboard.qKey.isPressed;
            case KeyCode.E: return keyboard.eKey.isPressed;
            case KeyCode.F: return keyboard.fKey.isPressed;
            case KeyCode.Escape: return keyboard.escapeKey.isPressed;
            case KeyCode.LeftShift: return keyboard.leftShiftKey.isPressed;
            case KeyCode.RightShift: return keyboard.rightShiftKey.isPressed;
            case KeyCode.LeftControl: return keyboard.leftCtrlKey.isPressed;
            case KeyCode.RightControl: return keyboard.rightCtrlKey.isPressed;
        }
        return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(key);
#else
        return false;
#endif
    }

    private bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        switch (key)
        {
            case KeyCode.F: return keyboard.fKey.wasPressedThisFrame;
            case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
        }
        return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#else
        return false;
#endif
    }

    private bool IsRightMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1);
#else
        return false;
#endif
    }

    private bool WasLeftMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f;
#else
        return Vector2.zero;
#endif
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private float NormalisePitch(float x)
    {
        return x > 180f ? x - 360f : x;
    }
}
