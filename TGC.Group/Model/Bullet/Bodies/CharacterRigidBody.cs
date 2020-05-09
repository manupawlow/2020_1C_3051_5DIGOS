﻿using BulletSharp;
using BulletSharp.Math;
using Microsoft.DirectX.DirectInput;
using System;
using TGC.Core.BoundingVolumes;
using TGC.Core.BulletPhysics;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Group.Model.Draw;
using TGC.Group.Utils;

namespace TGC.Group.Model.Bullet.Bodies
{
    class CharacterRigidBody
    {
        struct Constants
        {
            public static float speed = 450f;
            public static TGCVector3 cameraHeight = new TGCVector3(0, 85, 0);
            public static TGCVector3 planeDirector
            {
                get
                {
                    var director = new TGCVector3(-1, 0, 0);
                    director.TransformCoordinate(TGCMatrix.RotationY(FastMath.PI_HALF));
                    return director;
                }
            }
            public static float capsuleSize = 160f;
            public static float capsuleRadius = 40f;
        }

        private string MediaDir;
        private string ShadersDir;
        private BulletRigidBodyFactory rigidBodyFactory = BulletRigidBodyFactory.Instance;
        public RigidBody body;
        public bool isCharacter;
        private CameraFPS Camera;
        private TGCVector3 position;
        private TGCVector3 indoorPosition;
        private TGCVector3 outdoorPosition;
        private float prevLatitude;

        private CharacterStatus status;        

        public CharacterRigidBody(CameraFPS camera, string mediaDir, string shadersDir)
        {
            MediaDir = mediaDir;
            ShadersDir = shadersDir;
            isCharacter = true;
            Camera = camera;
            indoorPosition = camera.getIndoorPosition();
            outdoorPosition = camera.getOutdoorPosition();
            Init();
        }

        private void Init()
        {
            status = new CharacterStatus(MediaDir, ShadersDir);

            if (Camera.isOutside) position = Camera.getOutdoorPosition();
            else position = Camera.getIndoorPosition();
            prevLatitude = Camera.Latitude;
            Constants.planeDirector.TransformCoordinate(TGCMatrix.RotationY(FastMath.PI_HALF));

            #region Create rigidBody
            body = rigidBodyFactory.CreateCapsule(Constants.capsuleRadius, Constants.capsuleSize, position, 1f, false);
            body.CenterOfMassTransform = TGCMatrix.Translation(position).ToBulletMatrix();
            #endregion
        }

        public void Update(TgcD3dInput input)
        {
            var speed = Constants.speed;
            body.ActivationState = ActivationState.ActiveTag;

            canRecoverOxygen();

            #region Movimiento 

            body.AngularVelocity = TGCVector3.Empty.ToBulletVector3();

            var director = Camera.LookAt - Camera.position;
            director.Normalize();

            var sideRotation = Camera.Latitude - prevLatitude;
            var sideDirector = Constants.planeDirector;
            sideDirector.TransformCoordinate(TGCMatrix.RotationY(sideRotation));

            if (!isOutOfWater())
            {
                if (input.keyDown(Key.W))
                {
                    body.LinearVelocity = director.ToBulletVector3() * speed;
                }

                if (input.keyDown(Key.S))
                {
                    body.LinearVelocity = director.ToBulletVector3() * -speed;
                }

                if (input.keyDown(Key.A))
                {
                    body.LinearVelocity = sideDirector.ToBulletVector3() * -speed;
                }

                if (input.keyDown(Key.D))
                {
                    body.LinearVelocity = sideDirector.ToBulletVector3() * speed;
                }

                if (input.keyDown(Key.Space))
                {
                    body.LinearVelocity = Vector3.UnitY * speed;
                }

                if (input.keyDown(Key.LeftControl))
                {
                    body.LinearVelocity = Vector3.UnitY * -speed;
                }
            }
            else
                body.ApplyCentralImpulse(Vector3.UnitY * -5);

            if (input.keyUp(Key.W) || input.keyUp(Key.S) || input.keyUp(Key.A) || input.keyUp(Key.D)
                 || input.keyUp(Key.LeftControl) || input.keyUp(Key.Space))
            {
                body.LinearVelocity = Vector3.Zero;
                body.AngularVelocity = Vector3.Zero;
            }

            if (input.keyPressed(Key.E)) Teleport();

            body.LinearVelocity += TGCVector3.Up.ToBulletVector3() * getGravity();

            Camera.position = new TGCVector3(body.CenterOfMassPosition) + Constants.cameraHeight;

            #endregion

            status.Update();
        }

        public  void Render()
        {
            status.Render();
        }

        private float getGravity()
        {
            return body.CenterOfMassPosition.Y < 0 ? -200 : -5;
        }

        private bool isOutOfWater()
        {
            return Camera.position.Y > 3505;
        }

        private bool isInsideShip()
        {
            return Camera.position.Y < 0;
        }

        public  void Dispose()
        {
            body.Dispose();
            status.Dispose();
        }

        public  void Teleport()
        {
            if (Camera.isOutside)
                body.CenterOfMassTransform = TGCMatrix.Translation(indoorPosition).ToBulletMatrix();
            else
                body.CenterOfMassTransform = TGCMatrix.Translation(outdoorPosition).ToBulletMatrix();

            Camera.position = new TGCVector3(body.CenterOfMassPosition);
            body.LinearVelocity = Vector3.Zero;
            body.AngularVelocity = Vector3.Zero;
        }

        private void canRecoverOxygen()
        {
            status.canBreathe = isOutOfWater() || isInsideShip();
        }
    }
}
