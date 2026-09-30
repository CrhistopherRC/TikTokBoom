using UnityEngine;
using UnityEngine.InputSystem;

public class AnimacionEntrada : MonoBehaviour
{
    public InputActionProperty DispadadorValue;
    public InputActionProperty AgarradorValue;

    public Animator AnimacionDeMano;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float Trigger = DispadadorValue.action.ReadValue<float>();
        float Grip = AgarradorValue.action.ReadValue<float>();

        AnimacionDeMano.SetFloat("Trigger", Trigger);
        AnimacionDeMano.SetFloat("Grip", Grip);
        
    }
}
