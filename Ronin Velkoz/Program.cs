using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using RoninVelkoz.Modes;
using EloBuddy.SDK;
using Mario_s_Lib;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Rendering;
using SharpDX;
using static RoninVelkoz.Menus;
using static RoninVelkoz.SpellsManager;

namespace RoninVelkoz
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        /// The firs thing that runs on the template
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }
        //public static AIHeroClient Player
        //{
        //    get { return ObjectManager.Player; }
        //}
        public static AIHeroClient Champion { get { return Player.Instance; } }
        private static List<Vector2> Perpendiculars { get; set; }
        private static MissileClient QMissile;
        /// <summary>
        /// This event is triggered when the game loads
        /// </summary>
        /// <param name="args"></param>
        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            //Put the name of the champion here
            if (Champion.ChampionName != "Velkoz") return;
            Chat.Print("Welcome to the Ronin´s BETA ;)");
            SpellsManager.InitializeSpells();
            Menus.CreateMenu();
            ModeManager.InitializeModes();
            DrawingsManager.InitializeDrawings();
            Interrupter.OnInterruptableSpell += InterruptMode;
            Gapcloser.OnGapcloser += GapCloserMode;
            Game.OnUpdate += QSplitter;
            GameObject.OnCreate += OnCreate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            GameObject.OnCreate += SpellsManager.OnCreate;
        }

        public static void UltFollowMode()
        {
            var target = TargetSelector.GetTarget(SpellsManager.R.Range, DamageType.Magical);
            if (target != null)
                Champion.Spellbook.UpdateChargeableSpell(SpellSlot.R, target.ServerPosition, false, false);
            else
            {
                var mtarget = TargetManager.GetMinionTarget(SpellsManager.R.Range, DamageType.Magical);
                if (mtarget != null)
                    Champion.Spellbook.UpdateChargeableSpell(SpellSlot.R, mtarget.ServerPosition, false, false);
            }
        }

        public static void StackMode()
        {
            foreach (var item in Champion.InventoryItems)
            {
                if ((item.Id == ItemId.Tear_of_the_Goddess || item.Id == ItemId.Tear_of_the_Goddess_Crystal_Scar ||
                     item.Id == ItemId.Archangels_Staff || item.Id == ItemId.Archangels_Staff_Crystal_Scar ||
                     item.Id == ItemId.Manamune || item.Id == ItemId.Manamune_Crystal_Scar)
                    && Champion.IsInShopRange())
                {
                    if ((int)(Game.Time - SpellsManager.StackerStamp) >= 2)
                    {
                        SpellsManager.Q.Cast(Champion);
                        SpellsManager.StackerStamp = Game.Time;
                    }
                }
            }
        }

        public static void InterruptMode(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (sender != null && RoninVelkoz.Menus.MiscMenu.GetCheckBoxValue("Einterrupt"))
            {
                var target = TargetManager.GetChampionTarget(SpellsManager.E.Range, DamageType.Magical);
                if (target != null)
                    SpellsManager.E.Cast(target);
            }
        }

        public static void GapCloserMode(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender != null && RoninVelkoz.Menus.MiscMenu.GetCheckBoxValue("Egapc"))
            {
                var target = TargetManager.GetChampionTarget(SpellsManager.E.Range, DamageType.Magical);
                if (target != null)
                    SpellsManager.E.Cast(target);
            }
        }


        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }

        public static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly)
            {
                Perpendiculars = new List<Vector2>();
                var missile = (MissileClient)sender;
                if (missile.SData.Name != null && missile.SData.Name == "VelkozQMissile")
                    {
                    QMissile = missile;
                    Chat.Print("oncreat");
                    
                    Handle = missile;
                    Direction = (missile.EndPosition.To2D() - missile.StartPosition.To2D()).Normalized();
                    Chat.Print("oncreat2");
                    Perpendiculars.Add(Direction.Perpendicular());
                    Perpendiculars.Add(Direction.Perpendicular2());
                    Chat.Print("oncreat3");
                    }
            }
        }

        public static void OnCreate(GameObject sender, EventArgs args)
        {
            // Check if the sender is a MissleClient
            var missile = (MissileClient)sender;
            if ( missile.SData.Name == "VelkozQMissile")
            {
                // Apply the needed values

            }
        }

        public static void QSplitter(EventArgs args)
        {
            // Check if the missile is active
            if (Handle != null && SpellsManager.Q.IsReady() && SpellsManager.Q.Name == "velkozqsplitactivate")

            {
                Chat.Print("Q detected");
                foreach (var perpendicular in Perpendiculars)
                {
                    if (Handle != null)
                    {
                        var startPos = Handle.Position.To2D();
                        var endPos = Handle.Position.To2D() + SpellRange * perpendicular;

                        var collisionObjects = ObjectManager.Get<Obj_AI_Base>()
                            .Where(o => o.IsEnemy && !o.IsDead && !o.IsStructure() && !o.IsWard() && !o.IsInvulnerable
                                    && o.Distance(Champion, true) < (SpellRange + 200).Pow()
                                    && o.ServerPosition.To2D().Distance(startPos, endPos, true, true) <= (SpellWidth * 2 + o.BoundingRadius).Pow());
                        if (collisionObjects!= null)
                        {
                            Chat.Print("collisionObjects");

                        }
                        
                        var colliding = collisionObjects
                            .Where(o => o.Type == GameObjectType.AIHeroClient && o.IsValidTarget()
                                    && Prediction.Position.Collision.LinearMissileCollision(o, startPos, endPos, MissileSpeed, SpellWidth, CastDelay, (int)o.BoundingRadius))
                                .OrderBy(o => o.Distance(Champion, true)).FirstOrDefault();

                        if (colliding != null)
                        {
                            Chat.Print("Split");
                            Player.CastSpell(SpellSlot.Q);
                            Handle = null;
                        }
                       
                    }
                }
            }
            else
                Handle = null;
        }

    }
}
