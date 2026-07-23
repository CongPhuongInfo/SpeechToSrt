Imports System.Speech.Recognition
Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports NAudio.Wave
Imports Whisper.net
Imports Whisper.net.Ggml

' Một câu/đoạn đã nhận diện, kèm mốc thời gian bắt đầu-kết thúc (dùng để xuất SRT).
Public Class SpeechSegment
    Public Property Start As TimeSpan
    Public Property [End] As TimeSpan
    Public Property Text As String
End Class

' Kết quả trả về sau khi nhận diện: toàn bộ văn bản + danh sách từng câu có timestamp.
' Với SAPI, Segments sẽ rỗng vì engine này không cung cấp timestamp đáng tin cậy theo câu.
Public Class RecognitionResult
    Public Property FullText As String
    Public Property Segments As New List(Of SpeechSegment)()

    Public ReadOnly Property HasTimestamps As Boolean
        Get
            Return Segments IsNot Nothing AndAlso Segments.Count > 0
        End Get
    End Property
End Class

' ==================== Toàn bộ logic nhận diện giọng nói ====================
' Tách riêng khỏi Form1 để UI chỉ lo hiển thị, còn module này lo xử lý.
' log      : dùng để in các dòng log chi tiết (kèm timestamp do Form1 tự thêm)
' progress : dùng để báo % tiến trình (chỉ áp dụng cho Whisper, vì SAPI không
'            có cách nào biết trước % chính xác)
Module SpeechEngine

    ' ==================== WHISPER (offline, hỗ trợ tiếng Việt) ====================

    Public Async Function RecognizeWithWhisperAsync(filePath As String,
                                                      languageCode As String,
                                                      log As IProgress(Of String),
                                                      progress As IProgress(Of Integer)) As Task(Of RecognitionResult)
        Dim wavPath As String = Nothing
        Dim modelPath As String = IO.Path.Combine(AppContext.BaseDirectory, "models", "ggml-base.bin")

        Try
            log.Report("Đang kiểm tra model Whisper...")
            Await DownloadModelIfNeeded(modelPath, GgmlType.Base, log)

            log.Report("Đang chuyển đổi audio sang định dạng WAV 16kHz mono...")
            wavPath = ConvertToWav(filePath)

            Dim totalDurationMs As Double = GetWavDurationMs(wavPath)
            If totalDurationMs > 0 Then
                log.Report($"Thời lượng file: {TimeSpan.FromMilliseconds(totalDurationMs):hh\:mm\:ss}")
            End If

            log.Report($"Ngôn ngữ đã chọn: {languageCode}")

            Dim sb As New Text.StringBuilder()
            Dim segments As New List(Of SpeechSegment)()

            Using whisperFactory As WhisperFactory = WhisperFactory.FromPath(modelPath)
                Using processor As WhisperProcessor = whisperFactory.CreateBuilder().
                                                  WithLanguage(languageCode).
                                                  Build()

                    log.Report("Đang nhận diện giọng nói bằng Whisper...")
                    Using fileStream As IO.FileStream = IO.File.OpenRead(wavPath)
                        Dim asyncEnum = processor.ProcessAsync(fileStream).GetAsyncEnumerator()
                        While Await asyncEnum.MoveNextAsync()
                            Dim result = asyncEnum.Current
                            log.Report($"[{result.Start}->{result.End}] {result.Text}")
                            sb.AppendLine(result.Text)

                            segments.Add(New SpeechSegment With {
                                .Start = result.Start,
                                .[End] = result.End,
                                .Text = result.Text.Trim()
                            })

                            If totalDurationMs > 0 Then
                                Dim percent As Integer = CInt(Math.Min(100, (result.End.TotalMilliseconds / totalDurationMs) * 100))
                                progress.Report(percent)
                            End If
                        End While
                        Await asyncEnum.DisposeAsync()
                    End Using
                End Using
            End Using

            progress.Report(100)
            log.Report("Đang nhận diện Whisper hoàn tất.")

            Return New RecognitionResult With {
                .FullText = sb.ToString().Trim(),
                .Segments = segments
            }
        Finally
            If wavPath IsNot Nothing AndAlso IO.File.Exists(wavPath) Then
                Try
                    IO.File.Delete(wavPath)
                    log.Report("Đã xóa file WAV tạm.")
                Catch
                End Try
            End If
        End Try
    End Function

    ' Tự tải model ggml về thư mục "models" cạnh file .exe nếu chưa có.
    ' GgmlType.Base ~ 150MB, cân bằng tốc độ/độ chính xác.
    ' Có thể đổi thành GgmlType.Small hoặc GgmlType.Medium để chính xác hơn (chậm hơn, nặng hơn).
    Private Async Function DownloadModelIfNeeded(modelPath As String, ggmlType As GgmlType, log As IProgress(Of String)) As Task
        If IO.File.Exists(modelPath) Then
            log.Report("Model đã có sẵn: " & modelPath)
            Return
        End If

        Dim modelDir As String = IO.Path.GetDirectoryName(modelPath)
        IO.Directory.CreateDirectory(modelDir)

        log.Report($"Chưa có model, đang tải model '{ggmlType}' (có thể mất vài phút)...")
        Using modelStream = Await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType)
            Using fileWriter = IO.File.OpenWrite(modelPath)
                Await modelStream.CopyToAsync(fileWriter)
            End Using
        End Using
        log.Report("Tải model xong: " & modelPath)
    End Function

    ' ==================== SAPI / System.Speech (dự phòng, chỉ tiếng Anh...) ====================
    ' Hàm này chạy đồng bộ (blocking), Form1 sẽ gọi qua Task.Run để không treo UI.
    ' Không có timestamp đáng tin cậy theo câu nên Segments trả về rỗng (không xuất được SRT).
    Public Function RecognizeWithSapi(filePath As String, log As IProgress(Of String)) As RecognitionResult
        Dim wavPath As String = filePath
        Dim sb As New Text.StringBuilder()
        Dim readyToExit As New ManualResetEvent(False)

        Try
            log.Report("Đang chuyển đổi audio sang định dạng WAV phù hợp...")
            wavPath = ConvertToWav(filePath)

            Using recognizer As New SpeechRecognitionEngine(New CultureInfo("en-US"))
                recognizer.LoadGrammar(New DictationGrammar())
                recognizer.SetInputToWaveFile(wavPath)

                AddHandler recognizer.SpeechRecognized,
                    Sub(sender, e)
                        If e.Result IsNot Nothing Then
                            log.Report("Kết quả: " & e.Result.Text)
                            sb.AppendLine(e.Result.Text)
                        End If
                    End Sub

                AddHandler recognizer.RecognizeCompleted,
                    Sub(sender, e)
                        If e.Error IsNot Nothing Then
                            log.Report("Lỗi khi nhận diện: " & e.Error.Message)
                        End If
                        readyToExit.Set()
                    End Sub

                log.Report("Đang nhận diện giọng nói bằng SAPI...")
                recognizer.RecognizeAsync(RecognizeMode.Multiple)
                readyToExit.WaitOne()
            End Using

            log.Report("Nhận diện SAPI hoàn tất.")
            Return New RecognitionResult With {
                .FullText = sb.ToString().Trim(),
                .Segments = New List(Of SpeechSegment)()
            }
        Finally
            If wavPath <> filePath AndAlso IO.File.Exists(wavPath) Then
                Try
                    IO.File.Delete(wavPath)
                    log.Report("Đã xóa file WAV tạm.")
                Catch
                End Try
            End If
        End Try
    End Function

    ' ==================== Dùng chung: convert audio -> WAV PCM 16-bit mono 16kHz ====================

    Public Function ConvertToWav(inputPath As String) As String
        Dim wavPath As String = IO.Path.Combine(
            IO.Path.GetTempPath(),
            IO.Path.GetFileNameWithoutExtension(inputPath) & "_" & Guid.NewGuid().ToString("N").Substring(0, 8) & ".wav")

        Dim targetFormat As New WaveFormat(16000, 16, 1)

        Using reader As New AudioFileReader(inputPath)
            Using resampler As New MediaFoundationResampler(reader, targetFormat)
                resampler.ResamplerQuality = 60
                WaveFileWriter.CreateWaveFile(wavPath, resampler)
            End Using
        End Using

        Return wavPath
    End Function

    Private Function GetWavDurationMs(wavPath As String) As Double
        Try
            Using reader As New WaveFileReader(wavPath)
                Return reader.TotalTime.TotalMilliseconds
            End Using
        Catch
            Return 0
        End Try
    End Function

    ' ==================== Xuất SRT ====================

    ' Định dạng mốc thời gian theo chuẩn SRT: HH:MM:SS,mmm
    Private Function FormatSrtTime(t As TimeSpan) As String
        Dim totalHours As Integer = CInt(Math.Floor(t.TotalHours))
        Return $"{totalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}"
    End Function

    ' Ghép danh sách segment thành nội dung file .srt hoàn chỉnh.
    Public Function BuildSrtContent(segments As List(Of SpeechSegment)) As String
        Dim sb As New Text.StringBuilder()
        For i As Integer = 0 To segments.Count - 1
            Dim seg = segments(i)
            sb.AppendLine((i + 1).ToString())
            sb.AppendLine($"{FormatSrtTime(seg.Start)} --> {FormatSrtTime(seg.End)}")
            sb.AppendLine(seg.Text)
            sb.AppendLine()
        Next
        Return sb.ToString()
    End Function

End Module
