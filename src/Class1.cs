using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using EnemyAPI;
using GungeonAPI;
using ItemAPI;
using NpcApi;
using SaveAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MonoMod.Utils;
using Brave.BulletScript;
using Random = System.Random;
using FullSerializer;
using Gungeon;
using LootTableAPI;
using Alexandria.CharacterAPI;
using BepInEx;
using Alexandria;

using UnityEngine.Networking;

namespace TheGrinReaper
{
    [BepInPlugin(GUID, "The Grin Reaper", "0.0.1")]
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInDependency("kyle.etg.gapi")]
    [BepInDependency("alexandria.etgmod.alexandria")]
    public class Initialisation : BaseUnityPlugin
    {
        public const string GUID = "pretzel.etg.grinreaper";
        public static Initialisation instance;

        public void Awake()
        {
        }
        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }
        public void GMStart(GameManager manager)
        {
            try
            {
                ETGModConsole.Log("Initializing the Grin Reaper...");

                instance = this;

                ETGMod.Assets.SetupSpritesFromFolder(System.IO.Path.Combine(this.FolderPath(), "sprites"));

                StaticReferences.Init();
                Tools.Init();

                FakePrefabHooks.Init();

                ItemBuilder.Init();
                AudioResourceLoader.InitAudio();
                ETGModMainBehaviour.Instance.gameObject.AddComponent<AudioSource>();

                //VFX Setup
                VFX.Init();

                //Set up the grin reaper
                Bombo.Init();

                ETGModConsole.Log("Yay! :D");
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
    }
}


