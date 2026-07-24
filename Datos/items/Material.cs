using System.Collections.Generic;

namespace SoulAnchor.Datos.Items
{
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
