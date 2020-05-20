﻿using System;
using System.Collections.Generic;
using System.Linq;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model.MeshBuilders
{
    static class MeshDuplicator
    {
        #region Atributos
        public static string MediaDir;
        private static Dictionary<MeshType, CommonMesh> Meshes = new Dictionary<MeshType, CommonMesh>();
        private static int MeshCounter = 0;
        #endregion

        #region Metodos
        public static void InitOriginalMeshes()
        {
            if (MediaDir == null)
                throw new Exception("MediaDir variable is null, set a value first");

            Meshes.Add(MeshType.normalCoral, new CommonMesh(MediaDir,"normalCoral"));
            Meshes.Add(MeshType.treeCoral, new CommonMesh(MediaDir,"treeCoral"));
            Meshes.Add(MeshType.spiralCoral, new CommonMesh(MediaDir,"spiralCoral"));
            Meshes.Add(MeshType.ironOre, new CommonMesh(MediaDir,"iron"));
            Meshes.Add(MeshType.silverOre, new CommonMesh(MediaDir,"silver"));
            Meshes.Add(MeshType.goldOre, new CommonMesh(MediaDir,"gold"));
            Meshes.Add(MeshType.rock, new CommonMesh(MediaDir,"rock-n"));
            Meshes.Add(MeshType.normalFish, new CommonMesh(MediaDir,"fish"));
            Meshes.Add(MeshType.yellowFish, new CommonMesh(MediaDir,"yellowFish"));
            Meshes.Add(MeshType.alga, new CommonMesh(MediaDir,"alga"));
            Meshes.Add(MeshType.alga_2, new CommonMesh(MediaDir,"new_alga"));
            Meshes.Add(MeshType.alga_3, new CommonMesh(MediaDir,"alga_3"));
            Meshes.Add(MeshType.alga_4, new CommonMesh(MediaDir,"alga_4"));
        }

        public static TgcMesh GetDuplicateMesh(MeshType meshType)
        {
            var originalMesh = Meshes[meshType].Mesh;
            return originalMesh.createMeshInstance(originalMesh.Name + "_" + MeshCounter++);
        }
        #endregion
    }
}
