using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Threading.Tasks;

namespace LMNParticleSystem
{
    [Serializable]
    public class ParticleSystem
    {
        #region Particle Emitter
        /// <summary>
        /// Particle emitter class
        /// </summary>
        [Serializable]
        sealed class ParticleEmitter : IDisposable
        {

            [NonSerialized]
            private Random random;
            private Vector2 EmitterSize;
            [NonSerialized]
            private Texture2D ParticlesTexture;
            private int _particlesPerEmit;
            private float _timePerEmit;
            [NonSerialized]
            private float Timer;
            private EventArgs evtArgs;
            private Color ParticleColor;
            private Vector2 _particlesVelocity;
            private float _particleLifeTime;
            
            [XmlElement("Position")]
            public Vector2 EmitterPosition;
            [XmlElement("Path")]
            public string PathToTexture;
            public int MaxParticlesCount;
            [XmlElement("RandomColor")]
            public bool IsRandomColor;
            [XmlElement("RandomDirection")]
            public bool IsRandomDirection;

            public float ParticleAngularVelocity { get; set; }

            [XmlIgnore]
            public Particle[] ParticlesM { get; private set; } //Particles array

            public event EventHandler OnEmitterLoad;

            public event EventHandler OnEmitterCreate;

            public bool Enabled { get; set; }
            [XmlIgnore]
            public int CurrentParticlesCount { get; set; }

            [XmlElement("PerEmitParticlesCount")]
            public int ParticlesPerEmit
            {
                get { return _particlesPerEmit; }
                set
                {
                    if (value < 0)
                        _particlesPerEmit = 0;
                    else
                        _particlesPerEmit = value;
                }
            }

            public Vector2 Size
            {
                get { return EmitterSize; }
                set { EmitterSize.X = value.X < 0 ? 0 : value.X; EmitterSize.Y = value.Y < 0 ? 0 : value.Y; }
            }

            public Color ParticlesColor
            {
                get { return ParticleColor; }
                set
                {
                    ParticleColor.A = Math.Max((byte)0, (byte)value.A);
                    ParticleColor.R = Math.Max((byte)0, (byte)value.R);
                    ParticleColor.G = Math.Max((byte)0, (byte)value.G);
                    ParticleColor.B = Math.Max((byte)0, (byte)value.B);
                }
            }

            public Vector2 ParticlesVelocity
            {
                get { return _particlesVelocity; }
                set { _particlesVelocity = value; }
            }

            public float ParticleLifeTime
            {
                get { return _particleLifeTime; }
                set { _particleLifeTime = Math.Max(0, value); }
            }

            public float TimePerEmit
            {
                get { return _timePerEmit; }
                set { _timePerEmit = value > 0 ? value : 0; }
            }

            public ParticleEmitter()
            {
                random = new Random();
                IsRandomDirection = false;
                ParticlesColor = Color.White;
                Enabled = true;

                if (OnEmitterCreate != null)
                    OnEmitterCreate(this, null);
            }

            public void Update(GameTime gameTime)
            {
                if (Enabled)
                {
                    Timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if ((Timer > _timePerEmit) && (MaxParticlesCount > CurrentParticlesCount))
                    {
                        for (int i = 0; ParticlesPerEmit > i; i++)
                            Emit();                                     //Particle creation function
                        Timer = 0f;
                    }

                    for (int i = 0; i < MaxParticlesCount; i++)
                    {
                        if (ParticlesM[i] != null)
                            ParticlesM[i].Update(gameTime);
                    }
                }
            }


            public void Draw(SpriteBatch spriteBatch)
            {
                if (ParticlesTexture != null)
                {
                    for (int i = 0; i < MaxParticlesCount; i++)
                    {
                        if (ParticlesM[i] != null)
                            ParticlesM[i].Draw(spriteBatch);
                    }
                }

            }

            public void LoadContent(ContentManager contentManager)
            {
                try
                {
                    ParticlesTexture = contentManager.Load<Texture2D>(PathToTexture);
                }
                catch (FileNotFoundException)
                {
                    ParticlesTexture = contentManager.Load<Texture2D>(@"Texture\Particles\DefaultTexture");
                }
                catch (ArgumentNullException)
                {
                    ParticlesTexture = contentManager.Load<Texture2D>(@"Texture\Particles\DefaultTexture");
                }
                ParticlesM = new Particle[MaxParticlesCount];

                if (OnEmitterLoad != null)
                    OnEmitterLoad(this, null);
            }

            public void Emit()
            {
                if (MaxParticlesCount > CurrentParticlesCount)
                {
                    int index = 0;

                    for (int i = 0; i < ParticlesM.Length; i++)
                    {
                        if (ParticlesM[i] == null)
                        {
                            index = i;
                            break;
                        }
                    }

                    Particle particle = new Particle(new Vector2(random.Next((int)EmitterPosition.X, (int)EmitterPosition.X + (int)EmitterSize.X),
                        random.Next((int)EmitterPosition.Y, (int)EmitterPosition.Y + (int)EmitterSize.Y)), 0f, ParticlesTexture, 0.1f, this, index, IsRandomColor);

                    ParticlesM[index] = particle;
                }
            }

            public void Dispose()
            {

                Parallel.For(0, ParticlesM.Length, (i, j) =>
                {
                    if (ParticlesM[i] != null)
                    {
                        ParticlesM[i].Destroy(this, EventArgs.Empty);
                    }
                    i++;
                });

                ParticlesTexture.Dispose();
            }
        }

        #endregion

        #region Particle

        sealed class Particle
        {
            float _sizeX;
            float _sizeY;
            float _positionX;
            float _positionY;
            float _rotation;
            float _scale;
            float _lifeTime;
            float alphaDecreaseTime;
            float alphaDecreaseTimer;
            bool isVisible;
            [NonSerialized]
            float CurrentLifeTime;
            [NonSerialized]
            Rectangle rect;
            EventArgs EvtArgs;
            ParticleEmitter parentEmitter;
            [NonSerialized]
            Random random;
            [NonSerialized]
            Vector2 _velocity;

            [NonSerialized]
            private int index;

            [NonSerialized]
            public Texture2D texture;

            /// <summary>
            /// Particle current speed
            /// </summary>
            public Vector2 Speed { get; private set; }

            public float AngularVelocity { get; set; }

            /// <summary>
            /// Current angular speed
            /// </summary>
            public Vector2 AngularSpeed { get; private set; }

            /// <summary>
            /// Particle color effect
            /// </summary>
            public Color Color { get; set; }

            public float PositionX
            {
                get { return _positionX; }
                set
                {
                    _positionX = value;
                }
            }

            public float PositionY
            {
                get { return _positionY; }
                set
                {
                    _positionY = value;
                }
            }

            public Vector2 Velocity
            {
                get { return _velocity; }
                set { _velocity = value; }
            }

            public Vector2 Origin
            {
                get
                {
                    return new Vector2(texture.Width / 2, texture.Height / 2);
                }
                private set { }
            }

            public float Scale
            {
                get { return _scale; }
                set
                {
                    if (value < 0)
                        _scale = 1;
                    else
                        _scale = value;
                }
            }

            /// <summary>
            /// Particle actual width (multiplied by scale)
            /// </summary>
            public float Width
            {
                get
                {
                    if (texture != null)
                        return texture.Width * Scale;
                    else
                        return 0;

                }
                private set { _sizeX = value; }
            }

            /// <summary>
            /// Particle actual height (multiplied by scale)
            /// </summary>
            public float Height
            {
                get
                {
                    if (texture != null)
                        return texture.Width * Scale;
                    else
                        return 0;
                }
                private set { _sizeY = value; }
            }

            /// <summary>
            /// One particle life time
            /// </summary>
            public float LifeTime
            {
                get { return _lifeTime; }
                set
                {
                    if (value < 0)
                    {
                        _lifeTime = 0;
                    }
                    else
                    {
                        _lifeTime = value;
                    }
                }
            }

            public bool Visible
            {
                get
                {
                    return isVisible;
                }
            }

            public bool Enabled
            {
                get
                {
                    return parentEmitter.Enabled;
                }
            }

            /// <summary>
            /// When particle life time ends OnDeath event is call.
            /// </summary>
            public event EventHandler OnDeath;

            /// <summary>
            /// When particle appears OnBirth event is called.
            /// </summary>
            public event EventHandler OnBirth;
            public event EventHandler<EventArgs> VisibleChanged;
            public event EventHandler<EventArgs> EnabledChanged;

            public Particle(Vector2 position, float angularVelocity, Texture2D textre, float scale, ParticleEmitter parentEmit, int index, bool RandomColor = false)
            {
                parentEmitter = parentEmit;
                random = new Random();
                texture = textre;
                Scale = (float)random.NextDouble() / 2.5f;
                PositionX = position.X;
                PositionY = position.Y;
                Velocity = parentEmitter.ParticlesVelocity;
                this.index = index;

                if (parentEmitter.IsRandomDirection)
                    if (random.Next(0, 10) < 5)
                        Velocity = parentEmitter.ParticlesVelocity * -1;

                LifeTime = parentEmitter.ParticleLifeTime + random.Next(500);
                Width = textre.Width;
                Height = textre.Height;
                AngularVelocity = parentEmitter.ParticleAngularVelocity;


                if (RandomColor)
                    Color = new Color(random.Next(255), random.Next(255), random.Next(255), random.Next(240, 255));
                else
                    Color = parentEmit.ParticlesColor;
                _rotation = 0f;
                Origin = new Vector2((PositionX), (PositionY));


                if (OnBirth != null)
                    OnBirth(this, EvtArgs);

                OnDeath += Destroy;
                parentEmitter.CurrentParticlesCount++;
                //System.Diagnostics.Debug.Print(this.ToString() + " created");
                alphaDecreaseTime = LifeTime / Color.A;
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                spriteBatch.Draw(texture, new Vector2(PositionX, PositionY), null, Color, _rotation, new Vector2(texture.Width / 2, texture.Height / 2), Scale, SpriteEffects.None, Scale);
            }

            public void Update(GameTime gameTime)
            {
                CurrentLifeTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                alphaDecreaseTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                Color = new Color(Color, MathHelper.Lerp(1, 0, CurrentLifeTime / LifeTime));

                if (CurrentLifeTime > LifeTime)
                {
                    if (OnDeath != null)
                        OnDeath(this, EvtArgs);
                }

                /*
                if (PositionX > (new Viewport()).Width || PositionY > (new Viewport()).Height)
                {
                    if (OnDeath != null)
                        OnDeath(this, EvtArgs);
                }
                */

                if (!parentEmitter.IsRandomDirection)
                {
                    PositionX += (Velocity.X * (float)gameTime.ElapsedGameTime.TotalSeconds) * Scale;
                    PositionY += (Velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds) * Scale;
                }
                else
                {
                    {
                        PositionX += (Velocity.X * (float)gameTime.ElapsedGameTime.TotalSeconds) * Scale;
                        PositionY += (Velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds) * Scale;
                    }
                }
                _rotation += AngularVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            public void Destroy(object Sender, EventArgs eArgs)
            {
                parentEmitter.ParticlesM[index] = null;
                parentEmitter.CurrentParticlesCount--;

                /*
                Parallel.For(0, parentEmitter.ParticlesM.Length, (i, loopState) =>
                    {
                        if (parentEmitter.ParticlesM[i] != null)
                        {
                            if (parentEmitter.ParticlesM[i].Equals(Sender))
                            {
                                parentEmitter.ParticlesM[i] = null;

                                parentEmitter.CurrentParticlesCount--;
                                loopState.Stop();
                            }
                        }
                    });
                    */
            }
        }

        #endregion

        ContentManager contentManager;
        ParticleEmitter emitter;

        public Vector2 EmitterSize
        {
            get { return emitter.Size;  }
            set { emitter.Size = value; }
        }

        public Vector2 EmitterPosition
        {
            get { return emitter.EmitterPosition;  }
            set { emitter.EmitterPosition = value; }
        }

        public string ParticlesTexturePath
        {
            get { return emitter.PathToTexture; }
            set
            {
                emitter.Enabled = false;
                emitter.PathToTexture = value;
                if (contentManager != null)
                    emitter.LoadContent(contentManager);
                else
                    throw new NullReferenceException("LMNParticleSystem: content manager is referenced to null. LoadContent method must be called first.");
                emitter.Enabled = true;
            }
        }

        public int ParticlesPerEmit
        {
            get { return emitter.ParticlesPerEmit;  }
            set { emitter.ParticlesPerEmit = value; }
        }

        public float SecondsPerEmit
        {
            get { return emitter.TimePerEmit / 1000f;  }
            set { emitter.TimePerEmit = value * 1000f; }
        }

        public ParticleSystem()
        {
            emitter = new ParticleEmitter();
        }

        public Color ParticlesBlendingColor
        {
            get { return emitter.ParticlesColor;  }
            set { emitter.ParticlesColor = value; }
        }

        public float ParticlesLifeTimeSec
        {
            get { return emitter.ParticleLifeTime / 1000f;  }
            set { emitter.ParticleLifeTime = value * 1000f; }
        }

        public Vector2 ParticlesVelocity
        {
            get { return emitter.ParticlesVelocity;  }
            set { emitter.ParticlesVelocity = value; }
        }

        public float ParticlesAngularVelocity
        {
            get { return emitter.ParticleAngularVelocity;  }
            set { emitter.ParticleAngularVelocity = value; }
        }

        public int MaxParticlesCount
        {
            get { return emitter.MaxParticlesCount;  }
            set { emitter.MaxParticlesCount = value; }
        }

        public bool IsEnabled
        {
            get { return emitter.Enabled; }
        }

        public void SetEmitterSize(Vector2 newSize)
        {
            emitter.Size = newSize;
        }

        public void SetEmitterSize(int newSizeX, int newSizeY)
        {
            emitter.Size = new Vector2(newSizeX, newSizeY);
        }

        public void LoadContent(ContentManager manager)
        {
            contentManager = manager;
            emitter.LoadContent(contentManager);
        }

        public void Update(GameTime gameTime)
        {
            emitter.Update(gameTime);
        }

        public void Draw(SpriteBatch sb)
        {
            emitter.Draw(sb);
        }

        public void SwitchState()
        {
            emitter.Enabled = !emitter.Enabled;
        }
    }
}
