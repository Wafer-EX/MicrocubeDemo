﻿using Microcube.Graphics.ColorModels;
using Microcube.Graphics.Raster;
using Microcube.Input;
using Silk.NET.Maths;

namespace Microcube.UI.Components.Containers
{
    public class InputInterceptorContainer : Container
    {
        public required Predicate<GameActionBatch>? OnInterception { get; set; }

        public InputInterceptorContainer() : base() { }

        public override IEnumerable<Sprite> GetSprites(Rectangle<float> displayedArea)
        {
            if (BackgroundColor != RgbaColor.Transparent)
                yield return new Sprite(displayedArea, BackgroundColor);

            if (Child != null)
            {
                foreach (Sprite sprite in Child.GetSprites(displayedArea))
                    yield return sprite;
            }
        }

        public override void Input(GameActionBatch actionBatch)
        {
            bool isIntercepted = OnInterception?.Invoke(actionBatch) ?? false;
            if (!isIntercepted)
                base.Input(actionBatch);
        }
    }
}