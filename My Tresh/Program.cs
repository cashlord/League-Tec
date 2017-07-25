using System;
using Aimtec;
using Aimtec.SDK.Events;

namespace My_Tresh
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
            var unused = new MyTresh();
            Console.WriteLine("My Tresh loaded");
        }
    }
}
