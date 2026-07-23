# SpeechApp

Ứng dụng WinForms (VB.NET, .NET 9) nhận diện giọng nói từ file âm thanh, hỗ trợ nhiều ngôn ngữ qua **Whisper** (offline) hoặc **SAPI** (Windows Speech Recognition), có xuất kết quả ra file `.txt` và phụ đề `.srt`.

## Tính năng

- Chọn file âm thanh: `.wav`, `.mp3`, `.wma`, `.m4a`, `.aac`
- Hai engine nhận diện:
  - **Whisper** (offline, dùng model `ggml-base`, đa ngôn ngữ) — khuyến nghị dùng chính
  - **SAPI** (`System.Speech`) — dự phòng, chỉ nhận diện tiếng Anh (`en-US`)
- Chọn ngôn ngữ cho Whisper: Tự động phát hiện, Việt, Anh, Trung, Nhật, Hàn, Pháp, Đức, Nga, Thái
- Thanh tiến trình:
  - Whisper: chạy theo % thực tế (tính theo mốc thời gian đã nhận diện so với tổng thời lượng file)
  - SAPI: chạy kiểu Marquee (vô định) vì engine này không báo % được
- Log chi tiết từng bước (kèm giờ:phút:giây), hiển thị riêng với kết quả
- Xuất kết quả nhận diện ra file `.txt`
- Xuất phụ đề `.srt` (chỉ khả dụng khi dùng Whisper, vì SAPI không có timestamp theo câu)

## Yêu cầu hệ thống

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download) trở lên
- Kết nối mạng cho lần chạy đầu tiên với Whisper (tự động tải model `ggml-base.bin`, khoảng 150MB, lưu vào thư mục `models/` cạnh file `.exe`)

## Cấu trúc project

```
SpeechApp/
├── Program.vb          Entry point (khởi động WinForms)
├── Form1.vb             Giao diện chính (code thuần, không dùng designer)
├── SpeechEngine.vb       Toàn bộ logic nhận diện (Whisper + SAPI) và xuất SRT
├── SpeechApp.vbproj      File project (.NET 9, WinForms)
├── build.bat             Script build (Release)
└── run.bat               Script chạy nhanh sau khi build
```

## Cách build & chạy

1. Cài [.NET 9 SDK](https://dotnet.microsoft.com/download) nếu chưa có.
2. Chạy `build.bat` để restore NuGet packages và build bản Release.
3. Chạy `run.bat`, hoặc mở trực tiếp:
   ```
   bin\Release\net9.0-windows\SpeechApp.exe
   ```

## Cách dùng

1. Bấm **"Chọn file..."** để chọn file âm thanh.
2. Chọn engine: **Whisper** (khuyến nghị, hỗ trợ nhiều ngôn ngữ) hoặc **SAPI** (chỉ tiếng Anh).
3. Nếu dùng Whisper, chọn ngôn ngữ ở ô **"Ngôn ngữ (Whisper)"** (để "Tự động phát hiện" nếu không chắc, hoặc chọn đúng ngôn ngữ để nhận diện nhanh và chính xác hơn).
4. Bấm **"Bắt đầu nhận diện"** và theo dõi tiến trình + log chi tiết.
5. Sau khi hoàn tất:
   - Bấm **"Lưu kết quả..."** để xuất văn bản ra `.txt`
   - Bấm **"Xuất SRT..."** để xuất phụ đề `.srt` (chỉ bật khi dùng Whisper)

## Ghi chú về chất lượng nhận diện

- Model mặc định là `GgmlType.Base` (~150MB) — cân bằng giữa tốc độ và độ chính xác. Muốn chính xác hơn (đổi lại chậm hơn, nặng hơn), sửa trong `SpeechEngine.vb`:
  ```vb
  Await DownloadModelIfNeeded(modelPath, GgmlType.Base, log)
  ```
  đổi `GgmlType.Base` thành `GgmlType.Small` hoặc `GgmlType.Medium` (và đổi luôn tên file trong `modelPath` để tránh lẫn với model cũ).
- SAPI cần máy đã cài gói ngôn ngữ tiếng Anh của Windows Speech Recognition thì mới hoạt động được.
- File âm thanh sẽ luôn được convert sang WAV PCM 16-bit mono 16kHz trước khi đưa vào engine nhận diện (dùng NAudio), file WAV tạm sẽ tự xóa sau khi xong.

## Giới hạn hiện tại

- SRT chỉ xuất được với Whisper vì SAPI không cung cấp timestamp theo câu.
- Whisper cần tải model lần đầu (cần mạng), các lần sau dùng lại model đã tải.
- Chưa hỗ trợ chọn nhiều file cùng lúc (xử lý hàng loạt).
