using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SoulAnchor.Datos.Items
{
    // Ficha cruda de un objeto tal como se lee de objetos_stats.json.
    // La 'category' determina qué clase concreta construye ItemDatabase.CrearInstancia:
    //   "consumible" -> Consumible | "material" -> Material | "basura" -> ObjetoVendible
    public class ItemData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("valor_oro")]
        public int ValorOro { get; set; }

        [JsonPropertyName("cura_hp_pct")]
        public float CuraHpPct { get; set; }

        [JsonPropertyName("cura_mp_pct")]
        public float CuraMpPct { get; set; }

        // Ids de recetas de CraftingManager que consumen este objeto como ingrediente.
        // Vacío para los que todavía no tiene ningún uso implementado
        [JsonPropertyName("usado_en_crafteo")]
        public List<string> UsadoEnCrafteo { get; set; } = new();
    }
}