using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MoreSlugcats;
using UnityEngine;
using RWCustom;
using BepInEx;
using IL;
using On;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;
using RewiredConsts;

namespace artificer_rubicon_hell_bonanza
{
    [BepInPlugin("dannad.lockeveryroom", "Artificer Rubicon Hell Bonanza", "0.0.24")]
    public class Artificer_Hell : BaseUnityPlugin
    {
        public Player player;
        public Artificer_HellOptions options;
        public CustomHRGuardManager manager = null;
        public RainWorldGame rwg;
        public Artificer_Hell()
        {
            this.options=new Artificer_HellOptions(this);
        }
        public void OnEnable()
        {
            On.Player.NewRoom+=Player_NewRoom;
            On.RainWorld.Update+=RainWorld_Update;
            On.RainWorld.OnModsInit+=RainWorld_OnModsInit;
            On.RainWorldGame.ctor+=this.RainWorldGame_ctor;
            On.Creature.NewRoom+=this.Creature_NewRoom;
        }

        private void Creature_NewRoom(On.Creature.orig_NewRoom orig, Creature self, Room newRoom)
        {
            orig(self, newRoom);
            if(this.manager!=null&&this.manager.triggered&&this.manager.funmode)
            {
                this.manager.AddNewCreature(self);
            }
        }

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            this.rwg=self;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                MachineConnector.SetRegisteredOI("lockeveryroom", options);
            }
            catch
            {
                Logger.LogMessage("BRRR BRRR YOU FUCKED UP!!! YOU FUCKED UPPP VERY BADLY!!!!!");
            }
        }

        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig(self);
            if(this.manager!=null&&this.manager.creatureList!=null&&this.manager.creatureList.Count!=0&&this.manager.triggered) this.manager.Update();
        }
        private void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            this.player=self;
            if(this.manager!=null&&this.manager.room.abstractRoom.name==newRoom.abstractRoom.name)
            {
                return;
            }
            if(this.manager!=null&&this.manager.room!=newRoom)
            {
                this.manager.Dispose();
                this.manager=null;
                this.manager=new CustomHRGuardManager(self, this.Logger, this.options, this.rwg);
            }
            if(this.manager!=null)
            {
                foreach(MultiChain chain in this.manager.multiChains.Values)
                {
                    chain.Dispose();
                }
            }
            this.manager=null;
            this.manager=new CustomHRGuardManager(self, this.Logger, this.options, this.rwg);
            this.manager?.OnRoomEnter(newRoom);
        }
        public class MultiChain : IDisposable
        {
            public BepInEx.Logging.ManualLogSource log;
            private Room room;
            public AbstractCreature creature;
            private int chainsActiveTime = 0;
            public List<VoidChain> chains;
            public MultiChain(AbstractCreature creature, Room room, ManualLogSource log, List<int> exits)
            {
                this.creature=creature;
                this.room=room;
                this.chains=new List<VoidChain>();
                for(int k = 0;k<exits.Count;k++)
                {
                    Vector2 vector = this.room.MiddleOfTile(this.room.shortcuts[exits[k]].StartTile)+IntVector2.ToVector2(this.room.ShorcutEntranceHoleDirection(this.room.shortcuts[exits[k]].StartTile))*15f;
                    VoidChain voidChain = new VoidChain(this.room, vector, creature.realizedCreature.mainBodyChunk.pos);
                    this.room.AddObject(voidChain);
                    this.chains.Add(voidChain);
                    //this.room.shortcuts[k].shortCutType=ShortcutData.Type.DeadEnd;
                }
                //log.LogMessage($"mchain ctor; count:{this.chains.Count}");
                this.log=log;
                this.log.LogMessage($"I am a chain! {this.creature.creatureTemplate.name}{this.creature.ID}");
            }
            public void Update()
            {
                //log.LogMessage($"i am updaaating; chains{this.chains != null} {this.chains.Count}, creature{this.creature.creatureTemplate.name}");
                for(int i = 0;i<this.chains.Count;i++)
                {
                    try
                    {
                        //log.LogDebug($"chain:{this.chains[i] != null}, creature:{creature.ID}, alpha:{this.chains[i].proximityAlpha}");
                        this.chains[i].stuckPosB=creature.realizedCreature.mainBodyChunk.pos;
                        this.chains[i].colorFlash=Mathf.Sin(3.1415927f*((float)this.chainsActiveTime/400f)-3.1415927f)*0.25f+0.25f+0.5f*Mathf.Min(1f, this.chainsActiveTime/80f);
                        //this.chains[i].colorFlash = 1f;
                        //voidChains[i].proximityAlpha = 1f - Mathf.InverseLerp(150f, 750f, Custom.Dist(myPlayer.mainBodyChunk.pos, voidChains[i].stuckPosA));
                        //chains[i].colorFlash = UnityEngine.Random.value;
                        chains[i].proximityAlpha=0.5f;
                    }
                    catch(Exception ex)
                    {
                        log.LogMessage($"Multichain update fucked up, {ex.Message}");
                    }
                }
                //log.LogMessage("i am out of creature loop!");
                this.chainsActiveTime++;
            }
            public void Dispose()
            {
                if(this.chains!=null)
                {
                    for(int i = 0;i<this.chains.Count;i++)
                    {
                        this.room.RemoveObject(this.chains[i]);
                    }
                }
                this.chains=null;
                this.room=null;
                this.creature=null;
                //GC.Collect();
            }
        }
        public class CustomHRGuardManager : IDisposable
        {
            public int activeTime = 0;
            public Room room;
            public BepInEx.Logging.ManualLogSource log;
            public List<AbstractCreature> creatureList = new List<AbstractCreature>();
            public bool triggered;
            private Player myPlayer;
            public Dictionary<AbstractCreature, MultiChain> multiChains = new Dictionary<AbstractCreature, MultiChain>();
            public bool chainsActive;
            private Artificer_HellOptions options;
            public string currentBanlist = "funny";
            public List<int> chainedShortcuts = new List<int>();
            public bool funmode = false;
            public Dictionary<string, Dictionary<CreatureTemplate.Type, bool>> banlist = new Dictionary<string, Dictionary<CreatureTemplate.Type, bool>>
            {
                {
                    "Funny", new Dictionary<CreatureTemplate.Type, bool>
                    {
                        {CreatureTemplate.Type.Slugcat, true}
                    }
                },
                {
                    "Hardcore", new Dictionary<CreatureTemplate.Type, bool>
                    {
                        {CreatureTemplate.Type.Slugcat, true},
                        {CreatureTemplate.Type.GarbageWorm, true},
                    }
                },
                {
                    "Casual", new Dictionary<CreatureTemplate.Type, bool>
                    {
                        {CreatureTemplate.Type.Slugcat, true},
                        {CreatureTemplate.Type.GarbageWorm, true},
                        {CreatureTemplate.Type.TempleGuard, true},
                        {CreatureTemplate.Type.BigEel, true},
                        {CreatureTemplate.Type.Deer, true},
                    }
                },
                {
                    "NoCritters", new Dictionary<CreatureTemplate.Type, bool>
                    {
                        {CreatureTemplate.Type.Slugcat, true},
                        {CreatureTemplate.Type.GarbageWorm, true},
                        {CreatureTemplate.Type.Overseer, true},
                        {CreatureTemplate.Type.TempleGuard, true},
                        {CreatureTemplate.Type.BigEel, true},
                        {CreatureTemplate.Type.Deer, true},
                        {CreatureTemplate.Type.Spider, true},
                        {CreatureTemplate.Type.Leech, true},
                        {CreatureTemplate.Type.Fly, true},
                    }
                },
            };
            private RainWorldGame rwgInstance;
            public bool l = true;
            public CustomHRGuardManager(Player player, BepInEx.Logging.ManualLogSource log, Artificer_HellOptions options, RainWorldGame rwgInstance)
            {
                this.rwgInstance=rwgInstance;
                this.options=options;
                this.log=log;
                this.room=null;
                this.myPlayer=player;
                this.l=this.options.logging.Value;
                this.currentBanlist=this.options.currentBanlist.Value;
                this.funmode=this.options.genocideMode.Value;
                if(l) this.log.LogMessage("created manager instance");
            }
            public void OnRoomEnter(Room room)
            {
                if(l) this.log.LogMessage($"on room enter");
                if(this.room!=null&&this.room==room)
                {
                    if(l) this.log.LogMessage($"entered same room");
                    return;
                }
                this.room=room;
                if(this.room==null)
                {
                    if(l) this.log.LogMessage("room is null");
                    return;
                }
                if(this.room.abstractRoom.creatures.Count<=0)
                {
                    if(l) this.log.LogMessage("no creatures");
                    this.triggered=false;
                    return;
                }
                this.GetCreatureList();
                if(!this.chainsActive)
                {
                    if(l) this.log.LogMessage("on room enter2.2");
                    if(this.creatureList.Count==0)
                    {
                        return;
                    }
                    for(int i = 0;i<this.room.shortcuts.Length;i++)
                    {
                        if(this.room.shortcuts[i].shortCutType==ShortcutData.Type.RoomExit)
                        {
                            this.chainedShortcuts.Add(i);
                            this.room.shortcuts[i].shortCutType=ShortcutData.Type.DeadEnd;
                            if(l) this.log.LogMessage($"added shortcut index {i}");
                        }
                    }
                    for(int i = 0;i<(this.creatureList.Count>=3 ? 3 : this.creatureList.Count);i++)
                    {
                        this.multiChains.Add(this.creatureList[i], new MultiChain(this.creatureList[i], this.room, this.log, this.chainedShortcuts));
                    }
                    if(l) this.log.LogMessage($"total shortcuts tracked:{this.chainedShortcuts.Count}");
                    if(l) this.log.LogMessage("setting triggered to TRUE");
                    this.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Lock, 0f, 1f, global::UnityEngine.Random.value*0.5f+0.8f);
                    this.triggered=true;
                    this.chainsActive=true;
                    this.ConfuseCreatures();
                }
            }

            private void GetCreatureList()
            {
                if(l) log.LogMessage("updating creature list");
                for(int i = 0;i<this.room.abstractRoom.creatures.Count;i++)
                {
                    AbstractCreature creature = this.room.abstractRoom.creatures[i];
                    if(!creature.state.dead&&!creature.realizedCreature.inShortcut)
                    {
                        if(!this.banlist[this.currentBanlist].TryGetValue(creature.creatureTemplate.type, out _))
                        {
                            if(creature.creatureTemplate.type==CreatureTemplate.Type.Overseer&&this.myPlayer.SlugCatClass==MoreSlugcatsEnums.SlugcatStatsName.Spear)
                            {
                                Overseer overseer = (Overseer)creature.realizedCreature;
                                if(overseer.PlayerGuide) continue;
                            }
                            if(l) this.log.LogMessage($"added creature {creature}");
                            this.creatureList.Add(creature);
                        }
                    }
                }
            }

            public void Update()
            {
                if(this.room.game.IsArenaSession)
                {
                    return;
                }
                if(this.multiChains!=null)
                {
                    //if(l) log.LogMessage(this.multiChains.Count);
                    if(this.chainsActive&&this.myPlayer!=null&&this.myPlayer.enteringShortCut!=null&&this.room.shortcutData(this.myPlayer.enteringShortCut.Value).ToNode)
                    {
                        this.myPlayer.firstChunk.vel=this.room.ShorcutEntranceHoleDirection(this.myPlayer.enteringShortCut.Value).ToVector2()*4f;
                        this.myPlayer.enteringShortCut=null;
                    }
                    if(this.triggered)
                    {
                        this.activeTime++;
                        if(this.activeTime%400==0)
                        {
                            this.DoRedundancyCreatureCheck();
                        }
                        if(this.options.timer.Value!=-1&&this.activeTime/40>this.options.timer.Value)
                        {
                            this.FreePlayer();
                            return;
                        }
                        for(int i = 0;i<creatureList.Count;i++)
                        {
                            AbstractCreature creature = this.creatureList[i];
                            creature.creatureTemplate.doesNotUseDens=true;
                            creature.realizedCreature.enteringShortCut=null;
                            if(creature.InDen)
                            {
                                creature.Die();
                            }
                            if(creature.state.dead)
                            {
                                this.creatureList.Remove(creature);
                                if(this.creatureList.Count==0)
                                {
                                    this.FreePlayer();
                                    if(l) this.log.LogMessage("no more creatures, setting triggered to FALSE");
                                    return;
                                }
                                if(this.multiChains.ContainsKey(creature))
                                {
                                    this.multiChains[creature].Dispose();
                                    this.multiChains.Remove(creature);
                                    if(l) this.log.LogMessage($"chain removed");
                                    this.AssignNewChain();
                                }
                                if(l) this.log.LogMessage($"removed dead creature {creature.creatureTemplate.name};id:{creature.ID};listcount:{this.multiChains.Count}");
                                //foreach(KeyValuePair<AbstractCreature, MultiChain> KV in this.multiChains)
                                //{
                                //    log.LogMessage($"{KV.Key!=null}; {KV.Value!=null}");
                                //}
                            }

                            //log.LogMessage($"i wanna updaaate ch:{this.multiChains.Count}, cr:{this.creatureList.Count}");
                            //for (int u = 0; u < this.multiChains.Values.Count; u++)

                            //try
                            //{
                            //log.LogMessage($"something something chain update loop ind:{u}, {this.multiChains.Count}");
                            //MultiChain[] chains = this.multiChains.Values.ToArray<MultiChain>();
                            //chains[u].Update();
                            //}
                            //catch (Exception ex)
                            //{
                            //    log.LogMessage($"{ex.Message}");
                            //}
                            //if(l) log.LogDebug($"updated creatures: {c}");

                        }
                        int c = 0;
                        foreach(KeyValuePair<AbstractCreature, MultiChain> KV in this.multiChains)
                        {
                            if(KV.Value!=null)
                            {

                                //if(l) log.LogDebug($"i updated chain! creature ID: {KV.Key.ID} chainCreature ID:{KV.Value.creature.ID}");
                                KV.Value.Update();
                                //c++;
                            }
                            else
                            {
                                log.LogFatal("WEEEEEEEEEEE WOOOOOOOOOOOOOOO!!!! WEEEEEEEEEEEEEE WOOOOOOOOOOOOOOOOOOOOO!!!!\nYOU ARE A FUCKING IDIOT!!!!!!\nTHERE'S A NULL CHAIN IN THE CHAINLIST!!!!!!");
                                //KV.Value.Dispose();
                            }
                        }
                        //log.LogMessage("i am out of creature update loop!!!!");
                    }
                }
            }


            private void AssignNewChain()
            {
                if (this.creatureList.Count == 0)
                {
                    return;
                }
                int index = this.creatureList.Count>3 ? 3 : this.creatureList.Count-1;
                if(l) this.log.LogMessage($"assign new chain called|creatures count:{this.creatureList.Count}; index: {index}");
                if(this.multiChains.ContainsKey(this.creatureList[index]))
                {
                    if(l) log.LogMessage("There's already a creature with key like that!");
                    return;
                }
                this.multiChains.Add(this.creatureList[index], new MultiChain(this.creatureList[index], this.room, this.log, this.chainedShortcuts));
                if(l) this.log.LogMessage($"adding a new chain; count:{this.creatureList.Count}|{this.creatureList[index].creatureTemplate.name}{this.creatureList[index].ID}");
                //return;
                
                //for(int i = 0;i<(this.creatureList.Count>=3 ? 3 : this.creatureList.Count);i++)
                //{
                //    try
                //    {
                //        this.multiChains.Add(this.creatureList[i], new MultiChain(this.creatureList[i], this.room, this.log));
                //        if(l) log.LogMessage($"added {i}");
                //    }
                //    catch
                //    {
                //        continue;
                //    }
                //}
            }

            private void DoRedundancyCreatureCheck()
            {
                void HandleChainsAndStuff(AbstractCreature c)
                {
                    this.creatureList.Remove(c);
                    if(this.multiChains.ContainsKey(c))
                    {
                        this.multiChains[c].Dispose();
                        this.multiChains.Remove(c);
                    }
                }
                string s = "";
                string ss = "";
                string rs = "";
                for(int i = 0;i<this.creatureList.Count;i++)
                {
                    AbstractCreature creature = this.creatureList[i];
                    s+=$"{creature.creatureTemplate.name}({creature.ID}),";
                }
                foreach(KeyValuePair<AbstractCreature, MultiChain> kv in this.multiChains)
                {
                    ss+=kv.Key.creatureTemplate.name+kv.Key.ID+",";
                }
                rs+=$"RedCeck|creatureCount:{this.creatureList.Count};chainCount:{this.multiChains.Count}\ncreatureList:{s}\ncreatureChains:{ss}\n";
                for(int i = 0;i<this.creatureList.Count;i++)
                {
                    AbstractCreature c = this.creatureList[i];
                    if(!this.banlist[this.currentBanlist].TryGetValue(c.creatureTemplate.type, out _)) //redundant but it's basically free so i'll keep it just in case
                    {
                        if(c.Room!=this.room.abstractRoom)
                        {
                            if(l) this.log.LogMessage($"removed {c.creatureTemplate.name}|wrong room; creature is in:{c.Room.name}, manager is in:{this.room.abstractRoom.name}");
                            HandleChainsAndStuff(c);
                            continue;
                        }
                        if(c.realizedCreature.dead)
                        {
                            if(l) this.log.LogMessage($"removed {c.creatureTemplate.name}|creature is dead");
                            HandleChainsAndStuff(c);
                            continue;
                        }
                        if(c.InDen)
                        {
                            if(l) this.log.LogMessage($"removed {c.creatureTemplate.name}|creature is in den {c.Room.name}");
                            HandleChainsAndStuff(c);
                            continue;
                        }
                    }
                }
                s="";
                ss="";
                for(int i = 0;i<this.creatureList.Count;i++)
                {
                    AbstractCreature creature = this.creatureList[i];
                    s+=$"{creature.creatureTemplate.name}({creature.ID}),";
                }
                foreach(KeyValuePair<AbstractCreature, MultiChain> kv in this.multiChains)
                {
                    ss+=kv.Key.creatureTemplate.name+kv.Key.ID+",";
                }
                rs+=$"PostRedCeck|creatureCount:{this.creatureList.Count};chainCount:{this.multiChains.Count};creatureList:{s}";
                if(l) log.LogMessage(rs);
                if(this.creatureList.Count==0)
                {
                    this.FreePlayer();
                }
            }

            //public MultiChain GetChainByCreature(AbstractCreature creature)
            //{
            //    for (int i = 0; i < this.multiChains.Count; i++)
            //    {
            //        if (this.multiChains[i].creature == creature)
            //        {
            //            return this.multiChains[i];
            //        }
            //    }
            //    return null;
            //}
            private void FreePlayer()
            {
                if(l) this.log.LogMessage("freeing player");
                this.triggered=false;
                this.chainsActive=false;
                this.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, global::UnityEngine.Random.value*0.5f+0.8f);
                for(int i = 0;i<this.chainedShortcuts.Count;i++)
                {
                    this.room.shortcuts[this.chainedShortcuts[i]].shortCutType=ShortcutData.Type.RoomExit;
                    if(l) this.log.LogMessage($"freed shortcut: ind:{this.chainedShortcuts[i]}, type is now: {this.room.shortcuts[this.chainedShortcuts[i]].shortCutType.ToString()}");

                }
                //  this.rwgInstance.cameras[0].shortcutGraphics.ClearSprites();
                this.rwgInstance.cameras[0].shortcutGraphics.NewRoom();
                foreach(KeyValuePair<AbstractCreature, MultiChain> KV in this.multiChains)
                {
                    KV.Value?.Dispose();
                }
            }

            public void ConfuseCreatures()
            {
                if(l) this.log.LogMessage("confusing creatures");
                foreach(AbstractCreature abstractCreature in this.creatureList)
                {
                    if(abstractCreature.abstractAI!=null&&abstractCreature.abstractAI.destination.room!=this.room.abstractRoom.index)
                    {
                        abstractCreature.abstractAI.SetDestination(this.room.ToWorldCoordinate(this.room.RandomPos()));
                    }
                    if(abstractCreature.realizedCreature!=null&&abstractCreature.spawnDen.room==this.room.abstractRoom.index&&abstractCreature.realizedCreature.enteringShortCut!=null&&this.room.shortcutData(abstractCreature.realizedCreature.enteringShortCut.Value).ToNode)
                    {
                        abstractCreature.realizedCreature.enteringShortCut=null;
                    }
                }
            }
            public void Dispose()
            {
                if(l) log.LogMessage("disposing manager");
                foreach(KeyValuePair<AbstractCreature, MultiChain> KV in this.multiChains)
                {
                    KV.Value.Dispose();
                }
                this.chainedShortcuts=null;
                this.creatureList=null;
                this.room=null;
                this.multiChains=null;
                this.myPlayer=null;
                //GC.Collect();
            }

            internal void AddNewCreature(Creature creature)
            {
                this.creatureList.Add(creature.abstractCreature);
                this.DoRedundancyCreatureCheck();
                this.AssignNewChain();
            }
        }
    }
}