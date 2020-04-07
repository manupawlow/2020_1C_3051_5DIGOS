using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Group.Utils;
using System.Collections.Generic;
using System.Windows.Forms;
using TGC.Group.Model.Corales;
using System;
using TGC.Group.Model.Minerals;
using TGC.Group.Model.Terrains;
using TGC.Group.Model.Sharky;
using TGC.Group.Model.Fishes;
using TGC.Group.Model.MeshBuilders;
using TGC.Group.Model.Watercraft;

namespace TGC.Group.Model
{
    public class GameModel : TgcExample
    {
        private float time;        
        private TgcScene roomNavecita;
        private List<TgcMesh> corales = new List<TgcMesh>();
        private List<TgcMesh> minerals = new List<TgcMesh>();
        private List<Fish> fishes;
        private Sky skyBox;
        private InsideRoom room;
        private Ship ship;

        private Tuple<float, float> positionRangeX = new Tuple<float, float>(-2900, 2900);
        private Tuple<float, float> positionRangeZ = new Tuple<float, float>(-2900, 2900);

        private FishBuilder fishBuilder;
        private World terrain;
        private World water;
        private Shark shark;

        private MeshBuilder meshBuilder;

        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
            fishBuilder = new FishBuilder(mediaDir);
            meshBuilder = new MeshBuilder();
            MeshDuplicator.MediaDir = mediaDir;
        }

        public override void Init()
        {   
            /** Camera **/
            Camara = new CamaraFPS(Input);
            /** World **/
            terrain = new Terrain(MediaDir, ShadersDir);
            terrain.LoadWorld(TGCVector3.Empty);
            water = new Water(MediaDir, ShadersDir);
            water.LoadWorld(new TGCVector3(0, 3500, 0));                        
            skyBox = new Sky(MediaDir, ShadersDir);
            skyBox.LoadSkyBox();
            /** InsideRoom **/
            room = new InsideRoom(MediaDir, ShadersDir);
            room.LoadRoom();
            /** Enemy **/
            shark = new Shark(MediaDir, ShadersDir);
            shark.LoadShark();
            /** Ship **/
            ship = new Ship(MediaDir, ShadersDir);
            ship.LoadShip();            
            /** Fish **/
            fishes = fishBuilder.CreateRandomFishes(30, positionRangeX, positionRangeZ);
            fishBuilder.LocateFishesInTerrain(terrain.world, fishes, water.world.Center.Y - 300);
            /** Vegetation **/
            MeshDuplicator.InitOriginalMeshes();
            var normalCorals = meshBuilder.CreateNewScaledMeshes(MeshType.normalCoral, 33, 4);
            meshBuilder.LocateMeshesInTerrain(ref normalCorals, positionRangeX, positionRangeZ, terrain.world);
            var treeCorals = meshBuilder.CreateNewScaledMeshes(MeshType.treeCoral, 33, 10);
            meshBuilder.LocateMeshesInTerrain(ref treeCorals, positionRangeX, positionRangeZ, terrain.world);
            var spiralCorals = meshBuilder.CreateNewScaledMeshes(MeshType.spiralCoral, 33, 10);
            meshBuilder.LocateMeshesInTerrain(ref spiralCorals, positionRangeX, positionRangeZ, terrain.world);
            var goldOre = meshBuilder.CreateNewScaledMeshes(MeshType.goldOre, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref goldOre, positionRangeX, positionRangeZ, terrain.world);
            var goldOreCommon = meshBuilder.CreateNewScaledMeshes(MeshType.goldOreCommon, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref goldOreCommon, positionRangeX, positionRangeZ, terrain.world);
            var silverOre = meshBuilder.CreateNewScaledMeshes(MeshType.silverOre, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref silverOre, positionRangeX, positionRangeZ, terrain.world);
            var silverOreCommon = meshBuilder.CreateNewScaledMeshes(MeshType.silverOreCommon, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref silverOreCommon, positionRangeX, positionRangeZ, terrain.world);
            var ironOre = meshBuilder.CreateNewScaledMeshes(MeshType.ironOre, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref ironOre, positionRangeX, positionRangeZ, terrain.world);
            var ironOreCommon = meshBuilder.CreateNewScaledMeshes(MeshType.ironOreCommon, 15, 5);
            meshBuilder.LocateMeshesInTerrain(ref ironOreCommon, positionRangeX, positionRangeZ, terrain.world);
            var rock = meshBuilder.CreateNewScaledMeshes(MeshType.rock, 30, 8);
            meshBuilder.LocateMeshesInTerrain(ref rock, positionRangeX, positionRangeZ, terrain.world);

            corales.AddRange(normalCorals);
            corales.AddRange(treeCorals);
            corales.AddRange(spiralCorals);
            minerals.AddRange(goldOre);
            minerals.AddRange(goldOreCommon);
            minerals.AddRange(silverOre);
            minerals.AddRange(silverOreCommon);
            minerals.AddRange(ironOre);
            minerals.AddRange(ironOreCommon);
            minerals.AddRange(rock);

        }

        public override void Update()
        {
            PreUpdate();            
            PostUpdate();
        }

        public override void Render()
        {
            PreRender();

            time += ElapsedTime;

            DrawText.drawText("Prueba de ubicacion de objetos en el terreno", 0, 20, Color.Red);
            DrawText.drawText("camPos: [" + Camara.Position.X.ToString() + "; "
                                          + Camara.Position.Y.ToString() + "; "
                                          + Camara.Position.Z.ToString() + "] ",
                              0, 60, Color.DarkRed);
            DrawText.drawText("camLookAt: [" + Camara.LookAt.X.ToString() + "; "
                                          + Camara.LookAt.Y.ToString() + "; "
                                          + Camara.LookAt.Z.ToString() + "] ",
                              0, 80, Color.DarkRed);

            DrawText.drawText("TIME: [" + time.ToString() + "]", 0, 100, Color.DarkRed);

            // TODO: Habilito la habitacion para que se muestre en un rango de tiempo
            if (time <= 30 && time >= 20)
                room.Render();            

            terrain.Render();
            water.Render();
            skyBox.Render();
            ship.Render();

            corales.ForEach(coral =>
            {
                coral.UpdateMeshTransform();
                coral.Render();
            });

            minerals.ForEach(ore =>
            {
                ore.UpdateMeshTransform();
                ore.Render();

            });

            shark.Render();
            fishes.ForEach(fish =>
            {
                fish.Mesh.UpdateMeshTransform();
                fish.Render();

            });

            PostRender();
        }

        public override void Dispose()
        {
            ship.Dispose();
            room.Dispose();            
            terrain.Dispose();
            water.Dispose();
            skyBox.Dispose();
            shark.Dispose();
            corales.ForEach(coral => coral.Dispose());
            minerals.ForEach(ore => ore.Dispose());
            fishes.ForEach(fish => fish.Dispose());
        }

       /* private float ObtenerMaximaAlturaTerreno()
        {
            var maximo = 0f;
            for (int x = 0; x < terrainHeightmap.HeightmapData.GetLength(0); x++)
            {
                for (int z = 0; z < terrainHeightmap.HeightmapData.GetLength(0); z++)
                {
                    var posibleMaximo = terrainHeightmap.HeightmapData[x, z];
                    if (maximo < terrainHeightmap.HeightmapData[x, z])
                    {
                        maximo = posibleMaximo;
                    }
                }
            }
            return maximo;
        }
        */
    }
}
