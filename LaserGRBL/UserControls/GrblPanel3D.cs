using LaserGRBL.Obj3D;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Cameras;
using SharpGL.Version;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using Tools;
using static LaserGRBL.ProgramRange;
using LaserGRBL.Camera;
using LaserGRBL.LightBurn;

namespace LaserGRBL.UserControls
{

    [ToolboxBitmap(typeof(SceneControl), "GrblScene")]
	public partial class GrblPanel3D : UserControl, IGrblPanel
	{
		// last mouse position
		private Point? mLastMousePos = null;
		// current mouse world position
        private Point? mMousePos = null;
        // current mouse world position
        private PointF? mMouseWorldPosition { get; set; }

		private Panel pnlPositionBar;
		private Button btnTogglePosBar;
		private NumericUpDown numPosX;
		private NumericUpDown numPosY;
		private Button btnLaser1Pct;
		private bool mIsDraggingJob = false;
		private PointF? mDragJobStartWorldPos = null;
		private PointF mDragJobCurrentOffset = PointF.Empty;
		private bool mJobMoveMode = false;
		private bool mIsLaserGuideOn = false;
		// main camera
		private OrthographicCamera mCamera { get; } = new OrthographicCamera();
		private GLColor mBackgroundColor { get; set; }
		private GLColor mTicksColor { get; set; }
		private GLColor mMinorsColor { get; set; }
		private GLColor mOriginsColor { get; set; }
		private GLColor mPointerColor { get; set; }
		private GLColor mTextColor { get; set; }
        private GLColor mTextBoundingColor { get; set; }
        // default font
        private const string mFontName = "Courier New";
        private Font mTextFont { get; set; } = new Font(mFontName, 12);
		// background
		private Grid3D mGrid = null;
		// grbl object
		private Grbl3D mGrbl3D = null;
		private Grbl3D mGrbl3DOff = null;
		private object mGrbl3DLock = new object();
		private string mMessage = null;
        // reload request
        private bool mReload = false;
		// invalidate all request
		private bool mInvalidateAll = false;
		// viewport padding
		private Padding mPadding = new Padding(70, 0, 0, 30);
		// grbl core
		private GrblCore Core;
		public float PointerSize { get; set; } = 3;
		// last control size
		private PointF mLastControlSize;
		// drawing thread
		private Tools.ThreadObject mThreadDraw;
		// generated 3d bitmap
		private DoubleBufferBmp mBmp = new DoubleBufferBmp();
		// open gl object
		private OpenGL OpenGL;
		private GPoint mLastWPos;
		private GPoint mLastMPos;
		private float mCurF;
		private float mCurS;
		bool forcez = false;
		private bool mFSTrig;

		private Base.Mathematics.MobileDAverageCalculator FrameTime;
		private Base.Mathematics.MobileDAverageCalculator SleepTime;
		private Base.Mathematics.MobileDAverageCalculator RefreshRate;

		private static Exception FatalException;

		private bool mShowCursor = true;
		public bool ShowCursor
		{
            get => mShowCursor;
            set
			{
				if (mShowCursor != value)
                {
                    mShowCursor = value;
					if (value)
					{
						Cursor.Show();
					}
					else
					{
						Cursor.Hide();
					}
                }
            }
        }

		// 0 = never run, 1 = init begin, 2 = create complete, 3 = init complete, 4 = draw begin, 5 = draw end, > 5 = running (can be tested with a timer to check if it stop incrementing)
		private static ulong OpCounter;

		public static string CurrentVendor = "";
		public static string CurrentRenderer = "";
		public static string CurrentGLVersion = "";

		private static string FirstGlError = null;

		public static string GlDiagnosticMessage
		{
			get
			{
				if (Settings.CurrentGraphicMode == Settings.GraphicMode.GDI) //we not use GrblPanel3D
					return null;

				if (FatalException != null)
					return FatalException.Message;
				else if (FirstGlError != null)
					return FirstGlError;
				else if (OpCounter < 5)
					return $"OpCounter {OpCounter}";

				return null;
			}
		}

		public AutoResetEvent RR = new AutoResetEvent(true);		//redraw required
		Tools.ThreadObject TH = null;	//drawing thread
		public GrblPanel3D()
		{
			InitializeComponent();
			OpCounter = 0;
			FrameTime = new Base.Mathematics.MobileDAverageCalculator(10);
			SleepTime = new Base.Mathematics.MobileDAverageCalculator(10);
			RefreshRate = new Base.Mathematics.MobileDAverageCalculator(3);
			mLastWPos = GPoint.Zero;
			mLastMPos = GPoint.Zero;
			forcez = Settings.GetObject("Enale Z Jog Control", false);
			MouseDoubleClick += GrblPanel3D_DoubleClick;
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			MouseWheel += GrblSceneControl_MouseWheel;
			MouseDown += GrblSceneControl_MouseDown;
			MouseMove += GrblSceneControl_MouseMove;
			MouseUp += GrblSceneControl_MouseUp;
			MouseLeave += GrblSceneControl_MouseLeave;
			Grbl3D.OnLoadingPercentageChange += Grbl3D_OnLoadingPercentageChange;
			Resize += GrblPanel3D_Resize;
			Disposed += GrblPanel3D_Disposed;
			Camera.CameraOverlayManager.OnFrameUpdated += () => { try { if (IsHandleCreated) Invalidate(); } catch { } };
			mCamera.Position = new Vertex(0, 0, 0);
			mCamera.Near = 0;
			mCamera.Far = 100000000;
			AutoSizeDrawing();
			mLastControlSize = new PointF(Width, Height);
			mGrid = new Grid3D(mCamera);
			OnColorChange();
			TH = new Tools.ThreadObject(DrawScene, 10000, true, "OpenGL", InitializeOpenGL, ThreadPriority.Lowest, ApartmentState.STA, RR);
			TH.Start();

			/*
			// TEST JOG 
			Task.Factory.StartNew(() => {
				Random RNG = new Random();
				while (true)
				{
					Core?.JogToPosition(new PointF(50 - RNG.Next(100), 50 - RNG.Next(100)), false);
					if (RNG.Next(20) == 10)
						Core?.JogAbort();
					if (RNG.Next(20) == 10)
						Thread.Sleep(10);
				}
			});
			*/
			InitializePositioningBar();
		}

		private static double GetRulerStep(double n)
        {
			n = Math.Abs(n);

            int digitCount = Convert.ToInt32(Math.Max(3, Math.Floor(Math.Log10(n) + 1)));
            int power = Convert.ToInt32(Math.Pow(10, digitCount - 1));
            double step = n / power;
            double result;
            if (step < 1)
                result = 5;
            else if (step < 2)
                result = 10;
            else if (step < 6)
                result = 30;
            else
                result = 50;
            return result * Math.Pow(10, digitCount - 3);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				try
				{
					TH?.Stop();
					TH?.Dispose();
					TH = null;
				} catch { }
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		[System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
		protected void InitializeOpenGL()
		{
			try
			{
				OpCounter++;

				object parameter = null;
				OpenGL = new OpenGL();


				if (Settings.RequestedGraphicMode == Settings.GraphicMode.DIB)
				{
					Settings.CurrentGraphicMode = Settings.GraphicMode.DIB;
					OpenGL.Create(OpenGLVersion.OpenGL2_1, RenderContextType.DIBSection, Width, Height, 32, parameter);
					CheckError(OpenGL, "Create");
				}
				else // if we are here the requested is AUTO or FBO so we can proceed with FBO and fallback to DIB
				{
					try
					{
						Settings.CurrentGraphicMode = Settings.GraphicMode.FBO;
						OpenGL.Create(OpenGLVersion.OpenGL2_1, RenderContextType.FBO, Width, Height, 32, parameter);
						if (OpenGL.GetError() != OpenGL.GL_NO_ERROR) throw new Exception("Cannot Create FBO");
					}
					catch
					{
						Settings.CurrentGraphicMode = Settings.GraphicMode.DIB;
						OpenGL.Create(OpenGLVersion.OpenGL2_1, RenderContextType.DIBSection, Width, Height, 32, parameter);
						CheckError(OpenGL, "Create");
					}
				}

				OpCounter++;

				try { CurrentVendor = OpenGL.Vendor; } catch { CurrentVendor = "Unknown"; }
				try { CurrentRenderer = OpenGL.Renderer; } catch { CurrentRenderer = "Unknown"; }
				try { CurrentGLVersion = OpenGL.Version; } catch { CurrentGLVersion = "0.0"; }

				Logger.LogMessage("OpenGL", "{0}->{1} OpenGL {2}, {3}, {4}", Settings.RequestedGraphicMode, Settings.CurrentGraphicMode, CurrentGLVersion, CurrentVendor, CurrentRenderer);

				OpenGL.ShadeModel(OpenGL.GL_SMOOTH);
				CheckError(OpenGL, "ShadeModel");
				OpenGL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
				OpenGL.ClearDepth(1.0f);
				OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
				CheckError(OpenGL, "DepthTest");
				OpenGL.DepthFunc(OpenGL.GL_LEQUAL);
				CheckError(OpenGL, "DepthFunc");
				OpenGL.Hint(OpenGL.GL_PERSPECTIVE_CORRECTION_HINT, OpenGL.GL_NICEST);
				CheckError(OpenGL, "Hint");

				OpCounter++;
			}
			catch (Exception ex)
			{
				Logger.LogException("OpenGL", ex);
				FatalException = ex;
				Invalidate();
				ExceptionManager.OnHandledException(ex, true);
			}
		}

		[System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
		private void DrawScene()
		{
			try
			{
				if (FatalException != null) return;

				OpCounter++;
				Tools.SimpleCrono crono = new Tools.SimpleCrono(true);
				OpenGL.MakeCurrent();
				CheckError(OpenGL, "MakeCurrent");
				OpenGL.SetDimensions(Width, Height);
				OpenGL.Viewport(0, 0, Width, Height);
				mCamera.Project(OpenGL);
				CheckError(OpenGL, "Viewport");
				OpenGL.ClearColor(mBackgroundColor.R, mBackgroundColor.G, mBackgroundColor.B, mBackgroundColor.A);
				OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
				CheckError(OpenGL, "Clear");
				// render grid
				mGrid.ControlWidth = Width;
				mGrid.TicksColor = mTicksColor;
				mGrid.MinorsColor = mMinorsColor;
				mGrid.OriginsColor = mOriginsColor;
				mGrid.Render(OpenGL);
				CheckError(OpenGL, "RenderGrid");
				// enable anti alias
				OpenGL.Enable(OpenGL.GL_BLEND);
				OpenGL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
				CheckError(OpenGL, "Blend");
				OpenGL.Enable(OpenGL.GL_LINE_SMOOTH);
				OpenGL.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
				CheckError(OpenGL, "Smooth");
				// manage grbl object
				if (mReload)
				{
					mReload = false;
					Grbl3D oldGrbl3D = mGrbl3D;
                    Grbl3D oldGrbl3DOff = mGrbl3DOff;
                    lock (mGrbl3DLock)
                    {
                        mGrbl3D = null;
                        mGrbl3DOff = null;
					}
					mMessage = Strings.ClearDrawing;
					oldGrbl3D?.Dispose();
                    oldGrbl3DOff?.Dispose();
					mMessage = Strings.PrepareDrawing;
                    Grbl3D newGrbl3D = new Grbl3D(Core, "LaserOn", false, ColorScheme.PreviewLaserPower, ColorScheme.PreviewBackColor, ColorScheme.PreviewJobRange);
                    Grbl3D newGrbl3DOff = new Grbl3D(Core, "LaserOff", true, ColorScheme.PreviewOtherMovement, ColorScheme.PreviewBackColor, ColorScheme.PreviewJobRange);
                    mMessage = null;
                    lock (mGrbl3DLock)
					{
						mGrbl3D = newGrbl3D;
						mGrbl3DOff = newGrbl3DOff;
                    }
                    mGrbl3DOff.LineWidth = 1;
				}
				if (mGrbl3D != null)
				{
					if (mInvalidateAll)
					{
						mInvalidateAll = false;
						mGrbl3D.Color = ColorScheme.PreviewLaserPower;
						mGrbl3D.BackgroundColor = ColorScheme.PreviewBackColor;
                        mGrbl3D.BoundingBoxColor = ColorScheme.PreviewJobRange;
                        mGrbl3D.InvalidateAll();
						mGrbl3DOff.Color = ColorScheme.PreviewOtherMovement;
                        mGrbl3DOff.BackgroundColor = ColorScheme.PreviewBackColor;
                        mGrbl3DOff.InvalidateAll();
					}
					if (Core.ShowLaserOffMovements.Value)
					{
						mGrbl3DOff.Invalidate();
						mGrbl3DOff.Render(OpenGL);
					}
					mGrbl3D.ShowBoundingBox = Core.ShowBoundingBox.Value;
                    mGrbl3D.Invalidate();
					mGrbl3D.Render(OpenGL);
				}
				CheckError(OpenGL, "RenderObject");
				// disable anti alias
				OpenGL.Disable(OpenGL.GL_BLEND);
				OpenGL.Disable(OpenGL.GL_LINE_SMOOTH);
				CheckError(OpenGL, "BlendDisable");
				// main hud
				DrawPointer();
				CheckError(OpenGL, "Pointer");
				DrawRulers();
				CheckError(OpenGL, "Rulers");
				OpenGL.Flush();
				CheckError(OpenGL, "Flush");
				Bitmap newBmp = new Bitmap(Width, Height);
				// clone opengl graphics
				using (Graphics g = Graphics.FromImage(newBmp))
				{
					IntPtr handleDeviceContext = g.GetHdc();
					OpenGL.Blit(handleDeviceContext);
					CheckError(OpenGL, "Blit");
					g.ReleaseHdc(handleDeviceContext);
				}
				mBmp.Bitmap = newBmp;

				FrameTime.EnqueueNewSample(crono.ElapsedTime.TotalMilliseconds);

				crono.Start();
				Thread.Sleep(BestSleep(FrameTime.CurrentValue, 10, 100, 15, 50));
				SleepTime.EnqueueNewSample(crono.ElapsedTime.TotalMilliseconds);

				// call control invalidate
				Invalidate();

				OpCounter++;
			}
			catch (Exception ex)
			{
				Logger.LogException("OpenGL", ex);
				FatalException = ex;
				Invalidate();
				ExceptionManager.OnHandledException(ex, true);
			}
		}

		private static int BestSleep(double renderTime, double minRender, double maxRender, double minSleep, double maxSleep)
		{
			return (int) Math.Max(minSleep, Math.Min(maxSleep, (renderTime - minRender) * (maxSleep - minSleep) / (maxRender - minRender) + minSleep));
		}

		private void SetWorldPosition(double left, double right, double bottom, double top)
        {
			double maxViewport = Grid3D.ViewportSize * 2;
            if (right - left > maxViewport) return;
            if (bottom - top > maxViewport) return;
            if (left < - maxViewport / 2) {
				right += (-maxViewport / 2 - left);
				left = -maxViewport / 2;
            }
            if (right > maxViewport / 2)
            {
                left += (maxViewport / 2 - right);
                right = maxViewport / 2;
            }
            if (bottom < -maxViewport / 2)
            {
                top += (-maxViewport / 2 - bottom);
                bottom = -maxViewport / 2;
            }
            if (top > maxViewport / 2)
            {
                bottom += (maxViewport / 2 - top);
                top = maxViewport / 2;
            }
            // set new camera coords
            mCamera.Left = left;
			mCamera.Right = right;
			mCamera.Bottom = bottom;
			mCamera.Top = top;
			mGrid?.Invalidate();
			// call control invalidate
			RR.Set();
		}

		private void GrblPanel3D_Resize(object sender, EventArgs e)
		{
			// compute ratiobesed on last size
			double wRatio = Width / mLastControlSize.X;
			double hRatio = Height / mLastControlSize.Y;
			// compute increment
			double wIncrement = (mCamera.Right - mCamera.Left) - (mCamera.Right - mCamera.Left) * wRatio;
            double hIncrement = (mCamera.Top - mCamera.Bottom) - (mCamera.Top - mCamera.Bottom) * hRatio;
			double newLeft = mCamera.Left + wIncrement / 2;
			double newRight = mCamera.Right - wIncrement / 2;
			double newTop = mCamera.Top - hIncrement / 2;
			double newBottom = mCamera.Bottom + hIncrement / 2;
            double rw = 1;
            double rh = 1;
			double maxViewport = Grid3D.ViewportSize * 2;
            if (newRight - newLeft > maxViewport)
            {
                rw = maxViewport / (newRight - newLeft);
            }
            if (newBottom - newTop > maxViewport)
            {
                rh = maxViewport / (newBottom - newTop);
            }
			double r = Math.Min(rw, rh);
            newLeft = newLeft * r;
            newRight = newRight * r;
            newTop = newTop * r;
            newBottom = newBottom * r;
            // set world positions
            SetWorldPosition(newLeft, newRight, newBottom, newTop);
			// save last size
			mLastControlSize = new PointF(Width, Height);
		}

		private void GrblPanel3D_DoubleClick(object sender, MouseEventArgs e)
		{
			if (Settings.GetObject("Click N Jog", true) && mMouseWorldPosition != null)
			{
				Core.JogToPosition((PointF)mMouseWorldPosition, e.Button == MouseButtons.Right);
			}
		}

		static uint errcounter = 0;
		public static bool CheckError(OpenGL gl, string action)
		{
			bool rv = false;
			uint err = gl.GetError();
			uint loopCount = 0; //loop with count checks to prevent infinite loop in case of code errors
			while (err != OpenGL.GL_NO_ERROR && loopCount++ < 20) //glGetError should always be called in a loop, until it returns GL_NO_ERROR https://registry.khronos.org/OpenGL-Refpages/gl4/html/glGetError.xhtml
			{
				rv = true;
				if (errcounter < 3) //we store only first 3 error
				{
					errcounter++;
					string message = string.Format("[{0}] {1}: {2}", err, action, gl.GetErrorDescription(err));
					if (FirstGlError == null) FirstGlError = message; //we send only the first one
					Logger.LogMessage("OpenGL", message);
				}
				err = gl.GetError();
			}
			return rv;
		}

		private void GrblPanel3D_Disposed(object sender, EventArgs e)
		{
			DisposeGrbl3D();
			mThreadDraw?.Dispose();
		}

		private void DrawPointer()
		{
			if (Core == null) return;
			// draw laser cross
			GLColor color = new GLColor();
			color.FromColor(mPointerColor);
			OpenGL.Color(color);
			OpenGL.LineWidth(PointerSize);
			OpenGL.Begin(OpenGL.GL_LINES);
			Vertex pointerPos = new Vertex(Core.WorkPosition.X, Core.WorkPosition.Y, 0.2f);
			double size = mGrid.GridSize * 1.5;
			OpenGL.Vertex(pointerPos.X - size, pointerPos.Y, pointerPos.Z);
			OpenGL.Vertex(pointerPos.X + size, pointerPos.Y, pointerPos.Z);
			OpenGL.End();
			OpenGL.Begin(OpenGL.GL_LINES);
			OpenGL.Vertex(pointerPos.X, pointerPos.Y - size, pointerPos.Z);
			OpenGL.Vertex(pointerPos.X, pointerPos.Y + size, pointerPos.Z);
			OpenGL.End();
			OpenGL.Flush();
		}

		private string HumanReadableLength(int mm, double worldWidth, out string uom)
        {
            if (worldWidth > 10000)
            {
                uom = "m";
                return $"{Math.Round(mm / 1000.0, 0)}";
            }
            else if (worldWidth > 1000)
            {
                uom = "cm";
                return $"{Math.Round(mm / 10.0, 0)}";
			}
			else
			{
				uom = "mm";
				return $"{mm}";
			}
		}

		private SizeF MeasureOpenGlText(string text)
		{
			Size size = TextRenderer.MeasureText(text, mTextFont);
            return new SizeF(size.Width * 0.8f, size.Height * 0.8f);
        }

		private void DrawText(string text, double x, double y, GLColor color)
		{
            OpenGL.DrawText((int)x, (int)y, color.R, color.G, color.B, mTextFont.FontFamily.Name, mTextFont.Size, text);
        }

        public void DrawRulers()
		{
			// clear left ruler background
			OpenGL.Enable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.Scissor(0, 0, mPadding.Left, Height);
			OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
			OpenGL.Disable(OpenGL.GL_SCISSOR_TEST);
			// clear bottom ruler background
			OpenGL.Enable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.Scissor(0, 0, Width, mPadding.Bottom);
			OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
			OpenGL.Disable(OpenGL.GL_SCISSOR_TEST);
			// clear right ruler background
			OpenGL.Enable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.Scissor(Width - mPadding.Right, 0, mPadding.Right, Height);
			OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
			OpenGL.Disable(OpenGL.GL_SCISSOR_TEST);
			// clear top ruler background
			OpenGL.Enable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.Scissor(0, Height - mPadding.Top, Width, mPadding.Top);
			OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
			OpenGL.Disable(OpenGL.GL_SCISSOR_TEST);
			// get world width
			double worldWidth = mCamera.Right - mCamera.Left;
			// init rulers step
			double step = GetRulerStep(worldWidth);
			// half step
			int halfStep = (int)(step / 2);
			// get ratio
            double wRatio = Width / (mCamera.Right - mCamera.Left);
			// unit of measure
			string uom = string.Empty;
            HumanReadableLength(0, worldWidth, out uom); //call it just to be sure to have uom loaded by worldWidth
			int? minDrawingX = null;
            int? maxDrawingX = null;
            int? minDrawingY = null;
            int? maxDrawingY = null;
            if (Core?.LoadedFile?.Range.DrawingRange.ValidRange == true)
            {
                minDrawingX = (int)Core.LoadedFile.Range.DrawingRange.X.Min;
                maxDrawingX = (int)Core.LoadedFile.Range.DrawingRange.X.Max;
                minDrawingY = (int)Core.LoadedFile.Range.DrawingRange.Y.Min;
                maxDrawingY = (int)Core.LoadedFile.Range.DrawingRange.Y.Max;
            }
			bool showBoundingBox = Core?.ShowBoundingBox.Value ?? false;
            // draw horizontal
            for (int i = (int)mCamera.Left + (int)(mPadding.Left / wRatio); i <= (int)mCamera.Right - (int)(mPadding.Right / wRatio); i += 1)
			{
				bool canDraw = i % step == 0;
                if (showBoundingBox && (minDrawingX != null || maxDrawingX != null))
                {
					canDraw &=
						i < (minDrawingX - halfStep) ||
						(i > (minDrawingX + halfStep) && i < (maxDrawingX - halfStep)) ||
						i > (maxDrawingX + halfStep);
                    canDraw |= i == maxDrawingX || i == minDrawingX;
                }
                if (canDraw)
				{
					string text = HumanReadableLength(i, worldWidth, out uom);
					SizeF sizeProp = MeasureOpenGlText(text);
					double x = i * wRatio - mCamera.Left * wRatio - sizeProp.Width / 4f;
					double y = mPadding.Bottom - sizeProp.Height;
					DrawText(text, x, y, i == minDrawingX || i == maxDrawingX ? mTextBoundingColor : mTextColor);
                }
			}
			// get ratio
			double hRatio = Height / (mCamera.Top - mCamera.Bottom);
            // draw vertical
            for (int i = (int)mCamera.Bottom + (int)(mPadding.Bottom / hRatio); i <= (int)mCamera.Top - (int)(mPadding.Top / hRatio); i += 1)
            {
                bool canDraw = i % step == 0;
                if (showBoundingBox && (minDrawingY != null || maxDrawingY != null))
                {
                    canDraw &=
                        i < (minDrawingY - halfStep) ||
                        (i > (minDrawingY + halfStep) && i < (maxDrawingY - halfStep)) ||
                        i > (maxDrawingY + halfStep);
                    canDraw |= i == maxDrawingY || i == minDrawingY;
                }
                if (canDraw)
				{
					string text = HumanReadableLength(i, worldWidth, out uom);
					SizeF sizeProp = MeasureOpenGlText(text);
                    double x = mPadding.Left - sizeProp.Width;
					double y = i * hRatio - mCamera.Bottom * hRatio - sizeProp.Height / 4f;
                    DrawText(text, x, y, i == minDrawingY || i == maxDrawingY ? mTextBoundingColor : mTextColor);
                }
            }
			// clear uom  background
			OpenGL.Enable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.Scissor(0, 0, mPadding.Left, mPadding.Bottom);
			OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
			OpenGL.Disable(OpenGL.GL_SCISSOR_TEST);

			// draw unit of measure
			SizeF uomSizeProp = MeasureOpenGlText(uom);
			DrawText(uom, mPadding.Left - uomSizeProp.Width -20, mPadding.Bottom - uomSizeProp.Height, mTextBoundingColor);

            OpenGL.Flush();
		}

		Tools.SimpleCrono FpsCrono;
		protected override void OnPaint(PaintEventArgs e)
		{
			Exception OnPaintException = null;

			if (FpsCrono != null) RefreshRate.EnqueueNewSample(FpsCrono.ElapsedTime.TotalMilliseconds); //compute time distance between two OnPaint
			FpsCrono = new Tools.SimpleCrono(true);

            Bitmap bmp = mBmp.Bitmap;
            if (bmp != null)
			{
				try
				{
					e.Graphics.DrawImage(bmp, new Point(0, 0));
					DrawCameraOverlay(e);
					DoGDIDraw(e);
				}
				catch (Exception ex) { OnPaintException = ex;  }
			}
			else
			{
				// nothing to draw from GL Thread
			}

			if (FatalException != null)
				DrawException(e, FatalException.ToString());
			else if (OnPaintException != null)
				DrawException(e, OnPaintException.ToString());
			else if (OpCounter == 0)        // 0 = never run, 1 = init begin, 2 = create complete, 3 = init complete, 4 = draw begin, 5 = draw end, > 5 = running (can be tested with a timer to check if it stop incrementing)
				DrawException(e, "0.Thread Starting");
			else if (OpCounter == 1)
				DrawException(e, "1.Initializing OpenGL");
			else if (OpCounter == 2)
				DrawException(e, "2.Creation Complete");
			else if (OpCounter == 3)
				DrawException(e, "3.Init Complete");
			else if (OpCounter == 4)
				DrawException(e, "4.Draw Begin");
			else if (mBmp == null)
				;
		}

		private void DrawException(PaintEventArgs e, string text)
		{
			try
			{
				using (Font font = new Font(mFontName, 12))
				{
					Size size = MeasureText(text, mTextFont);
					Size size2 = new Size(size.Width + 20, size.Height);
					Point point = new Point((Width - size2.Width) / 2, (Height - size2.Height) / 2);
					DrawOverlay(e, point, size2, Color.Red, 100);
					e.Graphics.DrawString(text, font, Brushes.White, point.X, point.Y);
				}
			}
			catch { }
		}

		private string FormatCoord(float coord)
		{
			return string.Format("{0,10:######0.000}", coord);
		}

		private void DrawCameraOverlay(PaintEventArgs e)
		{
			if (!Camera.CameraOverlayManager.Enabled) return;
			using (Bitmap camFrame = Camera.CameraOverlayManager.WarpedFrame)
			{
				if (camFrame == null) return;

				try
				{
					float bedW = Camera.CameraOverlayManager.BedWidthMm * Camera.CameraOverlayManager.ScaleX;
					float bedH = Camera.CameraOverlayManager.BedHeightMm * Camera.CameraOverlayManager.ScaleY;
					float offX = Camera.CameraOverlayManager.OffsetX_Mm;
					float offY = Camera.CameraOverlayManager.OffsetY_Mm;

					// Map machine coordinates (offX, offY) and (offX + bedW, offY + bedH) to screen pixels
					float x0 = (float)((offX - mCamera.Left) / (mCamera.Right - mCamera.Left) * Width);
					float y0 = (float)(Height - (offY - mCamera.Bottom) / (mCamera.Top - mCamera.Bottom) * Height);
					float x1 = (float)((offX + bedW - mCamera.Left) / (mCamera.Right - mCamera.Left) * Width);
					float y1 = (float)(Height - (offY + bedH - mCamera.Bottom) / (mCamera.Top - mCamera.Bottom) * Height);

					Rectangle destRect = Rectangle.FromLTRB(
						(int)Math.Min(x0, x1),
						(int)Math.Min(y0, y1),
						(int)Math.Max(x0, x1),
						(int)Math.Max(y0, y1)
					);

					if (destRect.Width > 0 && destRect.Height > 0 && destRect.IntersectsWith(ClientRectangle))
					{
						float opacity = Math.Max(0.05f, Math.Min(1.0f, Camera.CameraOverlayManager.Opacity));
						ColorMatrix cm = new ColorMatrix();
						cm.Matrix33 = opacity;

						using (ImageAttributes ia = new ImageAttributes())
						{
							ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
							e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
							e.Graphics.DrawImage(camFrame, destRect, 0, 0, camFrame.Width, camFrame.Height, GraphicsUnit.Pixel, ia);
						}

						using (Pen bedPen = new Pen(Color.FromArgb((int)(opacity * 180), Color.Cyan), 1.5f) { DashStyle = DashStyle.Dash })
						{
							e.Graphics.DrawRectangle(bedPen, destRect);
						}
					}
				}
				catch { }
			}
		}

		private void DoGDIDraw(PaintEventArgs e)
		{
			if (Core == null) return;
			const int top = 12;
			using (Brush b = new SolidBrush(ColorScheme.PreviewText))
			using (Font font = new Font(mFontName, 10))
			{
				// set aliasing
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				// use z
				bool useZ = mLastWPos.Z != 0 || mLastMPos.Z != 0 || forcez;
				string text = "         X          Y";
				if (useZ) text += "          Z    ";
				// last position
				text += $"\nMCO {FormatCoord(mLastMPos.X)} {FormatCoord(mLastMPos.Y)}";
				if (useZ) text += $" {FormatCoord(mLastMPos.Z)}";
				// working offset
				if (Core.WorkingOffset != GPoint.Zero)
				{
					text += $"\nWCO {FormatCoord(mLastWPos.X)} {FormatCoord(mLastWPos.Y)}";
					if (useZ) text += $" {FormatCoord(mLastWPos.Z)}";
				}
				// mouse info
				if (mMouseWorldPosition != null)
				{
					PointF pos = (PointF)mMouseWorldPosition;
					text += $"\nPTR {FormatCoord(pos.X)} {FormatCoord(pos.Y)}";
					if (useZ) text += "          ";
				}
				Size size = MeasureText(text, font);
				Point point = new Point(Width - size.Width - mPadding.Right, top);
				DrawOverlay(e, point, size, ColorScheme.PreviewRuler, 100);
				e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
				e.Graphics.DrawString(text, font, b, point.X, point.Y);

				// laser info
				if (mCurF != 0 || mCurS != 0 || mFSTrig)
				{
					mFSTrig = true;
					int pos = point.Y + size.Height + 5;
					text = $"F {string.Format("{0,6:####0.##}", mCurF)}\nS {string.Format("{0,6:##0.##}", mCurS)}";
					size = MeasureText(text, font);
					point = new Point(Width - size.Width - mPadding.Right, pos);
					DrawOverlay(e, point, size, ColorScheme.PreviewRuler, 100);
					e.Graphics.DrawString(text, font, b, point.X, point.Y);
				}

				// performance
				if (Core.ShowPerformanceDiagnostic.Value)
				{
					int pos = point.Y + size.Height + 5;

					string VertexString = null;
					ulong VertexCounter = 0;
					lock (mGrbl3DLock)
					{VertexCounter = mGrbl3D != null ? mGrbl3D.VertexCounter : 0;}

					if (VertexCounter < 1000)
						VertexString = string.Format("{0,6:##0}", VertexCounter);
					else if (VertexCounter < 1000000)
						VertexString = string.Format("{0,6:###0.0} K", VertexCounter / 1000.0);
					else if (VertexCounter < 1000000000)
						VertexString = string.Format("{0,6:###0.0} M", VertexCounter / 1000000.0);
					else
						VertexString = string.Format("{0,6:###0.0} B", VertexCounter / 1000000000.0);

					text = $"VER   {VertexString}\nAFT   {string.Format("{0,6:##0} ms", FrameTime.Avg)}\nAST   {string.Format("{0,6:##0} ms", SleepTime.Avg)}\nRPS   {string.Format("{0,6:##0}", Math.Max(0, Math.Floor(1000.0 / RefreshRate.Avg - 1)))}";
					size = MeasureText(text, font);
					point = new Point(Width - size.Width - mPadding.Right, pos);
					DrawOverlay(e, point, size, ColorScheme.PreviewRuler, 100);
					e.Graphics.DrawString(text, font, b, point.X, point.Y);
                }

                // loading
                lock (mGrbl3DLock)
                {
                    if (mMessage != null || mGrbl3D != null && mGrbl3D.LoadingPercentage < 100 || Core.ShowLaserOffMovements.Value && mGrbl3DOff != null && mGrbl3DOff.LoadingPercentage < 100)
                    {
                        int pos = point.Y + size.Height + 5;
                        double? perc;
                        perc = mGrbl3D?.LoadingPercentage ?? 0;
                        perc += mGrbl3DOff?.LoadingPercentage ?? 0;
                        if (Core.ShowLaserOffMovements.Value) perc /= 2;
                        text = mMessage == null ? $"{Strings.Loading} {perc:0.0}%" : mMessage;
                        size = MeasureText(text, font);
                        point = new Point(Width - size.Width - mPadding.Right, pos);
                        DrawOverlay(e, point, size, ColorScheme.PreviewRuler, 100);
                        e.Graphics.DrawString(text, font, b, point.X, point.Y);
                    }
                }

                if (mGrbl3D == null)
				{
					text = $"{Strings.TipsBasicUsage}\r\n{Strings.TipsZoom}\r\n{Strings.TipsPan}\r\n{Strings.TipsJog}\r\n\r\n";
                    string shortcut = "";
					string a_asd = GetShortcut(HotKeysManager.HotKey.Actions.AutoSizeDrawing);
					string a_zin = GetShortcut(HotKeysManager.HotKey.Actions.ZoomInDrawing);
					string a_zou = GetShortcut(HotKeysManager.HotKey.Actions.ZoomInDrawing);
					if (!string.IsNullOrEmpty(a_asd)) shortcut += $"{GetShortcut(HotKeysManager.HotKey.Actions.AutoSizeDrawing)}: {Strings.TipsZoomAuto}\r\n";
					if (!string.IsNullOrEmpty(a_zin)) shortcut += $"{GetShortcut(HotKeysManager.HotKey.Actions.ZoomInDrawing)}: {Strings.TipsZoomIn}\r\n";
					if (!string.IsNullOrEmpty(a_zou)) shortcut += $"{GetShortcut(HotKeysManager.HotKey.Actions.ZoomOutDrawing)}: {Strings.TipsZoomOut}\r\n";
					if (!string.IsNullOrEmpty(shortcut))
						text = text + $"{Strings.TipsKeyboardShortcuts}\r\n" + shortcut;
                    text = text.Trim("\r\n".ToCharArray());
                    size = MeasureText(text, font);
					point = new Point(Width - size.Width - mPadding.Right, Height - size.Height - 35);
					DrawOverlay(e, point, size, ColorScheme.PreviewRuler, 60);
					e.Graphics.DrawString(text, font, b, point.X, point.Y);
				}

				if (Core.CrossCursor.Value &&
					mMousePos != null &&
					mMousePos.Value.X >= mPadding.Left &&
					mMousePos.Value.X <= Width - mPadding.Right &&
					mMousePos.Value.Y >= mPadding.Top &&
					mMousePos.Value.Y <= Height - mPadding.Bottom)
                {
					Color loShadowColor = Color.FromArgb(
						128,
						ColorScheme.PreviewBackColor.R,
                        ColorScheme.PreviewBackColor.G,
                        ColorScheme.PreviewBackColor.B
                    );
                    using (Pen pCross = new Pen(loShadowColor))
                    {
                        DrawCross(e.Graphics, pCross, new Point(mMousePos.Value.X + 1, mMousePos.Value.Y + 1));
                    }
                    using (Pen pCross = new Pen(ColorScheme.PreviewCrossCursor))
                    {
						DrawCross(e.Graphics, pCross, mMousePos.Value);
                        ShowCursor = false;
                    }
                }
				else
				{
					ShowCursor = true;
				}

				// Live ghost box when dragging job
				if (mIsDraggingJob && Core?.LoadedFile?.Range?.DrawingRange?.ValidRange == true)
				{
					float minX = (float)Core.LoadedFile.Range.DrawingRange.X.Min + mDragJobCurrentOffset.X;
					float maxX = (float)Core.LoadedFile.Range.DrawingRange.X.Max + mDragJobCurrentOffset.X;
					float minY = (float)Core.LoadedFile.Range.DrawingRange.Y.Min + mDragJobCurrentOffset.Y;
					float maxY = (float)Core.LoadedFile.Range.DrawingRange.Y.Max + mDragJobCurrentOffset.Y;

					float sx0 = (float)((minX - mCamera.Left) / (mCamera.Right - mCamera.Left) * Width);
					float sy0 = (float)(Height - (minY - mCamera.Bottom) / (mCamera.Top - mCamera.Bottom) * Height);
					float sx1 = (float)((maxX - mCamera.Left) / (mCamera.Right - mCamera.Left) * Width);
					float sy1 = (float)(Height - (maxY - mCamera.Bottom) / (mCamera.Top - mCamera.Bottom) * Height);

					Rectangle dragRect = Rectangle.FromLTRB(
						(int)Math.Min(sx0, sx1),
						(int)Math.Min(sy0, sy1),
						(int)Math.Max(sx0, sx1),
						(int)Math.Max(sy0, sy1)
					);

					using (Brush dragFill = new SolidBrush(Color.FromArgb(70, Color.DeepSkyBlue)))
					{
						e.Graphics.FillRectangle(dragFill, dragRect);
					}
					using (Pen dragPen = new Pen(Color.Cyan, 2f) { DashStyle = DashStyle.Dash })
					{
						e.Graphics.DrawRectangle(dragPen, dragRect);
					}

					string dragText = $"🎯 Posição: X={minX:0.0}, Y={minY:0.0} mm (ΔX={mDragJobCurrentOffset.X:+0.0;-0.0;0.0}, ΔY={mDragJobCurrentOffset.Y:+0.0;-0.0;0.0})";
					using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
					{
						Size txtSize = TextRenderer.MeasureText(dragText, f);
						Point txtPt = new Point(Math.Max(10, dragRect.Left), Math.Max(10, dragRect.Top - txtSize.Height - 6));
						DrawOverlay(e, txtPt, txtSize, Color.FromArgb(20, 20, 30), 200);
						e.Graphics.DrawString(dragText, f, Brushes.White, txtPt.X, txtPt.Y);
					}
				}
				else if (mJobMoveMode)
				{
					string modeText = "✋ Modo Mover Trabalho Ativo (Clique e arraste para posicionar)";
					using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
					{
						Size txtSize = TextRenderer.MeasureText(modeText, f);
						Point txtPt = new Point((Width - txtSize.Width) / 2, 10);
						DrawOverlay(e, txtPt, txtSize, Color.FromArgb(30, 60, 90), 210);
						e.Graphics.DrawString(modeText, f, Brushes.Cyan, txtPt.X, txtPt.Y);
					}
				}

			}
		}

		private void DrawCross(Graphics g, Pen pCross, Point point)
        {
			const int halfCrossSize = 4;
            g.DrawLine(pCross, new Point(mPadding.Left, point.Y), new Point(point.X - 5, point.Y));
            g.DrawLine(pCross, new Point(point.X + halfCrossSize, point.Y), new Point(Width - mPadding.Right, point.Y));
            g.DrawLine(pCross, new Point(point.X, mPadding.Top), new Point(point.X, point.Y - halfCrossSize));
            g.DrawLine(pCross, new Point(point.X, point.Y + halfCrossSize), new Point(point.X, Height - mPadding.Bottom));
            g.DrawRectangle(pCross, point.X - halfCrossSize, point.Y - halfCrossSize, halfCrossSize * 2, halfCrossSize * 2);
        }

		private string GetShortcut(HotKeysManager.HotKey.Actions action)
		{
			string shortcut = Core.GetHotKeyString(action);
			if (shortcut != null)
			{
				shortcut = shortcut.Replace("+", " ");
				shortcut = shortcut.Replace("Add", "+");
				shortcut = shortcut.Replace("Subtract", "-");
			}
			return shortcut;
		}

		private void DrawOverlay(PaintEventArgs e, Point point, Size size, Color color, int alfa)
		{
			Color c = Color.FromArgb(alfa, color);
			using (Brush brush = new SolidBrush(c))
			using (GraphicsPath path = Tools.Graph.RoundedRect(new Rectangle(point.X - 5, point.Y - 5, size.Width, size.Height), 7))
			{
				e.Graphics.FillPath(brush, path);
			}
		}

		private Size MeasureText(string text, Font font)
		{
			Size size = TextRenderer.MeasureText(text, font);
			size.Width += 10;
			size.Height += 5;
			return size;
		}

		private void GrblSceneControl_MouseLeave(object sender, EventArgs e)
		{
			mLastMousePos = null;
			mMouseWorldPosition = null;
			mMousePos = null;
			if (Core?.CrossCursor.Value == true) ShowCursor = true;
			Cursor.Current = Cursors.Default;
		}

		private void GrblSceneControl_MouseUp(object sender, MouseEventArgs e)
		{
			if (mIsDraggingJob)
			{
				mIsDraggingJob = false;
				float dx = mDragJobCurrentOffset.X;
				float dy = mDragJobCurrentOffset.Y;
				mDragJobCurrentOffset = PointF.Empty;
				mDragJobStartWorldPos = null;
				if (Math.Abs(dx) > 0.05f || Math.Abs(dy) > 0.05f)
				{
					Core?.TranslateLoadedFile((decimal)dx, (decimal)dy);
					UpdatePositionBarCoords();
				}
				Invalidate();
			}

			if (Core?.CrossCursor.Value == true) ShowCursor = true;
			Cursor.Current = Cursors.Default;
			mLastMousePos = null;
			mMouseWorldPosition = null;
		}

		private void GrblSceneControl_MouseDown(object sender, MouseEventArgs e)
		{
			double xp = e.X / (double)Width * (mCamera.Right - mCamera.Left) + mCamera.Left;
			double yp = (Height - e.Y) / (double)Height * (mCamera.Top - mCamera.Bottom) + mCamera.Bottom;
			mMouseWorldPosition = new PointF((float)xp, (float)yp);

			if (e.Button == MouseButtons.Right)
			{
				ShowSceneContextMenu(e.Location, new PointF((float)xp, (float)yp));
				return;
			}

			if (e.Button == MouseButtons.Left)
			{
				bool hasDrawing = Core?.LoadedFile?.Range?.DrawingRange?.ValidRange == true;
				if (hasDrawing && (ModifierKeys.HasFlag(Keys.Shift) || mJobMoveMode))
				{
					mIsDraggingJob = true;
					mDragJobStartWorldPos = new PointF((float)xp, (float)yp);
					mDragJobCurrentOffset = PointF.Empty;
					Cursor.Current = Cursors.SizeAll;
					Invalidate();
					return;
				}
			}

			mMousePos = null;
			Cursor.Current = Cursors.Cross;
			mLastMousePos = e.Location;
			Invalidate();
		}

		private void GrblSceneControl_MouseMove(object sender, MouseEventArgs e)
		{
			// compute mouse coord
			mMousePos = e.Location;
			double xp = e.X / (double)Width * (mCamera.Right - mCamera.Left) + mCamera.Left;
			double yp = (Height - e.Y) / (double)Height * (mCamera.Top - mCamera.Bottom) + mCamera.Bottom;
			mMouseWorldPosition = new PointF((float)xp, (float)yp);

			if (mIsDraggingJob && mDragJobStartWorldPos != null)
			{
				Cursor.Current = Cursors.SizeAll;
				mDragJobCurrentOffset = new PointF((float)(xp - mDragJobStartWorldPos.Value.X), (float)(yp - mDragJobStartWorldPos.Value.Y));
				Invalidate();
				return;
			}

			// if in drag
			if (mLastMousePos != null && e.Button == MouseButtons.Left)
			{
				Cursor.Current = Cursors.SizeAll;
				Point lastPos = (Point)mLastMousePos;
				double k = (mCamera.Right - mCamera.Left) / Width;
				double dx = e.X - lastPos.X;
				double dy = e.Y - lastPos.Y;
				SetWorldPosition(mCamera.Left - dx * k, mCamera.Right - dx * k, mCamera.Bottom + dy * k, mCamera.Top + dy * k);
				mLastMousePos = e.Location;
				mMousePos = null;
			}
			Invalidate();
		}

		private void ShowSceneContextMenu(Point screenPos, PointF worldPos)
		{
			ContextMenuStrip cms = new ContextMenuStrip();
			cms.Renderer = new MaterialMenuRenderer();

			bool hasDrawing = Core?.LoadedFile?.Range?.DrawingRange?.ValidRange == true;

			ToolStripMenuItem titleItem = new ToolStripMenuItem($"🎯 Mesa: X={worldPos.X:0.0} mm, Y={worldPos.Y:0.0} mm")
			{
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				Enabled = false
			};
			cms.Items.Add(titleItem);
			cms.Items.Add(new ToolStripSeparator());

			if (hasDrawing)
			{
				ToolStripMenuItem miCenter = new ToolStripMenuItem($"🎯 Posicionar Imagem Aqui (Centro)", null, (s, ev) =>
				{
					Core?.MoveLoadedFileTo((decimal)worldPos.X, (decimal)worldPos.Y, true);
					UpdatePositionBarCoords();
				})
				{
					Font = new Font("Segoe UI", 9f, FontStyle.Bold)
				};
				cms.Items.Add(miCenter);

				ToolStripMenuItem miCorner = new ToolStripMenuItem($"📐 Posicionar Imagem Aqui (Canto 0,0)", null, (s, ev) =>
				{
					Core?.MoveLoadedFileTo((decimal)worldPos.X, (decimal)worldPos.Y, false);
					UpdatePositionBarCoords();
				});
				cms.Items.Add(miCorner);

				ToolStripMenuItem miCenterBed = new ToolStripMenuItem($"🎯 Centralizar Imagem na Câmera / Mesa", null, (s, ev) =>
				{
					float bedW = CameraOverlayManager.BedWidthMm * CameraOverlayManager.ScaleX;
					float bedH = CameraOverlayManager.BedHeightMm * CameraOverlayManager.ScaleY;
					float targetX = CameraOverlayManager.OffsetX_Mm + bedW / 2f;
					float targetY = CameraOverlayManager.OffsetY_Mm + bedH / 2f;
					Core?.MoveLoadedFileTo((decimal)targetX, (decimal)targetY, true);
					UpdatePositionBarCoords();
				});
				cms.Items.Add(miCenterBed);

				ToolStripMenuItem miReset = new ToolStripMenuItem($"↺ Resetar Posição para Origem (0, 0)", null, (s, ev) =>
				{
					Core?.ResetLoadedFilePosition();
					UpdatePositionBarCoords();
				});
				cms.Items.Add(miReset);

				ToolStripMenuItem miRotDrawing = new ToolStripMenuItem($"🔄 Rotacionar Desenho / Imagem (90°)", null, (s, ev) =>
				{
					Core?.RotateLoadedFile(90m);
					UpdatePositionBarCoords();
				});
				cms.Items.Add(miRotDrawing);

				cms.Items.Add(new ToolStripSeparator());
			}

			ToolStripMenuItem miJog = new ToolStripMenuItem($"📍 Mover Cabeça do Laser para Cá (Jog)", null, (s, ev) =>
			{
				Core?.JogToPosition(worldPos, false);
			});
			cms.Items.Add(miJog);

			ToolStripMenuItem miLaserToggle = new ToolStripMenuItem(mIsLaserGuideOn ? $"💡 Desligar Laser (Foco)" : $"💡 Ligar Laser 1% (Foco / Mira)", null, (s, ev) =>
			{
				ToggleLaser1Percent();
			});
			cms.Items.Add(miLaserToggle);

			cms.Items.Add(new ToolStripSeparator());

			ToolStripMenuItem miDragToggle = new ToolStripMenuItem(mJobMoveMode ? $"✋ Desativar Modo Arrastar Imagem" : $"✋ Ativar Modo Arrastar Imagem (ou segure SHIFT)", null, (s, ev) =>
			{
				mJobMoveMode = !mJobMoveMode;
				Invalidate();
			})
			{
				Checked = mJobMoveMode
			};
			cms.Items.Add(miDragToggle);

			ToolStripMenuItem miCamera = new ToolStripMenuItem($"📷 Gerenciador de Câmera...", null, (s, ev) =>
			{
				CameraManagerForm.CreateAndShowDialog(Core);
			});
			cms.Items.Add(miCamera);

			ToolStripMenuItem miRotPhoto = new ToolStripMenuItem($"📷 Rotacionar Foto da Câmera (90°)", null, (s, ev) =>
			{
				CameraOverlayManager.RotatePhoto(90);
				Invalidate();
			});
			cms.Items.Add(miRotPhoto);

			ToolStripMenuItem miFraming = new ToolStripMenuItem($"🖼 Enquadramento (Framing / LightBurn)...", null, (s, ev) =>
			{
				LightBurnFramingForm.CreateAndShowDialog(Core);
			});
			cms.Items.Add(miFraming);

			cms.Show(this, screenPos);
		}

		private void ToggleLaser1Percent()
		{
			mIsLaserGuideOn = !mIsLaserGuideOn;
			if (mIsLaserGuideOn)
			{
				int maxPwm = (int)(GrblCore.Configuration != null ? GrblCore.Configuration.MaxPWM : 1000);
				int pwr = Math.Max(1, (int)Math.Round(maxPwm * 0.01));
				Core?.ExecuteCustomCode($"M3 S{pwr}");
			}
			else
			{
				Core?.ExecuteCustomCode("M5 S0");
			}
			if (btnLaser1Pct != null)
			{
				btnLaser1Pct.BackColor = mIsLaserGuideOn ? ColorScheme.PressedButtons : Color.FromArgb(40, 50, 65);
				btnLaser1Pct.Text = mIsLaserGuideOn ? "💡 Laser ON" : "💡 Laser 1%";
			}
		}

		private void InitializePositioningBar()
		{
			btnTogglePosBar = new Button
			{
				Text = "🎯 Posicionar",
				Size = new Size(110, 26),
				Location = new Point(Math.Max(10, Width - 120), Math.Max(10, Height - 36)),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(220, 30, 41, 59),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
				Cursor = Cursors.Hand
			};
			btnTogglePosBar.FlatAppearance.BorderColor = Color.FromArgb(70, 85, 110);
			btnTogglePosBar.Click += (s, e) =>
			{
				pnlPositionBar.Visible = !pnlPositionBar.Visible;
				if (pnlPositionBar.Visible)
				{
					pnlPositionBar.BringToFront();
					UpdatePositionBarCoords();
				}
			};

			pnlPositionBar = new Panel
			{
				Size = new Size(330, 195),
				Location = new Point(Math.Max(10, Width - 345), Math.Max(10, Height - 240)),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				BackColor = Color.FromArgb(235, 20, 25, 35),
				BorderStyle = BorderStyle.FixedSingle,
				Visible = false
			};

			Label lblHeader = new Label
			{
				Text = "🎯 Posicionamento do Trabalho",
				Font = new Font("Segoe UI", 9f, FontStyle.Bold),
				ForeColor = Color.Cyan,
				Location = new Point(8, 6),
				Size = new Size(270, 20)
			};

			Button btnCloseBar = new Button
			{
				Text = "✕",
				Size = new Size(22, 20),
				Location = new Point(300, 5),
				FlatStyle = FlatStyle.Flat,
				ForeColor = Color.Silver,
				BackColor = Color.Transparent,
				Font = new Font("Segoe UI", 8f)
			};
			btnCloseBar.FlatAppearance.BorderSize = 0;
			btnCloseBar.Click += (s, e) => pnlPositionBar.Visible = false;

			Label lblX = new Label { Text = "X:", Location = new Point(8, 32), Size = new Size(20, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f) };
			numPosX = new NumericUpDown
			{
				Location = new Point(30, 30),
				Size = new Size(70, 22),
				DecimalPlaces = 1,
				Minimum = -2000,
				Maximum = 2000,
				BackColor = Color.FromArgb(30, 41, 59),
				ForeColor = Color.White
			};

			Label lblY = new Label { Text = "Y:", Location = new Point(108, 32), Size = new Size(20, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f) };
			numPosY = new NumericUpDown
			{
				Location = new Point(130, 30),
				Size = new Size(70, 22),
				DecimalPlaces = 1,
				Minimum = -2000,
				Maximum = 2000,
				BackColor = Color.FromArgb(30, 41, 59),
				ForeColor = Color.White
			};

			Button btnApply = new Button
			{
				Text = "Ir",
				Location = new Point(208, 29),
				Size = new Size(40, 24),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(40, 70, 110),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 8f)
			};
			btnApply.Click += (s, e) =>
			{
				Core?.MoveLoadedFileTo(numPosX.Value, numPosY.Value, true);
			};

			Button btnModeDrag = new Button
			{
				Text = "✋",
				Location = new Point(252, 29),
				Size = new Size(32, 24),
				FlatStyle = FlatStyle.Flat,
				BackColor = mJobMoveMode ? ColorScheme.PressedButtons : Color.FromArgb(40, 50, 65),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 9f)
			};
			btnModeDrag.Click += (s, e) =>
			{
				mJobMoveMode = !mJobMoveMode;
				btnModeDrag.BackColor = mJobMoveMode ? ColorScheme.PressedButtons : Color.FromArgb(40, 50, 65);
				Invalidate();
			};

			// Nudges row
			FlowLayoutPanel pnlNudges = new FlowLayoutPanel
			{
				Location = new Point(6, 58),
				Size = new Size(318, 28),
				WrapContents = false
			};

			Action<string, decimal, decimal> addNudge = (txt, dx, dy) =>
			{
				Button btnNudge = new Button
				{
					Text = txt,
					Size = new Size(38, 24),
					Margin = new Padding(1),
					FlatStyle = FlatStyle.Flat,
					BackColor = Color.FromArgb(35, 45, 60),
					ForeColor = Color.White,
					Font = new Font("Segoe UI", 7.5f)
				};
				btnNudge.FlatAppearance.BorderColor = Color.FromArgb(60, 75, 95);
				btnNudge.Click += (s, e) =>
				{
					Core?.TranslateLoadedFile(dx, dy);
					UpdatePositionBarCoords();
				};
				pnlNudges.Controls.Add(btnNudge);
			};

			addNudge("◄10", -10m, 0m);
			addNudge("◄1", -1m, 0m);
			addNudge("1►", 1m, 0m);
			addNudge("10►", 10m, 0m);
			addNudge("▲10", 0m, 10m);
			addNudge("▲1", 0m, 1m);
			addNudge("1▼", 0m, -1m);
			addNudge("10▼", 0m, -10m);

			// Action buttons row
			FlowLayoutPanel pnlActions = new FlowLayoutPanel
			{
				Location = new Point(6, 90),
				Size = new Size(318, 30),
				WrapContents = false
			};

			Button btnCenterCamera = new Button
			{
				Text = "🎯 Câmera",
				Size = new Size(74, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(30, 58, 85),
				ForeColor = Color.Cyan,
				Font = new Font("Segoe UI", 8f)
			};
			btnCenterCamera.Click += (s, e) =>
			{
				float bedW = CameraOverlayManager.BedWidthMm * CameraOverlayManager.ScaleX;
				float bedH = CameraOverlayManager.BedHeightMm * CameraOverlayManager.ScaleY;
				float targetX = CameraOverlayManager.OffsetX_Mm + bedW / 2f;
				float targetY = CameraOverlayManager.OffsetY_Mm + bedH / 2f;
				Core?.MoveLoadedFileTo((decimal)targetX, (decimal)targetY, true);
				UpdatePositionBarCoords();
			};

			Button btnAlignLaser = new Button
			{
				Text = "📍 Laser",
				Size = new Size(62, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(30, 58, 85),
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 8f)
			};
			btnAlignLaser.Click += (s, e) =>
			{
				Core?.MoveLoadedFileTo((decimal)mLastWPos.X, (decimal)mLastWPos.Y, true);
				UpdatePositionBarCoords();
			};

			btnLaser1Pct = new Button
			{
				Text = "💡 Laser 1%",
				Size = new Size(82, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(40, 50, 65),
				ForeColor = Color.Yellow,
				Font = new Font("Segoe UI", 8f)
			};
			btnLaser1Pct.Click += (s, e) =>
			{
				ToggleLaser1Percent();
			};

			Button btnReset = new Button
			{
				Text = "↺ (0,0)",
				Size = new Size(54, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(45, 30, 40),
				ForeColor = Color.Silver,
				Font = new Font("Segoe UI", 8f)
			};
			btnReset.Click += (s, e) =>
			{
				Core?.ResetLoadedFilePosition();
				UpdatePositionBarCoords();
			};

			pnlActions.Controls.Add(btnCenterCamera);
			pnlActions.Controls.Add(btnAlignLaser);
			pnlActions.Controls.Add(btnLaser1Pct);
			pnlActions.Controls.Add(btnReset);

			// Rotate row
			FlowLayoutPanel pnlRotateRow = new FlowLayoutPanel
			{
				Location = new Point(6, 122),
				Size = new Size(318, 28),
				WrapContents = false
			};

			Button btnRotPhoto = new Button
			{
				Text = "📷 Girar Foto (90°)",
				Size = new Size(152, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(30, 58, 85),
				ForeColor = Color.Yellow,
				Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
				Cursor = Cursors.Hand
			};
			btnRotPhoto.Click += (s, e) =>
			{
				CameraOverlayManager.RotatePhoto(90);
				Invalidate();
			};

			Button btnRotJob = new Button
			{
				Text = "🔄 Girar Desenho (90°)",
				Size = new Size(156, 24),
				Margin = new Padding(1),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(30, 58, 85),
				ForeColor = Color.Cyan,
				Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
				Cursor = Cursors.Hand
			};
			btnRotJob.Click += (s, e) =>
			{
				Core?.RotateLoadedFile(90m);
				UpdatePositionBarCoords();
			};

			pnlRotateRow.Controls.Add(btnRotPhoto);
			pnlRotateRow.Controls.Add(btnRotJob);

			Label lblTip = new Label
			{
				Text = "💡 Dica: Clique direito na mesa ou segure SHIFT + arraste com mouse!",
				Location = new Point(6, 154),
				Size = new Size(318, 30),
				Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
				ForeColor = Color.FromArgb(148, 163, 184)
			};

			pnlPositionBar.Controls.Add(lblHeader);
			pnlPositionBar.Controls.Add(btnCloseBar);
			pnlPositionBar.Controls.Add(lblX);
			pnlPositionBar.Controls.Add(numPosX);
			pnlPositionBar.Controls.Add(lblY);
			pnlPositionBar.Controls.Add(numPosY);
			pnlPositionBar.Controls.Add(btnApply);
			pnlPositionBar.Controls.Add(btnModeDrag);
			pnlPositionBar.Controls.Add(pnlNudges);
			pnlPositionBar.Controls.Add(pnlActions);
			pnlPositionBar.Controls.Add(pnlRotateRow);
			pnlPositionBar.Controls.Add(lblTip);

			Controls.Add(pnlPositionBar);
			Controls.Add(btnTogglePosBar);
			pnlPositionBar.BringToFront();
			btnTogglePosBar.BringToFront();
		}

		private void UpdatePositionBarCoords()
		{
			try
			{
				if (numPosX != null && numPosY != null && Core?.LoadedFile?.Range?.DrawingRange?.ValidRange == true)
				{
					decimal cx = (decimal)Core.LoadedFile.Range.DrawingRange.Center.X;
					decimal cy = (decimal)Core.LoadedFile.Range.DrawingRange.Center.Y;
					if (numPosX.Value != cx) numPosX.Value = Math.Max(numPosX.Minimum, Math.Min(numPosX.Maximum, cx));
					if (numPosY.Value != cy) numPosY.Value = Math.Max(numPosY.Minimum, Math.Min(numPosY.Maximum, cy));
				}
			}
			catch { }
		}

		private void GrblSceneControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (mMouseWorldPosition != null)
			{
				float k = e.Delta / 1000f;
				PointF mousePos = (PointF)mMouseWorldPosition;
				SetWorldPosition(
					mCamera.Left + (mousePos.X - mCamera.Left) * k,
					mCamera.Right - (mCamera.Right - mousePos.X) * k,
					mCamera.Bottom + (mousePos.Y - mCamera.Bottom) * k,
					mCamera.Top - (mCamera.Top - mousePos.Y) * k
				);
			}
		}

		public void SetCore(GrblCore core)
		{
			Core = core;
            Core.OnFileLoading += OnFileLoading;
			Core.OnFileLoaded += OnFileLoaded;
			Core.ShowExecutedCommands.OnChange += ShowExecutedCommands_OnChange;
			Core.PreviewLineSize.OnChange += PrerviewLineSize_OnChange;
			Core.ShowLaserOffMovements.OnChange += ShowLaserOffMovements_OnChange;
            Core.ShowBoundingBox.OnChange += ShowBoundingBox_OnChange;
			Core.OnAutoSizeDrawing += Core_OnAutoSizeDrawing;
			Core.OnZoomInDrawing += Core_OnZoomInDrawing;
			Core.OnZoomOutDrawing += Core_OnZoomOutDrawing;
			Core.OnProgramEnded += OnProgramEnded;
		}

        private void OnFileLoading(long elapsed, string filename)
        {
			mMessage = Strings.Loading;
        }

        private void OnProgramEnded()
		{
			foreach (var command in Core.LoadedFile.Commands)
			{
				command.ClearResult();
			}
		}

		private void Core_OnAutoSizeDrawing(GrblCore obj)
		{
			AutoSizeDrawing();
		}
		private void Core_OnZoomInDrawing(GrblCore obj)
		{
			ZoomIn();
		}

		private void Core_OnZoomOutDrawing(GrblCore obj)
		{
			ZoomOut();
		}

		private void PrerviewLineSize_OnChange(Tools.RetainedSetting<float> obj)
		{
			if (mGrbl3D != null) mGrbl3D.LineWidth = obj.Value;
			mInvalidateAll = true;
			RR.Set();
		}

		private void ShowLaserOffMovements_OnChange(RetainedSetting<bool> obj)
		{
			mInvalidateAll = true;
			RR.Set();
		}

        private void ShowBoundingBox_OnChange(RetainedSetting<bool> obj)
        {
            mInvalidateAll = true;
            RR.Set();
        }

        private void ShowExecutedCommands_OnChange(Tools.RetainedSetting<bool> obj)
		{
			mInvalidateAll = true;
			RR.Set();
		}

		private void DisposeGrbl3D()
		{
			mReload = true;
		}

		private void OnFileLoaded(long elapsed, string filename)
		{
			DisposeGrbl3D();
			mReload = true;
			if (filename != "Translated")
				AutoSizeDrawing();
			else
				Invalidate();
			UpdatePositionBarCoords();
		}
		
		public void AutoSizeDrawing()
		{
			double ratio = Width / (double)Height;
			double availableWidth = Width - mPadding.Left - mPadding.Right;
			double availableHeight = Height - mPadding.Bottom - mPadding.Top;
			const double growFactor = 1.1;
			const decimal defaultViewport = 100;

			XYRange autosizeRange;

			if (Core?.LoadedFile.Range.DrawingRange.ValidRange == true)
			{
				autosizeRange = Core.AutoSizeOnDrawing.Value ? Core.LoadedFile.Range.DrawingRange : Core.LoadedFile.Range.MovingRange;
			}
			else
			{
				autosizeRange = new XYRange();
				autosizeRange.X.Min = -defaultViewport;
				autosizeRange.X.Max = defaultViewport;
				autosizeRange.Y.Min = -defaultViewport;
				autosizeRange.Y.Max = defaultViewport;
			}

			double drawingWidth = (double)autosizeRange.Width * growFactor;
			double drawingHeight = (double)autosizeRange.Height * growFactor;

			double widthRatio = drawingWidth / availableWidth;
			double paddingLeftWorld = mPadding.Left * widthRatio;
			double paddingRightWorld = mPadding.Right * widthRatio;

			double heightRatio = drawingHeight / availableHeight;
			double paddingBottomWorld = mPadding.Bottom * heightRatio;
			double paddingTopWorld = mPadding.Top * heightRatio;

			double left;
			double right;
			double bottom;
			double top;

			if (drawingWidth / drawingHeight > availableWidth / availableHeight)
			{
				left = (double)autosizeRange.Center.X - drawingWidth / 2;
				right = (double)autosizeRange.Center.X + drawingWidth / 2;

				left -= paddingLeftWorld;
				right += paddingRightWorld;

				double resizedHeight = (right - left) / ratio;
				double centerY = autosizeRange.Center.Y;

				bottom = centerY - resizedHeight / 2;
				top = centerY + resizedHeight / 2;

				double bottomShift = (top - bottom) / Height * mPadding.Bottom / 2;
				bottom -= bottomShift;
				top -= bottomShift;
			}
			else
			{
				bottom = (double)autosizeRange.Center.Y - drawingHeight / 2;
				top = (double)autosizeRange.Center.Y + drawingHeight / 2;

				bottom -= paddingBottomWorld;
				top += paddingTopWorld;

				double resizedWidth = (top - bottom) * ratio;
				double centerX = autosizeRange.Center.X;

				left = centerX - resizedWidth / 2;
				right = centerX + resizedWidth / 2;

				double leftShift = (right - left) / Width * mPadding.Left / 2;
				left -= leftShift;
				right -= leftShift;
			}

			SetWorldPosition(
				left,
				right,
				bottom,
				top
			);
		}

		PeriodicEventTimer InvalidateTimer = new Tools.PeriodicEventTimer(TimeSpan.FromSeconds(1), true);
		public void TimerUpdate()
		{
			if (Core != null && (mLastWPos != Core.WorkPosition || mLastMPos != Core.MachinePosition || mCurF != Core.CurrentF || mCurS != Core.CurrentS))
			{
				mLastWPos = Core.WorkPosition;
				mLastMPos = Core.MachinePosition;
				mCurF = Core.CurrentF;
				mCurS = Core.CurrentS;
				RR.Set();
			}

			if (InvalidateTimer.Expired)
				Invalidate();
		}

		private void Grbl3D_OnLoadingPercentageChange()
		{
			Invalidate();
		}

		public void OnColorChange()
		{
			mBackgroundColor = ColorScheme.PreviewBackColor;
			mTextColor = ColorScheme.PreviewText;
			mTextBoundingColor = ColorScheme.PreviewJobRange;
			mOriginsColor = ColorScheme.PreviewRuler;
			mPointerColor = ColorScheme.PreviewCross;
			mTicksColor = ColorScheme.PreviewGrid;
			mMinorsColor = ColorScheme.PreviewGridMinor;
			BackColor = mBackgroundColor;
			mInvalidateAll = true;
			RR.Set();
		}

		private void GrblPanel3D_Load(object sender, EventArgs e)
		{
			Load -= GrblPanel3D_Load;
			TimDetectIssue.Start();
		}

		private void TimDetectIssue_Tick(object sender, EventArgs e)
		{
			TimDetectIssue.Stop();
			if (OpCounter < 5 || FatalException != null) //non è riuscito a completare nemmeno un disegno nei 5 secondi del timer! (o se c'è stata una eccezione nei primi 5 secondi)
			{
				if (FatalException == null) //fatal exception are logged where they are generated
					Logger.LogMessage("OpenGL", "Rendering issue detected! OpCounte: {0}", OpCounter);

				if (Settings.CurrentGraphicMode == Settings.GraphicMode.FBO && MessageBox.Show(Strings.FBOIssueSuggestDBO, Strings.FirmwareRequireRestart, MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					Settings.ConfiguredGraphicMode = Settings.GraphicMode.DIB;
					UsageStats.DoNotSendNow = true;
					Application2.RestartNoCommandLine();
				}
				else if (Settings.CurrentGraphicMode == Settings.GraphicMode.DIB && MessageBox.Show(Strings.DBOIssueSuggestGDI, Strings.FirmwareRequireRestart, MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					Settings.ConfiguredGraphicMode = Settings.GraphicMode.GDI;
					UsageStats.DoNotSendNow = true;
					Application2.RestartNoCommandLine();
				}
			}
		}

		internal void ZoomIn()
		{
			CenterZoom(1.0 / 1.1);
		}

		internal void ZoomOut()
		{
			CenterZoom(1.1);
		}

		private void CenterZoom(double k)
		{
			// Calculate the center of the current view
			double centerX = (mCamera.Left + mCamera.Right) / 2;
			double centerY = (mCamera.Bottom + mCamera.Top) / 2;

			// Calculate the new half-width and half-height based on the zoom factor
			double halfWidth = (mCamera.Right - mCamera.Left) / 2 ;
			double halfHeight = (mCamera.Top - mCamera.Bottom) / 2 ;

			halfWidth = halfWidth * k;
			halfHeight = halfHeight * k;

			// Update the boundaries
			double left = centerX - halfWidth;
			double right = centerX + halfWidth;
			double bottom = centerY - halfHeight;
			double top = centerY + halfHeight;

			SetWorldPosition(left, right, bottom, top);
		}
	}
}