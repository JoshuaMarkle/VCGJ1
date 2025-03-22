using UnityEngine; using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance;

    private InputSystemActions playerInput;
    public Vector2 moveDir;
    public bool jumping;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        playerInput = new InputSystemActions();
        playerInput.Player.Enable();
        
        // Bind Input Actions
        playerInput.Player.Move.performed += ctx => moveDir = ctx.ReadValue<Vector2>();
        playerInput.Player.Move.canceled += ctx => moveDir = Vector2.zero;
        playerInput.Player.Jump.performed += ctx => jumping = true;
    }

	public void LateUpdate() {
		jumping = false;
	}
}
