using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ChatApp.Core;

namespace ChatApp.Server;

/// <summary>
/// Sunucu penceresi: dinleme ayarları, bağlı istemci listesi ve sunucu günlüğü.
/// </summary>
public class ServerForm : Form
{
    private const int MaxLogLines = 1000;

    private readonly ChatServer server = new();

    private readonly ComboBox bindCombo = new() { Name = "bindCombo", DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly NumericUpDown portInput = new() { Name = "portInput", Minimum = 1024, Maximum = 65535, Value = 9000, Width = 80 };
    private readonly Button startButton = CreateButton("startButton", "Başlat");
    private readonly Button stopButton = CreateButton("stopButton", "Durdur");
    private readonly Label statusIndicator = new()
    {
        AutoSize = false,
        Size = new Size(170, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.DimGray,
        Text = "■ DURDURULDU",
        Margin = new Padding(12, 3, 3, 3)
    };
    private readonly ListView clientList = new()
    {
        Name = "clientList",
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        GridLines = true
    };
    private readonly ListBox logList = new()
    {
        Name = "logList",
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9.5f),
        IntegralHeight = false,
        HorizontalScrollbar = true
    };
    private readonly GroupBox clientsGroup = new() { Text = "Bağlı İstemciler (0)", Dock = DockStyle.Fill, Padding = new Padding(8) };
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel addressLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    private bool closingAfterStop;

    public ServerForm()
    {
        Text = "YMH219 - Chat Sunucusu";
        ClientSize = new Size(960, 560);
        MinimumSize = new Size(820, 420);
        StartPosition = FormStartPosition.CenterScreen;

        bindCombo.Items.AddRange(new object[] { "Tüm ağ arayüzleri (0.0.0.0)", "Yalnızca bu bilgisayar (127.0.0.1)" });
        bindCombo.SelectedIndex = 0;

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(8, 8, 8, 4) };
        toolbar.Controls.AddRange(new Control[]
        {
            CreateLabel("Dinleme:"), bindCombo, CreateLabel("Port:"), portInput, startButton, stopButton, statusIndicator
        });

        clientList.Columns.Add("Kullanıcı", 110);
        clientList.Columns.Add("Adres", 140);
        clientList.Columns.Add("Bağlanma", 90);
        clientsGroup.Controls.Add(clientList);

        var logGroup = new GroupBox { Text = "Sunucu Günlüğü", Dock = DockStyle.Fill, Padding = new Padding(8) };
        logGroup.Controls.Add(logList);

        var split = new SplitContainer { Dock = DockStyle.Fill };
        split.Panel1.Padding = new Padding(8, 0, 0, 8);
        split.Panel2.Padding = new Padding(0, 0, 8, 8);
        split.Panel1.Controls.Add(clientsGroup);
        split.Panel2.Controls.Add(logGroup);

        statusStrip.Items.Add(addressLabel);

        Controls.Add(split);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);

        startButton.Click += (_, _) => StartServer();
        stopButton.Click += async (_, _) => await StopServerAsync();

        server.Log += text => RunOnUi(() => AddLog(text));
        server.ClientConnected += info => RunOnUi(() => AddClient(info));
        server.ClientDisconnected += info => RunOnUi(() => RemoveClient(info));

        UpdateState();
        Load += (_, _) => split.SplitterDistance = 370;
    }

    private void StartServer()
    {
        IPAddress address = bindCombo.SelectedIndex == 1 ? IPAddress.Loopback : IPAddress.Any;
        int port = (int)portInput.Value;

        try
        {
            server.Start(address, port);
        }
        catch (SocketException ex)
        {
            string reason = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? $"{port} numaralı port başka bir uygulama tarafından kullanılıyor."
                : ex.Message;
            AddLog($"Sunucu başlatılamadı: {reason}");
            MessageBox.Show(this, $"Sunucu başlatılamadı.\n\n{reason}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateState();
    }

    private async Task StopServerAsync()
    {
        stopButton.Enabled = false;
        await server.StopAsync();
        clientList.Items.Clear();
        UpdateClientCount();
        UpdateState();
    }

    private void UpdateState()
    {
        bool running = server.IsRunning;
        startButton.Enabled = !running;
        stopButton.Enabled = running;
        bindCombo.Enabled = !running;
        portInput.Enabled = !running;

        statusIndicator.Text = running ? "● ÇALIŞIYOR" : "■ DURDURULDU";
        statusIndicator.BackColor = running ? Color.SeaGreen : Color.DimGray;

        addressLabel.Text = running
            ? $"Dinleniyor: {server.LocalEndPoint}   |   Bu bilgisayarın IP adresleri: {string.Join(", ", GetLocalAddresses())}"
            : "Sunucu durduruldu";
    }

    private void AddClient(ClientInfo info)
    {
        var item = new ListViewItem(info.UserName) { Name = info.UserName };
        item.SubItems.Add(info.Address);
        item.SubItems.Add(info.ConnectedAt.ToString("HH:mm:ss"));
        clientList.Items.Add(item);
        UpdateClientCount();
    }

    private void RemoveClient(ClientInfo info)
    {
        clientList.Items.RemoveByKey(info.UserName);
        UpdateClientCount();
    }

    private void UpdateClientCount()
    {
        clientsGroup.Text = $"Bağlı İstemciler ({clientList.Items.Count})";
    }

    private void AddLog(string text)
    {
        if (logList.Items.Count >= MaxLogLines)
        {
            logList.Items.RemoveAt(0);
        }

        logList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {text}");
        logList.TopIndex = Math.Max(0, logList.Items.Count - 1);
    }

    private void RunOnUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // Pencere kapanırken gelen olaylar yok sayılır.
        }
    }

    private static IEnumerable<string> GetLocalAddresses()
    {
        try
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToList();
            return addresses.Count > 0 ? addresses : new[] { "127.0.0.1" };
        }
        catch (NetworkInformationException)
        {
            return new[] { "127.0.0.1" };
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (closingAfterStop || !server.IsRunning)
        {
            return;
        }

        e.Cancel = true;
        Enabled = false;
        await server.StopAsync();
        closingAfterStop = true;
        Close();
    }

    private static Label CreateLabel(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
    }

    private static Button CreateButton(string name, string text)
    {
        return new Button { Name = name, Text = text, AutoSize = true, MinimumSize = new Size(90, 30), Margin = new Padding(6, 3, 3, 3) };
    }
}
