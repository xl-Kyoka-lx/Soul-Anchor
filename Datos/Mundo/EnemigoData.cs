using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SoulAnchor.Datos.Mundo
{
    // Una entrada de la drop_table de un enemigo.
    public class DropEntry
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = "";

        [JsonPropertyName("chance_pct")]
        public int ChancePct { get; set; }

        // Solo relevante para el drop especial "oro": porcentaje extra sobre la recompensa base
        // (ej. Goblin y Ladrón sueltan "más de lo normal" según la tabla de enemigos).
        [JsonPropertyName("cantidad_extra_pct")]
        public int CantidadExtraPct { get; set; }
    }

    // Estado que un enemigo aplica al jugador/aliado al conectar un golpe (ej. veneno de la Araña Gigante).
    public class OnHitStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("duracion_turnos")]
        public int DuracionTurnos { get; set; }

        [JsonPropertyName("chance_pct")]
        public int ChancePct { get; set; }

        [JsonPropertyName("dano_por_turno")]
        public int DanoPorTurno { get; set; }
    }

    // Mecánicas especiales de un enemigo. Todo lo que antes hubiera sido "un if hardcodeado por nombre"
    // ahora vive acá, así agregar un enemigo nuevo no toca C#.
    public class EnemigoTraits
    {
        [JsonPropertyName("evasion_pct")]
        public int EvasionPct { get; set; }

        [JsonPropertyName("debilidad_elemental")]
        public string? DebilidadElemental { get; set; }

        [JsonPropertyName("debilidad_multiplicador")]
        public float DebilidadMultiplicador { get; set; } = 1.0f;

        [JsonPropertyName("usa_pocion_curacion_barata")]
        public bool UsaPocionCuracionBarata { get; set; }

        // Pesos de aggro por nombre de aliado (ej. {"Sumire": 70, "default": 15}).
        // Si es null, Enemigo.ElegirObjetivo cae al comportamiento clásico del GDD 17.1.
        [JsonPropertyName("aggro_override")]
        public Dictionary<string, int>? AggroOverride { get; set; }

        [JsonPropertyName("on_hit_apply_status")]
        public OnHitStatus? OnHitApplyStatus { get; set; }
    }

    // Ficha completa de un enemigo, tal como se lee de enemigos_*_stats.json.
    public class EnemigoData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("hp")]
        public int Hp { get; set; }

        [JsonPropertyName("atk")]
        public int Atk { get; set; }

        [JsonPropertyName("def")]
        public int Def { get; set; }

        [JsonPropertyName("xp_base")]
        public int XpBase { get; set; }

        [JsonPropertyName("tipo_ia")]
        public string TipoIA { get; set; } = "Normal";

        [JsonPropertyName("traits")]
        public EnemigoTraits Traits { get; set; } = new();

        [JsonPropertyName("drop_table")]
        public List<DropEntry> DropTable { get; set; } = new();
    }
}
