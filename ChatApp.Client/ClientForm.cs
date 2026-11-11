using ChatApp.Core;

namespace ChatApp.Client;

/// <summary>
/// İstemci penceresi: bağlantı ayarları, mesaj alanı, çevrimiçi kullanıcılar ve mesaj gönderme.
/// </summary>
public class ClientForm : Form
{
    private static readonly Color SystemColor = Color.Gray;
    private static readonly Color ErrorColor = Color.Firebrick;

    private readonly ChatClient client = new();

    private readonly TextBox hostInput = new() { Name = "hostInput", Text = "127.0.0.1", Width = 130 };
    private readonly NumericUpDown portInput = new() { Name = "portInput", Minimum = 1, Maximum = 65535, Value = 9000, Width = 80 };
    private readonly TextBox userNameInput = new() { Name = "userNameInput", Width = 130, MaxLength = UserNameRules.MaxLength };
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
    private readonly GroupBox usersGroup = new() { Text = "Çevrimiçi (0)", Dock = DockStyle.Fill, Padding = new Padding(8) };
    private readonly ComboBox recipientCombo = new() { Name = "recipientCombo", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox messageInput = new() { Name = "messageInput", Dock = DockStyle.Fill, MaxLength = UserNameRules.MaxMessageLength, Font = new Font("Segoe UI", 10.5f) };
    private readonly Button sendButton = CreateButton("sendButton", "Gönder");
    private readonly Button saveHistoryButton = CreateButton("saveHistoryButton", "Geçmişi Kaydet");
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Text = "Bağlı değil" };

    private bool connecting;
    private bool closingAfterDisconnect;

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

        recipientCombo.Items.Add(UserNameRules.Everyone);
        recipientCombo.SelectedIndex = 0;
        sendButton.Enabled = false;
        messageInput.Enabled = false;

        connectButton.Click += async (_, _) => await ConnectAsync();
        disconnectButton.Click += async (_, _) => await client.DisconnectAsync();
        userNameInput.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await ConnectAsync();
            }
        };

        client.MessageReceived += message => RunOnUi(() => HandleMessage(message));
        client.Disconnected += reason => RunOnUi(() => HandleDisconnected(reason));

        UpdateState();
        Load += (_, _) => split.SplitterDistance = split.Width - 210;
        Shown += (_, _) => userNameInput.Focus();
    }

    private async Task ConnectAsync()
    {
        if (connecting || client.IsConnected)
        {
            return;
        }

        connecting = true;
        UpdateState();
        string host = hostInput.Text.Trim();
        int port = (int)portInput.Value;

        try
        {
            await client.ConnectAsync(host, port, userNameInput.Text);
            AppendSystem($"{host}:{port} sunucusuna \"{client.UserName}\" olarak bağlanıldı.", SystemColor);
        }
        catch (ChatException ex)
        {
            AppendSystem($"Bağlanılamadı: {ex.Message}", ErrorColor);
            MessageBox.Show(this, ex.Message, "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            connecting = false;
            UpdateState();
        }
    }

    private void HandleMessage(ChatMessage message)
    {
        switch (message.Type)
        {
            case MessageType.UserList:
                UpdateUsers(message.Users ?? new List<string>());
                break;
            case MessageType.UserJoined:
                AppendSystem($"{message.From} sohbete katıldı.", SystemColor, message.Timestamp);
                break;
            case MessageType.UserLeft:
                AppendSystem($"{message.From} sohbetten ayrıldı.", SystemColor, message.Timestamp);
                break;
            case MessageType.System:
                AppendSystem($"Sunucu: {message.Text}", ErrorColor, message.Timestamp);
                break;
        }
    }

    private void HandleDisconnected(string reason)
    {
        AppendSystem(reason, ErrorColor);
        usersList.Items.Clear();
        usersGroup.Text = "Çevrimiçi (0)";
        UpdateState();
    }

    private void UpdateUsers(IReadOnlyCollection<string> users)
    {
        usersList.BeginUpdate();
        usersList.Items.Clear();
        foreach (string user in users)
        {
            bool isMe = string.Equals(user, client.UserName, StringComparison.OrdinalIgnoreCase);
            usersList.Items.Add(isMe ? $"{user} (siz)" : user);
        }
        usersList.EndUpdate();
        usersGroup.Text = $"Çevrimiçi ({users.Count})";
    }

    private void UpdateState()
    {
        bool connected = client.IsConnected;
        bool idle = !connected && !connecting;

        hostInput.Enabled = idle;
        portInput.Enabled = idle;
        userNameInput.Enabled = idle;
        connectButton.Enabled = idle;
        disconnectButton.Enabled = connected;

        if (connecting)
        {
            statusIndicator.Text = "● BAĞLANIYOR";
            statusIndicator.BackColor = Color.DarkOrange;
            statusLabel.Text = "Sunucuya bağlanılıyor...";
        }
        else if (connected)
        {
            statusIndicator.Text = "● BAĞLI";
            statusIndicator.BackColor = Color.SeaGreen;
            statusLabel.Text = $"Bağlı: {hostInput.Text.Trim()}:{portInput.Value}   |   Kullanıcı: {client.UserName}";
        }
        else
        {
            statusIndicator.Text = "■ BAĞLI DEĞİL";
            statusIndicator.BackColor = Color.DimGray;
            statusLabel.Text = "Bağlı değil";
        }
    }

    private void AppendSystem(string text, Color color, DateTime? time = null)
    {
        AppendText($"[{time ?? DateTime.Now:HH:mm:ss}] ", Color.DarkGray, FontStyle.Regular);
        AppendText(text + Environment.NewLine, color, FontStyle.Italic);
        ScrollToEnd();
    }

    private void AppendText(string text, Color color, FontStyle style)
    {
        chatBox.SelectionStart = chatBox.TextLength;
        chatBox.SelectionLength = 0;
        chatBox.SelectionColor = color;
        chatBox.SelectionFont = new Font(chatBox.Font, style);
        chatBox.AppendText(text);
    }

    private void ScrollToEnd()
    {
        chatBox.SelectionStart = chatBox.TextLength;
        chatBox.ScrollToCaret();
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

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (closingAfterDisconnect || !client.IsConnected)
        {
            return;
        }

        e.Cancel = true;
        Enabled = false;
        await client.DisconnectAsync();
        closingAfterDisconnect = true;
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
