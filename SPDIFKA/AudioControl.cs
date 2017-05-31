﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SPDIFKA
{
    class AudioControl
    {
        private Boolean isSoundStarted = false;
        private SoundPlayer sound;
        private static AudioControl instance;
        private static UserPreferences UserPerfs = new UserPreferences();
        private UnmanagedMemoryStream sound_type;

        private AudioControl() { }

        /// <summary>
        /// Singleton management of the audio controls.
        /// </summary>
        public static AudioControl Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new AudioControl();
                }
                return instance;
            }
        }

        /// <summary>
        /// Start the audio playback which will keep the SPDIF link alive.
        /// </summary>
        public void start()
        {
            UserPerfs.Load(); //Reload preferences to make sure the latest sound type is applied.
            if (UserPerfs.SoundType == UserPreferences.Sound.Inaudible)
            {
                sound_type = Properties.Resources.inaudible;
            }
            else if (UserPerfs.SoundType == UserPreferences.Sound.Silent)
            {
                sound_type = Properties.Resources.blank;
            }
            else
            {
                sound_type = Properties.Resources.inaudible;
            }

            sound = new SoundPlayer(sound_type);
            sound.LoadCompleted += new AsyncCompletedEventHandler(wavPlayer_LoadCompleted);
            sound.LoadAsync();
        }
        
        /// <summary>
        /// Stop the audio playback which will stop the SPDIF link.
        /// </summary>
        public void stop()
        {
            //Make sure we don't try to call Stop on a null object.
            if(this.sound != null)
            {
                this.sound.Stop();
                sound.Dispose();
                sound = null;
            }
            isSoundStarted = false;
        }

        /// <summary>
        /// Check to see the current state of the audio playback is either started or stopped.
        /// </summary>
        /// <returns></returns>
        public Boolean isRunning()
        {
            return isSoundStarted;
        }


        /// <summary>
        /// Async WAV file event loading and begin playback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wavPlayer_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            isSoundStarted = true;
            ((System.Media.SoundPlayer)sender).PlayLooping();
        }
    }
}
