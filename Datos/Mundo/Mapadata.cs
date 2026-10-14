using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SoulAnchor.Datos.Mundo
{
    public class PosicionData
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
    }

    public class UbicacionData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }

        // "ciudad" abre el menú de ciudad; "pueblo", "cueva" y "tp" son puntos especiales
        // (otro mapa), todavía sin implementar.
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "ciudad";
        [JsonPropertyName("implementada")] public bool Implementada { get; set; }
    }

    // Rectángulo de mapa que cambia la tabla de monstruos. Los límites son inclusivos.
    // Se evalúan en orden: gana la primera zona que contenga la casilla.
    public class ZonaData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("min_x")] public int MinX { get; set; }
        [JsonPropertyName("max_x")] public int MaxX { get; set; }
        [JsonPropertyName("min_y")] public int MinY { get; set; }
        [JsonPropertyName("max_y")] public int MaxY { get; set; }
        [JsonPropertyName("tabla_monstruos")] public string TablaMonstruos { get; set; } = "";

        // 1.0 = frecuencia normal de la tabla. 0.25 = un cuarto de los encuentros.
        [JsonPropertyName("multiplicador_encuentros")] public double MultiplicadorEncuentros { get; set; } = 1.0;

        public bool Contiene(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    // Rectángulo que bloquea el paso (ej. el río).
    public class ObstaculoData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("min_x")] public int MinX { get; set; }
        [JsonPropertyName("max_x")] public int MaxX { get; set; }
        [JsonPropertyName("min_y")] public int MinY { get; set; }
        [JsonPropertyName("max_y")] public int MaxY { get; set; }
        [JsonPropertyName("evento")] public string Evento { get; set; } = "";

        public bool Contiene(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    // Casilla que cruza un obstáculo, pero solo si el flag de historia está activo.
    public class PuenteData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
        [JsonPropertyName("flag_requerido")] public string FlagRequerido { get; set; } = "";
        [JsonPropertyName("evento_destruido")] public string EventoDestruido { get; set; } = "";

        // Casillas desde las que se avisa del estado del puente al llegar.
        [JsonPropertyName("avisos_en")] public List<PosicionData> AvisosEn { get; set; } = new();
    }

    public class MapaData
    {
        [JsonPropertyName("limite")] public int Limite { get; set; } = 13;
        [JsonPropertyName("ubicaciones")] public List<UbicacionData> Ubicaciones { get; set; } = new();
        [JsonPropertyName("zonas")] public List<ZonaData> Zonas { get; set; } = new();
        [JsonPropertyName("zona_por_defecto")] public ZonaData ZonaPorDefecto { get; set; } = new();
        [JsonPropertyName("obstaculos")] public List<ObstaculoData> Obstaculos { get; set; } = new();
        [JsonPropertyName("puentes")] public List<PuenteData> Puentes { get; set; } = new();
    }

    public class EnemigoPesoData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("peso")] public int Peso { get; set; }
    }

    public class GrupoPesoData
    {
        [JsonPropertyName("cantidad")] public int Cantidad { get; set; }
        [JsonPropertyName("peso")] public int Peso { get; set; }
    }

    // Un archivo monstruos_<zona>.json: qué enemigos salen, cuántos juntos y con qué frecuencia.
    public class TablaMonstruosData
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("encuentro_pct")] public double EncuentroPct { get; set; }
        [JsonPropertyName("loot_pct")] public double LootPct { get; set; }

        // Se suma al nivel del protagonista al crear los enemigos (ej. el Rift es más duro).
        [JsonPropertyName("bonus_nivel")] public int BonusNivel { get; set; }

        [JsonPropertyName("enemigos")] public List<EnemigoPesoData> Enemigos { get; set; } = new();
        [JsonPropertyName("tamano_grupo")] public List<GrupoPesoData> TamanoGrupo { get; set; } = new();
    }
}