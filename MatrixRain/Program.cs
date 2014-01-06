using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

// ReSharper disable AccessToDisposedClosure
namespace MatrixRain
{
    class Program
    {
       
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Game1())
            {
                game.Run(60.0);
            }
        }
    }

    public class Row
    {
        public char[] Letters;
        public int Position;
        public int X;
        public int Y;
    }


    public class Game1 : GameWindow
    {
        private Bitmap _bitmap;
        private int _texture;
        //private readonly Font _font = new Font("Bookshelf Symbol 7", 12);
        private readonly Font _font = new Font("Katakana", 14);
        private Brush _brush = new SolidBrush(Color.ForestGreen);
        private readonly List<Brush> _head;
        private readonly List<Brush> _tail; 
        
        private readonly List<Row> _rows;
        private const int Total = 200;
        private DateTime _lastUpdate = DateTime.Now;
        private readonly int[] _slots = new int[Total];
        private const int Steps = 10;

        private int _activated;

        public Game1()
            : base(800, 600, GraphicsMode.Default, "Sample")
        {
            VSync = VSyncMode.On;
            WindowBorder = WindowBorder.Fixed;
            _rows = new List<Row>(Total);
            var measure = TextRenderer.MeasureText("i", _font);
            // Random pixel placement
            for (var i = 0; i < Total; i++)
            {
                _slots[i] = (i*(measure.Width-4)); //+ _rng.Next(1, 5);
            }

            // Ensure starting positions are random
            _slots = _slots.OrderBy(x => _rng.Next()).ToArray();

            _head = new List<Brush>();
            _tail = new List<Brush>();

            for (var i = 0; i < Steps; i++)
            {
                _head.Add(
                    new SolidBrush(Color.FromArgb(0 + 255/Steps*(Steps - (i)), 255, 0 + 255/Steps*(Steps - i))));
                _tail.Add(new SolidBrush(Color.FromArgb(0, 255 - 255 / Steps * (Steps - i), 0)));
            }
        }

        private int CreateTexture()
        {
            int textureId;
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Replace);//Important, or wrong color on some computers
            Bitmap bitmap = _bitmap;
            GL.GenTextures(1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.Finish();
            bitmap.UnlockBits(data);
            return textureId;
        }

        public void UpdateText()
        {
            using (Graphics gfx = Graphics.FromImage(_bitmap))
            {
                gfx.Clear(Color.Black);
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                gfx.DrawString("Hello world", _font, _brush, new PointF(0, 0));
            }

            BitmapData data =
                _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _bitmap.Width, _bitmap.Height,
                PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            _bitmap.UnlockBits(data);
        }

        

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            _texture = CreateTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _bitmap.Width, _bitmap.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Draw();
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var height = _font.Height;
            base.OnUpdateFrame(e);
            if (Keyboard[Key.Escape]) Exit();
            if (_activated < Total)
            {
                _rows.Add(new Row());
                _rows[_activated].Letters = RandomString(_rng.Next(30, 80)).ToCharArray();
                _rows[_activated].X = _slots[_activated];
                _rows[_activated].Position = 0;
                _rows[_activated].Y = _rng.Next(-200, 200);
                _activated++;
            }
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 15) return;
            
            using (var gfx = Graphics.FromImage(_bitmap))
            {
                gfx.Clear(Color.Black);
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                foreach (var item in _rows)
                {
                    if (item.Position > item.Letters.Length)
                    {
                        item.Letters[item.Position - (item.Letters.Length + 1)] = Convert.ToChar(" ");
                    }
                    if (item.Position >= item.Letters.Length * 2)
                    {
                        item.Letters = RandomString(_rng.Next(30, 80)).ToCharArray();
                        item.Y = _rng.Next(-200, 200);
                        item.Position = 0;
                    }
                    item.Position++;
                    for (var i = 0; i < item.Letters.Length; i++)
                    {
                        _brush = new SolidBrush(Color.FromArgb(0, 255, 0));
                        if (item.Position < i) break;
                        if(item.Position - i < Steps)
                            _brush = _head[item.Position - i];

                        var tailEnd = item.Position - (item.Letters.Length - 8 - Steps + 1);
                        if (i <= tailEnd)
                            _brush = Brushes.Black;
                        else if(i < tailEnd + Steps && i > tailEnd)
                            _brush = _tail[i - tailEnd];
                        
                        gfx.DrawString(item.Letters[i].ToString(CultureInfo.InvariantCulture), _font, _brush, item.X, item.Y + (i * height));

                    }

                }

                // Blur
                /*var blurSize = 10;
                for (Int32 xx = 0; xx < _bitmap.Width; xx++)
                {
                    for (Int32 yy = 0; yy < _bitmap.Height; yy++)
                    {
                        Int32 avgR = 0, avgG = 0, avgB = 0;
                        Int32 blurPixelCount = 0;

                        // average the color of the red, green and blue for each pixel in the
                        // blur size while making sure you don't go outside the image bounds
                        for (Int32 x = xx; (x < xx + blurSize && x < _bitmap.Width); x++)
                        {
                            for (Int32 y = yy; (y < yy + blurSize && y < _bitmap.Height); y++)
                            {
                                Color pixel = _bitmap.GetPixel(x, y);

                                avgR += pixel.R;
                                avgG += pixel.G;
                                avgB += pixel.B;

                                blurPixelCount++;
                            }
                        }

                        avgR = avgR / blurPixelCount;
                        avgG = avgG / blurPixelCount;
                        avgB = avgB / blurPixelCount;

                        // now that we know the average for the blur size, set each pixel to that color
                        for (Int32 x = xx; x < xx + blurSize && x < _bitmap.Width; x++)
                            for (Int32 y = yy; y < yy + blurSize && y < _bitmap.Height; y++)
                                _bitmap.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                    }
                }*/

                BitmapData data = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _bitmap.Width, _bitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                _bitmap.UnlockBits(data);
                _lastUpdate = DateTime.Now;
            }
        }

        public void Draw()
        {
            GL.PushMatrix();
            GL.LoadIdentity();

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(0, ClientSize.Width, ClientSize.Height, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);

            GL.PushMatrix();//
            GL.LoadMatrix(ref orthoProjection);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.DstColor);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, _texture);


            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(_bitmap.Width, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(_bitmap.Width, _bitmap.Height);
            GL.TexCoord2(0, 1); GL.Vertex2(0, _bitmap.Height);
            GL.End();
            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

        #region Random numbers

        private static readonly Random _rng = new Random();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXY";

        private static string RandomString(int size)
        {
            var buffer = new char[size];

            for (var i = 0; i < size; i++)
            {
                buffer[i] = Chars[_rng.Next(Chars.Length)];
            }
            return new string(buffer);
        }

        #endregion
    }
}
