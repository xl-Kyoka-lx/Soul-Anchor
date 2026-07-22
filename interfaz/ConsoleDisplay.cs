using System;
using System.Collections.Generic;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives; // Librería de colores y posiciones en SadConsole
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
        TextoHistoria,   // pantalla genérica de texto secuencial (prólogo, intro, reclutamiento, etc.)
        Ciudad,
        Gremio,
        Exploracion      // placeholder hasta que se implemente el mundo/mapa real
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

        // Qué hacer cuando termina la secuencia de texto activa (prólogo, intro, reclutamiento...)
        private Action? alTerminarSecuencia;

        // Nombre del protagonista elegido por el jugador. "Ren" por defecto, editable en DibujarNombrePersonaje.
        private string nombreJugador = "Ren";
        private const int LongitudMaximaNombre = 12;

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

        // Pantalla genérica para cualquier secuencia de texto narrativo (prólogo, intro, reclutamiento...)
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
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_preguntar"), MostrarNoImplementado); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_gremio"), IrAGremio); y++;
                AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "ciudad_salir_mapa"), IrAExploracionPlaceholder);
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

        public void DibujarExploracionPlaceholder()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string nombreCiudad = gameManager.UbicacionActual?.Nombre ?? "Ciudad del Oeste";
            int y = DibujarHeaderJuego(nombreCiudad);

            Surface.Print(2, y, LocalizationManager.Obtener(idioma, "exploracion_no_implementada"), Color.Yellow);
            y += 2;

            AgregarOpcion(2, y, LocalizationManager.Obtener(idioma, "gremio_volver"), VolverACiudadDesdeExploracion);
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

        private void VolverACiudadDesdeExploracion()
        {
            pantallaActual = PantallaUI.Ciudad;
            DibujarCiudad();
        }

        private void IrAExploracionPlaceholder()
        {
            pantallaActual = PantallaUI.Exploracion;
            DibujarExploracionPlaceholder();
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
                case PantallaUI.Exploracion:
                    DibujarExploracionPlaceholder();
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
                case PantallaUI.Exploracion:
                    ProcesarInputNumerico();
                    break;

                case PantallaUI.TextoHistoria:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.Enter) ||
                        GameHost.Instance.Keyboard.IsKeyPressed(Keys.Space))
                    {
                        AvanzarTextoHistoria();
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