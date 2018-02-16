using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

namespace MarioKart.Bot.NET
{
    public class ImgWnd : System.Windows.Forms.Form
    {
        public Bitmap m_img;
        public ImgWnd()
        {
            this.SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.DoubleBuffer,
            true);
            this.Paint += ImgWnd_Paint;
        }

        private void ImgWnd_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if(m_img!=null)
                g.DrawImage(m_img, 0f, 0f);
        }

        public void LoadImage(Bitmap bmp)
        {
            m_img = bmp;
            Invalidate();
        }
    }
}
