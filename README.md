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

## 🚀 Future Development Ideas & TODOs

### UI/UX Enhancements
- [ ] Implement "Always on Top" option for window behavior
- [ ] Add Compact Mode view
- [ ] Create Dark Mode theme support
- [ ] Add visual feedback for audio input/output
  - Rolling 1-second activity indicator (green light)
  - Optional: Audio levels visualization/EQ

### Core Features
- [ ] Add automatic clipboard copy after transcription
- [ ] Implement Windows-H like functionality
  - Auto-start recording when application launches
  - Send transcribed text to previously focused control
- [ ] Explore integration with alternative speech-to-text services
  - Investigate AI Explain for better speaker separation
  - Compare cost efficiency vs. Whisper
  - Evaluate word recognition accuracy across services

### Integration & Export
- [ ] Develop post-transcription workflow options
  - Create pluggable architecture for external processors
  - Support command-line execution with transcription file input
  - Allow intention/context passing to external processors
- [ ] Add export capabilities for various use cases:
  - Sprint retrospective reports
  - CFR documentation
  - Meeting summaries

### Nice-to-Have
- [ ] Implement modular post-processing system
- [ ] Create plugin architecture for extended functionality
- [ ] Add configuration options for automated workflows

---
💡 Note: This list is dynamic and will be updated based on user feedback and development priorities.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Feel free to submit issues and pull requests.

---
Made with ❤️ using OpenAI's Whisper technology
