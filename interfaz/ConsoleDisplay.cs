using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives; // Librería de colores y posiciones en SadConsole
using SoulAnchor.Datos.Entidades;
using SoulAnchor.Datos.Mundo;
using SoulAnchor.Managers;

namespace SoulAnchor.interfaz
{
    // Estados propios de la pantalla de UI, separados del EstadoJuego del GameManager,
    // porque esto es flujo de presentación/menús, no estado real del mundo del juego.
    public enum PantallaUI
    {
        SeleccionIdioma,
        MenuPrincipal,
        NombrePersonaje,
        PreguntaPrologo,
        TextoHistoria,      // pantalla genérica de texto secuencial (prólogo, intro, reclutamiento, preguntar...)
        Ciudad,
        Gremio,
        SeleccionDestino,   // elegir a qué ciudad viajar (GDD 19.6)
        Exploracion,        // menú de movimiento real (GDD 8.1)
        Combate,            // pantalla de batalla (GDD 2.5)
        SeleccionCategoriaHechizo, // submenú de Combate: Común o Magia de Sangre
        SeleccionObjetivoCombate, // submenú de Combate: elegir a qué enemigo atacar/hechizar
        SeleccionHechizoCombate,  // submenú de Combate: elegir qué hechizo lanzar
        ResultadoCombate,   // pantalla intermedia victoria/derrota/fuga antes de continuar
        Mochila,            // vista de inventario (submenú, accesible desde Exploracion)
        Stats,              // vista de stats del grupo (submenú, accesible desde Exploracion)
        MensajeCombate      // pausa de lectura: mensajes de combate (ataques, multi-kill, huida fallida)
    }

    // Representa una opción de menú numerada y clickeable en la pantalla actual.
    public class OpcionMenu
    {
        public string Texto = "";   // Ya incluye el prefijo numérico, ej: "1. Gremio"
        public int X;
        public int Y;
        public Action? Accion;
    }

    // Heredamos de 'Console' para que esta clase sea una pantalla dibujable en SadConsole
    public class ConsoleDisplay : SadConsole.Console
    {
        private GameManager gameManager;
        private PantallaUI pantallaActual;
        private string idioma = "es"; // "es" o "en", coincide con las claves del JSON

        // Opciones numeradas/clickeables de la pantalla que se está mostrando ahora mismo.
        // El índice en la lista (+1) es el número que el jugador ve y puede presionar en el teclado.
        private List<OpcionMenu> opcionesActuales = new List<OpcionMenu>();

        // Recordamos qué opción tiene el mouse encima para el efecto hover (GDD 15.4)
        private OpcionMenu? opcionConHover = null;

        // Qué hacer cuando termina la secuencia de texto activa (prólogo, intro, reclutamiento, preguntar...)
        private Action? alTerminarSecuencia;

        // Nombre del protagonista elegido por el jugador. "Ren" por defecto, editable en DibujarNombrePersonaje.
        private string nombreJugador = "Ren";
        private const int LongitudMaximaNombre = 12;

        // A qué pantalla volver al presionar "Volver" en Mochila/Stats (se puede llegar a ambas
        // desde Exploracion o desde Combate más adelante, así que no asumimos un origen fijo).
        private PantallaUI pantallaOrigenSubmenu = PantallaUI.Ciudad;

        // Estado del loop de turnos de combate (GDD 2.2, mecánica de Multi-Kill). El orden de
        // turno de los aliados se re-arma al empezar cada ronda a partir de quién sigue vivo.
        private Queue<Personaje> colaTurnosAliados = new Queue<Personaje>();
        private Personaje? actorActual;
        private bool resultadoCombateVictoria;
        private bool resultadoCombateProtaMurio;

        // A qué acción mandar el enemigo elegido en la pantalla SeleccionObjetivoCombate (Atacar
        // o el hechizo que se haya elegido antes). Se guarda como campo porque el redibujado por
        // hover del mouse necesita poder reconstruir la pantalla sin perder este dato.
        private Action<Enemigo>? alSeleccionarObjetivo;

        // Mensajes de combate en pausa (GDD: legibilidad de "quién atacó a quién"). Antes esto se
        // imprimía como texto suelto encima de la pantalla, y cualquier redibujado (ej. el mouse
        // pasando por encima de una opción) lo borraba casi al instante. Ahora es su propia
        // pantalla: no avanza sola, espera Enter o click.
        private List<string> mensajesPendientes = new List<string>();
        private Action? alContinuarMensaje;

        private static readonly Keys[] teclasNumericas =
        {
            Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9
        };

        public ConsoleDisplay(int width, int height, GameManager gm) : base(width, height)
        {
            gameManager = gm;

            UseMouse = true;
            UseKeyboard = true;

            LocalizationManager.Cargar();

            pantallaActual = PantallaUI.SeleccionIdioma;
            DibujarPantallaFirstBoot();
        }

        // ==========================================
        // DIBUJO DE PANTALLAS
        // ==========================================

        public void DibujarPantallaFirstBoot()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(20, 10, LocalizationManager.Obtener(idioma, "idioma_pregunta"), Color.White);

            AgregarOpcion(25, 12, LocalizationManager.Obtener(idioma, "idioma_opcion_es"), () => SeleccionarIdioma("es"));
            AgregarOpcion(25, 13, LocalizationManager.Obtener(idioma, "idioma_opcion_en"), () => SeleccionarIdioma("en"));
        }

        //el Menú Principal es exclusivamente por mouse hasta iniciar/cargar partida
  
        public void DibujarMenuPrincipal()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(20, 5, LocalizationManager.Obtener(idioma, "titulo_juego"), Color.Red);

            AgregarOpcionSinNumero(25, 10, LocalizationManager.Obtener(idioma, "menu_nueva_partida"), IrANombrePersonaje);
            AgregarOpcionSinNumero(25, 12, LocalizationManager.Obtener(idioma, "menu_cargar_partida"), CargarPartidaDesdeMenu);
            AgregarOpcionSinNumero(25, 14, LocalizationManager.Obtener(idioma, "menu_ajustes"), () => { /* TODO: pantalla de ajustes */ });
            AgregarOpcionSinNumero(25, 16, LocalizationManager.Obtener(idioma, "menu_salir"), () => Environment.Exit(0));
        }

        public void DibujarNombrePersonaje()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string prompt = idioma == "en" ? "Enter your character's name:" : "Ingresa el nombre de tu personaje:";
            Surface.Print(15, 8, TextoEncoding.ACP437(prompt), Color.Yellow);

            // Cursor visual simple al final del texto escrito
            Surface.Print(15, 10, TextoEncoding.ACP437(nombreJugador + "_"), Color.White);

            string ayuda = idioma == "en"
                ? "(type to edit, Backspace to delete, Enter to confirm)"
                : "(Presiona Enter para confirmar)";
            Surface.Print(15, 12, TextoEncoding.ACP437(ayuda), Color.Gray);

            AgregarOpcion(15, 14, idioma == "en" ? "Continue" : "Continuar", ConfirmarNombre);
        }

        public void DibujarPreguntaPrologo()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(15, 10, LocalizationManager.Obtener(idioma, "prologo_pregunta"), Color.Yellow);

            AgregarOpcion(25, 12, LocalizationManager.Obtener(idioma, "prologo_si"), () => IniciarPartida(leerPrologo: true));
            AgregarOpcion(25, 13, LocalizationManager.Obtener(idioma, "prologo_no"), () => IniciarPartida(leerPrologo: false));
        }

        // Pantalla genérica para cualquier secuencia de texto narrativo (prólogo, intro, reclutamiento, preguntar...)
        public void DibujarTextoHistoria()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string texto = gameManager.Historia.PaginaActualTexto;

            int y = 4;
            foreach (string linea in EnvolverTexto(texto, 60))
            {
                Surface.Print(5, y, linea, Color.White);
                y++;
            }

            string indicador = idioma == "en" ? "(click or press Enter to continue)" : "(click o presiona Enter para continuar)";
            Surface.Print(5, 22, indicador, Color.Gray);
        }

        // Divide un texto largo en líneas de máximo 'ancho' caracteres, respetando palabras completas
        private static List<string> EnvolverTexto(string texto, int ancho)
        {
            var lineas = new List<string>();
            var palabras = texto.Split(' ');
            var lineaActual = "";

            foreach (var palabra in palabras)
            {
                string candidata = lineaActual.Length == 0 ? palabra : lineaActual + " " + palabra;
                if (candidata.Length > ancho)
                {
                    lineas.Add(lineaActual);
                    lineaActual = palabra;
                }
                else
                {
                    lineaActual = candidata;
                }
            }

            if (lineaActual.Length > 0) lineas.Add(lineaActual);
            return lineas;
        }

        // Header compartido por las pantallas "en partida" (Ciudad, Gremio...), igual al formato del GDD:
        // === SOUL ANCHOR ===
        // Ubicación: X
        // Oro: N
        // Científico (Lvl N): HP X/X | MP X/X
        // Niña (Lvl N): HP X/X | MP X/X
        // Hombre (Lvl N): HP X/X | MP X/X
        // ========================================
        // Devuelve la fila (Y) donde deberían empezar las opciones numeradas.
        private int DibujarHeaderJuego(string ubicacionTexto)
        {
            Surface.Print(2, 1, "=== SOUL ANCHOR ===", Color.Yellow);
            Surface.Print(2, 2, TextoEncoding.ACP437($"Ubicación: {ubicacionTexto}"), Color.White);
            Surface.Print(2, 3, TextoEncoding.ACP437($"Oro: {gameManager.Prota?.Oro ?? 0}"), Color.White);

            int y = 4;
            foreach (var personaje in gameManager.GrupoActivo)
            {
                string linea = $"{personaje.Nombre} (Lvl {personaje.Nivel}): HP {personaje.HpActual}/{personaje.HpMaximo} | MP {personaje.MpActual}/{personaje.MpMaximo}";
                Surface.Print(2, y, TextoEncoding.ACP437(linea), Color.Cyan);
                y++;
            }

            Surface.Print(2, y, new string('=', 40), Color.DarkCyan);
            y += 2;

            return y;
        }

        // Ciudad del Oeste (u otra ciudad). Al inicio del juego (antes de colocar el anuncio en el
        // gremio) solo se puede ir a la Posada o al Gremio, tal como indica el GDD Parte 2.
        public void DibujarCiudad()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string nombreCiudad = gameManager.UbicacionActual?.Nombre ?? "Ciudad del Oeste";
            int y = DibujarHeaderJuego(nombreCiudad);

            if (!gameManager.AnuncioColocado)
            {
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_posada"), IrAPosada); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_gremio"), IrAGremio);
            }
            else
            {
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_sanacion"), MostrarNoImplementado); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_tienda"), MostrarNoImplementado); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_herreria"), MostrarNoImplementado); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_tp"), MostrarNoImplementado); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_posada"), IrAPosada); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_preguntar"), IrAPreguntar); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_gremio"), IrAGremio); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_salir_mapa"), SalirDeCiudad);
            }
        }

        public void DibujarGremio()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string nombreCiudad = gameManager.UbicacionActual?.Nombre ?? "Ciudad del Oeste";
            int y = DibujarHeaderJuego($"Gremio de Aventureros ({nombreCiudad})");

            if (!gameManager.AnuncioColocado)
            {
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "gremio_colocar_anuncio"), ColocarAnuncioYRecluta);
                y++;
            }
            else
            {
                Surface.Print(2, y, LocalizationManager.Obtener(idioma, "gremio_sin_misiones"), Color.Gray);
                y += 2;
                // TODO: sistema real de misiones del gremio (GDD sección 11)
            }

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "gremio_volver"), VolverACiudadDesdeGremio);
        }

        // ---- Reservado para el futuro Teletransporte (GDD 9.4) ----
        // No está conectado a ningún botón todavía (la opción "Teletransporte" del menú de ciudad
        // sigue sin implementarse). Lista las ciudades ya visitadas y salta directo a la elegida.
        public void DibujarSeleccionDestino()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string nombreCiudad = gameManager.UbicacionActual?.Nombre ?? "";
            int y = DibujarHeaderJuego(nombreCiudad);

            Surface.Print(2, y, LocalizationManager.Obtener(idioma, "destino_titulo"), Color.Yellow);
            y += 2;

            var destinos = gameManager.ObtenerDestinosDisponibles();

            if (destinos.Count == 0)
            {
                Surface.Print(2, y, LocalizationManager.Obtener(idioma, "destino_sin_opciones"), Color.Gray);
                y += 2;
            }
            else
            {
                foreach (var destino in destinos)
                {
                    string etiqueta = TextoEncoding.ACP437($"{destino.Nombre} ({destino.X}, {destino.Y})");
                    AgregarOpcion(2, y, etiqueta, () =>
                    {
                        gameManager.TeletransportarA(destino);
                        pantallaActual = PantallaUI.Ciudad;
                        DibujarCiudad();
                    });
                    y++;
                }
                y++;
            }

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "destino_volver"), () => { pantallaActual = PantallaUI.Ciudad; DibujarCiudad(); });
        }

        private void IrAExploracion()
        {
            pantallaActual = PantallaUI.Exploracion;
            DibujarExploracion();
        }

        // "Salir de la ciudad": abre el menú de viaje en la casilla de la ciudad (donde se ofrece
        // volver a "Entrar").
        private void SalirDeCiudad()
        {
            gameManager.SalirDeCiudad();
            IrAExploracion();
        }

        // Menú de viaje (GDD 8.1) sobre el plano 13x13. Avanzar=Norte, Retroceder=Sur,
        // Derecha=Este, Izquierda=Oeste, un paso de 1 casilla cada uno. Si se está parado sobre una
        // ubicación, aparece "Entrar a X" AL FINAL de la lista (así los números 1-7 de las demás
        // opciones no cambian de lugar y apretar "1" repetido no mete al jugador a la ciudad sin querer).
        public void DibujarExploracion()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            int y = DibujarHeaderJuego(DescribirPosicionActual());

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_avanzar"), () => EjecutarMovimiento(DireccionMovimiento.Norte)); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_izquierda"), () => EjecutarMovimiento(DireccionMovimiento.Oeste)); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_derecha"), () => EjecutarMovimiento(DireccionMovimiento.Este)); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_retroceder"), () => EjecutarMovimiento(DireccionMovimiento.Sur)); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_mochila"), IrAMochila); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_stats"), IrAStats); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "exploracion_investigar"), AccionInvestigar); y++;

            var ubicacion = gameManager.UbicacionActual;
            if (ubicacion != null)
            {
                string etiqueta = LocalizationManager.ObtenerFormateado(idioma, "exploracion_entrar", ubicacion.Nombre);
                AgregarOpcion(2, y, etiqueta, EntrarALugar);
            }
        }

        // Header del menú de viaje: coordenadas siempre (útil para ubicarse) y la zona de monstruos
        // si se está en campo abierto, o el nombre del lugar si se está sobre una ubicación.
        private string DescribirPosicionActual()
        {
            int x = gameManager.PosicionX;
            int y = gameManager.PosicionY;

            if (gameManager.UbicacionActual != null)
            {
                return LocalizationManager.ObtenerFormateado(idioma, "exploracion_en_ubicacion",
                    gameManager.UbicacionActual.Nombre, x, y);
            }

            var zona = MapaDatabase.ObtenerZona(x, y);
            string nombreZona = LocalizationManager.Obtener(idioma, "zona_" + zona.Id);
            return LocalizationManager.ObtenerFormateado(idioma, "exploracion_coordenadas", x, y, nombreZona);
        }

        // "Entrar a X": las ciudades abren su menú; cuevas, portal y pueblo todavía no tienen su
        // mapa hecho, así que solo avisan.
        private void EntrarALugar()
        {
            var ubicacion = gameManager.UbicacionActual;
            if (ubicacion == null) return;

            if (gameManager.EntrarAUbicacionActual())
            {
                pantallaActual = PantallaUI.Ciudad;
                DibujarCiudad();
                return;
            }

            string mensaje = LocalizationManager.ObtenerFormateado(idioma, "ubicacion_no_disponible", ubicacion.Nombre);
            MostrarMensajeCombate(new List<string> { mensaje }, DibujarExploracion);
        }

        private void EjecutarMovimiento(DireccionMovimiento direccion)
        {
            gameManager.Mover(direccion);

            if (gameManager.EstadoActual == EstadoJuego.EnCombate)
            {
                IniciarCombate();
                return;
            }

            // Avisos del mapa y del camino. Van en la pantalla de mensaje (Enter/click para seguir),
            // no impresos encima del menú, porque el hover del mouse repinta la pantalla y los borra.
            string? claveMensaje = gameManager.UltimoEventoExploracion switch
            {
                "limite" => "exploracion_limite",
                "rio" => "exploracion_rio",
                "puente_destruido" => "exploracion_puente_destruido",
                "loot" => "exploracion_encontraste_algo",
                _ => null
            };

            if (claveMensaje != null)
            {
                MostrarMensajeCombate(new List<string> { LocalizationManager.Obtener(idioma, claveMensaje) }, DibujarExploracion);
            }
            else
            {
                DibujarExploracion();
            }
        }

        // Placeholder mecánico: no hay todavía contenido de "investigar" definido (lore/objetos
        // ocultos por ubicación). Dejar esto separado del RNG de Avanzar para no pisar ese sistema.
        private void AccionInvestigar()
        {
            Surface.Print(2, 21, LocalizationManager.Obtener(idioma, "exploracion_investigar_resultado"), Color.Gray);
        }

        private void IrAMochila()
        {
            pantallaOrigenSubmenu = pantallaActual;
            pantallaActual = PantallaUI.Mochila;
            DibujarMochila();
        }

        private void IrAStats()
        {
            pantallaOrigenSubmenu = pantallaActual;
            pantallaActual = PantallaUI.Stats;
            DibujarStats();
        }

        public void DibujarMochila()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 1, LocalizationManager.Obtener(idioma, "mochila_titulo"), Color.Yellow);
            int y = 3;

            if (gameManager.Prota == null || gameManager.Prota.Mochila.Count == 0)
            {
                Surface.Print(2, y, LocalizationManager.Obtener(idioma, "mochila_vacia"), Color.Gray);
                y += 2;
            }
            else
            {
                foreach (var entrada in gameManager.Prota.Mochila)
                {
                    // Los ids nuevos (carne, veneno, hueso...) tienen ficha en ItemDatabase y se
                    // muestran con su nombre localizado; los objetos viejos por nombre display
                    // (ej. "Hierba Orderia") simplemente se imprimen tal cual como fallback.
                    string nombre = ItemDatabase.Existe(entrada.Key)
                        ? ItemDatabase.ObtenerTexto(entrada.Key, idioma).Nombre
                        : entrada.Key;

                    Surface.Print(2, y, TextoEncoding.ACP437($"{nombre} x{entrada.Value}"), Color.White);
                    y++;
                }
                y++;
            }

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "destino_volver"), VolverDesdeSubmenu);
        }

        public void DibujarStats()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 1, LocalizationManager.Obtener(idioma, "stats_titulo"), Color.Yellow);
            int y = 3;

            foreach (var personaje in gameManager.GrupoActivo)
            {
                Surface.Print(2, y, TextoEncoding.ACP437($"{personaje.Nombre} - Lvl {personaje.Nivel}"), Color.Cyan);
                y++;

                string linea1 = $"HP {personaje.HpActual}/{personaje.HpMaximo}  MP {personaje.MpActual}/{personaje.MpMaximo}  ATK {personaje.Atk}  DEF {personaje.Def}";
                Surface.Print(4, y, TextoEncoding.ACP437(linea1), Color.White);
                y++;

                string linea2 = $"XP {personaje.XpActual}/{personaje.XpParaSiguienteNivel}  |  " +
                    LocalizationManager.ObtenerFormateado(idioma, "stats_puntos_sin_asignar", personaje.PuntosDisponibles);
                Surface.Print(4, y, TextoEncoding.ACP437(linea2), Color.Gray);
                y++;

                if (personaje.EstadosAlterados.Count > 0)
                {
                    string estados = string.Join(", ", personaje.EstadosAlterados.Select(e => $"{e.Tipo}({e.TurnosRestantes})"));
                    Surface.Print(4, y, TextoEncoding.ACP437(LocalizationManager.ObtenerFormateado(idioma, "stats_estados", estados)), Color.Orange);
                    y++;
                }

                y++;
            }

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "destino_volver"), VolverDesdeSubmenu);
        }

        private void VolverDesdeSubmenu()
        {
            pantallaActual = pantallaOrigenSubmenu;
            RedibujarPantallaActual();
        }

        // ==========================================
        // COMBATE (GDD 2, 2.2 Multi-Kill, 2.5 UI)
        // ==========================================

        // Arma la cola de turno de la ronda a partir de quién sigue vivo y avanza al primer actor.
        // Se llama tanto al entrar en combate como al cerrar cada ronda (después de la fase enemiga).
        private void IniciarCombate()
        {
            pantallaActual = PantallaUI.Combate;
            colaTurnosAliados = new Queue<Personaje>(gameManager.GrupoActivo.Where(p => p.EstaVivo()));
            SiguienteActor();
        }

        private void SiguienteActor()
        {
            if (colaTurnosAliados.Count == 0)
            {
                FaseEnemigos();
                return;
            }

            actorActual = colaTurnosAliados.Dequeue();

            if (!actorActual.EstaVivo())
            {
                SiguienteActor();
                return;
            }

            DibujarCombate();
        }

        public void DibujarCombate()
        {
            // Se fuerza acá adentro (no solo en IniciarCombate) porque este método también se usa
            // como continuación después de MostrarMensajeCombate (ej. tras el mensaje de Multi-Kill,
            // o tras "no lograste huir" tal como llega tras pasar por SiguienteActor). Si no se
            // reseteara acá, la pantalla se veía bien pero pantallaActual seguía en MensajeCombate,
            // y ni el mouse ni el teclado volvían a reconocer los botones — el bug que quedaba
            // "pegado" después de derrotar a un enemigo en un grupo de varios.
            pantallaActual = PantallaUI.Combate;

            Surface.Clear();
            opcionesActuales.Clear();

            var combate = gameManager.CombateActual;
            if (combate == null) return;

            // Reusamos el mismo header que Ciudad/Exploración: esto es lo que faltaba y por eso
            // no se veía el HP/MP de Ren, Sumire y Tetsu durante la batalla.
            int y = DibujarHeaderJuego(LocalizationManager.Obtener(idioma, "combate_ubicacion_header"));

            Surface.Print(2, y, LocalizationManager.ObtenerFormateado(idioma, "combate_turno_de", actorActual?.Nombre ?? ""), Color.White);
            y += 2;

            foreach (var enemigo in combate.Enemigos)
            {
                string estado = enemigo.EstaVivo()
                    ? $"{enemigo.HpActual}/{enemigo.HpMaximo} HP"
                    : LocalizationManager.Obtener(idioma, "combate_derrotado");

                Surface.Print(2, y, TextoEncoding.ACP437($"{enemigo.Nombre}: {estado}"), enemigo.EstaVivo() ? Color.Red : Color.Gray);
                y++;
            }

            y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_atacar"), AccionAtacar); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_mochila"), MostrarNoImplementado); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_hechizo"), AccionHechizo); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_huir"), AccionHuir);
        }

        private void AccionAtacar()
        {
            var combate = gameManager.CombateActual;
            if (combate == null || actorActual == null) return;

            var enemigosVivos = combate.Enemigos.Where(e => e.EstaVivo()).ToList();
            if (enemigosVivos.Count == 0) return;

            if (enemigosVivos.Count == 1)
            {
                ResolverAtaque(enemigosVivos[0]);
            }
            else
            {
                MostrarSeleccionObjetivo(enemigosVivos, ResolverAtaque);
            }
        }

        // Genérico: sirve tanto para Atacar como para Hechizo (y a futuro cualquier acción que
        // necesite "elegir a qué enemigo"). Guarda el callback en un campo para que el redibujado
        // por hover del mouse (RedibujarPantallaActual) pueda reconstruir la misma pantalla sin
        // perder a dónde había que mandar la selección.
        private void MostrarSeleccionObjetivo(List<Enemigo> enemigosVivos, Action<Enemigo> alSeleccionar)
        {
            alSeleccionarObjetivo = alSeleccionar;
            pantallaActual = PantallaUI.SeleccionObjetivoCombate;
            DibujarSeleccionObjetivo(enemigosVivos);
        }

        private void DibujarSeleccionObjetivo(List<Enemigo> enemigosVivos)
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 1, LocalizationManager.Obtener(idioma, "combate_elegir_objetivo"), Color.Yellow);
            int y = 3;

            foreach (var enemigo in enemigosVivos)
            {
                string etiqueta = TextoEncoding.ACP437($"{enemigo.Nombre} ({enemigo.HpActual}/{enemigo.HpMaximo} HP)");
                AgregarOpcion(2, y, etiqueta, () => alSeleccionarObjetivo?.Invoke(enemigo));
                y++;
            }
        }

        // Resuelve un ataque físico y aplica la mecánica de Multi-Kill (GDD 2.2): si el objetivo
        // muere y quedan más enemigos en pie, el mismo actorActual conserva el turno.
        private void ResolverAtaque(Enemigo objetivo)
        {
            var combate = gameManager.CombateActual;
            if (combate == null || actorActual == null) return;

            pantallaActual = PantallaUI.Combate; // por si veníamos de SeleccionObjetivoCombate

            bool mato = combate.EjecutarAtaqueFisico(actorActual, objetivo);
            combate.VerificarEstadoCombate();

            if (combate.CombateTerminado)
            {
                TerminarCombate();
                return;
            }

            bool quedanEnemigos = combate.Enemigos.Any(e => e.EstaVivo());

            if (mato && quedanEnemigos)
            {
                string mensaje = LocalizationManager.ObtenerFormateado(idioma, "combate_multikill", objetivo.Nombre, actorActual.Nombre);
                MostrarMensajeCombate(new List<string> { mensaje }, DibujarCombate);
            }
            else
            {
                SiguienteActor();
            }
        }

        // ---- Hechizo (GDD 4.1 común / 2.4 sangre) ----

        // Constantes de Magia de Sangre: mismos hechizos que el jugador ya conoce en común, pero
        // el doble de costo y el doble de efecto. Quedan como constantes únicas a propósito, para
        // poder subirlas a x3 después sin tocar el resto de la lógica.
        private const int MultiplicadorCostoSangre = 2;
        private const float MultiplicadorEfectoSangre = 2f;
        private const float PorcentajeHpSumireSinMana = 0.8f;

        private bool hechizoModoSangre;

        private void AccionHechizo()
        {
            if (actorActual == null) return;

            if (actorActual.HechizosConocidos.Count == 0)
            {
                string mensaje = LocalizationManager.Obtener(idioma, "combate_sin_hechizos");
                MostrarMensajeCombate(new List<string> { mensaje }, DibujarCombate);
                return;
            }

            pantallaActual = PantallaUI.SeleccionCategoriaHechizo;
            DibujarSeleccionCategoriaHechizo();
        }

        public void DibujarSeleccionCategoriaHechizo()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 1, LocalizationManager.Obtener(idioma, "combate_elegir_categoria_hechizo"), Color.Yellow);
            int y = 3;

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_categoria_comun"), () => IrASeleccionHechizo(esSangre: false)); y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "combate_categoria_sangre"), () => IrASeleccionHechizo(esSangre: true)); y++;
            y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "destino_volver"), DibujarCombate);
        }

        private void IrASeleccionHechizo(bool esSangre)
        {
            hechizoModoSangre = esSangre;
            pantallaActual = PantallaUI.SeleccionHechizoCombate;
            DibujarSeleccionHechizo();
        }

        public void DibujarSeleccionHechizo()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 1, LocalizationManager.Obtener(idioma, "combate_elegir_hechizo"), Color.Yellow);
            int y = 3;

            if (actorActual != null)
            {
                foreach (var idHechizo in actorActual.HechizosConocidos)
                {
                    var datos = HechizoDatabase.Obtener(idHechizo);
                    var (nombre, _) = HechizoDatabase.ObtenerTexto(idHechizo, idioma);

                    string etiqueta = hechizoModoSangre
                        ? TextoEncoding.ACP437($"{nombre} [{LocalizationManager.Obtener(idioma, "combate_categoria_sangre")}] ({DescribirCostoSangre(datos)})")
                        : TextoEncoding.ACP437($"{nombre} ({datos.CostoMp} MP)");

                    AgregarOpcion(2, y, etiqueta, () => SeleccionarHechizo(datos));
                    y++;
                }
            }

            y++;
            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "destino_volver"), () =>
            {
                pantallaActual = PantallaUI.SeleccionCategoriaHechizo;
                DibujarSeleccionCategoriaHechizo();
            });
        }

        // Texto de costo mostrado en la lista ANTES de lanzar, para que el jugador sepa a qué se
        // arriesga. Para Sumire es condicional: si no le alcanza el MP en este momento, ya avisa
        // que va a pagar en HP.
        private string DescribirCostoSangre(HechizoData hechizo)
        {
            if (actorActual == null) return "";

            int costoMpDoble = hechizo.CostoMp * MultiplicadorCostoSangre;

            if (actorActual.EsMagoDeSangre)
            {
                if (actorActual.MpActual >= costoMpDoble)
                {
                    return $"{costoMpDoble} MP";
                }
                return LocalizationManager.Obtener(idioma, "combate_costo_sangre_hp_sumire");
            }

            return $"{costoMpDoble} HP";
        }

        private void SeleccionarHechizo(HechizoData hechizo)
        {
            var combate = gameManager.CombateActual;
            if (combate == null || actorActual == null) return;

            if (hechizoModoSangre)
            {
                // Magia de Sangre no bloquea por costo insuficiente: el riesgo de quedar fuera de
                // combate es justamente el punto (GDD 2.4). Se resuelve directo a elegir objetivo.
                var enemigosVivosSangre = combate.Enemigos.Where(e => e.EstaVivo()).ToList();
                if (enemigosVivosSangre.Count == 0) return;

                if (enemigosVivosSangre.Count == 1)
                {
                    ResolverHechizoSangre(hechizo, enemigosVivosSangre[0]);
                }
                else
                {
                    MostrarSeleccionObjetivo(enemigosVivosSangre, enemigo => ResolverHechizoSangre(hechizo, enemigo));
                }
                return;
            }

            if (actorActual.MpActual < hechizo.CostoMp)
            {
                string mensaje = LocalizationManager.Obtener(idioma, "combate_mp_insuficiente");
                MostrarMensajeCombate(new List<string> { mensaje }, DibujarSeleccionHechizo);
                return;
            }

            var enemigosVivos = combate.Enemigos.Where(e => e.EstaVivo()).ToList();
            if (enemigosVivos.Count == 0) return;

            if (enemigosVivos.Count == 1)
            {
                ResolverHechizo(hechizo, enemigosVivos[0]);
            }
            else
            {
                MostrarSeleccionObjetivo(enemigosVivos, enemigo => ResolverHechizo(hechizo, enemigo));
            }
        }

        // Mismo flujo que ResolverAtaque (incluido Multi-Kill), pero pasando por
        // CombatSystem.EjecutarMagia: aplica penalización de Tetsu, debilidad elemental del
        // enemigo y el efecto de estado extra del hechizo (ej. quemadura) si corresponde.
        private void ResolverHechizo(HechizoData hechizo, Enemigo objetivo)
        {
            var combate = gameManager.CombateActual;
            if (combate == null || actorActual == null) return;

            pantallaActual = PantallaUI.Combate;

            int dano = hechizo.DanoBase + (int)(hechizo.DanoPorNivel * actorActual.Nivel);
            var efecto = hechizo.EfectoExtra;

            bool mato = combate.EjecutarMagia(
                actorActual, objetivo, dano, hechizo.CostoMp, hechizo.Elemento,
                efecto?.Status, efecto?.ChancePct ?? 0, efecto?.DuracionTurnos ?? 0, efecto?.DanoPorTurno ?? 0);

            combate.VerificarEstadoCombate();

            if (combate.CombateTerminado)
            {
                TerminarCombate();
                return;
            }

            bool quedanEnemigos = combate.Enemigos.Any(e => e.EstaVivo());

            if (mato && quedanEnemigos)
            {
                string mensaje = LocalizationManager.ObtenerFormateado(idioma, "combate_multikill", objetivo.Nombre, actorActual.Nombre);
                MostrarMensajeCombate(new List<string> { mensaje }, DibujarCombate);
            }
            else
            {
                SiguienteActor();
            }
        }

        // Magia de Sangre (GDD 2.4): mismo hechizo que la versión común, pero paga costo doble en
        // HP (o en MP si es Sumire, salvo que no le alcance — ahí paga 80% de su HP actual) y el
        // efecto sale multiplicado. Nunca se bloquea por falta de recurso: si el costo en HP supera
        // lo disponible, el hechizo se lanza igual y el personaje queda en 0 HP (fuera de combate).
        private void ResolverHechizoSangre(HechizoData hechizo, Enemigo objetivo)
        {
            var combate = gameManager.CombateActual;
            if (combate == null || actorActual == null) return;

            pantallaActual = PantallaUI.Combate;

            int costoBase = hechizo.CostoMp * MultiplicadorCostoSangre;
            var mensajes = new List<string>();

            if (actorActual.EsMagoDeSangre)
            {
                if (actorActual.MpActual >= costoBase)
                {
                    actorActual.ConsumirMp(costoBase);
                    mensajes.Add(LocalizationManager.ObtenerFormateado(idioma, "combate_sangre_costo_mp", actorActual.Nombre, costoBase));
                }
                else
                {
                    int costoHp = (int)(actorActual.HpActual * PorcentajeHpSumireSinMana);
                    actorActual.PagarCostoSangre(costoHp);
                    mensajes.Add(LocalizationManager.ObtenerFormateado(idioma, "combate_sangre_costo_hp", actorActual.Nombre, costoHp));
                }
            }
            else
            {
                actorActual.PagarCostoSangre(costoBase);
                mensajes.Add(LocalizationManager.ObtenerFormateado(idioma, "combate_sangre_costo_hp", actorActual.Nombre, costoBase));
            }

            bool lanzadorSigueVivo = actorActual.EstaVivo();
            if (!lanzadorSigueVivo)
            {
                mensajes.Add(LocalizationManager.ObtenerFormateado(idioma, "combate_sangre_colapso", actorActual.Nombre));
            }

            int dano = (int)((hechizo.DanoBase + (hechizo.DanoPorNivel * actorActual.Nivel)) * MultiplicadorEfectoSangre);
            var efecto = hechizo.EfectoExtra;

            // costoMp=0 porque el costo ya se cobró manualmente arriba (en MP o en HP según el caso).
            bool mato = combate.EjecutarMagia(
                actorActual, objetivo, dano, 0, hechizo.Elemento,
                efecto?.Status, efecto?.ChancePct ?? 0, efecto?.DuracionTurnos ?? 0, efecto?.DanoPorTurno ?? 0);

            combate.VerificarEstadoCombate();

            MostrarMensajeCombate(mensajes, () =>
            {
                if (combate.CombateTerminado)
                {
                    TerminarCombate();
                    return;
                }

                bool quedanEnemigos = combate.Enemigos.Any(e => e.EstaVivo());

                if (!lanzadorSigueVivo)
                {
                    // Sin Multi-Kill posible para alguien que quedó inconsciente con su propio hechizo.
                    SiguienteActor();
                }
                else if (mato && quedanEnemigos)
                {
                    string mensajeKill = LocalizationManager.ObtenerFormateado(idioma, "combate_multikill", objetivo.Nombre, actorActual.Nombre);
                    MostrarMensajeCombate(new List<string> { mensajeKill }, DibujarCombate);
                }
                else
                {
                    SiguienteActor();
                }
            });
        }

        private void AccionHuir()
        {
            var combate = gameManager.CombateActual;
            if (combate == null) return;

            bool escapo = combate.IntentarHuir();

            if (escapo)
            {
                TerminarCombate();
            }
            else
            {
                string mensaje = LocalizationManager.Obtener(idioma, "combate_huir_fallo");
                MostrarMensajeCombate(new List<string> { mensaje }, SiguienteActor);
            }
        }

        // Fase enemiga: cada enemigo vivo elige objetivo (Enemigo.ElegirObjetivo, pesos por aggro)
        // y ataca; al cerrar la ronda se tickean los estados alterados (veneno, quemadura...) antes
        // de revisar victoria/derrota y, si el combate sigue, arrancar la ronda siguiente.
        private void FaseEnemigos()
        {
            var combate = gameManager.CombateActual;
            if (combate == null) return;

            var aliadosVivos = gameManager.GrupoActivo.Where(a => a.EstaVivo()).ToList();
            var mensajesAtaque = new List<string>();

            foreach (var enemigo in combate.Enemigos.Where(e => e.EstaVivo()))
            {
                var objetivo = enemigo.ElegirObjetivo(aliadosVivos);
                if (objetivo != null && objetivo.EstaVivo())
                {
                    // Medimos el daño real por diferencia de HP (antes/después), en vez de
                    // recalcular la fórmula acá, para que el mensaje siempre coincida exacto
                    // con lo que CombatSystem aplicó (mitigación de DEF incluida).
                    int hpAntes = objetivo.HpActual;
                    combate.EjecutarAtaqueEnemigo(enemigo, objetivo);
                    int danio = hpAntes - objetivo.HpActual;

                    mensajesAtaque.Add(LocalizationManager.ObtenerFormateado(idioma, "combate_enemigo_ataca", enemigo.Nombre, objetivo.Nombre, danio));
                }
            }

            combate.ProcesarEstadosDeRonda();
            combate.VerificarEstadoCombate();

            // Mostramos los mensajes de ataque ANTES de decidir a dónde ir, así el jugador
            // siempre ve qué pasó (incluso si ese último golpe terminó el combate) antes de saltar
            // a la ronda siguiente o a la pantalla de victoria/derrota.
            MostrarMensajeCombate(mensajesAtaque, () =>
            {
                if (combate.CombateTerminado)
                {
                    TerminarCombate();
                }
                else
                {
                    IniciarCombate(); // nueva ronda
                }
            });
        }

        private void TerminarCombate()
        {
            resultadoCombateVictoria = gameManager.CombateActual?.Victoria ?? false;

            // OJO: hay que leer esto ANTES de RevisarFinDeCombate. Si Prota murió, ese método lo
            // revive con 1 HP como parte de la penalización de muerte (GDD 12); si chequeáramos
            // EstaVivo() después, la derrota nunca se detectaría porque ya estaría "vivo" de nuevo.
            resultadoCombateProtaMurio = !resultadoCombateVictoria && !(gameManager.Prota?.EstaVivo() ?? true);

            gameManager.RevisarFinDeCombate();

            pantallaActual = PantallaUI.ResultadoCombate;
            DibujarResultadoCombate();
        }

        public void DibujarResultadoCombate()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            if (resultadoCombateVictoria)
            {
                Surface.Print(2, 2, LocalizationManager.Obtener(idioma, "combate_victoria"), Color.Green);
                Surface.Print(2, 4, TextoEncoding.ACP437($"Oro: {gameManager.Prota?.Oro ?? 0}"), Color.White);
            }
            else if (resultadoCombateProtaMurio)
            {
                Surface.Print(2, 2, LocalizationManager.Obtener(idioma, "combate_derrota"), Color.Red);
            }
            else
            {
                Surface.Print(2, 2, LocalizationManager.Obtener(idioma, "combate_escapaste"), Color.Yellow);
            }

            AgregarOpcion(2, 8, LocalizationManager.Obtener(idioma, "combate_continuar"), ContinuarDespuesDeCombate);
        }

        private void ContinuarDespuesDeCombate()
        {
            if (gameManager.EstadoActual == EstadoJuego.EnCiudad)
            {
                pantallaActual = PantallaUI.Ciudad;
                DibujarCiudad();
            }
            else
            {
                pantallaActual = PantallaUI.Exploracion;
                DibujarExploracion();
            }
        }

        // Muestra uno o más mensajes de combate y PAUSA ahí hasta que el jugador presione Enter o
        // haga click. 'continuar' es lo que debía pasar después (ej. arrancar la ronda siguiente,
        // o devolver el turno al mismo actor tras un Multi-Kill).
        private void MostrarMensajeCombate(List<string> mensajes, Action continuar)
        {
            if (mensajes.Count == 0)
            {
                continuar();
                return;
            }

            mensajesPendientes = mensajes;
            alContinuarMensaje = continuar;
            pantallaActual = PantallaUI.MensajeCombate;
            DibujarMensajeCombate();
        }

        public void DibujarMensajeCombate()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            int y = gameManager.CombateActual != null
                ? DibujarHeaderJuego(LocalizationManager.Obtener(idioma, "combate_ubicacion_header"))
                : 2;

            y++;
            foreach (var mensaje in mensajesPendientes)
            {
                Surface.Print(2, y, TextoEncoding.ACP437(mensaje), Color.Orange);
                y++;
            }

            string indicador = idioma == "en" ? "(click or press Enter to continue)" : "(click o presiona Enter para continuar)";
            Surface.Print(2, 22, TextoEncoding.ACP437(indicador), Color.Gray);
        }

        private void ContinuarMensajeCombate()
        {
            var continuar = alContinuarMensaje;
            alContinuarMensaje = null;
            mensajesPendientes = new List<string>();
            continuar?.Invoke();
        }

        // Muestra feedback visible cuando se presiona (mouse o teclado) una opción que aún no está implementada.
        private void MostrarNoImplementado()
        {
            Surface.Print(2, 21, LocalizationManager.Obtener(idioma, "opcion_no_implementada"), Color.Red);
        }

        // Registra una opción numerada (1., 2., 3...) clickeable y accesible por teclado.
        private void AgregarOpcion(int x, int y, string texto, Action accion)
        {
            int numero = opcionesActuales.Count + 1;
            string textoConNumero = $"{numero}. {texto}";
            Surface.Print(x, y, textoConNumero, Color.White);
            opcionesActuales.Add(new OpcionMenu { Texto = textoConNumero, X = x, Y = y, Accion = accion });
        }

        // Igual que AgregarOpcion, pero sin número, para el Menú Principal (mouse-only por GDD 15.2)
        private void AgregarOpcionSinNumero(int x, int y, string texto, Action accion)
        {
            Surface.Print(x, y, texto, Color.White);
            opcionesActuales.Add(new OpcionMenu { Texto = texto, X = x, Y = y, Accion = accion });
        }

        // ==========================================
        // TRANSICIONES DE ESTADO
        // ==========================================

        private void SeleccionarIdioma(string idiomaElegido)
        {
            idioma = idiomaElegido;
            pantallaActual = PantallaUI.MenuPrincipal;
            DibujarMenuPrincipal();
        }

        private void IrANombrePersonaje()
        {
            nombreJugador = "Ren";
            pantallaActual = PantallaUI.NombrePersonaje;
            DibujarNombrePersonaje();
        }

        private void ConfirmarNombre()
        {
            if (string.IsNullOrWhiteSpace(nombreJugador))
            {
                nombreJugador = "Ren";
            }

            IrAPreguntaPrologo();
        }

        private void IrAPreguntaPrologo()
        {
            pantallaActual = PantallaUI.PreguntaPrologo;
            DibujarPreguntaPrologo();
        }

        private void CargarPartidaDesdeMenu()
        {
            gameManager.CargarPartida();

            if (gameManager.Prota == null)
            {
                // El sistema de carga real todavía no restaura al jugador; avisamos y nos quedamos en el menú.
                DibujarMenuPrincipal();
                Surface.Print(5, 19, TextoEncoding.ACP437(idioma == "en"
                    ? "(No working save system yet)"
                    : "(Aún no hay sistema de carga funcional)"), Color.Red);
                return;
            }

            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        private void IniciarPartida(bool leerPrologo)
        {
            gameManager.IniciarNuevaPartida(nombreJugador);

            if (leerPrologo)
            {
                MostrarSecuencia("Prologo_localizacion.json", DespuesDelPrologo);
            }
            else
            {
                DespuesDelPrologo();
            }
        }

        private void DespuesDelPrologo()
        {
            MostrarSecuencia("Intro_localizacion.json", DespuesDeIntro);
        }

        private void DespuesDeIntro()
        {
            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        private void IrAPosada()
        {
            if (gameManager.Prota != null)
            {
                var datos = new DatosGuardado
                {
                    UbicacionX = gameManager.PosicionX,
                    UbicacionY = gameManager.PosicionY,
                    Oro = gameManager.Prota.Oro,
                    Mochila = gameManager.Prota.Mochila,
                    NivelJugador = gameManager.Prota.Nivel,
                    HpJugador = gameManager.Prota.HpActual,
                    MpJugador = gameManager.Prota.MpActual,
                    AtkJugador = gameManager.Prota.Atk,
                    DefJugador = gameManager.Prota.Def,
                    PuntosDisponibles = gameManager.Prota.PuntosDisponibles
                };
                SaveManager.GuardarPartida(datos);
            }

            DibujarCiudad();
            Surface.Print(2, 21, LocalizationManager.Obtener(idioma, "guardado_confirmacion"), Color.Green);
        }

        private void IrAGremio()
        {
            pantallaActual = PantallaUI.Gremio;
            DibujarGremio();
        }

        private void VolverACiudadDesdeGremio()
        {
            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        private void ColocarAnuncioYRecluta()
        {
            MostrarSecuencia("Reclutamiento_localizacion.json", DespuesDeReclutamiento);
        }

        private void DespuesDeReclutamiento()
        {
            gameManager.UnirCompanieros();
            gameManager.ColocarAnuncio();
            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        // Opción "Preguntar" (GDD 9.6): le pide al PreguntaManager cuál diálogo aplica según el
        // estado actual del juego (ubicación, progreso del gremio, flags de historia futuros...) y
        // lo muestra reutilizando la misma pantalla de texto secuencial que prólogo/intro/reclutamiento.
        private void IrAPreguntar()
        {
            string archivoDialogo = gameManager.Preguntas.ObtenerDialogoActivo(gameManager);
            MostrarSecuencia(archivoDialogo, VolverACiudadDesdePreguntar);
        }

        private void VolverACiudadDesdePreguntar()
        {
            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        // Carga y muestra una secuencia de texto; al terminar (última página), ejecuta 'alTerminar'.
        private void MostrarSecuencia(string nombreArchivo, Action alTerminar)
        {
            gameManager.Historia.IniciarSecuencia(nombreArchivo, idioma);
            alTerminarSecuencia = alTerminar;
            pantallaActual = PantallaUI.TextoHistoria;
            DibujarTextoHistoria();
        }

        private void AvanzarTextoHistoria()
        {
            gameManager.Historia.AvanzarPagina();

            if (gameManager.Historia.SecuenciaTerminada)
            {
                var callback = alTerminarSecuencia;
                alTerminarSecuencia = null;
                callback?.Invoke();
            }
            else
            {
                DibujarTextoHistoria();
            }
        }

        // ==========================================
        // LÓGICA DE MOUSE
        // ==========================================

        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            base.ProcessMouse(state);

            if (pantallaActual == PantallaUI.TextoHistoria)
            {
                if (state.IsOnScreenObject && state.Mouse.LeftClicked)
                {
                    AvanzarTextoHistoria();
                }
                return true;
            }

            if (pantallaActual == PantallaUI.MensajeCombate)
            {
                if (state.IsOnScreenObject && state.Mouse.LeftClicked)
                {
                    ContinuarMensajeCombate();
                }
                return true;
            }

            if (!state.IsOnScreenObject)
            {
                if (opcionConHover != null)
                {
                    // El mouse salió de toda opción; quitamos el hover visual
                    RedibujarPantallaActual();
                    opcionConHover = null;
                }
                return false;
            }

            Point celda = state.CellPosition;
            OpcionMenu? opcionBajoElMouse = null;

            foreach (var opcion in opcionesActuales)
            {
                if (celda.Y == opcion.Y && celda.X >= opcion.X && celda.X < opcion.X + opcion.Texto.Length)
                {
                    opcionBajoElMouse = opcion;
                    break;
                }
            }

            // Efecto hover (GDD 15.4): oscurece el texto de la opción bajo el cursor
            if (opcionBajoElMouse != opcionConHover)
            {
                RedibujarPantallaActual();

                if (opcionBajoElMouse != null)
                {
                    Surface.Print(opcionBajoElMouse.X, opcionBajoElMouse.Y, opcionBajoElMouse.Texto, Color.Gray);
                }

                opcionConHover = opcionBajoElMouse;
            }

            if (opcionBajoElMouse != null && state.Mouse.LeftClicked)
            {
                opcionBajoElMouse.Accion?.Invoke();
            }

            return true;
        }

        private void RedibujarPantallaActual()
        {
            switch (pantallaActual)
            {
                case PantallaUI.SeleccionIdioma:
                    DibujarPantallaFirstBoot();
                    break;
                case PantallaUI.MenuPrincipal:
                    DibujarMenuPrincipal();
                    break;
                case PantallaUI.NombrePersonaje:
                    DibujarNombrePersonaje();
                    break;
                case PantallaUI.PreguntaPrologo:
                    DibujarPreguntaPrologo();
                    break;
                case PantallaUI.TextoHistoria:
                    DibujarTextoHistoria();
                    break;
                case PantallaUI.Ciudad:
                    DibujarCiudad();
                    break;
                case PantallaUI.Gremio:
                    DibujarGremio();
                    break;
                case PantallaUI.SeleccionDestino:
                    DibujarSeleccionDestino();
                    break;
                case PantallaUI.Exploracion:
                    DibujarExploracion();
                    break;
                case PantallaUI.Combate:
                    DibujarCombate();
                    break;
                case PantallaUI.SeleccionObjetivoCombate:
                    // No guardamos la lista de enemigos vivos por separado; regenerarla acá es
                    // barato y evita duplicar estado. Si no queda ninguno (no debería pasar antes
                    // de resolver el combate), simplemente no repinta nada.
                    var vivosParaRedibujo = gameManager.CombateActual?.Enemigos.Where(e => e.EstaVivo()).ToList();
                    if (vivosParaRedibujo != null && vivosParaRedibujo.Count > 0)
                    {
                        DibujarSeleccionObjetivo(vivosParaRedibujo);
                    }
                    break;
                case PantallaUI.SeleccionHechizoCombate:
                    DibujarSeleccionHechizo();
                    break;
                case PantallaUI.SeleccionCategoriaHechizo:
                    DibujarSeleccionCategoriaHechizo();
                    break;
                case PantallaUI.ResultadoCombate:
                    DibujarResultadoCombate();
                    break;
                case PantallaUI.Mochila:
                    DibujarMochila();
                    break;
                case PantallaUI.Stats:
                    DibujarStats();
                    break;
                case PantallaUI.MensajeCombate:
                    DibujarMensajeCombate();
                    break;
            }
        }

        // ==========================================
        // LÓGICA DE TECLADO (input híbrido, GDD 15.1)
        // ==========================================

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            switch (pantallaActual)
            {
                case PantallaUI.SeleccionIdioma:
                case PantallaUI.PreguntaPrologo:
                case PantallaUI.Ciudad:
                case PantallaUI.Gremio:
                case PantallaUI.SeleccionDestino:
                case PantallaUI.Exploracion:
                case PantallaUI.Combate:
                case PantallaUI.SeleccionObjetivoCombate:
                case PantallaUI.SeleccionHechizoCombate:
                case PantallaUI.SeleccionCategoriaHechizo:
                case PantallaUI.ResultadoCombate:
                case PantallaUI.Mochila:
                case PantallaUI.Stats:
                    ProcesarInputNumerico();
                    break;

                case PantallaUI.TextoHistoria:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.Enter) ||
                        GameHost.Instance.Keyboard.IsKeyPressed(Keys.Space))
                    {
                        AvanzarTextoHistoria();
                    }
                    break;

                case PantallaUI.MensajeCombate:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.Enter) ||
                        GameHost.Instance.Keyboard.IsKeyPressed(Keys.Space))
                    {
                        ContinuarMensajeCombate();
                    }
                    break;

                case PantallaUI.MenuPrincipal:
                    // GDD 15.2: el Menú Principal es exclusivamente por mouse hasta iniciar/cargar partida.
                    break;

                case PantallaUI.NombrePersonaje:
                    ProcesarInputTexto();
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.Enter))
                    {
                        ConfirmarNombre();
                    }
                    break;
            }
        }

        // Revisa si se presionó una tecla numérica (1-9) y ejecuta la opción correspondiente,
        // igual que si el jugador le hubiera hecho click. Funciona para cualquier pantalla numerada.
        private void ProcesarInputNumerico()
        {
            for (int i = 0; i < teclasNumericas.Length; i++)
            {
                if (GameHost.Instance.Keyboard.IsKeyPressed(teclasNumericas[i]))
                {
                    if (i < opcionesActuales.Count)
                    {
                        opcionesActuales[i].Accion?.Invoke();
                    }
                    return; // solo procesamos una tecla numérica por frame
                }
            }
        }

        // Captura texto libre para el campo de nombre: letras/números se agregan, Backspace borra.
        // Enter se maneja aparte (confirma el nombre), no como texto.
        private void ProcesarInputTexto()
        {
            bool cambio = false;

            foreach (var tecla in GameHost.Instance.Keyboard.KeysPressed)
            {
                if (tecla.Key == Keys.Back)
                {
                    if (nombreJugador.Length > 0)
                    {
                        nombreJugador = nombreJugador.Substring(0, nombreJugador.Length - 1);
                        cambio = true;
                    }
                }
                else if (tecla.Character != 0 && !char.IsControl(tecla.Character))
                {
                    if (nombreJugador.Length < LongitudMaximaNombre)
                    {
                        nombreJugador += tecla.Character;
                        cambio = true;
                    }
                }
            }

            if (cambio)
            {
                DibujarNombrePersonaje();
            }
        }
    }
}