using NLua;
using System;
using System.Collections;
using System.Drawing;
using System.Windows;
using Point = System.Windows.Point;

namespace LuaSTGRoadDrawer
{
    enum MOVETYPE
    {
        MOVE_NORMAL,
        MOVE_ACCEL,
        MOVE_DECEL,
        MOVE_ACC_DEC,
    }

    class RoadCalculator
    {
        /// <summary>
        /// 起始坐标位置
        /// </summary>
        public Point StartPoint = new Point(0, 0);

        /// <summary>
        /// 结束坐标位置
        /// </summary>
        public Point EndPoint = new Point(0, 0);

        /// <summary>
        /// 拆分点数量
        /// </summary>
        public int PointCount = 60;

        public ArrayList PointList = new ArrayList();
        private readonly object _listlock = new object();

        public void InsertConsolePoint(Point point)
        {
            lock (_listlock)
            {
                PointList.Add(point);
            }
        }

        public void RemoveConsolePoint(int index)
        {
            lock (_listlock)
            {
                try
                {
                    PointList.RemoveAt(index);
                }
                catch
                {
                }
            }
        }

        public void ClearConsolePoint()
        {
            lock (_listlock)
            {
                PointList.Clear();
            }
        }

        /// <summary>
        /// 移动方式
        /// </summary>
        public MOVETYPE MoveType = MOVETYPE.MOVE_NORMAL;

        public string LuaTask = @"
local dx = TARGET_X - POS_X
local dy = TARGET_Y - POS_Y
local xs = POS_X
local ys = POS_Y
if MODE == 1 then
	for s = 1 / FRAME, 1 + 0.5 / FRAME, 1 / FRAME do
		s = s * s
		POS_X = xs + s * dx
		POS_Y = ys + s * dy
		yield()
	end
elseif MODE == 2 then
	for s = 1 / FRAME, 1 + 0.5 / FRAME, 1 / FRAME do
		s = s * 2 - s * s
		POS_X = xs + s * dx
		POS_Y = ys + s * dy
		yield()
	end
elseif MODE == 3 then
	for s = 1 / FRAME, 1 + 0.5 / FRAME, 1 / FRAME do
		if s < 0.5 then
			s = s * s * 2
		else
			s = -2 * s * s + 4 * s - 1
		end
		POS_X = xs + s * dx
		POS_Y = ys + s * dy
		yield()
	end
else
	for s = 1 / FRAME, 1 + 0.5 / FRAME, 1 / FRAME do
		POS_X = xs + s * dx
		POS_Y = ys + s * dy
		yield()
	end
end
";

        public ArrayList Calculate()
        {
            ArrayList list = new ArrayList();
            Lua lua = GetLua();
            try
            {
                lua.DoString($"___G_THREAD = coroutine.create(function() {LuaTask} end)");
                while ((bool)lua.DoString("return coroutine.status(___G_THREAD) ~= \"dead\"")[0])
                {
                    object[] result = lua.DoString("return coroutine.resume(___G_THREAD)");
                    if (result.Length > 1)
                    {
                        throw new Exception((string)result[1] + "\n\n" + (string)lua.DoString("return debug.traceback(___G_THREAD)")[0]);
                    }
                    else
                    {
                        list.Add(new Point((double)lua["POS_X"], (double)lua["POS_Y"]));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return list;
        }

        private Lua GetLua()
        {
            Lua lua = new Lua();
            lua.DoString($"POS_X = {StartPoint.X}");
            lua.DoString($"POS_Y = {StartPoint.Y}");
            lua.DoString($"TARGET_X = {EndPoint.X}");
            lua.DoString($"TARGET_Y = {EndPoint.Y}");
            lua.DoString($"FRAME = {PointCount}");
            lua.DoString($"MODE = {(int)MoveType}");
            lua.DoString("yield = coroutine.yield");
            lua.DoString("POINTLIST = {}");
            var t = lua.GetTable("POINTLIST");
            var index = 1;
            foreach (Point p in (ArrayList)PointList.Clone())
            {
                t[index] = p.X;
                t[index + 1] = p.Y;
                index += 2;
            }
            return lua;
        }
    }

    class LuaSTGWorldMap
    {
        /// <summary>
        /// 屏幕宽度
        /// </summary>
        public int ScreenWidth = 640;

        /// <summary>
        /// 屏幕高度
        /// </summary>
        public int ScreenHeight = 480;

        /// <summary>
        /// World数据
        /// </summary>
        public LuaSTGWorld World = new LuaSTGWorld();

        /// <summary>
        /// 输出背景色
        /// </summary>
        public Color BackgroundColor = Color.White;

        /// <summary>
        /// 输出世界边框色
        /// </summary>
        public Color WorldBorderColor = Color.Gray;

        /// <summary>
        /// 控制点集色
        /// </summary>
        public Color ConsolePointColor = Color.Red;

        /// <summary>
        /// 起始点颜色
        /// </summary>
        public Color StartPointColor = Color.DarkGreen;

        /// <summary>
        /// 结束点颜色
        /// </summary>
        public Color EndPointColor = Color.DarkBlue;

        /// <summary>
        /// 输出点集色
        /// </summary>
        public Color PointColor = Color.Black;

        /// <summary>
        /// 输出世界边界宽度
        /// </summary>
        public int WorldBorderScale = 2;

        /// <summary>
        /// 输出世界控制点大小
        /// </summary>
        public float WorldConsolePointScale = 3;

        /// <summary>
        /// 输出世界点大小
        /// </summary>
        public float WorldPointScale = 2;

        private void DrawPoint(Graphics g, Pen brush, LuaSTGWorld World, Point point, float size)
        {
            double scaleX = World.ScreenScale.Width / World.WorldScale.Width;
            double scaleY = World.ScreenScale.Height / World.WorldScale.Height;
            g.DrawEllipse(brush, new RectangleF(
                (float)(World.ScreenLeft + scaleX * (point.X - World.Left)),
                (float)(ScreenHeight - (World.ScreenBottom + scaleY * (point.Y - World.Bottom))),
                size, size));
        }

        /// <summary>
        /// 获取当前状态的Bitmap对象
        /// 使用完需要释放Bitmap
        /// </summary>
        /// <returns></returns>
        public Bitmap GetImage(ArrayList ConsolePoints, ArrayList ResultPoints, Point StartPoint, Point EndPoint)
        {
            Bitmap pic = new Bitmap(ScreenWidth, ScreenHeight);
            Graphics g = Graphics.FromImage(pic);
            g.Clear(BackgroundColor);
            SizeF Scale = World.ScreenScale;
            SolidBrush WorldBorderBrush = new SolidBrush(WorldBorderColor);
            g.FillRectangle(WorldBorderBrush, new RectangleF(World.ScreenLeft, ScreenHeight - World.ScreenTop, WorldBorderScale, Scale.Height));
            g.FillRectangle(WorldBorderBrush, new RectangleF(World.ScreenLeft, ScreenHeight - World.ScreenTop, Scale.Width, WorldBorderScale));
            g.FillRectangle(WorldBorderBrush, new RectangleF(World.ScreenRight, ScreenHeight - World.ScreenTop, WorldBorderScale, Scale.Height));
            g.FillRectangle(WorldBorderBrush, new RectangleF(World.ScreenLeft, ScreenHeight - World.ScreenBottom, Scale.Width, WorldBorderScale));
            if (WorldPointScale > WorldConsolePointScale)
            {
                Pen PointBrush = new Pen(PointColor);
                foreach (Point p in ResultPoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldPointScale);
                }
                PointBrush = new Pen(ConsolePointColor);
                foreach (Point p in ConsolePoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldConsolePointScale);
                }
            }
            else if (WorldPointScale < WorldConsolePointScale)
            {
                Pen PointBrush = new Pen(ConsolePointColor);
                foreach (Point p in ConsolePoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldConsolePointScale);
                }
                PointBrush = new Pen(PointColor);
                foreach (Point p in ResultPoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldPointScale);
                }
            }
            else
            {
                Pen PointBrush = new Pen(PointColor);
                foreach (Point p in ResultPoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldPointScale);
                }
                PointBrush = new Pen(ConsolePointColor);
                foreach (Point p in ConsolePoints)
                {
                    DrawPoint(g, PointBrush, World, p, WorldConsolePointScale);
                }
            }
            DrawPoint(g, new Pen(StartPointColor), World, StartPoint, WorldPointScale);
            DrawPoint(g, new Pen(EndPointColor), World, EndPoint, WorldPointScale);
            g.Dispose();
            return pic;
        }
    }

    class LuaSTGWorld
    {
        /// <summary>
        /// 世界左边界坐标值
        /// </summary>
        public int Left = -192;

        /// <summary>
        /// 世界右边界坐标值
        /// </summary>
        public int Right = 192;

        /// <summary>
        /// 世界底边界坐标值
        /// </summary>
        public int Bottom = -224;

        /// <summary>
        /// 世界上边界坐标值
        /// </summary>
        public int Top = 224;

        /// <summary>
        /// 世界中心坐标
        /// </summary>
        public Point WorldCenter
        {
            get
            {
                return new Point((Left + Right) / 2, (Bottom + Top) / 2);
            }
        }

        /// <summary>
        /// 世界大小
        /// </summary>
        public SizeF WorldScale
        {
            get
            {
                return new SizeF(Right - Left, Top - Bottom);
            }
        }

        /// <summary>
        /// 左边界位于屏幕位置
        /// </summary>
        public int ScreenLeft = 32;

        /// <summary>
        /// 右边界位于屏幕位置
        /// </summary>
        public int ScreenRight = 416;

        /// <summary>
        /// 底边界位于屏幕位置
        /// </summary>
        public int ScreenBottom = 16;

        /// <summary>
        /// 上边界位于屏幕位置
        /// </summary>
        public int ScreenTop = 464;

        /// <summary>
        /// 世界渲染屏幕中心位置
        /// </summary>
        public Point ScreenCenter
        {
            get
            {
                return new Point((ScreenLeft + ScreenRight) / 2, (ScreenBottom + ScreenTop) / 2);
            }
        }

        /// <summary>
        /// 世界渲染屏幕大小
        /// </summary>
        public SizeF ScreenScale
        {
            get
            {
                return new SizeF(ScreenRight - ScreenLeft, ScreenTop - ScreenBottom);
            }
        }

    }
}