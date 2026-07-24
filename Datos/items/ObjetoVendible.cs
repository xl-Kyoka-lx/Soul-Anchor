namespace SoulAnchor.Datos.Items
{
    public class ObjetoVendible : Item
    {
        public ObjetoVendible(string id, string nombre, string desc, int valor)
        {
            Id = id;
            Nombre = nombre;
            Descripcion = desc;
            ValorOro = valor;
        }
    }
}
