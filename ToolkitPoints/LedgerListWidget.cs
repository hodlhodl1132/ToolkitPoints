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
using System.Collections.Generic;
using System.Linq;
using ToolkitCore.Utilities;
using ToolkitPoints.Windows;
using UnityEngine;
using Verse;

namespace ToolkitPoints
{
    public class LedgerListWidget
    {
        private static readonly int RowHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
        private bool _hasScrollbars;
        private Vector2 _scrollPos = Vector2.zero;
        private Ledger _selectedLedger;
        private SortOrder _sortOrder = SortOrder.Ascending;
        private string query;

        public bool QueryHasResults { get; set; }

        public Ledger SelectedLedger
        {
            get => _selectedLedger;
            set
            {
                _selectedLedger = value;
                OnLedgerSelected(_selectedLedger);
            }
        }

        public event EventHandler<Ledger> LedgerSelected;

        public void Draw(Rect region)
        {
            GameFont fontCache = Text.Font;
            Text.Font = GameFont.Small;

            GUI.BeginGroup(region);

            var headerRect = new Rect(0f, 0f, region.width, Text.SmallFontHeight);
            var contentRect = new Rect(0f, Text.SmallFontHeight, region.width, region.height - Text.SmallFontHeight);

            GUI.BeginGroup(headerRect);
            DrawHeader(headerRect);
            GUI.EndGroup();

            GUI.BeginGroup(contentRect);
            DrawContent(contentRect.AtZero());
            GUI.EndGroup();

            GUI.EndGroup();

            Text.Font = fontCache;
        }

        private void DrawHeader(Rect region)
        {
            float width = _hasScrollbars ? region.width - 16f : region.width;
            var nameHeaderRect = new Rect(0f, 0f, width, region.height);
            var nameHeaderTextRect = new Rect(region.height + 2f, 0f, nameHeaderRect.width - region.height - 2f, nameHeaderRect.height);
            Rect sortNotchRect = SettingsHelper.RectForIcon(new Rect(nameHeaderRect.x, nameHeaderRect.y, nameHeaderRect.height, nameHeaderRect.height));
            var addRect = new Rect(nameHeaderRect.width - nameHeaderRect.height + 2f, nameHeaderRect.y + 2f, nameHeaderRect.height - 4f, nameHeaderRect.height - 4f);
            bool addClicked = addRect.WasLeftClicked();

            if (SettingsHelper.DrawTableHeader(nameHeaderRect) && !addClicked)
            {
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }

            SettingsHelper.DrawSortIndicator(sortNotchRect, _sortOrder);
            SettingsHelper.DrawLabel(nameHeaderTextRect, "Name");

            if (Widgets.ButtonImage(addRect, TexButton.Plus) || addClicked)
            {
                InputDialog.Popup(
                    "Name New Ledger",
                    s => ToolkitPointsSettings.allLedgers.Add(
                        new Ledger
                        {
                            Identifier = s,
                            Active = false
                        }
                    )
                );
            }

            TooltipHandler.TipRegion(addRect, "Click to add a new ledger.");
        }

        private void DrawContent(Rect region)
        {
            var view = new Rect(0f, 0f, region.width - (_hasScrollbars ? 16f : 0f), RowHeight * ToolkitPointsSettings.allLedgers.Count);
            _hasScrollbars = view.height > region.height;

            GUI.BeginGroup(region);
            Widgets.BeginScrollView(region, ref _scrollPos, view);

            var row = 0;
            Ledger toRemove = null;
            foreach (Ledger ledger in GetLedgersInOrder())
            {
                var lineRect = new Rect(0f, RowHeight * row, view.width, RowHeight);

                if (!lineRect.IsRegionVisible(view, _scrollPos))
                {
                    row++;
                    continue;
                }

                if (row % 2 == 0)
                {
                    Widgets.DrawLightHighlight(lineRect);
                }

                if (ledger == ToolkitPointsSettings.activeLedger)
                {
                    Widgets.DrawHighlightSelected(lineRect);
                }

                GUI.BeginGroup(lineRect);
                if (DrawLedgerRow(lineRect.AtZero(), ledger))
                {
                    toRemove = ledger;
                }
                GUI.EndGroup();
                row++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();

            QueryHasResults = row > 0;

            ToolkitPointsSettings.allLedgers.Remove(toRemove);
        }

        private bool DrawLedgerRow(Rect region, Ledger ledger)
        {
            var nameRect = new Rect(0f, 0f, region.width - (region.height - 8f) * 3f, region.height);
            Rect iconRect = SettingsHelper.RectForIcon(new Rect(region.width - region.height, 0f, region.height, region.height));

            SettingsHelper.DrawLabel(nameRect, ledger.Identifier);

            if (Widgets.ButtonInvisible(nameRect))
            {
                ToolkitPointsSettings.activeLedger = ledger;
                ValidateLedgerSettings();
                SelectedLedger = ledger;
            }

            bool isBypassRequested = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (SettingsHelper.DrawFieldIcon(
                    iconRect,
                    TexButton.DeleteX,
                    "Click to delete this ledger. If clicked while holding CONTROL, the confirmation dialog will not appear."
                ))
            {
                InitiateDeleteOperation(ledger, isBypassRequested);
            }

            iconRect = iconRect.ShiftLeft(-8f);
            if (SettingsHelper.DrawFieldIcon(
                    iconRect,
                    TexButton.NewFile,
                    "Click to reset the balances of all viewers within this ledger. If clicked while holding SHIFT, all viewers from the ledger will be deleted. If clicked while holding CTRL, the confirmation dialog will not appear."
                ))
            {
                InitiateResetOperation(ledger, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), isBypassRequested);
            }

            iconRect = iconRect.ShiftLeft(-8f);
            if (SettingsHelper.DrawFieldIcon(iconRect, TexButton.Rename, "Click to rename this ledger"))
            {
                InputDialog.Popup("Rename Ledger", s => ledger.Identifier = s);
            }

            return false;
        }
        private void InitiateDeleteOperation(Ledger ledger, bool isBypassRequested)
        {

            if (isBypassRequested)
            {
                PerformDeleteOperation(ledger);
            }
            else
            {
                ConfirmationDialog.Popup(
                    "Are you sure?",
                    $@"Deleted ledgers can't be undone. Are you sure you want to delete the ledger ""{ledger.Identifier}""?",
                    () => PerformDeleteOperation(ledger)
                );
            }
        }

        private static void InitiateResetOperation(Ledger ledger, bool shouldConfirm, bool isHardReset)
        {

            if (shouldConfirm)
            {
                PerformResetOperation(ledger, isHardReset);
            }
            else
            {
                ConfirmationDialog.Popup(
                    "Are you sure?",
                    (isHardReset ? "Clearing " : "Resetting ") + $@"can't be undone. Are you sure you want to do this to the ledger ""{ledger.Identifier}""?",
                    () => PerformResetOperation(ledger, isHardReset)
                );
            }
        }

        private static void PerformResetOperation(Ledger ledger, bool isHardReset)
        {
            if (isHardReset)
            {
                ledger.Balances.Clear();
                return;
            }

            foreach (ViewerBalance balance in ledger.Balances)
            {
                balance.Points = ToolkitPointsSettings.pointsPerReward;
            }
        }

        private void PerformDeleteOperation(Ledger ledger)
        {
            ToolkitPointsSettings.allLedgers.Remove(ledger);

            if (ToolkitPointsSettings.activeLedger != ledger)
            {
                ValidateLedgerSettings();
                return;
            }

            ToolkitPointsSettings.activeLedger = ToolkitPointsSettings.allLedgers.FirstOrDefault(l => l != ledger);
            ValidateLedgerSettings();
            SelectedLedger = ToolkitPointsSettings.activeLedger;
        }

        private static void ValidateLedgerSettings()
        {
            foreach (Ledger ledger in ToolkitPointsSettings.allLedgers)
            {
                ledger.Active = ToolkitPointsSettings.activeLedger == ledger;
            }

            if (ToolkitPointsSettings.activeLedger == null)
            {
                ToolkitPointsSettings.activeLedger = new Ledger
                {
                    Active = true
                };
            }

            if (ToolkitPointsSettings.allLedgers.Contains(ToolkitPointsSettings.activeLedger))
            {
                return;
            }

            ToolkitPointsSettings.allLedgers.Add(ToolkitPointsSettings.activeLedger);
        }

        public void NotifySearchQueryChanged(string newQuery)
        {
            query = newQuery;
        }

        private IEnumerable<Ledger> GetLedgersInOrder()
        {
            switch (_sortOrder)
            {
                case SortOrder.Ascending:
                    return GetFilteredLedgers().OrderBy(l => l.Identifier);
                case SortOrder.Descending:
                    return GetFilteredLedgers().OrderByDescending(l => l.Identifier);
            }

            return GetFilteredLedgers();
        }

        private IEnumerable<Ledger> GetFilteredLedgers()
        {
            return query.NullOrEmpty() ? ToolkitPointsSettings.allLedgers : ToolkitPointsSettings.allLedgers.Where(l => l.Identifier.ToLower().Contains(query.ToLower()));
        }

        protected virtual void OnLedgerSelected(Ledger l)
        {
            LedgerSelected?.Invoke(this, l);
        }
    }
}
