﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;
using SPDIFKA.Lib;

namespace SPDIFKA {
    interface ILoopAudioPlayer : IDisposable {
        void TryDispose();
    }

    class WaveOutLoopAudioPlayer : ILoopAudioPlayer {
        private WaveOut SoundPlayer;
        private bool IsIdisposed;
        private readonly UnmanagedMemoryStream Stream;
        private readonly int DeviceId;

        public WaveOutLoopAudioPlayer(UnmanagedMemoryStream stream, int deviceId) {
            this.Stream = stream;
            this.DeviceId = deviceId;
            this.Start();
        }

        private void Start() {
            this.Stream.Position = 0;
            var reader = new WaveFileReader(this.Stream);
            var loop = new LoopStream(reader);
            this.SoundPlayer = new WaveOut { DeviceNumber = this.DeviceId };
            this.SoundPlayer.PlaybackStopped += this.SoundPlayer_PlaybackStopped;
            this.SoundPlayer.Init(loop);
            this.SoundPlayer.Play();
        }

        private void Stop() {
            if (this.SoundPlayer != null) {
                this.SoundPlayer.PlaybackStopped -= this.SoundPlayer_PlaybackStopped;
                this.SoundPlayer.Stop();
                this.SoundPlayer.Dispose();
                this.SoundPlayer = null;
            }
        }

        private void SoundPlayer_PlaybackStopped(object sender, StoppedEventArgs e) {
            if (!this.IsIdisposed) {
                try {
                    this.Stop();
                }
                catch {
                    //Do nothing
                }
                this.Start();
            }
        }

        public void TryDispose() {
            try {
                this.Dispose();
            }
            catch {
                //Do nothing.
            }
        }

        public void Dispose() {
            this.IsIdisposed = true;
            this.Stop();
        }
    }

    class AudioControl {
        public static readonly Lazy<AudioControl> Instance = new Lazy<AudioControl>(() => new AudioControl());
        private static readonly UserPreferences UserPerfs = new UserPreferences();

        public bool IsRunning { get; private set; }
        private List<ILoopAudioPlayer> AudioPlayers = new List<ILoopAudioPlayer>();
        private UnmanagedMemoryStream Sound;

        /// <summary>
        /// Start the audio playback which will keep the SPDIF link alive.
        /// </summary>
        public void Start() {
            UserPerfs.Load(); //Reload preferences to make sure the latest sound type is applied.
            foreach (var player in this.AudioPlayers) {
                player.TryDispose();
            }
            switch (UserPerfs.SoundType) {
                case UserPreferences.Sound.Silent:
                    this.Sound = Properties.Resources.blank;
                    var players = PlaySoundAsync(Properties.Resources.inaudible);
                    this.AudioPlayers = PlaySoundAsync(this.Sound);
                    if (players.Count != 0 && this.AudioPlayers.Count != 0) {
                        var timer = new Timer { Interval = 2000 };
                        timer.Tick += (sender, e) => {
                            foreach (var p in players) {
                                p.TryDispose();
                            }
                            timer.Dispose();
                        };
                        timer.Start();
                    }
                    break;
                case UserPreferences.Sound.Inaudible:
                default:
                    this.Sound = Properties.Resources.inaudible;
                    this.AudioPlayers = PlaySoundAsync(this.Sound);
                    break;
            }

            this.IsRunning = true;
        }

        private static List<ILoopAudioPlayer> PlaySoundAsync(UnmanagedMemoryStream sound) {
            var deviceIds = new HashSet<int>();
            foreach (var deviceName in UserPerfs.EnabledDeviceNames) {
                if (deviceName == UserPreferences.DEFAULT_AUDIO_DEVICE) {
                    deviceIds.Add(-1);
                }
                else {
                    if (WaveOut.DeviceCount <= 0) continue;
                    for (var deviceId = -1; deviceId < WaveOut.DeviceCount; deviceId++) {
                        var capabilities = WaveOut.GetCapabilities(deviceId);
                        if (capabilities.ProductName == deviceName) {
                            deviceIds.Add(deviceId);
                        }
                    }
                }
            }
            var players = new List<ILoopAudioPlayer>(deviceIds.Count);
            foreach (var deviceId in deviceIds) {
                try {
                    players.Add(new WaveOutLoopAudioPlayer(sound, deviceId: deviceId));
                }
                catch (Exception ex) {
                    //MessageBox.Show("Error: " + ex);
                    // Do nothing
                }
            }
            return players;
        }

        /// <summary>
        /// Stop the audio playback which will stop the SPDIF link.
        /// </summary>
        public void Stop() {
            foreach (var player in this.AudioPlayers) {
                player.TryDispose();
            }
            this.AudioPlayers.Clear();
            this.IsRunning = false;
        }
    }
}
