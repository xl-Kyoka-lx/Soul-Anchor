using SoulAnchor.Datos.Entidades;

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

        // Aplica el efecto de curación sobre el objetivo. Pensado para conectarse al handler de
        // "Usar objeto" del menú Mochila (GDD 2.1) una vez que esa pantalla exista.
        public void AplicarEfecto(Personaje objetivo)
        {
            if (CuraHp)
            {
                int cantidad = (int)(objetivo.HpMaximo * (PorcentajeCura / 100f));
                objetivo.Curar(cantidad);
            }

            if (CuraMp)
            {
                int cantidad = (int)(objetivo.MpMaximo * (PorcentajeCura / 100f));
                objetivo.GanarMp(cantidad);
            }
        }
    }
}