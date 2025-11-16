using System;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.UI
{
    public class UITransform : GameComponent, IAlignable
    {
        public Vector2 anchorLeftTop = Vector2.Zero;
        public Vector2 anchorRightBottom = Vector2.One;
        [DontSerialize] public Rectangle LocalBounds
        {
            get => new(
                (int)OffsetMin.X,
                (int)OffsetMin.Y,
                HorizontalAlignment == AlignmentH.Stretch ?
                    (int)(OffsetMax.X - OffsetMin.X) : Width,
                VerticalAlignment == AlignmentV.Stretch ?
                    (int)(OffsetMax.Y - OffsetMin.Y) : Height
            );
            set
            {
                OffsetMin = new Vector2(value.Left, value.Top);
                OffsetMax = new Vector2(value.Right, value.Bottom);
                Width = value.Width;
                Height = value.Height;
            }
        }
        public Vector2 OffsetMin { get; set; } = Vector2.Zero;
        public Vector2 OffsetMax { get; set; } = Vector2.Zero;
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        [DontSerialize] public Rectangle Bounds { get; private set; } = Rectangle.Empty;

        public AlignmentH HorizontalAlignment
        {
            get
            {
                if (anchorLeftTop.X == 0)
                {
                    if (anchorRightBottom.X == 0) return AlignmentH.Left;
                    if (anchorRightBottom.X == 1) return AlignmentH.Stretch;
                }
                if (anchorLeftTop.X == 0.5f && anchorRightBottom.X == 0.5f) return AlignmentH.Center;
                if (anchorLeftTop.X == 1 && anchorRightBottom.X == 1) return AlignmentH.Right;
                return AlignmentH.Specific;
            }
            set
            {
                switch (value)
                {
                    case AlignmentH.Left:
                        anchorLeftTop = new Vector2(0, anchorLeftTop.Y);
                        anchorRightBottom = new Vector2(0, anchorRightBottom.Y);
                        break;
                    case AlignmentH.Center:
                        anchorLeftTop = new Vector2(0.5f, anchorLeftTop.Y);
                        anchorRightBottom = new Vector2(0.5f, anchorRightBottom.Y);
                        break;
                    case AlignmentH.Right:
                        anchorLeftTop = new Vector2(1, anchorLeftTop.Y);
                        anchorRightBottom = new Vector2(1, anchorRightBottom.Y);
                        break;
                    case AlignmentH.Stretch:
                        anchorLeftTop = new Vector2(0, anchorLeftTop.Y);
                        anchorRightBottom = new Vector2(1, anchorRightBottom.Y);
                        break;
                    case AlignmentH.Specific:
                        break;
                }
            }
        }
        public enum AlignmentH
        {
            Left,
            Center,
            Right,
            Stretch,
            Specific
        }
        public AlignmentV VerticalAlignment
        {
            get
            {
                if (anchorLeftTop.Y == 0)
                {
                    if (anchorRightBottom.Y == 0) return AlignmentV.Top;
                    if (anchorRightBottom.Y == 1) return AlignmentV.Stretch;
                }
                if (anchorLeftTop.Y == 0.5f && anchorRightBottom.Y == 0.5f) return AlignmentV.Middle;
                if (anchorLeftTop.Y == 1 && anchorRightBottom.Y == 1) return AlignmentV.Bottom;
                return AlignmentV.Specific;
            }
            set
            {
                switch (value)
                {
                    case AlignmentV.Top:
                        anchorLeftTop = new Vector2(anchorLeftTop.X, 0);
                        anchorRightBottom = new Vector2(anchorRightBottom.X, 0);
                        break;
                    case AlignmentV.Middle:
                        anchorLeftTop = new Vector2(anchorLeftTop.X, 0.5f);
                        anchorRightBottom = new Vector2(anchorRightBottom.X, 0.5f);
                        break;
                    case AlignmentV.Bottom:
                        anchorLeftTop = new Vector2(anchorLeftTop.X, 1);
                        anchorRightBottom = new Vector2(anchorRightBottom.X, 1);
                        break;
                    case AlignmentV.Stretch:
                        anchorLeftTop = new Vector2(anchorLeftTop.X, 0);
                        anchorRightBottom = new Vector2(anchorRightBottom.X, 1);
                        break;
                    case AlignmentV.Specific:
                        break;
                }
            }
        }
        public enum AlignmentV
        {
            Top,
            Middle,
            Bottom,
            Stretch,
            Specific
        }

        public Vector2 origin = Vector2.Zero;
        [HideInInspector] public Vector2 OriginPosition => new(
            Bounds.X + (Bounds.Width * origin.X),
            Bounds.Y + (Bounds.Height * origin.Y)
        );

        public float LocalRotation = 0;
        [DontSerialize, HideInInspector] public float Rotation = 0;

        public override UITransform Copy()
        {
            var copy = (UITransform) MemberwiseClone();
            copy.gameObject = null;
            copy.uiTransform = copy;
            return copy;
        }

        public void RefreshBounds(UITransform parentTransform = null)
        {
            parentTransform ??= gameObject.Parent.uiTransform;
            //Считаем глобальный угол поворота
            Rotation = (parentTransform?.Rotation ?? 0) + LocalRotation;

            //Находим координаты якорей
            Rectangle parentRect = parentTransform.Bounds;
            int anchorLeft = (int)(parentRect.X + (parentRect.Width * anchorLeftTop.X));
            int anchorRight = (int)(parentRect.X + (parentRect.Width * anchorRightBottom.X));
            int anchorTop = (int)(parentRect.Y + (parentRect.Height * anchorLeftTop.Y));
            int anchorBottom = (int)(parentRect.Y + (parentRect.Height * anchorRightBottom.Y));

            //Получаем позицию опорной точки объекта
            int x = anchorLeft;
            int y = anchorTop;
            int width = Width;
            if (anchorLeft == anchorRight)
            {
                if (HorizontalAlignment == AlignmentH.Right)
                    x -= (int)OffsetMax.X;
                else x += (int)OffsetMin.X;
            }
            else
            {
                x += (int)OffsetMin.X;
                width = (int)(anchorRight - anchorLeft - OffsetMax.X - OffsetMin.X);
                if (width < 0)
                {
                    Bounds = Rectangle.Empty;
                    return;
                }
            }
            int height = Height;
            if (anchorTop == anchorBottom)
            {
                if (VerticalAlignment == AlignmentV.Bottom)
                    y -= (int)OffsetMax.Y;
                else y += (int)OffsetMin.Y;
            }
            else
            {
                y += (int)OffsetMin.Y;
                height = (int)(anchorBottom - anchorTop - OffsetMax.Y - OffsetMin.Y);
                if (height < 0)
                {
                    Bounds = Rectangle.Empty;
                    return;
                }
            }

            //К опорной точке применяем вращение вокруг опорной точки родителя
            (x, y) = ((IAlignable)this).PositionAfterTurningParent(parentTransform, x, y);

            x -= (int)(origin.X * width);
            y -= (int)(origin.Y * height);

            //Применяем полученные координаты
            Bounds = new Rectangle(
                x,
                y,
                width,
                height
            );
        }

        public bool Contains(Point point) // Определение принадлежности с учетом поворота
        {
            if (Rotation == 0) return Bounds.Contains(point);
            
            Vector2 pointRotated = LocalPosition(point.ToVector2());
            return Bounds.Contains(pointRotated);
        }

        private Vector2 LocalPosition(Vector2 globalPoint)
        {
            Vector2 pivot = new(OriginPosition.X, OriginPosition.Y);
            return Vector2.RotateAround(globalPoint, pivot, (float)(-Rotation * Math.PI / 180.0));
        }

        public static UITransform TransformOfBounds(Rectangle bounds) => new() { Bounds = bounds };
    }
}