using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;

    public static float worldQuatTheta = 0f;
    public static Vector3 worldQuatDirn = new(0f, 0f, 1f);
    public static bool hasMoved = false;

    // User configurable fields
    [SerializeField] private float moveSpeed = 10f;


    private void Awake()
    {
        // 1. Initialize your custom controls asset
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        // 2. You must enable the action map to listen to keys
        controls.Movement.Enable();
    }

    private void OnDisable()
    {
        // 3. Disable it when the object is inactive to avoid memory leaks
        controls.Movement.Disable();
    }

    private void Update()
    {

        bool dirty = false;
        Quaternion q = new();


        // Read the 1D Axis value (W returns 1, S returns -1, nothing returns 0)
        float fwdrevInput = controls.Movement.FwdRev.ReadValue<float>();
        if (fwdrevInput > 0)
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.right);
        }

        if (fwdrevInput < 0)
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.left);
        }

        // Read the 1D Axis value (D returns 1, A returns -1, nothing returns 0)
        float leftrightInput = controls.Movement.LeftRight.ReadValue<float>();
        if (leftrightInput > 0)
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.forward);
        }

        if (leftrightInput < 0)
        {
            dirty = true;
            q = Quaternion.AngleAxis(moveSpeed * Time.deltaTime, Vector3.back);
        }


        if (dirty)
        {

            Quaternion worldQ = Quaternion.AngleAxis(worldQuatTheta * 180f / Mathf.PI, worldQuatDirn);
            worldQ = q * worldQ;
            worldQ.ToAngleAxis(out float worldQuatThetaDeg, out worldQuatDirn);
            worldQuatTheta = worldQuatThetaDeg * Mathf.PI / 180f;

        }

        hasMoved = dirty;

    }
}
