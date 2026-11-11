namespace ChatApp.Client;

/// <summary>
/// İstemci penceresi: bağlantı ayarları, mesaj alanı, çevrimiçi kullanıcılar ve mesaj gönderme.
/// </summary>
public class ClientForm : Form
{
    private readonly TextBox hostInput = new() { Name = "hostInput", Text = "127.0.0.1", Width = 130 };
    private readonly NumericUpDown portInput = new() { Name = "portInput", Minimum = 1, Maximum = 65535, Value = 9000, Width = 80 };
    private readonly TextBox userNameInput = new() { Name = "userNameInput", Width = 130, MaxLength = 20 };
    private readonly Button connectButton = CreateButton("connectButton", "Bağlan");
    private readonly Button disconnectButton = CreateButton("disconnectButton", "Bağlantıyı Kes");
    private readonly Label statusIndicator = new()
    {
        AutoSize = false,
        Size = new Size(150, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.DimGray,
        Text = "■ BAĞLI DEĞİL",
        Margin = new Padding(12, 3, 3, 3)
    };
    private readonly RichTextBox chatBox = new()
    {
        Name = "chatBox",
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = Color.White,
        Font = new Font("Segoe UI", 10.5f)
    };
    private readonly ListBox usersList = new() { Name = "usersList", Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Segoe UI", 10f) };
    private readonly ComboBox recipientCombo = new() { Name = "recipientCombo", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox messageInput = new() { Name = "messageInput", Dock = DockStyle.Fill, MaxLength = 500, Font = new Font("Segoe UI", 10.5f) };
    private readonly Button sendButton = CreateButton("sendButton", "Gönder");
    private readonly Button saveHistoryButton = CreateButton("saveHistoryButton", "Geçmişi Kaydet");
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Text = "Bağlı değil" };

    public ClientForm()
    {
        Text = "YMH219 - Chat İstemcisi";
        ClientSize = new Size(960, 600);
        MinimumSize = new Size(860, 450);
        StartPosition = FormStartPosition.CenterScreen;

        var connectionBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(8, 8, 8, 4) };
        connectionBar.Controls.AddRange(new Control[]
        {
            CreateLabel("Sunucu:"), hostInput, CreateLabel("Port:"), portInput, CreateLabel("Kullanıcı adı:"), userNameInput,
            connectButton, disconnectButton, statusIndicator
        });

        var chatGroup = new GroupBox { Text = "Mesajlar", Dock = DockStyle.Fill, Padding = new Padding(8) };
        chatGroup.Controls.Add(chatBox);

        var usersGroup = new GroupBox { Text = "Çevrimiçi (0)", Dock = DockStyle.Fill, Padding = new Padding(8) };
        usersGroup.Controls.Add(usersList);

        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2 };
        split.Panel1.Padding = new Padding(8, 0, 0, 0);
        split.Panel2.Padding = new Padding(0, 0, 8, 0);
        split.Panel1.Controls.Add(chatGroup);
        split.Panel2.Controls.Add(usersGroup);

        var sendBar = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 46, ColumnCount = 5, Padding = new Padding(8, 4, 8, 4) };
        sendBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sendBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        sendBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sendBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sendBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sendBar.Controls.Add(CreateLabel("Alıcı:"), 0, 0);
        sendBar.Controls.Add(recipientCombo, 1, 0);
        sendBar.Controls.Add(messageInput, 2, 0);
        sendBar.Controls.Add(sendButton, 3, 0);
        sendBar.Controls.Add(saveHistoryButton, 4, 0);

        statusStrip.Items.Add(statusLabel);

        Controls.Add(split);
        Controls.Add(sendBar);
        Controls.Add(connectionBar);
        Controls.Add(statusStrip);

        recipientCombo.Items.Add("Herkes");
        recipientCombo.SelectedIndex = 0;
        disconnectButton.Enabled = false;
        sendButton.Enabled = false;
        messageInput.Enabled = false;
        Load += (_, _) => split.SplitterDistance = split.Width - 210;
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
