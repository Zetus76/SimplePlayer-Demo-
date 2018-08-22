using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Runtime.InteropServices;
using System.Windows.Interop;

using System.Windows.Forms;

using SimplePlayer.Source;

namespace SimplePlayer
{
    /// <summary>
    /// Логика взаимодействия для PlayListsWindow.xaml
    /// </summary>
    public partial class PlayListsWindow : Window
    {
        List<string> trackslist;
        private OnTrackListLoaded d;

        #region Инициализация (конструктор)

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="mode"></param>
        public PlayListsWindow (List<string> tl , OnTrackListLoaded sender)
        {
            trackslist = tl;                                            // получаем список трек листа (полные пути к трекам)
            d = sender;
            InitializeComponent();
            SourceInitialized += Window_SourceInitialized;              // Делаем неактивной кнопку минимизации окна
            ReadPlayLists();                                            // Считываем имена всех плейлистов из базы данных  
            ShowDialog();                                               // показываем окно (открываем как модальное)
        }

        #endregion
        #region Делаем неактивной кнопку минимизации окна 

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x20000;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd_m = new WindowInteropHelper((Window)sender).Handle;
            var value_m = GetWindowLong(hwnd_m, GWL_STYLE);
            SetWindowLong(hwnd_m, GWL_STYLE, (int)(value_m & ~WS_MINIMIZEBOX));
        }

        #endregion

        #region Метод считывания списка имен всех плейлистов , хранящихся в базе данных и их добавления в листбокс

        /// <summary>
        /// Считываем список имен всех плейлистов , хранящихся в базе данных и добавляем их в листбокс
        /// </summary>
        private void ReadPlayLists()
        {
            SQLiteBd db = SQLiteBd.GetDbHandle("DB");
            List<string> playlistsnames = db.LoadPlayListsDB(PlayerEntries.NameTablePlayLists);

            foreach (string name in playlistsnames)
            {
                PlayListsBox.Items.Add(name);
            }
        }
        #endregion

        #region Обработчики событий кнопок ( Save , Delete , Load , double click)

        /// <summary>
        /// Обработчик события  - при нажатии на кнопку "Save" (запись выбранного плейлиста с треками в таблицы базы данных)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            bool result = false; // была ли сделана запись или перезапись плейлиста

            // если треклист пуст, то ненадо записывать никаких данных
            if (trackslist.Count == 0)
            {
                ShowMessError("EMPTY_TRACK_LIST");
                return;
            }

            // если пользователь не ввёл имя нового плей листа перед записью его в базу данных, то ненадо записывать никаких данных
            if (PlayListName.Text == String.Empty)
            {
                ShowMessError("EMPTY_NAME_PLAYLIST");
                return;
            }

            SQLiteBd db = SQLiteBd.GetDbHandle("DB");
            result = db.SavePlayListsDB (PlayerEntries.NameTablePlayLists, PlayerEntries.NameTableTracks, trackslist, PlayListName.Text);

            if(result) Close(); //закрываем это окно если запись была сделана
        }


        /// <summary>
        /// Обработчик события -  при нажатии на кнопку "Delete" (удаление плейлиста с треками из таблиц базы данных и из тексбокса)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            bool result = false; // выполнено удаление или нет 

            if (PlayListName.Text == string.Empty )   // если имя плейлиста пустое, то не нужно ничего удалять - выход
            {
                ShowMessError("NO_SELECT_PLAYLIST"); 
                return;
            }

            if(PlayListsBox.Items.Count == 0) //если листбокс  пуст
            {
                ShowMessError("PLAYLIST_NOT_FOUND");
                return;
            }
            else 
            {
                SQLiteBd db = SQLiteBd.GetDbHandle("DB");
                result = db.DeletePlayListsDB (PlayerEntries.NameTablePlayLists, PlayerEntries.NameTableTracks, PlayListName.Text);   // удаляем данные из таблиц базы данных
                if(result)             
                {
                   for(int i = 0; i < PlayListsBox.Items.Count; i++) //ищем имя плелиста введёное пользователем в элемент PlayListName ,если находим совпадение в листбоксе , то удаляем его из листбокса
                    {
                        if(PlayListsBox.Items[i].ToString() == PlayListName.Text)
                        {
                            PlayListsBox.Items.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    ShowMessError("PLAYLIST_NOT_FOUND");                    // значит пользователем было введено имя несуществуещего плей листа
                }
            }
        }


        /// <summary>
        /// Обработчик события -  при нажатии на кнопку "Load" (загрузка выбранного плейлиста(треков находящихся в нём))
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListName.Text == string.Empty)   // если имя плейлиста пустое, то не нужно ничего загружать - выход
            {
                ShowMessError("NO_SELECT_PLAYLIST");
                return;
            }

            List<string> tracklist = new List<string>();

            SQLiteBd db = SQLiteBd.GetDbHandle("DB");
            if (PlayListsBox.Items.Count != 0) tracklist = db.LoadTrackListDB(PlayListName.Text, PlayerEntries.NameTableTracks); //если листбокс не пуст

            if (tracklist.Count != 0) // если трек лист не пуст (загружен)
            {
                d(tracklist); // вызываем постройку  треклиста с треами полученными из загруженного плейлиста 
                Close(); // закрываем это окно
            }
            else
            {
                ShowMessError("PLAYLIST_NOT_FOUND"); // значит пользователем было введено имя несуществуещего плей листа
            }
        }

        /// <summary>
        /// Обработчик события - doubleClick = загрузка выбранного из листбокса плей листа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayListsBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnLoad_Click(null, null);
        }

        #endregion

        #region Обработчик события  - изменение значения в листбоксе (происходит копирование выделенной строки из листбокса в текстбокс)

        /// <summary>
        /// Обработчик события происходящeго при выделении в листбоксе имени плйелиста (имя плйлиста копируется в текстбокс)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayListsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(PlayListsBox.SelectedItem != null) PlayListName.Text = PlayListsBox.SelectedItem.ToString();
        }

        #endregion

        #region Метод - показа предупреждений при неверных действиях пользователя

        /// <summary>
        /// Метод показа сообщений пользователю , о его некорректных действиях
        /// </summary>
        /// <param name="error"></param>
        private void ShowMessError(string error)
        {
            string message = "";

            // Configure message box
            if (error == "EMPTY_TRACK_LIST")    message =   "Track list is empty";
            if (error == "EMPTY_NAME_PLAYLIST") message =   "No specifed name of playlist";
            if (error == "NO_SELECT_PLAYLIST")  message =   "First select the playlist";
            if (error == "PLAYLIST_NOT_FOUND")  message =   "Playlist not found";

            string caption = "Attention!";
            MessageBoxButton buttons = MessageBoxButton.OK;

            // Show message box
            MessageBoxResult result = System.Windows.MessageBox.Show(message, caption, buttons);
        }

        #endregion
    }
}


