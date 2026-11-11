namespace ChatApp.Server;

/// <summary>
/// Sunucu penceresi: dinleme ayarları, bağlı istemci listesi ve sunucu günlüğü.
/// </summary>
public class ServerForm : Form
{
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
        addressLabel.Text = "Sunucu durduruldu";

        Controls.Add(split);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);

        stopButton.Enabled = false;
        Load += (_, _) => split.SplitterDistance = 370;
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
