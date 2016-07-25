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
            //Loading.OnLoadingCompleteSpectatorMode += Loading_OnLoadingComplete;
        }
        //public static AIHeroClient Player
        //{
        //    get { return ObjectManager.Player; }
        //}
        public static AIHeroClient Champion { get { return Player.Instance; } }
        private static List<Vector2> Perpendiculars { get; set; }
        private static MissileClient QMissile;
        private static MissileClient Handle;
        public const float maxAngle = 96f;
        private static Vector3 intersection;
        public static Vector3 intersection2 = intersection;
        //public static Vector3 LastPosition = new Vector3(Handle.Position).To3D(); 
        public static float QTime = 0;
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
            Game.OnUpdate += QSplitter2;
            //Game.OnUpdate += OnTick;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            //GameObject.OnCreate += SpellsManager.OnCreate;
            Drawing.OnDraw += OnDraw3;
        }
        
        private static void OnDraw3(EventArgs args)
        {
             if (intersection != null)
             {
             	
             }
             //var CurrentTarget = TargetSelector.GetTarget(1500, DamageType.Magical);
	    // var enemydirection = CurrentTarget.ServerPosition;
	  //   var startPos = Handle.Position.To2D();
         //    var intertwoD = intersection.To2D();


                   
		  //   Circle.Draw(SharpDX.Color.White, 10, 50, intertwoD.To3D());
	            // Circle.Draw(SharpDX.Color.Blue, 100, 100, enemydirection);
	         //    Circle.Draw(SharpDX.Color.Red, 10, 50, startPos.To3D());
	           //  foreach (var perpendicular in Perpendiculars)
	          //   {
	          //      var endPos = Handle.Position.To2D() + 1000 * perpendicular;
	           //     Circle.Draw(SharpDX.Color.Yellow, 10, 60, endPos.To3D());
	                 
        	   //  }
               
             
        }
        
        private static void OnTick(EventArgs args)
        {
        	var CurrentTarget = TargetSelector.GetTarget(1500, DamageType.Magical);
        	if (CurrentTarget != null)
                {
                	const float step = maxAngle / 12f;
                	var currentAngle = 0f;
			var currentStep = 0f;
			var cos = Math.Cos(currentAngle);
			var intcos = (int)cos;
			
			var enemydirection = (CurrentTarget.Position.To2D()).Normalized();
                        var skillshotline = ( Champion.ServerPosition.To2D() + enemydirection.Rotated(currentAngle) * 1100 * intcos);
			while(true)	
			{
                            // Validate the counter, break if no valid spot was found in previous loops
                                if (currentStep > maxAngle && currentAngle < 0)
                                {
                                    break;
                                }

                                // Check next angle
                                if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                                {
                                    currentAngle = (currentStep) * (float) Math.PI / 180;
                                    currentStep += step;
                                }
                                else if (currentAngle > 0)
                                {
                                    currentAngle = -currentAngle;
                                }
                                var collisionObjects = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Both,
				Champion.Position,1500, false);
                                foreach (var minion in collisionObjects)
                                if(!Prediction.Position.Collision.LinearMissileCollision(minion, Champion.Position.To2D(), intersection.To2D(), 1300, 100, 250) && !Prediction.Position.Collision.LinearMissileCollision(minion, intersection.To2D(), CurrentTarget.Position.To2D() , 1300, 100, 250))
	                        { 
	                                if (currentStep == 0)
	                                {
	                                      currentStep = step;
	                                      intersection = CurrentTarget.ServerPosition;
					}
					else
	                                {
				            intersection = skillshotline.To3D();
				            break;
					}
	                        }
			}        
	
				
      
                                

                }        




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
            if (sender.IsAlly && sender != null && sender.Type != GameObjectType.obj_AI_Minion)
            {
                Perpendiculars = new List<Vector2>();
                var missile = (MissileClient)sender;
                if (missile.SData.Name != null && missile.SData.Name == "VelkozQMissile")
                    {
                    QMissile = missile;
                    Handle = missile;
                    QTime = Core.GameTickCount;
                    }
            }
        }



        private static void QSplitter(EventArgs args)
        {
            // Check if the missile is active
            if (Handle != null && Core.GameTickCount - QTime <= 1000)

            {
           //     Chat.Print("Q detected");
                Direction = (Handle.EndPosition.To2D() - Handle.StartPosition.To2D()).Normalized();
                Perpendiculars.Add(Direction.Perpendicular());
                Perpendiculars.Add(Direction.Perpendicular2());

            }
            else
                Handle = null;
        }
        private static void QSplitter2(EventArgs args)
        {
		if (Handle != null)
		{
	                foreach (var perpendicular in Perpendiculars)
	                {
	                    if (Handle != null)
	                    {
	                        var startPos = Handle.Position.To2D();
	                        var endPos = Handle.Position.To2D() + 1100 * perpendicular;
	
	                        var collisionObjects = EntityManager.Heroes.Enemies.Where(it => it.IsValidTarget(1500));
	                       // if (collisionObjects.Count() >= 1)
	                     //   {
	                      //      Chat.Print("enemy");
	                            
	                     //   }
	                        
	                        foreach (var hero in collisionObjects)
	                        {
		                if ( Prediction.Position.Collision.LinearMissileCollision(hero, startPos, endPos, 2000, 100, 0))
	                    	{
	
	
	                           SpellsManager.Q.Cast(Champion);
	
	                           
	                        }
	                        }
	                       
	                    }
	                }
		}
                

        }

    }
}
