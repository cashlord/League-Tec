using System;
using Aimtec;
using Aimtec.SDK.Events;

namespace MyTresh
{
    internal class Program
    {
        private static void Main()
        {
            GameEvents.GameStart += OnLoadingComplete;
        }
        private static void OnLoadingComplete()
        {
            if (ObjectManager.GetLocalPlayer().ChampionName != "Thresh") return;
            var unused = new FrOnDaLThresh();
            Console.WriteLine("MyTresh loaded");
        }
    }
}
