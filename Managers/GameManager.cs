using System;
using System.Collections.Generic;
using System.Linq;
using SoulAnchor.Datos;
using SoulAnchor.Datos.Entidades;
using SoulAnchor.Datos.Mundo;

// que paja, ia
namespace SoulAnchor.Managers
{
    public enum EstadoJuego { MenuPrincipal, Explorando, EnCiudad, EnCombate, FinJuego }

    // Las 4 direcciones del movimiento por el plano 13x13 (GDD 8.1). Avanzar = Norte (y+1),
    // Retroceder = Sur (y-1), Derecha = Este (x+1), Izquierda = Oeste (x-1). Cada paso = 1 casilla.
    public enum DireccionMovimiento { Norte, Sur, Este, Oeste }

    public class GameManager
    {
        public EstadoJuego EstadoActual { get; private set; }

        // Prota puede ser null hasta que se llame a IniciarNuevaPartida() o CargarPartida()
        public Jugador? Prota { get; private set; }
        public List<Personaje> GrupoActivo { get; private set; }

        // Sumire y Tetsu existen desde el inicio de la partida, pero no se unen al GrupoActivo
        // (ni aparecen en el header de stats) hasta que UnirCompanieros() se llama, tras el
        // reclutamiento narrativo en el gremio (ver GDD Parte 2).
        private Companero? sumire;
        private Companero? tetsu;

        // Posición del jugador en el plano cartesiano (enteros, de -13 a 13). Es lo que se guarda
        // en la partida y lo que decide en qué zona está y qué hay en esa casilla.
        public int PosicionX { get; private set; }
        public int PosicionY { get; private set; }

        // Ubicación (ciudad, cueva, portal...) sobre la que está parado el jugador ahora mismo, o
        // null si está en una casilla vacía. Estar sobre una ciudad NO es estar dentro: para eso
        // el jugador elige "Entrar" y EstadoActual pasa a EnCiudad.
        public Ubicacion? UbicacionActual { get; private set; }

        // Última ciudad visitada: punto de reaparición si el grupo cae en combate.
        public Ubicacion? UltimaCiudadVisitada { get; private set; }

        // Ubicaciones que el jugador ya pisó (GDD 9.4: el Teletransporte solo lleva a lugares
        // visitados). Se llena sola en Mover() al llegar a cada punto.
        public HashSet<string> UbicacionesDescubiertas { get; private set; }

        // Flags de historia (ej. "puente_reparado"). El mapa los consulta para abrir caminos.
        public HashSet<string> Flags { get; private set; }

        // Resultado del último paso de exploración, para que la UI muestre feedback sin acoplar
        // texto localizado a este manager: "limite", "rio", "puente_destruido", "loot" o null.
        public string? UltimoEventoExploracion { get; private set; }

        // Managers secundarios
        public QuestManager Gremio { get; private set; }
        public StoryManager Historia { get; private set; }
        public PreguntaManager Preguntas { get; private set; }
        public CombatSystem? CombateActual { get; private set; }

        // Progreso narrativo mínimo: ¿ya se publicó el anuncio buscando el artefacto?
        // Antes de esto, la Ciudad del Oeste solo ofrece Posada y Gremio (ver GDD Parte 2).
        public bool AnuncioColocado { get; private set; }

        private readonly Random rng = new Random();

        public void ColocarAnuncio()
        {
            AnuncioColocado = true;
        }

        public GameManager()
        {
            EstadoActual = EstadoJuego.MenuPrincipal;
            Gremio = new QuestManager();
            Historia = new StoryManager();
            Preguntas = new PreguntaManager();
            GrupoActivo = new List<Personaje>();
            UbicacionesDescubiertas = new HashSet<string>();
            Flags = new HashSet<string>();
        }

        // 1. INICIO Y CARGA DE PARTIDA

        public void IniciarNuevaPartida(string nombreProta)
        {
            // Creamos al equipo desde la Database. Sumire y Tetsu existen desde ya (tienen stats,
            // pueden subir de nivel más adelante si hiciera falta), pero todavía no forman parte
            // del grupo activo: se unen narrativamente al terminar el reclutamiento en el gremio.
            Prota = Database.CrearRen(nombreProta);
            sumire = Database.CrearSumire();
            tetsu = Database.CrearTetsu();

            GrupoActivo.Clear();
            GrupoActivo.Add(Prota);

            // La partida empieza dentro de la ciudad del Oeste (menú de ciudad directo).
            var oeste = MapaDatabase.ObtenerPorId("oeste") ?? MapaDatabase.Ubicaciones[0];
            UbicacionActual = oeste;
            UltimaCiudadVisitada = oeste;
            PosicionX = oeste.X;
            PosicionY = oeste.Y;

            UbicacionesDescubiertas.Clear();
            UbicacionesDescubiertas.Add(oeste.Nombre);
            Flags.Clear();

            EstadoActual = EstadoJuego.EnCiudad; // Empezamos a salvo
        }

        // Se llama cuando el reclutamiento narrativo termina (tras colocar el anuncio en el gremio).
        // Suma a Sumire y Tetsu al grupo activo si todavía no estaban.
        public void UnirCompanieros()
        {
            if (sumire != null && !GrupoActivo.Contains(sumire))
            {
                GrupoActivo.Add(sumire);
            }

            if (tetsu != null && !GrupoActivo.Contains(tetsu))
            {
                GrupoActivo.Add(tetsu);
            }
        }

        public void CargarPartida()
        {
            DatosGuardado? datos = SaveManager.CargarPartida();
            if (datos != null)
            {
                EstadoActual = EstadoJuego.Explorando;
            }
        }

        public void DescubrirUbicacion(string nombre)
        {
            UbicacionesDescubiertas.Add(nombre);
        }

        // Ciudades ya visitadas, candidatas a destino del futuro Teletransporte (GDD 9.4: "Solo hacia
        // ciudades previamente visitadas"). Todavía no hay UI que lo use.
        public List<Ubicacion> ObtenerDestinosDisponibles()
        {
            string? actual = UbicacionActual?.Nombre;

            return MapaDatabase.Ubicaciones
                .Where(u => u.EsCiudad && UbicacionesDescubiertas.Contains(u.Nombre) && u.Nombre != actual)
                .ToList();
        }

        // Para el futuro Teletransporte: salta directo a la ciudad, sin pasos intermedios.
        public void TeletransportarA(Ubicacion destino)
        {
            PosicionX = destino.X;
            PosicionY = destino.Y;
            UbicacionActual = destino;
            UltimaCiudadVisitada = destino;
            UltimoEventoExploracion = null;
            EstadoActual = EstadoJuego.EnCiudad;
        }

        public bool TieneFlag(string flag) => Flags.Contains(flag);

        // Ej. ActivarFlag("puente_reparado") al terminar la historia: el puente pasa a cruzarse.
        public void ActivarFlag(string flag) => Flags.Add(flag);

        // ==========================================
        // 2. EXPLORACIÓN POR EL PLANO (GDD 8.1, mapa 13x13 en Data/Jsons/soul anchor mapa/mapa.json)
        // ==========================================

        // Un paso de 1 casilla. Si la casilla de destino está fuera del mapa o bloqueada (río,
        // puente destruido) el jugador no se mueve y UltimoEventoExploracion dice por qué. Si hay
        // una ubicación en la casilla, UbicacionActual la refleja (la UI ofrece "Entrar"). En
        // casillas vacías se tira el encuentro de la zona (monstruos distintos según el color).
        public void Mover(DireccionMovimiento direccion)
        {
            if (Prota == null) return;

            int nuevaX = PosicionX;
            int nuevaY = PosicionY;

            switch (direccion)
            {
                case DireccionMovimiento.Norte: nuevaY++; break;
                case DireccionMovimiento.Sur: nuevaY--; break;
                case DireccionMovimiento.Este: nuevaX++; break;
                case DireccionMovimiento.Oeste: nuevaX--; break;
            }

            var (permitido, eventoBloqueo) = MapaDatabase.EvaluarCasilla(nuevaX, nuevaY, TieneFlag);
            if (!permitido)
            {
                UltimoEventoExploracion = eventoBloqueo;
                return; // no se mueve; sigue en su casilla (y en su UbicacionActual si estaba en una)
            }

            PosicionX = nuevaX;
            PosicionY = nuevaY;
            EstadoActual = EstadoJuego.Explorando;
            UltimoEventoExploracion = null;

            var ubicacion = MapaDatabase.UbicacionEn(PosicionX, PosicionY);
            UbicacionActual = ubicacion;

            if (ubicacion != null)
            {
                UbicacionesDescubiertas.Add(ubicacion.Nombre);
                if (ubicacion.EsCiudad) UltimaCiudadVisitada = ubicacion;
                return; // llegar a un punto no dispara encuentros
            }

            var aviso = MapaDatabase.AvisoAlLlegar(PosicionX, PosicionY, TieneFlag);
            if (aviso != null)
            {
                UltimoEventoExploracion = aviso; // ej. "puente_destruido" al llegar a (-9, 1)
                return;
            }

            LanzarRNGEncuentro();
        }

        // "Entrar" a la ciudad sobre la que se está parado: abre el menú de ciudad. Devuelve false
        // si no hay ubicación acá o si no es una ciudad (cuevas, portal y pueblo: mapas sin hacer).
        public bool EntrarAUbicacionActual()
        {
            if (UbicacionActual == null || !UbicacionActual.EsCiudad) return false;

            EstadoActual = EstadoJuego.EnCiudad;
            return true;
        }

        // "Salir de ciudad": vuelve al menú de viaje, en la misma casilla de la ciudad.
        public void SalirDeCiudad()
        {
            UltimoEventoExploracion = null;
            EstadoActual = EstadoJuego.Explorando;
        }

        // Cada paso por una casilla vacía: probabilidad de combate y de loot según la tabla de
        // monstruos de la zona (bosque / desierto / rift). La zona sin color usa la tabla del bosque
        // con un multiplicador bajo, definido en mapa.json.
        private void LanzarRNGEncuentro()
        {
            if (Prota == null) return;

            UltimoEventoExploracion = null;

            var zona = MapaDatabase.ObtenerZona(PosicionX, PosicionY);
            var tabla = MapaDatabase.ObtenerTablaMonstruos(zona.TablaMonstruos);

            double chanceCombate = tabla.EncuentroPct * zona.MultiplicadorEncuentros;

            if (rng.NextDouble() * 100.0 < chanceCombate)
            {
                GenerarCombateSalvaje(tabla);
            }
            else if (rng.NextDouble() * 100.0 < tabla.LootPct)
            {
                Prota.AgregarObjeto("Hierba Orderia", 1);
                UltimoEventoExploracion = "loot";
            }
        }

        // ==========================================
        // 3. TRANSICIÓN A COMBATE
        // ==========================================

        // Elige cuántos enemigos y cuáles salen leyendo la tabla de la zona (pesos del JSON).
        // El GDD 18.1 da +20% XP al grupo (>1 enemigo); CombatSystem lo aplica solo.
        private void GenerarCombateSalvaje(TablaMonstruosData tabla)
        {
            if (Prota == null) return;

            int cantidadEnemigos = ElegirPorPeso(tabla.TamanoGrupo.Select(g => (g.Cantidad, g.Peso)).ToList(), 1);

            var pesosEnemigos = tabla.Enemigos.Select(e => (e.Id, e.Peso)).ToList();
            var enemigosSalvajes = new List<Enemigo>();

            for (int i = 0; i < cantidadEnemigos; i++)
            {
                string idElegido = ElegirPorPeso(pesosEnemigos, "lobo");
                enemigosSalvajes.Add(Database.CrearDesdeId(idElegido, Prota.Nivel + tabla.BonusNivel));
            }

            CombateActual = new CombatSystem(GrupoActivo, enemigosSalvajes);
            EstadoActual = EstadoJuego.EnCombate;
        }

        // Sorteo ponderado: cada opción sale con probabilidad peso / suma de pesos.
        private T ElegirPorPeso<T>(List<(T Valor, int Peso)> opciones, T porDefecto)
        {
            int total = opciones.Sum(o => o.Peso);
            if (total <= 0) return porDefecto;

            int tirada = rng.Next(0, total);
            int acumulado = 0;

            foreach (var (valor, peso) in opciones)
            {
                acumulado += peso;
                if (tirada < acumulado) return valor;
            }

            return porDefecto;
        }

        public void RevisarFinDeCombate()
        {
            if (CombateActual == null || !CombateActual.CombateTerminado) return;

            bool victoria = CombateActual.Victoria;
            CombateActual = null; // se limpia siempre, gane, huya o pierda el grupo

            if (victoria)
            {
                EstadoActual = EstadoJuego.Explorando;
                return;
            }

            if (Prota != null && Prota.EstaVivo())
            {
                // Fuga exitosa o derrota parcial del grupo con el protagonista vivo: se sigue explorando.
                EstadoActual = EstadoJuego.Explorando;
            }
            else
            {
                EjecutarPenalizacionMuerte();
            }
        }

        private void EjecutarPenalizacionMuerte()
        {
            if (Prota == null) return;

            Prota.Curar(1);
            Prota.GastarOro(50);

            // Regresa a la última ciudad visitada como punto seguro. No hay todavía un sistema
            // real de checkpoints por posada/ciudad (SaveManager es un slot único y no restaura
            // stats en memoria); cuando eso exista, este es el lugar para usar el último punto de
            // guardado real en vez de este fallback.
            var ciudad = UltimaCiudadVisitada ?? MapaDatabase.ObtenerPorId("oeste") ?? MapaDatabase.Ubicaciones[0];
            UbicacionActual = ciudad;
            UltimaCiudadVisitada = ciudad;
            PosicionX = ciudad.X;
            PosicionY = ciudad.Y;
            EstadoActual = EstadoJuego.EnCiudad;
        }
    }
}