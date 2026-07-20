namespace SoulAnchor.Datos.Items
{
    public class Consumible : Item
    {
        public bool CuraHp { get; private set; }
        public bool CuraMp { get; private set; }
        public float PorcentajeCura { get; private set; }
        public bool EsMaterialCrafteo { get; private set; }

        public Consumible(string id, string nombre, string desc, int valor, bool curaHp, bool curaMp, float porcentaje, bool esMaterial)
        {
            Id = id;
            Nombre = nombre;
            Descripcion = desc;
            ValorOro = valor;
            CuraHp = curaHp;
            CuraMp = curaMp;
            PorcentajeCura = porcentaje;
            EsMaterialCrafteo = esMaterial;
        }
    }
}