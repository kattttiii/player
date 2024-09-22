using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AudioPlayer2
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer; // Медиа плеер для воспроизведения аудио
        private List<string> audioFilePaths; // Список путей к аудиофайлам
        private int currentAudioIndex; // Индекс текущего воспроизводимого аудиофайла
        private bool isPlaying = false; // для отслеживания состояния проигрывания
        private bool isRepeatEnabled = false; // для отслеживания активации повтора проигрывания
        private bool isShuffleEnabled = false; // для отслеживания активации случайного воспроизведения
        private DispatcherTimer timer; // Таймер для обновления информации о проигрываемом аудио

        public MainWindow()
        {
            InitializeComponent();

            mediaPlayer = new MediaPlayer();
            audioFilePaths = new List<string>();
            currentAudioIndex = 0; //начальный индекс для уадиофайла

            mediaPlayer.MediaEnded += new EventHandler(Player_MediaEnded);
            //таймер
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); //интервал
            timer.Tick += Timer_Tick;
        }


        //таймер
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                sliderPosition.Maximum = mediaPlayer.NaturalDuration.HasTimeSpan ? mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;

                TimeSpan currentTime = mediaPlayer.Position; //текущая позиция во времени
                TimeSpan totalTime;
                try
                {
                    totalTime = mediaPlayer.NaturalDuration.TimeSpan;
                }
                catch (InvalidOperationException)
                {
                    Thread.Sleep(5000); // Ожидаем 5 секунд, чтобы избежать ошибки при получении общего времени аудиофайла
                    totalTime = mediaPlayer.NaturalDuration.TimeSpan;
                    lblCurrentTime.Content = string.Format("{0:mm\\:ss}", totalTime);// Обновляем отображаемое текущее время
                    lblRemainingTime.Content = string.Format("{0:mm\\:ss}", totalTime - currentTime);// Обновляем отображаемое оставшееся время
                    sliderPosition.Value = mediaPlayer.Position.TotalSeconds;
                }
                lblCurrentTime.Content = string.Format("{0:mm\\:ss}", totalTime);
                lblRemainingTime.Content = string.Format("{0:mm\\:ss}", totalTime - currentTime);
                sliderPosition.Value = mediaPlayer.Position.TotalSeconds;
            }
        }
        // Обработчик события нажатия на кнопку выбора папки с аудиофайлами
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog browser = new CommonOpenFileDialog { IsFolderPicker = true };// Создаем диалоговое окно выбора папки
            CommonFileDialogResult result = browser.ShowDialog();
            // Если папка выбрана
            if (result == CommonFileDialogResult.Ok)
            {
                string[] validExtensions = { ".mp3", ".m4a", ".wav" };
                audioFilePaths = Directory.GetFiles(browser.FileName).Where(file => validExtensions.Contains(Path.GetExtension(file))).ToList();
                // Получаем список аудиофайлов в выбранной папке с валидными расширениями (это те что выше)
                if (audioFilePaths.Count > 0)
                {
                    OpenAudioFile(0);
                }
                else
                {
                    MessageBox.Show("В выбранной папке нет аудио файлов.");
                }
            }
        }

        // окончание воспроизведения аудиофайла
        private void Player_MediaEnded(object sender, EventArgs e)
        {
            // Если активирован повтор или есть следующий аудиофайл в списке, переходим к следующему аудиофайлу
            if (isRepeatEnabled)
                if (isRepeatEnabled)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(0);
                mediaPlayer.Position = newPosition;
            }
            else if (currentAudioIndex < audioFilePaths.Count - 1)
            {
                BtnNext_Click(null, null);
            }
            else
            {
                currentAudioIndex = 0;
                OpenAudioFile(0);
            }
        }

        // Метод для открытия аудиофайла по индексу в списке
        private void OpenAudioFile(int index)
        {
            if (audioFilePaths.Count > index)
            {
                currentAudioIndex = index;
                string audioFilePath = audioFilePaths[currentAudioIndex];

                mediaPlayer.Open(new Uri(audioFilePath));
                mediaPlayer.Play();

                isPlaying = true;
                btnPlayPause.Content = "В";

                lblCurrentAudioFile.Content = Path.GetFileName(audioFilePath);

                timer.Start();
            }
        }
        
        //как работает пауза/воспроизв.
        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                mediaPlayer.Pause();
                isPlaying = false;
                btnPlayPause.Content = "В"; //пометки чтобы не забыть и отображалось в плеере
            }
            else
            {
                mediaPlayer.Play();
                isPlaying = true;
                btnPlayPause.Content = "П";
            }
        }

        //чтобы перемешывать
        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            ShuffleAudioFiles();

            if (isShuffleEnabled)
            {
                btnShuffle.Content = "ON";
            }
            else
            {
                btnShuffle.Content = "OFF";
            }
        }

        //чтобы откатить
        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleEnabled)
            {
                ShuffleAudioFiles();
            }
            else
            {
                if (currentAudioIndex > 0)
                {
                    OpenAudioFile(currentAudioIndex - 1);
                }
            }
        }

        //след файл
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleEnabled)
            {
                ShuffleAudioFiles();
            }
            else
            {
                if (currentAudioIndex < audioFilePaths.Count - 1)
                {
                    OpenAudioFile(currentAudioIndex + 1);
                }
            }
        }

        //повтор песни
        private void BtnRepeat_Click(object sender, RoutedEventArgs e)
        {
            Repeat();

            if (isRepeatEnabled)
            {
                btnRepeat.Content = "ON";
            }
            else
            {
                btnRepeat.Content = "OFF";
            }
        }

        //перемешка
        private void ShuffleAudioFiles()
        {
            isShuffleEnabled = !isShuffleEnabled;

            if (isShuffleEnabled)
            {
                List<string> shuffledAudioFilePaths = new List<string>(audioFilePaths);
                shuffledAudioFilePaths.Shuffle();

                audioFilePaths = shuffledAudioFilePaths;
                currentAudioIndex = 0;
            }
            else
            {
                audioFilePaths.Sort();
                currentAudioIndex = audioFilePaths.IndexOf(mediaPlayer.Source?.LocalPath);
            }

            OpenAudioFile(currentAudioIndex);
        }
       
        //повтор трека
        private void Repeat()
        {
            isRepeatEnabled = !isRepeatEnabled;
        }
        private void SliderPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(sliderPosition.Value);
                mediaPlayer.Position = newPosition;
            }
        }


        //громкость звука
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new HistoryWindow(audioFilePaths, currentAudioIndex);
            historyWindow.ShowDialog();

            if (historyWindow.DialogResult == true)
            {
                int selectedAudioIndex = historyWindow.SelectedAudioIndex;
                OpenAudioFile(selectedAudioIndex);
            }
        }
    }
    // Расширение для списка, позволяющее перемешивать его элементы
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random random = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int newIndex = random.Next(i + 1);
                T value = list[i];

                list[i] = list[newIndex];
                list[newIndex] = value;
            }
        }
    }
}
