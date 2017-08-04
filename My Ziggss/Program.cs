using System;
using Aimtec;
using Aimtec.SDK.Events;

namespace My_Ziggs
{
    internal class Program
    {
        private static void Main()
        {
            GameEvents.GameStart += OnLoadingComplete;
        }
        private static void OnLoadingComplete()
        {
            if (ObjectManager.GetLocalPlayer().ChampionName != "Ziggs") return;
            var unused = new MyZiggss();
            Console.WriteLine("My Ziggss loaded");
        }
    }
}
