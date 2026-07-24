namespace SoulAnchor.Datos.Items
{
    // Objeto "basura/tesoro": no se puede usar ni craftear con él. Su único propósito es
    // venderse en la Herrería/Tienda a cambio de oro (ej. Armadura Rota).
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
