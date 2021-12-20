using System;
using System.CodeDom.Compiler;
using System.Linq;
using RimWorld;
using ToolkitCore.Utilities;
using UnityEngine;
using Verse;

namespace ToolkitPoints.Windows
{
    public class LedgerWindow : Window
    {
        private readonly QuickSearchWidget _listSearchWidget;
        private readonly LedgerListWidget _listWidget;
        private readonly QuickSearchWidget _tableSearchWidget;
        private readonly LedgerTableWidget _tableWidget;
        private string finePointsBuffer;
        private bool finePointsBufferInvalid;
        private int finePointsProxy;
        private string pointsBuffer;
        private bool pointsBufferInvalid;
        private bool showingLedgers;
        private bool isTransferAvailable;

        public LedgerWindow()
        {
            _listSearchWidget = new QuickSearchWidget();
            _tableSearchWidget = new QuickSearchWidget();
            _tableWidget = new LedgerTableWidget
            {
                SelectedLedger = ToolkitPointsSettings.activeLedger
            };
            _listWidget = new LedgerListWidget
            {
                SelectedLedger = ToolkitPointsSettings.activeLedger
            };
            _tableWidget.ViewerSelected += OnViewerSelected;
            _listWidget.LedgerSelected += OnLedgerSelected;
            doCloseButton = false;
            doCloseX = false;
        }

        public override Vector2 InitialSize => new Vector2(400, 450);

        private ViewerBalance SelectedViewer => _tableWidget.SelectedViewer;

        private void OnViewerSelected(object sender, ViewerBalance e)
        {
            if (e == null)
            {
                pointsBuffer = null;
                pointsBufferInvalid = false;

                finePointsProxy = 0;
                finePointsBuffer = null;
                finePointsBufferInvalid = false;
                isTransferAvailable = false;
                return;
            }

            pointsBufferInvalid = false;
            pointsBuffer = e.Points.ToString("N0");

            finePointsProxy = 0;
            finePointsBuffer = "0";
            finePointsBufferInvalid = false;

            isTransferAvailable = _listWidget.SelectedLedger.Balances.Count(v => v.Username.Equals(e.Username, StringComparison.InvariantCultureIgnoreCase)) > 1;
        }

        private void OnLedgerSelected(object sender, Ledger l)
        {
            _tableWidget.SelectedLedger = l;

            pointsBuffer = null;
            pointsBufferInvalid = false;

            finePointsProxy = 0;
            finePointsBuffer = null;
            finePointsBufferInvalid = false;
        }

        public override void DoWindowContents(Rect region)
        {
            GameFont fontCache = Text.Font;
            Text.Font = GameFont.Small;

            GUI.BeginGroup(region);

            var headerRegion = new Rect(0f, 0f, region.width, Mathf.CeilToInt(Text.SmallFontHeight * 1.5f));
            var contentRegion = new Rect(0f, headerRegion.height + 2f, region.width, region.height - headerRegion.height * 2f - 4f);
            var footerRegion = new Rect(0f, region.height - headerRegion.height, region.width, headerRegion.height);

            GUI.BeginGroup(headerRegion);

            if (showingLedgers)
            {
                DrawLedgerListHeader(headerRegion);
            }
            else
            {
                DrawLedgerViewHeader(headerRegion);
            }

            GUI.EndGroup();

            GUI.BeginGroup(contentRegion);

            if (showingLedgers)
            {
                DrawLedgerList(contentRegion.AtZero());
            }
            else
            {
                DrawLedgerContent(contentRegion.AtZero());
            }

            GUI.EndGroup();

            GUI.BeginGroup(footerRegion);
            DrawFooter(footerRegion.AtZero());
            GUI.EndGroup();

            GUI.EndGroup();
            Text.Font = fontCache;
        }

        private void DrawFooter(Rect region)
        {
            var closeButton = new Rect(region.width - CloseButSize.x, 0f, CloseButSize.x, region.height);
            Rect resetButton = closeButton.ShiftLeft();

            if (Widgets.ButtonText(closeButton, "Close"))
            {
                _listWidget.SelectedLedger = null;
                _tableWidget.SelectedViewer = null;
                Close();
            }

            if (!showingLedgers && Widgets.ButtonText(resetButton, "Reset all"))
            {
                ConfirmationDialog.Popup(
                    "Are you sure?",
                    $@"Are you sure you want to reset all the viewers within the ""{_tableWidget.SelectedLedger.Identifier}"" ledger to {ToolkitPointsSettings.pointsPerReward:N0} {ToolkitPointsSettings.pointsBaseName}.",
                    ResetAll
                );
            }
        }

        private void ResetAll()
        {
            foreach (ViewerBalance balance in _tableWidget.SelectedLedger.Balances)
            {
                balance.Points = ToolkitPointsSettings.pointsPerReward;
            }
        }

        private void DrawLedgerListHeader(Rect region)
        {
            var buttonRect = new Rect(0f, 0f, Mathf.CeilToInt(region.width * 0.2f), region.height);
            var remainingRect = new Rect(buttonRect.width + 10f, 0f, region.width - 10f - buttonRect.width, region.height);
            float searchWidth = Mathf.CeilToInt(region.width * 0.45f);
            var searchRect = new Rect(remainingRect.x + remainingRect.width, remainingRect.y, searchWidth, Text.SmallFontHeight);

            if (Widgets.ButtonText(buttonRect, "◄ Back"))
            {
                showingLedgers = false;
            }

            _listSearchWidget.OnGUI(searchRect, NotifyQueryChanged);
        }

        private void DrawLedgerViewHeader(Rect region)
        {
            var buttonRect = new Rect(0f, 0f, Mathf.CeilToInt(region.width * (!showingLedgers ? 0.3f : 0.2f)), region.height);
            var remainingRect = new Rect(buttonRect.width + 10f, 0f, region.width - 10f - buttonRect.width, region.height);

            SettingsHelper.DrawColoredLabel(
                ToolkitPointsSettings.useMultipleLedgers ? remainingRect : region,
                ToolkitPointsSettings.useMultipleLedgers ? _listWidget.SelectedLedger.Identifier : "<i>Default Ledger</i>",
                ToolkitPointsSettings.useMultipleLedgers ? Color.white : Color.grey
            );

            if (ToolkitPointsSettings.useMultipleLedgers && Widgets.ButtonText(buttonRect, "Select ledger..."))
            {
                showingLedgers = true;
            }
        }

        private void DrawLedgerContent(Rect region)
        {
            var panelRect = new Rect(4f, 10f, region.width - 8f, Mathf.FloorToInt(region.width * 0.55f) - Text.SmallFontHeight - 20f);
            var leftColumnRect = new Rect(0f, 0f, Mathf.FloorToInt(panelRect.width * 0.5f) - 15f, panelRect.height);
            var rightColumnRect = new Rect(leftColumnRect.width + 30f, 0f, leftColumnRect.width, panelRect.height);
            var tableRect = new Rect(0f, panelRect.height + 16f, region.width, Mathf.FloorToInt(region.height * 0.45f));

            GUI.BeginGroup(panelRect);

            if (SelectedViewer != null)
            {
                GUI.BeginGroup(leftColumnRect);
                DrawLeftColumn(leftColumnRect.AtZero());
                GUI.EndGroup();

                GUI.BeginGroup(rightColumnRect);
                DrawRightColumn(rightColumnRect.AtZero());
                GUI.EndGroup();
            }

            GUI.EndGroup();

            GUI.BeginGroup(tableRect);
            if (_tableWidget.SelectedLedger != null)
            {
                _tableWidget.Draw(tableRect.AtZero());
            }
            GUI.EndGroup();

            float searchWidth = Mathf.CeilToInt(region.width * 0.45f);
            var searchRect = new Rect(region.width - searchWidth, tableRect.y - Text.SmallFontHeight - 2f, searchWidth, Text.SmallFontHeight);
            _tableSearchWidget.OnGUI(searchRect, NotifyQueryChanged);
        }

        private void DrawRightColumn(Rect region)
        {
            float lineHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
            var lineRect = new Rect(0f, 0f, region.width, Text.SmallFontHeight);
            SettingsHelper.DrawLabel(lineRect, "Points");

            lineRect.y += lineRect.height;
            lineRect.height = lineHeight;

            GUI.backgroundColor = pointsBufferInvalid ? Color.red : Color.white;
            if (SettingsHelper.DrawNumberField(lineRect, ref pointsBuffer, out int value, out bool invalid))
            {
                SetPoints(value);
                pointsBufferInvalid = false;
            }
            else if (invalid)
            {
                pointsBufferInvalid = true;
            }
            GUI.backgroundColor = Color.white;

            SettingsHelper.DrawFieldIcon(
                lineRect,
                pointsBufferInvalid ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex,
                pointsBufferInvalid ? $"{pointsBuffer} is not a valid integer" : $"{SelectedViewer.Username}'s balance has been set to {SelectedViewer.Points:N0} coins."
            );

            lineRect.y += lineRect.height + 4f;
            var removePointsRect = new Rect(lineRect.x, lineRect.y, lineRect.height, lineRect.height);
            var addPointsRect = new Rect(lineRect.x + lineRect.width - lineRect.height, lineRect.y, lineRect.height, lineRect.height);
            var pointsEntryRect = new Rect(lineRect.x + lineRect.height + 2f, lineRect.y, lineRect.width - lineRect.height * 2f - 4f, lineRect.height);

            GUI.backgroundColor = finePointsBufferInvalid ? Color.red : Color.white;
            if (SettingsHelper.DrawNumberField(pointsEntryRect, ref finePointsBuffer, out int fineValue, out bool fineInvalid))
            {
                finePointsProxy = fineValue;
                finePointsBufferInvalid = false;
            }
            else if (fineInvalid)
            {
                finePointsBufferInvalid = true;
                return;
            }
            GUI.backgroundColor = Color.white;

            SettingsHelper.DrawFieldIcon(
                pointsEntryRect,
                finePointsBufferInvalid ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex,
                finePointsBufferInvalid
                    ? $"{finePointsBuffer} is not a valid integer"
                    : $"{SelectedViewer.Username}'s will be modified in increments of {finePointsProxy} coins."
            );

            if (Widgets.ButtonText(removePointsRect, "-"))
            {
                RemovePoints(finePointsProxy);
            }

            if (Widgets.ButtonText(addPointsRect, "+"))
            {
                AddPoints(finePointsProxy);
            }

            TooltipHandler.TipRegion(addPointsRect, $"Click to add {finePointsProxy:N0} point(s) to {SelectedViewer.Username}'s balance");
            TooltipHandler.TipRegion(removePointsRect, $"Click to remove {finePointsProxy:N0} point(s) from {SelectedViewer.Username}'s balance");
        }

        private void DrawLeftColumn(Rect region)
        {
            if (_tableWidget.SelectedViewer == null)
            {
                return;
            }

            float lineHeight = Mathf.CeilToInt(Text.SmallFontHeight * 1.25f);
            var lineRect = new Rect(0f, 0f, region.width, Text.SmallFontHeight);
            SettingsHelper.DrawLabel(lineRect, "Username");

            lineRect.y += lineRect.height;
            lineRect.height = lineHeight;

            if (SettingsHelper.DrawTextField(lineRect, SelectedViewer.Username, out string result))
            {
                SelectedViewer.Username = result;
                isTransferAvailable =
                    _listWidget.SelectedLedger.Balances.Count(v => v.Username.Equals(SelectedViewer.Username, StringComparison.InvariantCultureIgnoreCase)) > 1;
            }

            if (isTransferAvailable && SettingsHelper.DrawFieldIcon(lineRect, "⇄", $"Click to merge all accounts named {SelectedViewer.Username}."))
            {
                var surrogate = new ViewerBalance
                {
                    Username = SelectedViewer.Username,
                    Points = _listWidget.SelectedLedger.Balances.Where(v => v.Username.Equals(SelectedViewer.Username, StringComparison.InvariantCultureIgnoreCase))
                       .Sum(v => v.Points)
                };
                _tableWidget.SelectedViewer = surrogate;
                _listWidget.SelectedLedger.Balances.RemoveAll(v => v.Username.Equals(SelectedViewer.Username));
                _listWidget.SelectedLedger.Balances.Add(surrogate);
                isTransferAvailable = false;
            }
        }

        private void DrawLedgerList(Rect region)
        {
            GUI.BeginGroup(region);

            _listWidget.Draw(region.AtZero());

            GUI.EndGroup();
        }

        private void NotifyQueryChanged()
        {
            if (showingLedgers)
            {
                _listWidget.NotifySearchQueryChanged(_listSearchWidget.filter.Text);
            }
            else
            {
                _tableWidget.NotifySearchQueryChanged(_tableSearchWidget.filter.Text);
            }
        }

        private void SetPoints(int amount, bool invalidateBuffer = false)
        {
            SelectedViewer.Points = amount;

            if (!invalidateBuffer)
            {
                return;
            }

            pointsBuffer = amount.ToString("N0");
        }

        private void AddPoints(int amount)
        {
            SetPoints(SelectedViewer.Points + amount, true);
        }

        private void RemovePoints(int amount)
        {
            SetPoints(SelectedViewer.Points - amount, true);
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);

            SaveLoadUtility.SaveAll();
        }
    }
}
