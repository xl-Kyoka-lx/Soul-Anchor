using SadConsole;
using SoulAnchor.Managers;
using SoulAnchor.interfaz;

namespace SoulAnchor
{
    class Program
    {
        static void Main()
        {
            Game.Create(80, 25);

            Game.Instance.OnStart = Init;
            Game.Instance.Run();
            Game.Instance.Dispose();
        }

        static void Init()
        {
            GameManager gm = new GameManager();

            ConsoleDisplay pantallaPrincipal = new ConsoleDisplay(80, 25, gm);
            
            Game.Instance.Screen = pantallaPrincipal;
            Game.Instance.DestroyDefaultStartingConsole();
        }
    }
}