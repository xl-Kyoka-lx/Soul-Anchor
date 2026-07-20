namespace SoulAnchor.Datos.Items
{
    public abstract class Item
    {
        public string Id { get; protected set; }
        public string Nombre { get; protected set; }
        public string Descripcion { get; protected set; }
        public int ValorOro { get; protected set; }
    }
}