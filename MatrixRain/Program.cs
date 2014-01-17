using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable AccessToDisposedClosure
namespace MatrixRain
{
    class Program
    {
       
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new Rain())
            {
                game.Run(60.0);
            }
        }
    }
}
