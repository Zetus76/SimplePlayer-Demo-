using Un4seen.Bass;


namespace SimplePlayer.Source
{
    /// <summary>
    /// Класс представляющий прослойку между клсассом MainWindow и классом BassLibrary (фассад)
    /// </summary>
    public class Player
    {
        #region Перечисления

        /// <summary>
        /// Состояние проигрывания трека
        /// </summary>
        public enum PlayState
        {
            /// <summary>Трек остановлен</summary>
            STOPPED = (int)BASSActive.BASS_ACTIVE_STOPPED,
            /// <summary>Трек проигрывается</summary>
            PLAYING = (int)BASSActive.BASS_ACTIVE_PLAYING,
            /// <summary>Трек заглушен</summary>
            STALLED = (int)BASSActive.BASS_ACTIVE_STALLED,
            /// <summary>Трек на паузе</summary>
            PAUSED = (int)BASSActive.BASS_ACTIVE_PAUSED          
        }

        #endregion

        #region Поля (свойства трека) получаемые и устанавливаемые в BassLibrary

        /// <summary>
        /// Громкость трека
        /// </summary>
        public int Value
        {
            set
            {
                BassLibrary.v_TrackVolume = value;
                BassLibrary.SetVolumeToStream(BassLibrary.v_Stream, value);
            }
            get { return BassLibrary.v_TrackVolume; }
        }

        /// <summary>
        /// Статус  трека 
        /// </summary>
        public PlayState playstatus
        {
            get { return (PlayState)Bass.BASS_ChannelIsActive(BassLibrary.v_Stream); }
        }

        /// <summary>
        /// Произошёл переход на следующий трек (был или нет)
        /// </summary>
        public bool gonexttrack
        {
            get { return BassLibrary.GoNextTrack(); }
        }

        /// <summary>
        /// Текущая позиция трека в потоке
        /// </summary>
        public int posofstream
        {
            set { }
            get { return BassLibrary.GetPosOfStream(BassLibrary.v_Stream); }
        }

        /// <summary>
        /// Длительность трека (в секундах)
        /// </summary>
        public int timeofstream
        {
            set { }
            get { return BassLibrary.GetTimeOfStream(BassLibrary.v_Stream); }
        }

        /// <summary>
        /// Поток трека 
        /// </summary>
        public int stream
        {
            set { }
            get { return BassLibrary.v_Stream; }
        }

        /// <summary>
        /// Позиция ползунка на слайдере длительности трека
        /// </summary>
        public int setposofscroll
        {
            set
            {
                BassLibrary.SetPosOfScroll(BassLibrary.v_Stream, value);
            } 
        }

        /// <summary>
        /// Зацикленное проигрывание плей листа (включенно или нет)
        /// </summary>
        private bool loopTr;
        public bool LoopTr
        {
            set
            {
                if (value)
                {
                    loopTr = true;
                    return;
                }
                else
                {
                    loopTr = false;
                }
            }

            get
            {
                return loopTr;
            }
        }

        /// <summary>
        /// Частота проигрывания
        /// </summary>
        public int hz
        {
            set
            {
                BassLibrary.v_HZ = value;
            }

            get
            {
                return BassLibrary.v_HZ;
            }

        }
        #endregion

        #region Методы работающие с BassLibrary
        /// <summary>
        /// Метод - проигрывает выбранный трек
        /// </summary>
        /// <param name="CurrentTrackPath"> Путь к треку </param>
        public void PlayTrack(string CurrentTrackPath)
        {
            BassLibrary.Play(CurrentTrackPath, BassLibrary.v_TrackVolume);
        }


        /// <summary>
        /// Метод - останавливающий проигрывание трека
        /// </summary>
        public void StopTrack()
        {
            BassLibrary.Stop();        
        }


        /// <summary>
        /// Метод - приостанавливающий проигрывание трека (пауза)
        /// </summary>
        public void PauseTrack()
        {
            BassLibrary.Pаuse();
        }
        #endregion
    }
}
