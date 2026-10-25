
using UnityEngine;

public class IndicadorManoVR : MonoBehaviour
{
    [Header("CONTROLADOR VR")]
    public Transform manoDerecha;

    [Header("INTERFAZ")]
    public RectTransform indicador;
    public RectTransform seccionReferencia;

    [Header("CONFIGURACION")]
    public float distanciaMaxima = 0.15f;

    [Range(0f, 0.9f)]
    public float proporcionCentro = 0.34f;

    private RectTransform canvasRect;

    void Awake()
    {
        canvasRect = GetComponent<RectTransform>();

        if (indicador != null)
            indicador.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (manoDerecha == null ||
            indicador == null ||
            seccionReferencia == null)
            return;

        // Distancia de la mano al plano del menu
        Vector3 normal = seccionReferencia.forward;

        float distanciaConSigno = Vector3.Dot(
            manoDerecha.position - seccionReferencia.position,
            normal
        );

        float distancia = Mathf.Abs(distanciaConSigno);

        // Posicion local de la mano
        Vector3 posicionLocal =
            seccionReferencia.InverseTransformPoint(
                manoDerecha.position
            );

        Vector2 posicion = new Vector2(
            posicionLocal.x - seccionReferencia.rect.center.x,
            posicionLocal.y - seccionReferencia.rect.center.y
        );

        // Comprobar los limites del menu
        float radioExterior = Mathf.Min(
            seccionReferencia.rect.width,
            seccionReferencia.rect.height
        ) / 2f;

        float radioInterior =
            radioExterior * proporcionCentro;

        bool dentroDelMenu =
            distancia <= distanciaMaxima &&
            posicion.magnitude >= radioInterior &&
            posicion.magnitude <= radioExterior;

        indicador.gameObject.SetActive(dentroDelMenu);

        if (!dentroDelMenu)
            return;

        // Proyectar la mano sobre el plano del menu
        Vector3 puntoProyectado =
            manoDerecha.position -
            normal * distanciaConSigno;

        // Convertir al espacio local del Canvas
        Vector3 puntoCanvas =
            canvasRect.InverseTransformPoint(puntoProyectado);

        // Mover el indicador
        indicador.anchoredPosition = new Vector2(
            puntoCanvas.x,
            puntoCanvas.y
        );
    }
}
