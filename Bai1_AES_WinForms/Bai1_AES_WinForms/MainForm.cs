using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Bai1_AES_WinForms;

public sealed class MainForm : Form
{
    private readonly List<Button> _allButtons = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly Label _keyStatus = new()
    {
        AutoSize = true,
        MaximumSize = new Size(480, 0),
        Text = "Chưa có khóa. Hãy sinh khóa mới hoặc import tệp khóa.",
        Padding = new Padding(8, 9, 8, 0)
    };
    private readonly Label _operationStatus = new()
    {
        AutoSize = true,
        Text = "Sẵn sàng.",
        ForeColor = Color.DarkSlateGray,
        Padding = new Padding(4, 7, 4, 0)
    };

    private readonly TextBox _plainTextBox = new();
    private readonly TextBox _cipherTextBox = new();
    private readonly TextBox _decryptedTextBox = new();
    private readonly TextBox _fileLog = new();
    private byte[]? _key;
    private bool _isBusy;

    public MainForm()
    {
        Text = "Bài 1 - Mã hóa AES-256 | An toàn và Bảo mật thông tin";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1120, 820);
        MinimumSize = new Size(900, 680);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 250, 252);

        BuildMainLayout();
        UpdateKeyStatus();
    }

    private void BuildMainLayout()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
            BackColor = BackColor
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(main);

        var header = new Panel { Dock = DockStyle.Fill };
        var title = new Label
        {
            Text = "BÀI 1 — MÃ HÓA AES-256-GCM",
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(2, 0),
            ForeColor = Color.FromArgb(24, 49, 83)
        };
        var subtitle = new Label
        {
            Text = "Sinh / lưu / import khóa • Mã hóa văn bản và tệp lớn • Đo thời gian • So sánh byte",
            AutoSize = true,
            Location = new Point(4, 38),
            ForeColor = Color.FromArgb(80, 92, 105)
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        main.Controls.Add(header, 0, 0);

        main.Controls.Add(BuildKeyPanel(), 0, 1);
        BuildTabs();
        main.Controls.Add(_tabs, 0, 2);
        _operationStatus.Dock = DockStyle.Fill;
        main.Controls.Add(_operationStatus, 0, 3);
    }

    private Control BuildKeyPanel()
    {
        var group = new GroupBox
        {
            Text = "Quản lý khóa AES-256 (32 byte)",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(2, 6, 2, 2)
        };

        flow.Controls.Add(CreateButton("Sinh khóa mới", GenerateKey_Click, 125));
        flow.Controls.Add(CreateButton("Lưu khóa...", SaveKey_Click, 110));
        flow.Controls.Add(CreateButton("Import khóa...", ImportKey_Click, 120));
        flow.Controls.Add(_keyStatus);
        group.Controls.Add(flow);
        return group;
    }

    private void BuildTabs()
    {
        var textTab = new TabPage("Mã hóa văn bản") { BackColor = Color.White, Padding = new Padding(8) };
        var fileTab = new TabPage("Mã hóa tệp & đo hiệu năng") { BackColor = Color.White, Padding = new Padding(8) };
        BuildTextTab(textTab);
        BuildFileTab(fileTab);
        _tabs.TabPages.Add(textTab);
        _tabs.TabPages.Add(fileTab);
    }

    private void BuildTextTab(TabPage tab)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        layout.Controls.Add(CreateSectionLabel("1. Văn bản gốc (UTF-8)"), 0, 0);
        ConfigureMultilineTextBox(_plainTextBox);
        _plainTextBox.PlaceholderText = "Nhập văn bản muốn mã hóa...";
        layout.Controls.Add(_plainTextBox, 0, 1);

        var encryptFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        encryptFlow.Controls.Add(CreateButton("Mã hóa văn bản → Base64", EncryptText_Click, 205));
        encryptFlow.Controls.Add(CreateButton("Xóa nội dung", ClearText_Click, 120));
        layout.Controls.Add(encryptFlow, 0, 2);

        layout.Controls.Add(CreateSectionLabel("2. Bản mã Base64 (có thể dán/sửa trước khi giải mã)"), 0, 3);
        ConfigureMultilineTextBox(_cipherTextBox);
        _cipherTextBox.WordWrap = true;
        _cipherTextBox.PlaceholderText = "Bản mã được tạo ra sẽ xuất hiện ở đây...";
        layout.Controls.Add(_cipherTextBox, 0, 4);

        var decryptFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 0) };
        decryptFlow.Controls.Add(CreateButton("Giải mã Base64 bên trên", DecryptText_Click, 205));
        layout.Controls.Add(decryptFlow, 0, 5);

        layout.Controls.Add(CreateSectionLabel("3. Văn bản sau giải mã"), 0, 6);
        ConfigureMultilineTextBox(_decryptedTextBox);
        _decryptedTextBox.ReadOnly = true;
        _decryptedTextBox.BackColor = Color.FromArgb(246, 248, 250);
        layout.Controls.Add(_decryptedTextBox, 0, 7);

        tab.Controls.Add(layout);
    }

    private void BuildFileTab(TabPage tab)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chọn tệp bất kỳ để mã hóa hoặc giải mã. Tệp được xử lý theo từng khối 1 MiB, nên không cần nạp toàn bộ tệp hàng GB vào RAM. Thời gian và tốc độ được ghi vào nhật ký bên dưới.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(55, 65, 81)
        };
        layout.Controls.Add(note, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        actions.Controls.Add(CreateButton("Chọn tệp và mã hóa...", EncryptFile_Click, 220));
        actions.Controls.Add(CreateButton("Chọn tệp mã hóa và giải mã...", DecryptFile_Click, 250));
        actions.Controls.Add(CreateButton("So sánh 2 tệp theo byte...", CompareFiles_Click, 225));
        actions.Controls.Add(CreateButton("Xóa nhật ký", ClearLog_Click, 115));
        layout.Controls.Add(actions, 0, 1);

        ConfigureMultilineTextBox(_fileLog);
        _fileLog.ReadOnly = true;
        _fileLog.WordWrap = false;
        _fileLog.ScrollBars = ScrollBars.Both;
        _fileLog.Font = new Font("Consolas", 9.5F);
        _fileLog.BackColor = Color.FromArgb(249, 250, 251);
        _fileLog.Text = "NHẬT KÝ THỰC NGHIỆM AES\r\n" + new string('=', 76) + "\r\n";
        layout.Controls.Add(_fileLog, 0, 2);

        tab.Controls.Add(layout);
    }

    private Button CreateButton(string text, EventHandler onClick, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            Margin = new Padding(3, 2, 7, 2),
            FlatStyle = FlatStyle.System,
            AutoSize = false
        };
        button.Click += onClick;
        _allButtons.Add(button);
        return button;
    }

    private static Label CreateSectionLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
    };

    private static void ConfigureMultilineTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.AcceptsReturn = true;
        textBox.AcceptsTab = true;
        textBox.Font = new Font("Consolas", 10F);
    }

    private void GenerateKey_Click(object? sender, EventArgs e)
    {
        if (_key is not null)
        {
            DialogResult answer = MessageBox.Show(
                "Sinh khóa mới sẽ thay khóa hiện tại trong bộ nhớ. Những tệp đã mã hóa bằng khóa cũ chỉ giải mã được khi bạn còn giữ khóa cũ. Bạn có muốn tiếp tục?",
                "Xác nhận sinh khóa mới", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
                return;
        }

        ReplaceKey(AesGcmService.GenerateKey());
        _operationStatus.Text = "Đã sinh khóa AES-256 mới. Hãy lưu khóa nếu cần dùng lại sau khi đóng ứng dụng.";
        MessageBox.Show(
            "Đã sinh khóa AES-256 (32 byte) trong bộ nhớ. Hãy bấm 'Lưu khóa...' để có thể sử dụng lại sau này.",
            "Sinh khóa thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveKey_Click(object? sender, EventArgs e)
    {
        if (!EnsureKey())
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu khóa AES-256",
            Filter = "Tệp khóa JSON (*.key.json)|*.key.json|Tất cả tệp (*.*)|*.*",
            FileName = "aes256.key.json",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            KeyFileService.Save(dialog.FileName, _key!);
            _operationStatus.Text = "Đã lưu tệp khóa.";
            MessageBox.Show(
                "Đã lưu khóa. Lưu ý: tệp JSON chứa khóa ở dạng Base64 nhưng không được mã hóa hay bảo vệ bằng mật khẩu. Hãy giữ tệp này an toàn.",
                "Lưu khóa thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError("Không thể lưu khóa", ex);
        }
    }

    private void ImportKey_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import khóa AES-256",
            Filter = "Tệp khóa JSON (*.key.json;*.json)|*.key.json;*.json|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            byte[] importedKey = KeyFileService.Load(dialog.FileName);
            ReplaceKey(importedKey);
            _operationStatus.Text = "Đã import khóa AES-256.";
            MessageBox.Show("Đã import khóa AES-256 thành công.", "Import khóa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError("Không thể import khóa", ex);
        }
    }

    private void EncryptText_Click(object? sender, EventArgs e)
    {
        if (!EnsureKey())
            return;

        try
        {
            _cipherTextBox.Text = AesGcmService.EncryptText(_plainTextBox.Text, _key!);
            _decryptedTextBox.Clear();
            _operationStatus.Text = "Đã mã hóa văn bản. Base64 là cách biểu diễn kết quả nhị phân, không phải thuật toán mã hóa.";
        }
        catch (Exception ex)
        {
            ShowError("Mã hóa văn bản thất bại", ex);
        }
    }

    private void DecryptText_Click(object? sender, EventArgs e)
    {
        if (!EnsureKey())
            return;

        try
        {
            _decryptedTextBox.Text = AesGcmService.DecryptText(_cipherTextBox.Text, _key!);
            _operationStatus.Text = "Giải mã thành công; xác thực GCM đã đạt.";
        }
        catch (Exception ex)
        {
            _decryptedTextBox.Clear();
            ShowError("Giải mã văn bản thất bại", ex);
        }
    }

    private void ClearText_Click(object? sender, EventArgs e)
    {
        _plainTextBox.Clear();
        _cipherTextBox.Clear();
        _decryptedTextBox.Clear();
        _operationStatus.Text = "Đã xóa nội dung trên tab văn bản.";
    }

    private async void EncryptFile_Click(object? sender, EventArgs e)
    {
        if (!EnsureKey())
            return;

        string inputPath;
        using (var open = new OpenFileDialog
        {
            Title = "Chọn tệp nguồn cần mã hóa",
            Filter = "Tất cả tệp (*.*)|*.*",
            CheckFileExists = true
        })
        {
            if (open.ShowDialog(this) != DialogResult.OK)
                return;
            inputPath = open.FileName;
        }

        string defaultOutputName = Path.GetFileName(inputPath) + ".aesgcm";
        string outputPath;
        using (var save = new SaveFileDialog
        {
            Title = "Lưu tệp đã mã hóa",
            Filter = "Tệp AES-GCM (*.aesgcm)|*.aesgcm|Tất cả tệp (*.*)|*.*",
            FileName = defaultOutputName,
            DefaultExt = "aesgcm",
            AddExtension = true,
            OverwritePrompt = true
        })
        {
            if (save.ShowDialog(this) != DialogResult.OK)
                return;
            outputPath = save.FileName;
        }

        byte[] keySnapshot = _key!.ToArray();
        var stopwatch = Stopwatch.StartNew();
        string finalStatus = "Mã hóa tệp hoàn tất.";
        SetBusy(true, "Đang mã hóa tệp theo từng khối... Vui lòng không đóng ứng dụng.");
        try
        {
            FileCryptResult result = await Task.Run(() => AesGcmService.EncryptFile(inputPath, outputPath, keySnapshot));
            stopwatch.Stop();
            double mibPerSecond = ToMiB(result.InputBytes) / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.000001);
            AppendLog(
                "MÃ HÓA TỆP THÀNH CÔNG",
                $"Tệp nguồn       : {inputPath}",
                $"Tệp mã hóa      : {outputPath}",
                $"Dung lượng gốc  : {result.InputBytes:N0} byte ({FormatBytes(result.InputBytes)})",
                $"Dung lượng mã hóa: {result.OutputBytes:N0} byte ({FormatBytes(result.OutputBytes)})",
                $"Thời gian       : {FormatDuration(stopwatch.Elapsed)}",
                $"Tốc độ          : {mibPerSecond:N2} MiB/s (tính theo dung lượng tệp nguồn)");
            finalStatus = "Mã hóa xong: " + Path.GetFileName(outputPath);
        }
        catch (Exception ex)
        {
            finalStatus = "Mã hóa thất bại.";
            ShowError("Mã hóa tệp thất bại", ex);
            AppendLog("MÃ HÓA TỆP THẤT BẠI", $"Nguồn: {inputPath}", $"Lỗi: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            CryptographicOperations.ZeroMemory(keySnapshot);
            SetBusy(false, finalStatus);
        }
    }

    private async void DecryptFile_Click(object? sender, EventArgs e)
    {
        if (!EnsureKey())
            return;

        string inputPath;
        using (var open = new OpenFileDialog
        {
            Title = "Chọn tệp AES-GCM cần giải mã",
            Filter = "Tệp AES-GCM (*.aesgcm)|*.aesgcm|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true
        })
        {
            if (open.ShowDialog(this) != DialogResult.OK)
                return;
            inputPath = open.FileName;
        }

        string suggestedName = Path.GetFileNameWithoutExtension(inputPath);
        if (string.IsNullOrWhiteSpace(suggestedName))
            suggestedName = "decrypted.bin";
        else
            suggestedName += ".decrypted";

        string outputPath;
        using (var save = new SaveFileDialog
        {
            Title = "Lưu tệp sau giải mã",
            Filter = "Tất cả tệp (*.*)|*.*",
            FileName = suggestedName,
            OverwritePrompt = true
        })
        {
            if (save.ShowDialog(this) != DialogResult.OK)
                return;
            outputPath = save.FileName;
        }

        byte[] keySnapshot = _key!.ToArray();
        var stopwatch = Stopwatch.StartNew();
        string finalStatus = "Giải mã tệp hoàn tất.";
        SetBusy(true, "Đang xác thực và giải mã tệp theo từng khối...");
        try
        {
            FileCryptResult result = await Task.Run(() => AesGcmService.DecryptFile(inputPath, outputPath, keySnapshot));
            stopwatch.Stop();
            double mibPerSecond = ToMiB(result.OutputBytes) / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.000001);
            AppendLog(
                "GIẢI MÃ TỆP THÀNH CÔNG",
                $"Tệp mã hóa      : {inputPath}",
                $"Tệp khôi phục   : {outputPath}",
                $"Dung lượng mã hóa: {result.InputBytes:N0} byte ({FormatBytes(result.InputBytes)})",
                $"Dung lượng khôi phục: {result.OutputBytes:N0} byte ({FormatBytes(result.OutputBytes)})",
                $"Thời gian       : {FormatDuration(stopwatch.Elapsed)}",
                $"Tốc độ          : {mibPerSecond:N2} MiB/s (tính theo dung lượng tệp khôi phục)",
                "Xác thực AES-GCM: THÀNH CÔNG");
            finalStatus = "Giải mã xong: " + Path.GetFileName(outputPath);
        }
        catch (Exception ex)
        {
            finalStatus = "Giải mã thất bại: tệp có thể sai khóa, bị sửa hoặc bị cắt cụt.";
            ShowError("Giải mã tệp thất bại", ex);
            AppendLog("GIẢI MÃ TỆP THẤT BẠI", $"Tệp mã hóa: {inputPath}", $"Lỗi: {ex.Message}",
                "Nếu xác thực thất bại, ứng dụng sẽ không giữ lại tệp giải mã chưa hoàn chỉnh.");
        }
        finally
        {
            stopwatch.Stop();
            CryptographicOperations.ZeroMemory(keySnapshot);
            SetBusy(false, finalStatus);
        }
    }

    private async void CompareFiles_Click(object? sender, EventArgs e)
    {
        string firstPath;
        using (var firstDialog = new OpenFileDialog
        {
            Title = "Chọn tệp thứ nhất (thường là tệp nguồn)",
            Filter = "Tất cả tệp (*.*)|*.*",
            CheckFileExists = true
        })
        {
            if (firstDialog.ShowDialog(this) != DialogResult.OK)
                return;
            firstPath = firstDialog.FileName;
        }

        string secondPath;
        using (var secondDialog = new OpenFileDialog
        {
            Title = "Chọn tệp thứ hai (thường là tệp đã giải mã)",
            Filter = "Tất cả tệp (*.*)|*.*",
            CheckFileExists = true
        })
        {
            if (secondDialog.ShowDialog(this) != DialogResult.OK)
                return;
            secondPath = secondDialog.FileName;
        }

        var stopwatch = Stopwatch.StartNew();
        string finalStatus = "So sánh tệp hoàn tất.";
        SetBusy(true, "Đang so sánh nội dung từng byte...");
        try
        {
            FileComparisonResult result = await Task.Run(() => AesGcmService.CompareFiles(firstPath, secondPath));
            stopwatch.Stop();
            if (result.AreEqual)
            {
                AppendLog(
                    "SO SÁNH BYTE: GIỐNG HỆT",
                    $"Tệp thứ nhất : {firstPath}",
                    $"Tệp thứ hai  : {secondPath}",
                    $"Kích thước   : {result.FirstLength:N0} byte ở cả hai tệp",
                    $"Byte đã so sánh: {result.ComparedBytes:N0}",
                    $"Thời gian so sánh: {FormatDuration(stopwatch.Elapsed)}",
                    "KẾT LUẬN: Tất cả byte của hai tệp giống nhau hoàn toàn.");
                finalStatus = "Hai tệp giống nhau hoàn toàn theo từng byte.";
                MessageBox.Show("Hai tệp giống nhau hoàn toàn theo từng byte.", "Kết quả so sánh",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AppendLog(
                    "SO SÁNH BYTE: KHÁC NHAU",
                    $"Tệp thứ nhất : {firstPath} ({result.FirstLength:N0} byte)",
                    $"Tệp thứ hai  : {secondPath} ({result.SecondLength:N0} byte)",
                    $"Offset byte khác đầu tiên (tính từ 0): {result.FirstDifferenceOffset}",
                    $"Số byte đã xét (có thể gồm byte khác đầu tiên): {result.ComparedBytes}",
                    $"Thời gian so sánh: {FormatDuration(stopwatch.Elapsed)}",
                    "KẾT LUẬN: Hai tệp không giống nhau hoàn toàn.");
                finalStatus = "Hai tệp khác nhau.";
                MessageBox.Show($"Hai tệp KHÁC NHAU. Vị trí khác đầu tiên (offset bắt đầu từ 0): {result.FirstDifferenceOffset}",
                    "Kết quả so sánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            finalStatus = "Không thể so sánh hai tệp.";
            ShowError("So sánh tệp thất bại", ex);
        }
        finally
        {
            stopwatch.Stop();
            SetBusy(false, finalStatus);
        }
    }

    private void ClearLog_Click(object? sender, EventArgs e)
    {
        _fileLog.Clear();
        AppendLog("BẮT ĐẦU NHẬT KÝ MỚI");
    }

    private bool EnsureKey()
    {
        if (_key is { Length: AesGcmService.KeySizeBytes })
            return true;

        MessageBox.Show("Chưa có khóa AES-256. Hãy bấm 'Sinh khóa mới' hoặc 'Import khóa...' trước.",
            "Thiếu khóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private void ReplaceKey(byte[] newKey)
    {
        if (_key is not null)
            CryptographicOperations.ZeroMemory(_key);
        _key = newKey;
        UpdateKeyStatus();
    }

    private void UpdateKeyStatus()
    {
        _keyStatus.Text = _key is { Length: AesGcmService.KeySizeBytes }
            ? "Đã có khóa trong RAM: AES-256, 256 bit / 32 byte."
            : "Chưa có khóa. Hãy sinh khóa mới hoặc import tệp khóa.";
    }

    private void SetBusy(bool busy, string statusText)
    {
        _isBusy = busy;
        foreach (Button button in _allButtons)
            button.Enabled = !busy;
        _tabs.Enabled = !busy;
        UseWaitCursor = busy;
        _operationStatus.Text = statusText;
    }

    private void AppendLog(params string[] lines)
    {
        _fileLog.AppendText("\r\n" + new string('-', 76) + "\r\n");
        _fileLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n");
        foreach (string line in lines)
            _fileLog.AppendText(line + "\r\n");
    }

    private static double ToMiB(long bytes) => bytes / (1024d * 1024d);

    private static string FormatBytes(long bytes)
    {
        const double kib = 1024d;
        const double mib = kib * 1024d;
        const double gib = mib * 1024d;
        if (bytes >= gib)
            return $"{bytes / gib:N2} GiB";
        if (bytes >= mib)
            return $"{bytes / mib:N2} MiB";
        if (bytes >= kib)
            return $"{bytes / kib:N2} KiB";
        return $"{bytes:N0} byte";
    }

    private static string FormatDuration(TimeSpan elapsed) =>
        elapsed.ToString(@"hh\:mm\:ss\.fff");

    private void ShowError(string title, Exception exception)
    {
        MessageBox.Show(this, exception.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isBusy)
        {
            e.Cancel = true;
            MessageBox.Show(this, "Ứng dụng đang xử lý tệp. Hãy đợi thao tác kết thúc trước khi đóng để tránh làm gián đoạn bài thực nghiệm.",
                "Đang xử lý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }
        base.OnFormClosed(e);
    }
}
