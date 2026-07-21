using System;
using System.Collections.Generic;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives; // Librería de colores y posiciones en SadConsole
using SoulAnchor.Managers;

namespace SoulAnchor.interfaz
{
    // Estados propios de la pantalla de UI (onboarding), separados del EstadoJuego del GameManager,
    // porque esto es flujo de presentación, no estado real del mundo del juego.
    public enum PantallaUI
    {
        SeleccionIdioma,
        MenuPrincipal,
        PreguntaPrologo,
        Prologo,
        Juego
    }

    // Representa una opción de menú clickeable/seleccionable en la pantalla actual.
    public class OpcionMenu
    {
        public string Texto = "";
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

        // Opciones clickeables/seleccionables de la pantalla que se está mostrando ahora mismo
        private List<OpcionMenu> opcionesActuales = new List<OpcionMenu>();

        // Recordamos qué opción tiene el mouse encima para el efecto hover (GDD 15.4)
        private OpcionMenu? opcionConHover = null;

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

        public void DibujarMenuPrincipal()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(20, 5, LocalizationManager.Obtener(idioma, "titulo_juego"), Color.Red);

            AgregarOpcion(25, 10, LocalizationManager.Obtener(idioma, "menu_nueva_partida"), IrAPreguntaPrologo);
            AgregarOpcion(25, 12, LocalizationManager.Obtener(idioma, "menu_cargar_partida"), CargarPartidaDesdeMenu);
            AgregarOpcion(25, 14, LocalizationManager.Obtener(idioma, "menu_ajustes"), () => { /* TODO: pantalla de ajustes */ });
            AgregarOpcion(25, 16, LocalizationManager.Obtener(idioma, "menu_salir"), () => Environment.Exit(0));
        }

        public void DibujarPreguntaPrologo()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(15, 10, LocalizationManager.Obtener(idioma, "prologo_pregunta"), Color.Yellow);

            AgregarOpcion(25, 12, LocalizationManager.Obtener(idioma, "prologo_si"), () => IniciarPartida(leerPrologo: true));
            AgregarOpcion(25, 13, LocalizationManager.Obtener(idioma, "prologo_no"), () => IniciarPartida(leerPrologo: false));
        }

        public void DibujarPrologo()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            string texto = gameManager.Historia.PaginaActualTexto;

            // Imprimimos el texto de la página envuelto a 60 columnas para que no se corte
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

        public void DibujarPantallaJuegoIniciado()
        {
            Surface.Clear();
            opcionesActuales.Clear();

            Surface.Print(2, 2, LocalizationManager.Obtener(idioma, "juego_iniciado_titulo"), Color.Green);
            Surface.Print(2, 4, LocalizationManager.ObtenerFormateado(
                idioma,
                "juego_iniciado_bienvenida",
                gameManager.Prota!.Nombre,
                gameManager.UbicacionActual!.Nombre));
        }

        // Helper para registrar una opción de menú: la imprime y la deja lista para detectar click/hover
        private void AgregarOpcion(int x, int y, string texto, Action accion)
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

        private void IrAPreguntaPrologo()
        {
            pantallaActual = PantallaUI.PreguntaPrologo;
            DibujarPreguntaPrologo();
        }

        private void CargarPartidaDesdeMenu()
        {
            gameManager.CargarPartida();
            pantallaActual = PantallaUI.Juego;
            // Aquí luego dibujaremos la pantalla de exploración/ciudad real en vez de este placeholder
            DibujarPantallaJuegoIniciado();
        }

        private void IniciarPartida(bool leerPrologo)
        {
            gameManager.IniciarNuevaPartida("Ren");

            if (leerPrologo)
            {
                gameManager.Historia.IniciarPrologo(idioma);
                pantallaActual = PantallaUI.Prologo;
                DibujarPrologo();
            }
            else
            {
                pantallaActual = PantallaUI.Juego;
                DibujarPantallaJuegoIniciado();
            }
        }

        private void AvanzarPrologo()
        {
            gameManager.Historia.AvanzarPagina();

            if (gameManager.Historia.PrologoTerminado)
            {
                pantallaActual = PantallaUI.Juego;
                DibujarPantallaJuegoIniciado();
            }
            else
            {
                DibujarPrologo();
            }
        }

        // ==========================================
        // LÓGICA DE MOUSE
        // ==========================================

        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            base.ProcessMouse(state);

            if (pantallaActual == PantallaUI.Prologo)
            {
                if (state.IsOnScreenObject && state.Mouse.LeftClicked)
                {
                    AvanzarPrologo();
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
                case PantallaUI.PreguntaPrologo:
                    DibujarPreguntaPrologo();
                    break;
                case PantallaUI.Juego:
                    // La pantalla de juego se maneja aparte una vez esté implementada la exploración
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
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.D1))
                    {
                        SeleccionarIdioma("es");
                    }
                    else if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.D2))
                    {
                        SeleccionarIdioma("en");
                    }
                    break;

                case PantallaUI.MenuPrincipal:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.N))
                    {
                        IrAPreguntaPrologo();
                    }
                    break;

                case PantallaUI.PreguntaPrologo:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.D1))
                    {
                        IniciarPartida(leerPrologo: true);
                    }
                    else if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.D2))
                    {
                        IniciarPartida(leerPrologo: false);
                    }
                    break;

                case PantallaUI.Prologo:
                    if (GameHost.Instance.Keyboard.IsKeyPressed(Keys.Enter) ||
                        GameHost.Instance.Keyboard.IsKeyPressed(Keys.Space))
                    {
                        AvanzarPrologo();
                    }
                    break;

                case PantallaUI.Juego:
                    // Aquí entrará luego el input de exploración (WASD, menús numéricos, etc.)
                    break;
            }
        }
    }
}