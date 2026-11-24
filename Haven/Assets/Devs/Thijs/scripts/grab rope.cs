
    using UnityEngine;
using UnityEngine.InputSystem;

public class RopeGrabber : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    [SerializeField] private RopeVerlet _rope;
    [SerializeField] private InputActionReference _gripAction;
    [SerializeField] private float _grabRadius = 0.12f;

    private void OnEnable()
    {
        _gripAction.action.Enable();
        _gripAction.action.performed += OnGripPressed;
        _gripAction.action.canceled += OnGripReleased;
    }

    private void OnDisable()
    {
        _gripAction.action.performed -= OnGripPressed;
        _gripAction.action.canceled -= OnGripReleased;
        _rope.ReleaseSegments(transform);
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        int index = _rope.GetClosestSegment(transform.position, _grabRadius);

        if (index != -1)
        {
            _rope.GrabSegment(index, transform);
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        _rope.ReleaseSegments(transform);
    }
}
