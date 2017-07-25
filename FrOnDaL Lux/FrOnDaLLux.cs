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

namespace FrOnDaL_Lux
{
    internal class FrOnDaLLux
    {
        public static Menu Main = new Menu("Index", "FrOnDaL Lux", true);
        public static Orbwalker Orbwalker = new Orbwalker();
        public static Obj_AI_Hero Lux => ObjectManager.GetLocalPlayer();
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
        public FrOnDaLLux()
        {
            /*Spells*/
            _q = new Spell(SpellSlot.Q, 1250f);
            _w = new Spell(SpellSlot.W, 1150f);
            _e = new Spell(SpellSlot.E, 1100f);
            _r = new Spell(SpellSlot.R, 3340f);

            _q.SetSkillshot(0.250f, 70f, 1300f, true, SkillshotType.Line);
            _w.SetSkillshot(0.25f, 150f, 1200f, false, SkillshotType.Line);
            _e.SetSkillshot(0.25f, 275f, 1050f, false, SkillshotType.Circle);
            _r.SetSkillshot(1f, 150f, float.MaxValue, false, SkillshotType.Circle);
            
            Orbwalker.Attach(Main);
            
            /*Combo Menu*/
            var combo = new Menu("combo", "Combo")
            {
                new MenuBool("q", "Use Combo Q"),
                new MenuList("qHit", "Q Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 1),
                new MenuSliderBool("w", "Use Combo W / if Mana >= x%", true, 15, 0, 99),
                new MenuSliderBool("wAuto", "Use Auto W / if Mana >= x%", false, 60, 0, 99),
                new MenuSlider("wProtect", "Use W Ally Heal <= x%", 50, 1, 99),
                new MenuBool("e", "Use Combo E"),
                new MenuList("eHit", "E Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 1),
                new MenuSlider("UnitsEhit", "E Hit x Units Enemy", 1, 1, 3),
                new MenuBool("r", "Use Combo R"),
                new MenuList("rHit", "R Hitchances", new []{ "Low", "Medium", "High", "VeryHigh", "Dashing", "Immobile" }, 2),
                new MenuBool("rKillSteal", "Auto R KillSteal"),
                new MenuKeyBind("keyR", "R Key:", KeyCode.T, KeybindType.Press)
            };
            combo.OnValueChanged += HcMenu_ValueChanged;
            Main.Add(combo);

            /*LaneClear Menu*/
            var laneclear = new Menu("laneclear", "Lane Clear")
            {
                new MenuSliderBool("q", "Use Q / if Mana >= x%", false, 60, 0, 99),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 60, 0, 99),
                new MenuSlider("UnitsEhit", "E range Hit x Units minions >= x%", 3, 1, 7)
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
                new MenuSliderBool("q", "Use Q / if Mana >= x%", false, 70, 0, 99),
                new MenuSliderBool("e", "Use E / if Mana >= x%", true, 70, 0, 99),
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
                Render.Circle(Lux.Position, _q.Range, 180, Color.Green);
            }
            if (Main["drawings"]["w"].As<MenuBool>().Enabled)
            {
                Render.Circle(Lux.Position, _w.Range, 180, Color.Green);
            }
            if (Main["drawings"]["e"].As<MenuBool>().Enabled)
            {
                Render.Circle(Lux.Position, _e.Range, 180, Color.Green);
            }
            if (Main["drawings"]["r"].As<MenuBool>().Enabled)
            {
                Render.Circle(Lux.Position, _r.Range, 180, Color.Green);
            }           
        }

        private static void Game_OnUpdate()
        {
            if (Lux.IsDead || MenuGUI.IsChatOpen()) return;        
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
            if (Main["combo"]["wAuto"].As<MenuSliderBool>().Enabled && Lux.ManaPercent() > Main["combo"]["wAuto"].As<MenuSliderBool>().Value && Orbwalker.Mode != OrbwalkingMode.Combo)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsInRange(_w.Range) && x.IsAlly && x.HealthPercent() <= Main["combo"]["wProtect"].Value && x.CountEnemyHeroesInRange(750) >= 1))
                {
                    if (ally.IsInRange(_w.Range) && !Lux.IsRecalling())
                    {
                        _w.CastOnUnit(ally);
                    }
                }
            }
            if (Main["jungleclear"]["jungSteal"].As<MenuKeyBind>().Enabled && _r.Ready)
            {
                foreach (var jungSteal in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget(_r.Range) && Lux.GetSpellDamage(x, SpellSlot.R) >= x.Health))
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
                var target = TargetSelector.Implementation.GetOrderedTargets(_r.Range).FirstOrDefault(x => x.Health < Lux.GetSpellDamage(x, SpellSlot.R));
                if (target == null) return;
                _r.Cast(target);
            }
        }

        /*Combo*/
        private static void Combo()
        {
            if (Main["combo"]["e"].As<MenuBool>().Enabled && _e.Ready)
            {
                var target = TargetSelector.GetTarget(_e.Range);
                if (target == null) return; 
                var prediction = _e.GetPrediction(target);
                if (target.CountEnemyHeroesInRange(_e.Width) >= Main["combo"]["UnitsEhit"].As<MenuSlider>().Value)
                {
                    if (prediction.HitChance >= HitChance.High)
                    {                       
                        _e.Cast(prediction.CastPosition);
                    }
                }
            }

            if (Main["combo"]["q"].As<MenuBool>().Enabled && _q.Ready)
            {
                var target = TargetSelector.GetTarget(_q.Range);
                if (target == null) return;                
                    _q.Cast(target);
                
            }

            if (Main["combo"]["w"].As<MenuSliderBool>().Enabled && Lux.ManaPercent() > Main["combo"]["w"].As<MenuSliderBool>().Value && _w.Ready)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsInRange(_w.Range) && x.IsAlly && x.HealthPercent() <= Main["combo"]["wProtect"].Value && x.CountEnemyHeroesInRange(750) >= 1))
                {
                    if (ally.IsInRange(_w.Range) && !Lux.IsRecalling())
                    {
                        _w.CastOnUnit(ally);
                    }
                }
            }

            if (!Main["combo"]["r"].As<MenuBool>().Enabled || !_r.Ready) return;
            {
                var target = TargetSelector.Implementation.GetOrderedTargets(_r.Range).FirstOrDefault(x => x.Health < Lux.GetSpellDamage(x, SpellSlot.R));
                if (target == null) return;
                _r.Cast(target);
            }
        }

        /*Harass*/
        private static void Harass()
        {
            if (Main["harass"]["e"].As<MenuSliderBool>().Enabled && Lux.ManaPercent() > Main["harass"]["e"].As<MenuSliderBool>().Value && _e.Ready)
            {
                var target = TargetSelector.GetTarget(_e.Range);
                if (target == null) return;
                var prediction = _e.GetPrediction(target);
                if (target.CountEnemyHeroesInRange(_e.Width) >= Main["harass"]["UnitsEhit"].As<MenuSlider>().Value)
                {
                    if (prediction.HitChance >= HitChance.Low)
                    {
                        _e.Cast(prediction.CastPosition);
                    }
                }
            }

            if (!Main["harass"]["q"].As<MenuSliderBool>().Enabled || !(Lux.ManaPercent() > Main["harass"]["q"].As<MenuSliderBool>().Value) || !_q.Ready) return;
            {
                var target = TargetSelector.GetTarget(_q.Range);
                if (target == null) return;
                _q.Cast(target);
            }
        }

        /*LaneClear*/
        private static void LaneClear()
        {
            if (Main["laneclear"]["e"].As<MenuSliderBool>().Enabled && Lux.ManaPercent() > Main["laneclear"]["e"].As<MenuSliderBool>().Value && _e.Ready)
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range)).ToList())
                {
                    if (!minion.IsValidTarget(_e.Range) || minion == null) continue;
                    var countt = GameObjects.EnemyMinions.Count(x => x.IsValidTarget(_e.Range));
                    if (countt >= Main["laneclear"]["UnitsEhit"].As<MenuSlider>().Value)
                    {
                        _e.Cast(minion);
                    }
                }
            }

            if (!Main["laneclear"]["q"].As<MenuSliderBool>().Enabled || !(Lux.ManaPercent() > Main["laneclear"]["q"].As<MenuSliderBool>().Value) || !_q.Ready) return;
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range)).ToList())
                {
                    if (!minion.IsValidTarget(_q.Range) || minion == null) continue;
                   
                    _q.Cast(minion);
                    
                }
            }
        }

        public static List<Obj_AI_Minion> GetGenericJungleMinionsTargets()
        {
            return GetGenericJungleMinionsTargetsInRange(float.MaxValue);
        }

        public static List<Obj_AI_Minion> GetGenericJungleMinionsTargetsInRange(float range)
        {
            return GameObjects.Jungle.Where(x => (GameObjects.JungleSmall.Contains(x) || GameObjects.JungleLarge.Contains(x) || GameObjects.JungleLegendary.Contains(x)) && x.IsValidTarget(range)).ToList();
        }

        /*JungleClear*/
        private static void JungleClear()
        {
            if (Main["jungleclear"]["q"].As<MenuSliderBool>().Enabled && Lux.ManaPercent() > Main["jungleclear"]["q"].As<MenuSliderBool>().Value && _q.Ready)
            {
                foreach (var jungleTarget in GetGenericJungleMinionsTargetsInRange(_q.Range))
                {
                    if (jungleTarget.IsValidTarget(_q.Range) && GetGenericJungleMinionsTargets().Contains(jungleTarget) && jungleTarget.IsValidSpellTarget())
                    {
                        _q.Cast(jungleTarget);
                    }
                }
            }

            if (!Main["jungleclear"]["e"].As<MenuSliderBool>().Enabled || !(Lux.ManaPercent() > Main["jungleclear"]["e"].As<MenuSliderBool>().Value) || !_e.Ready) return;
            {
                foreach (var jungleTarget in GetGenericJungleMinionsTargetsInRange(_e.Range))
                {
                    if (jungleTarget.IsValidTarget(_e.Range) && GetGenericJungleMinionsTargets().Contains(jungleTarget) && jungleTarget.IsValidSpellTarget())
                    {
                        _e.Cast(jungleTarget);
                    }
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
                            var drawStartXPos = (float)(barPos.X + (unit.Health > Lux.GetSpellDamage(unit, SpellSlot.R) ? width *((unit.Health - Lux.GetSpellDamage(unit, SpellSlot.R)) / unit.MaxHealth * 100 / 100) : 0));
                            Render.Line(drawStartXPos, barPos.Y, drawEndXPos, barPos.Y, 9, true, unit.Health < Lux.GetSpellDamage(unit, SpellSlot.R) ? Color.GreenYellow : Color.ForestGreen);
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
