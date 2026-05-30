using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
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
        readonly TextBox fiveMBox = new TextBox();
        readonly TextBox gtaBox = new TextBox();
        readonly TextBox logBox = new TextBox();
        readonly Button detectButton = new Button();
        readonly Button cleanButton = new Button();
        readonly Button simulateButton = new Button();
        readonly Button browseFiveMButton = new Button();
        readonly CheckBox cleanFiveM = new CheckBox();
        readonly CheckBox cleanGta = new CheckBox();
        readonly Label statusLabel = new Label();

        readonly Color bg = Color.FromArgb(5, 5, 6);
        readonly Color panel = Color.FromArgb(18, 18, 20);
        readonly Color panel2 = Color.FromArgb(26, 26, 29);
        readonly Color panel3 = Color.FromArgb(32, 32, 36);
        readonly Color white = Color.FromArgb(246, 246, 246);
        readonly Color text = Color.FromArgb(232, 232, 232);
        readonly Color muted = Color.FromArgb(150, 150, 154);
        readonly Color line = Color.FromArgb(58, 58, 64);
        readonly Color good = Color.FromArgb(235, 235, 235);
        readonly Color warn = Color.FromArgb(210, 210, 210);

        public MainForm()
        {
            Text = "GX Limpeza - FiveM / GTA";
            Icon icon = LoadEmbeddedIcon("GXCleanerIcon");
            if (icon != null) Icon = icon;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 680);
            Size = new Size(1040, 720);
            BackColor = bg;
            Font = new Font("Segoe UI", 10F);

            BuildUi();
            DetectPaths();
        }

        void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28),
                ColumnCount = 1,
                RowCount = 3,
                BackColor = bg
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var paths = Card();
            paths.Padding = new Padding(22);
            var pathGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, BackColor = panel };
            pathGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            pathGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pathGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
            pathGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            pathGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            pathGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            pathGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            paths.Controls.Add(pathGrid);

            var pathsTitle = Label("Diretorios detectados", white, 15F, true);
            pathGrid.Controls.Add(pathsTitle, 0, 0);
            pathGrid.SetColumnSpan(pathsTitle, 2);
            statusLabel.Text = "Pronto";
            statusLabel.ForeColor = Color.Black;
            statusLabel.BackColor = good;
            statusLabel.AutoSize = false;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("Segoe UI Semibold", 9F);
            statusLabel.Margin = new Padding(6, 9, 0, 9);
            pathGrid.Controls.Add(statusLabel, 2, 0);
            pathGrid.Controls.Add(Label("FiveM", muted), 0, 1);
            pathGrid.Controls.Add(Label("GTA V", muted), 0, 2);
            StyleTextBox(fiveMBox);
            StyleTextBox(gtaBox);
            pathGrid.Controls.Add(fiveMBox, 1, 1);
            pathGrid.Controls.Add(gtaBox, 1, 2);
            StyleButton(browseFiveMButton, "Escolher", false);
            browseFiveMButton.Click += delegate { BrowseFiveM(); };
            pathGrid.Controls.Add(browseFiveMButton, 2, 1);
            StyleButton(detectButton, "Detectar", false);
            detectButton.Click += delegate { DetectPaths(); };
            pathGrid.Controls.Add(detectButton, 2, 2);
            root.Controls.Add(paths, 0, 0);

            var actionCard = Card();
            actionCard.Padding = new Padding(18);
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = panel };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158));
            cleanFiveM.Text = "Limpeza completa FiveM";
            cleanGta.Text = "Limpeza completa GTA V";
            StyleToggle(cleanFiveM);
            StyleToggle(cleanGta);
            actions.Controls.Add(cleanFiveM, 0, 0);
            actions.Controls.Add(cleanGta, 1, 0);
            StyleButton(simulateButton, "Simular", false);
            StyleButton(cleanButton, "Limpar", true);
            simulateButton.Click += delegate { RunCleanup(true); };
            cleanButton.Click += delegate { RunCleanup(false); };
            actions.Controls.Add(simulateButton, 3, 0);
            actions.Controls.Add(cleanButton, 4, 0);
            actionCard.Controls.Add(actions);
            root.Controls.Add(actionCard, 0, 1);

            var logCard = Card();
            logCard.Padding = new Padding(16);
            var logGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = panel };
            logGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            logGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logGrid.Controls.Add(Label("Registro da limpeza", white, 12F, true), 0, 0);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = Color.FromArgb(8, 8, 9);
            logBox.ForeColor = Color.FromArgb(220, 220, 220);
            logBox.Font = new Font("Consolas", 10F);
            logGrid.Controls.Add(logBox, 0, 1);
            logCard.Controls.Add(logGrid);
            root.Controls.Add(logCard, 0, 2);
        }

        Panel Card()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = panel,
                Margin = new Padding(0, 0, 0, 18),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        Label Label(string value, Color color, float size = 10F, bool bold = false)
        {
            return new Label
            {
                Text = value,
                ForeColor = color,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        void StyleTextBox(TextBox box)
        {
            box.Dock = DockStyle.Fill;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Color.FromArgb(9, 9, 10);
            box.ForeColor = text;
            box.Font = new Font("Segoe UI Semibold", 9.5F);
            box.Margin = new Padding(0, 11, 14, 11);
        }

        void StyleButton(Button button, string caption, bool primary)
        {
            button.Text = caption;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = line;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(225, 225, 225) : Color.FromArgb(42, 42, 47);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(200, 200, 200) : Color.FromArgb(52, 52, 58);
            button.BackColor = primary ? white : panel2;
            button.ForeColor = primary ? Color.Black : white;
            button.Font = new Font("Segoe UI Semibold", 10F);
            button.Margin = new Padding(8, 8, 0, 8);
            button.MinimumSize = new Size(112, 28);
        }

        void StyleToggle(CheckBox cb)
        {
            cb.Appearance = Appearance.Button;
            cb.Checked = true;
            cb.Dock = DockStyle.Fill;
            cb.FlatStyle = FlatStyle.Flat;
            cb.FlatAppearance.BorderSize = 1;
            cb.FlatAppearance.BorderColor = line;
            cb.TextAlign = ContentAlignment.MiddleCenter;
            cb.Font = new Font("Segoe UI Semibold", 10F);
            cb.Margin = new Padding(0, 16, 14, 16);
            cb.UseVisualStyleBackColor = false;
            cb.CheckedChanged += delegate { UpdateToggle(cb); };
            UpdateToggle(cb);
        }

        void UpdateToggle(CheckBox cb)
        {
            cb.BackColor = cb.Checked ? white : panel2;
            cb.ForeColor = cb.Checked ? Color.Black : white;
            cb.Text = (cb.Checked ? "ON  " : "OFF ") + cb.Text.Replace("ON  ", "").Replace("OFF ", "");
        }

        void Log(string message)
        {
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
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

        void SetStatus(string value, Color color)
        {
            statusLabel.Text = value;
            statusLabel.ForeColor = Color.Black;
            statusLabel.BackColor = color;
        }

        void DetectPaths()
        {
            logBox.Clear();
            SetStatus("Detectando...", white);
            Log("Procurando FiveM...");
            string fiveM = FindFiveMApp(fiveMBox.Text);
            if (String.IsNullOrWhiteSpace(fiveM))
            {
                fiveMBox.Text = "";
                gtaBox.Text = "";
                Log("FiveM nao encontrado automaticamente. Use o botao Escolher.");
                SetStatus("FiveM nao encontrado", warn);
                return;
            }

            fiveMBox.Text = fiveM;
            Log("FiveM: " + fiveM);

            string gta = GetGtaPathFromCitizenFxIni(Path.Combine(fiveM, "CitizenFX.ini"));
            gtaBox.Text = gta ?? "";
            if (!String.IsNullOrWhiteSpace(gta)) Log("GTA V: " + gta);
            else Log("GTA V nao encontrado no CitizenFX.ini. A limpeza do GTA pode ficar desligada.");

            SetStatus("Detectado", good);
        }

        void BrowseFiveM()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta FiveM.app";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (IsFiveMApp(dialog.SelectedPath))
                    {
                        fiveMBox.Text = dialog.SelectedPath;
                        gtaBox.Text = GetGtaPathFromCitizenFxIni(Path.Combine(dialog.SelectedPath, "CitizenFX.ini")) ?? "";
                        Log("FiveM definido manualmente: " + dialog.SelectedPath);
                        SetStatus("Pronto", good);
                    }
                    else
                    {
                        MessageBox.Show(this, "Essa pasta nao parece ser um FiveM.app valido com CitizenFX.ini.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        void RunCleanup(bool whatIf)
        {
            string fiveM = fiveMBox.Text.Trim();
            string gta = gtaBox.Text.Trim();
            if (!IsFiveMApp(fiveM))
            {
                MessageBox.Show(this, "FiveM.app invalido. Clique em Detectar ou Escolher.", "GX Limpeza", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!whatIf)
            {
                var result = MessageBox.Show(this, "Pode apagar os itens selecionados?", "Confirmar limpeza", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }

            SetStatus(whatIf ? "Simulando..." : "Limpando...", whatIf ? warn : white);
            Log(whatIf ? "Simulacao iniciada. Nada sera apagado." : "Limpeza iniciada.");

            if (cleanFiveM.Checked) CleanFiveMMods(fiveM, whatIf);
            if (cleanGta.Checked && Directory.Exists(gta)) CleanGtaMods(gta, whatIf);

            SetStatus(whatIf ? "Simulacao concluida" : "Limpeza concluida", good);
            Log(whatIf ? "Simulacao concluida." : "Limpeza concluida.");
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

        void ClearFolderContents(string folder, bool whatIf)
        {
            if (!Directory.Exists(folder))
            {
                Log("Pasta nao encontrada: " + folder);
                return;
            }

            var entries = Directory.GetFileSystemEntries(folder);
            if (entries.Length == 0)
            {
                Log("Ja estava vazia: " + folder);
                return;
            }

            foreach (string entry in entries)
            {
                if (whatIf)
                {
                    Log("WHATIF removeria: " + entry);
                    continue;
                }

                try
                {
                    if (Directory.Exists(entry)) Directory.Delete(entry, true);
                    else File.Delete(entry);
                    Log("removido: " + entry);
                }
                catch (Exception ex)
                {
                    Log("erro ao remover " + entry + ": " + ex.Message);
                }
            }
        }

        void CleanFiveMMods(string fiveM, bool whatIf)
        {
            Log("Limpando mods do FiveM...");

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
                    ClearFolderContents(target, whatIf);
                }
                else
                {
                    RemovePath(target, whatIf);
                }
            }
        }

        void CleanGtaMods(string gta, bool whatIf)
        {
            Log("Limpando mods do GTA V...");

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
                "_weatherlist.ini",
                "enbhelper.dll",
                "enblocal.ini",
                "enbadaptation.fx.ini",
                "enbbloom.fx.ini",
                "enbeffect.fx.ini",
                "enbeffectpostpass.fx.ini",
                "enbeffectprepass.fx.ini",
                "enblens.fx.ini",
                "enblightsprite.fx.ini",
                "intlightsprite.fx.ini",
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

            bool found = false;
            foreach (string name in names)
            {
                string target = Path.Combine(gta, name);
                if (!File.Exists(target) && !Directory.Exists(target)) continue;
                found = true;
                RemovePath(target, whatIf);
            }

            if (!found) Log("Nenhum mod comum encontrado no GTA V.");
        }

        void RemovePath(string target, bool whatIf)
        {
            if (!File.Exists(target) && !Directory.Exists(target)) return;

            if (whatIf)
            {
                Log("WHATIF removeria: " + target);
                return;
            }

            try
            {
                if (Directory.Exists(target)) Directory.Delete(target, true);
                else File.Delete(target);
                Log("removido: " + target);
            }
            catch (Exception ex)
            {
                Log("erro ao remover " + target + ": " + ex.Message);
            }
        }
    }

    public class RoundedPanel : Panel
    {
        public int Radius { get; set; }
        public Color BorderColor { get; set; }

        public RoundedPanel()
        {
            Radius = 14;
            BorderColor = Color.FromArgb(58, 58, 64);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundRectCompat(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using (GraphicsPath path = RoundRectCompat(new Rectangle(0, 0, Width, Height), Radius))
                Region = new Region(path);
            Invalidate();
        }

        public static GraphicsPath RoundRectCompat(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedButton : Button
    {
        public int Radius { get; set; }
        public Color BorderColor { get; set; }
        public Color HoverColor { get; set; }
        public Color DownColor { get; set; }
        bool hovering;
        bool pressed;

        public RoundedButton()
        {
            Radius = 12;
            BorderColor = Color.FromArgb(58, 58, 64);
            HoverColor = Color.FromArgb(42, 42, 47);
            DownColor = Color.FromArgb(52, 52, 58);
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { pressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { pressed = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = pressed ? DownColor : (hovering ? HoverColor : BackColor);
            using (GraphicsPath path = RoundedPanel.RoundRectCompat(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(BorderColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
                e.Graphics.DrawString(Text, Font, textBrush, ClientRectangle, sf);
            }
        }
    }
}
