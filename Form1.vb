Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class Form1
    Inherits Form

    Private txtFilePath As TextBox
    Private btnBrowse As Button
    Private grpEngine As GroupBox
    Private rbWhisper As RadioButton
    Private rbSapi As RadioButton
    Private btnStart As Button
    Private btnSave As Button
    Private btnClearLog As Button
    Private progressBar As ProgressBar
    Private lblStatus As Label
    Private splitContainer As SplitContainer
    Private txtResult As TextBox
    Private txtLog As TextBox

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "SpeechApp - Nhận diện giọng nói"
        Me.Width = 900
        Me.Height = 650
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterScreen

        ' ---------- Khu vực chọn file + engine ----------
        Dim topPanel As New Panel()
        topPanel.Dock = DockStyle.Top
        topPanel.Height = 110

        Dim lblFile As New Label()
        lblFile.Text = "File âm thanh:"
        lblFile.Location = New Point(10, 12)
        lblFile.AutoSize = True

        txtFilePath = New TextBox()
        txtFilePath.Location = New Point(10, 32)
        txtFilePath.Width = 650
        txtFilePath.ReadOnly = True

        btnBrowse = New Button()
        btnBrowse.Text = "Chọn file..."
        btnBrowse.Location = New Point(670, 30)
        btnBrowse.Width = 110
        btnBrowse.Height = 26
        AddHandler btnBrowse.Click, AddressOf btnBrowse_Click

        grpEngine = New GroupBox()
        grpEngine.Text = "Engine nhận diện"
        grpEngine.Location = New Point(10, 62)
        grpEngine.Width = 400
        grpEngine.Height = 40

        rbWhisper = New RadioButton()
        rbWhisper.Text = "Whisper (offline, hỗ trợ tiếng Việt)"
        rbWhisper.Location = New Point(10, 15)
        rbWhisper.AutoSize = True
        rbWhisper.Checked = True

        rbSapi = New RadioButton()
        rbSapi.Text = "SAPI (System.Speech, chỉ tiếng Anh)"
        rbSapi.Location = New Point(230, 15)
        rbSapi.AutoSize = True

        grpEngine.Controls.Add(rbWhisper)
        grpEngine.Controls.Add(rbSapi)

        btnStart = New Button()
        btnStart.Text = "Bắt đầu nhận diện"
        btnStart.Location = New Point(420, 66)
        btnStart.Width = 160
        btnStart.Height = 32
        AddHandler btnStart.Click, AddressOf btnStart_Click

        topPanel.Controls.Add(lblFile)
        topPanel.Controls.Add(txtFilePath)
        topPanel.Controls.Add(btnBrowse)
        topPanel.Controls.Add(grpEngine)
        topPanel.Controls.Add(btnStart)

        ' ---------- Thanh tiến trình + trạng thái ----------
        Dim statusPanel As New Panel()
        statusPanel.Dock = DockStyle.Top
        statusPanel.Height = 45

        progressBar = New ProgressBar()
        progressBar.Location = New Point(10, 5)
        progressBar.Width = 860
        progressBar.Height = 18
        progressBar.Minimum = 0
        progressBar.Maximum = 100

        lblStatus = New Label()
        lblStatus.Text = "Sẵn sàng."
        lblStatus.Location = New Point(10, 26)
        lblStatus.AutoSize = True

        statusPanel.Controls.Add(progressBar)
        statusPanel.Controls.Add(lblStatus)

        ' ---------- Nút phía dưới ----------
        Dim bottomPanel As New Panel()
        bottomPanel.Dock = DockStyle.Bottom
        bottomPanel.Height = 42

        btnSave = New Button()
        btnSave.Text = "Lưu kết quả..."
        btnSave.Location = New Point(10, 6)
        btnSave.Width = 130
        AddHandler btnSave.Click, AddressOf btnSave_Click

        btnClearLog = New Button()
        btnClearLog.Text = "Xóa log"
        btnClearLog.Location = New Point(150, 6)
        btnClearLog.Width = 100
        AddHandler btnClearLog.Click, AddressOf btnClearLog_Click

        bottomPanel.Controls.Add(btnSave)
        bottomPanel.Controls.Add(btnClearLog)

        ' ---------- Khu vực kết quả (trên) + log chi tiết (dưới) ----------
        splitContainer = New SplitContainer()
        splitContainer.Dock = DockStyle.Fill
        splitContainer.Orientation = Orientation.Horizontal

        Dim lblResult As New Label()
        lblResult.Text = "Kết quả nhận diện:"
        lblResult.Dock = DockStyle.Top
        lblResult.Height = 20
        lblResult.Padding = New Padding(5, 3, 0, 0)

        txtResult = New TextBox()
        txtResult.Multiline = True
        txtResult.ReadOnly = True
        txtResult.ScrollBars = ScrollBars.Vertical
        txtResult.Dock = DockStyle.Fill
        txtResult.Font = New Font("Segoe UI", 11)

        Dim resultPanel As New Panel()
        resultPanel.Dock = DockStyle.Fill
        resultPanel.Controls.Add(txtResult)
        resultPanel.Controls.Add(lblResult)

        Dim lblLog As New Label()
        lblLog.Text = "Log chi tiết:"
        lblLog.Dock = DockStyle.Top
        lblLog.Height = 20
        lblLog.Padding = New Padding(5, 3, 0, 0)

        txtLog = New TextBox()
        txtLog.Multiline = True
        txtLog.ReadOnly = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.Dock = DockStyle.Fill
        txtLog.Font = New Font("Consolas", 9)
        txtLog.BackColor = Color.Black
        txtLog.ForeColor = Color.LightGreen

        Dim logPanel As New Panel()
        logPanel.Dock = DockStyle.Fill
        logPanel.Controls.Add(txtLog)
        logPanel.Controls.Add(lblLog)

        splitContainer.Panel1.Controls.Add(resultPanel)
        splitContainer.Panel2.Controls.Add(logPanel)

        Me.Controls.Add(splitContainer)
        Me.Controls.Add(bottomPanel)
        Me.Controls.Add(statusPanel)
        Me.Controls.Add(topPanel)

        ' Đặt tỉ lệ chia đôi sau khi form đã có kích thước thật (tránh lỗi SplitterDistance)
        AddHandler Me.Load, Sub()
                                splitContainer.SplitterDistance = CInt(splitContainer.Height * 0.45)
                            End Sub
    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Filter = "File âm thanh (*.wav;*.mp3;*.wma;*.m4a;*.aac)|*.wav;*.mp3;*.wma;*.m4a;*.aac|Tất cả file (*.*)|*.*"
            ofd.Title = "Chọn file âm thanh"
            If ofd.ShowDialog() = DialogResult.OK Then
                txtFilePath.Text = ofd.FileName
            End If
        End Using
    End Sub

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtFilePath.Text) OrElse Not IO.File.Exists(txtFilePath.Text) Then
            MessageBox.Show("Vui lòng chọn file âm thanh hợp lệ.", "Thiếu file", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim filePath As String = txtFilePath.Text
        Dim useWhisper As Boolean = rbWhisper.Checked

        SetUiEnabled(False)
        txtLog.Clear()
        txtResult.Clear()
        progressBar.Value = 0
        lblStatus.Text = "Đang xử lý..."

        ' Progress(Of T) tự động chạy callback trên UI thread vì được tạo trên UI thread,
        ' nên AppendLog/UpdateProgress không cần Invoke thủ công.
        Dim progressLog As New Progress(Of String)(AddressOf AppendLog)
        Dim progressPercent As New Progress(Of Integer)(AddressOf UpdateProgress)

        Try
            Dim resultText As String

            If useWhisper Then
                progressBar.Style = ProgressBarStyle.Continuous
                resultText = Await SpeechEngine.RecognizeWithWhisperAsync(filePath, progressLog, progressPercent)
            Else
                ' SAPI không báo % được nên dùng thanh chạy (Marquee) trong lúc xử lý
                progressBar.Style = ProgressBarStyle.Marquee
                resultText = Await Task.Run(Function() SpeechEngine.RecognizeWithSapi(filePath, progressLog))
                progressBar.Style = ProgressBarStyle.Continuous
                progressBar.Value = 100
            End If

            txtResult.Text = resultText
            lblStatus.Text = "Hoàn tất."
            AppendLog("===== HOÀN TẤT =====")
        Catch ex As Exception
            AppendLog("Lỗi: " & ex.Message)
            lblStatus.Text = "Thất bại: " & ex.Message
            MessageBox.Show(ex.Message, "Lỗi khi nhận diện", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            SetUiEnabled(True)
        End Try
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtResult.Text) Then
            MessageBox.Show("Chưa có kết quả để lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using sfd As New SaveFileDialog()
            sfd.Filter = "Text file (*.txt)|*.txt"
            sfd.FileName = "ket_qua_nhan_dien.txt"
            If sfd.ShowDialog() = DialogResult.OK Then
                IO.File.WriteAllText(sfd.FileName, txtResult.Text, System.Text.Encoding.UTF8)
                MessageBox.Show("Đã lưu kết quả.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Private Sub btnClearLog_Click(sender As Object, e As EventArgs)
        txtLog.Clear()
    End Sub

    Private Sub AppendLog(message As String)
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}")
    End Sub

    Private Sub UpdateProgress(percent As Integer)
        Dim clamped As Integer = Math.Min(progressBar.Maximum, Math.Max(progressBar.Minimum, percent))
        progressBar.Value = clamped
        lblStatus.Text = $"Đang nhận diện... {clamped}%"
    End Sub

    Private Sub SetUiEnabled(enabled As Boolean)
        btnBrowse.Enabled = enabled
        btnStart.Enabled = enabled
        rbWhisper.Enabled = enabled
        rbSapi.Enabled = enabled
        btnSave.Enabled = enabled
    End Sub

End Class
