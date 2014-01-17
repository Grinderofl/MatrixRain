using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace MatrixRain
{
    public class Rain : GameWindow
    {
        private Bitmap _bitmap;
        private int _texture;
        //private readonly Font _font = new Font("Bookshelf Symbol 7", 12);
        private readonly Font _font = new Font("Katakana", 20);
        //private readonly Font _font = new Font("Matrix Code NFI", 20);
        private Brush _brush = new SolidBrush(Color.ForestGreen);
        private readonly List<Brush> _head;
        private readonly List<Brush> _tail; 
        
        private readonly List<Row> _rows;
        private const int Total = 158;
        private DateTime _lastUpdate = DateTime.Now;
        private DateTime _lastSlowUpdate = DateTime.Now;

        private readonly int[] _slots = new int[Total];
        private const int Steps = 10;

        private int _activated;

        public Rain()
            : base(1024, 768, GraphicsMode.Default, "Sample")
        {
            VSync = VSyncMode.On;
            //WindowBorder = WindowBorder.Fixed;
            
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
            _weighs = new Dictionary<string, int>();
            InitWeightedRandomList();

        }

        private void InitWeightedRandomList()
        {
            //_weighs.Add("abcdefghijklmnopqrstuvwxyz", 5);
            _weighs.Add("ABCDEFGHIJKLMNOPQRSTUVWXY", 5);
            _weighs.Add("abcdefghijklm", 5);
            //_weighs.Add("$+-*/%=\"'#&_(),.?!\\^~][{}|", 5);
            _weighs.Add("0123456789", 1);
            _weighs.Add(" ", 5);

            Chars = "";
            foreach (var item in _weighs)
            {
                for (var i = 0; i < item.Value; i++)
                {
                    Chars += item.Key;
                }
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //WindowState = WindowState.Maximized;
            _bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            _texture = CreateTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _bitmap.Width, _bitmap.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(ClientRectangle);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);

            _bitmap.Dispose();
            _bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            BitmapData data = _bitmap.LockBits(new System.Drawing.Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.Finish();
            _bitmap.UnlockBits(data);
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

            bool slowUpdate = false;
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 15)
            {
                slowUpdate = true;
                _lastSlowUpdate = DateTime.Now;
            }

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

                    if (_rng.Next(0, 10) == 1)
                    {
                        var addChange = _rng.Next(item.Letters.Length);
                        if(!item.Changes.ContainsKey(addChange))
                            item.Changes.Add(addChange,
                                new KeyValuePair<char, int>(RandomString(1)[0], Steps - 1));
                    }
                        

                    if (item.Position >= item.Letters.Length * 2)
                    {
                        item.Letters = RandomString(_rng.Next(30, 80)).ToCharArray();
                        item.Y = _rng.Next(-200, 200);
                        item.Position = 0;
                        item.Changes = new Dictionary<int, KeyValuePair<char, int>>();
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

                        if (item.Changes.ContainsKey(i))
                        {
                            item.Letters[i] = item.Changes[i].Key;
                            if (item.Changes[i].Value > 0)
                            {
                                _brush = _head[Steps - item.Changes[i].Value];
                            }
                            else if (item.Changes[i].Value > -Steps+1)
                            {
                                _brush = _tail[Steps + item.Changes[i].Value - 1];
                            }
                            if(slowUpdate)
                                item.Changes[i] = new KeyValuePair<char, int>(item.Letters[i], item.Changes[i].Value - 1);
                        }

                        if(_brush != Brushes.Black)
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

        private string _debugInfo1 = "";

        public void Draw()
        {
            GL.PushMatrix();
            GL.LoadIdentity();

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(0, ClientSize.Width, ClientSize.Height, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);

            GL.PushMatrix();//
            GL.LoadMatrix(ref orthoProjection);

            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.DstColor);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            var debugInfo1 = string.Format("New dimensions rendered: {0}x{1}, Client: {2}x{3}", _bitmap.Width, _bitmap.Height, ClientSize.Width, ClientSize.Height);
            if (debugInfo1 != _debugInfo1)
            {
                _debugInfo1 = debugInfo1;
                Console.WriteLine(debugInfo1);
            }

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(_bitmap.Width, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(_bitmap.Width, _bitmap.Height);
            GL.TexCoord2(0, 1); GL.Vertex2(0, _bitmap.Height);
            GL.End();
            GL.PopMatrix();

            //GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

        #region Random numbers

        private static readonly Random _rng = new Random();
        private string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXY";

        private readonly Dictionary<string, int> _weighs;

        private string RandomString(int size)
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