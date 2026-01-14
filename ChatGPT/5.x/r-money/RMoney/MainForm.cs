using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace RMoney
{
    public sealed class MainForm : Form
    {
        private readonly IReadOnlyDictionary<string, Step> _steps = Flow.Build();
        private readonly List<string> _linear = Flow.LinearOrder().ToList();
        private readonly List<string> _history = new();

        private string _current = "intro";
        private UserState _state = new();

        private readonly TreeView _tree = new();

        private readonly Panel _header = new();
        private readonly Label _brand = new();
        private readonly Label _tagline = new();
        private readonly Label _stepLabel = new();
        private readonly ProgressBar _progress = new();
        private readonly Label _breadcrumb = new();

        private readonly Label _title = new();
        private readonly TextBox _body = new();

        private readonly Panel _moneyPanel = new();
        private readonly TextBox _moneyBox = new();

        private readonly Panel _yesNoPanel = new();
        private readonly RadioButton _yes = new();
        private readonly RadioButton _no = new();

        private readonly Panel _choicePanel = new();
        private readonly List<RadioButton> _choices = new();

        private readonly Panel _summaryPanel = new();
        private readonly ListBox _notes = new();
        private readonly CheckedListBox _actions = new();
        private readonly TextBox _why = new();

        private readonly Button _back = new();
        private readonly Button _next = new();
        private readonly Button _save = new();
        private readonly Button _load = new();
        private readonly Button _reset = new();
        private readonly Button _csv = new();
        private readonly Button _pdf = new();

        public MainForm()
        {
            Text = Branding.AppName;
            Font = Branding.Body;
            BackColor = Branding.Bg;
            ForeColor = Branding.Text;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 760);

            TryLoadIcon();

            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 320, FixedPanel = FixedPanel.Panel1 };
            Controls.Add(split);

            _tree.Dock = DockStyle.Fill;
            _tree.BackColor = Branding.Panel;
            _tree.ForeColor = Branding.Text;
            _tree.BorderStyle = BorderStyle.None;
            _tree.HideSelection = false;
            _tree.AfterSelect += (_, __) =>
            {
                var id = _tree.SelectedNode?.Tag as string;
                if (string.IsNullOrWhiteSpace(id)) return;
                if (id == _current) return;
                if (id == "intro" || _history.Contains(id)) JumpTo(id);
            };
            split.Panel1.Controls.Add(_tree);

            var right = new Panel { Dock = DockStyle.Fill, BackColor = Branding.Bg };
            split.Panel2.Controls.Add(right);

            _header.Dock = DockStyle.Top;
            _header.Height = 92;
            _header.BackColor = Branding.Panel;
            _header.Padding = new Padding(14, 10, 14, 10);
            right.Controls.Add(_header);

            _brand.Text = Branding.AppName;
            _brand.Font = Branding.H1;
            _brand.ForeColor = Branding.Accent;
            _brand.AutoSize = true;
            _brand.Location = new Point(14, 10);

            _tagline.Text = Branding.Tagline;
            _tagline.AutoSize = true;
            _tagline.ForeColor = Branding.Muted;
            _tagline.Location = new Point(16, 44);

            _stepLabel.AutoSize = true;
            _stepLabel.ForeColor = Branding.Text;
            _stepLabel.Location = new Point(320, 16);

            _progress.Width = 460;
            _progress.Height = 16;
            _progress.Location = new Point(320, 40);

            _breadcrumb.AutoSize = true;
            _breadcrumb.ForeColor = Branding.Muted;
            _breadcrumb.Location = new Point(320, 62);

            _header.Controls.Add(_brand);
            _header.Controls.Add(_tagline);
            _header.Controls.Add(_stepLabel);
            _header.Controls.Add(_progress);
            _header.Controls.Add(_breadcrumb);

            _title.Dock = DockStyle.Top;
            _title.Height = 44;
            _title.Font = Branding.H2;
            _title.Padding = new Padding(14, 12, 14, 0);
            right.Controls.Add(_title);

            _body.Dock = DockStyle.Top;
            _body.Multiline = true;
            _body.ReadOnly = true;
            _body.ScrollBars = ScrollBars.Vertical;
            _body.Height = 210;
            _body.BorderStyle = BorderStyle.FixedSingle;
            _body.BackColor = Color.White;
            _body.ForeColor = Color.Black;
            _body.Font = Branding.BodyLarge;
            right.Controls.Add(_body);

            _moneyPanel.Dock = DockStyle.Top;
            _moneyPanel.Height = 66;
            _moneyPanel.Padding = new Padding(14, 10, 14, 10);
            _moneyPanel.Visible = false;
            var moneyLbl = new Label { Text = "Amount ($):", AutoSize = true, ForeColor = Branding.Text, Location = new Point(14, 18) };
            _moneyBox.Width = 240;
            _moneyBox.Location = new Point(112, 14);
            _moneyPanel.Controls.Add(moneyLbl);
            _moneyPanel.Controls.Add(_moneyBox);
            right.Controls.Add(_moneyPanel);

            _yesNoPanel.Dock = DockStyle.Top;
            _yesNoPanel.Height = 66;
            _yesNoPanel.Padding = new Padding(14, 10, 14, 10);
            _yesNoPanel.Visible = false;
            _yes.Text = "Yes"; _yes.AutoSize = true; _yes.Location = new Point(14, 16);
            _no.Text = "No"; _no.AutoSize = true; _no.Location = new Point(80, 16);
            _yesNoPanel.Controls.Add(_yes);
            _yesNoPanel.Controls.Add(_no);
            right.Controls.Add(_yesNoPanel);

            _choicePanel.Dock = DockStyle.Top;
            _choicePanel.Height = 110;
            _choicePanel.Padding = new Padding(14, 10, 14, 10);
            _choicePanel.Visible = false;
            right.Controls.Add(_choicePanel);

            _summaryPanel.Dock = DockStyle.Fill;
            _summaryPanel.Padding = new Padding(14);
            _summaryPanel.Visible = false;

            var notesLbl = new Label { Text = "Key notes", AutoSize = true, Font = Branding.H2, ForeColor = Branding.Text, Location = new Point(14, 10) };
            _notes.Location = new Point(14, 36);
            _notes.Size = new Size(820, 120);

            var actionsLbl = new Label { Text = "Checklist (click item to see WHY)", AutoSize = true, Font = Branding.H2, ForeColor = Branding.Text, Location = new Point(14, 168) };
            _actions.Location = new Point(14, 194);
            _actions.Size = new Size(820, 260);
            _actions.CheckOnClick = true;
            _actions.SelectedIndexChanged += (_, __) => ShowWhy();
            _actions.ItemCheck += (_, e) =>
            {
                if (e.Index >= 0 && e.Index < _state.Plan.Count)
                    _state.Plan[e.Index].Done = (e.NewValue == CheckState.Checked);
            };

            var whyLbl = new Label { Text = "Why this item is here", AutoSize = true, Font = Branding.H2, ForeColor = Branding.Text, Location = new Point(14, 468) };
            _why.Location = new Point(14, 494);
            _why.Size = new Size(820, 140);
            _why.Multiline = true;
            _why.ReadOnly = true;
            _why.ScrollBars = ScrollBars.Vertical;
            _why.Font = Branding.Mono;

            _summaryPanel.Controls.Add(notesLbl);
            _summaryPanel.Controls.Add(_notes);
            _summaryPanel.Controls.Add(actionsLbl);
            _summaryPanel.Controls.Add(_actions);
            _summaryPanel.Controls.Add(whyLbl);
            _summaryPanel.Controls.Add(_why);
            right.Controls.Add(_summaryPanel);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(14, 10, 14, 10), BackColor = Branding.Panel };
            right.Controls.Add(bottom);

            _save.Text = "Save…"; _save.Width = 90; _save.Click += (_, __) => SaveProfile();
            _load.Text = "Load…"; _load.Width = 90; _load.Click += (_, __) => LoadProfile();
            _reset.Text = "Reset"; _reset.Width = 90; _reset.Click += (_, __) => ResetAll();
            _csv.Text = "Export CSV…"; _csv.Width = 110; _csv.Click += (_, __) => ExportCsv();
            _pdf.Text = "Export PDF…"; _pdf.Width = 110; _pdf.Click += (_, __) => ExportPdf();

            _back.Text = "Back"; _back.Width = 90; _back.Click += (_, __) => GoBack();
            _next.Text = "Next"; _next.Width = 90; _next.Click += (_, __) => GoNext();

            bottom.Controls.Add(_save);
            bottom.Controls.Add(_load);
            bottom.Controls.Add(_reset);
            bottom.Controls.Add(_csv);
            bottom.Controls.Add(_pdf);
            bottom.Controls.Add(_back);
            bottom.Controls.Add(_next);

            _save.Location = new Point(14, 14);
            _load.Location = new Point(112, 14);
            _reset.Location = new Point(210, 14);
            _csv.Location = new Point(308, 14);
            _pdf.Location = new Point(428, 14);

            _next.Location = new Point(bottom.Width - 104, 14);
            _next.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _back.Location = new Point(bottom.Width - 206, 14);
            _back.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            AcceptButton = _next;

            BuildTree();
            RenderStep();
        }

        private void TryLoadIcon()
        {
            try
            {
                var devIconPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "rmoney.ico");
                if (System.IO.File.Exists(devIconPath))
                    Icon = new Icon(devIconPath);
            }
            catch { }
        }

        private void BuildTree()
        {
            _tree.Nodes.Clear();
            var root = new TreeNode("r-money flow") { Tag = "intro" };
            _tree.Nodes.Add(root);
            foreach (var id in _linear)
                root.Nodes.Add(new TreeNode(_steps[id].Title) { Tag = id });
            root.Expand();
        }

        private void UpdateTreeStatus()
        {
            foreach (TreeNode root in _tree.Nodes)
                foreach (TreeNode node in root.Nodes)
                {
                    var id = node.Tag as string;
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    bool visited = _history.Contains(id);
                    bool current = id == _current;
                    node.ForeColor = current ? Branding.Accent : (visited ? Branding.Text : Branding.Muted);
                    node.Text = (current ? "▶ " : visited ? "✔ " : "• ") + _steps[id].Title;
                }
        }

        private void RenderStep()
        {
            var step = _steps[_current];
            _title.Text = step.Title;

            int idx = Math.Max(0, _linear.IndexOf(_current));
            _stepLabel.Text = $"Step {idx + 1} of {_linear.Count}";
            _progress.Minimum = 0;
            _progress.Maximum = _linear.Count;
            _progress.Value = Math.Min(_linear.Count, idx + 1);

            var crumbs = _history.Select(h => _steps[h].Title).TakeLast(4).ToList();
            crumbs.Add(step.Title);
            _breadcrumb.Text = "Path: " + string.Join(" → ", crumbs);

            _body.Text = step.Kind switch
            {
                StepKind.Intro or StepKind.Money or StepKind.YesNo or StepKind.Choice => step.Prompt,
                StepKind.Info => step.DynamicText?.Invoke(_state) ?? "",
                StepKind.Summary => "Review your plan. Check items off. Export or Save.",
                _ => ""
            };

            _moneyPanel.Visible = step.Kind == StepKind.Money;
            _yesNoPanel.Visible = step.Kind == StepKind.YesNo;
            _choicePanel.Visible = step.Kind == StepKind.Choice;
            _summaryPanel.Visible = step.Kind == StepKind.Summary;

            if (step.Kind == StepKind.Money) { _moneyBox.Text = ""; _moneyBox.Focus(); }
            if (step.Kind == StepKind.YesNo) { _yes.Checked = false; _no.Checked = false; }
            if (step.Kind == StepKind.Choice) BuildChoices(step);

            if (step.Kind == StepKind.Summary)
            {
                Flow.ComputePlan(_state);
                _notes.Items.Clear();
                _actions.Items.Clear();
                _why.Text = "";

                foreach (var n in _state.Notes) _notes.Items.Add("• " + n);
                for (int i = 0; i < _state.Plan.Count; i++)
                    _actions.Items.Add($"{i + 1}. {_state.Plan[i].Text}", _state.Plan[i].Done);
            }

            _back.Enabled = _history.Count > 0;
            _next.Text = step.Kind == StepKind.Summary ? "Finish" : "Next";

            _csv.Enabled = step.Kind == StepKind.Summary;
            _pdf.Enabled = step.Kind == StepKind.Summary;

            UpdateTreeStatus();
        }

        private void BuildChoices(Step step)
        {
            _choicePanel.Controls.Clear();
            _choices.Clear();
            int y = 12;
            for (int i = 0; i < step.Choices.Length; i++)
            {
                var rb = new RadioButton { Text = step.Choices[i], AutoSize = true, ForeColor = Branding.Text, Location = new Point(14, y) };
                y += 28;
                _choices.Add(rb);
                _choicePanel.Controls.Add(rb);
            }
        }

        private void ShowWhy()
        {
            int idx = _actions.SelectedIndex;
            if (idx < 0 || idx >= _state.Plan.Count) { _why.Text = ""; return; }
            var item = _state.Plan[idx];
            _why.Text = $"ACTION:\r\n{item.Text}\r\n\r\nTRIGGERS:\r\n- {string.Join("\r\n- ", item.Reasons)}";
        }

        private void GoNext()
        {
            var step = _steps[_current];
            if (step.Kind == StepKind.Summary) { Close(); return; }

            if (step.Kind == StepKind.Money)
            {
                if (!TryMoney(_moneyBox.Text, out var v))
                {
                    MessageBox.Show("Enter a valid non-negative dollar amount.", "Invalid amount",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _moneyBox.Focus();
                    return;
                }
                step.RecordMoney?.Invoke(_state, v);
                Navigate(step.NextId);
                return;
            }

            if (step.Kind == StepKind.YesNo)
            {
                bool? ans = _yes.Checked ? true : _no.Checked ? false : (bool?)null;
                if (ans is null)
                {
                    MessageBox.Show("Choose Yes or No.", "Selection required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                step.RecordYesNo?.Invoke(_state, ans.Value);
                Navigate(ans.Value ? step.YesNextId : step.NoNextId);
                return;
            }

            if (step.Kind == StepKind.Choice)
            {
                int chosen = _choices.FindIndex(c => c.Checked);
                if (chosen < 0)
                {
                    MessageBox.Show("Choose one option.", "Selection required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                step.RecordChoice?.Invoke(_state, chosen);
                Navigate(step.NextId);
                return;
            }

            Navigate(step.NextId);
        }

        private void Navigate(string next)
        {
            if (string.IsNullOrWhiteSpace(next)) return;
            _history.Add(_current);
            _current = next;
            RenderStep();
        }

        private void GoBack()
        {
            if (_history.Count == 0) return;
            _current = _history.Last();
            _history.RemoveAt(_history.Count - 1);
            RenderStep();
        }

        private void JumpTo(string id)
        {
            if (id == "intro") { _history.Clear(); _current = "intro"; RenderStep(); return; }
            int pos = _history.IndexOf(id);
            if (pos >= 0)
            {
                _history.RemoveRange(pos + 1, _history.Count - (pos + 1));
                _current = id;
                RenderStep();
            }
        }

        private void ResetAll()
        {
            if (MessageBox.Show("Reset all answers?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _state = new UserState();
            _history.Clear();
            _current = "intro";
            RenderStep();
        }

        private void SaveProfile()
        {
            using var sfd = new SaveFileDialog { Filter = "JSON (*.json)|*.json", Title = "Save r-money profile" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            Flow.ComputePlan(_state);
            ProfileStorage.Save(sfd.FileName, _state);
        }

        private void LoadProfile()
        {
            using var ofd = new OpenFileDialog { Filter = "JSON (*.json)|*.json", Title = "Load r-money profile" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            _state = ProfileStorage.Load(ofd.FileName);
            _history.Clear();
            _current = "budgetCheck";
            RenderStep();
        }

        private void ExportCsv()
        {
            using var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", Title = "Export CSV" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            Exporter.ExportCsv(sfd.FileName, _state);
        }

        private void ExportPdf()
        {
            using var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", Title = "Export PDF" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            Exporter.ExportPdf(sfd.FileName, _state);
        }

        private static bool TryMoney(string text, out decimal v)
        {
            var styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
            if (decimal.TryParse(text, styles, CultureInfo.CurrentCulture, out v) && v >= 0) return true;
            if (decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out v) && v >= 0) return true;
            v = 0; return false;
        }
    }
}

