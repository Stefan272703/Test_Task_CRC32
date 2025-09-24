using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Test_Task
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<FileJson> Files = new ObservableCollection<FileJson>();
        public MainWindow()
        {
            InitializeComponent();
            FileData.ItemsSource = Files;
        }



        // Функция добавления файла в список
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = " Все файлы *.*|*.*";

            string filepath = "";
            FileInfo fileinfo = null;
            if (openFileDialog.ShowDialog() == true)
            {
                filepath = openFileDialog.FileName;
                fileinfo = new FileInfo(filepath);


                if (Files.Select(x => x.FilePath).Contains(filepath)) // если список путей к файлу содержит выьранный пользователем путь к файлу 
                {
                    byte[] fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                    uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчитываем CRC32 файла

                    foreach (var file in Files)
                    {
                        if (file.Checksum != $"{crc32:X8}" && file.FilePath == filepath)
                        {
                            var message = MessageBox.Show("Выбранный файл со схожим путем и именем есть в файле-списе, но отличаются контрольной суммой. Обновить контрольную сумму существующего файла?",
                                "Одинаковый путь и имя файла",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);
                            switch (message)
                            {
                                case MessageBoxResult.Yes:
                                    Files[Files.IndexOf(file)].Checksum = $"{crc32:X8}";
                                    FileData.Items.Refresh(); // Обновляем таблицу, для отображения новго результата
                                    break;
                                case MessageBoxResult.No:
                                    break;
                            }

                        }
                    }

                    var msgbox = MessageBox.Show("Выбранный файл со схожими путем и именем уже есть в файле-списке, как и контрольная сумма",
                        "Одинаковый путь и имя файла",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else // иначе если список путей к файлу не содержит путь к файлу, выбранный пользователем, то просто добавляем файл в список
                {
                    AddFile();
                }
            }

            // добавление файла в список
            void AddFile()
            {
                byte[] fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                
                var check = CheckDublicatesChecksum($"{crc32:X8}"); // Проверка на совпадение контрольной суммы

                if (check.Item1) // Если есть дубликат и пользователь выбрал поменять контрольную сумму
                {
                    while (Files.Select(x => x.Checksum).Contains($"{crc32:X8}")) // пока контрольная сумма добавляемого файла не станет уникальной
                    {
                        using (var stream = new FileStream(filepath, FileMode.Append))
                        {
                            stream.WriteByte(1); // Добавляем байт 
                        }

                        fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                        crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32
                    }
                    var file = new FileJson { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    Files.Add(file); // Добавление в список информации о файле
                }
                else // иначе если  нет дубликата или пользователь отказался менять контрольную сумму
                {
                    crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                    var file = new FileJson { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    Files.Add(file);
                }
            }
        }



        //Сохранение файла
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            FileJson.SaveJson(Files); // вызов метода сохранения файла в формат JSON

        }

        // Импорт файла
        private void ImportFile(object sender, RoutedEventArgs e)
        {
            var files_json = FileJson.Import_JSON(); // получаем список файлов, сохраненных в JSON
            if (files_json == null) { }
            else
            {
                foreach (var file in files_json) // Импорт файла из сохраненного файла-списка JSON
                {
                    // Добавляем каждый файл в список
                    CheckHasOrCorrectCheckSum(file); // вызов метода для наличия или проверки на корректность контрольной суммы

                }
                MessageBox.Show("Файл импортирован в таблицу");
            }
        }

        //функция проверки наличия или соответветстия контрольной суммы
        private void CheckHasOrCorrectCheckSum(FileJson jsonfile)
        {
            bool file_exists = File.Exists(jsonfile.FilePath);

            if (jsonfile.FilePath == string.Empty || !file_exists) // если путь к файлу пустой или неккоректен
            {
                var msg = MessageBox.Show($"отсутствует или неправильно введен путь к файлу {jsonfile.FilePath}. Добавить/изменить его и вставить в файл-список?", "Некоректный путь к файлу", MessageBoxButton.YesNo, MessageBoxImage.Question);

                switch (msg) // Выбор добавить/изменить файл или не выбрать
                {
                    case MessageBoxResult.Yes:

                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = " Все файлы *.*|*.*";


                        if (openFileDialog.ShowDialog() == true)
                        {
                            string filePath = openFileDialog.FileName;
                            var fileinfo = new FileInfo(filePath);

                            byte[] fileBytes = File.ReadAllBytes(filePath); // считываем байты файла

                            uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                            var file = new FileJson { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filePath };

                            Files.Add(file);

                        }
                        else
                        {
                            MessageBox.Show("Не выбран файл, Нажмите \"Ок\" для дальнейшего импорта файлов ","Не выбран файл", MessageBoxButton.OK);
                        }

                            break;
                     case MessageBoxResult.No:

                        break;
                }
            }
            else
            {
                var fileinfo = new FileInfo(jsonfile.FilePath); // информация о файле

                byte[] fileBytes = File.ReadAllBytes(jsonfile.FilePath); // считываем байты файла

                uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                if(jsonfile.Checksum != $"{crc32:X8}") // Если неккореткно рассчитана контрольная сумма
                {
                   var message = MessageBox.Show($"для данного файла {jsonfile.FilePath} отсутствует или неккоректно рассчитана контрольная сумма. Пересчитать и вставить файл в список?",
                       "Контрольная сумма",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question);

                    switch (message) // Выбор рассчитать контрольную сумму и вставить файл или не добавлять
                    {
                        case MessageBoxResult.Yes: // Добавляе файл
                            var file = new FileJson { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = jsonfile.FilePath };
                            Files.Add(file);
                            break;
                        case MessageBoxResult.No: // Не добавляем файл
                            break;
                    }
                }
                else // Иначе, если контролльная сумма рассчитана правильно
                {
                    var file = new FileJson { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = jsonfile.FilePath };
                    Files.Add(file);
                }

            }
        }

        // функция проверки на дубликаты контрольные суммы
        (bool, int) CheckDublicatesChecksum(string cur_checksum)
        {
            Files.Select(x => x.Checksum); // Выбираем только контрольные суммы
            if (Files.Select(x => x.Checksum).Contains(cur_checksum)) // если файл-список содержит уже контрольную сумму
            {
                var msg = MessageBox.Show("Контрольная сумма файла уже существует в файле-списке\n Изменить ее и добавить файл (Да)\n Просто добавить файл без изменений (Нет)?",
                    "Совпадение контрольных сумм",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                switch (msg) // Выбор изменять не изменять контрольную сумму
                {
                    case MessageBoxResult.Yes: // Возвращаем истину
                        return (true, Files.Select(x => x.Checksum).Where(x => x == cur_checksum).Count()); // вовзращаем истину и количество изменений
                    case MessageBoxResult.No: // возвращаем ложь
                        return (false, 0);
                }
            }
            return (false, 0);
        }


        private void FileData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction == DataGridEditAction.Commit) //Действие подтверждено(изменение ячейки произошло)
            {
                if (e.Column is DataGridBoundColumn column)
                {
                    var bindingPath = (column.Binding as Binding).Path.Path;

                    if (bindingPath == "File_Name")
                    {
                        CommitChange();
                    }
                    else if (bindingPath == "Checksum")
                    {
                        CommitChange();
                    }
                    else if (bindingPath == "FilePath")
                    {
                        CommitChange();
                    }
                }
            }
            void CommitChange()
            {
                int rowIndex = e.Row.GetIndex();
                var textbox = e.EditingElement as TextBox;

                var text = textbox.Text;

                if (rowIndex >= 0 && rowIndex < Files.Count)
                {
                    var item = Files[rowIndex];

                    item.FilePath = text;
                    Files[rowIndex] = item;
                }
            }
        }

        // Функция удаления строки/строк при нажатии на кнопку "Удалить"
        private void DeleteRow(object sender, RoutedEventArgs e)
        {
            var selectedFiles = FileData.SelectedItems.Cast<FileJson>().ToList();

            if(selectedFiles.Count == 0)
            {

                MessageBox.Show("Не выбрана(ы) строка(и) для удаления", "Удаление строки", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var file in selectedFiles) // выбираем каждую выделенную строку для удаления файла из списка
            {
                if (Files.Contains(file))
                {
                    Files.Remove(file);
                }
            }
        }
    }
}
