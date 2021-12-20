// MIT License
//
// Copyright (c) 2021 SirRandoo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using ToolkitCore.Utilities;
using UnityEngine;
using Verse;

namespace ToolkitPoints.Windows
{
    public class InputDialog : Window
    {
        private static readonly float ButtonHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
        private readonly Action cancelAction;
        private readonly Action closeAction;
        private readonly Action<string> enterAction;
        private string container = "";

        public override Vector2 InitialSize => new Vector2(300f, optionalTitle.NullOrEmpty() ? 100f : 140f);


        public InputDialog(Action<string> onEnter, Action onCancel = null, Action onClose = null)
        {
            closeAction = onClose;
            cancelAction = onCancel;
            enterAction = onEnter;
            onlyOneOfTypeAllowed = false;
        }
        public InputDialog(string title, Action<string> onEnter, Action onCancel = null, Action onClose = null) : this(onEnter, onCancel, onClose)
        {
            optionalTitle = title;
        }

        public override void DoWindowContents(Rect region)
        {
            GUI.BeginGroup(region);
            var buttonRow = new Rect(0f, region.height - ButtonHeight, region.width, ButtonHeight);
            var inputRect = new Rect(0f, 0f, region.width, region.height - buttonRow.height - 5f);
            var buttonRect = new Rect(region.width - CloseButSize.x, 0f, CloseButSize.x, ButtonHeight);

            GUI.BeginGroup(inputRect);
            container = Widgets.TextField(inputRect, container);
            GUI.EndGroup();

            GUI.BeginGroup(buttonRow);

            if (Widgets.ButtonText(buttonRect, "Cancel"))
            {
                cancelAction?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(buttonRect.ShiftLeft(), "OK"))
            {
                enterAction?.Invoke(container);
                Close();
            }

            GUI.EndGroup();

            GUI.EndGroup();
        }

        public override void Notify_ClickOutsideWindow()
        {
            cancelAction?.Invoke();
            base.Notify_ClickOutsideWindow();
        }

        public override void OnAcceptKeyPressed()
        {
            enterAction?.Invoke(container);
            base.OnAcceptKeyPressed();
        }

        public override void OnCancelKeyPressed()
        {
            cancelAction?.Invoke();
            base.OnCancelKeyPressed();
        }

        public override void PostClose()
        {
            base.PostClose();

            closeAction?.Invoke();
        }

        public static void Popup(string title, Action<string> onEnter, Action onCancel = null, Action onClose = null)
        {
            Find.WindowStack.Add(new InputDialog(title, onEnter, onCancel, onClose));
        }
    }
}
