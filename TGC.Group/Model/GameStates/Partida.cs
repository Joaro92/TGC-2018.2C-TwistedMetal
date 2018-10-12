﻿using System.Drawing;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Examples.Camara;
using TGC.Group.Bullet.Physics;
using TGC.Group.Nivel1;
using TGC.Group.PlayerOne;
using TGC.Group.Utils;
using TGC.Core.Direct3D;
using BulletSharp.Math;
using TGC.Core.Textures;
using TGC.Examples.Engine2D.Spaceship.Core;
using DeviceType = SharpDX.DirectInput.DeviceType;
using Key = Microsoft.DirectX.DirectInput.Key;
using System.Drawing.Text;
using TGC.Core.Text;

namespace TGC.Group.Model.GameStates
{
    public class Partida : IGameState
    {
        private GameModel gameModel;

        private readonly string[] vehicleColors = new string[] { "Blue", "Citrus", "Green", "Orange", "Red", "Silver", "Violet" };
        private readonly ModoCamara[] modosCamara = new ModoCamara[] { ModoCamara.NORMAL, ModoCamara.LEJOS, ModoCamara.CERCA };

        private PhysicsGame physicsEngine;
        private Player1 player1;
        private TgcThirdPersonCamera camaraInterna;
        private bool drawUpVector = false;
        private bool showBoundingBox = false;
        private TgcArrow directionArrow;
        private float anguloCamara;
        private float halfsPI;
        private bool mirarHaciaAtras;
        private ModoCamara modoCamara = ModoCamara.NORMAL;
        private Drawer2D drawer2D;
        private int screenHeight, screenWidth;
        private CustomSprite statsBar, healthBar, specialBar, weaponsHud;
        private TGCVector2 specialScale, hpScale;
        private TgcText2D speed, km, actualWeapon, ammoQuantity, border;

        public Partida(GameModel gameModel)
        {
            this.gameModel = gameModel;

            // Tamaño de la pantalla
            screenHeight = D3DDevice.Instance.Device.Viewport.Height;
            screenWidth = D3DDevice.Instance.Device.Viewport.Width;
            
            // Inicializamos la interface para dibujar sprites 2D
            drawer2D = new Drawer2D();

            // Sprite del HUD de la velocidad y stats del jugador
            statsBar = new CustomSprite();
            statsBar.Bitmap = new CustomBitmap(gameModel.MediaDir + "Images\\stats.png", D3DDevice.Instance.Device);
            statsBar.Position = new TGCVector2(screenWidth * 0.81f, screenHeight * 0.695f);

            var scalingFactorX = (float)screenWidth / (float)statsBar.Bitmap.Width;
            var scalingFactorY = (float)screenHeight / (float)statsBar.Bitmap.Height;

            statsBar.Scaling = new TGCVector2(0.25f, 0.42f) * (scalingFactorY / scalingFactorX);

            // Sprite del HUD de las armas
            weaponsHud = new CustomSprite();
            weaponsHud.Bitmap = new CustomBitmap(gameModel.MediaDir + "Images\\weapons hud 2.png", D3DDevice.Instance.Device);
            weaponsHud.Position = new TGCVector2(-15, screenHeight * 0.64f);

            scalingFactorX = (float)screenWidth / (float)weaponsHud.Bitmap.Width;
            scalingFactorY = (float)screenHeight / (float)weaponsHud.Bitmap.Height;

            weaponsHud.Scaling = new TGCVector2(0.6f, 0.6f) * (scalingFactorY / scalingFactorX);

            // Sprite que representa la vida
            healthBar = new CustomSprite();
            healthBar.Bitmap = new CustomBitmap(gameModel.MediaDir + "Images\\healthBar.png", D3DDevice.Instance.Device);
            healthBar.Position = new TGCVector2(screenWidth * 0.8605f, screenHeight * 0.728f); //para 125 % escalado
            //healthBar.Position = new TGCVector2(screenWidth * 0.8515f, screenHeight * 0.7215f); //para 100% escalado

            scalingFactorX = (float)screenWidth / (float)healthBar.Bitmap.Width;
            scalingFactorY = (float)screenHeight / (float)healthBar.Bitmap.Height;

            healthBar.Scaling = new TGCVector2(0.079f, 0.08f) * (scalingFactorY / scalingFactorX);
            hpScale = healthBar.Scaling;

            // Sprite de la barra de especiales
            specialBar = new CustomSprite();
            specialBar.Bitmap = new CustomBitmap(gameModel.MediaDir + "Images\\specialBar.png", D3DDevice.Instance.Device);
            specialBar.Position = new TGCVector2(screenWidth * 0.861f, screenHeight * 0.83f); //para 125 % escalado
            //specialBar.Position = new TGCVector2(screenWidth * 0.8515f, screenHeight * 0.8025f); //para 100 % escalado

            scalingFactorX = (float)screenWidth / (float)specialBar.Bitmap.Width;
            scalingFactorY = (float)screenHeight / (float)specialBar.Bitmap.Height;

            specialBar.Scaling = new TGCVector2(0.079f, 0.08f) * (scalingFactorY / scalingFactorX);
            specialScale = specialBar.Scaling;

            // Preparamos el mundo físico con todos los elementos que pertenecen a el
            physicsEngine = new NivelUno();
            player1 = physicsEngine.Init();

            // Configuramos la Cámara en tercera persona para que siga a nuestro Player 1
            camaraInterna = new TgcThirdPersonCamera(new TGCVector3(player1.rigidBody.CenterOfMassPosition), new TGCVector3(0, 2, 0), modoCamara.AlturaCamara(), modoCamara.ProfundidadCamara());
            this.gameModel.Camara = camaraInterna;

            // Creamos una flecha que representara el vector UP del auto
            directionArrow = new TgcArrow();
            directionArrow.BodyColor = Color.Red;
            directionArrow.HeadColor = Color.Green;
            directionArrow.Thickness = 0.1f;
            directionArrow.HeadSize = new TGCVector2(1, 2);

            // Fuente para mostrar la velocidad
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile(gameModel.MediaDir + "Fonts\\Open 24 Display St.ttf");
            FontFamily family = pfc.Families[0];
            var speedFont = new Font(family, 32);
            var kmFont = new Font(family, 20);

            speed = new TgcText2D
            {
                Text = "0",
                Color = Color.Green,
                Position = new Point((int)(screenWidth * 0.397f), (int)(screenHeight * 0.906f))  // para 125% escalado
                //Position = new Point((int)(screenWidth * 0.38f), (int)(screenHeight * 0.865f)) // para 100% escalado

            };
            speed.changeFont(speedFont);
            km = new TgcText2D
            {
                Text = "km",
                Color = Color.Black,
                Position = new Point((int)(screenWidth * 0.431f), (int)(screenHeight * 0.927f)) // para 125% escalado
                //Position = new Point((int)(screenWidth * 0.41f), (int)(screenHeight * 0.88f)) // para 100% escalado

            };
            km.changeFont(kmFont);

            // Fuentes para mostrar la munición y armas
            pfc.AddFontFile(gameModel.MediaDir + "Fonts\\Insanibc.ttf");
            family = pfc.Families[0];
            var actualWeaponFont = new Font(family, 24);
            var ammoQuantityFont = new Font(family, 22);

            actualWeapon = new TgcText2D
            {
                Text = "[ None ]",
                Color = Color.Black,
                Position = new Point(-(int)(screenWidth * 0.406f), (int)(screenHeight * 0.921f))  // para 125% escalado
            };
            actualWeapon.changeFont(actualWeaponFont);

            ammoQuantity = new TgcText2D
            {
                Text = "0",
                Color = Color.Black,
                Position = new Point(-(int)(screenWidth * 0.345f), (int)(screenHeight * 0.856f))  // para 125% escalado
            };
            ammoQuantity.changeFont(ammoQuantityFont);
            border = new TgcText2D // El borde es para que tenga un color blanco de fondo para que se distinga más
            {
                Text = "0",
                Color = Color.White,
                Position = new Point(-(int)(screenWidth * 0.3453f), (int)(screenHeight * 0.8535f))  // para 125% escalado
            };
            border.changeFont(actualWeaponFont);
        }

        public void Update()
        {
            // Mostrar bounding box del TgcMesh
            if (gameModel.Input.keyPressed(Key.F1))
            {
                showBoundingBox = !showBoundingBox;
            }

            // Girar la cámara unos grados
            if (gameModel.Input.keyPressed(Key.F2))
            {
                if (anguloCamara == 0.33f)
                {
                    anguloCamara = -anguloCamara;
                }
                else
                {
                    anguloCamara += 0.33f;
                }
            }

            // Dibujar el Vector UP
            if (gameModel.Input.keyPressed(Key.F3))
            {
                drawUpVector = !drawUpVector;
            }

            if (gameModel.Input.keyPressed(Key.F4))
            {
                TgcTexture[] diffuseMaps = player1.tgcMesh.DiffuseMaps;

                string newTextureName = "";
                int index = 0;
                foreach (TgcTexture texture in diffuseMaps)
                {
                    if (texture.FileName.Contains("Car Material"))
                    {
                        newTextureName = texture.FilePath;
                        break;
                    }
                    index++;
                }

                string oldColor = newTextureName.Split('\\')[5].Split(' ')[2].Split('.')[0];
                string newColor = vehicleColors.getNextOption(oldColor);
                newTextureName = newTextureName.Replace(oldColor, newColor);

                var textureAux = TgcTexture.createTexture(D3DDevice.Instance.Device, newTextureName.Split('\\')[5], newTextureName);
                player1.tgcMesh.addDiffuseMap(textureAux);
                player1.tgcMesh.deleteDiffuseMap(index, 4); //de donde sale el 4?
            }

            // Mirar hacia atras
            if (gameModel.Input.keyDown(Key.C) || gameModel.JoystickButtonDown(4))
            {
                mirarHaciaAtras = true;
                halfsPI = 0;
            }
            else
                mirarHaciaAtras = false;

            // Rotar 90° la cámara
            if (gameModel.Input.keyPressed(Key.F5))
            {
                halfsPI = (halfsPI + FastMath.PI_HALF) % FastMath.TWO_PI;
            }

            // Modo cámara
            if (gameModel.Input.keyPressed(Key.V))
            {
                modoCamara = modosCamara.getNextOption(modoCamara);

                camaraInterna.OffsetHeight = modoCamara.AlturaCamara();
                camaraInterna.OffsetForward = modoCamara.ProfundidadCamara();
            }

            if (gameModel.Input.keyPressed(Key.Z))
            {
                ammoQuantity.Text = (int.Parse(ammoQuantity.Text) + 1).ToString();
            }

            // Hacer que la cámara apunte a nuestro Player 1
            camaraInterna.Target = new TGCVector3(player1.rigidBody.CenterOfMassPosition);
            camaraInterna.RotationY = Quat.ToEulerAngles(player1.rigidBody.Orientation).Y + anguloCamara + halfsPI + (mirarHaciaAtras ? FastMath.PI : 0);

            // Actualizar el Vector UP si se dibuja
            if (drawUpVector)
            {
                directionArrow.PStart = new TGCVector3(player1.rigidBody.CenterOfMassPosition);
                directionArrow.PEnd = directionArrow.PStart + new TGCVector3(Vector3.TransformNormal(Vector3.UnitY, player1.rigidBody.InterpolationWorldTransform)) * 3.5f;
                directionArrow.updateValues();
            }

            // Actualizar el mundo físico
            player1 = physicsEngine.Update(gameModel, camaraInterna, modoCamara);

            // Actualizamos la barra de especial
            specialBar.Scaling = new TGCVector2(specialScale.X * (player1.specialPoints / 100f), specialScale.Y);
            healthBar.Scaling = new TGCVector2(hpScale.X * (player1.hitPoints / 100f), hpScale.Y);

            // Actualizar los stats
            if (player1.specialPoints < 100)
            {
                player1.specialPoints += gameModel.ElapsedTime;
            }
            else
            {
                player1.specialPoints = 100;
            }
        }

        public void Render()
        {
            if (player1.hitPoints <= 0)
            {
                gameModel.Exit();
                return;
            }

            drawer2D.BeginDrawSprite();
            drawer2D.DrawSprite(statsBar);
            drawer2D.DrawSprite(healthBar);
            drawer2D.DrawSprite(specialBar);
            drawer2D.DrawSprite(weaponsHud);
            drawer2D.EndDrawSprite();

            speed.Text = player1.linealVelocity;

            if (player1.linealVelocity.Contains("-"))
            {
                speed.Color = Color.IndianRed;
            }
            else
            {
                speed.Color = Color.Green;
            }

            speed.render();
            km.render();
            actualWeapon.render();

            border.Text = ammoQuantity.Text;
            border.render();
            ammoQuantity.render();
            
            
            // Texto en pantalla sobre los comandos disponibles
            var DrawText = gameModel.DrawText;
            //DrawText.drawText("Con la tecla F1 se dibuja el bounding box (Deprecado, las colisiones las maneja Bullet)", 3, 20, Color.YellowGreen);
            //DrawText.drawText("Con la tecla F2 se rota el ángulo de la cámara", 3, 35, Color.YellowGreen);
            //DrawText.drawText("Con la tecla F3 se dibuja el Vector UP del vehículo", 3, 50, Color.YellowGreen);
            //DrawText.drawText("Con la tecla V se cambia el modo de cámara (NORMAL, LEJOS, CERCA)", 3, 65, Color.YellowGreen);
            //DrawText.drawText("W A S D para el movimiento básico", 3, 80, Color.YellowGreen);
            //DrawText.drawText("Control Izquierdo para frenar", 3, 95, Color.YellowGreen);
            //DrawText.drawText("Tecla ESPACIO para saltar", 3, 110, Color.YellowGreen);
            //DrawText.drawText("Tecla C para mirar hacia atrás", 3, 125, Color.YellowGreen);


            // Texto en pantalla sobre el juego
            //DrawText.drawText(player1.linealVelocity + " Km", (int)(screenWidth * 0.898f), (int)(screenHeight * 0.931f), Color.Black);


            //if (player1.flippedTime > 0)
            //{
            //    DrawText.drawText("Tiempo dado vuelta: " + player1.flippedTime, 15, screenHeight - 35, Color.White);
            //}

            // Renderiza todo lo perteneciente al mundo físico
            physicsEngine.Render(gameModel);

            // Renderizar el Vector UP
            if (drawUpVector)
            {
                directionArrow.Render();
            }

            // Finalizar el dibujado de Sprites
            //
        }

        public void Dispose()
        {
            physicsEngine.Dispose();
            directionArrow.Dispose();
            player1.rigidBody.Dispose();
            statsBar.Dispose();
            healthBar.Dispose();
            specialBar.Dispose();
        }
    }
}