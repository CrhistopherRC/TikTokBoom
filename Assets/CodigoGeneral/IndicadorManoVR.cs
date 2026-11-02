using UnityEngine;
using UnityEngine.UI;

public class IndicadorManoVR : MonoBehaviour
{
    [Header("CONTROLADOR VR")]
    public Transform manoDerecha;
    [Header("INTERFAZ")]
    public RectTransform indicador;
    public RectTransform seccionReferencia;
    public SelectorMenuRadial selector;
    [Header("CONFIGURACION")]
    public float distanciaMaxima = .15f;
    [Range(0f, .9f)] public float proporcionCentro = .37f;
    private RectTransform canvasRect;
    private Image imagen;

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        if (selector == null) selector = GetComponent<SelectorMenuRadial>();
        if (indicador != null)
        {
            imagen = indicador.GetComponent<Image>();
            indicador.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (indicador == null || canvasRect == null) return;
        bool visible = selector != null && selector.enabled && selector.MenuAbierto && selector.PuntoEnMenu;
        indicador.gameObject.SetActive(visible);
        if (!visible) return;
        Vector3 local = canvasRect.InverseTransformPoint(selector.PuntoApuntado);
        indicador.anchoredPosition = new Vector2(local.x, local.y);
        if (imagen != null) imagen.color = selector.OpcionActual >= 0 ? Color.cyan : new Color(.85f, .9f, 1f);
    }
}
