using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MapDestinationEditorA
{
    public partial class Form1 : Form
    {
        // ===== EO text encoding (GBK) =====
        private static readonly Encoding Gbk =
            CodePagesEncodingProvider.Instance.GetEncoding(936) ?? Encoding.Latin1;

        // ===== Runtime data =====
        private byte[] _fileBytes = Array.Empty<byte>();
        private const int RecordSize = 532;
        private readonly List<Rec> _recs = new();
        private readonly Dictionary<int, string> _mapNames = new(); // MapID -> Name (from GameMap.ini)
        private bool _suspendEvents = false;

        // Client folder & last file
        private string _clientFolder = string.Empty;
        private string _lastLoadedFile = string.Empty;


        // Context menu + items
        private ContextMenuStrip cmsMap;
        private ToolStripMenuItem miAddMap, miDeleteMap;

        // Map tambahan yang dibuat user (tanpa rekod dalam .dat)
        private readonly HashSet<int> _extraMapIds = new();

        // Context menu untuk dataGridView2
        private ContextMenuStrip cmsMenu2;
        private ToolStripMenuItem miUp2, miDown2, miAddCoord2, miAddDir2, miDelete2;

        // === dv3 context menu ===
        private ContextMenuStrip cmsMenu3;
        private ToolStripMenuItem miUp3, miDown3, miAddCoord3, miDelete3;


        // --- selection context ---
        private enum Pane { None, Mid, Right }
        private Pane _currentPane = Pane.None;
        private Rec? _currentDv2Rec = null;
        private Rec? _currentDv3Rec = null;

        // --- store base title so we can append selections ---
        private readonly string _baseTitle = "MapDestination.dat Editor - By DuaSelipar";


        private sealed class Rec
        {
            public int Pos;
            public string Name = "";
            public string Info = "";
            public int MenuId;
            public int X;
            public int Y;
            public int ParentId;
            public int MapId;
            public override string ToString() => $"{Pos}. {Name}";
        }

        private sealed class MapTag
        {
            public int MapId;
            public MapTag(int id) { MapId = id; }
            public override string ToString() => $"Map {MapId}";
        }

        // EO 27-byte key
        private static readonly byte[] PKey = new byte[]
        {
            161,239,196,167,161,239,211,242,161,239,205,187,161,239,198,198,
            161,239,49,48,48,161,239,205,242,161,239
        };

        public Form1()
        {
            InitializeComponent();

            cmsMap = new ContextMenuStrip();
            miAddMap = new ToolStripMenuItem();
            miDeleteMap = new ToolStripMenuItem();

            cmsMenu2 = new ContextMenuStrip();
            miUp2 = new ToolStripMenuItem();
            miDown2 = new ToolStripMenuItem();
            miAddCoord2 = new ToolStripMenuItem();
            miAddDir2 = new ToolStripMenuItem();
            miDelete2 = new ToolStripMenuItem();

            cmsMenu3 = new ContextMenuStrip();
            miUp3 = new ToolStripMenuItem();
            miDown3 = new ToolStripMenuItem();
            miAddCoord3 = new ToolStripMenuItem();
            miDelete3 = new ToolStripMenuItem();

            SetUiLoaded(false);
            // Enable legacy code pages (GBK/936)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ApplyCjkFonts(); // <<— panggil ni

            // DGV setup
            dataGridView1.MultiSelect = false;
            dataGridView2.MultiSelect = false;
            dataGridView3.MultiSelect = false;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView3.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dataGridView1.RowHeadersVisible = false;
            dataGridView2.RowHeadersVisible = false;
            dataGridView3.RowHeadersVisible = false;


            // Optional (jika ada pada Designer): textBox1 & btnFind
            if (btnFind != null) btnFind.Click += btnFind_Click;

            btnSave.Click += btnSave_Click;

            InitializeMapContextMenu();
            dataGridView1.KeyDown += dataGridView1_KeyDown;
            dataGridView1.MouseDown += dataGridView1_MouseDown; // right-click select row

            InitializeMenu2ContextMenu();
            dataGridView2.KeyDown += dataGridView2_KeyDown;
            dataGridView2.MouseDown += dataGridView2_MouseDown; // right-click select row

            InitializeMenu3ContextMenu();
            dataGridView3.KeyDown += dataGridView3_KeyDown;
            dataGridView3.MouseDown += dataGridView3_MouseDown;   // right-click select row


            btnUpdate.Click += btnUpdate_Click;
            UpdateTitleBar(); // initial

            // re-hook untuk elak hilang wiring
            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
            dataGridView2.SelectionChanged -= dataGridView2_SelectionChanged;
            dataGridView3.SelectionChanged -= dataGridView3_SelectionChanged;

            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
            dataGridView2.SelectionChanged += dataGridView2_SelectionChanged;
            dataGridView3.SelectionChanged += dataGridView3_SelectionChanged;



        }

        // ====== Pick client folder (optional control) ======
        private void btnFind_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select your Eudemons Online client folder (root that contains ini/ or MapDestination.dat).",
                ShowNewFolderButton = false,
                UseDescriptionForTitle = true
            };

            if (textBox1 != null && Directory.Exists(textBox1.Text))
                fbd.SelectedPath = textBox1.Text;

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _clientFolder = fbd.SelectedPath;
                if (textBox1 != null) textBox1.Text = _clientFolder;
            }
        }

        // ================== UI: Load ==================
        private void btnLoad_Click(object sender, EventArgs e)
        {
            // Determine client folder
            _clientFolder = (textBox1 != null ? (textBox1.Text?.Trim() ?? "") : "").Trim();

            if (!Directory.Exists(_clientFolder))
            {
                // fallback: ask folder
                using var fbd = new FolderBrowserDialog
                {
                    Description = "Select your Eudemons Online client folder",
                    ShowNewFolderButton = false,
                    UseDescriptionForTitle = true
                };
                if (fbd.ShowDialog(this) != DialogResult.OK) return;
                _clientFolder = fbd.SelectedPath;
                if (textBox1 != null) textBox1.Text = _clientFolder;
            }

            // Find MapDestination.dat
            string datPath = ResolveMapDestinationPath(_clientFolder, out string explainDat);
            if (string.IsNullOrEmpty(datPath))
            {
                MessageBox.Show(this,
                    "MapDestination.dat was not found in this folder.\n" +
                    (string.IsNullOrEmpty(explainDat) ? "" : $"Note: {explainDat}"),
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load GameMap.ini names (best-effort)
            _mapNames.Clear();
            string gmPath = ResolveGameMapIniPath(_clientFolder, out string explainIni);
            if (!string.IsNullOrEmpty(gmPath))
            {
                try
                {
                    LoadGameMapNames(gmPath, _mapNames);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to parse GameMap.ini: {ex.Message}", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Not fatal if GameMap.ini is missing
                if (!string.IsNullOrEmpty(explainIni))
                    Console.WriteLine($"GameMap.ini note: {explainIni}");
            }

            _lastLoadedFile = datPath;
            LoadDat(datPath);

            // success if we have records
            if (_recs.Count > 0)
            {
                SetUiLoaded(true);
            }
            else
            {
                SetUiLoaded(false);
                MessageBox.Show(this, "The file was loaded but no valid records were found.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        // ================== DGV events ==================
        private void dataGridView1_SelectionChanged(object? sender, EventArgs e)
        {
            if (_suspendEvents) return;

            var tag = dataGridView1.CurrentRow?.Tag as MapTag;
            if (tag == null)
            {
                _currentPane = Pane.None;
                _currentDv2Rec = null;
                _currentDv3Rec = null;
                ClearGrid(dataGridView2);
                ClearGrid(dataGridView3);
                ClearDetails();
                UpdateEditingControls();
                UpdateTitleBar();
                return;
            }

            _suspendEvents = true;
            try
            {
                // reset context; PopulateMenuGrid will auto-select the first row and trigger dv2 SelectionChanged
                _currentPane = Pane.None;
                _currentDv2Rec = null;
                _currentDv3Rec = null;

                PopulateMenuGrid(tag.MapId);   // <- REQUIRED: populate dv2 for the selected map
            }
            finally
            {
                _suspendEvents = false;
            }

            UpdateEditingControls();
            UpdateTitleBar();
        }

        private void dataGridView2_SelectionChanged(object? sender, EventArgs e)
        {
            if (_suspendEvents) return;

            _currentPane = Pane.Mid;
            _currentDv3Rec = null;

            if (dataGridView2.CurrentRow?.Tag is Rec parent)
            {
                _currentDv2Rec = parent;
                ShowDetails(parent);
                PopulateChildGrid(parent.MenuId);
            }
            else
            {
                _currentDv2Rec = null;
                ClearDetails();
                ClearGrid(dataGridView3);
            }

            UpdateEditingControls();
            UpdateTitleBar();
        }

        private void dataGridView3_SelectionChanged(object? sender, EventArgs e)
        {
            if (_suspendEvents) return;

            _currentPane = Pane.Right;

            if (dataGridView3.CurrentRow?.Tag is Rec r)
            {
                _currentDv3Rec = r;
                ShowDetails(r);
            }
            else
            {
                _currentDv3Rec = null;
                // kekalkan details dari parent kalau ada — optional
            }

            UpdateEditingControls();
            UpdateTitleBar();
        }

        // ================== Path resolvers ==================
        private static string ResolveMapDestinationPath(string root, out string explain)
        {
            explain = "";
            try
            {
                var candidates = new[]
                {
            Path.Combine(root, "MapDestination.dat"),
            Path.Combine(root, "ini", "MapDestination.dat"),
            Path.Combine(root, "ini", "map", "MapDestination.dat"),
            Path.Combine(root, "data", "MapDestination.dat"),
            Path.Combine(root, "c3",   "MapDestination.dat"),
            Path.Combine(root, "ini",  "mapdestination.dat"),
        };
                foreach (var p in candidates)
                    if (File.Exists(p)) return p;

                // direct recursive (exact case first)
                var match = Directory.EnumerateFiles(root, "MapDestination.dat", SearchOption.AllDirectories)
                                     .FirstOrDefault();
                if (!string.IsNullOrEmpty(match)) return match;

                // last chance: case-insensitive
                match = Directory.EnumerateFiles(root, "*.dat", SearchOption.AllDirectories)
                                 .FirstOrDefault(f => string.Equals(Path.GetFileName(f),
                                                                    "MapDestination.dat",
                                                                    StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match)) return match;

                explain = "No match found in common locations or recursive search.";
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                explain = "Access denied while scanning some subfolders.";
                return string.Empty;
            }
            catch (Exception ex)
            {
                explain = $"Search error: {ex.Message}";
                return string.Empty;
            }
        }

        private static string ResolveGameMapIniPath(string root, out string explain)
        {
            explain = "";
            try
            {
                var candidates = new[]
                {
            Path.Combine(root, "ini", "GameMap.ini"),
            Path.Combine(root, "GameMap.ini"),
            Path.Combine(root, "INI", "GameMap.ini"),
            Path.Combine(root, "ini", "gamemap.ini"),
        };
                foreach (var p in candidates)
                    if (File.Exists(p)) return p;

                // exact-case recursive
                var match = Directory.EnumerateFiles(root, "GameMap.ini", SearchOption.AllDirectories)
                                     .FirstOrDefault();
                if (!string.IsNullOrEmpty(match)) return match;

                // last chance: case-insensitive
                match = Directory.EnumerateFiles(root, "*.ini", SearchOption.AllDirectories)
                                 .FirstOrDefault(f => string.Equals(Path.GetFileName(f),
                                                                    "gamemap.ini",
                                                                    StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match)) return match;

                explain = "GameMap.ini was not found; map names will be left empty.";
                return string.Empty;
            }
            catch (Exception ex)
            {
                explain = $"GameMap.ini search error: {ex.Message}";
                return string.Empty;
            }
        }


        // ================== GameMap.ini parser ==================
        // REPLACE your existing LoadGameMapNames with this:
        private static void LoadGameMapNames(string iniPath, IDictionary<int, string> mapNames)
        {
            mapNames.Clear();

            byte[] bytes = File.ReadAllBytes(iniPath);
            string text;

            // Detect BOMs first; else default to GBK/936
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                text = Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                text = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }
            else
            {
                // ← most EO clients: ANSI GBK (cp936)
                var gbk = CodePagesEncodingProvider.Instance.GetEncoding(936)!;
                text = gbk.GetString(bytes);
            }

            int? currentId = null;
            using var sr = new StringReader(text);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                var l = line.Trim();
                if (l.Length == 0 || l.StartsWith(";") || l.StartsWith("#")) continue;

                if (l.StartsWith("[") && l.EndsWith("]"))
                {
                    var sec = l.Substring(1, l.Length - 2).Trim();
                    if (sec.StartsWith("Map", StringComparison.OrdinalIgnoreCase))
                    {
                        var digits = new string(sec.SkipWhile(c => !char.IsDigit(c))
                                                   .TakeWhile(char.IsDigit).ToArray());
                        currentId = int.TryParse(digits, out var id) ? id : (int?)null;
                    }
                    else currentId = null;
                    continue;
                }

                if (currentId.HasValue)
                {
                    int eq = l.IndexOf('=');
                    if (eq > 0)
                    {
                        var key = l.Substring(0, eq).Trim();
                        var val = l.Substring(eq + 1).Trim();
                        if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                            if (!mapNames.ContainsKey(currentId.Value))
                                mapNames[currentId.Value] = val;
                    }
                }
            }
        }


        // ================== Core loader (.dat) ==================
        private void LoadDat(string filePath)
        {
            _suspendEvents = true;
            try
            {
                _recs.Clear();
                ClearDetails();
                ClearGrid(dataGridView1);
                ClearGrid(dataGridView2);
                ClearGrid(dataGridView3);

                _fileBytes = File.ReadAllBytes(filePath);
                if (_fileBytes.Length < 8) throw new InvalidDataException("File too small.");

                int declaredCount = ReadInt32LE(_fileBytes.AsSpan(0, 4));

                // assume 8 overhead bytes (4 header + 4 tail)
                int bodyLen = Math.Max(0, _fileBytes.Length - 8);
                var body = _fileBytes.AsSpan(4, bodyLen);

                int maxRecordsBySize = body.Length / RecordSize;
                int count = Math.Min(Math.Max(0, declaredCount), maxRecordsBySize);
                if (count <= 0) throw new InvalidDataException("No records.");

                for (int pos = 1; pos <= count; pos++)
                {
                    int start = (pos - 1) * RecordSize;
                    if (start + RecordSize > body.Length) break;

                    var recBytes = body.Slice(start, RecordSize).ToArray();
                    int idx = ((pos - 1) % 27) + 1; // 1..27

                    var r = new Rec
                    {
                        Pos = pos,
                        Name = ReadDecryptedString(recBytes.AsSpan(0, 255).ToArray(), idx),
                        Info = ReadDecryptedString(recBytes.AsSpan(255, 257).ToArray(), idx),
                        MenuId = ReadDecryptedInt32(recBytes.AsSpan(512, 4).ToArray(), idx),
                        X = ReadDecryptedInt32(recBytes.AsSpan(516, 4).ToArray(), idx),
                        Y = ReadDecryptedInt32(recBytes.AsSpan(520, 4).ToArray(), idx),
                        ParentId = ReadDecryptedInt32(recBytes.AsSpan(524, 4).ToArray(), idx),
                        MapId = ReadDecryptedInt32(recBytes.AsSpan(528, 4).ToArray(), idx),
                    };
                    _recs.Add(r);
                }

                PopulateMapGrid();
                this.Text = $"MapDestination.dat Editor - By DuaSelipar — {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _suspendEvents = false;
                if (dataGridView1.Rows.Count > 0)
                    dataGridView1.Rows[0].Selected = true;
            }
        }



        // dataGridView3: Children of selected menu (ParentId == MenuId), no Map filter
        private void PopulateChildGrid(int parentMenuId)
        {
            _suspendEvents = true;
            try
            {
                ClearGrid(dataGridView3);

                var children = _recs
                    .Where(r => r.ParentId == parentMenuId)
                    .OrderBy(r => r.Pos)
                    .ToList();

                foreach (var r in children)
                {
                    int idx = dataGridView3.Rows.Add();
                    var row = dataGridView3.Rows[idx];
                    row.Cells[0].Value = r.Name; // <-- name only
                    row.Tag = r;
                }

                if (dataGridView3.Rows.Count > 0)
                {
                    dataGridView3.Rows[0].Selected = true;
                    dataGridView3.CurrentCell = dataGridView3.Rows[0].Cells[0];
                }
            }
            finally
            {
                _suspendEvents = false;
            }

            // <<< PENTING: apply context manual supaya textbox terus hidup & berisi
            ApplyDv3SelectionContext();
        }


        // ================== Details panel ==================
        private void ShowDetails(Rec r)
        {
            txtName.Text = r.Name;
            txtInfo.Text = r.Info;
            txtMenu.Text = r.MenuId.ToString();
            txtParent.Text = r.ParentId.ToString();
            txtX.Text = r.X.ToString();
            txtY.Text = r.Y.ToString();
            txtMap.Text = r.MapId.ToString();
        }

        private void ClearDetails()
        {
            txtName.Clear();
            txtInfo.Clear();
            txtMenu.Clear();
            txtParent.Clear();
            txtX.Clear();
            txtY.Clear();
            txtMap.Clear();
        }

        // ================== Helpers ==================
        private static void ClearGrid(DataGridView dgv)
        {
            dgv.Rows.Clear();
            dgv.ClearSelection();
        }

        // ================== Decrypt helpers ==================
        private static byte[] DataSetDecrypt(byte[] pstr, int nIndex)
        {
            if (pstr.Length < 4) throw new ArgumentException("Need 4 bytes");
            byte[] ss = new byte[4];
            int keyIdx = ClampKeyIndex(nIndex);
            ss[0] = (byte)(pstr[0] ^ PKey[keyIdx]);
            if (nIndex >= 19 && nIndex <= 21)
            {
                ss[1] = pstr[1];
                ss[2] = pstr[2];
                ss[3] = pstr[3];
            }
            else
            {
                ss[1] = (byte)(pstr[1] ^ 0xFF);
                ss[2] = (byte)(pstr[2] ^ 0xFF);
                ss[3] = (byte)(pstr[3] ^ 0xFF);
            }
            return ss;
        }

        private static byte[] TextDecrypt(byte[] pstr, int nIndex)
        {
            byte[] data = new byte[pstr.Length];
            int keyIdx = ClampKeyIndex(nIndex);
            byte k = PKey[keyIdx];
            for (int i = 0; i < pstr.Length; i++)
                data[i] = (byte)(pstr[i] ^ k);
            return data;
        }

        private static string ReadDecryptedString(byte[] enc, int nIndex)
        {
            var raw = TextDecrypt(enc, nIndex);
            int zero = Array.IndexOf(raw, (byte)0x00);
            var span = zero >= 0 ? raw.AsSpan(0, zero) : raw.AsSpan();

            string s = Gbk.GetString(span);
            if (string.IsNullOrWhiteSpace(s))
                s = Encoding.Latin1.GetString(span);
            return s.Trim();
        }

        private static int ReadDecryptedInt32(ReadOnlySpan<byte> enc4, int nIndex)
        {
            byte[] dec = DataSetDecrypt(enc4.ToArray(), nIndex);
            return ReadInt32LE(dec);
        }

        private static int ReadInt32LE(ReadOnlySpan<byte> b)
        {
            if (b.Length < 4) throw new ArgumentException("Need 4 bytes");
            return b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24);
        }

        private static int ClampKeyIndex(int nIndex)
        {
            if (nIndex < 1) nIndex = 1;
            if (nIndex > 27) nIndex = 27;
            return nIndex - 1;
        }

        // ====== save ======
        private void btnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                // Tentukan path sasaran: default ke file yang kita load tadi
                string savePath = _lastLoadedFile;
                if (string.IsNullOrWhiteSpace(savePath) || !Directory.Exists(Path.GetDirectoryName(savePath)))
                {
                    using var sfd = new SaveFileDialog
                    {
                        Title = "Save MapDestination.dat",
                        Filter = "MapDestination.dat|MapDestination.dat|All files|*.*",
                        FileName = "MapDestination.dat"
                    };
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;
                    savePath = sfd.FileName;
                }

                // Susun rekod ikut Pos (1..N)
                var ordered = _recs.OrderBy(r => r.Pos).ToList();
                int count = ordered.Count;

                // Build body (rekod 532 bytes × N)
                using var ms = new MemoryStream(4 + count * RecordSize + 4);
                // Header: count (LE)
                ms.Write(WriteInt32LE(count), 0, 4);

                for (int i = 0; i < count; i++)
                {
                    var rec = ordered[i];
                    ms.Write(BuildRecordBytes(rec, i + 1), 0, RecordSize);
                }

                // Footer 4 bytes: kekalkan dari file asal jika ada; else tulis 0
                var tail = (_fileBytes != null && _fileBytes.Length >= 4)
                    ? _fileBytes.AsSpan(_fileBytes.Length - 4, 4).ToArray()
                    : new byte[4];
                ms.Write(tail, 0, 4);

                // Backup automatik
                if (File.Exists(savePath))
                {
                    var bak = savePath + ".bak";
                    File.Copy(savePath, bak, true);
                }

                File.WriteAllBytes(savePath, ms.ToArray());
                MessageBox.Show(this, "Saved OK: " + savePath, "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Save failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private byte[] BuildRecordBytes(Rec r, int pos1Based)
        {
            // index 1..27 ikut pos rekod (ikut source asal)
            int idx = ((pos1Based - 1) % 27) + 1;

            var buf = new byte[RecordSize];

            // Name (255) – GBK, pad 0
            var nameRaw = SafeBytesGBK(r.Name);
            var name255 = Pad(nameRaw, 255);
            var nameEnc = TextCrypt(name255, idx);
            Buffer.BlockCopy(nameEnc, 0, buf, 0, 255);

            // Info (257) – GBK, pad 0
            var infoRaw = SafeBytesGBK(r.Info);
            var info257 = Pad(infoRaw, 257);
            var infoEnc = TextCrypt(info257, idx);
            Buffer.BlockCopy(infoEnc, 0, buf, 255, 257);

            // Int fields (LE 4B) @512..531
            var bMenu = DataSetCrypt(WriteInt32LE(r.MenuId), idx);
            var bX = DataSetCrypt(WriteInt32LE(r.X), idx);
            var bY = DataSetCrypt(WriteInt32LE(r.Y), idx);
            var bParent = DataSetCrypt(WriteInt32LE(r.ParentId), idx);
            var bMap = DataSetCrypt(WriteInt32LE(r.MapId), idx);

            Buffer.BlockCopy(bMenu, 0, buf, 512, 4);
            Buffer.BlockCopy(bX, 0, buf, 516, 4);
            Buffer.BlockCopy(bY, 0, buf, 520, 4);
            Buffer.BlockCopy(bParent, 0, buf, 524, 4);
            Buffer.BlockCopy(bMap, 0, buf, 528, 4);

            return buf;
        }

        // XOR simetri: encrypt == decrypt
        private static byte[] TextCrypt(byte[] pstr, int nIndex)
        {
            byte[] data = new byte[pstr.Length];
            int keyIdx = ClampKeyIndex(nIndex);
            byte k = PKey[keyIdx];
            for (int i = 0; i < pstr.Length; i++)
                data[i] = (byte)(pstr[i] ^ k);
            return data;
        }

        private static byte[] DataSetCrypt(byte[] pstr4, int nIndex)
        {
            if (pstr4.Length < 4) throw new ArgumentException("Need 4 bytes");
            byte[] ss = new byte[4];
            int keyIdx = ClampKeyIndex(nIndex);
            ss[0] = (byte)(pstr4[0] ^ PKey[keyIdx]);
            if (nIndex >= 19 && nIndex <= 21)
            {
                ss[1] = pstr4[1];
                ss[2] = pstr4[2];
                ss[3] = pstr4[3];
            }
            else
            {
                ss[1] = (byte)(pstr4[1] ^ 0xFF);
                ss[2] = (byte)(pstr4[2] ^ 0xFF);
                ss[3] = (byte)(pstr4[3] ^ 0xFF);
            }
            return ss;
        }

        private static byte[] WriteInt32LE(int value)
        {
            return new byte[]
            {
        (byte)(value & 0xFF),
        (byte)((value >> 8) & 0xFF),
        (byte)((value >> 16) & 0xFF),
        (byte)((value >> 24) & 0xFF)
            };
        }

        private static byte[] SafeBytesGBK(string? s)
        {
            s ??= string.Empty;
            return Gbk.GetBytes(s);
        }

        private static byte[] Pad(byte[] src, int len)
        {
            var dst = new byte[len];
            Buffer.BlockCopy(src, 0, dst, 0, Math.Min(len, src.Length));
            return dst;
        }

        // ============


        private void InitializeMapContextMenu()
        {
            cmsMap = new ContextMenuStrip();

            miAddMap = new ToolStripMenuItem("Add New Map (&A)")
            {
                ShortcutKeys = Keys.Alt | Keys.A,
                ShowShortcutKeys = true
            };
            miAddMap.Click += (_, __) => AddNewMap_UI();

            miDeleteMap = new ToolStripMenuItem("Delete Map")
            {
                ShortcutKeys = Keys.Delete,
                ShowShortcutKeys = true
            };
            miDeleteMap.Click += (_, __) => DeleteSelectedMap_UI();

            cmsMap.Items.AddRange(new ToolStripItem[] { miAddMap, miDeleteMap });
            dataGridView1.ContextMenuStrip = cmsMap;
        }

        private void dataGridView1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedMap_UI();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.A && e.Alt)
            {
                AddNewMap_UI();
                e.Handled = true;
            }
        }

        // Pastikan right-click highlight row yang diklik
        private void dataGridView1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[hit.RowIndex].Cells[0];
                }
            }
        }


        private void AddNewMap_UI()
        {
            // Ensure _mapNames is loaded; if empty try to load from client folder
            if (!TryEnsureMapNamesLoaded())
            {
                MessageBox.Show(this,
                    "GameMap.ini was not found or failed to load.\nPlease select a valid client folder and try again.",
                    "GameMap.ini Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open picker (search + double-click)
            if (!PickMapIdFromIni(out int newId)) return;

            // Prevent duplicates (merge map IDs from .dat + extras)
            if (GetAllMapIds().Contains(newId))
            {
                MessageBox.Show(this, $"Map ID {newId} already exists.", "Duplicate",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Keep new map in manual set (without creating .dat record yet)
            _extraMapIds.Add(newId);

            // Refresh dv1 (name auto from _mapNames) and select the new map
            PopulateMapGrid();
            SelectMapInGrid(newId);

            // Clear dv2 & dv3
            ClearGrid(dataGridView2);
            ClearGrid(dataGridView3);
            ClearDetails();
        }

        private bool PickMapIdFromIni(out int selectedId)
        {
            selectedId = 0;

            var existing = new HashSet<int>(GetAllMapIds());
            var allItems = _mapNames
                .Select(kv => new MapRow { Id = kv.Key, Name = kv.Value })
                .Where(r => !existing.Contains(r.Id))
                .OrderBy(r => r.Id)
                .ToList();

            if (allItems.Count == 0)
            {
                MessageBox.Show(this, "No new maps to add (all are already present).", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // --- layout constants ---
            int gridW = 300;                 // << your requested width
            int dlgW = 12 + gridW + 12;     // left + grid + right margin
            int dlgH = 520;

            using var dlg = new Form
            {
                Text = "Add New Map from GameMap.ini",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new System.Drawing.Size(dlgW, dlgH)
            };

            var lbl = new Label { Left = 12, Top = 12, AutoSize = true, Text = "Search (ID / Name):" };
            var tb = new TextBox { Left = 12, Top = 32, Width = gridW };

            var grid = new DataGridView
            {
                Left = 12,
                Top = 64,
                Width = gridW,
                Height = 400,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,

                // fixed layout: no resize, vertical scroll only
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Vertical,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };

            // column widths (no H-scroll)
            int idWidth = 80;
            int nameWidth = Math.Max(
                140,
                gridW - idWidth - SystemInformation.VerticalScrollBarWidth - 6 // small margin
            );

            var colId = new DataGridViewTextBoxColumn
            {
                HeaderText = "ID",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = idWidth,
                Resizable = DataGridViewTriState.False
            };
            var colNm = new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = nameWidth,
                Resizable = DataGridViewTriState.False
            };
            grid.Columns.AddRange(colId, colNm);

            // center buttons under the 300px grid
            int btnW = 75, gap = 8;
            int buttonsLeft = 12 + (gridW - (btnW * 2 + gap)) / 2;

            var ok = new Button { Text = "Add", Left = buttonsLeft, Top = 474, Width = btnW, DialogResult = DialogResult.OK };
            var cc = new Button { Text = "Cancel", Left = buttonsLeft + btnW + gap, Top = 474, Width = btnW, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[] { lbl, tb, grid, ok, cc });
            dlg.AcceptButton = ok; dlg.CancelButton = cc;

            void FillGrid(IEnumerable<MapRow> src)
            {
                grid.Rows.Clear();
                foreach (var r in src)
                {
                    int i = grid.Rows.Add();
                    grid.Rows[i].Cells[0].Value = r.Id;
                    grid.Rows[i].Cells[1].Value = r.Name;
                    grid.Rows[i].Tag = r;
                }
                if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;
            }

            FillGrid(allItems);

            tb.TextChanged += (s, e) =>
            {
                var q = tb.Text.Trim();
                if (string.IsNullOrEmpty(q)) { FillGrid(allItems); }
                else
                {
                    var lower = q.ToLowerInvariant();
                    bool isNum = int.TryParse(q, out _);
                    var filtered = allItems.Where(r =>
                            (isNum && r.Id.ToString().Contains(q)) ||
                            (!isNum && (r.Name ?? "").ToLowerInvariant().Contains(lower)) ||
                            (!isNum && r.Id.ToString().Contains(q))
                        )
                        .OrderBy(r => r.Id);
                    FillGrid(filtered);
                }
            };

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var row = grid.CurrentRow;
                if (row?.Tag is MapRow mr)
                {
                    selectedId = mr.Id;
                    return true;
                }
                if (row != null && int.TryParse(Convert.ToString(row.Cells[0].Value), out int id))
                {
                    selectedId = id;
                    return true;
                }
                MessageBox.Show(this, "Please select a map.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return false;
        }



        private sealed class MapRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }


        private bool TryEnsureMapNamesLoaded()
        {
            if (_mapNames != null && _mapNames.Count > 0) return true;

            // cuba resolve dari _clientFolder; kalau kosong, cuba textBox1
            var root = !string.IsNullOrWhiteSpace(_clientFolder) ? _clientFolder
                       : (textBox1 != null ? (textBox1.Text?.Trim() ?? "") : "");
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return false;

            string ini = ResolveGameMapIniPath(root, out _);
            if (string.IsNullOrEmpty(ini) || !File.Exists(ini)) return false;

            _mapNames.Clear();
            try
            {
                LoadGameMapNames(ini, _mapNames);
                return _mapNames.Count > 0;
            }
            catch { return false; }
        }


        private void DeleteSelectedMap_UI()
        {
            var row = dataGridView1.CurrentRow;
            if (row == null || row.Tag is not MapTag mt) return;

            int mapId = mt.MapId;
            var ans = MessageBox.Show(this,
                $"Delete Map {mapId}? All related entries will be removed.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;

            // Buang semua rekod berkait dalam .dat
            _recs.RemoveAll(r => r.MapId == mapId);

            // Buang juga map manual kalau ada
            _extraMapIds.Remove(mapId);

            // Refresh UI
            PopulateMapGrid();
            ClearGrid(dataGridView2);
            ClearGrid(dataGridView3);
            ClearDetails();
        }

        private bool PromptForMapId(out int mapId)
        {
            mapId = 0;
            using (var dlg = new Form
            {
                Text = "Add New Map",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new System.Drawing.Size(320, 120)
            })
            {
                var lbl = new Label { Text = "Enter Map ID (numbers only):", AutoSize = true, Left = 12, Top = 12 };
                var tb = new TextBox { Left = 12, Top = 36, Width = 290 };
                var ok = new Button { Text = "OK", Left = 146, Top = 72, Width = 75, DialogResult = DialogResult.OK };
                var cc = new Button { Text = "Cancel", Left = 227, Top = 72, Width = 75, DialogResult = DialogResult.Cancel };

                // Hanya digit dibenarkan
                tb.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
                };

                dlg.Controls.AddRange(new Control[] { lbl, tb, ok, cc });
                dlg.AcceptButton = ok; dlg.CancelButton = cc;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (!int.TryParse(tb.Text, out mapId) || mapId <= 0)
                    {
                        MessageBox.Show(this, "Map ID must be a positive number.", "Invalid",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        private void SelectMapInGrid(int mapId)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (r.Tag is MapTag mt && mt.MapId == mapId)
                {
                    r.Selected = true;
                    dataGridView1.CurrentCell = r.Cells[0];
                    try { dataGridView1.FirstDisplayedScrollingRowIndex = r.Index; } catch { }
                    break;
                }
            }
        }

        private IEnumerable<int> GetAllMapIds()
        {
            return _recs.Select(r => r.MapId).Concat(_extraMapIds).Distinct();
        }

        private void PopulateMapGrid()
        {
            ClearGrid(dataGridView1);

            var maps = GetAllMapIds().OrderBy(id => id).ToList();
            foreach (var id in maps)
            {
                int idx = dataGridView1.Rows.Add();
                var row = dataGridView1.Rows[idx];
                row.Cells[0].Value = id; // Column1 = "ID"
                if (_mapNames.TryGetValue(id, out var nm))
                    row.Cells[1].Value = nm; // Column2 = "Name" dari GameMap.ini
                row.Tag = new MapTag(id);
            }
        }


        // Prefer modern CJK UI font; fallback ke SimSun/PMingLiU/MS Gothic
        private void ApplyCjkFonts()
        {
            var name = PickInstalledFont(new[]
            {
        "Microsoft YaHei UI",   // 简体中文 (Windows 10+)
        "Microsoft JhengHei UI",// 繁体中文
        "SimSun",               // 宋体 (GBK)
        "PMingLiU",             // 明體 (Big5)
        "MS Gothic"             // 日本語 fallback
    });

            var uiFont = new System.Drawing.Font(name, 9f);

            // DataGridView cells + headers
            void ApplyDgv(DataGridView dgv)
            {
                dgv.DefaultCellStyle.Font = uiFont;
                dgv.RowsDefaultCellStyle.Font = uiFont;
                dgv.AlternatingRowsDefaultCellStyle.Font = uiFont;
                dgv.ColumnHeadersDefaultCellStyle.Font = uiFont;
            }
            ApplyDgv(dataGridView1);
            ApplyDgv(dataGridView2);
            ApplyDgv(dataGridView3);

            // Text fields
            txtName.Font = uiFont;
            txtInfo.Font = uiFont;
            txtMenu.Font = uiFont;
            txtParent.Font = uiFont;
            txtX.Font = uiFont;
            txtY.Font = uiFont;
            txtMap.Font = uiFont;

            // Kalau ada path textbox client
            if (textBox1 != null) textBox1.Font = uiFont;

            // Bagi kemas rendering
            this.Font = uiFont; // optional: set form default
        }

        private static string PickInstalledFont(IEnumerable<string> candidates)
        {
            using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            var installed = new HashSet<string>(
                System.Drawing.FontFamily.Families.Select(f => f.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (var n in candidates)
                if (installed.Contains(n)) return n;

            return SystemFonts.MessageBoxFont?.Name ?? "Default Font"; // fallback
        }

        private void InitializeMenu2ContextMenu()
        {
            cmsMenu2 = new ContextMenuStrip();

            miUp2 = new ToolStripMenuItem("Move Up (Q)"); miUp2.Click += (_, __) => MoveSelectedMenu2(-1);
            miDown2 = new ToolStripMenuItem("Move Down (W)"); miDown2.Click += (_, __) => MoveSelectedMenu2(1);
            miAddCoord2 = new ToolStripMenuItem("Add Coordinate (A)"); miAddCoord2.Click += (_, __) => AddMenuItem_UI(true);
            miAddDir2 = new ToolStripMenuItem("Add Directory (T)"); miAddDir2.Click += (_, __) => AddMenuItem_UI(false);
            miDelete2 = new ToolStripMenuItem("Delete (Del)"); miDelete2.Click += (_, __) => DeleteMenuItem_UI();

            cmsMenu2.Items.AddRange(new ToolStripItem[] { miUp2, miDown2, new ToolStripSeparator(),
                                                  miAddCoord2, miAddDir2, new ToolStripSeparator(), miDelete2 });

            dataGridView2.ContextMenuStrip = cmsMenu2;
        }

        private void dataGridView2_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) { DeleteMenuItem_UI(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Q) { MoveSelectedMenu2(-1); e.Handled = true; return; }
            if (e.KeyCode == Keys.W) { MoveSelectedMenu2(1); e.Handled = true; return; }
            if (e.KeyCode == Keys.A) { AddMenuItem_UI(true); e.Handled = true; return; }
            if (e.KeyCode == Keys.T) { AddMenuItem_UI(false); e.Handled = true; return; }
        }

        // pastikan row yang di-right-click dipilih dulu
        private void dataGridView2_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView2.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridView2.ClearSelection();
                    dataGridView2.Rows[hit.RowIndex].Selected = true;
                    dataGridView2.CurrentCell = dataGridView2.Rows[hit.RowIndex].Cells[0];
                }
            }
        }

        private void AddMenuItem_UI(bool withXY)
        {
            if (!TryGetCurrentMapId(out int mapId))
            {
                MessageBox.Show(this, "Select a map first.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!PromptMenuFields(withXY, out string name, out string info, out int x, out int y))
                return;

            int nextMenuId = GetNextMenuId();   // nombor latest
            var rec = new Rec
            {
                Pos = _recs.Count + 1,
                Name = name,
                Info = info,
                MenuId = nextMenuId,
                X = withXY ? x : -1,
                Y = withXY ? y : -1,
                ParentId = -1,
                MapId = mapId
            };

            _recs.Add(rec);

            // Refresh dv2 dan select item baru
            PopulateMenuGrid(mapId);
            SelectMenuInGrid(rec);

            // TERPENTING: terus apply konteks supaya textbox tak kosong/disable
            ApplyDv2SelectionContext();
        }


        private void DeleteMenuItem_UI()
        {
            if (dataGridView2.CurrentRow?.Tag is not Rec parent) return;

            var ans = MessageBox.Show(this,
                $"Delete \"{parent.Name}\" and all its children?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;

            // delete children (dv3) berdasarkan ParentId == MenuId parent
            _recs.RemoveAll(r => r.ParentId == parent.MenuId);
            // delete parent
            _recs.Remove(parent);

            // (optional) resequence Pos agar kemas
            ResequencePositions();

            // refresh UI
            if (TryGetCurrentMapId(out int mapId))
                PopulateMenuGrid(mapId);
            ClearGrid(dataGridView3);
            ClearDetails();
        }

        private void MoveSelectedMenu2(int delta)
        {
            if (dataGridView2.CurrentRow?.Tag is not Rec target) return;
            if (!TryGetCurrentMapId(out int mapId)) return;

            // Termasuk MenuId == 0 (mentor)
            var list = _recs
                .Where(r => r.MapId == mapId && r.ParentId == -1 && r.MenuId >= 0)
                .OrderBy(r => r.Pos)
                .ToList();

            int idx = list.FindIndex(r => ReferenceEquals(r, target));
            if (idx < 0) return;

            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= list.Count) return;

            // swap Pos
            int posA = list[idx].Pos;
            list[idx].Pos = list[newIdx].Pos;
            list[newIdx].Pos = posA;

            PopulateMenuGrid(mapId);
            SelectMenuInGrid(target);
        }


        private bool PromptMenuFields(bool withXY, out string name, out string info, out int x, out int y)
        {
            name = info = ""; x = y = -1;

            using var dlg = new Form
            {
                Text = withXY ? "Add Coordinate" : "Add Directory",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new System.Drawing.Size(360, withXY ? 220 : 160)
            };

            var lblName = new Label { Left = 12, Top = 14, AutoSize = true, Text = "Name:" };
            var tbName = new TextBox { Left = 80, Top = 10, Width = 260 };
            var lblInfo = new Label { Left = 12, Top = 46, AutoSize = true, Text = "Info:" };
            var tbInfo = new TextBox { Left = 80, Top = 42, Width = 260 };

            TextBox tbX = null, tbY = null;
            if (withXY)
            {
                var lblX = new Label { Left = 12, Top = 78, AutoSize = true, Text = "X:" };
                tbX = new TextBox { Left = 80, Top = 74, Width = 100 };
                var lblY = new Label { Left = 190, Top = 78, AutoSize = true, Text = "Y:" };
                tbY = new TextBox { Left = 220, Top = 74, Width = 120 };

                // benarkan minus & digit sahaja
                KeyPressEventHandler onlyInt = (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '-')
                        e.Handled = true;
                    if (e.KeyChar == '-' && (s as TextBox)!.SelectionStart != 0)
                        e.Handled = true;
                };
                tbX.KeyPress += onlyInt;
                tbY.KeyPress += onlyInt;

                dlg.Controls.AddRange(new Control[] { lblX, tbX, lblY, tbY });
            }

            var ok = new Button { Text = "OK", Left = 196, Width = 70, Top = withXY ? 130 : 100, DialogResult = DialogResult.OK };
            var cc = new Button { Text = "Cancel", Left = 270, Width = 70, Top = withXY ? 130 : 100, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[] { lblName, tbName, lblInfo, tbInfo, ok, cc });
            dlg.AcceptButton = ok; dlg.CancelButton = cc;

            if (dlg.ShowDialog(this) != DialogResult.OK) return false;

            name = tbName.Text.Trim();
            info = tbInfo.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "Name is required.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (withXY)
            {
                if (!int.TryParse(tbX!.Text.Trim(), out x) ||
                    !int.TryParse(tbY!.Text.Trim(), out y))
                {
                    MessageBox.Show(this, "X and Y must be integers.", "Invalid",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        private bool TryGetCurrentMapId(out int mapId)
        {
            mapId = 0;
            var row = dataGridView1.CurrentRow;
            if (row?.Tag is MapTag mt) { mapId = mt.MapId; return true; }
            return false;
        }

        private int GetNextMenuId()
        {
            int max = _recs.Where(r => r.MenuId > 0).Select(r => r.MenuId).DefaultIfEmpty(0).Max();
            return max + 1;
        }

        private void SelectMenuInGrid(Rec target)
        {
            foreach (DataGridViewRow r in dataGridView2.Rows)
                if (ReferenceEquals(r.Tag, target))
                {
                    r.Selected = true;
                    dataGridView2.CurrentCell = r.Cells[0];
                    try { dataGridView2.FirstDisplayedScrollingRowIndex = r.Index; } catch { }
                    break;
                }
        }

        private void ResequencePositions()
        {
            for (int i = 0; i < _recs.Count; i++) _recs[i].Pos = i + 1;
        }

        private void PopulateMenuGrid(int mapId)
        {
            _suspendEvents = true;
            try
            {
                ClearDetails();
                ClearGrid(dataGridView2);
                ClearGrid(dataGridView3);

                var menus = _recs
                    .Where(r => r.MapId == mapId && r.ParentId == -1 && r.MenuId >= 0)
                    .OrderBy(r => r.Pos)
                    .ToList();

                foreach (var r in menus)
                {
                    int idx = dataGridView2.Rows.Add();
                    var row = dataGridView2.Rows[idx];
                    row.Cells[0].Value = r.Name; // <-- name only
                    row.Tag = r;
                }

                if (dataGridView2.Rows.Count > 0)
                {
                    dataGridView2.Rows[0].Selected = true;
                    dataGridView2.CurrentCell = dataGridView2.Rows[0].Cells[0];
                }
            }
            finally
            {
                _suspendEvents = false;
            }

            // Penting: kalau event SelectionChanged tak sempat trigger masa _suspendEvents=true,
            // kita apply konteks secara manual supaya textbox terus hidup & berisi.
            ApplyDv2SelectionContext();
        }

        private void ApplyDv2SelectionContext()
        {
            if (dataGridView2.CurrentRow?.Tag is Rec parent)
            {
                _currentPane = Pane.Mid;
                _currentDv2Rec = parent;
                _currentDv3Rec = null;

                ShowDetails(parent);
                PopulateChildGrid(parent.MenuId); // isi dv3 untuk parent semasa
                UpdateEditingControls();
                UpdateTitleBar();
            }
        }



        private void InitializeMenu3ContextMenu()
        {
            cmsMenu3 = new ContextMenuStrip();

            miUp3 = new ToolStripMenuItem("Move Up (Q)");
            miUp3.Click += (_, __) => MoveSelectedChild3(-1);

            miDown3 = new ToolStripMenuItem("Move Down (W)");
            miDown3.Click += (_, __) => MoveSelectedChild3(1);

            miAddCoord3 = new ToolStripMenuItem("Add Coordinate (A)");
            miAddCoord3.Click += (_, __) => AddChildCoordinate_UI();

            miDelete3 = new ToolStripMenuItem("Delete (Del)");
            miDelete3.Click += (_, __) => DeleteChild3();

            cmsMenu3.Items.AddRange(new ToolStripItem[]
            { miUp3, miDown3, new ToolStripSeparator(), miAddCoord3, new ToolStripSeparator(), miDelete3 });

            // enable/disable Add berdasarkan dv2 parent (X/Y)
            cmsMenu3.Opening += (_, __) =>
            {
                miAddCoord3.Enabled = (GetCurrentParentRec(out var p) && p.X == -1 && p.Y == -1);
            };

            dataGridView3.ContextMenuStrip = cmsMenu3;
        }

        private void dataGridView3_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) { DeleteChild3(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Q) { MoveSelectedChild3(-1); e.Handled = true; return; }
            if (e.KeyCode == Keys.W) { MoveSelectedChild3(1); e.Handled = true; return; }
            if (e.KeyCode == Keys.A)
            {
                if (GetCurrentParentRec(out var p) && p.X == -1 && p.Y == -1) AddChildCoordinate_UI();
                e.Handled = true;
                return;
            }
        }

        // pastikan baris yang right-click dipilih
        private void dataGridView3_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView3.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridView3.ClearSelection();
                    dataGridView3.Rows[hit.RowIndex].Selected = true;
                    dataGridView3.CurrentCell = dataGridView3.Rows[hit.RowIndex].Cells[0];
                }
            }
        }

        private void AddChildCoordinate_UI()
        {
            // hanya benarkan kalau parent di dv2 ialah "directory" (X=Y=-1)
            if (!GetCurrentParentRec(out var parent) || !(parent.X == -1 && parent.Y == -1))
            {
                MessageBox.Show(this, "Select a directory item on the middle list (X = Y = -1).",
                    "Cannot add here", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!PromptMenuFields(true, out string name, out string info, out int x, out int y))
                return;

            var child = new Rec
            {
                Pos = _recs.Count + 1,
                Name = name,
                Info = info,
                MenuId = -1,              // child coordinate tak guna menu
                X = x,
                Y = y,
                ParentId = parent.MenuId,   // link ke parent directory (dv2)
                MapId = -1               // <-- ikut spec baru: sentiasa -1
            };

            _recs.Add(child);
            PopulateChildGrid(parent.MenuId);  // refresh dv3
            SelectChildInGrid(child);

            ApplyDv3SelectionContext();
        }


        private void MoveSelectedChild3(int delta)
        {
            if (dataGridView3.CurrentRow?.Tag is not Rec child) return;
            if (!GetCurrentParentRec(out var parent)) return;

            var list = _recs
                .Where(r => r.ParentId == parent.MenuId)
                .OrderBy(r => r.Pos)
                .ToList();

            int idx = list.FindIndex(r => ReferenceEquals(r, child));
            if (idx < 0) return;

            int newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= list.Count) return;

            // swap Pos
            int tmp = list[idx].Pos;
            list[idx].Pos = list[newIdx].Pos;
            list[newIdx].Pos = tmp;

            PopulateChildGrid(parent.MenuId);
            SelectChildInGrid(child);
        }

        private void DeleteChild3()
        {
            if (dataGridView3.CurrentRow?.Tag is not Rec child) return;
            if (!GetCurrentParentRec(out var parent)) return;

            var ans = MessageBox.Show(this, $"Delete \"{child.Name}\"?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;

            _recs.Remove(child);
            ResequencePositions();                 // optional: kemaskan Pos global

            PopulateChildGrid(parent.MenuId);

            ApplyDv3SelectionContext(); 

        }

        private bool GetCurrentParentRec(out Rec parent)
        {
            parent = null;
            if (dataGridView2.CurrentRow?.Tag is Rec p) { parent = p; return true; }
            return false;
        }

        private void SelectChildInGrid(Rec target)
        {
            foreach (DataGridViewRow r in dataGridView3.Rows)
                if (ReferenceEquals(r.Tag, target))
                {
                    r.Selected = true;
                    dataGridView3.CurrentCell = r.Cells[0];
                    try { dataGridView3.FirstDisplayedScrollingRowIndex = r.Index; } catch { }
                    break;
                }
        }

        private void UpdateEditingControls()
        {
            // default: semua disabled
            txtName.Enabled = txtInfo.Enabled = false;
            txtX.Enabled = txtY.Enabled = false;
            txtMenu.Enabled = txtParent.Enabled = txtMap.Enabled = false;

            if (_currentPane == Pane.Mid && _currentDv2Rec != null)
            {
                // dv2: Name/Info selalu boleh edit
                txtName.Enabled = txtInfo.Enabled = true;

                bool isDirectory = (_currentDv2Rec.X == -1 && _currentDv2Rec.Y == -1);

                // directory: X,Y disabled; coordinate: X,Y enabled
                txtX.Enabled = txtY.Enabled = !isDirectory;

                // sentiasa disable ID fields pada dv2
                txtMenu.Enabled = false;
                txtParent.Enabled = false;
                txtMap.Enabled = false;

                if (isDirectory)
                {
                    // optional: paparkan -1 untuk konsisten
                    txtX.Text = "-1";
                    txtY.Text = "-1";
                }
            }
            else if (_currentPane == Pane.Right && _currentDv3Rec != null)
            {
                // dv3: Name/Info/X/Y boleh edit
                txtName.Enabled = txtInfo.Enabled = txtX.Enabled = txtY.Enabled = true;

                // sentiasa disable ID fields pada dv3
                txtMenu.Enabled = false;
                txtParent.Enabled = false;
                txtMap.Enabled = false;
            }
            // kalau None atau tiada seleksi, kekalkan semua disabled
        }


        private void btnUpdate_Click(object? sender, EventArgs e)
        {
            _suspendEvents = true;
            try
            {
                string? msgBody = null;

                if (_currentPane == Pane.Right && _currentDv3Rec != null)
                {
                    var r = _currentDv3Rec;

                    r.Name = txtName.Text.Trim();
                    r.Info = txtInfo.Text.Trim();

                    if (!int.TryParse(txtX.Text.Trim(), out int nx) ||
                        !int.TryParse(txtY.Text.Trim(), out int ny))
                    {
                        MessageBox.Show(this, "X and Y must be integers.", "Invalid",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    r.X = nx; r.Y = ny;

                    RefreshDv3Row(r);

                    // msg: parent (dv2) + child (dv3)
                    var pName = _currentDv2Rec?.Name ?? "(no parent)";
                    msgBody = $"Updated\n{pName}\n{r.Name}";
                }
                else if (_currentPane == Pane.Mid && _currentDv2Rec != null)
                {
                    var r = _currentDv2Rec;

                    r.Name = txtName.Text.Trim();
                    r.Info = txtInfo.Text.Trim();

                    bool isDirectory = (r.X == -1 && r.Y == -1);
                    if (!isDirectory)
                    {
                        if (!int.TryParse(txtX.Text.Trim(), out int nx) ||
                            !int.TryParse(txtY.Text.Trim(), out int ny))
                        {
                            MessageBox.Show(this, "X and Y must be integers.", "Invalid",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        r.X = nx; r.Y = ny;
                    }
                    else
                    {
                        r.X = -1; r.Y = -1;
                    }

                    RefreshDv2Row(r);

                    // msg: dv2 only
                    msgBody = $"Updated\n{r.Name}";
                }
                else
                {
                    MessageBox.Show(this, "Select an item in the middle (dv2) or right (dv3) list first.",
                        "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                UpdateEditingControls();
                UpdateTitleBar();

                if (!string.IsNullOrEmpty(msgBody))
                    MessageBox.Show(this, msgBody, "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Update failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _suspendEvents = false;
            }
        }


        private void RefreshDv2Row(Rec r)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (ReferenceEquals(row.Tag, r))
                {
                    row.Cells[0].Value = r.Name; // <-- name only
                    row.Selected = true;
                    dataGridView2.CurrentCell = row.Cells[0];
                    try { dataGridView2.FirstDisplayedScrollingRowIndex = row.Index; } catch { }
                    break;
                }
            }
        }

        private void RefreshDv3Row(Rec r)
        {
            foreach (DataGridViewRow row in dataGridView3.Rows)
            {
                if (ReferenceEquals(row.Tag, r))
                {
                    row.Cells[0].Value = r.Name; // <-- name only
                    row.Selected = true;
                    dataGridView3.CurrentCell = row.Cells[0];
                    try { dataGridView3.FirstDisplayedScrollingRowIndex = row.Index; } catch { }
                    break;
                }
            }
        }



        private static bool TryParseInt(string s, out int v)
        {
            return int.TryParse(s?.Trim(), out v);
        }

        private void UpdateTitleBar()
        {
            // base
            string title = _baseTitle;

            // map id (utamakan dv1 selection; fallback dv2/dv3 MapId)
            int mapId = 0;
            if (dataGridView1.CurrentRow?.Tag is MapTag mt) mapId = mt.MapId;
            else if (_currentDv2Rec != null) mapId = _currentDv2Rec.MapId;
            else if (_currentDv3Rec != null) mapId = _currentDv3Rec.MapId;

            if (mapId != 0) title += $"  |  [{mapId}]";

            // dv2 name
            if (_currentDv2Rec != null)
                title += $" -- {_currentDv2Rec.Name}";

            // dv3 name
            if (_currentDv3Rec != null)
                title += $" -- {_currentDv3Rec.Name}";

            this.Text = title;
        }

        private void ApplyDv3SelectionContext()
        {
            // kalau ada row terpilih di dv3 → jadikan “current dv3”
            if (dataGridView3.CurrentRow?.Tag is Rec r)
            {
                _currentPane = Pane.Right;
                _currentDv3Rec = r;
                ShowDetails(r);
            }
            else
            {
                _currentDv3Rec = null;
            }

            // kekalkan parent (dv2) sedia ada
            // _currentDv2Rec biar apa adanya

            UpdateEditingControls();
            UpdateTitleBar();
        }

        // panggil ni untuk toggle UI sebelum/selepas load
        private void SetUiLoaded(bool loaded)
        {
            // sentiasa boleh guna
            if (textBox1 != null) textBox1.Enabled = true;
            if (btnFind != null) btnFind.Enabled = true;
            if (btnLoad != null) btnLoad.Enabled = true;

            // senarai & butang lain bergantung pada status load
            dataGridView1.Enabled = loaded;
            dataGridView2.Enabled = loaded;
            dataGridView3.Enabled = loaded;

            if (btnUpdate != null) btnUpdate.Enabled = loaded;
            if (btnSave != null) btnSave.Enabled = loaded;

            // textbox detail: default off — nanti UpdateEditingControls akan hidupkan ikut seleksi
            txtName.Enabled = false;
            txtInfo.Enabled = false;
            txtX.Enabled = false;
            txtY.Enabled = false;
            txtMenu.Enabled = false;
            txtParent.Enabled = false;
            txtMap.Enabled = false;

            if (!loaded)
            {
                // bila belum load: kosongkan semua view & details
                ClearGrid(dataGridView1);
                ClearGrid(dataGridView2);
                ClearGrid(dataGridView3);
                ClearDetails();

                // reset context & title
                _currentPane = Pane.None;
                _currentDv2Rec = null;
                _currentDv3Rec = null;
                UpdateTitleBar();
            }
            else
            {
                // selepas load: biar handler seleksi hidupkan field yang patut
                UpdateEditingControls();
            }
        }


    }
}
