using Microsoft.VisualBasic;
using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;
using System.Text.Json;

namespace EasyWhisper
{
    // Adapted from TeamScriber.CommandLine.WhisperHelper https://github.com/PatBQc/TeamScriber
    internal class WhisperHelper
    {
        private static readonly TimeSpan AudioSegmentTime = new TimeSpan(0, 5, 0);

        public async static Task<string> GenerateTranscription(
            string filename,
            string? openAIApiKey = null,
            string? openAIBasePath = null,
            bool includeTimestamps = false,
            string languageCode = "fr")
        {
            // Configure your OpenAI API key
            if (string.IsNullOrEmpty(openAIApiKey))
            {
                openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            var openAiOptions = new OpenAIOptions()
            {
                ApiKey = openAIApiKey
            };

            // Set the base path if provided
            if (!string.IsNullOrEmpty(openAIBasePath))
            {
                openAiOptions.BaseDomain = openAIBasePath;
            }

            var openAiService = new OpenAIService(openAiOptions);

            //Console.WriteLine($"# Processing audio");
            //Console.WriteLine();
            //Console.WriteLine("Audio file: {audio}");
            //Console.WriteLine();

            //var outputDirectory = context.Options.TranscriptionOutputDirectory;
            //if (string.IsNullOrEmpty(outputDirectory))
            //{
            //    outputDirectory = Path.GetDirectoryName(audio);
            //    if (string.IsNullOrEmpty(outputDirectory))
            //    {
            //        outputDirectory = Directory.GetCurrentDirectory();
            //    }
            //    else
            //    {
            //        Directory.CreateDirectory(outputDirectory);
            //    }
            //    Console.WriteLine($"Output directory is not set. Defaulting to current directory: {outputDirectory}");
            //    Console.WriteLine();
            //}
            //else
            //{
            //    Directory.CreateDirectory(outputDirectory);
            //    Console.WriteLine($"Using configured output directory: {outputDirectory}");
            //    Console.WriteLine();
            //}

            //var transcription = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(audio) + ".txt");
            //context.Transcriptions.Add(transcription);

            var audioFileContent = await System.IO.File.ReadAllBytesAsync(filename);
            var audioChunks = SplitAudioIntoChunks(filename, audioFileContent, AudioSegmentTime);

            double progressChunk = 1 / (double)audioChunks.Count;
            double progress = 0.0;

            await Task.WhenAll(audioChunks.Select(async audioChunk =>
            {
                int retryCount = 5;
                bool success = false;

                while (!success && retryCount >= 0)
                {
                    try
                    {
                        Console.WriteLine($"Launching transcription chunk #{audioChunk.ID} of {audioChunks.Count}");

                        var responseFormat = includeTimestamps ? StaticValues.AudioStatics.ResponseFormat.Srt : StaticValues.AudioStatics.ResponseFormat.VerboseJson;

                        var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
                        {
                            FileName = "audio.mp3",
                            File = audioChunk.AudioSegment,
                            Model = Models.WhisperV1,
                            Language = languageCode,
                            ResponseFormat = responseFormat
                        });

                        if (audioResult.Successful)
                        {
                            success = true;
                            audioChunk.Text = responseFormat == StaticValues.AudioStatics.ResponseFormat.Srt ? ExtractPlainText(audioResult.Text) : audioResult.Text;
                            Console.WriteLine($"Transcription for chunk #{audioChunk.ID} of {audioChunks.Count} done.");
                            Console.WriteLine();
                            Console.WriteLine($"Transcription for chunk {audioChunk.ID}:" + Environment.NewLine + audioChunk.Text);
                            Console.WriteLine();

                            progress += progressChunk;
                            // context.ProgressRepporter?.Report(context.ProgressInfo);
                        }
                        else
                        {
                            if (audioResult.Error == null)
                            {
                                Console.WriteLine();
                                Console.WriteLine($"/!\\ Did not receive a successful response from Whisper API on chunk #{audioChunk.ID} /!\\");
                                Console.WriteLine();
                            }
                            else
                            {
                                Console.WriteLine();
                                Console.WriteLine($"/!\\ Did not receive a successful response from Whisper API on chunk #{audioChunk.ID} /!\\");
                                Console.WriteLine("Error " + audioResult.Error.Code + ": " + audioResult.Error.Message);
                                Console.WriteLine();
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"/!\\ Did not receive a proper response from Whisper API on chunk #{audioChunk.ID} /!\\");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine();
                    }

                    --retryCount;

                    if (!success)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Retrying chunk #{audioChunk.ID}...");
                        Console.WriteLine();
                    }
                }
            }));

            Console.WriteLine("--> Finished transcription of " + filename);
            Console.WriteLine();
            Console.WriteLine();

            // Adjust timestamps before concatenation
            if (includeTimestamps)
            {
                AdjustTimestamps(audioChunks);
            }


            var fullTranscriptionText = string.Join(Environment.NewLine, audioChunks.OrderBy(x => x.ID).Select(x => x.Text));
            return fullTranscriptionText;
            //File.WriteAllText(transcription, fullTranscriptionText);

            // context.ProgressInfo.Value = progressChunksCompleted;
            // context.ProgressRepporter?.Report(context.ProgressInfo);
        }

        private static void AdjustTimestamps(List<AudioChunk> audioChunks)
        {
            TimeSpan cumulativeDuration = TimeSpan.Zero;

            foreach (var chunk in audioChunks.OrderBy(x => x.ID))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    var lines = chunk.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(' ');
                        if (parts.Length > 0 && TimeSpan.TryParse(parts[0], out TimeSpan timestamp))
                        {
                            var newTimestamp = timestamp + cumulativeDuration;
                            lines[i] = newTimestamp.ToString(@"hh\:mm\:ss");
                        }
                    }
                    chunk.Text = string.Join(Environment.NewLine, lines);
                }
                cumulativeDuration += AudioSegmentTime;
            }
        }

        private static List<AudioChunk> SplitAudioIntoChunks(string audiofilename, byte[] audioFileContent, TimeSpan chunkSize)
        {
            var audioChunks = new List<AudioChunk>();
            int segmentIndex = 0;
            TimeSpan currentPosition = TimeSpan.Zero;

            // Utilisation de MediaFoundationReader pour tous les types de fichiers, y compris MP3 et M4A
            using (var reader = new MediaFoundationReader(audiofilename))
            {
                var estimatedChunks = (int)Math.Ceiling(reader.TotalTime.TotalSeconds / chunkSize.TotalSeconds);
                long targetBytes = (long)(chunkSize.TotalSeconds * reader.WaveFormat.AverageBytesPerSecond);

                // double progressChunk = LogicConsts.ProgressWeightAudioPerVideo / (double)estimatedChunks;
                // double progressChunksCompleted = context.ProgressInfo.Value + LogicConsts.ProgressWeightAudioPerVideo;

                while (currentPosition < reader.TotalTime)
                {
                    Console.WriteLine($"Splitting and converting audio (m4a --> mp3) to chunk #{segmentIndex + 1} of {estimatedChunks} to send to Whisper later");

                    using (var segmentStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        long bytesWritten = 0;

                        while (bytesWritten < targetBytes)
                        {
                            int bytesRead = reader.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                                break;

                            segmentStream.Write(buffer, 0, bytesRead);
                            bytesWritten += bytesRead;
                        }

                        segmentStream.Flush();
                        byte[] segmentBytes;

                        // Réinitialise la position du MemoryStream pour bien encoder en MP3
                        segmentStream.Position = 0;

                        using (var mp3Stream = new MemoryStream())
                        {
                            MediaFoundationEncoder.EncodeToMp3(new RawSourceWaveStream(segmentStream, reader.WaveFormat), mp3Stream);
                            mp3Stream.Flush();
                            segmentBytes = mp3Stream.ToArray();
                        }

                        audioChunks.Add(new AudioChunk() { ID = ++segmentIndex, AudioSegment = segmentBytes, Text = string.Empty });

                        // Enregistrer le segment dans le répertoire de sortie
                        //var outputDirectory = context.Options.AudioOutputDirectory ?? Directory.GetCurrentDirectory();
                        //var segmentFilename = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(audiofilename)}-part-{segmentIndex.ToString("0000")}.mp3");
                        //File.WriteAllBytes(segmentFilename, segmentBytes);

                        currentPosition += chunkSize;

                        //context.ProgressInfo.Value += progressChunk;
                        //context.ProgressRepporter?.Report(context.ProgressInfo);
                    }
                }

                //context.ProgressInfo.Value = progressChunksCompleted;
                //context.ProgressRepporter?.Report(context.ProgressInfo);
            }

            //Console.WriteLine();
            //Console.WriteLine($"Audio split ({LogicConsts.AudioSegmentTime.ToString()} max) and conversion (mp3) done.");
            //Console.WriteLine();

            return audioChunks;
        }

        private static string ExtractPlainText(string srtJsonResponse)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(srtJsonResponse);
                if (jsonDoc.RootElement.TryGetProperty("text", out var textElement))
                {
                    var srtLines = textElement.GetString().Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var plainTextBuilder = new StringBuilder();
                    foreach (var srtLine in srtLines)
                    {
                        var srtParts = srtLine.Split('\n');
                        if (srtParts.Length >= 3)
                        {
                            var timestamp = srtParts[1]; // Second line is the timestamp
                            var text = string.Join(" ", srtParts.Skip(2)); // Skip ID and get only the text
                            plainTextBuilder.AppendLine(timestamp);
                            plainTextBuilder.AppendLine(text);
                            plainTextBuilder.AppendLine(); // Add an extra new line for readability
                        }
                    }
                    return plainTextBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"/!\\ Error parsing SRT JSON response: {ex.Message}");
            }
            return string.Empty;
        }

    } // end of class
}

