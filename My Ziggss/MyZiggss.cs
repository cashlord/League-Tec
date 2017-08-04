using Aimtec;
using System.Linq;
using System.Drawing;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Util;
using Aimtec.SDK.Damage;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.Extensions;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.TargetSelector;
using System.Collections.Generic;
using Aimtec.SDK.Menu.Components;
using Aimtec.SDK.Prediction.Skillshots;

namespace My_Ziggs
{
    internal class MyZiggss
    {
        public static Menu Main = new Menu("Index", "My Ziggss", true);
        public static Orbwalker Orbwalker = new Orbwalker();
        public static Obj_AI_Hero Ziggs => ObjectManager.GetLocalPlayer();
        private static Spell _q, _w, _e, _r;
        public static readonly List<string> SpecialChampions = new List<string> { "Annie", "Jhin" };
        
        public static int SxOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 1 : 10;
        }
        public static int SyOffset(Obj_AI_Hero target)
        {
            return SpecialChampions.Contains(target.ChampionName) ? 7 : 20;
        }
        public MyZiggss()
        {
            /*Spells*/
            _q = new Spell(SpellSlot.Q, 1400f);
            _w = new Spell(SpellSlot.W, 1000f);
            _e = new Spell(SpellSlot.E, 900f);
            _r = new Spell(SpellSlot.R, 5300f);

            _q.SetSkillshot(0.3f, 130f, 1700f, true, SkillshotType.Line);
            _w.SetSkillshot(0.25f, 275f, 1750f, false, SkillshotType.Line);
            _e.SetSkillshot(0.5f, 100f, 1750f, false, SkillshotType.Circle);
            _r.SetSkillshot(1f, 500f, float.MaxValue, false, SkillshotType.Circle);
            
            Orbwalker.Attach(Main);
            
            /*Combo Menu*/
            var combo = new Menu("combo", "Combo")
            {
                new MenuBool("q", "Use Combo Q"),
                new MenuList("qHit", "Q Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 3),
                new MenuSliderBool("w", "Use Combo W / if Mana >= x%", true, 15, 0, 99),
                new MenuSliderBool("wAuto", "Use Auto W / if Mana >= x%", false, 60, 0, 99),
                new MenuSlider("wProtect", "Use W Ally Heal <= x%", 50, 1, 99),
                new MenuBool("e", "Use Combo E"),
                new MenuList("eHit", "E Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 1),
                new MenuSlider("UnitsEhit", "E Hit x Units Enemy", 1, 1, 3),
                new MenuBool("r", "Use Combo R"),
                new MenuList("rHit", "R Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 2),
                new MenuBool("rKillSteal", "Auto R KillSteal", false),
                new MenuKeyBind("keyR", "R Key:", KeyCode.T, KeybindType.Press)
            };
            combo.OnValueChanged += HcMenu_ValueChanged;
            Main.Add(combo);

            /*LaneClear Menu*/
            var laneclear = new Menu("laneclear", "Lane Clear")
            {
                new MenuSliderBool("q", "Use Q / if Mana >= x%", false, 60, 0, 99),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 60, 0, 99),
                new MenuSlider("UnitsEhit", "E Hit x Units minions >= x%", 3, 1, 7)
            };
            Main.Add(laneclear);

            /*JungleClear Menu*/
            var jungleclear = new Menu("jungleclear", "Jungle Clear")
            {
                new MenuSliderBool("q", "Use Q / if Mana >= x%", true, 30, 0, 99),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 30, 0, 99),
                new MenuKeyBind("jungSteal", "Baron - Dragon - RiftHerald Steal, R Key:", KeyCode.S, KeybindType.Toggle)
            };
            Main.Add(jungleclear);

            /*Harass Menu*/
            var harass = new Menu("harass", "Harass")
            {
                new MenuBool("autoHarass", "Auto Harass", false),
                new MenuKeyBind("keyHarass", "Harass Key:", KeyCode.C, KeybindType.Press),
                new MenuSliderBool("q", "Use Q / if Mana >= x%", false, 30, 0, 99),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 30, 0, 99),
                new MenuSlider("UnitsEhit", "E Hit x Units Enemy", 1, 1, 3),
            };
            Main.Add(harass);

            /*Drawings Menu*/
            var drawings = new Menu("drawings", "Drawings")
            {
                new MenuBool("q", "Draw Q", false),
                new MenuBool("w", "Draw W", false),
                new MenuBool("e", "Draw E"),
                new MenuBool("r", "Draw R"),
                new MenuBool("drawDamage", "Use Draw Ulti(R) Damage")
            };
            Main.Add(drawings);
            Main.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Render.OnPresent += DamageDraw;
            Render.OnPresent += SpellDraw;
        }

        /*Drawings*/
        private static void SpellDraw()
        {
            if (Main["drawings"]["q"].As<MenuBool>().Enabled)
            {
                Render.Circle(Ziggs.Position, _q.Range, 160, Color.Green);
            }
            if (Main["drawings"]["w"].As<MenuBool>().Enabled)
            {
                Render.Circle(Ziggs.Position, _w.Range, 175, Color.Green);
            }
            if (Main["drawings"]["e"].As<MenuBool>().Enabled)
            {
                Render.Circle(Ziggs.Position, _e.Range, 180, Color.Green);
            }
            if (Main["drawings"]["r"].As<MenuBool>().Enabled)
            {
                Render.Circle(Ziggs.Position, _r.Range, 220, Color.Red);
            }           
        }

        private static void Game_OnUpdate()
        {
            if (Ziggs.IsDead || MenuGUI.IsChatOpen()) return;        
            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Laneclear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }
            if (Main["harass"]["autoHarass"].As<MenuBool>().Enabled)
            {
                Harass();              
            }
            if (Main["combo"]["wAuto"].As<MenuSliderBool>().Enabled && Ziggs.ManaPercent() > Main["combo"]["wAuto"].As<MenuSliderBool>().Value && Orbwalker.Mode != OrbwalkingMode.Combo)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsInRange(_w.Range) && x.IsAlly && x.HealthPercent() <= Main["combo"]["wProtect"].Value && x.CountEnemyHeroesInRange(750) >= 1))
                {
                    if (ally.IsInRange(_w.Range) && !Ziggs.IsRecalling())
                    {
                        _w.CastOnUnit(ally);
                    }
                }
            }
            if (Main["jungleclear"]["jungSteal"].As<MenuKeyBind>().Enabled && _r.Ready)
            {
                foreach (var jungSteal in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget(_r.Range) && Ziggs.GetSpellDamage(x, SpellSlot.R) >= x.Health))
                {
                    if (jungSteal.UnitSkinName.StartsWith("SRU_Dragon") || jungSteal.UnitSkinName.StartsWith("SRU_Baron") || jungSteal.UnitSkinName.StartsWith("SRU_RiftHerald"))
                    {                     
                            _r.Cast(jungSteal);                                                
                    }                 
                }
            }
            if (_r.Ready && Main["combo"]["keyR"].As<MenuKeyBind>().Enabled)
            {
                var target = TargetSelector.GetTarget(_r.Range);
                if (target == null) return;
                _r.Cast(target);
            }

            if (!_r.Ready || !Main["combo"]["rKillSteal"].As<MenuBool>().Enabled) return;
            {
                var target = TargetSelector.Implementation.GetOrderedTargets(_r.Range).FirstOrDefault(x => x.Health < Ziggs.GetSpellDamage(x, SpellSlot.R));
                if (target != null && Ziggs.Distance(target) > 350)
                {
                    _r.Cast(target);
                }
            }
        }

        /*Combo*/
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1500) 
            if (target != null)
            {
                if (Main["combo"]["e"].As<MenuBool>().Enabled && _e.Ready && target.IsValidTarget(_e.Range))
                {                                
                    if (GameObjects.EnemyHeroes.Count(t => t.IsValidTarget(_e.Width, false, false, _e.GetPrediction(target).CastPosition)) >= Main["combo"]["UnitsEhit"].As<MenuSlider>().Value)
                    {
                        _e.Cast(_e.GetPrediction(target).CastPosition);
                    }
                }

                if (Main["combo"]["q"].As<MenuBool>().Enabled && _q.Ready && target.IsValidTarget(_q.Range))
                {
                    _q.Cast(target);                 
                }
            }
            
            if (Main["combo"]["w"].As<MenuSliderBool>().Enabled && Ziggs.ManaPercent() > Main["combo"]["w"].As<MenuSliderBool>().Value && _w.Ready)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsInRange(_w.Range) && x.IsAlly && x.HealthPercent() <= Main["combo"]["wProtect"].Value && x.CountEnemyHeroesInRange(750) >= 1))
                {
                    if (ally.IsInRange(_w.Range) && !Ziggs.IsRecalling())
                    {
                        _w.CastOnUnit(ally);
                    }
                }
            }

            if (!Main["combo"]["r"].As<MenuBool>().Enabled || !_r.Ready) return;
            {
                var targetR = TargetSelector.Implementation.GetOrderedTargets(_r.Range).FirstOrDefault(x => x.Health < Ziggs.GetSpellDamage(x, SpellSlot.R));
                if (targetR != null && Ziggs.Distance(target) > 350)
                {
                    _r.Cast(targetR);
                }
                
            }
        }

        /*Harass*/
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1500);
            if (target == null) return;
            if (Main["harass"]["e"].As<MenuSliderBool>().Enabled && Ziggs.ManaPercent() > Main["harass"]["e"].As<MenuSliderBool>().Value && _e.Ready && target.IsValidTarget(_e.Range))
            {                          
                if (GameObjects.EnemyHeroes.Count(t => t.IsValidTarget(_e.Width, false, true, _e.GetPrediction(target).CastPosition)) >= Main["harass"]["UnitsEhit"].As<MenuSlider>().Value)
                {
                    _e.Cast(_e.GetPrediction(target).CastPosition);
                }
            }

            if (!Main["harass"]["q"].As<MenuSliderBool>().Enabled || !(Ziggs.ManaPercent() > Main["harass"]["q"].As<MenuSliderBool>().Value) || !_q.Ready || !target.IsValidTarget(_q.Range)) return;
            {                    
                    _q.Cast(target);                           
            }
        }

        /*LaneClear*/
        private static void LaneClear()
        {
            if (Main["laneclear"]["e"].As<MenuSliderBool>().Enabled && Ziggs.ManaPercent() > Main["laneclear"]["e"].As<MenuSliderBool>().Value && _e.Ready)
            {                
                foreach (var targetE in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range)))
                {
                    if (targetE == null) continue;
                    if (GameObjects.EnemyMinions.Count(t => t.IsValidTarget(_e.Width, false, false, _e.GetPrediction(targetE).CastPosition)) >= Main["laneclear"]["UnitsEhit"].As<MenuSlider>().Value && !Ziggs.IsUnderEnemyTurret())
                    {
                        _e.Cast(_e.GetPrediction(targetE).CastPosition);
                    }
                }
            }

            if (!Main["laneclear"]["q"].As<MenuSliderBool>().Enabled || !(Ziggs.ManaPercent() > Main["laneclear"]["q"].As<MenuSliderBool>().Value) || !_q.Ready) return;
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range)).ToList())
                {
                    if (!minion.IsValidTarget(_q.Range) || minion == null) continue;                 
                    _q.Cast(minion);                    
                }
            }
        }

        /*JungleClear*/
        private static void JungleClear()
        {          
            foreach (var targetJ in GameObjects.Jungle.Where(x => !GameObjects.JungleSmall.Contains(x) && (GameObjects.JungleLarge.Contains(x) || GameObjects.JungleLegendary.Contains(x)) && x.IsValidTarget(900)))
            {
                if (Main["jungleclear"]["q"].As<MenuSliderBool>().Enabled && Ziggs.ManaPercent() > Main["jungleclear"]["q"].As<MenuSliderBool>().Value && _q.Ready)
                {
                    _q.Cast(targetJ);
                }

                if (Main["jungleclear"]["e"].As<MenuSliderBool>().Enabled && (Ziggs.ManaPercent() > Main["jungleclear"]["e"].As<MenuSliderBool>().Value) && _e.Ready )
                {
                    _e.Cast(targetJ.Position);
                }
            }
        }

        /*Draw Damage Ulti */
        private static void DamageDraw()
        {
            if (Main["drawings"]["drawDamage"].Enabled)
            {
                ObjectManager.Get<Obj_AI_Base>().Where(h => h is Obj_AI_Hero && h.IsValidTarget() && h.IsValidTarget(1700)).ToList().ForEach(unit =>
                        {
                            var heroUnit = unit as Obj_AI_Hero;
                            const int width = 103;
                            var xOffset = SxOffset(heroUnit);
                            var yOffset = SyOffset(heroUnit);
                            var barPos = unit.FloatingHealthBarPosition;
                            barPos.X += xOffset;
                            barPos.Y += yOffset;
                            var drawEndXPos = barPos.X + width * (unit.HealthPercent() / 100);
                            var drawStartXPos = (float)(barPos.X + (unit.Health > Ziggs.GetSpellDamage(unit, SpellSlot.R) ? width *((unit.Health - Ziggs.GetSpellDamage(unit, SpellSlot.R)) / unit.MaxHealth * 100 / 100) : 0));
                            Render.Line(drawStartXPos, barPos.Y, drawEndXPos, barPos.Y, 9, true, unit.Health < Ziggs.GetSpellDamage(unit, SpellSlot.R) ? Color.GreenYellow : Color.ForestGreen);
                        });
            }
        }

        /*Credit Eox*/
        private static void HcMenu_ValueChanged(MenuComponent sender, ValueChangedArgs args)
        {
            if (args.InternalName == "qHit")
            {
                _q.HitChance = (HitChance)args.GetNewValue<MenuList>().Value + 3;
            }          

            if (args.InternalName == "eHit")
            {
                _e.HitChance = (HitChance)args.GetNewValue<MenuList>().Value + 3;
            }

            if (args.InternalName == "rHit")
            {
                _r.HitChance = (HitChance)args.GetNewValue<MenuList>().Value + 3;
            }
        }  
    }
}
