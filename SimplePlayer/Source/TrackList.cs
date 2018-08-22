using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace SimplePlayer.Source
{
    /// <summary>
    /// Класс трек листа
    /// </summary>
    public class TrackList
    {
        #region Поля класса

        /// <summary>
        /// Зацикленное проигрывание плей листа включенно или нет
        /// </summary>
        private bool loop;
        public bool Loop
        {
            set
            {
                if (value) { loop = true; return;}
                else
                { loop = false;}
            }
            get{ return loop;}
        }

        /// <summary>
        /// Внутренний список треков
        /// </summary>
        private List<Track> _Tracks;

        /// <summary>
        /// Список треков
        /// </summary>
        public List<Track> Tracks
        {
            private set { _Tracks = Tracks; }
            get { return _Tracks; }
        }

        #endregion

        #region Вложенныйe классы

        /// <summary>
        /// Вложенный класс
        /// </summary>
        public class Track
        {
            /// <summary>
            /// Имя трека
            /// </summary>
            public String TrackName;

            /// <summary>
            /// Полный путь к треку
            /// </summary>
            public String Path;

            /// <summary>
            /// Плщлучаем расширение фала (формат  - MP3, FLAC ....)
            /// </summary>
            public string Extension
            {
                get
                {
                    string[] tmp = TrackName.Split('.');          // оставляем только формат
                    string str = tmp[tmp.Length - 1];
                    return str = str.ToUpper();                   // делаем все буквы заглавными  
                }
            }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="path"></param>
            public Track(String path)
            {
                Path = path;
                string[] tmp = Path.Split('\\');
                TrackName = tmp[tmp.Length - 1];
            }
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Конструктор
        /// </summary>
        public TrackList()
        {
            _Tracks = new List<Track>();
        }

        #endregion

        /// <summary>
        /// Получает файлы треков из диалогового окна (открытия файлов)
        /// </summary>
        public void OpenFilesTracks ()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;                      // разрешаем выбирать много файлов сразу

            if (openFileDialog.ShowDialog() == true)
            {
                string[] Files = openFileDialog.FileNames;          // помещаем все имена треков в строковый массив 
                foreach(var file in Files)        
                {
                    _Tracks.Add(new Track(file));
                }
            }
        }

        /// <summary>
        /// Создает экземпляры класса Track по списку путей к трекам из плейлиста
        /// </summary>
        /// <param name="Files">Строковой список путей к трекам</param>
        public void TrackCreate(List<string> Files)
        {
            foreach (var file in Files)
            {
                _Tracks.Add(new Track(file));
            }
        }
    }
}
