using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

public sealed class InicioJuegoVR : MonoBehaviour
{
    [SerializeField] private string escenaDestino = "Assets/Scenes/Escene Edificio/Edificio.unity";
    [SerializeField] private Button botonInicio;
    [SerializeField] private TextMeshProUGUI textoEstado;
    [SerializeField] private Image bordeEnfoque;
    private Camera camara;
    private bool gatilloAnterior;
    private bool cargando;
    private SelectorMenuRadial selectorMenu;

    private void Start()
    {
        foreach (var selector in FindObjectsByType<SelectorMenuRadial>(FindObjectsSortMode.None))
            if (selector.manoDerecha != null && selector.opciones != null && selector.opciones.Length > 0)
            {
                selectorMenu = selector;
                break;
            }
    }

    private void Update()
    {
        if (cargando || botonInicio == null) return;
        if (camara == null) camara = Camera.main;
        bool mirando = false;
        if (camara != null)
        {
            RectTransform rect = botonInicio.GetComponent<RectTransform>();
            Ray rayo = new Ray(camara.transform.position, camara.transform.forward);
            Plane plano = new Plane(rect.forward, rect.position);
            if (plano.Raycast(rayo, out float distancia) && distancia > 0f)
            {
                Vector3 punto = rect.InverseTransformPoint(rayo.GetPoint(distancia));
                mirando = rect.rect.Contains(new Vector2(punto.x, punto.y));
            }
        }
        bool apuntando = selectorMenu != null && selectorMenu.BotonApuntado == botonInicio;
        if (bordeEnfoque != null) bordeEnfoque.enabled = mirando || apuntando;
        if (textoEstado != null)
            textoEstado.text = apuntando ? "PULSA X PARA COMENZAR"
                : mirando ? "GATILLO PARA INICIAR / X SI APUNTAS CON EL MANDO"
                : "APUNTA AL BOTÓN CON EL MANDO Y PULSA X";
        bool gatillo = Gatillo(XRNode.RightHand) || Gatillo(XRNode.LeftHand);
        if (mirando && gatillo && !gatilloAnterior) botonInicio.onClick.Invoke();
        gatilloAnterior = gatillo;
    }

    private static bool Gatillo(XRNode mano)
    {
        InputDevice dispositivo = InputDevices.GetDeviceAtXRNode(mano);
        if (!dispositivo.isValid) return false;
        if (dispositivo.TryGetFeatureValue(CommonUsages.triggerButton, out bool pulsado) && pulsado) return true;
        return dispositivo.TryGetFeatureValue(CommonUsages.trigger, out float valor) && valor > 0.75f;
    }

    public void IniciarJuego()
    {
        if (cargando) return;
        if (!Application.CanStreamedLevelBeLoaded(escenaDestino))
        {
            Debug.LogError("La escena de destino no está disponible: " + escenaDestino, this);
            if (textoEstado != null) textoEstado.text = "NO SE PUDO ABRIR EL EDIFICIO";
            return;
        }
        cargando = true;
        if (botonInicio != null) botonInicio.interactable = false;
        if (textoEstado != null) textoEstado.text = "CARGANDO EDIFICIO...";
        SceneManager.LoadSceneAsync(escenaDestino, LoadSceneMode.Single);
    }
}
