﻿using BulletSharp;
using BulletSharp.Math;
using Microsoft.DirectX.DirectInput;
using System.Collections.Generic;
using TGC.Core.Collision;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Group.Bullet.Physics;
using TGC.Group.PlayerOne;
using TGC.Group.TGCEscenario;
using TGC.Examples.Camara;
using TGC.Core.Textures;
using Microsoft.DirectX.Direct3D;

namespace TGC.Group.Nivel1
{
    public class NivelUno : PhysicsGame
    {
        private Escenario escenario;
        private Player1 player1;
        private bool moving = false;
        private bool rotating = false;
        private bool jump = false;
        private bool jumped = false;
        private bool flag = false;
        private TGCMatrix wheelTransform;

        public override Player1 Init()
        {
            base.Init();

            // Cargamos el escenario y lo agregamos al mundo
            escenario = new Escenario("Escenarios\\escenario-objetos-TgcScene.xml");
            foreach(RigidBody rigid in escenario.rigidBodys)
            {
                world.AddRigidBody(rigid);
            }

            // Creamos a nuestro jugador y lo agregamos al mundo
            player1 = new Player1(world, "vehicles\\chassis-pickup-TgcScene.xml", "vehicles\\tires-minibus-TgcScene.xml", new TGCVector3(144, 20, 0));

            return player1;
        }

        public override Player1 Update(TgcD3dInput Input, TgcThirdPersonCamera camaraInterna, float ElapsedTime)
        {
            // Determinar que la simulación del mundo físico se va a procesar 60 veces por segundo
            world.StepSimulation(1 / 60f, 10);

            // Reiniciar variables de control
            moving = false;
            rotating = false;
            jump = false;

            // Actualizar la velocidad lineal instantanea del vehiculo
            player1.linealVelocity = (player1.rigidBody.InterpolationLinearVelocity.Length * 2).ToString();

            if (player1.linealVelocity.Length > 4)
            {
                player1.linealVelocity = player1.linealVelocity.Substring(0, 4);
            }

            // Si el jugador cayó a más de 100 unidades en Y, se lo hace respawnear
            if (player1.rigidBody.CenterOfMassPosition.Y < -100)
            {
                var transformationMatrix = TGCMatrix.RotationYawPitchRoll(FastMath.PI, 0, 0).ToBsMatrix;
                transformationMatrix.Origin = new Vector3(144, 20, 0);

                player1.rigidBody.MotionState = new DefaultMotionState(transformationMatrix);
                player1.rigidBody.LinearVelocity = Vector3.Zero;
                player1.rigidBody.AngularVelocity = Vector3.Zero;
            }

            // Detectar según el Input, si va a Rotar, Avanzar y/o Saltar
            // Adelante
            if (Input.keyDown(Key.W))
            {
                player1.Vehicle.ApplyEngineForce(player1.engineForce, 2);
                player1.Vehicle.ApplyEngineForce(player1.engineForce, 3);
                moving = true;
            }

            // Atras
            if (Input.keyDown(Key.S))
            {
                //player1.Vehicle.ApplyEngineForce(-player1.engineForce * 0.1f, 0);
                //player1.Vehicle.ApplyEngineForce(-player1.engineForce * 0.1f, 1);
                player1.Vehicle.ApplyEngineForce(-player1.engineForce * 0.33f, 2);
                player1.Vehicle.ApplyEngineForce(-player1.engineForce * 0.33f, 3);
                moving = true;
            }

            // Derecha
            if (Input.keyDown(Key.D))
            {
                player1.Vehicle.SetSteeringValue(player1.steeringAngle, 2);
                player1.Vehicle.SetSteeringValue(player1.steeringAngle, 3);
                rotating = true;
            }

            // Izquierda
            if (Input.keyDown(Key.A))
            {
                player1.Vehicle.SetSteeringValue(-player1.steeringAngle, 2);
                player1.Vehicle.SetSteeringValue(-player1.steeringAngle, 3);
                rotating = true;
            }

            // Saltar
            if (Input.keyDown(Key.Space))
            {
                jump = true;
            }

            // Si no se presionó ninguna tecla
            if (!rotating)
            {
                player1.Vehicle.SetSteeringValue(0, 2);
                player1.Vehicle.SetSteeringValue(0, 3);
            }

            if (!moving)
            {
                player1.Vehicle.ApplyEngineForce(0, 0);
                player1.Vehicle.ApplyEngineForce(0, 1);
                player1.Vehicle.ApplyEngineForce(0, 2);
                player1.Vehicle.ApplyEngineForce(0, 3);
            }

            // Frenar
            if (Input.keyDown(Key.LeftControl))
            {
                player1.Vehicle.SetBrake(13, 0); //Puede ser una propiedad
                player1.Vehicle.SetBrake(13, 1);
                player1.Vehicle.SetBrake(8, 2); //Puede ser una propiedad
                player1.Vehicle.SetBrake(8, 3);
            }
            else
            {
                //Default braking force, always added otherwise there is no friction on the wheels
                if (!moving)
                {
                    player1.Vehicle.SetBrake(1.05f, 0);
                    player1.Vehicle.SetBrake(1.05f, 1);
                    player1.Vehicle.SetBrake(1.05f, 2);
                    player1.Vehicle.SetBrake(1.05f, 3);
                }
                else
                {
                    player1.Vehicle.SetBrake(0.05f, 0);
                    player1.Vehicle.SetBrake(0.05f, 1);
                    player1.Vehicle.SetBrake(0.05f, 2);
                    player1.Vehicle.SetBrake(0.05f, 3);
                }
            }

            // Realizar el salto
            if (jump && !jumped && !flag)
            {
                player1.rigidBody.ApplyCentralImpulse(new Vector3(0, 900, 0)); //Puede ser una propiedad
                jumped = true;
            }

            if (jumped && player1.rigidBody.LinearVelocity.Y < -0.1f)
            {
                flag = true;
                jumped = false;
            }

            if (player1.rigidBody.LinearVelocity.Y > -0.05f)
            {
                flag = false;
            }

            // Actualizar la inclinación del vehiculo
            player1.yawPitchRoll = Quat.ToEulerAngles(player1.rigidBody.Orientation);

            // Si está lo suficientemente rotado en los ejes X o Z no se va a poder mover, por eso lo enderezamos
            if (FastMath.Abs(player1.yawPitchRoll.X) > 1.4f || FastMath.Abs(player1.yawPitchRoll.Z) > 1.4f)
            {
                player1.flippedTime += ElapsedTime;

                if (player1.flippedTime > 3)
                {
                    var transformationMatrix = TGCMatrix.RotationYawPitchRoll(FastMath.PI, 0, 0).ToBsMatrix;
                    transformationMatrix.Origin = player1.rigidBody.WorldTransform.Origin + new Vector3(0, 10, 0);

                    player1.rigidBody.MotionState = new DefaultMotionState(transformationMatrix);
                    player1.rigidBody.LinearVelocity = Vector3.Zero;
                    player1.rigidBody.AngularVelocity = Vector3.Zero;
                    player1.flippedTime = 0;
                }
            }
            else
            {
                player1.flippedTime = 0;
            }

            return player1;
        }

        public override void Render()
        { 
            // Renderizar la malla del auto, en este caso solo el Chasis
            player1.tgcMesh.Transform = new TGCMatrix(player1.Vehicle.ChassisWorldTransform);
            player1.tgcMesh.Render();

            // Como las ruedas no son cuerpos rigidos (aún) se procede a realizar las transformaciones de las ruedas para renderizar
            wheelTransform = TGCMatrix.RotationY(player1.Vehicle.GetSteeringValue(0)) * TGCMatrix.RotationTGCQuaternion(new TGCQuaternion(player1.rigidBody.Orientation.X, player1.rigidBody.Orientation.Y, player1.rigidBody.Orientation.Z, player1.rigidBody.Orientation.W)) * TGCMatrix.Translation(new TGCVector3(player1.Vehicle.GetWheelInfo(0).WorldTransform.Origin));
            player1.Wheel.Transform = wheelTransform;
            player1.Wheel.Render();

            wheelTransform = TGCMatrix.RotationY(player1.Vehicle.GetSteeringValue(1) + FastMath.PI) * TGCMatrix.RotationTGCQuaternion(new TGCQuaternion(player1.rigidBody.Orientation.X, player1.rigidBody.Orientation.Y, player1.rigidBody.Orientation.Z, player1.rigidBody.Orientation.W)) * TGCMatrix.Translation(new TGCVector3(player1.Vehicle.GetWheelInfo(1).WorldTransform.Origin));
            player1.Wheel.Transform = wheelTransform;
            player1.Wheel.Render();

            wheelTransform = TGCMatrix.RotationY(-player1.Vehicle.GetSteeringValue(2)) * TGCMatrix.RotationTGCQuaternion(new TGCQuaternion(player1.rigidBody.Orientation.X, player1.rigidBody.Orientation.Y, player1.rigidBody.Orientation.Z, player1.rigidBody.Orientation.W)) * TGCMatrix.Translation(new TGCVector3(player1.Vehicle.GetWheelInfo(2).WorldTransform.Origin));
            player1.Wheel.Transform = wheelTransform;
            player1.Wheel.Render();

            wheelTransform = TGCMatrix.RotationY(-player1.Vehicle.GetSteeringValue(3) + FastMath.PI) *  TGCMatrix.RotationTGCQuaternion(new TGCQuaternion(player1.rigidBody.Orientation.X, player1.rigidBody.Orientation.Y, player1.rigidBody.Orientation.Z, player1.rigidBody.Orientation.W)) * TGCMatrix.Translation(new TGCVector3(player1.Vehicle.GetWheelInfo(3).WorldTransform.Origin));
            player1.Wheel.Transform = wheelTransform;
            player1.Wheel.Render();

            // Renderizar el escenario
            escenario.tgcScene.RenderAll();
        }

        public override void Dispose()
        {
            world.Dispose();
            dispatcher.Dispose();
            collisionConfiguration.Dispose();
            constraintSolver.Dispose();
            broadphase.Dispose();
            player1.tgcMesh.Dispose();
            player1.rigidBody.Dispose();
            escenario.Dispose();
        }
    }
}
