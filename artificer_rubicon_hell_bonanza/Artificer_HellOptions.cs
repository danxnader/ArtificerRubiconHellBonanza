using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IL;
using IL.Menu.Remix.MixedUI.ValueTypes;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace artificer_rubicon_hell_bonanza
{
    public class Artificer_HellOptions : OptionInterface
    {
        static Logger logger;
        public class OpFuckingCreatureTuple : UIconfig
        {
            DyeableRect rect;
            FContainer container;
            OpCheckBox button;
            public Configurable<CreatureTemplate.Type> Creature;
            public Configurable<bool> Enabled;
            public Configurable<Dictionary<CreatureTemplate.Type, bool>> ProperConfigurable;
            CreatureTemplate.Type creature;
            public OpFuckingCreatureTuple(Configurable<Dictionary<CreatureTemplate.Type, bool>> config, Vector2 pos, CreatureTemplate.Type creature) : base(config, pos, 0)
            {
                this.creature=creature;
                this.ProperConfigurable=config;
                this.rect=new DyeableRect(container, pos, new Vector2(300, 30), true);
                this.button=new OpCheckBox(this.Enabled, pos+new Vector2(100, 10));
                this.button.OnValueUpdate+=this.Button_OnValueUpdate;
            }

            private void Button_OnValueUpdate(UIconfig config, string value, string oldValue)
            {
                logger.Log($"{value}");
                //this.ProperConfigurable.Value[creature]=value;
            }
        }
        public Configurable<int> timer;
        public Configurable<bool> logging;
        public Configurable<bool> genocideMode;
        public Configurable<string> currentBanlist;
        public Configurable<bool> huntedMode;
        public Configurable<bool> excludeVultures;
        public Configurable<bool> excludeSquids;
        public Configurable<Dictionary<CreatureTemplate.Type, bool>> creatureDictionary;
        public Artificer_Hell instance;
        public Artificer_HellOptions(Artificer_Hell instance)
        {
            this.timer=this.config.Bind<int>("timer", -1, new ConfigurableInfo("Pussy Mode", new ConfigAcceptableRange<int>(-1, 600), "", new object[] { "Timer" }));
            //this.selectedBanlist = this.config.Bind<int>("selectedbanlist", 1, new ConfigurableInfo("Current Banlist", new ConfigAcceptableRange<int>(1, 4), "", new object[] { "Banlist" }));
            this.logging=this.config.Bind<bool>("logging", true);
            this.genocideMode=this.config.Bind<bool>("genocideMode", false);
            this.currentBanlist=this.config.Bind<string>("selectedBanlist", "Hardcore", new ConfigAcceptableList<string>(new string[] { "Funny", "Hardcore", "Casual", "NoCritters" }));
            this.huntedMode=this.config.Bind<bool>("huntedMode", false);
            this.excludeVultures=this.config.Bind<bool>("excludeVultures", true);
            this.excludeSquids=this.config.Bind<bool>("excludeSquids", true);
            this.instance=instance;
            Dictionary<CreatureTemplate.Type, bool> creatureDict = new Dictionary<CreatureTemplate.Type, bool>();
            ExtEnumType a = CreatureTemplate.Type.GetExtEnumType(typeof(CreatureTemplate.Type));
            logger.Log($"FUCKING AAGGH\n\n\n\n {a.entries}\n\n\n\n");
            //foreach (object value in CreatureTemplate.Type.GetExtEnumType(typeof(CreatureTemplate.Type)))
            //{

            //}
            this.creatureDictionary=this.config.Bind<Dictionary<CreatureTemplate.Type, bool>>("CreatureList", null);
        }
        public override void Initialize()
        {
            var opTab = new OpTab(this, "Settings");
            var creatureListTab = new OpTab(this, "CreatureList");
            this.Tabs=new OpTab[] { opTab, };
            UIelement[] opTabOptions = new UIelement[]
            {
                new OpLabel(10f, 570f, "weak..?", true),
                new OpUpdown(this.timer, new UnityEngine.Vector2(180f, 535f), 60),
                new OpLabel(10f, 540f, "Freedom timer (-1:off;sec):"),
                new OpCheckBox(this.logging, new UnityEngine.Vector2(180f, 505f)),
                new OpLabel(10f, 465f, "Total Genocide Kill Everybody FUCK YEAH!!!!:"),
                new OpCheckBox(this.genocideMode, new UnityEngine.Vector2(280f, 465f)),
                new OpLabel(10f, 505f, "Logging (off to reduce lag):"),
                new OpListBox(this.currentBanlist, new UnityEngine.Vector2(500f, 480f), 100, 4, true),
                new OpLabel(250f, 545f, "Current banlist:\nFunny: no bans\nHardcore: Technically unkillable creatures\nCasual: Unkillable creatures\nNoCritters: Casual+small buggers"),
                new OpCheckBox(this.huntedMode, new UnityEngine.Vector2(180f, 435f)),
                new OpLabel(10f, 435f, "Faux-hunted burden:"),
                new OpCheckBox(this.excludeVultures, new UnityEngine.Vector2(180f, 405f)),
                new OpLabel(10f, 405f, "Exclude vultures:"),
                new OpCheckBox(this.excludeSquids, new UnityEngine.Vector2(180f, 375f)),
                new OpLabel(10f, 375f, "Exclude squids:"),
            };
            opTab.AddItems(opTabOptions);
            OpScrollBox box = new OpScrollBox(creatureListTab, 1000f);
            //OpFuckingCreatureTuple fuckingCreatureTuple = new OpFuckingCreatureTuple()
            //OpResourceList list = new   
            //box.AddItems
            //UIelement[] creatureListTabOptions = new UIelement[]
            //{
            //};

        }
    }
}
