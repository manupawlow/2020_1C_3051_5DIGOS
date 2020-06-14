﻿using Microsoft.DirectX.DirectInput;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TGC.Core.Input;
using TGC.Group.Model.Callbacks;
using TGC.Core.Mathematica;
using TGC.Core.Shaders;
using TGC.Group.Model.Objects;
using TGC.Group.Model.Status;
using TGC.Group.Utils;
using static TGC.Group.Model.Objects.Common;
using Effect = Microsoft.DirectX.Direct3D.Effect;
using TGC.Core.SceneLoader;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Model.Optimization.Quadtree;
using TGC.Core.Geometry;
using Microsoft.DirectX.Direct3D;

namespace TGC.Group.Model
{
    class GameObjectManager
    {
        private readonly string MediaDir, ShadersDir;
        private MeshBuilder MeshBuilder;
        private readonly Ray Ray;
        private Effect FogShader;
        private TGCVector3 LightPosition = new TGCVector3(1530, 14000, 100);
        public PhysicalWorld PhysicalWorld { get; set; }
        public TgcD3dInput Input { get; set; }
        public CameraFPS Camera { get; set; }
        public Character Character { get; set; }
        public Shark Shark { get; set; }
        public Ship Ship { get; set; }
        public Skybox Skybox { get; set; }
        public Terrain Terrain { get; set; }
        public Water Water { get; set; }
        public List<Fish> Fishes { get; set; }
        public Vegetation Vegetation { get; set; }
        public Weapon Weapon { get; set; }
        public Common Common { get; set; }
        public string ItemSelected { get; set; }
        public bool NearObjectForSelect { get; set; }
        public bool ShowInfoItemCollect { get; set; }
        public bool ShowScene { get; set; }
        public Quadtree QuadTree { get; private set; }
        public TGCBox LightBox { get; set; }

        public GameObjectManager(string mediaDir, string shadersDir, CameraFPS camera, TgcD3dInput input)
        {
            MediaDir = mediaDir;
            ShadersDir = shadersDir;
            Camera = camera;
            Input = input;
            Ray = new Ray(input);
            QuadTree = new Quadtree();
            InitializerObjects();
        }

        private void InitializerObjects()
        {
            FogShader = TGCShaders.Instance.LoadEffect(ShadersDir + "Shaders.fx");            
                        
            FogShader.SetValue("ColorFog", Color.SteelBlue.ToArgb());
            FogShader.SetValue("StartFogDistance", 5000);
            FogShader.SetValue("EndFogDistance", 10000);

            FogShader.SetValue("shipAmbientColor", Color.White.ToArgb());
            FogShader.SetValue("diffuseColor", Color.LightGoldenrodYellow.ToArgb());
            FogShader.SetValue("specularColor", Color.White.ToArgb());
            FogShader.SetValue("specularExp", 20);
            FogShader.SetValue("shipKSpecular", 0.5f);
            FogShader.SetValue("lightPosition", TGCVector3.TGCVector3ToFloat4Array(LightPosition));

            /* Initializer object */
            LightBox = TGCBox.fromSize(TGCVector3.One * 150, Color.White);
            Skybox = new Skybox(MediaDir, Camera);
            Water = new Water(MediaDir, ShadersDir, new TGCVector3(0, 3500, 0));
            Ship = new Ship(MediaDir);
            ShowScene = true;
            Terrain = new Terrain(MediaDir, ShadersDir);
            MeshBuilder = new MeshBuilder(Terrain, Water);
            Shark = new Shark(MediaDir, Skybox, Terrain, Camera);
            Character = new Character(Camera, Input);
            Weapon = new Weapon(MediaDir, Camera);
            Vegetation = new Vegetation(MediaDir);
            Common = new Common(MediaDir);
            Fishes = Common.ListFishes.Select(mesh => new Fish(MediaDir, Skybox, Terrain, mesh)).ToList();
            AddWeaponToCharacter();

            /* Location */

            MeshBuilder.LocateMeshesInWorld(meshes: ref Vegetation.ListAlgas, area: Skybox.CurrentPerimeter);
            MeshBuilder.LocateMeshesInWorld(meshes: ref Common.ListCorals, area: Skybox.CurrentPerimeter);
            MeshBuilder.LocateMeshesInWorld(meshes: ref Common.ListOres, area: Skybox.CurrentPerimeter);
            MeshBuilder.LocateMeshesInWorld(meshes: ref Common.ListRock, area: Skybox.CurrentPerimeter);
            MeshBuilder.LocateMeshesInWorld(meshes: ref Common.ListFishes, area: Skybox.CurrentPerimeter);

            Common.LocateObjects();

            /* Add rigidBody to the world */

            PhysicalWorld = new PhysicalWorld();
            PhysicalWorld.AddBodyToTheWorld(Terrain.Body);
            PhysicalWorld.AddBodyToTheWorld(Character.Body);
            PhysicalWorld.AddBodyToTheWorld(Ship.BodyOutdoorShip);
            PhysicalWorld.AddBodyToTheWorld(Ship.BodyIndoorShip);
            PhysicalWorld.AddBodyToTheWorld(Shark.Body);
            Common.ListCorals.ForEach(coral => PhysicalWorld.AddBodyToTheWorld(coral.Body));
            Common.ListOres.ForEach(ore => PhysicalWorld.AddBodyToTheWorld(ore.Body));
            Common.ListRock.ForEach(rock => PhysicalWorld.AddBodyToTheWorld(rock.Body));

            Skybox.SetShader(FogShader, "Fog");
            Common.SetShader(FogShader, "Fog");
            Shark.SetShader(FogShader, "Fog");
            Vegetation.SetShader(FogShader, "FogVegetation");
            Ship.SetShader(FogShader, "Ship_Light");

            LightBox.Transform = TGCMatrix.Translation(LightPosition);

            var meshes = GetStaticMeshes();
            QuadTree.Camera = Camera;
            QuadTree.create(meshes, Terrain.world.BoundingBox);
        }

        public void CreateBulletCallbacks(CharacterStatus characterStatus) =>
            PhysicalWorld.AddContactPairTest(Shark.Body, Character.Body, new SharkAttackCallback(Shark, characterStatus));

        public void Dispose()
        {
            Skybox.Dispose();
            Terrain.Dispose();
            Water.Dispose();
            Ship.Dispose();
            Shark.Dispose();
            Character.Dispose();
            Fishes.ForEach(fish => fish.Dispose());
            Vegetation.Dispose();
            Common.Dispose();
            Weapon.Dispose();
        }

        public void Render(TgcFrustum frustum)
        {
            Character.Render();
            
            if (Character.IsInsideShip)
                Ship.RenderIndoorShip();
            else
            {
                FogShader.SetValue("CameraPos", TGCVector3.TGCVector3ToFloat4Array(Camera.Position));
                FogShader.SetValue("eyePosition", TGCVector3.TGCVector3ToFloat4Array(Camera.Position));              
                Ship.RenderOutdoorShip();
                Skybox.Render(Terrain.SizeWorld());
                Terrain.Render();
                Shark.Render();
                QuadTree.Render(frustum, false);
                Fishes.ForEach(fish => fish.Render());
                Water.Render();
            }
        }

        public void Update(float elapsedTime, float timeBeetweenUpdate)
        {
            PhysicalWorld.dynamicsWorld.StepSimulation(elapsedTime, maxSubSteps: 10, timeBeetweenUpdate);
            Fishes.ForEach(fish => fish.Update(elapsedTime, Camera));
            Skybox.Update();
            Shark.Update(elapsedTime);
            Water.Update(elapsedTime, Camera.Position);
            Terrain.Update(elapsedTime, Camera.Position);            
            Character.LooksAtTheHatch = Ray.IntersectsWithObject(objectAABB: Ship.Plane.BoundingBox, distance: 500);
            Character.CanAttack = Ray.IntersectsWithObject(objectAABB: Shark.Mesh.BoundingBox, distance: 150);
            Character.NearShip = Ray.IntersectsWithObject(objectAABB: Ship.OutdoorMesh.BoundingBox, distance: 500);
            Character.IsNearSkybox = Skybox.IsNearSkybox;

            DetectSelectedItem();
        }

        public void AddWeaponToCharacter() => Character.Weapon = Weapon;

        public void UpdateCharacter(float elapsedTime) => Character.Update(Ray, Shark.Mesh, elapsedTime);

        private void DetectSelectedItem()
        {
            bool NearCoralForSelect = false;
            bool NearOreForSelect = false;
            bool NearFishForSelect = false;
                        
            TypeCommon Coral = Common.ListCorals.Find(coral => NearCoralForSelect = Ray.IntersectsWithObject(objectAABB: coral.Mesh.BoundingBox, distance: 500));
            TypeCommon Ore = Common.ListOres.Find(ore => NearOreForSelect = Ray.IntersectsWithObject(objectAABB: ore.Mesh.BoundingBox, distance: 500));

            if (Character.CanFish && Coral.Mesh is null && Ore.Mesh is null)
            {
                Fish itemFish = Fishes.Find(fish => NearFishForSelect = Ray.IntersectsWithObject(objectAABB: fish.BoundingBox, distance: 500));
                if (NearFishForSelect) SelectItem(itemFish);
            }
            
            NearObjectForSelect = NearCoralForSelect || NearOreForSelect || NearFishForSelect;

            if (NearCoralForSelect) SelectItem(Coral);
            else if (NearOreForSelect) SelectItem(Ore);
        }

        private void SelectItem(TypeCommon item)
        {
            if (item.Mesh != null && Input.keyPressed(Key.E))
            {
                ShowInfoItemCollect = true;
                ItemSelected = item.Name;
                PhysicalWorld.RemoveBodyToTheWorld(item.Body);
                QuadTree.RemoveMesh(item.Mesh);
                if (item.Name.Contains("CORAL")) Common.ListCorals.Remove(item);
                else Common.ListOres.Remove(item);
            }
        }

        private void SelectItem(Fish item)
        {
            if (Input.keyPressed(Key.E))
            {
                ShowInfoItemCollect = true;
                ItemSelected = item.Mesh.Name;
                Fishes.Remove(item);
                Common.ListFishes.Remove(item.Mesh);
            }
        }       

        private List<TgcMesh> GetStaticMeshes()
        {
            var meshes = new List<TgcMesh>();
            meshes.AddRange(Common.AllMeshes());
            meshes.AddRange(Vegetation.ListAlgas.Select(mesh => mesh.Mesh));
            return meshes;
        }
    }
}