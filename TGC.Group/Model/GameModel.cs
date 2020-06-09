﻿using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Mathematica;
using TGC.Group.Form;
using TGC.Group.Model.Objects;
using TGC.Group.Model.Status;
using TGC.Group.Utils;

namespace TGC.Group.Model
{
    public class GameModel : TGCExample
    {
        public struct Perimeter
        {
            public float xMin, xMax, zMin, zMax;
            public Perimeter(float xMin, float xMax, float zMin, float zMax)
            {
                this.xMin = xMin;
                this.xMax = xMax;
                this.zMin = zMin;
                this.zMax = zMax;
            }
        }

        private CameraFPS camera;
        private FullQuad FullQuad;

        private GameState StateGame;
        private GameState StateMenu;
        private GameState StateHelp;
        private GameState StateExit;
        private GameState CurrentState;

        private DrawButton Play;
        private DrawButton Help;
        private DrawButton Exit;

        private DrawSprite Title;

        private GameObjectManager ObjectManager;
        private GameInventoryManager InventoryManager;
        private GameEventsManager EventsManager;
        private Game2DManager Draw2DManager;
        private Game2DManager PointerAndInstruction;
        private CharacterStatus CharacterStatus;
        private SharkStatus SharkStatus;
        
        private bool ActiveInventory { get; set; }
        private bool ExitGame { get; set; }
        private bool CanCraftObjects => ObjectManager.Character.IsInsideShip;

        public float TimeToRevive { get; set; }
        public float TimeToAlarm { get; set; }
        public float ItemHistoryTime { get; set; }

        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir) => FixedTickEnable = true;

        public override void Dispose()
        {
            FullQuad.Dispose();
            ObjectManager.Dispose();
            Draw2DManager.Dispose();
            Title.Dispose();
        }

        public override void Update() => CurrentState.Update();
        public override void Render()
        {
            CurrentState.Render();
            if (ExitGame) Application.Exit();
        }

        public override void Init()
        {
            Camera = camera = new CameraFPS(Input);
            FullQuad = new FullQuad(MediaDir, ShadersDir, ElapsedTime);
            PointerAndInstruction = new Game2DManager(MediaDir);
            InitializerState();

            InitializerMenu();
            InitializerGame();
        }
        
        private void InitializerMenu()
        {
            Title = new DrawSprite(MediaDir);
            Title.SetImage("subnautica.png");
            Title.SetInitialScallingAndPosition(new TGCVector2(0.8f, 0.8f), new TGCVector2(50, 50));

            Play = new DrawButton(MediaDir, Input);
            Play.InitializerButton(text: "Play", scale: new TGCVector2(0.4f, 0.4f), position: new TGCVector2(50, 500),
                           action: () => CurrentState = StateGame);
            Help = new DrawButton(MediaDir, Input);
            Help.InitializerButton(text: "Help", scale: new TGCVector2(0.4f, 0.4f), position: new TGCVector2(50, 580),
                           action: () => CurrentState = StateHelp);
            Exit = new DrawButton(MediaDir, Input);
            Exit.InitializerButton(text: "Exit", scale: new TGCVector2(0.4f, 0.4f), 
                                   position: new TGCVector2(PointerAndInstruction.ScreenWitdh - Help.Size.X - 50, PointerAndInstruction.ScreenHeight - Help.Size.Y - 50),
                                   action: () => CurrentState = StateExit);

            camera.Position = new TGCVector3(1030, 3900, 2500);
            camera.Lock = true;
        }

        private void InitializerGame()
        {
            ObjectManager = new GameObjectManager(MediaDir, ShadersDir, camera, Input);
            CharacterStatus = new CharacterStatus(ObjectManager.Character);
            SharkStatus = new SharkStatus();
            EventsManager = new GameEventsManager(ObjectManager.Shark, ObjectManager.Character);
            Draw2DManager = new Game2DManager(MediaDir, CharacterStatus, SharkStatus);
            InventoryManager = new GameInventoryManager();
        }

        private void InitializerState()
        {
            StateGame = new GameState()
            {
                Update = UpdateGame,
                Render = RenderGame
            };

            StateMenu = new GameState()
            {
                Update = UpdateMenu,
                Render = RenderMenu
            };

            StateHelp = new GameState()
            {
                Update = UpdateInstructionHelp,
                Render = RenderMenu
            };

            StateExit = new GameState()
            {
                Update = UpdateExit,
                Render = RenderMenu
            };

            CurrentState = StateMenu;
        }

        #region Help
        private void UpdateInstructionHelp()
        {
            Play.Invisible = true;
            Help.Invisible = true;
            PointerAndInstruction.ShowHelp = true;
            UpdateMenu();
        }

        private void UpdateExit()
        {
            Play.Invisible = false;
            Help.Invisible = false;
            PointerAndInstruction.ShowHelp = false;
            CurrentState = StateMenu;
            UpdateMenu();
        }
        #endregion

        #region Menu
        private void RenderMenu()
        {
            PreRender();
            if (ObjectManager.ShowScene)
            {
                ObjectManager.Skybox.Render();
                ObjectManager.Ship.OutdoorMesh.Render();
                ObjectManager.Water.Render();
            }
            Title.Render();
            Play.Render();
            Help.Render();
            Exit.Render();

            PointerAndInstruction.RenderMousePointer();
            PostRender();
        }

        private void UpdateMenu()
        {            
            Play.Update();
            Help.Update();
            if (CurrentState == StateMenu)
            {
                Exit.Update();
                if (CurrentState == StateExit)
                    ExitGame = true;
            }
            else
                Exit.Update();

            if (ObjectManager.ShowScene)
            {
                ObjectManager.Skybox.Update();
                ObjectManager.Water.Update(ElapsedTime);
            }

            if (CurrentState == StateGame)
            {
                Play.Dispose();
                Help.Dispose();
                Exit.Dispose();
                camera.Lock = false;
            }
        }
        #endregion

        #region Game
        private void RenderGame()
        {
            FullQuad.PreRenderMeshes();
            ObjectManager.Render();
            FullQuad.Render();
            Draw2DManager.Render();
            PostRender();
        }

        private void UpdateGame()
        {          
            if (Input.keyPressed(Key.F1)) Draw2DManager.ShowHelp = !Draw2DManager.ShowHelp;
            ObjectManager.CreateBulletCallbacks(CharacterStatus);
            if (CharacterStatus.IsDead)
            {
                TimeToRevive += ElapsedTime;
                if (TimeToRevive < 5)
                {
                    FullQuad.SetTime(ElapsedTime);
                    FullQuad.RenderTeleportEffect = true;
                }
                else
                {
                    CharacterStatus.Respawn();
                    FullQuad.RenderTeleportEffect = FullQuad.RenderAlarmEffect = false;
                }
                return;
            }
            TimeToRevive = 0;
            if (Input.keyPressed(Key.I)) Draw2DManager.ActiveInventory = camera.Lock =
                    FullQuad.RenderPDA = ActiveInventory = !ActiveInventory;
            if (!ActiveInventory) UpdateEvents();
            ObjectManager.Character.RestartBodySpeed();
            if (Input.keyPressed(Key.E)) ObjectManager.Character.Teleport();
            UpdateFlags();
            UpdateInfoItemCollect();
            if (Input.keyPressed(Key.P)) ObjectManager.Character.CanFish = ObjectManager.Character.HasWeapon =
                ObjectManager.Character.HasDivingHelmet = true;
        }

        private void UpdateEvents()
        {
            ObjectManager.UpdateCharacter(ElapsedTime);
            ObjectManager.Update(ElapsedTime, TimeBetweenUpdates, Frustum);
            EventsManager.Update(ElapsedTime, ObjectManager.Fishes, SharkStatus);
            InventoryManager.AddItem(ObjectManager.ItemSelected);
            Draw2DManager.ItemHistory = InventoryManager.ItemHistory;
            ObjectManager.ItemSelected = null;
            CharacterStatus.Update(ElapsedTime);
            FullQuad.RenderAlarmEffect = CharacterStatus.ActiveRenderAlarm;
            Draw2DManager.DistanceWithShip = FastUtils.DistanceBetweenVectors(camera.Position, ObjectManager.Ship.PositionShip);
            Draw2DManager.ShowIndicatorShip = Draw2DManager.DistanceWithShip > 15000 && !ObjectManager.Character.IsInsideShip;
            Draw2DManager.ShowSharkLife = EventsManager.SharkIsAttacking && !SharkStatus.IsDead;
            if (CharacterStatus.ActiveAlarmForDamageReceived)
            {
                TimeToAlarm += ElapsedTime;
                if (TimeToAlarm > 2)
                {
                    FullQuad.RenderAlarmEffect = CharacterStatus.ActiveAlarmForDamageReceived = false;
                    TimeToAlarm = 0;
                }
            }
            SharkStatus.DamageReceived = ObjectManager.Character.AttackedShark;
            SharkStatus.Update();
            ObjectManager.Shark.DeathMove = SharkStatus.IsDead;
            ObjectManager.Character.AttackedShark = SharkStatus.DamageReceived;
            Draw2DManager.Update();
            Draw2DManager.Inventory.UpdateItems(InventoryManager.Items);
        }

        private void UpdateInfoItemCollect()
        {
            if (!Draw2DManager.ShowInfoItemCollect) return;
            
            ItemHistoryTime += ElapsedTime;
            if (ItemHistoryTime > Draw2DManager.ItemHistoryTime)
            {
                Draw2DManager.ShowInfoItemCollect = false;
                InventoryManager.ItemHistory.RemoveRange(0, InventoryManager.ItemHistory.Count);
                ItemHistoryTime = 0;
            }
        }

        private void UpdateFlags()
        {
            if (CanCraftObjects && Draw2DManager.ActiveInventory)
            {
                if (Input.keyPressed(Key.M)) ObjectManager.Character.HasWeapon = GameCraftingManager.CanCraftWeapon(InventoryManager.Items);
                if (Input.keyPressed(Key.N)) ObjectManager.Character.HasDivingHelmet = CharacterStatus.HasDivingHelmet = GameCraftingManager.CanCraftDivingHelmet(InventoryManager.Items);
                if (Input.keyPressed(Key.B)) ObjectManager.Character.CanFish = GameCraftingManager.CanCatchFish(InventoryManager.Items);
            }

            Draw2DManager.ShowInfoExitShip = ObjectManager.Character.LooksAtTheHatch;
            Draw2DManager.ShowInfoEnterShip = ObjectManager.Character.NearShip;
            Draw2DManager.NearObjectForSelect = ObjectManager.NearObjectForSelect;
            Draw2DManager.ShowInfoItemCollect = ObjectManager.ShowInfoItemCollect;
        }
        #endregion
    }
}