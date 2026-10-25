using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GXCleanerFiveM
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        const int Edge = 28;
        const int Gutter = 20;
        const int Pad = 22;
        const int ContentTop = 84;
        const string IdleListMessage = "Clique em \"Ver o que vai sair\" para montar a lista. Depois é só desmarcar o que quiser manter.";

        readonly PathField fiveMField;
        readonly PathField gtaField;
        readonly GameCard cleanFiveM;
        readonly GameCard cleanGta;
        readonly GxButton browseFiveMButton = new GxButton("Trocar pasta", ButtonKind.Secondary, Glyph.Folder);
        readonly GxButton browseGtaButton = new GxButton("Trocar pasta", ButtonKind.Secondary, Glyph.Folder);
        readonly GxButton detectButton = new GxButton("Procurar de novo", ButtonKind.Ghost, Glyph.Search);
        readonly GxButton simulateButton = new GxButton("Ver o que vai sair", ButtonKind.Secondary, Glyph.Eye);
        readonly GxButton cleanButton = new GxButton("Limpar agora", ButtonKind.Primary, Glyph.Trash);
        readonly StatusChip statusChip = new StatusChip();
        readonly ResultView result = new ResultView();
        readonly ItemList itemList = new ItemList();

        readonly Bitmap logo = Theme.LoadImage("GXLogo");
        readonly Bitmap fiveMImage = Theme.LoadImage("GXFiveM");
        readonly Bitmap gtaImage = Theme.LoadImage("GXGta");

        Rectangle leftPanel, rightPanel, logRect;
        Point step1, step2, step3, resultTitle;
        List<CleanTarget> currentTargets = new List<CleanTarget>();
        bool ready;
        bool busy;
        bool cleaning;
        bool listReady;

        public Bitmap Scene { get; private set; }

        public MainForm()
        {
            Text = "GX Limpeza - FiveM / GTA V";
            Icon icon = LoadEmbeddedIcon("GXCleanerIcon");
            if (icon != null) Icon = icon;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1164, 650);
            MinimumSize = SizeFromClientSize(new Size(984, 600));
            BackColor = Theme.Deep;
            ForeColor = Theme.Ink;
            Font = Theme.Body;
            DoubleBuffered = true;

            fiveMField = new PathField(fiveMImage, "FiveM");
            gtaField = new PathField(gtaImage, "GTA V");
            cleanFiveM = new GameCard("FiveM", "Apaga a pasta mods, os plugins, o ReShade, o cache de shaders e a pasta citizen (o FiveM baixa ela de novo quando abrir).", fiveMImage);
            cleanGta = new GameCard("GTA V", "Apaga ReShade, ENB, ScriptHook, OpenIV, menus .asi e as pastas mods e scripts da pasta do jogo.", gtaImage);

            BuildUi();
            ready = true;
            Shown += delegate { DetectPaths(); };
        }

        void BuildUi()
        {
            browseFiveMButton.AccessibleName = "Trocar pasta do FiveM";
            browseGtaButton.AccessibleName = "Trocar pasta do GTA V";
            browseFiveMButton.Click += delegate { BrowseFiveM(); };
            browseGtaButton.Click += delegate { BrowseGta(); };
            detectButton.Click += delegate { DetectPaths(); };
            simulateButton.Click += delegate { RunCleanup(true); };
            cleanButton.Click += delegate { RunCleanup(false); };

            itemList.SelectionChanged += delegate { ShowSelectionResult(); };
            cleanFiveM.CheckedChanged += delegate { ChoicesChanged(); };
            cleanGta.CheckedChanged += delegate { ChoicesChanged(); };

            Controls.AddRange(new Control[]
            {
                statusChip, detectButton, fiveMField, browseFiveMButton, gtaField, browseGtaButton,
                cleanFiveM, cleanGta, simulateButton, cleanButton, result, itemList
            });
            ShowIdleResult();
            RefreshPathState();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dark = 1;
                Native.DwmSetWindowAttribute(Handle, 20, ref dark, 4);
                int caption = Theme.Deep.R | (Theme.Deep.G << 8) | (Theme.Deep.B << 16);
                Native.DwmSetWindowAttribute(Handle, 35, ref caption, 4);
            }
            catch { }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutUi();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutUi();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cleaning && e.CloseReason == CloseReason.UserClosing)
            {
                MessageBox.Show(this, "Espere a limpeza terminar antes de fechar.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Scene == null)
            {
                e.Graphics.Clear(Theme.Deep);
                return;
            }
            e.Graphics.DrawImage(Scene, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }

        void LayoutUi()
        {
            if (!ready) return;
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w < 400 || h < 400) return;

            SuspendLayout();
            statusChip.SetBounds(w - Edge - 280, 22, 280, 32);

            int leftWidth = (int)((w - 2 * Edge - Gutter) * 0.6f);
            leftPanel = new Rectangle(Edge, ContentTop, leftWidth, h - Edge - ContentTop);
            rightPanel = new Rectangle(leftPanel.Right + Gutter, ContentTop, w - Edge - leftPanel.Right - Gutter, leftPanel.Height);

            int x = leftPanel.X + Pad, inner = leftPanel.Width - 2 * Pad, y = leftPanel.Y + Pad;
            step1 = new Point(x, y);
            detectButton.SetBounds(x + inner - 160, y - 2, 160, 30);
            y += 26 + 14;

            const int fieldH = 46, browseW = 136;
            fiveMField.SetBounds(x, y, inner - browseW - 10, fieldH);
            browseFiveMButton.SetBounds(x + inner - browseW, y, browseW, fieldH);
            y += fieldH + 10;
            gtaField.SetBounds(x, y, inner - browseW - 10, fieldH);
            browseGtaButton.SetBounds(x + inner - browseW, y, browseW, fieldH);
            y += fieldH + 26;
            step2 = new Point(x, y);
            y += 26 + 14;

            int actionsY = leftPanel.Bottom - Pad - 52;
            step3 = new Point(x, actionsY - 14 - 26);
            int cardH = Math.Max(96, step3.Y - 26 - y);
            int cardW = (inner - 14) / 2;
            cleanFiveM.SetBounds(x, y, cardW, cardH);
            cleanGta.SetBounds(x + inner - cardW, y, cardW, cardH);

            int simulateW = (inner - 12) * 9 / 20;
            simulateButton.SetBounds(x, actionsY, simulateW, 52);
            cleanButton.SetBounds(x + simulateW + 12, actionsY, inner - simulateW - 12, 52);

            int rx = rightPanel.X + Pad, rinner = rightPanel.Width - 2 * Pad, ry = rightPanel.Y + Pad;
            resultTitle = new Point(rx, ry);
            ry += 26 + 14;
            result.SetBounds(rx, ry, rinner, 68);
            ry += 68 + 16;
            logRect = new Rectangle(rx, ry, rinner, rightPanel.Bottom - Pad - ry);
            itemList.SetBounds(logRect.X + 4, logRect.Y + 8, logRect.Width - 8, logRect.Height - 16);
            ResumeLayout();

            RebuildScene();
            Invalidate(true);
        }

        // Tudo que e fixo (paineis, titulos) vira uma imagem so; os controles copiam o pedaco que fica atras deles.
        void RebuildScene()
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Theme.Deep);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                PaintHeader(g);
                PaintPanel(g, leftPanel);
                PaintPanel(g, rightPanel);
                PaintStep(g, step1, "1", "Confira onde está o jogo");
                PaintStep(g, step2, "2", "Escolha o que limpar");
                PaintStep(g, step3, "3", "Confira a lista e limpe");
                Theme.DrawText(g, "O que aconteceu", Theme.Title, Theme.Ink, new RectangleF(resultTitle.X, resultTitle.Y, rightPanel.Width - 2 * Pad, 26));

                using (GraphicsPath path = Theme.Round(Theme.Inner(logRect), 12))
                using (var fill = new SolidBrush(Theme.Inset))
                using (var pen = new Pen(Theme.InsetBorder))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
            }

            Bitmap old = Scene;
            Scene = bmp;
            if (old != null) old.Dispose();
        }

        void PaintHeader(Graphics g)
        {
            int x = Edge, y = 23, logoH = 30;
            if (logo != null)
            {
                int logoW = logo.Width * logoH / logo.Height;
                g.DrawImage(logo, new Rectangle(x, y, logoW, logoH));
                x += logoW + 14;
                using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255)))
                    g.DrawLine(pen, x, y + 5, x, y + logoH - 5);
                x += 14;
            }
            Theme.DrawText(g, "LIMPEZA DE MODS", Theme.Brand, Theme.Ink, new RectangleF(x, y, 320, logoH));
        }

        void PaintPanel(Graphics g, Rectangle r)
        {
            using (GraphicsPath path = Theme.Round(Theme.Inner(r), 18))
            using (var fill = new SolidBrush(Theme.Panel))
            using (var pen = new Pen(Theme.PanelBorder))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }

        void PaintStep(Graphics g, Point p, string number, string title)
        {
            var badge = new RectangleF(p.X, p.Y, 26, 26);
            using (var fill = new SolidBrush(Theme.Accent))
                g.FillEllipse(fill, badge);
            Theme.DrawText(g, number, Theme.Badge, Theme.OnAccent, badge, StringAlignment.Center, false, StringTrimming.None);
            Theme.DrawText(g, title, Theme.Title, Theme.Ink, new RectangleF(p.X + 38, p.Y, 420, 26));
        }

        void SetStatus(string value, Color color)
        {
            statusChip.Set(value, color);
            statusChip.Update();
        }

        void ShowIdleResult()
        {
            result.SetResult("", Theme.Accent, "Nada foi apagado ainda");
        }

        void RefreshPathState()
        {
            bool fiveOk = IsFiveMApp(fiveMField.Value);
            bool gtaOk = IsGtaFolder(gtaField.Value);
            fiveMField.Ok = fiveOk;
            gtaField.Ok = gtaOk;
            cleanFiveM.Warning = fiveOk ? null : "pasta não encontrada";
            cleanGta.Warning = gtaOk ? null : "pasta não encontrada";
        }

        void SetBusy(bool on)
        {
            busy = on;
            foreach (Control c in new Control[] { browseFiveMButton, browseGtaButton, detectButton, simulateButton, cleanButton, cleanFiveM, cleanGta })
                c.Enabled = !on;
        }

        Icon LoadEmbeddedIcon(string name)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (stream == null) return null;
                    return new Icon(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        void DetectPaths()
        {
            if (busy) return;
            listReady = false;
            itemList.ShowMessage("Procurando o FiveM e o GTA V...");
            SetBusy(true);
            SetSearching(true);
            SetStatus("Procurando...", Theme.Accent);

            // A busca olha atalhos, registro e discos; roda fora da tela para a janela nao travar.
            string current = fiveMField.Value;
            var worker = new Thread(() =>
            {
                string fiveM = FindFiveMApp(current);
                string gta = String.IsNullOrWhiteSpace(fiveM) ? null : FindGta(fiveM);
                BeginInvoke(new Action(() => ApplyDetectedPaths(fiveM, gta)));
            });
            worker.IsBackground = true;
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        void ApplyDetectedPaths(string fiveM, string gta)
        {
            SetBusy(false);
            SetSearching(false);

            if (String.IsNullOrWhiteSpace(fiveM))
            {
                fiveMField.Value = "";
                gtaField.Value = "";
                RefreshPathState();
                itemList.ShowMessage("Não achei o FiveM. Clique em Trocar pasta e escolha a pasta FiveM.app.");
                SetStatus("FiveM não encontrado", Theme.Bad);
                return;
            }

            fiveMField.Value = fiveM;
            gtaField.Value = gta ?? "";
            RefreshPathState();
            itemList.ShowMessage(IdleListMessage);
            SetStatus("FiveM encontrado", Theme.Good);
            ShowIdleResult();
        }

        void SetSearching(bool on)
        {
            fiveMField.Searching = on;
            gtaField.Searching = on;
            if (!on) return;
            cleanFiveM.Warning = null;
            cleanGta.Warning = null;
        }

        void BrowseFiveM()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta FiveM.app";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!IsFiveMApp(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "Essa pasta não parece ser a FiveM.app (falta o arquivo CitizenFX.ini).", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                fiveMField.Value = dialog.SelectedPath;
                string gta = FindGta(dialog.SelectedPath);
                if (IsGtaFolder(gta)) gtaField.Value = gta;
                RefreshPathState();
                PathsChanged();
                SetStatus("FiveM encontrado", Theme.Good);
            }
        }

        void BrowseGta()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta do GTA V (onde fica o GTA5.exe)";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!IsGtaFolder(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "Essa pasta não tem o GTA5.exe. Escolha a pasta onde o GTA V está instalado.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                gtaField.Value = dialog.SelectedPath;
                RefreshPathState();
                PathsChanged();
            }
        }

        // Caminho trocado: a lista antiga nao vale mais.
        void PathsChanged()
        {
            listReady = false;
            itemList.ShowMessage(IdleListMessage);
            ShowIdleResult();
        }

        // Cartao FiveM / GTA V marcado ou desmarcado: refaz a lista se ela estiver na tela.
        void ChoicesChanged()
        {
            if (!cleanFiveM.Checked && !cleanGta.Checked)
            {
                PathsChanged();
                return;
            }
            if (listReady) BuildList();
        }

        bool BuildList()
        {
            string fiveM = fiveMField.Value.Trim();
            string gta = gtaField.Value.Trim();
            if (!IsFiveMApp(fiveM))
            {
                MessageBox.Show(this, "Não achei a pasta FiveM.app. Clique em Procurar de novo ou em Trocar pasta.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!cleanFiveM.Checked && !cleanGta.Checked)
            {
                MessageBox.Show(this, "Marque pelo menos um dos dois: FiveM ou GTA V.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            var games = new List<string>();
            var notes = new Dictionary<string, string>();
            var targets = new List<CleanTarget>();
            if (cleanFiveM.Checked)
            {
                games.Add("FiveM");
                CollectFiveMTargets(fiveM, targets);
            }
            if (cleanGta.Checked)
            {
                games.Add("GTA V");
                if (IsGtaFolder(gta)) CollectGtaTargets(gta, targets);
                else notes["GTA V"] = "pasta não encontrada, ficou de fora";
            }

            currentTargets = targets;
            listReady = true;
            itemList.SetItems(games, targets, notes);
            if (targets.Count == 0) ShowAlreadyClean();
            else
            {
                ShowSelectionResult();
                SetStatus("Lista pronta", Theme.Accent);
            }
            return true;
        }

        void ShowSelectionResult()
        {
            int count = currentTargets.Count(t => t.Selected);
            result.SetResult(count.ToString(), Theme.Accent, count == 1 ? "item vai sair" : "itens vão sair");
        }

        void RunCleanup(bool whatIf)
        {
            if (busy) return;
            // "Ver o que vai sair" sempre monta a lista de novo; "Limpar agora" usa a lista da tela, ou monta uma se nao tiver.
            if ((whatIf || !listReady) && !BuildList()) return;
            if (whatIf || currentTargets.Count == 0) return;

            List<CleanTarget> selected = currentTargets.Where(t => t.Selected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Nenhum item marcado na lista.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int fiveCount = selected.Count(t => t.Game == "FiveM");
            int gtaCount = selected.Count - fiveCount;
            string message = "Vão ser apagados " + Plural(selected.Count, "item", "itens") + ":\n\n";
            if (fiveCount > 0) message += "      FiveM: " + fiveCount + "\n";
            if (gtaCount > 0) message += "      GTA V: " + gtaCount + "\n";
            message += "\nIsso não dá para desfazer.";
            string running = RunningGame();
            if (running != null) message += "\n\nO " + running + " está aberto. Feche antes, senão alguns arquivos não vão sair.";
            message += "\n\nPode apagar?";
            if (MessageBox.Show(this, message, "Confirmar limpeza", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            SetBusy(true);
            cleaning = true;
            itemList.Locked = true;
            SetStatus("Limpando...", Theme.Accent);
            result.SetResult("0/" + selected.Count, Theme.Accent, "Limpando...");

            var worker = new Thread(() =>
            {
                int removed = 0, failed = 0;
                foreach (CleanTarget t in selected)
                {
                    string error = DeletePath(t.FullPath);
                    if (error == null) removed++;
                    else failed++;
                    int progress = removed + failed;
                    BeginInvoke(new Action(() =>
                    {
                        t.Error = error;
                        t.Done = true;
                        itemList.Reveal(t);
                        result.SetResult(progress + "/" + selected.Count, Theme.Accent, "Limpando...");
                    }));
                }
                BeginInvoke(new Action(() => FinishCleanup(removed, failed)));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        void FinishCleanup(int removed, int failed)
        {
            cleaning = false;
            listReady = false;
            SetBusy(false);
            string label = removed == 1 ? "item removido" : "itens removidos";
            if (failed == 0)
            {
                result.SetResult(removed.ToString(), Theme.Good, label);
                SetStatus("Limpeza concluída", Theme.Good);
            }
            else
            {
                result.SetResult(removed.ToString(), Theme.Accent, label + " · " + failed + " com erro");
                SetStatus("Terminou com erros", Theme.Bad);
            }
        }

        void ShowAlreadyClean()
        {
            result.SetResult("0", Theme.Good, "Já está limpo");
            SetStatus("Já está limpo", Theme.Good);
        }

        static string Plural(int count, string one, string many)
        {
            return count + " " + (count == 1 ? one : many);
        }

        static string RunningGame()
        {
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    if (p.ProcessName.StartsWith("FiveM", StringComparison.OrdinalIgnoreCase)) return "FiveM";
                    if (p.ProcessName.Equals("GTA5", StringComparison.OrdinalIgnoreCase)) return "GTA V";
                }
            }
            catch { }
            return null;
        }

        static string FindFiveMApp(string providedPath)
        {
            var candidates = new List<string>();
            AddFiveMPathCandidates(candidates, providedPath);

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AddLocalAppDataCandidates(candidates, local);

            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.ProcessName.StartsWith("FiveM", StringComparison.OrdinalIgnoreCase))
                            AddFiveMPathCandidates(candidates, p.MainModule.FileName);
                    }
                    catch { }
                }
            }
            catch { }

            AddShortcutCandidates(candidates);
            AddRegistryCandidates(candidates);

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            {
                AddFiveMPathCandidates(candidates, Path.Combine(drive.RootDirectory.FullName, "FiveM"));
                AddFiveMPathCandidates(candidates, Path.Combine(drive.RootDirectory.FullName, "Games", "FiveM"));
                AddFiveMPathCandidates(candidates, Path.Combine(drive.RootDirectory.FullName, "Jogos", "FiveM"));
            }

            foreach (var candidate in candidates.Where(s => !String.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (IsFiveMApp(candidate)) return Path.GetFullPath(candidate);
            }

            return null;
        }

        static void AddFiveMPathCandidates(List<string> list, string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return;
            string clean = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            list.Add(clean);

            if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                clean = Path.GetDirectoryName(clean);
                if (!String.IsNullOrWhiteSpace(clean)) list.Add(clean);
            }

            if (!String.IsNullOrWhiteSpace(clean))
            {
                list.Add(Path.Combine(clean, "FiveM.app"));
                list.Add(Path.Combine(clean, "FiveM", "FiveM.app"));
            }
        }

        static void AddLocalAppDataCandidates(List<string> list, string local)
        {
            AddFiveMPathCandidates(list, Path.Combine(local, "FiveM"));
            AddFiveMPathCandidates(list, Path.Combine(local, "FFiveM2", "FiveM"));
            AddFiveMPathCandidates(list, Path.Combine(local, "CitizenFX"));
        }

        static void AddShortcutCandidates(List<string> list)
        {
            try
            {
                var roots = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
                }.Where(Directory.Exists);

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                foreach (string root in roots)
                {
                    foreach (string link in Directory.GetFiles(root, "*.lnk", SearchOption.AllDirectories))
                    {
                        if (!Regex.IsMatch(Path.GetFileName(link), "FiveM|CitizenFX", RegexOptions.IgnoreCase)) continue;
                        try
                        {
                            dynamic shortcut = shell.CreateShortcut(link);
                            AddFiveMPathCandidates(list, shortcut.TargetPath);
                            AddFiveMPathCandidates(list, shortcut.WorkingDirectory);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        static void AddRegistryCandidates(List<string> list)
        {
            string[] roots =
            {
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var baseKey in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (string root in roots)
                {
                    try
                    {
                        using (var key = baseKey.OpenSubKey(root))
                        {
                            if (key == null) continue;
                            foreach (string name in key.GetSubKeyNames())
                            {
                                using (var sub = key.OpenSubKey(name))
                                {
                                    if (sub == null) continue;
                                    string display = Convert.ToString(sub.GetValue("DisplayName"));
                                    string install = Convert.ToString(sub.GetValue("InstallLocation"));
                                    string icon = Convert.ToString(sub.GetValue("DisplayIcon"));
                                    string all = (display + " " + install + " " + icon);
                                    if (Regex.IsMatch(all, "FiveM|CitizenFX", RegexOptions.IgnoreCase))
                                    {
                                        AddFiveMPathCandidates(list, install);
                                        AddFiveMPathCandidates(list, icon);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        static bool IsFiveMApp(string path)
        {
            try { return Directory.Exists(path) && File.Exists(Path.Combine(path, "CitizenFX.ini")); }
            catch { return false; }
        }

        static bool IsGtaFolder(string path)
        {
            try
            {
                return Directory.Exists(path) &&
                    (File.Exists(Path.Combine(path, "GTA5.exe")) ||
                     File.Exists(Path.Combine(path, "PlayGTAV.exe")) ||
                     File.Exists(Path.Combine(path, "GTA5_Enhanced.exe")));
            }
            catch { return false; }
        }

        static string FindGta(string fiveM)
        {
            string gta = GetGtaPathFromCitizenFxIni(Path.Combine(fiveM, "CitizenFX.ini"));
            if (IsGtaFolder(gta)) return gta;

            foreach (string keyPath in new[] { @"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V", @"SOFTWARE\Rockstar Games\Grand Theft Auto V" })
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (key == null) continue;
                        foreach (string value in new[] { "InstallFolder", "InstallFolderSteam", "InstallFolderEpic" })
                        {
                            string path = Convert.ToString(key.GetValue(value)).Trim().Trim('"');
                            if (IsGtaFolder(path)) return Path.GetFullPath(path);
                        }
                    }
                }
                catch { }
            }

            return gta;
        }

        static string GetGtaPathFromCitizenFxIni(string iniPath)
        {
            try
            {
                foreach (string line in File.ReadAllLines(iniPath))
                {
                    var match = Regex.Match(line.Trim(), @"^(IVPath|GTAVPath|GTAPath)\s*=\s*(.+)$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string path = match.Groups[2].Value.Trim().Trim('"');
                        if (Directory.Exists(path)) return Path.GetFullPath(path);
                    }
                }
            }
            catch { }
            return null;
        }

        void CollectFiveMTargets(string fiveM, List<CleanTarget> targets)
        {
            string[] folders =
            {
                "mods",
                "plugins",
                "reshade-shaders",
                "ReShade",
                "ShaderCache",
                "CitizenFX\\cache\\priv",
                "citizen"
            };

            foreach (string folder in folders)
            {
                string target = Path.Combine(fiveM, folder);
                if (folder.Equals("mods", StringComparison.OrdinalIgnoreCase) ||
                    folder.Equals("plugins", StringComparison.OrdinalIgnoreCase))
                {
                    // mods e plugins ficam, so o conteudo sai
                    if (!Directory.Exists(target)) continue;
                    try
                    {
                        foreach (string entry in Directory.GetFileSystemEntries(target))
                            targets.Add(new CleanTarget("FiveM", fiveM, entry));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Não consegui ler a pasta " + folder + ": " + ex.Message, "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (File.Exists(target) || Directory.Exists(target))
                {
                    targets.Add(new CleanTarget("FiveM", fiveM, target));
                }
            }
        }

        void CollectGtaTargets(string gta, List<CleanTarget> targets)
        {
            string[] names =
            {
                "mods",
                "scripts",
                "plugins",
                "reshade-shaders",
                "enbseries",
                "shaderinput",
                "menyooStuff",
                "QuantV",
                "NVE",
                "NaturalVision Evolved",
                "d3d11.dll",
                "d3d12.dll",
                "dxgi.dll",
                "dinput8.dll",
                "ScriptHookV.dll",
                "ScriptHookVDotNet.asi",
                "ScriptHookVDotNet2.dll",
                "ScriptHookVDotNet3.dll",
                "NativeTrainer.asi",
                "Menyoo.asi",
                "OpenIV.asi",
                "openCameraV.asi",
                "HeapAdjuster.asi",
                "PackfileLimitAdjuster.asi",
                "Gameconfig.xml",
                "ReShade.ini",
                "ReShade.log",
                "ReShadePreset.ini",
                "enblocal.ini",
                "enbseries.ini",
                "enbfeeder.ini",
                "enbfeeder.asi",
                "enbseries.h",
                "enbadaptation.fx",
                "enbbloom.fx",
                "enbeffect.fx",
                "enbeffectpostpass.fx",
                "enbeffectprepass.fx",
                "enblens.fx",
                "enblightsprite.fx",
                "OpenIV.log",
                "openCameraV.log"
            };

            foreach (string name in names)
            {
                string target = Path.Combine(gta, name);
                if (File.Exists(target) || Directory.Exists(target))
                    targets.Add(new CleanTarget("GTA V", gta, target));
            }
        }

        static string DeletePath(string target)
        {
            try
            {
                if (Directory.Exists(target)) Directory.Delete(target, true);
                else if (File.Exists(target)) File.Delete(target);
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return "sem permissão: feche o jogo ou abra como administrador";
            }
            catch (IOException)
            {
                return "em uso: feche o FiveM e o GTA V";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    class CleanTarget
    {
        public readonly string Game;
        public readonly string Root;
        public readonly string FullPath;
        public bool Selected = true;
        public bool Done;
        public string Error;

        public CleanTarget(string game, string root, string fullPath)
        {
            Game = game;
            Root = root;
            FullPath = fullPath;
        }

        public string Display
        {
            get
            {
                if (!FullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) return FullPath;
                return FullPath.Substring(Root.Length).TrimStart('\\', '/');
            }
        }
    }

    static class Theme
    {
        public static readonly Color Deep = Color.FromArgb(11, 13, 18);
        public static readonly Color Panel = Color.FromArgb(16, 19, 26);
        public static readonly Color PanelBorder = Color.FromArgb(34, 255, 255, 255);
        public static readonly Color Inset = Color.FromArgb(10, 12, 16);
        public static readonly Color InsetBorder = Color.FromArgb(42, 47, 58);
        public static readonly Color Card = Color.FromArgb(21, 24, 32);
        public static readonly Color CardBorder = Color.FromArgb(48, 54, 66);
        public static readonly Color Ink = Color.FromArgb(243, 244, 246);
        public static readonly Color Muted = Color.FromArgb(160, 168, 181);
        public static readonly Color Faint = Color.FromArgb(110, 118, 132);
        public static readonly Color Accent = Color.FromArgb(245, 127, 0);
        public static readonly Color AccentHover = Color.FromArgb(255, 147, 38);
        public static readonly Color AccentDown = Color.FromArgb(214, 108, 0);
        public static readonly Color OnAccent = Color.FromArgb(26, 14, 2);
        public static readonly Color Good = Color.FromArgb(52, 211, 153);
        public static readonly Color Bad = Color.FromArgb(248, 113, 113);

        static readonly HashSet<string> families = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

        public static readonly Font Brand = Pick("Bahnschrift SemiBold", 14F);
        public static readonly Font Title = Pick("Bahnschrift SemiBold", 12.5F);
        public static readonly Font CardTitle = Pick("Bahnschrift SemiBold", 13.5F);
        public static readonly Font ResultHead = Pick("Bahnschrift SemiBold", 13F);
        public static readonly Font FieldLabel = Pick("Bahnschrift SemiBold", 10.5F);
        public static readonly Font Badge = Pick("Bahnschrift SemiBold", 11F);
        public static readonly Font ButtonFont = Pick("Bahnschrift SemiBold", 11F);
        public static readonly Font ButtonStrong = Pick("Bahnschrift SemiBold", 12.5F);
        public static readonly Font Big = Pick("Bahnschrift SemiBold", 27F);
        public static readonly Font Body = new Font("Segoe UI", 10F);
        public static readonly Font Small = new Font("Segoe UI", 9F);
        public static readonly Font Row = new Font("Segoe UI", 9.5F);
        public static readonly Font ListHead = Pick("Bahnschrift SemiBold", 11F);

        static Font Pick(string family, float size)
        {
            return new Font(families.Contains(family) ? family : "Segoe UI Semibold", size);
        }

        public static Bitmap LoadImage(string name)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (stream == null) return null;
                    using (Image img = Image.FromStream(stream))
                    {
                        var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppPArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                            g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
                        return bmp;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap Thumb(Image src, int size)
        {
            if (src == null) return null;
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            using (var attr = new ImageAttributes())
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attr.SetWrapMode(WrapMode.TileFlipXY);
                int s = Math.Min(src.Width, src.Height);
                g.DrawImage(src, new Rectangle(0, 0, size, size), (src.Width - s) / 2, (src.Height - s) / 2, s, s, GraphicsUnit.Pixel, attr);
            }
            return bmp;
        }

        public static void DrawRoundedImage(Graphics g, Image img, RectangleF r, float radius)
        {
            using (var brush = new TextureBrush(img, WrapMode.Clamp))
            using (GraphicsPath path = Round(r, radius))
            {
                brush.TranslateTransform(r.X, r.Y);
                brush.ScaleTransform(r.Width / img.Width, r.Height / img.Height);
                g.FillPath(brush, path);
            }
        }

        // Caixinha de marcar: laranja com o check, um traco quando so parte do grupo esta marcada.
        public static void DrawCheck(Graphics g, RectangleF box, bool on, bool partial, bool dim)
        {
            using (GraphicsPath path = Round(box, box.Width * 0.28f))
            {
                if (!on)
                {
                    using (var pen = new Pen(dim ? Color.FromArgb(60, 66, 80) : Color.FromArgb(96, 104, 120), 1.5f))
                        g.DrawPath(pen, path);
                    return;
                }
                using (var fill = new SolidBrush(dim ? Color.FromArgb(120, Accent) : Accent))
                    g.FillPath(fill, path);
                if (partial)
                {
                    using (var pen = new Pen(OnAccent, Math.Max(1.6f, box.Width * 0.11f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(pen, box.X + box.Width * 0.3f, box.Y + box.Height / 2f, box.Right - box.Width * 0.3f, box.Y + box.Height / 2f);
                }
                else
                {
                    float pad = box.Width * 0.17f;
                    Glyphs.Draw(g, Glyph.Check, RectangleF.Inflate(box, -pad, -pad), OnAccent, Math.Max(1.6f, box.Width * 0.09f));
                }
            }
        }

        public static RectangleF Inner(Rectangle r)
        {
            return new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f);
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d < 1)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static TextRenderingHint Hint(Font f)
        {
            return f.SizeInPoints >= 14 ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.ClearTypeGridFit;
        }

        // Texto pequeno usa o espacamento padrao: no modo tipografico o ponto de "Preset.ini" some embaixo do "t".
        static StringFormat BaseFormat(Font f)
        {
            return new StringFormat(f.SizeInPoints >= 14 ? StringFormat.GenericTypographic : StringFormat.GenericDefault);
        }

        public static SizeF Measure(Graphics g, string s, Font f)
        {
            g.TextRenderingHint = Hint(f);
            using (var sf = BaseFormat(f))
            {
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
                return g.MeasureString(s, f, new PointF(0, 0), sf);
            }
        }

        public static void DrawText(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            DrawText(g, s, f, c, r, StringAlignment.Near, false, StringTrimming.EllipsisCharacter);
        }

        public static void DrawText(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment align, bool wrap, StringTrimming trimming)
        {
            g.TextRenderingHint = Hint(f);
            using (var sf = BaseFormat(f))
            using (var brush = new SolidBrush(c))
            {
                sf.Alignment = align;
                sf.LineAlignment = wrap ? StringAlignment.Near : StringAlignment.Center;
                sf.Trimming = trimming;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                if (!wrap) sf.FormatFlags |= StringFormatFlags.NoWrap;
                g.DrawString(s, f, brush, r, sf);
            }
        }
    }

    static class Native
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }

    static class Backdrop
    {
        // Copia o pedaco da cena que fica atras do controle, para ele parecer transparente.
        public static void Paint(Control c, Graphics g)
        {
            MainForm form = c.FindForm() as MainForm;
            Bitmap scene = form != null ? form.Scene : null;
            if (scene == null)
            {
                g.Clear(Theme.Deep);
                return;
            }
            Point p = form.PointToClient(c.PointToScreen(Point.Empty));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(scene, new Rectangle(0, 0, c.Width, c.Height), new Rectangle(p.X, p.Y, c.Width, c.Height), GraphicsUnit.Pixel);
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.InterpolationMode = InterpolationMode.Default;
        }
    }

    public class SceneControl : Control
    {
        public SceneControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Backdrop.Paint(this, e.Graphics);
        }
    }

    public enum Glyph { None, Folder, Search, Eye, Trash, Check, Alert }

    static class Glyphs
    {
        // Icones desenhados numa caixa de 16x16.
        public static void Draw(Graphics g, Glyph glyph, RectangleF r, Color color, float width = 1.7f)
        {
            float s = r.Width / 16f;
            Func<float, float, PointF> P = (x, y) => new PointF(r.X + x * s, r.Y + y * s);
            using (var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var fill = new SolidBrush(color))
            using (var path = new GraphicsPath())
            {
                switch (glyph)
                {
                    case Glyph.Folder:
                        path.AddLines(new[] { P(1.5f, 3.5f), P(6f, 3.5f), P(7.6f, 5.2f), P(14.5f, 5.2f), P(14.5f, 13.5f), P(1.5f, 13.5f) });
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                        break;
                    case Glyph.Search:
                        g.DrawEllipse(pen, r.X + 1.5f * s, r.Y + 1.5f * s, 9.5f * s, 9.5f * s);
                        g.DrawLine(pen, P(9.8f, 9.8f), P(14.3f, 14.3f));
                        break;
                    case Glyph.Eye:
                        path.AddBezier(P(1f, 8f), P(4f, 3f), P(12f, 3f), P(15f, 8f));
                        path.AddBezier(P(15f, 8f), P(12f, 13f), P(4f, 13f), P(1f, 8f));
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                        g.FillEllipse(fill, r.X + 5.6f * s, r.Y + 5.6f * s, 4.8f * s, 4.8f * s);
                        break;
                    case Glyph.Trash:
                        g.DrawLine(pen, P(2f, 4f), P(14f, 4f));
                        g.DrawLines(pen, new[] { P(6f, 4f), P(6.6f, 1.8f), P(9.4f, 1.8f), P(10f, 4f) });
                        g.DrawLines(pen, new[] { P(3.6f, 4f), P(4.6f, 14.4f), P(11.4f, 14.4f), P(12.4f, 4f) });
                        g.DrawLine(pen, P(6.8f, 7f), P(7f, 11.6f));
                        g.DrawLine(pen, P(9.2f, 7f), P(9f, 11.6f));
                        break;
                    case Glyph.Check:
                        g.DrawLines(pen, new[] { P(3f, 8.5f), P(6.5f, 12f), P(13f, 4.5f) });
                        break;
                    case Glyph.Alert:
                        g.DrawEllipse(pen, r.X + 1f * s, r.Y + 1f * s, 14f * s, 14f * s);
                        g.DrawLine(pen, P(8f, 4.6f), P(8f, 8.8f));
                        g.FillEllipse(fill, r.X + 6.9f * s, r.Y + 10.5f * s, 2.2f * s, 2.2f * s);
                        break;
                }
            }
        }
    }

    public enum ButtonKind { Primary, Secondary, Ghost }

    public class GxButton : Button
    {
        readonly ButtonKind kind;
        readonly Glyph glyph;
        bool hovering;
        bool pressed;

        public GxButton(string caption, ButtonKind kind, Glyph glyph)
        {
            this.kind = kind;
            this.glyph = glyph;
            Text = caption;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            Font = kind == ButtonKind.Primary ? Theme.ButtonStrong : Theme.ButtonFont;
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { if (mevent.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { pressed = false; Invalidate(); base.OnMouseUp(mevent); }
        protected override void OnEnabledChanged(EventArgs e) { hovering = false; pressed = false; Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Backdrop.Paint(this, pevent.Graphics);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Backdrop.Paint(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill, border, fore;
            if (kind == ButtonKind.Primary)
            {
                fill = !Enabled ? Color.FromArgb(110, 120, 66, 10) : pressed ? Theme.AccentDown : hovering ? Theme.AccentHover : Theme.Accent;
                border = fill;
                fore = Enabled ? Theme.OnAccent : Color.FromArgb(150, 255, 255, 255);
            }
            else if (kind == ButtonKind.Secondary)
            {
                fill = pressed ? Color.FromArgb(46, 52, 65) : hovering ? Color.FromArgb(37, 42, 53) : Color.FromArgb(26, 30, 39);
                border = hovering && Enabled ? Color.FromArgb(88, 96, 114) : Color.FromArgb(56, 62, 76);
                fore = Enabled ? Theme.Ink : Theme.Faint;
            }
            else
            {
                fill = hovering ? Color.FromArgb(26, 255, 255, 255) : Color.Transparent;
                border = Color.Transparent;
                fore = !Enabled ? Theme.Faint : hovering ? Theme.Ink : Theme.Muted;
            }

            using (GraphicsPath path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), kind == ButtonKind.Ghost ? 8 : 12))
            {
                if (fill.A > 0)
                    using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                if (border.A > 0)
                    using (var p = new Pen(border)) g.DrawPath(p, path);
            }
            if (Focused && ShowFocusCues)
                using (GraphicsPath ring = Theme.Round(new RectangleF(2.5f, 2.5f, Width - 5f, Height - 5f), 10))
                using (var p = new Pen(Color.FromArgb(220, kind == ButtonKind.Primary ? Theme.OnAccent : Theme.Accent), 1.5f))
                    g.DrawPath(p, ring);

            float icon = glyph == Glyph.None ? 0 : 16;
            float gap = icon > 0 ? 9 : 0;
            float textW = Theme.Measure(g, Text, Font).Width;
            float x = (Width - (icon + gap + textW)) / 2f;
            if (icon > 0) Glyphs.Draw(g, glyph, new RectangleF(x, (Height - 16) / 2f, 16, 16), fore, kind == ButtonKind.Primary ? 1.9f : 1.6f);
            Theme.DrawText(g, Text, Font, fore, new RectangleF(x + icon + gap, 0, textW + 6, Height));
        }
    }

    public class PathField : SceneControl
    {
        readonly Bitmap icon;
        readonly string caption;
        readonly ToolTip tip = new ToolTip();
        string value = "";
        bool ok;
        bool searching;

        public PathField(Image image, string caption)
        {
            this.caption = caption;
            icon = Theme.Thumb(image, 26);
            AccessibleRole = AccessibleRole.StaticText;
            AccessibleName = "Pasta do " + caption;
        }

        public string Value
        {
            get { return value; }
            set
            {
                this.value = value ?? "";
                AccessibleDescription = this.value;
                tip.SetToolTip(this, this.value);
                Invalidate();
            }
        }

        public bool Ok
        {
            get { return ok; }
            set { ok = value; Invalidate(); }
        }

        public bool Searching
        {
            get { return searching; }
            set { searching = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12))
            using (var fill = new SolidBrush(Theme.Inset))
            using (var pen = new Pen(Theme.InsetBorder))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            float cy = Height / 2f;
            if (icon != null) Theme.DrawRoundedImage(g, icon, new RectangleF(12, cy - 13, 26, 26), 7);
            Theme.DrawText(g, caption, Theme.FieldLabel, Theme.Ink, new RectangleF(48, 0, 56, Height));
            using (var pen = new Pen(Theme.InsetBorder))
                g.DrawLine(pen, 106, cy - 11, 106, cy + 11);

            bool empty = value.Length == 0;
            string shown = searching ? "procurando..." : empty ? "não encontrada, clique em Trocar pasta" : value;
            Theme.DrawText(g, shown, Theme.Body, searching || empty ? Theme.Faint : Color.FromArgb(206, 212, 222),
                new RectangleF(118, 0, Width - 118 - 40, Height), StringAlignment.Near, false, StringTrimming.EllipsisPath);

            var mark = new RectangleF(Width - 30, cy - 9, 18, 18);
            if (searching) return;
            if (ok)
            {
                using (var fill = new SolidBrush(Color.FromArgb(46, Theme.Good)))
                    g.FillEllipse(fill, mark);
                Glyphs.Draw(g, Glyph.Check, RectangleF.Inflate(mark, -3.5f, -3.5f), Theme.Good, 1.8f);
            }
            else
            {
                Glyphs.Draw(g, Glyph.Alert, mark, Theme.Bad, 1.6f);
            }
        }
    }

    public class GameCard : SceneControl
    {
        readonly string title;
        readonly string details;
        readonly Bitmap thumb;
        bool isChecked = true;
        bool hovering;
        string warning;

        public GameCard(string title, string details, Image image)
        {
            this.title = title;
            this.details = details;
            thumb = Theme.Thumb(image, 60);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = "Limpar " + title;
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public string Warning
        {
            get { return warning; }
            set { warning = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { hovering = false; Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnClick(EventArgs e)
        {
            Focus();
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = isChecked ? Color.FromArgb(34, 26, 19) : hovering ? Color.FromArgb(27, 31, 40) : Theme.Card;
            Color border = isChecked ? Theme.Accent : hovering ? Color.FromArgb(84, 92, 108) : Theme.CardBorder;
            using (GraphicsPath path = Theme.Round(new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f), 14))
            using (var b = new SolidBrush(fill))
            using (var p = new Pen(border, isChecked ? 1.5f : 1f))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            if (Focused && ShowFocusCues)
                using (GraphicsPath ring = Theme.Round(new RectangleF(3.5f, 3.5f, Width - 7f, Height - 7f), 11))
                using (var p = new Pen(Color.FromArgb(120, Theme.Accent)))
                    g.DrawPath(p, ring);

            var img = new RectangleF(16, 16, 60, 60);
            if (thumb != null) Theme.DrawRoundedImage(g, thumb, img, 12);
            if (!isChecked)
                using (GraphicsPath p = Theme.Round(img, 12))
                using (var b = new SolidBrush(Color.FromArgb(160, 12, 14, 19)))
                    g.FillPath(b, p);

            float tx = 92, tw = Width - tx - 52;
            Theme.DrawText(g, title, Theme.CardTitle, isChecked ? Theme.Ink : Theme.Muted, new RectangleF(tx, 20, tw, 26));
            string state = warning ?? (isChecked ? "vai ser limpo" : "não vai mexer");
            Color stateColor = warning != null ? Theme.Bad : isChecked ? Theme.Accent : Theme.Faint;
            Theme.DrawText(g, state, Theme.Small, stateColor, new RectangleF(tx, 48, tw, 20));

            Theme.DrawCheck(g, new RectangleF(Width - 16 - 24, 18, 24, 24), isChecked, false, false);

            if (Height > 110)
            {
                Theme.DrawText(g, details, Theme.Small, isChecked ? Theme.Muted : Theme.Faint,
                    new RectangleF(16, 90, Width - 32, Height - 90 - 12), StringAlignment.Near, true, StringTrimming.EllipsisWord);
            }
        }
    }

    // Lista do que vai sair: um grupo por jogo, uma caixinha por item, com rolagem propria.
    class ItemList : Control
    {
        class Row
        {
            public string Game;
            public CleanTarget Target;
            public string Note;
            public Color NoteColor;
            public bool Header;
            public int Top;
            public int Height;
        }

        const int RowH = 26;
        const int HeaderH = 32;
        const int Bar = 14;

        readonly List<Row> rows = new List<Row>();
        string message = "";
        int scroll;
        int contentH;
        int hover = -1;
        bool dragging;
        float dragOffset;
        bool locked;

        public event EventHandler SelectionChanged;

        public ItemList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Theme.Inset;
            AccessibleRole = AccessibleRole.List;
            AccessibleName = "Lista do que vai sair";
        }

        public bool Locked
        {
            get { return locked; }
            set { locked = value; hover = -1; Cursor = Cursors.Default; Invalidate(); }
        }

        public void ShowMessage(string text)
        {
            rows.Clear();
            message = text;
            scroll = 0;
            contentH = 0;
            Locked = false;
        }

        public void SetItems(List<string> games, List<CleanTarget> targets, Dictionary<string, string> notes)
        {
            rows.Clear();
            message = "";
            int y = 4;
            foreach (string game in games)
            {
                if (rows.Count > 0) y += 10;
                rows.Add(new Row { Game = game, Header = true, Top = y, Height = HeaderH });
                y += HeaderH;

                List<CleanTarget> items = targets.Where(t => t.Game == game).ToList();
                string note;
                if (notes.TryGetValue(game, out note))
                {
                    rows.Add(new Row { Game = game, Note = note, NoteColor = Theme.Bad, Top = y, Height = RowH });
                    y += RowH;
                }
                else if (items.Count == 0)
                {
                    rows.Add(new Row { Game = game, Note = "nada para limpar aqui", NoteColor = Theme.Good, Top = y, Height = RowH });
                    y += RowH;
                }
                foreach (CleanTarget t in items)
                {
                    rows.Add(new Row { Game = game, Target = t, Top = y, Height = RowH });
                    y += RowH;
                }
            }
            contentH = y + 6;
            scroll = 0;
            Locked = false;
        }

        public void Reveal(CleanTarget target)
        {
            Row row = rows.FirstOrDefault(r => r.Target == target);
            if (row == null) return;
            if (row.Top < scroll) scroll = row.Top - 4;
            else if (row.Top + row.Height > scroll + Height) scroll = row.Top + row.Height - Height + 4;
            ClampScroll();
            Invalidate();
        }

        int MaxScroll
        {
            get { return Math.Max(0, contentH - Height); }
        }

        void ClampScroll()
        {
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll));
        }

        List<Row> ItemsOf(string game)
        {
            return rows.Where(r => r.Game == game && r.Target != null).ToList();
        }

        int HitRow(Point p)
        {
            if (MaxScroll > 0 && p.X >= Width - Bar) return -1;
            int y = p.Y + scroll;
            for (int i = 0; i < rows.Count; i++)
                if (y >= rows[i].Top && y < rows[i].Top + rows[i].Height) return i;
            return -1;
        }

        bool CanToggle(int index)
        {
            if (locked || index < 0) return false;
            Row row = rows[index];
            return row.Header ? ItemsOf(row.Game).Count > 0 : row.Target != null;
        }

        RectangleF Thumb()
        {
            if (MaxScroll == 0) return RectangleF.Empty;
            float h = Math.Max(30f, (float)Height * Height / contentH);
            float y = (Height - h) * scroll / MaxScroll;
            return new RectangleF(Width - 9, y + 2, 5, h - 4);
        }

        void DragTo(int y)
        {
            RectangleF thumb = Thumb();
            float track = Height - (thumb.Height + 4);
            if (track <= 0) return;
            scroll = (int)((y - dragOffset - 2) * MaxScroll / track);
            ClampScroll();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            scroll -= e.Delta * RowH * 3 / 120;
            ClampScroll();
            hover = -1;
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();

            RectangleF thumb = Thumb();
            if (!thumb.IsEmpty && e.X >= Width - Bar)
            {
                dragging = true;
                dragOffset = thumb.Contains(thumb.X, e.Y) ? e.Y - thumb.Y : thumb.Height / 2f;
                DragTo(e.Y);
                return;
            }

            int index = HitRow(e.Location);
            if (!CanToggle(index)) return;
            Row row = rows[index];
            if (row.Header)
            {
                List<Row> items = ItemsOf(row.Game);
                bool all = items.All(r => r.Target.Selected);
                foreach (Row r in items) r.Target.Selected = !all;
            }
            else
            {
                row.Target.Selected = !row.Target.Selected;
            }
            Invalidate();
            if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                DragTo(e.Y);
                return;
            }
            int index = HitRow(e.Location);
            if (!CanToggle(index)) index = -1;
            if (index == hover) return;
            hover = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging) return;
            dragging = false;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover == -1) return;
            hover = -1;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampScroll();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (rows.Count == 0)
            {
                Theme.DrawText(g, message, Theme.Small, Theme.Faint, new RectangleF(28, Height / 2f - 18, Width - 56, 40),
                    StringAlignment.Center, true, StringTrimming.EllipsisWord);
                return;
            }

            float textRight = Width - Bar - 6;
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                float top = row.Top - scroll;
                if (top + row.Height < 0 || top > Height) continue;
                float cy = top + row.Height / 2f;

                if (i == hover)
                    using (GraphicsPath p = Theme.Round(new RectangleF(4, top + 1, Width - Bar - 4, row.Height - 2), 6))
                    using (var b = new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
                        g.FillPath(b, p);

                if (row.Header)
                {
                    List<Row> items = ItemsOf(row.Game);
                    int on = items.Count(r => r.Target.Selected);
                    if (items.Count > 0)
                        Theme.DrawCheck(g, new RectangleF(12, cy - 8, 16, 16), on > 0, on > 0 && on < items.Count, locked);
                    float nameW = Theme.Measure(g, row.Game, Theme.ListHead).Width;
                    Theme.DrawText(g, row.Game, Theme.ListHead, Theme.Ink, new RectangleF(38, top, nameW + 6, row.Height));
                    Theme.DrawText(g, items.Count + (items.Count == 1 ? " item" : " itens"), Theme.Small, Theme.Faint,
                        new RectangleF(38 + nameW + 10, top, 120, row.Height));
                }
                else if (row.Target == null)
                {
                    Theme.DrawText(g, row.Note, Theme.Row, row.NoteColor, new RectangleF(38, top, textRight - 38, row.Height));
                }
                else
                {
                    CleanTarget t = row.Target;
                    var box = new RectangleF(38, cy - 8, 16, 16);
                    string text = t.Display;
                    Color color;
                    if (t.Done && t.Error == null)
                    {
                        Glyphs.Draw(g, Glyph.Check, box, Theme.Good, 1.9f);
                        color = Theme.Faint;
                    }
                    else if (t.Done)
                    {
                        Glyphs.Draw(g, Glyph.Alert, box, Theme.Bad, 1.5f);
                        color = Theme.Bad;
                        text += "  (" + t.Error + ")";
                    }
                    else
                    {
                        Theme.DrawCheck(g, box, t.Selected, false, locked);
                        color = t.Selected ? Color.FromArgb(214, 219, 228) : Theme.Faint;
                    }
                    Theme.DrawText(g, text, Theme.Row, color, new RectangleF(64, top, textRight - 64, row.Height),
                        StringAlignment.Near, false, StringTrimming.EllipsisPath);
                }
            }

            RectangleF thumb = Thumb();
            if (!thumb.IsEmpty)
                using (GraphicsPath p = Theme.Round(thumb, 2.5f))
                using (var b = new SolidBrush(Color.FromArgb(dragging ? 140 : 70, 255, 255, 255)))
                    g.FillPath(b, p);
        }
    }

    public class StatusChip : SceneControl
    {
        string text = "";
        Color color = Theme.Muted;

        public StatusChip()
        {
            AccessibleRole = AccessibleRole.StaticText;
        }

        public void Set(string value, Color dot)
        {
            text = value;
            color = dot;
            AccessibleName = value;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (text.Length == 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float textW = Theme.Measure(g, text, Theme.Small).Width;
            float w = 16 + 8 + 9 + textW + 16;
            var r = new RectangleF(Width - w - 0.5f, 0.5f, w, Height - 1f);
            using (GraphicsPath path = Theme.Round(r, r.Height / 2f))
            using (var b = new SolidBrush(Color.FromArgb(205, 14, 17, 23)))
            using (var p = new Pen(Color.FromArgb(40, 255, 255, 255)))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            float cy = Height / 2f;
            using (var glow = new SolidBrush(Color.FromArgb(60, color)))
                g.FillEllipse(glow, r.X + 13, cy - 7, 14, 14);
            using (var dot = new SolidBrush(color))
                g.FillEllipse(dot, r.X + 16, cy - 4, 8, 8);
            Theme.DrawText(g, text, Theme.Small, Theme.Ink, new RectangleF(r.X + 33, 0, textW + 8, Height));
        }
    }

    public class ResultView : SceneControl
    {
        string number = "";
        string headline = "";
        Color color = Theme.Accent;

        public ResultView()
        {
            AccessibleRole = AccessibleRole.StaticText;
        }

        public void SetResult(string bigNumber, Color numberColor, string title)
        {
            number = bigNumber ?? "";
            color = numberColor;
            headline = title;
            AccessibleName = (number + " " + headline).Trim();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 14))
            using (var b = new SolidBrush(Theme.Card))
            using (var p = new Pen(Theme.CardBorder))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            float x = 18;
            if (number.Length == 0)
            {
                var circle = new RectangleF(x, Height / 2f - 19, 38, 38);
                using (var b = new SolidBrush(Color.FromArgb(40, color)))
                    g.FillEllipse(b, circle);
                Glyphs.Draw(g, Glyph.Eye, RectangleF.Inflate(circle, -10, -10), color, 1.8f);
                x += 38 + 14;
            }
            else
            {
                float numberW = Theme.Measure(g, number, Theme.Big).Width;
                Theme.DrawText(g, number, Theme.Big, color, new RectangleF(x, 0, numberW + 6, Height));
                x += numberW + 16;
            }

            Theme.DrawText(g, headline, Theme.ResultHead, Theme.Ink, new RectangleF(x, 0, Width - x - 16, Height));
        }
    }
}
