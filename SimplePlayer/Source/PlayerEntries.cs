using System;
using System.Collections.Generic;

namespace SimplePlayer.Source
{
    /// <summary>
    /// Константные сущности плеера
    /// </summary>
    public static class PlayerEntries
    {
        /// <summary> имя таблицы в базе данных, в ней хранятся - имена плейлистов </summary>
        public static readonly String NameTablePlayLists = "NamesPlayLists";

        /// <summary> имя таблицы в базе данных, в ней хранятся полные пути к трекам и к какому плей листу принадлежит каждый трек </summary>      
        public static readonly String NameTableTracks    = "Tracks";

        /// <summary> разделитель номера трека с именем трека </summary>
        public static readonly String TrackNameDelimiter = ". ";

        /// <summary> словарь цветов для лейбла отображающего формат трека (МP3 , FLAC ....)  </summary>
        public static readonly Dictionary<string, string> ColorList = new Dictionary<string, string>() 
        {
            { "MP3",  "#c40202" },
            { "WAV",  "#099906" },
            { "OGG",  "#0199ED" },
            { "FLAC", "#f27500" }
        };
    }
}
