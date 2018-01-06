using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using UnityEngine.Rendering;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;

namespace KerbalKlinic
{

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KerbalKlinic : MonoBehaviour
    {   bool StockPrice;
        Rect MenuWindow;
        public Vector2 scrollPosition;
        ProtoCrewMember SelectedKerb;
        KSP.UI.Screens.ApplicationLauncherButton appLauncherButton;
        public string KlinicPriceString;
        public double KlinicPrice;
        public Rect KlinicWindow = new Rect(200, 200, 100, 100);
        bool ButtonPress = false;
        string RelPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public int ToolbarINT = 0;
        ConfigNode Konf = ConfigNode.Load(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+"/files/config.cfg");
        

        
        
        
        








       


        public void Awake()
        {
            //Get values from cfg
            MenuWindow = new Rect(Screen.width / 2 + int.Parse(Konf.GetValue("WindowPosX")), Screen.height / 2 + int.Parse(Konf.GetValue("WindowPosY")), 400, 400);
            KlinicPriceString = Konf.GetValue("Cost");
            StockPrice = bool.Parse(Konf.GetValue("Stock price"));
            //create appbutton
            if (appLauncherButton == null)
            {
                var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(RelPath + "/files/button.png"));
                appLauncherButton =  KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(
                  () => { ButtonPress = true; },
                  () => { ButtonPress = false;},
                  null, null, null, null,
                  KSP.UI.Screens.ApplicationLauncher.AppScenes.SPACECENTER,
                   texture);
               
                
             }
        }
        
        public void OnGUI()
        {
            //create GUI
            if (ButtonPress == true ) 
            {
                GUI.skin = HighLogic.Skin;
                MenuWindow = GUI.Window(0, MenuWindow, MenuGUI, "Kerbal Klinic 1.1");
                
            }
           
        }
        
        void MenuGUI(int windowID)
        {
           
            //LISTENERSTELLUNG / get dead and alive kerbals
            IEnumerable<ProtoCrewMember> KerbalKIA = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Dead);
            IEnumerable<ProtoCrewMember> KerbalAlive = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Assigned);
            
            //Toolbar
           string[] toolbarSTRING = new string[] { "KerbalKlinic", "Options" };
           ToolbarINT = GUI.Toolbar(new Rect(20, 30, 360, 30), ToolbarINT, toolbarSTRING);

            //Main Window
            if (ToolbarINT == 0)
            {
                //Label
                GUI.Label(new Rect(100, 70, 200, 20), "Select Kerbal");

                //KerbalSelection
                GUI.BeginGroup(new Rect(50, 100, 300, 230));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(300), GUILayout.Height(230));
                
                        //generate buttons for dead kerbals
                foreach (ProtoCrewMember p in KerbalKIA)
                {
                    if (p != null)
                    {
                        if (GUILayout.Button(p.ToString()))
                        {
                            Debug.Log(p.ToString());
                            SelectedKerb = p;
                        }
                    }
                }
                
                GUI.EndScrollView();
                GUI.EndGroup();

                //Resurrection Button
                if (SelectedKerb != null && SelectedKerb.rosterStatus != ProtoCrewMember.RosterStatus.Available)
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {   if (StockPrice == true)
                         { CalculateHireCost(); }
                        else if (StockPrice == false)
                        { KlinicPrice = double.Parse(KlinicPriceString); }
                        if (GUI.Button(new Rect(20, 340, 360, 60), "Resurrect " + SelectedKerb + " for " + KlinicPrice.ToString() + " funds."))
                        {
                            if (Funding.CanAfford(Convert.ToSingle(KlinicPrice)))
                            {
                                Funding.Instance.AddFunds(-KlinicPrice, TransactionReasons.CrewRecruited);
                                SelectedKerb.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                            }

                        }
                    }
                    else
                    {
                        if (GUI.Button(new Rect(20, 340, 360, 60), "Resurrect " + SelectedKerb))
                        {SelectedKerb.rosterStatus = ProtoCrewMember.RosterStatus.Available; }
                    }
                }
               
            }

            //Options
            else if(ToolbarINT == 1)
            {
                GUI.Label(new Rect(20, 70, 360, 20), "Change cost");
                KlinicPriceString = GUI.TextField(new Rect(20, 100, 360, 30), KlinicPriceString);
                KlinicPriceString = Regex.Replace(KlinicPriceString, "[^0-9]", "");
                if(KlinicPriceString == null)
                {
                    KlinicPriceString = "0";
                }
                if(GUI.Button(new Rect(20, 150, 360, 50), "Save in config"))
                {
                   
                    Konf.SetValue("Cost", KlinicPriceString);
                    Konf.Save(RelPath+"/files/config.cfg");
                }
                StockPrice = GUI.Toggle(new Rect(20, 220, 360, 40), StockPrice, "Use stock price");
                
            }
            GUI.DragWindow(new Rect(0, 0, 400, 400));

        }
        void OnDisable()
        {
            KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Konf.SetValue("WindowPosX", MenuWindow.x - Screen.width/2);
            Konf.SetValue("WindowPosY", MenuWindow.y - Screen.height/2);
            Konf.SetValue("Stock price", StockPrice);
            Konf.Save(RelPath + "/files/config.cfg");
        }

        void CalculateHireCost()
        {
            double HiredKerbals = HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() + 1;
            KlinicPrice = (150 * Math.Pow(HiredKerbals, 2) + 12350 * HiredKerbals) / 10;
        }
    }
}
