using System.Collections.Generic;

namespace SoulAnchor.Datos.Items
{
    // Objeto que solo existe como ingrediente de crafteo (ver CraftingManager). No cura, no se
    // "usa" directamente sobre un personaje; su único destino es acumularse en la Mochila hasta
    // reunir la cantidad que pide una receta. Si UsadoEnCrafteo está vacío, el objeto todavía no
    // tiene ninguna receta implementada y simplemente ocupa espacio en el inventario (ej. Hueso).
    public class Material : Item
    {
        public List<string> UsadoEnCrafteo { get; private set; }

        public Material(string id, string nombre, string desc, int valor, List<string>? usadoEnCrafteo = null)
        {
            Id = id;
            Nombre = nombre;
            Descripcion = desc;
            ValorOro = valor;
            UsadoEnCrafteo = usadoEnCrafteo ?? new List<string>();
        }
    }
}
