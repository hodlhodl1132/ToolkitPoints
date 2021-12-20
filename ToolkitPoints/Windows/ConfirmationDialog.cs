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
    public class ConfirmationDialog : Window
    {
        private static readonly float ButtonHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
        private readonly Action cancelAction;
        private readonly Action closeAction;
        private readonly Action confirmAction;
        private readonly string message;

        public override Vector2 InitialSize => new Vector2(300f, 200f);


        public ConfirmationDialog(string message, Action onConfirm, Action onCancel = null, Action onClose = null)
        {
            this.message = message;

            closeAction = onClose;
            cancelAction = onCancel;
            confirmAction = onConfirm;
            onlyOneOfTypeAllowed = false;
        }
        public ConfirmationDialog(string title, string message, Action onConfirm, Action onCancel = null, Action onClose = null) : this(message, onConfirm, onCancel, onClose)
        {
            optionalTitle = title;
        }

        public override void DoWindowContents(Rect region)
        {
            GUI.BeginGroup(region);
            var buttonRow = new Rect(0f, region.height - ButtonHeight, region.width, ButtonHeight);
            var messageRect = new Rect(0f, 0f, region.width, region.height - buttonRow.height - 5f);
            var buttonRect = new Rect(region.width - CloseButSize.x, 0f, CloseButSize.x, ButtonHeight);

            GUI.BeginGroup(messageRect);
            SettingsHelper.DrawLabel(messageRect, message, TextAnchor.UpperCenter);
            GUI.EndGroup();

            GUI.BeginGroup(buttonRow);

            if (Widgets.ButtonText(buttonRect, "Cancel"))
            {
                cancelAction?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(buttonRect.ShiftLeft(), "Confirm"))
            {
                confirmAction?.Invoke();
                Close();
            }

            GUI.EndGroup();

            GUI.EndGroup();
        }

        public override void PostClose()
        {
            base.PostClose();

            closeAction?.Invoke();
        }

        public static void Popup(string title, string message, Action onConfirm, Action onCancel = null, Action onClose = null)
        {
            Find.WindowStack.Add(new ConfirmationDialog(title, message, onConfirm, onCancel, onClose));
        }
    }
}
