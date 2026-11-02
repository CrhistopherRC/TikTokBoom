using System;
using UnityEngine;

/// <summary>
/// Keeps the nearby hotel floors active while the player is inside the building.
/// Floor roots and lights are assigned in the scene, so updates do not search the hierarchy.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-900)]
public sealed class HotelRendimientoVR : MonoBehaviour
{
    [Serializable]
    public sealed class Floor
    {
        public float altura;
        public GameObject[] grupos = Array.Empty<GameObject>();
        public Light[] luces = Array.Empty<Light>();

        [NonSerialized] internal bool[] lucesIniciales;
    }

    [SerializeField] private Transform jugador;
    [SerializeField] private Floor[] plantas = Array.Empty<Floor>();
    [SerializeField, Min(0.1f)] private float radioVertical = 5f;
    [SerializeField, Min(0.02f)] private float intervalo = 0.2f;

    private float siguienteActualizacion;
    private bool preparado;
    private bool saliendo;

    public int PlantasActivas { get; private set; }

    private void Awake()
    {
        if (jugador == null && Camera.main != null) jugador = Camera.main.transform.root;
        Preparar();
        ActualizarAhora();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        Preparar();
        ActualizarAhora();
    }

    private void Update()
    {
        if (Time.unscaledTime < siguienteActualizacion) return;
        ActualizarAhora();
    }

    private void Preparar()
    {
        if (preparado) return;
        preparado = true;

        if (plantas == null) return;
        for (int i = 0; i < plantas.Length; i++)
        {
            var planta = plantas[i];
            if (planta == null) continue;
            int cantidad = planta.luces == null ? 0 : planta.luces.Length;
            planta.lucesIniciales = new bool[cantidad];
            for (int j = 0; j < cantidad; j++)
            {
                var luz = planta.luces[j];
                planta.lucesIniciales[j] = luz != null && luz.enabled;
            }
        }
    }

    public void ActualizarAhora()
    {
        if (!Application.isPlaying) return;
        Preparar();
        siguienteActualizacion = Time.unscaledTime + Mathf.Max(0.02f, intervalo);

        bool dentro = false;
        float alturaJugador = 0f;
        if (jugador != null)
        {
            Vector3 local = transform.InverseTransformPoint(jugador.position);
            dentro = Finito(local.x) && Finito(local.y) && Finito(local.z)
                && Mathf.Abs(local.x) <= 22f && Mathf.Abs(local.z) <= 14f
                && local.y >= -1f && local.y <= 34f;
            alturaJugador = local.y;
        }

        PlantasActivas = 0;
        if (plantas == null) return;
        float distancia = Mathf.Max(0.1f, radioVertical);
        for (int i = 0; i < plantas.Length; i++)
        {
            var planta = plantas[i];
            if (planta == null) continue;
            bool activar = !dentro || !Finito(planta.altura)
                || Mathf.Abs(alturaJugador - planta.altura) <= distancia;
            if (activar) PlantasActivas++;
            Aplicar(planta, activar);
        }
    }

    private void Aplicar(Floor planta, bool activar)
    {
        if (planta.grupos != null)
        {
            for (int i = 0; i < planta.grupos.Length; i++)
            {
                var grupo = planta.grupos[i];
                if (grupo == null || grupo == gameObject || transform.IsChildOf(grupo.transform)) continue;
                if (grupo.activeSelf != activar) grupo.SetActive(activar);
            }
        }

        if (planta.luces == null || planta.lucesIniciales == null) return;
        int cantidad = Mathf.Min(planta.luces.Length, planta.lucesIniciales.Length);
        for (int i = 0; i < cantidad; i++)
        {
            var luz = planta.luces[i];
            if (luz == null) continue;
            bool habilitar = activar && planta.lucesIniciales[i];
            if (luz.enabled != habilitar) luz.enabled = habilitar;
        }
    }

    private void OnDisable()
    {
        if (saliendo || !preparado || plantas == null) return;
        PlantasActivas = 0;
        for (int i = 0; i < plantas.Length; i++)
        {
            var planta = plantas[i];
            if (planta == null) continue;
            Aplicar(planta, true);
            PlantasActivas++;
        }
    }

    private void OnApplicationQuit() { saliendo = true; }

    private static bool Finito(float valor)
    {
        return !float.IsNaN(valor) && !float.IsInfinity(valor);
    }
}
