using System.Text.Json.Serialization;

namespace SoulAnchor.Datos.Mundo
{
    // Efecto de estado que un hechizo puede aplicar al impactar (ej. quemadura de Bola de Fuego).
    // Mismo esqueleto que Enemigo.OnHitStatus, pero para hechizos en vez de ataques enemigos.
    public class HechizoEfectoExtra
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("chance_pct")]
        public int ChancePct { get; set; }

        [JsonPropertyName("duracion_turnos")]
        public int DuracionTurnos { get; set; }

        [JsonPropertyName("dano_por_turno")]
        public int DanoPorTurno { get; set; }
    }

    // Ficha de un hechizo tal como se lee de hechizos_*_stats.json (GDD 4.1).
    public class HechizoData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("elemento")]
        public string Elemento { get; set; } = "";

        [JsonPropertyName("dano_base")]
        public int DanoBase { get; set; }

        [JsonPropertyName("dano_por_nivel")]
        public float DanoPorNivel { get; set; }

        [JsonPropertyName("costo_mp")]
        public int CostoMp { get; set; }

        [JsonPropertyName("efecto_extra")]
        public HechizoEfectoExtra? EfectoExtra { get; set; }
    }
}
