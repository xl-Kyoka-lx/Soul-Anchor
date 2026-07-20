namespace SoulAnchor.Datos.Items
{
    public class Arma : Item
    {
        public int AtkBase { get; private set; }
        public int NivelAfinidad { get; private set; }
        public bool EsMagica { get; private set; }

        public Arma(string id, string nombre, string desc, int valor, int atkBase, bool esMagica)
        {
            Id = id;
            Nombre = nombre;
            Descripcion = desc;
            ValorOro = valor;
            AtkBase = atkBase;
            NivelAfinidad = 1;
            EsMagica = esMagica;
        }

        public int ObtenerDanioTotal(int nivelPersonaje, bool esTetsu)
        {
            if (EsMagica) return AtkBase;

            int nivelEfectivo = nivelPersonaje;
            
            if (esTetsu) nivelEfectivo += 5; 

            return AtkBase + nivelEfectivo; 
        }

        public void MejorarEnHerreria(int aumentoBase)
        {
            AtkBase += aumentoBase;
        }
    }
}