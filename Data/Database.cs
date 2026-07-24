using System;
using System.Collections.Generic;
using SoulAnchor.Datos.Entidades;
using SoulAnchor.Managers;

namespace SoulAnchor.Datos
{
    public static class Database
    {
       
        public static Jugador CrearRen(string nombrePersonalizado = "Ren")
        {
            // Orden: Nombre, Nivel, HPMax, MPMax, ATK, DEF
            return new Jugador(nombrePersonalizado, 1, 100, 70, 10, 8);
        }

        public static Companero CrearSumire()
        {
            return new Companero("Sumire", 1, 80, 120, 12, 6);
        }

        public static Companero CrearTetsu()
        {
            return new Companero("Tetsu", 1, 140, 20, 25, 15); 
        }

        public static Enemigo CrearDesdeId(string id, int nivelGenerado)
        {
            var datos = EnemigoDatabase.Obtener(id);

            int hpBase = datos.Hp + (nivelGenerado * 5);
            int atkBase = datos.Atk + (nivelGenerado * 2);
            int defBase = datos.Def + nivelGenerado;

            TipoIA tipo = Enum.TryParse<TipoIA>(datos.TipoIA, out var parsed) ? parsed : TipoIA.Normal;

            var enemigo = new Enemigo(datos.Id, nivelGenerado, hpBase, 0, atkBase, defBase, tipo);
            enemigo.AsignarDatos(datos);
            return enemigo;
        }

        // Wrappers de compatibilidad: mantienen el nombre de método que ya usa GameManager,
        // pero ahora leen los stats desde el JSON en vez de tenerlos hardcodeados.
        public static Enemigo CrearLobo(int nivelGenerado) => CrearDesdeId("lobo", nivelGenerado);
        public static Enemigo CrearGoblin(int nivelGenerado) => CrearDesdeId("goblin", nivelGenerado);
        public static Enemigo CrearLadron(int nivelGenerado) => CrearDesdeId("ladron", nivelGenerado);
        public static Enemigo CrearEsqueleto(int nivelGenerado) => CrearDesdeId("esqueleto", nivelGenerado);
        public static Enemigo CrearAranaGigante(int nivelGenerado) => CrearDesdeId("arana_gigante", nivelGenerado);

        public static Enemigo CrearEcoDelRift(Jugador jugadorActual)
        {
            return new Enemigo("Eco del Rift", jugadorActual.Nivel, jugadorActual.HpMaximo, 0, jugadorActual.Atk, jugadorActual.Def, TipoIA.EcoDelRift);
        }
    }
    
}