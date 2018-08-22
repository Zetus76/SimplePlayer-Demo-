using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms;
using SimplePlayer.Source;

namespace SimplePlayer
{
    public delegate void OnTrackListLoaded( List<string> data );   // создание делегата

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Поля класса ( Sql, TrackList , Player , Playtimer, Spectrumtimer)

        /// <summary> объект для работы с базой данных( vst плагины, плей листы) </summary>
        private SQLiteBd Sql = new SQLiteBd("DB");

        /// <summary> объект треклиста </summary>
        private TrackList TrackList = new TrackList();

        /// <summary> объект прослойки между UI( MainWindow) и BassLibrary </summary>
        private Player Player = new Player();

        /// <summary> объект таймера для изменения значения ползунка шкалы времени и данных в лейбле текущей позиции трека </summary>
        private DispatcherTimer Playtimer = new DispatcherTimer();

        /// <summary> Объект создающий  значок в области уведомлений </summary>
        private NotifyIcon NotifyIcon1;    

        #endregion

        #region Инициализация 

        /// <summary>
        /// Конструктор
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            OpenDbAndCreateTables();                                    // открываем базу данных плеера и создаём программно нужные таблицы
            ShowInTaskbar = false;                                      // отображать или нет окно плеера в панели задач
            InitTray();                                                 // Инициализация трея
            SourceInitialized += Window_SourceInitialized;              // Делаем неактивной кнопку максимизации окна плеера
            InitTrackVolume();                                          // Инициализируем громкость
            InitPlayTimer();                                            // Инициализируем таймер проигрывания трека 
            TrackListBox.SelectedIndex = 0;                             // индекс ставим на первый трек в плейлисте листбокса
            LabelHz.Content = Player.hz.ToString();                     // записываем в лейбл , частоту
        }

        /// <summary>
        /// Инициализация громкости трека
        /// </summary>
        private void InitTrackVolume()
        {
            GroupBoxVolumeTrack.Header = "Volume : " + Player.Value;    // отрисовываем значение громкости в лейбле текстом
            SldrTrackVolume.Value = Player.Value;                       // выставляем значение в слайдер и отрисовываем сам ползунок слайдера
        }


        /// <summary>
        /// Инициализация таймера проигрывания трека
        /// </summary>
        public void InitPlayTimer()
        {
            Playtimer.Tick += new EventHandler(PlayTimerTick);
            Playtimer.Interval = new TimeSpan(0, 0, 1);
        }


        /// <summary>
        /// Инициализация трея
        /// </summary>
        private void InitTray()
        {
            NotifyIcon1 = new NotifyIcon(); // Create the NotifyIcon.

            // Свойство Icon устанавливает значок, который будет отображаться
            // в systray для этого приложения.
            NotifyIcon1.Icon = new Icon("Icona.ico");

            // Свойство Text задает текст, который будет отображаться,
            // в подсказке, когда мышь нависает над значком systray.
           
            NotifyIcon1.Text = "SimplePlayer (Demo)";//показываем в трее, в подсказке название нашего плеера 
            NotifyIcon1.Visible = true;

            // Handle the DoubleClick event to activate the form.
            //Обработайте событие DoubleClick, чтобы активировать форму.
            NotifyIcon1.DoubleClick += new EventHandler(notifyIcon1_DoubleClick);
        }

        #endregion

        #region Обработчики событий GUI

            #region Обработчик события - изменения состояния окна MainWindow 
        /// <summary>
        /// Обработчик события - изменения состояния окна MainWindow (при нормальном состоянии убираем это окно из таскбара)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimplePlayerwindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                ShowInTaskbar = false;                   // НЕ отображать окно плеера в панели задач
            }
        }
        #endregion

            #region Делаем неактивной кнопку максимизации окна плеера

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
        }

        #endregion

            #region Обработчик Трея 

        /// <summary>
        /// Обработка события двойного клика мышкой на иконку в трее
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            // Если окно свёрнуто, то разворачиваем его (при двойном клике по иконке в трее)
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                ShowInTaskbar = false;      // не отображать окно плеера в панели задач
                Activate();                 // Activate the form.
                return;
            }

            // Если окно развёрнуто, то сворачиваем его и  отображаем в панели задачь(при двойном клике по иконке в трее)
            if (WindowState == WindowState.Normal)
            {
                ShowInTaskbar = true;                   // отображать окно плеера в панели задач
                WindowState = WindowState.Minimized;    // сворачиваем окно плеера в таскбар
            }
        }

        #endregion

            #region Переопределяем событие закрытия окна(плеера) , добавляем скрытие иконки в трее, к закрытию приложения

        /// <summary>
        /// Переопределение события закрытия окна (плеера) по кнопке [Х] ,прячем иконку в трее
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            NotifyIcon1.Visible = false;    //прячем иконку в трее
            base.OnClosed(e);
        }

        #endregion

            #region Переопределяем событие сворачивания окна(плеера)при сворачивании разрешаем ему отображаться в taskBar  

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                if (wParam.ToInt32() == SC_MINIMIZE)
                {
                    ShowInTaskbar = true;       // отображать окно плеера в панели задач
                }
            }
            return IntPtr.Zero;
        }
        #endregion

            #region Событие происходящее по таймеру проигрывания (авто переход на следущий трек после окончания проигрывания текущего и тд и тп)

        /// <summary>
        /// Обработчик события , происходящего по таймеру проигрывания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayTimerTick(object sender, EventArgs e)
        {
            TimePosOfStream.Content = TimeSpan.FromSeconds(Player.posofstream).ToString();  // записываем в лейбл ,текущую позицию трека
            SldrTimePlay.Value = Player.posofstream;                                        // присваеваем позицию нашего трека

            // переход  на проигрывание следущего трека в трек боксе
            if (Player.gonexttrack)
            {
                if (Player.LoopTr)  //если зацикливание текущего трека включено
                {
                    ButtonPlay_Click(null, null); // проигрываем закончившийся трек снова
                    return;
                }

                int index = TrackListBox.SelectedIndex;      // получаем текущий индекс в листбоксе треклиста
                if (index != -1)                             // если выделение есть , в  треклисте  листбокса
                {
                    if (index != TrackListBox.Items.Count - 1)   // и это не конец листбокса
                    {
                        TrackListBox.SelectedIndex = index + 1;     // выделяем следующий трек в листбоксе треклиста 
                        ButtonPlay_Click(null, null);               // начинаем проигрывать новый трек.
                    }
                    else
                    {
                        if (!TrackList.Loop) //если зацикливание проигрывания треклиста отключено
                        {
                            ButtonStop_Click(null, null); // это был последний трек в листбоксе треклиста, ничего больше играть не нужно
                        }
                        else // иначе
                        {
                            TrackListBox.SelectedIndex = 0;
                            ButtonPlay_Click(null, null); //начинаем проигрывать первый трек.
                        }
                    }
                }
            }
        }

        #endregion

            #region Обработчики событий  - элементов (кнопок управления треками ("Open" ,"Play","Stop","Pause","LoopTrack ON/OFF")

        /// <summary>
        ///  Обработчик события нажатия(клика мышкой) кнопки - "Open" в плеере  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLoadTraks_Click(object sender, RoutedEventArgs e)
        {
            int index = TrackListBox.SelectedIndex; // запоминаем выделенный индекс трека в плей листе лист бокса

            TrackList.OpenFilesTracks();
            TrackListBox.Items.Clear();  // очищаем ListBox

            // перебираем все имена в цикле и заносим их в листбокс плеера
            int trackNum = 1;
            foreach (var track in TrackList.Tracks)
            {
                TrackListBox.Items.Add(trackNum.ToString() + PlayerEntries.TrackNameDelimiter + track.TrackName);
                trackNum++;
            }

            if (index == -1) // если нет выделения
            {
                TrackListBox.SelectedIndex = 0; // выделяем 1 трек в треклисте листбокса
            }
            else
            {
                TrackListBox.SelectedIndex = index; ;  // вспоминаем и устанавливаем выделенный индекс трека в треклисте листбокса
            }
            GroupBoxPlayList.Header = "Tracks : " + TrackListBox.Items.Count.ToString();    // записываем колличество треков в хидер GroupBoxPlayList
        }

        /// <summary>
        /// Обработчик события нажатия(клика мышкой) кнопки - "Play" в плеере 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            if ((TrackListBox.Items.Count != 0) && (TrackListBox.SelectedIndex != -1))
            {

                Player.PlayTrack(TrackList.Tracks[TrackListBox.SelectedIndex].Path);                        // play track
                TimePosOfStream.Content = TimeSpan.FromSeconds(Player.posofstream).ToString();              // записываем в лейбл ,текущую позицию трека
                LabelHz.Content = Player.hz.ToString();                                                     // записываем в лейбл , частоту
                GroupBoxPlaySliderTime.Header = TimeSpan.FromSeconds(Player.timeofstream).ToString();       // записываем в лейбл длительность трека
                SldrTimePlay.Maximum = Player.timeofstream;                                                 // записываем в слайдер длительность открытоого трека(музыки)
                SldrTimePlay.Value = Player.posofstream;                                                    // присваеваем позицию нашего трека (ползунок) в слайдере длительности трека
                SetColorsFormats();                                                                         // красим в нужные цвета лейбл (фоммат трека)
                Playtimer.Start();                                                                          // включаем play таймер
            }
        }

        /// <summary>
        /// Метод установки цветов текста и фона в лейбл формата трека
        /// </summary>
        private void SetColorsFormats()
        {
            string str = TrackList.Tracks[TrackListBox.SelectedIndex].Extension;

            // --- Красим фон лейблу3 в разные цвета,  в зависимости от формата ----
            LabelFormat.Foreground = System.Windows.Media.Brushes.White;    // делаем цвет текста лейбла белым
            LabelFormat.Background = System.Windows.Media.Brushes.Gray;     // делаем цвет фона лейбла серым

            if (PlayerEntries.ColorList.ContainsKey(str))
            {
                LabelFormat.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(PlayerEntries.ColorList[str]); //цвет фона лейбла              
            }

            str = "    " + str;                         // добавляем пробелы текстовой строке в начало лейбла
            LabelFormat.Content = str;                  // пишем строку в лейбл 
        }


        /// <summary>
        /// Обработчик события нажатия(клика мышкой) кнопки - " Stop" в плеере 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.playstatus == Player.PlayState.PLAYING || Player.playstatus == Player.PlayState.PAUSED) // если канал в режиме проигрывания или паузы
            {
                Player.StopTrack();
                Playtimer.Stop();                                                       // выключаем таймер
                SldrTimePlay.Value = 0;                                                 // присваеваем позицию нашего трека(ползунок) в сдайдере длительности трека
                TimePosOfStream.Content = TimeSpan.FromSeconds(0).ToString();           // записываем в лейбл ,текущую позицию трека
                GroupBoxPlaySliderTime.Header = TimeSpan.FromSeconds(0).ToString();     // записываем в лейбл длительность трека
            }
        }


        /// <summary>
        ///  Обработчик события нажатия(клика мышкой) кнопки - "Pause" в плеере
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            Player.PauseTrack();
        }

        #endregion
            #region Обработчики событий  - элементов слайдеров ( длительность трека , горомкость трека)

        /// <summary>
        /// Обработчик события изменения значения, ползунка слайдера длительности трека(перемотка мышкой)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SldrTimePlay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SldrTimePlay.IsFocused && SldrTimePlay.IsMouseOver)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed) //если нажата левая клавиша мышки
                {
                    Player.setposofscroll = (int)SldrTimePlay.Value;
                }
            }
        }

        /// <summary>
        ///  Обработчик события изменения значения, ползунка слайдера громкости трека(изменение мышкой)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SldrTrackVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                int vol = (int)Math.Floor(e.NewValue + 0.5);
                GroupBoxVolumeTrack.Header = "Volume : " + vol.ToString();  // отрисовываем значение громкости в лейбле текстом
                Player.Value = vol;
            }
            catch { }
        }

        #endregion

            #region Обработчики событий - происходящих для элементов управления трек листом (двойной клик по треку,"nextTrack","prefTrack","DownTR","UpTR", "REmoveTrack","Loop ON/OFF","ClearTL")

        /// <summary>
        /// Воспроизводим выбранный трек по двойному клику мышкой в трек листе (листбоксе)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ButtonPlay_Click(null, null); // начинаем воспроизведение выбранного трка
        }

        /// <summary>
        /// Событие по кнопке " nextTrack " Проигрывание следующего трека в трек листе 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNextTrack_Click(object sender, RoutedEventArgs e)
        {
            int index = TrackListBox.SelectedIndex;       // получаем текущий индекс в листбоксе плейлиста
            if (index != -1)                              // если выделение есть , в  треклисте  листбокса
            {
                if (index != TrackListBox.Items.Count - 1)   // и это не конец листбокса
                {
                    TrackListBox.SelectedIndex = index + 1;  // выделяем следующий трек в листбоксе треклиста 
                    ButtonPlay_Click(null, null); //начинаем проигрывать новый трек.
                }
                else
                {
                    TrackListBox.SelectedIndex = 0;  // индекс ставим на первый трек в треклиcте листбокса
                    ButtonPlay_Click(null, null);    //начинаем проигрывать первый трек в треклисте.

                }
            }
        }

        /// <summary>
        /// Событие по кнопке " prefTrack "  Проигрывание  предыдущего трека
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPrevTrack_Click(object sender, RoutedEventArgs e)
        {
            int index = TrackListBox.SelectedIndex;       // получаем текущий индекс в листбоксе треклиста
            if (index != -1)                              // если выделение есть , в  треклисте листбокса
            {
                if (index > 0)   // и это не начало треклиста листбокса
                {
                    TrackListBox.SelectedIndex = index - 1;  // выделяем следующий трек в листбоксе треклиста 
                    ButtonPlay_Click(null, null);            //начинаем проигрывать новый трек.
                }
                else
                {
                    TrackListBox.SelectedIndex = TrackListBox.Items.Count - 1;  // индекс ставим на последний трек в треклисте листбокса
                    ButtonPlay_Click(null, null);                               //начинаем проигрывать последний трек в треклисте.
                }
            }
        }


        /// <summary>
        /// Обработчик события нажатия(клика мышкой) кнопки - "DownTR" в трек листе (перемещения трека в низ на 1 позицию в треклисте)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveTrackDown_Click(object sender, RoutedEventArgs e)
        {
            // если треклист листбокса пуст , то выход
            if (TrackListBox.SelectedIndex == -1) return;

            // если в треклисте всего 1 трек, то ненужно никуда его передвигать - выход
            if (TrackListBox.Items.Count == 1) return;

            // если выбран последний трек, то ничего смещать не нужно - выход
            if (TrackListBox.SelectedIndex + 1 == TrackListBox.Items.Count) return;

            // запоминаем выделенный трек
            int index = TrackListBox.SelectedIndex;

            string str1 = (string)TrackListBox.Items[TrackListBox.SelectedIndex];
            string str2 = (string)TrackListBox.Items[TrackListBox.SelectedIndex + 1];

            int i = 0;
            for (;;)
            {
                if (str1[i] != ' ')
                {
                    str1 = str1.Remove(0, 1); // удаляем цифру если не пробел
                }
                else
                {
                    break;
                }
            }

            i = 0;
            for (;;)
            {
                if (str2[i] != ' ')
                {
                    str2 = str2.Remove(0, 1); // удаляем цифру если не пробел
                }
                else
                {
                    break;
                }
            }

            // меняем местами цифры и 2 строки в листбоксе
            int ind = TrackListBox.SelectedIndex;
            ind += 2;
            TrackListBox.Items[TrackListBox.SelectedIndex + 1] = ind.ToString() + "." + str1;
            ind--;
            TrackListBox.Items[TrackListBox.SelectedIndex] = ind.ToString() + "." + str2;

            // ---- меняем местами выбранный путь с путём который следующий  в списке(List) полных имён
            TrackList.Track path = TrackList.Tracks[index];                 // сохраняем путь выбранного трека - верхний
            TrackList.Tracks.Remove(TrackList.Tracks[index]);               // удаляем из листа путь выбранного трека -верхний
            TrackList.Tracks.Insert(index + 1, path);                       // вставляем путь верхнего трека ниже на 1 позицию в треклисте

            TrackListBox.SelectedIndex = index + 1;      // повторно выделяем трек
        }


        /// <summary>
        /// Обработчик события нажатия(клика мышкой) кнопки - "UpTR" в трек листе (перемещения трека вверх на 1 позицию в треклисте)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMoveTrackUp_Click(object sender, RoutedEventArgs e)
        {
            // если треклист листбокса пуст , то выход
            if (TrackListBox.SelectedIndex == -1) return;

            // если в треклисте всего 1 трек, то ненужно никуда его передвигать - выход
            if (TrackListBox.Items.Count == 1) return;

            // если выбран первый трек, то ничего смещать не нужно - выход
            if (TrackListBox.SelectedIndex == 0) return;

            // запоминаем выделенный трек
            int index = TrackListBox.SelectedIndex;

            string str1 = (string)TrackListBox.Items[TrackListBox.SelectedIndex];
            string str2 = (string)TrackListBox.Items[TrackListBox.SelectedIndex - 1];

            int i = 0;
            for (;;)
            {
                if (str1[i] != ' ')
                {
                    str1 = str1.Remove(0, 1); // удаляем цифру если не пробел
                }
                else
                {
                    break;
                }
            }

            i = 0;
            for (;;)
            {
                if (str2[i] != ' ')
                {
                    str2 = str2.Remove(0, 1); // удаляем цифру если не пробел
                }
                else
                {
                    break;
                }
            }

            // меняем местами цифры и 2 строки в листбоксе
            int ind = TrackListBox.SelectedIndex;
            TrackListBox.Items[TrackListBox.SelectedIndex - 1] = ind.ToString() + "." + str1;
            ind++;
            TrackListBox.Items[TrackListBox.SelectedIndex] = ind.ToString() + "." + str2;

            // меняем местами выбранный пуить с путём который предыдущий  в списке(List) полных имён
            TrackList.Track path = TrackList.Tracks[index - 1];                   // сохраняем путь выбранного трека - верхний
            TrackList.Tracks.Remove(TrackList.Tracks[index - 1]);                 // удаляем из листа путь выбранного трека -верхний
            TrackList.Tracks.Insert(index, path);                                 // вставляем путь верхнего трека ниже на 1 позицию в треклисте

            TrackListBox.SelectedIndex = index - 1;     // переводим проигрывание трека на номер выше
        }


        /// <summary>
        ///  Обработчик события нажатия кнопки - "REmoveTrack" в трек листе (удаляем трек из треклиста)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRemoveTrack_Click(object sender, RoutedEventArgs e)
        {
            // если треклист листбокса пуст , то выход
            if (TrackListBox.SelectedIndex == -1) return;

            int index = TrackListBox.SelectedIndex;                             // получаем индекс удаляемого трека

            TrackList.Tracks.RemoveAt(TrackListBox.SelectedIndex);              // удаление полного пути к треку
            TrackListBox.Items.RemoveAt(TrackListBox.SelectedIndex);            // удаление в листбоксе

            // если выбран последний трек 
            if (index == TrackListBox.Items.Count)
            {
                TrackListBox.SelectedIndex = index - 1;                         // выделяем трек
            }

            // если выбран последний трек и ещё есть треки
            else if (index == TrackListBox.Items.Count && TrackListBox.Items.Count > 1)
            {
                TrackListBox.SelectedIndex = index - 1;                         // выделяем трек который выше удаляемого
            }
            // если выбран первый трек и ещё есть треки 
            else if (index == 0 && TrackListBox.Items.Count > 1)
            {
                TrackListBox.SelectedIndex = index;                             // выделяем трек
                RenumderListboxTrack();
            }
            else
            {
                // если выбран не последний трек и ещё есть треки
                TrackListBox.SelectedIndex = index;                             // выделяем трек
                RenumderListboxTrack();
            }
        }

        /// <summary>
        /// Метод перестройки нумерации треков по порядку (1....n) в листбоксе трек листа 
        /// </summary>
        private void RenumderListboxTrack()
        {
            int tmpi = TrackListBox.SelectedIndex;      // запоминаем выделенный трек (индекс)

            for (int ind = 0; ind < TrackListBox.Items.Count; ind++)
            {
                List<String> str = TrackListBox.Items[ind].ToString().Split(new String[] { PlayerEntries.TrackNameDelimiter }, StringSplitOptions.None).ToList<String>();

                TrackListBox.Items[ind] = (ind + 1).ToString() + PlayerEntries.TrackNameDelimiter + str[1];
            }

            TrackListBox.SelectedIndex = tmpi;           // вспоминаем и выделяем его снова
        }


        /// <summary>
        ///  Обработчик события нажатия кнопки - "ClearTL"  очистка трек листа (листбокса) и листа треков (в нём хранятся полные пути к файлам треков) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearTrackList_Click(object sender, RoutedEventArgs e)
        {
            if (TrackListBox.Items.Count >  0)      // если трек лист не пуст,то очищаем его
            {
                TrackList.Tracks.Clear();            // чистим треки
                TrackListBox.Items.Clear();          // чистим полностью листбокс треклиста
            }
        }
        #endregion

        #endregion

        #region Открытие бызы данных и создание всех нужных таблиц

        public void OpenDbAndCreateTables()
        {
            Sql.OpenSimplePlayerBD();                                           // открывем базу данных
            Sql.CreateNamesPlayListTable (PlayerEntries.NameTablePlayLists);    // создаём таблицу в базе данных, для плей листов (ИМЕНА ПЛЕЙЛИСТОВ ) , если такой ещё не созданно
            Sql.CreateTracksTable (PlayerEntries.NameTableTracks);              // создаём таблицу в базе данных, для треков (ИМЯ ПЛЕЙЛИСТА и ИМЯ ТРЕКА(путь к нему) ) , если такой ещё не созданно
        }

        #endregion

        #region Обработчик события - открытия(создания) формы(окна работы с плейлистами) по нажатию на кнопку "PlayLists" UI

        /// <summary>
        ///  Обработчик события нажатия кнопки "PlayLIsts" 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPlayList_Click(object sender, RoutedEventArgs e)
        {
            List<string> tl = new List<string>();

            foreach (TrackList.Track track  in TrackList.Tracks ) // получаем список треклиста (плные пути к трекам)
            {
                tl.Add(track.Path);
            }

            PlayListsWindow PlayLIstBD = new PlayListsWindow (tl , new OnTrackListLoaded(LoadTracksOfPlayList)); // создаём объект(окно)  
        }
        #endregion

        #region Обработчик кастомноного события - мтеод  пересобирает треклист, полученый с загрузки плейлиста из базы данных 

        /// <summary>
        /// Метод обрабатывающий принятые данные (пересобирает треклист, полученый с загрузки плейлиста из базы данных ) 
        /// </summary>
        /// <param name="TrackList">Список треклиста</param>
        void LoadTracksOfPlayList(List<string> TrackList)
        {
            ButtonStop_Click(null, null);                               // останавливаем проигрывание трека,если оно запущено

            TrackListBox.Items.Clear();                                 // очищаем ListBox
            this.TrackList.Tracks.Clear();                              // очищаем треклист   
            this.TrackList.TrackCreate(TrackList);                      // запоняем новый треклист, данными загруженными с плей листа

            // перебираем все имена в цикле и заносим их в листбокс плеера
            int trackNum = 1;
            foreach (var track in this.TrackList.Tracks)
            {
                TrackListBox.Items.Add(trackNum.ToString() + PlayerEntries.TrackNameDelimiter + track.TrackName);
                trackNum++;
            }

            TrackListBox.SelectedIndex = 0; // выделяем 1 трек в плей листе листбокса
            GroupBoxPlayList.Header = "Tracks : " + TrackListBox.Items.Count.ToString();    // записываем колличество треков в хидер GroupBoxPlayList
        }
        #endregion
    }
}