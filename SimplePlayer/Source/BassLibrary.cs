using System;
using Un4seen.Bass;

namespace SimplePlayer.Source
{
    /// <summary>
    /// Статический класс 
    /// </summary>
    public static class BassLibrary
    {
        #region Поля класса

        /// <summary>
        /// Частота дискретизации ( ..., 44100 , 48000 ,...)
        /// </summary>
        public static int v_HZ = 44100;

        /// <summary>
        /// Состояние инициализации
        /// </summary>
        private static bool v_InitDefaultDevice;

        /// <summary>
        /// Поток
        /// </summary>
        public static int v_Stream;

        /// <summary>
        /// Громкость 
        /// </summary>
        public static int v_TrackVolume = 50;

        #endregion

        /// <summary>
        /// Метод  - инициализации устройства
        /// </summary>
        /// <param name="hz">Частота дискретизации</param>
        /// <returns></returns>
        private static bool InitBass(int hz)
        {
            if(!v_InitDefaultDevice)
            {
                v_InitDefaultDevice = Bass.BASS_Init(-1, hz, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            }
            return v_InitDefaultDevice;
        }


        /// <summary>
        ///  Метод - проигрывания трека (плей)
        /// </summary>
        /// <param name="filename"> полный путь к файлу </param>
        /// <param name="vol">Громкость</param>
        public static void Play(string filename, int vol)
        {
            if (Bass.BASS_ChannelIsActive(v_Stream) != BASSActive.BASS_ACTIVE_PAUSED) // если канал не в режиме паузы
            {
                if (v_Stream != 0) Stop();// если поток(канал) не равен нулю 

                if (InitBass(v_HZ)) // если библиотека иниацелизированна то загружаем файл
                {
                    v_Stream = Bass.BASS_StreamCreateFile(filename, 0, 0, BASSFlag.BASS_DEFAULT); 

                    if (v_Stream != 0)          // если поток(канал) не равен нулю и имеет нужный формат
                    {
                        SetVolumeToStream(v_Stream, v_TrackVolume);
                        Bass.BASS_ChannelPlay(v_Stream, false);
                    }
                }
            }
            else
            {
                Bass.BASS_ChannelPlay(v_Stream, false); // иначе в паузе и возобнавляем проигрывания трека(музыки)
            }
        }

       
        /// <summary>
        /// Мутод- останавливающий проигрывание трека (стоп) и освобождающий ресурсы 
        /// </summary>
        public static void Stop()
        {
            Bass.BASS_ChannelStop(v_Stream);
            Bass.BASS_StreamFree(v_Stream);
        }
        

        /// <summary>
        /// Метод - приостанавливающий проигрывания трека (пауза) 
        /// </summary>
        public static void Pаuse()
        {
            if (Bass.BASS_ChannelIsActive(v_Stream) == BASSActive.BASS_ACTIVE_PLAYING) Bass.BASS_ChannelPause(v_Stream);
        }

      
        /// <summary>
        /// Метод - установки громкости трека 
        /// </summary>
        /// <param name="stream"> Поток </param>
        /// <param name="vol"> Громкость </param>
        public static void SetVolumeToStream(int stream, int vol)
        {
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, vol / 100F);
        }


        /// <summary>
        /// Метод - получения длительности трека (музыки) в секундах
        /// </summary>
        /// <param name="stream"> Поток </param>
        /// <returns></returns>
        public static int GetTimeOfStream(int stream)
        {
            long TimeBytes = Bass.BASS_ChannelGetLength(stream);
            double Time = Bass.BASS_ChannelBytes2Seconds(stream, TimeBytes);
            return (int)Time;
        }


        /// <summary>
        /// Метод - получения текущей позиции в секундах
        /// </summary>
        /// <param name="stream"> Поток </param>
        /// <returns></returns>
        public static int GetPosOfStream(int stream)
        {
            long pos = Bass.BASS_ChannelGetPosition(stream);
            int posSec = (int)Bass.BASS_ChannelBytes2Seconds(stream, pos);
            return posSec;
        }


        /// <summary>
        /// Метод - перемотки слайдера длительности трека
        /// </summary>
        /// <param name="stream"> Поток</param>
        /// <param name="pos"> Позиция в потоке </param>
        public static void SetPosOfScroll(int stream, int pos)
        {
            Bass.BASS_ChannelSetPosition(stream, (double)pos);
        }


        /// <summary>
        /// Метод - проигрывания следующего трека в АВТО режиме (проигрывает следущий трек по завершению текущего)
        /// </summary>
        /// <returns></returns>
        public static bool GoNextTrack()
        {
            if ((Bass.BASS_ChannelIsActive(v_Stream) == BASSActive.BASS_ACTIVE_STOPPED))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}