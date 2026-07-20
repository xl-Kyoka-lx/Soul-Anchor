using System;
using System.Collections.Generic;
using SoulAnchor.Datos;
using SoulAnchor.Datos.Entidades;
using SoulAnchor.Datos.Mundo;

// que paja, ia
namespace SoulAnchor.Managers
{
    public enum EstadoJuego { MenuPrincipal, Explorando, EnCiudad, EnCombate, FinJuego }

    public class GameManager
    {
        public EstadoJuego EstadoActual { get; private set; }
        
        public Jugador Prota { get; private set; }
        public List<Personaje> GrupoActivo { get; private set; }
        
        public float PosicionX { get; private set; }
        public float PosicionY { get; private set; }
        public Ubicacion UbicacionActual { get; private set; }
        public Ubicacion DestinoFijado { get; private set; }

        // Managers secundarios
        public QuestManager Gremio { get; private set; }
        public CombatSystem CombateActual { get; private set; }

        public GameManager()
        {
            EstadoActual = EstadoJuego.MenuPrincipal;
            Gremio = new QuestManager();
            GrupoActivo = new List<Personaje>();
        }

        // ==========================================
        // 1. INICIO Y CARGA DE PARTIDA
        // ==========================================
        
        public void IniciarNuevaPartida(string nombreProta)
        {
            // Creamos al equipo desde la Database
            Prota = Database.CrearRen(nombreProta);
            Companero sumire = Database.CrearSumire();
            Companero tetsu = Database.CrearTetsu();

            GrupoActivo.Clear();
            GrupoActivo.Add(Prota);
            GrupoActivo.Add(sumire);
            GrupoActivo.Add(tetsu);

            var ciudades = RegistroMapa.ObtenerTodasLasUbicaciones();
            UbicacionActual = ciudades[0]; // Ciudad del Oeste
            PosicionX = UbicacionActual.X;
            PosicionY = UbicacionActual.Y;

            EstadoActual = EstadoJuego.EnCiudad; // Empezamos a salvo
        }

        public void CargarPartida()
        {
            DatosGuardado datos = SaveManager.CargarPartida();
            if (datos != null)
            {
                // Aquí reconstruirías a los personajes usando los datos
                // y actualizarías PosicionX y PosicionY.
                EstadoActual = EstadoJuego.Explorando;
            }
        }

        // ==========================================
        // 2. SISTEMA DE EXPLORACIÓN Y VIAJE
        // ==========================================
        
        public void FijarDestino(Ubicacion nuevoDestino)
        {
            DestinoFijado = nuevoDestino;
            EstadoActual = EstadoJuego.Explorando;
            UbicacionActual = null; // Ya no estamos en la ciudad
        }

        public void AvanzarHaciaDestino()
        {
            if (DestinoFijado == null) return;

            float[] nuevaPos = Ubicacion.AvanzarHaciaDestino(PosicionX, PosicionY, DestinoFijado);
            PosicionX = nuevaPos[0];
            PosicionY = nuevaPos[1];

            float distanciaRestante = Ubicacion.CalcularDistancia(PosicionX, PosicionY, DestinoFijado.X, DestinoFijado.Y);
            if (distanciaRestante <= 0f)
            {
                UbicacionActual = DestinoFijado;
                DestinoFijado = null;
                EstadoActual = EstadoJuego.EnCiudad; // Llegamos a puerto seguro
                return;
            }

            LanzarRNGEncuentro();
        }

        private void LanzarRNGEncuentro()
        {
            Random rng = new Random();
            int tirada = rng.Next(1, 11);

            if (tirada >= 5 && tirada <= 8)
            {
                GenerarCombateSalvaje();
            }
            else if (tirada == 9)
            {
                Prota.AgregarObjeto("Hierba Orderia", 1);
            }
        }

        // ==========================================
        // 3. TRANSICIÓN A COMBATE
        // ==========================================
        
        private void GenerarCombateSalvaje()
        {
            // Creamos un grupo de enemigos basado en el nivel del jugador
            List<Enemigo> enemigosSalvajes = new List<Enemigo>();
            enemigosSalvajes.Add(Database.CrearLobo(Prota.Nivel)); 
            
            // Podrías añadir lógica aquí para que a veces salgan 2 o 3 enemigos

            CombateActual = new CombatSystem(GrupoActivo, enemigosSalvajes);
            EstadoActual = EstadoJuego.EnCombate;
        }

        public void RevisarFinDeCombate()
        {
            if (CombateActual != null && CombateActual.CombateTerminado)
            {
                if (CombateActual.Victoria)
                {
                    CombateActual = null;
                    EstadoActual = EstadoJuego.Explorando;
                }
                else
                {
                    if (Prota.EstaVivo()) 
                    {
                        EstadoActual = EstadoJuego.Explorando;
                    }
                    else
                    {

                        EjecutarPenalizacionMuerte();
                    }
                }
            }
        }

        private void EjecutarPenalizacionMuerte()
        {
            Prota.Curar(1); 
            Prota.GastarOro(50);
            CargarPartida(); 
        }
    }
}