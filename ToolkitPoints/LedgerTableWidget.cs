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
using UnityEngine;
using Verse;

namespace ToolkitPoints
{
    public enum SortKey { Name, Points }
    public enum SortOrder { Ascending, Descending }

    public class LedgerTableWidget
    {
        private static readonly int TableRowHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
        private bool _hasScrollbars;
        private Vector2 _scrollPos = Vector2.zero;
        private SortKey _sortKey = SortKey.Name;
        private SortOrder _sortOrder = SortOrder.Ascending;
        private string query;

        public bool QueryHasResults { get; set; }

        public Ledger SelectedLedger { get; set; }

        public ViewerBalance SelectedViewer
        {
            get => _selectedViewer;
            set
            {
                _selectedViewer = value;
                OnViewerSelected(_selectedViewer);
            }
        }

        public event EventHandler<ViewerBalance> ViewerSelected;
        private ViewerBalance _selectedViewer;

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
            var nameHeaderRect = new Rect(0f, 0f, Mathf.FloorToInt(width * 0.5f), region.height);
            var nameHeaderTextRect = new Rect(
                _sortKey == SortKey.Name ? region.height + 2f : 0f,
                0f,
                _sortKey == SortKey.Name ? nameHeaderRect.width - region.height - 2f : nameHeaderRect.width,
                nameHeaderRect.height
            );
            var pointsHeaderRect = new Rect(nameHeaderRect.width, 0f, nameHeaderRect.width, region.height);
            var pointsHeaderTextRect = new Rect(
                (_sortKey == SortKey.Points ? pointsHeaderRect.x + region.height : pointsHeaderRect.x) + 2f,
                0f,
                _sortKey == SortKey.Points ? pointsHeaderRect.width - region.height - 2f : pointsHeaderRect.width,
                pointsHeaderRect.height
            );

            if (SettingsHelper.DrawTableHeader(nameHeaderRect))
            {
                _sortOrder = _sortKey != SortKey.Name ? SortOrder.Descending : _sortOrder;
                _sortKey = SortKey.Name;
                NotifySortOrderChanged();
            }

            if (SettingsHelper.DrawTableHeader(pointsHeaderRect))
            {
                _sortOrder = _sortKey != SortKey.Points ? SortOrder.Descending : _sortOrder;
                _sortKey = SortKey.Points;
                NotifySortOrderChanged();
            }

            Rect? sortRect = null;
            switch (_sortKey)
            {
                case SortKey.Name:
                    sortRect = SettingsHelper.RectForIcon(new Rect(nameHeaderRect.x, nameHeaderRect.y, nameHeaderRect.height, nameHeaderRect.height));
                    break;
                case SortKey.Points:
                    sortRect = SettingsHelper.RectForIcon(new Rect(pointsHeaderRect.x, pointsHeaderRect.y, pointsHeaderRect.height, pointsHeaderRect.height));
                    break;
            }

            if (sortRect.HasValue)
            {
                SettingsHelper.DrawSortIndicator(sortRect.Value, _sortOrder);
            }

            SettingsHelper.DrawLabel(nameHeaderTextRect, "Username");
            SettingsHelper.DrawLabel(pointsHeaderTextRect, ToolkitPointsSettings.pointsBaseName);
        }
        private void DrawContent(Rect region)
        {
            var view = new Rect(0f, 0f, region.width - (_hasScrollbars ? 16f : 0f), TableRowHeight * SelectedLedger.Balances.Count);
            _hasScrollbars = view.height > region.height;

            GUI.BeginGroup(region);
            Widgets.BeginScrollView(region, ref _scrollPos, view);

            var row = 0;
            foreach (ViewerBalance balance in GetBalancesInOrder())
            {
                var lineRect = new Rect(0f, TableRowHeight * row, view.width, TableRowHeight);

                if (!lineRect.IsRegionVisible(view, _scrollPos))
                {
                    row++;
                    continue;
                }

                if (row % 2 == 0)
                {
                    Widgets.DrawLightHighlight(lineRect);
                }

                if (balance == SelectedViewer)
                {
                    Widgets.DrawHighlightSelected(lineRect);
                }

                if (Widgets.ButtonInvisible(lineRect))
                {
                    SelectedViewer = balance;
                }

                GUI.BeginGroup(lineRect);
                DrawTableRow(lineRect.AtZero(), balance);
                GUI.EndGroup();
                row++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();

            QueryHasResults = row > 0;
        }
        private static void DrawTableRow(Rect region, ViewerBalance balance)
        {
            var nameRect = new Rect(2f, 0f, Mathf.CeilToInt(region.width * 0.5f) - 2f, region.height);
            var pointsRect = new Rect(nameRect.width + 2f, 0f, nameRect.width, region.height);

            SettingsHelper.DrawLabel(nameRect, balance.Username.CapitalizeFirst());
            SettingsHelper.DrawLabel(pointsRect, balance.Points.ToString("N0"));
        }

        private void NotifySortOrderChanged()
        {
            _sortOrder = _sortOrder == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;
        }
        public void NotifySearchQueryChanged(string newQuery)
        {
            query = newQuery;
        }

        private IEnumerable<ViewerBalance> GetBalancesInOrder()
        {
            switch (_sortKey)
            {
                case SortKey.Name:
                    return OrderBalancesByName();
                case SortKey.Points:
                    return OrderBalancesByPoints();
            }

            return SelectedLedger.Balances;
        }
        private IEnumerable<ViewerBalance> OrderBalancesByName()
        {
            switch (_sortOrder)
            {
                case SortOrder.Ascending:
                    return GetFilteredBalances().OrderBy(v => v.Username);
                case SortOrder.Descending:
                    return GetFilteredBalances().OrderByDescending(v => v.Username);
            }

            return GetFilteredBalances();
        }
        private IEnumerable<ViewerBalance> OrderBalancesByPoints()
        {
            switch (_sortOrder)
            {
                case SortOrder.Ascending:
                    return GetFilteredBalances().OrderBy(v => v.Points).ThenBy(v => v.Username);
                case SortOrder.Descending:
                    return GetFilteredBalances().OrderByDescending(v => v.Points).ThenByDescending(v => v.Username);
            }

            return SelectedLedger.Balances;
        }
        private IEnumerable<ViewerBalance> GetFilteredBalances()
        {
            return query.NullOrEmpty() ? SelectedLedger.Balances : SelectedLedger.Balances.Where(v => v.Username.ToLower().Contains(query.ToLower()));
        }
        protected virtual void OnViewerSelected(ViewerBalance e)
        {
            ViewerSelected?.Invoke(this, e);
        }
    }
}
