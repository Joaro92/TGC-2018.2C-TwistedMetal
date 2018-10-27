using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Mathematica;
using TGC.Core.Shaders;
using TGC.Examples.Camara;
using TGC.Group.Model.Items;
using TGC.Group.Model.Vehicles;
using TGC.Group.Model.World.Weapons;
using TGC.Group.Physics;
using TGC.Group.Utils;
using Button = TGC.Group.Model.Input.Button;
using Effect = Microsoft.DirectX.Direct3D.Effect;

namespace TGC.Group.Model.World
{
    public class NivelUno : PhysicsGame
    {
        private readonly TGCVector3 initialPos = new TGCVector3(144f, 7.5f, 0f);

        private Effect effectScene, effectVehicle;

        public NivelUno(Vehiculo vehiculoP1)
        {
            // Cargamos el escenario y lo agregamos al mundo
            var dir = Game.Default.MediaDirectory + Game.Default.ScenariosDirectory;
            escenario = new Scenario(world, dir + "scene-level1c-TgcScene.xml");
            
            // Creamos a nuestro jugador y lo agregamos al mundo
            player1 = new Player1(world, vehiculoP1, initialPos); // mover a Partida

            // Le damos unas armas a nuestro jugador
            player1.AddWeapon(new Power());
            player1.SelectedWeapon.Ammo += 1;

            // Crear SkyBox
            skyBox = Skybox.InitSkybox();

            // Spawneamos algunos items
            SpawnItems();

            //Cargar Shader personalizado
            effectScene = TgcShaders.loadEffect(Game.Default.ShadersDirectory + "ToonShadingScene.fx");
            effectVehicle = TgcShaders.loadEffect(Game.Default.ShadersDirectory + "ToonShadingVehicle2.fx");

            //Configurar los valores de cada luz
            var lightColors = new ColorValue[1];
            var pointLightPositions = new Vector4[1];
            var pointLightIntensity = new float[1];
            var pointLightAttenuation = new float[1];
            for (var i = 0; i < 1; i++)
            {
                lightColors[i] = ColorValue.FromColor(Color.White);
                pointLightPositions[i] = TGCVector3.Vector3ToVector4(new TGCVector3(400, 900, -80));
                pointLightIntensity[i] = 165;
                pointLightAttenuation[i] = 0.29f;
            }

            effectScene.SetValue("materialEmissiveColor", TGCVector3.Vector3ToVector4(new TGCVector3(0.51f, 0.51f, 0.51f)));
            effectScene.SetValue("materialDiffuseColor", TGCVector3.Vector3ToVector4(new TGCVector3(1, 1, 0.999f)));
            effectScene.SetValue("lightColor", lightColors);
            effectScene.SetValue("lightPosition", pointLightPositions);
            effectScene.SetValue("lightIntensity", pointLightIntensity);
            effectScene.SetValue("lightAttenuation", pointLightAttenuation);

            effectVehicle.SetValue("materialEmissiveColor", TGCVector3.Vector3ToVector4(new TGCVector3(0.51f, 0.51f, 0.51f)));
            effectVehicle.SetValue("materialDiffuseColor", TGCVector3.Vector3ToVector4(new TGCVector3(1, 1, 0.999f)));
            effectVehicle.SetValue("lightColor", lightColors);
            effectVehicle.SetValue("lightPosition", pointLightPositions);
            effectVehicle.SetValue("lightIntensity", pointLightIntensity);
            effectVehicle.SetValue("lightAttenuation", pointLightAttenuation);

            // le asigno el efecto a la malla
            player1.Mesh.Effect = effectVehicle;
            player1.Mesh.Technique = "RenderScene";
            //player1.Mesh.D3dMesh.ComputeNormals();

            foreach (var block in escenario.TgcScene.Meshes)
            {
                //if ( || block.Name.Equals("Arbusto") || block.Name.Equals("Pasto") || block.Name.Equals("Flores"))
                //    continue;
                if (block.Name.Contains("Arbol") || block.Name.Contains("Palmera"))
                {
                    block.D3dMesh.ComputeNormals();
                    block.Effect = effectScene;
                    block.Technique = "RenderScene";
                }


                if (char.IsLower(block.Name[0]) || block.Name.Equals("Roca") || block.Name.Equals("ParedCastillo") || block.Name.Equals("PilarEgipcio"))
                {
                    block.D3dMesh.ComputeNormals();
                    block.Effect = effectScene;
                    block.Technique = "RenderScene";
                }


            }
        }

        public override void Update(GameModel gameModel, TgcThirdPersonCamera camaraInterna, ModoCamara modoCamara)
        {
            // Determinar que la simulación del mundo físico se va a procesar 60 veces por segundo
            world.StepSimulation(1 / 60f, 30);

            // Actualizar variables de control
            UpdateControlVariables(gameModel);

            // Actualizar variables del jugador que requieren calculos complejos una sola vez
            player1.UpdateInternalValues();

            // Si el jugador cayó a más de 100 unidades en Y, se lo hace respawnear
            if (player1.RigidBody.CenterOfMassPosition.Y < -100)
            {
                player1.Respawn(inflictDmg, initialPos);
            }

            //Si está lo suficientemente rotado en los ejes X o Z no se va a poder mover, por eso lo enderezamos
            if (FastMath.Abs(player1.yawPitchRoll.X) > 1.39f || FastMath.Abs(player1.yawPitchRoll.Z) > 1.39f)
            {
                player1.flippedTime += gameModel.ElapsedTime;
                if (player1.flippedTime > 3)
                {
                    player1.Straighten();
                }
            }
            else
            {
                player1.flippedTime = 0;
            }

            // Manejar los inputs del teclado y joystick
            player1.ReactToInputs(gameModel);

            // Disparar Machinegun
            if (gameModel.Input.keyDown(Key.E) || gameModel.Input.buttonDown(Button.R2))
            {
                FireMachinegun(gameModel);
            }

            // Disparar arma especial
            if (gameModel.Input.keyPressed(Key.R) || gameModel.Input.buttonPressed(Button.L2))
            {
                FireWeapon(gameModel, player1.SelectedWeapon);
            }

            // Metodo que se encarga de manejar las colisiones según corresponda
            CollisionsHandler(gameModel);

            // Ajustar la posicion de la cámara segun la colisión con los objetos del escenario
            AdjustCameraPosition(camaraInterna, modoCamara);
        }

        public override void Render(GameModel gameModel)
        {
            //effect.SetValue("mCamPos", TGCVector3.Vector3ToFloat3Array(gameModel.Camara.Position));
            player1.Render();

            //foreach (var mesh in escenario.TgcScene.Meshes)
            //{
            //    var r = TgcCollisionUtils.classifyFrustumAABB(gameModel.Frustum, mesh.BoundingBox);
            //    if (r != TgcCollisionUtils.FrustumResult.OUTSIDE)
            //    {
            //        mesh.Render();
            //    }
            //}

            gameModel.DrawText.drawText(player1.RigidBody.WorldTransform.Origin.ToString(), 5, 30, Color.Black);
            escenario.Render();

            skyBox.Render();

            bullets.ForEach(bullet => bullet.Render());
            foreach (Item item in items)
            {
                if (item.IsPresent)
                    item.Mesh.Render();
            }
        }

        // ------- Métodos Privados -------

        private void SpawnItems()
        {
            //base propia
            items.Add(new Health(new TGCVector3(168f, 4f, 24f)));
            items.Add(new Energy(new TGCVector3(72f, 4f, 24f)));
            items.Add(new PowerItem(new TGCVector3(168f, 4f, 72f)));

            //base enemiga
            items.Add(new Health(new TGCVector3(-216f, 4f, 552f)));
            items.Add(new Energy(new TGCVector3(-120f, 4f, 552f)));
            items.Add(new PowerItem(new TGCVector3(-216f, 4f, 504f)));

            //zonas dificiles
            items.Add(new PowerItem(new TGCVector3(-72f, 4f, 240f)));
            items.Add(new Health(new TGCVector3(216f, 10f, 264f)));
            items.Add(new Energy(new TGCVector3(-120f, 10f, 240f)));
            
        }
    }
}
