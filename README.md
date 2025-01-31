# EasyWhisper 🎙️

A powerful and user-friendly Windows application that converts speech to text in real-time using OpenAI's Whisper technology. Perfect for transcribing meetings, lectures, or any spoken content with high accuracy.

## ✨ Features

- 🎤 Real-time audio recording from microphone
- 🔊 System audio capture support
- 🌐 Dual processing options:
  - OpenAI API (cloud-based processing)
  - Local processing using Whisper.net
- 🌍 Language support:
  - Auto language detection
  - English optimization
  - French support
- ⚙️ Flexible configuration:
  - Multiple Whisper models (from Tiny to Large-V3)
  - Timestamp inclusion option
  - Recording file management
  - Transcript auto-save feature
- 🎨 Modern WPF interface with intuitive controls
- 📋 One-click copy to clipboard
- ⏱️ Real-time recording duration display
- 🔄 Progress tracking for transcription process

## 🚀 Requirements

- Windows OS
- .NET 8.0 Runtime
- Microphone (for audio input)
- Internet connection (for OpenAI API mode)

## 📥 Installation

1. Download the latest release from the releases page
2. Run the installer
3. Launch EasyWhisper from your Start menu

## 🎯 Usage

1. Launch EasyWhisper
2. Click the ⚙️ button to configure your preferences:
   - Choose between local or OpenAI processing
   - Select your preferred language
   - Configure the Whisper model (affects accuracy and speed)
   - Set additional options like timestamps and file saving
3. Click "Start Recording" to begin capturing audio
4. Speak or play the audio you want to transcribe
5. Click "Stop Recording" when finished
6. The transcribed text will appear in the main window
7. Use the "Copy to Clipboard" button to copy the transcription

## ⚙️ Configuration Options

- **Processing Mode**
  - OpenAI API (requires API key)
  - Local processing (uses Whisper.net)

- **Language Settings**
  - Auto-detect
  - English-optimized
  - French support

- **Whisper Models**
  - Tiny to Large-V3 options
  - Turbo variants for faster processing
  - English-specific models available

- **Additional Features**
  - System audio capture
  - Timestamp inclusion
  - Recording file retention
  - Automatic transcript saving

## 🛠️ Technical Details

Built with:
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Whisper.net
- NAudio for audio capture
- OpenAI API integration

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Feel free to submit issues and pull requests.

---
Made with ❤️ using OpenAI's Whisper technology
