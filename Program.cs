using System;
using Aimtec;
using Aimtec.SDK.Events;

namespace FrOnDaL_Lux
{
    internal class Program
    {
        private static void Main()
        {
            GameEvents.GameStart += OnLoadingComplete;
        }
        private static void OnLoadingComplete()
        {
            if (ObjectManager.GetLocalPlayer().ChampionName != "Lux") return;
            var unused = new FrOnDaLLux();
            Console.WriteLine("FrOnDaL Lux loaded");
        }
    }
}
