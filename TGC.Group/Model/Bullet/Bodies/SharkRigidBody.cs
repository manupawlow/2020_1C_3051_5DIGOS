﻿using BulletSharp;
using BulletSharp.Math;
using Microsoft.DirectX.DirectInput;
using TGC.Core.BulletPhysics;
using TGC.Core.Collision;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Group.Model.Draw;
using TGC.Group.Model.Sharky;
using TGC.Group.Model.Terrains;
using TGC.Group.Utils;
using TGC.Core.Text;
using System.Drawing;
using TGC.Core.Direct3D;

namespace TGC.Group.Model.Bullet.Bodies
{
    class SharkRigidBody
    {
        #region Atributos
        private const float MAX_LIFE = 250;
        private readonly TGCVector2 SHARK_HEIGHT = new TGCVector2(700, 1800);
        private const float SHARK_SEEK_TIME = 5;
        private TGCVector3 scale = new TGCVector3(5, 5, 5);
        private TGCVector3 position = new TGCVector3(-2885, 1720, -525);
        private TGCVector3 director = new TGCVector3(0, 0, 1);
        private readonly Sky skybox;
        private readonly Terrain terrain;
        private CameraFPS camera;
        private bool normalMove = false;
        private bool stalkerModeMove = false;
        private readonly float MaxYRotation = FastMath.PI - (FastMath.QUARTER_PI / 2);
        private readonly float MaxAxisRotation = FastMath.QUARTER_PI;
        private float acumulatedYRotation = 0;
        private float acumulatedXRotation = 0;
        private float seekTimeCounter = 0;
        private TGCMatrix TotalRotation = TGCMatrix.Identity;
        private float Life;
        private BulletRigidBodyFactory rigidBodyFactory = BulletRigidBodyFactory.Instance;
        private Sprite spriteLife;
        public static (int width, int height) screen = (width: D3DDevice.Instance.Device.Viewport.Width, height: D3DDevice.Instance.Device.Viewport.Height);


        public RigidBody body;
        public TgcMesh Mesh;

        public string MediaDir { get; private set; } = Game.Default.MediaDirectory;
        public string ShadersDir { get; private set; } = Game.Default.ShadersDirectory;

        #endregion

        #region Constructor

        public SharkRigidBody(Shark shark, Sky sky, Terrain terrain, CameraFPS camera)
        {
            Mesh = shark.Mesh;
            skybox = sky;
            this.terrain = terrain;
            this.camera = camera;
            Life = MAX_LIFE;
            Init();
        }

        #endregion

        #region Metodos

        private void Init()
        {
            Mesh.Transform = TGCMatrix.Scaling(scale) * TGCMatrix.Translation(position);
            body = rigidBodyFactory.CreateBox(new TGCVector3(88, 77, 280) * 2, 1000, position, 0, 0, 0, 0, false);
            body.CenterOfMassTransform = TGCMatrix.Translation(position).ToBulletMatrix();

            spriteLife = new Sprite(MediaDir, ShadersDir);
            spriteLife.setInitialSprite(new TGCVector2(0.4f, 0.5f), new TGCVector2(650, 0), "barra_vida");
        }

        public void Update(TgcD3dInput input, float elapsedTime, CharacterStatus playerStatus)
        {
            var speed = 1000f;
            var headPosition = GetHeadPosition();

            body.ActivationState = ActivationState.ActiveTag;
            body.AngularVelocity = Vector3.Zero;

            if (stalkerModeMove && CanSeekPlayer(out float rotationAngle, out TGCVector3 rotationAxis))
                PerformStalkerMove(elapsedTime, speed, rotationAngle, rotationAxis);
            else if (normalMove)
                PerformNormalMove(elapsedTime, speed, headPosition);

            if (IsCollapsinWithPlayer())
            {
                playerStatus.ReceiveDamage(30);
                ChangeSharkWay(elapsedTime);
            }

            if (seekTimeCounter >= SHARK_SEEK_TIME || input.keyDown(Key.P))
                ChangeSharkWay(elapsedTime);

            if (input.keyPressed(Key.T)) normalMove = !normalMove;
            if (input.keyPressed(Key.Y)) stalkerModeMove = !stalkerModeMove;
        }

        public void Render()
        {
            Mesh.Transform = TGCMatrix.Scaling(scale) * new TGCMatrix(body.InterpolationWorldTransform);
            Mesh.BoundingBox.transform(Mesh.Transform);
            Mesh.BoundingBox.Render();
            Mesh.Render();
            spriteLife.Render();
            if(MAX_LIFE>0)
            spriteLife.drawText("LIFE SHARK", Color.MediumVioletRed, new Point(580, 20), new Size(100, 100), TgcText2D.TextAlign.LEFT, new Font("Arial Black", 14, FontStyle.Bold));

        }

        public void Dispose()
        {
            body.Dispose();
            Mesh.Dispose();
            spriteLife.Dispose();
        }

        #region Movements
        private void PerformNormalMove(float elapsedTime, float speed, TGCVector3 headPosition)
        {
            var XRotation = 0f;
            var YRotation = 0f;
            terrain.world.interpoledHeight(headPosition.X, headPosition.Z, out float floorHeight);
            var distanceToFloor = body.CenterOfMassPosition.Y - floorHeight;
            var XRotationStep = FastMath.PI * 0.1f * elapsedTime;
            var YRotationStep = FastMath.PI * 0.4f * elapsedTime;

            if (distanceToFloor < SHARK_HEIGHT.X - 150 && acumulatedXRotation < MaxAxisRotation)
                XRotation = XRotationStep;
            else if (IsNumberBetweenInterval(distanceToFloor, SHARK_HEIGHT) && acumulatedXRotation > 0.0012)
                XRotation = -XRotationStep;
            if (distanceToFloor > SHARK_HEIGHT.Y + 150 && acumulatedXRotation > -MaxAxisRotation)
                XRotation = -XRotationStep;
            else if (IsNumberBetweenInterval(distanceToFloor, SHARK_HEIGHT) && acumulatedXRotation < -0.0012)
                XRotation = XRotationStep;

            if (!skybox.Contains(body) && FastMath.Abs(acumulatedYRotation) < MaxYRotation)
                YRotation = YRotationStep * RotationYSign();
            else
                acumulatedYRotation = 0;

            acumulatedXRotation += XRotation;
            acumulatedYRotation += YRotation;

            body.ActivationState = ActivationState.ActiveTag;

            if (XRotation != 0 || FastMath.Abs(acumulatedXRotation) > 0.0012)
            {
                var rotationAxis = TGCVector3.Cross(TGCVector3.Up, director);
                director.TransformCoordinate(TGCMatrix.RotationAxis(rotationAxis, XRotation));

                Mesh.Transform = TGCMatrix.Translation(TGCVector3.Empty) *
                                 TGCMatrix.RotationX(XRotation) *
                                 new TGCMatrix(body.InterpolationWorldTransform);
                body.WorldTransform = Mesh.Transform.ToBulletMatrix();
                speed /= 1.5f;
                TotalRotation *= TGCMatrix.RotationX(XRotation);
            }
            else if (YRotation != 0)
            {
                director.TransformCoordinate(TGCMatrix.RotationY(YRotation));
                Mesh.Transform = TGCMatrix.Translation(TGCVector3.Empty) *
                                 TGCMatrix.RotationY(YRotation) *
                                 new TGCMatrix(body.InterpolationWorldTransform);
                body.WorldTransform = Mesh.Transform.ToBulletMatrix();
                TotalRotation *= TGCMatrix.RotationY(YRotation);
            }
            body.LinearVelocity = director.ToBulletVector3() * -speed;
        }

        private void PerformStalkerMove(float elapsedTime, float speed, float rotationAngle, TGCVector3 rotationAxis)
        {
            var actualDirector = -1 * director;
            seekTimeCounter += elapsedTime;
            var RotationStep = FastMath.PI * 0.4f * elapsedTime;

            if (rotationAngle <= RotationStep)
                return;
            actualDirector.TransformCoordinate(TGCMatrix.RotationAxis(rotationAxis, RotationStep));
            var newRotation = TGCMatrix.RotationAxis(rotationAxis, RotationStep);
            TotalRotation *= newRotation;

            Mesh.Transform = TotalRotation * TGCMatrix.Translation(new TGCVector3(body.CenterOfMassPosition));
            body.WorldTransform = Mesh.Transform.ToBulletMatrix();

            director = -1 * actualDirector;
            body.LinearVelocity = director.ToBulletVector3() * -speed;
        }
        #endregion

        public void ReceiveDamage(float damage)
        {
            Life = FastMath.Clamp(Life - damage, 0, MAX_LIFE);
           
            var initialScale = spriteLife.initialScaleSprite;
            var newScale = new TGCVector2((Life / MAX_LIFE) * initialScale.X, initialScale.Y);
            spriteLife.sprite.Scaling = newScale;
        }

        #endregion

        #region Private Methods
        private TGCVector3 ObtainNormalVector(TGCVector3 vectorA, TGCVector3 vectorB)
        {
            return TGCVector3.Normalize(TGCVector3.Cross(vectorA, vectorB));
        }

        private bool IsCollapsinWithPlayer()
        {
            var distanceToPlayer = (camera.position - GetHeadPosition()).Length(); 
            return distanceToPlayer < 100;
        }
        private void ChangeSharkWay(float elapsedTime)
        {
            var rotation = TGCMatrix.RotationY(FastMath.PI * -RotationYSign());
            director = new TGCVector3(0, 0, 1);
            director.TransformCoordinate(rotation);
            Mesh.Transform = rotation *
                             TGCMatrix.Translation(new TGCVector3(body.CenterOfMassPosition));
            body.WorldTransform = Mesh.Transform.ToBulletMatrix();
            seekTimeCounter = 0;
            acumulatedXRotation = 0;
            acumulatedYRotation = 0;
            TotalRotation = rotation;
        }

        private bool CanSeekPlayer(out float rotationAngle, out TGCVector3 rotationAxis)
        {
            var actualDirector = -1 * director;
            var directorToPlayer = TGCVector3.Normalize(camera.position - new TGCVector3(body.CenterOfMassPosition));
            var NormalVectorFromDirAndPlayer = ObtainNormalVector(actualDirector, directorToPlayer);
            if (NormalVectorFromDirAndPlayer.Length() > 0.98f)
            {
                var dotProduct = TGCVector3.Dot(actualDirector, directorToPlayer) / (director.Length() * directorToPlayer.Length());
                if (dotProduct < 1)
                {
                    var RotationToPlayer = FastMath.Acos(dotProduct);
                    if (RotationToPlayer <= FastMath.QUARTER_PI)
                    {
                        rotationAngle = RotationToPlayer;
                        rotationAxis = NormalVectorFromDirAndPlayer;
                        return true;
                    }                    
                }
            }
            rotationAngle = 0;
            rotationAxis = TGCVector3.Empty;
            return false;
        }

        private bool IsNumberBetweenInterval(float number, TGCVector2 interval)
        {
            return number > interval.X && number < interval.Y;
        }

        private float RotationYSign()
        {
            var bodyToSkyboxCenterVector = TGCVector3.Normalize(skybox.getSkyboxCenter() - new TGCVector3(body.CenterOfMassPosition));
            var actualDirector = -1 * director;
            var normalVector = TGCVector3.Cross(actualDirector, bodyToSkyboxCenterVector);
            return normalVector.Y > 0 ? 1 : -1;
        }

        private TGCVector3 GetHeadPosition()
        {
            return new TGCVector3(body.CenterOfMassPosition) + director * -560;
        }
        #endregion
    }
}
