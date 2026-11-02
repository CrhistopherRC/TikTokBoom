using UnityEngine;
using UnityEngine.InputSystem;

public class InputPrueba : MonoBehaviour
{
    public InputActionProperty InputActionValue;
    public InputActionProperty InputActionButton;
    [SerializeField] private bool mostrarDiagnostico;
    private float ultimoValor;
    private bool ultimoBoton;
    private bool primeraLectura = true;

    private void Update()
    {
        if (!mostrarDiagnostico) return;
        var valorAction = InputActionValue.action;
        var botonAction = InputActionButton.action;
        if (valorAction == null || botonAction == null) return;
        float valor = valorAction.ReadValue<float>();
        bool boton = botonAction.IsPressed();
        if (primeraLectura || Mathf.Abs(valor - ultimoValor) > .01f)
            Debug.Log("Value : " + valor, this);
        if (primeraLectura || boton != ultimoBoton)
            Debug.Log("Button : " + boton, this);
        ultimoValor = valor;
        ultimoBoton = boton;
        primeraLectura = false;
    }
}
