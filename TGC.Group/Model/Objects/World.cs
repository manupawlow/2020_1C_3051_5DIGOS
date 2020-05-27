﻿using TGC.Core.Mathematica;
using TGC.Group.Utils;
using static TGC.Group.Model.GameModel;

namespace TGC.Group.Model.Objects
{
    internal abstract class World
    {
        protected string FILE_HEIGHTMAPS, FILE_TEXTURES, FILE_EFFECT;
        protected float SCALEXZ = 300f, SCALEY = 12f;
        private readonly string MediaDir, ShadersDir;
        protected TGCVector3 Position = TGCVector3.Empty;
        public SmartTerrain world = new SmartTerrain();
        protected string Technique;

        public World(string mediaDir, string shadersDir)
        {
            MediaDir = mediaDir;
            ShadersDir = shadersDir;
        }

        public virtual void Dispose() => world.Dispose();

        public virtual void LoadWorld()
        {
            world.LoadHeightmap(MediaDir + FILE_HEIGHTMAPS, SCALEXZ, SCALEY, Position);
            world.LoadTexture(MediaDir + FILE_TEXTURES);
            world.LoadEffect(ShadersDir + FILE_EFFECT, Technique);
        }

        public virtual void Render() => world.Render();

        public virtual Perimeter SizeWorld()
        {
            var sizeX = world.HeightmapData.GetLength(0) * SCALEXZ / 2;
            var sizeZ = world.HeightmapData.GetLength(1) * SCALEXZ / 2;
            
            Perimeter perimeter = new Perimeter
            {
                xMax = sizeX,
                xMin = -sizeX,
                zMax = sizeZ,
                zMin = -sizeZ
            };

            return perimeter;
        }

    }
}
