using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace EldenRingAutoFPSUnlocker.utils
{
  public class OverlayWindow : Form
  {
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOPMOST = 0x8;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int ULW_ALPHA = 0x02;

    internal struct BLENDFUNCTION
    {
      public byte BlendOp;
      public byte BlendFlags;
      public byte SourceConstantAlpha;
      public byte AlphaFormat;
    }

    private readonly Bitmap overlayBitmap;
    private readonly Graphics graphics;
    private readonly Font overlayFont;
    private readonly SolidBrush textBrush;

    private static readonly Rectangle DARK_BOUNDS = new Rectangle(20, 20, 365, 80);
    private static readonly Color TEXT_COLOR = Color.FromArgb(125, 184, 5);
    private const string FONT_NAME = "Consolas";
    private const int TEXT_SIZE = 18;
    private const float DARK_OPACITY_PERCENT = 0.65f;

    private static string text = "";
    private readonly System.Windows.Forms.Timer overlayTimer;

    public OverlayWindow(string text_, int time)
    {
      text = text_;

      overlayTimer = new System.Windows.Forms.Timer
      {
        Interval = time
      };
      overlayTimer.Tick += OverlayTimerEnd;
      overlayTimer.Start();

      FormBorderStyle = FormBorderStyle.None;
      Bounds = Screen.PrimaryScreen.Bounds;
      TopMost = true;
      ShowInTaskbar = false;

      overlayFont = new Font(FONT_NAME, TEXT_SIZE, FontStyle.Regular);
      textBrush = new SolidBrush(TEXT_COLOR);

      overlayBitmap = new Bitmap(Width, Height);
      graphics = Graphics.FromImage(overlayBitmap);
    }

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);
      IntPtr handle = Handle;

      int extendedStyle = DllAPI.GetWindowLong(handle, GWL_EXSTYLE);
      DllAPI.SetWindowLong(handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW);

      DrawOverlay();
    }

    private void DrawOverlay()
    {
      using (SolidBrush semiTransparentBlackBrush = new SolidBrush(Color.FromArgb((int)(255 * DARK_OPACITY_PERCENT), 0, 0, 0)))
      {
        graphics.FillRectangle(semiTransparentBlackBrush, DARK_BOUNDS);
      }

      graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
      graphics.DrawString(text, overlayFont, textBrush, new PointF(DARK_BOUNDS.X * 2, DARK_BOUNDS.Y + (DARK_BOUNDS.Height / 2) - (TEXT_SIZE * 0.85f)));

      UpdateOverlay();
    }

    private void UpdateOverlay()
    {
      IntPtr screenDC = DllAPI.GetDC(IntPtr.Zero);
      IntPtr memDC = DllAPI.CreateCompatibleDC(screenDC);
      IntPtr hBitmap = overlayBitmap.GetHbitmap(Color.FromArgb(0));
      IntPtr oldBitmap = DllAPI.SelectObject(memDC, hBitmap);

      Size size = new Size(overlayBitmap.Width, overlayBitmap.Height);
      Point pointSource = new Point(0, 0);
      Point topPos = new Point(Left, Top);
      BLENDFUNCTION blend = new BLENDFUNCTION
      {
        BlendOp = 0,
        BlendFlags = 0,
        SourceConstantAlpha = 255,
        AlphaFormat = 1
      };

      DllAPI.UpdateLayeredWindow(Handle, screenDC, ref topPos, ref size, memDC, ref pointSource, 0, ref blend, ULW_ALPHA);

      DllAPI.SelectObject(memDC, oldBitmap);
      DllAPI.DeleteObject(hBitmap);
      DllAPI.DeleteDC(memDC);
      DllAPI.ReleaseDC(IntPtr.Zero, screenDC);
    }

    private void OverlayTimerEnd(object sender, EventArgs e)
    {
      overlayTimer.Stop();
      Close();
    }

    [STAThread]
    public static void DisplayOverlay(string text_, int time_)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new OverlayWindow(text_, time_));
    }
  }

  public static class OpenOverlayWindow
  {
    internal static void Open(string text, int time)
    {
      Thread thread = new Thread(() =>
        OverlayWindow.DisplayOverlay(text, time));

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();
    }
  }
}
