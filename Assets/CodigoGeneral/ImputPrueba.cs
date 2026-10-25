using UnityEngine;
using UnityEngine.InputSystem;

public class InputPrueba : MonoBehaviour
{
    public InputActionProperty InputActionValue;    
    public InputActionProperty InputActionButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float value = InputActionValue.action.ReadValue<float>();
        Debug.Log("Value : " + value);

        bool button = InputActionButton.action.IsPressed();
        Debug.Log("Button : " + button);
        
    }
}
