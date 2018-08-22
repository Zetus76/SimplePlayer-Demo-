using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data.Common;
using System.Windows;

namespace SimplePlayer.Source
{
    /// <summary>
    /// Обёртка для SqlLite DB
    /// </summary>
    public class SQLiteBd
    {
        #region Поля класса

        /// <summary>
        /// Список созданных баз данных
        /// </summary>
        private static Dictionary<string, SQLiteBd> DbList = new Dictionary<string, SQLiteBd>();

        /// <summary>
        /// Уникальное соединение с SQLite.
        /// </summary>
        public SQLiteConnection BD;

        #endregion

        /// <summary>
        /// Метод - получающий ссылку на объект класса по ключу
        /// </summary>
        /// <param name="keyname">Ключ</param>
        /// <returns>Возвращает ссылку на экземляр класса обёртки SqlLite DB</returns>
        static public SQLiteBd GetDbHandle(string keyname)
        {
            if (DbList.ContainsKey(keyname))
            {
                return DbList[keyname];
            }
            throw new Exception("No database found");
        }

        #region Инициализация

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="keyname">Ключ, для последующего обращения к созданной БД посредством метода GetDbHandle</param>
        public SQLiteBd(string keyname)
        {
            DbList.Add(keyname, this); //запись в список (словарь)
        }

        #endregion

        #region Открытие и закрытие базы данных

        /// <summary>
        /// Мутод - открытия базы данных 
        /// </summary>
        public void OpenSimplePlayerBD()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
            builder.Add(@"Data Source", System.Windows.Forms.Application.StartupPath + "\\SimplePlayer.db");
            builder.Add(@"Version", 3);
            BD = new SQLiteConnection(builder.ConnectionString);
            //Console.Write(builder.ConnectionString);
            BD.Open();
        }


        /// <summary>
        /// Метод - закрытия базы данных
        /// </summary>
        public void CloseOpenSimplePlayerBD()
        {
            BD.Close();
        }

        #endregion

        #region Создание всех  таблиц в базе данных

        /// <summary>
        /// Метод - создания таблицы в базе данных, для плейлистов (ИМЕНА ПЛЕЙЛИСТОВ ) , если такой ещё не созданно
        /// </summary>
        /// <param name="tableName">Имя таблицы для имён плейлистов</param>
        public void CreateNamesPlayListTable(string tableName)
        {
            SQLiteCommand CMD = BD.CreateCommand();
            CMD.CommandText = @"create table if not exists [" + tableName + "]([id] INTEGER not null primary key autoincrement,[PlayList_Name] TEXT UNIQUE )";
            CMD.ExecuteNonQuery();
        }


        /// <summary>
        /// Метод - создания таблицы в базе данных, для треклиста (ИМЯ ПЛЕЙЛИСТА , ИМЯ ТРЕКА(путь к нему), НОМЕР ТРЕКА (в плей листе) ) , если такой ещё не созданно
        /// </summary>
        /// <param name="tableName">Имя таблицы для треков с параметрами(имя трека (полный путь),плейлист, и тд и тп)</param>
        public void CreateTracksTable (string tableName)
        {
            SQLiteCommand CMD = BD.CreateCommand();
            CMD.CommandText = @"create table if not exists [" + tableName + "]([id] INTEGER  UNIQUE not null primary key autoincrement,[PlayList_Name] TEXT, [Track_Name] TEXT , [Track_Number] INTEGER)";
            CMD.ExecuteNonQuery();
        }
        #endregion

        #region Методы работы с таблицами плейлистов 

        /// <summary>
        /// Метод - считывает из базы данных, список треков из выбранного плейлиста
        /// </summary>
        /// <param name="playlistname">Имя плейлиста</param>
        /// <param name="tablename">Имя таблицы</param>
        /// <returns>Возвращает список загруженных путей к трекам</returns>
        public List<string> LoadTrackListDB(string playlistname, string tablename)
        {
            List<string> TrackList = new List<string>();

            SQLiteCommand CM = BD.CreateCommand();
            CM.CommandText = "SELECT Track_Name  FROM `" + tablename + "` WHERE  PlayList_Name = '" + playlistname + "';";   // проверяем - существуют ли записи в базе данных  
            SQLiteDataReader SQL = CM.ExecuteReader();

            while (SQL.Read()) // читаем все поля записи из таблицы
            {
                string Track_Name = SQL["Track_Name"].ToString();
                TrackList.Add(Track_Name);
            }

            return TrackList;
        }


        /// <summary>
        /// Мутод - считывает из базы данных список плейлистов 
        /// </summary>
        /// <param name="tablename">Имя таблицы</param>
        /// <returns>Возвращает список названий считанных плейлистов</returns>
        public List<string> LoadPlayListsDB(string tablename)
        {
            List<string> PlayLists = new List<string>();

            SQLiteCommand CM = BD.CreateCommand();
            CM.CommandText = "SELECT PlayList_Name FROM `" + tablename + "`";   // проверяем - существуют ли записи в базе данных
            SQLiteDataReader SQL = CM.ExecuteReader();
            
            while (SQL.Read()) // читаем все поля записи из таблицы
            {
                string PlayList_Name = SQL["PlayList_Name"].ToString();
                PlayLists.Add(PlayList_Name);
            }

            return PlayLists;
        }


        /// <summary>
        /// Метод - записывает  в таблицу имён плейлистов -имя нового (создаваемого) плейлиста, а так же производит запись из треклиста , всех терков в таблицу треков.
        /// </summary>
        /// <param name="tablenamepl">Имя таблицы для имён плейлистов</param>
        /// <param name="tablenametrl">Имя таблицы для треков </param>
        /// <param name="trackslist">Список треклиста</param>
        /// <param name="playlistname">Имя записываемого плейлиста</param>
        /// <returns>Возвращает TRUE, если перезапись была выполнена успешно</returns>
        public bool SavePlayListsDB(string tablenamepl, string tablenametrl, List<string> trackslist, string playlistname)
        {
            // проверяем - существует ли уже запись такого ИМЕНИ плейлиста в базе данных
            SQLiteCommand CM = BD.CreateCommand();
            CM.CommandText = "SELECT PlayList_Name FROM `" + tablenamepl + "` WHERE PlayList_Name = '" + playlistname + "';";
            SQLiteDataReader SQL = CM.ExecuteReader();

            if (SQL.HasRows)    // если найденa запись
            {
                // переспрашиваем пользователя  - хочет ли он перезаписать существующий плей лист или нет
                if (MessageBoxResult.No == System.Windows.MessageBox.Show("Playlist will be overwritten. Are you sure?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    return false; // отмена перезаписи существуещего плей листа
                }
                else
                {
                    // удаляем все записи с именем данного плей листа из таблицы треков
                    string deletetxt = "DELETE FROM `" + tablenametrl + "` WHERE  PlayList_Name = '" + playlistname + "';";
                    SQLiteCommand CMD = new SQLiteCommand(deletetxt, BD);
                    CMD.ExecuteNonQuery();

                    // И снова записываем обновлёный список треков треклиста в базу данных
                    for (int i = 0; i < trackslist.Count; i++)
                    {
                        string text_track_list = "INSERT INTO `" + tablenametrl + "` (PlayList_Name, Track_Name, Track_Number) VALUES('" + playlistname + "'" + "," + "'" + trackslist[i] + "'" + "," + "'" + i + "'" + ");";
                        SQLiteCommand CMDD = new SQLiteCommand(text_track_list, BD);
                        CMDD.ExecuteNonQuery();
                    }
                }
                return true; // перезапись была сделана
            }
            else
            {
                // Запись в базу данных  - имени нового (создаваемого) плейлиста
                string text = "INSERT INTO `" + tablenamepl + "` (PlayList_Name) VALUES('" + playlistname + "');";
                SQLiteCommand CD = new SQLiteCommand(text, BD);
                CD.ExecuteNonQuery();

                // Запись списка треков треклиста в базу данных
                for (int i = 0; i < trackslist.Count; i++)
                {
                    string text_track_list = "INSERT INTO `" + tablenametrl + "` (PlayList_Name, Track_Name, Track_Number) VALUES('" + playlistname + "'" + "," + "'" + trackslist[i] + "'" + "," + "'" + i + "'" + ");";
                    SQLiteCommand CMD = new SQLiteCommand(text_track_list, BD);
                    CMD.ExecuteNonQuery();
                }
            }
            return true; // перезапись была сделана
        }


        /// <summary>
        /// Метод удвления из базы данных (таблиц) - имени выбранного плейлиста и списка треков
        /// </summary>
        /// <param name="tablenamepl">Имя таблицы плейлиста</param>
        /// <param name="tablenametrl">Имя таблицы треклиста</param>
        /// <param name="playlistname">Имя удаляемого плейлиста</param>
        public bool DeletePlayListsDB(string tablenamepl, string tablenametrl, string playlistname)
        {
            // проверяем - существует ли уже запись такого ИМЕНИ плейлиста в базе данных
            SQLiteCommand CM = BD.CreateCommand();
            CM.CommandText = "SELECT PlayList_Name FROM `" + tablenamepl + "` WHERE PlayList_Name = '" + playlistname + "';";
            SQLiteDataReader SQL = CM.ExecuteReader();

            if (SQL.HasRows)    // если найденa запись
            {
                // удаляем запись с  именем данного плейлиста из таблицы плейлистов
                string deletepltxt = "DELETE FROM `" + tablenamepl + "` WHERE  PlayList_Name = '" + playlistname + "';";
                SQLiteCommand CMDP = new SQLiteCommand(deletepltxt, BD);
                CMDP.ExecuteNonQuery();

                // удаляем все записи с именем данного плей листа из таблицы треков
                string deletetxt = "DELETE FROM `" + tablenametrl + "` WHERE  PlayList_Name = '" + playlistname + "';";
                SQLiteCommand CMD = new SQLiteCommand(deletetxt, BD);
                CMD.ExecuteNonQuery();

                return true; // удаление выполненно
            }
            return false; // удаление НЕ  выполнено так как нету в базе такого плй листа
        }

       #endregion
    }
}


