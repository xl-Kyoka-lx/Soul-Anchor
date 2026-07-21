using System;
using SadConsole;
using SadRogue.Primitives; // Librería de colores y posiciones en SadConsole
using SoulAnchor.Managers;

namespace SoulAnchor.interfaz
{


    // Heredamos de 'Console' para que esta clase sea una pantalla dibujable en SadConsole
    public class ConsoleDisplay : SadConsole.Console
    {
        private GameManager gameManager;
        private bool idiomaSeleccionado = false;
        private string idioma = "ES";

        public ConsoleDisplay(int width, int height, GameManager gm) : base(width, height)
        {
            gameManager = gm;
            
            UseMouse = true; 
            UseKeyboard = true;

            DibujarPantallaFirstBoot();
        }

        // ==========================================
        // DIBUJO DE PANTALLAS
        // ==========================================

        public void DibujarPantallaFirstBoot()
        {
            Surface.Clear(); // Limpiamos la pantalla

            // Imprimimos el texto en las coordenadas X, Y de la pantalla de la consola
            Surface.Print(20, 10, "Selecciona tu idioma / Select your language:", Color.White);
            Surface.Print(32, 12, "1. ES (Espanol)", Color.Cyan);
            Surface.Print(32, 13, "2. EN (English)", Color.Cyan);
        }

        public void DibujarMenuPrincipalES()
        {
            Surface.Clear();
            Surface.Print(29, 5,  "S O U L   A N C H O R", Color.Red);
            Surface.Print(32, 10, "Nueva Partida", Color.White);
            Surface.Print(32, 12, "Cargar Partida", Color.White);
            Surface.Print(35, 14, "Ajustes", Color.White);
            Surface.Print(36, 16, "Salir", Color.White);
        }
        public void DibujarMenuPrincipalEN()
        {
            Surface.Clear();
            Surface.Print(29, 5,  "S O U L   A N C H O R", Color.Red);
            Surface.Print(35, 10, "New Game", Color.White);
            Surface.Print(35 , 12, "Load Game", Color.White);
            Surface.Print(35, 14, "Settings", Color.White);
            Surface.Print(37, 16, "Exit", Color.White);
        }
        public void DibujarPreguntaPrologo()
        {
            Surface.Clear();
            Surface.Print(15, 10, "¿Quieres leer el prologo?", Color.Yellow);
            Surface.Print(15, 12, "1. Si", Color.White);
            Surface.Print(15, 13, "2. No", Color.White);
        }

        // ==========================================
        // LÓGICA DE INPUT (El bucle del juego)
        // ==========================================

        // Update se ejecuta constantemente (como en Unity)
        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            // Lógica temporal de teclado para probar rápido antes de meter el Mouse
            if (!idiomaSeleccionado)
            {
                if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.D1))
                {
                    idioma = "ES";
                    idiomaSeleccionado = true;
                    DibujarMenuPrincipalES();
                }
                else if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.D2))
                {
                    idioma = "EN";
                    idiomaSeleccionado = true;
                    DibujarMenuPrincipalEN();
                }
            }
            else if (gameManager.EstadoActual == EstadoJuego.MenuPrincipal)
            {
                // Aquí simulamos que el jugador hizo clic en "Nueva Partida" pulsando la tecla N
                // Más adelante lo cambiaremos a eventos de Mouse.LeftClicked
                if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.N))
                {
                    DibujarPreguntaPrologo();
                }
                // Si pulsa '2' simulamos que dijo No al prólogo
                else if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.D2))
                {
                    // Arrancamos el juego a través del GameManager
                    gameManager.IniciarNuevaPartida("Ren");

                    Surface.Clear();
                    Surface.Print(2, 2, "=== JUEGO INICIADO ===", Color.Green);
                    Surface.Print(2, 4, $"Bienvenido {gameManager.Prota!.Nombre}. Estas en {gameManager.UbicacionActual!.Nombre}.");
                }
            }
        }
    }
}