﻿using Microcube.Graphics.ColorModels;
using Microcube.Graphics.Raster;
using Microcube.Input;
using Silk.NET.Maths;

namespace Microcube.UI.Components.Layouts
{
    /// <summary>
    /// Represents a layout that shows only one component at the same time and can switch it.
    /// </summary>
    public class CardLayout : Layout
    {
        private int selectedIndex = 0;

        public override bool IsFocused { get; set; }

        /// <summary>
        /// Index of selected component. Process any value to fit it to component range.
        /// </summary>
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (Childs[selectedIndex] is IFocusable previousFocusable)
                    previousFocusable.IsFocused = false;

                selectedIndex = value;

                while (selectedIndex >= Childs.Count)
                    selectedIndex -= Childs.Count;

                while (selectedIndex < 0)
                    selectedIndex += Childs.Count;

                if (IsFocused && Childs[selectedIndex] is IFocusable newFocusable)
                    newFocusable.IsFocused = true;
            }
        }

        public override IReadOnlyList<Component?> Childs
        {
            get => base.Childs;
            set
            {
                base.Childs = value;
                if (base.Childs.Any())
                    SelectedIndex = 0;
            }
        }

        public CardLayout() : base() { }

        public override IEnumerable<Sprite> GetSprites(Rectangle<float> displayedArea)
        {
            if (BackgroundColor != RgbaColor.Transparent)
                yield return new Sprite(displayedArea, BackgroundColor);

            Component? selectedChild = Childs[SelectedIndex];
            foreach (Sprite sprite in selectedChild?.GetSprites(displayedArea) ?? Array.Empty<Sprite>())
                yield return sprite;
        }

        public override void Input(GameActionBatch actionBatch)
        {
            if (Childs.Any() && Childs[selectedIndex] is IFocusable focusable)
            {
                if (actionBatch.IsIncludeClick(GameAction.Escape) && focusable.IsLastFocused)
                    focusable.IsFocused = false;
                else
                {
                    if (actionBatch.IsIncludeClick(GameAction.Enter) && !focusable.IsFocused)
                        focusable.IsFocused = true;
                    else
                        focusable.Input(actionBatch);
                }
            }
        }
    }
}