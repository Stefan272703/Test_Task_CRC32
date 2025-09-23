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
        public ObservableCollection<File_JSON> files = new ObservableCollection<File_JSON>();
        public MainWindow()
        {
            InitializeComponent();
            FileData.ItemsSource = files;
        }



        // Функция добавления файла в список
        private void Add_File_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = " Все файлы *.*|*.*";

            string filepath = "";
            FileInfo fileinfo = null;
            if (openFileDialog.ShowDialog() == true)
            {
                filepath = openFileDialog.FileName;
                fileinfo = new FileInfo(filepath);
                if (files.Select(x => x.FilePath).Contains(filepath)) // если список путей к файлу содержит выьранный пользователем путь к файлу 
                {
                    //var msgbox = MessageBox.Show("Выбранный файл со схожими путем и именем уже есть в файле-списке", "Одинаковый путь и имя файла", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    byte[] fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                    uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32
                    foreach (var file in files) // смотри каждый файл в файле-списке
                    {
                        if(file.Checksum != $"{crc32:X8}" && file.FilePath == filepath)
                        {
                            var msgbox = MessageBox.Show("Выбранный файл со схожими путем и именем уже есть в файле-списке, но отличаются контрольной суммой" +
                                "\n Обновить контрольную сумму существующего файла?", 
                                "Одинаковый путь и имя файла", 
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Question);

                            switch (msgbox)
                            {
                                case MessageBoxResult.Yes: // Если да, то изменяем у существующего файла индекс
                                    files[files.IndexOf(file)].Checksum = $"{crc32:X8}"; // находим индекс, по которому поменяем значение контрольной суммы
                                    FileData.Items.Refresh(); // обновляем таблицу файла-списка

                                    break;
                                case MessageBoxResult.No: // Если нет, то ничего не изменяем и не добавляем
                                    break;
                            }
                            break;
                        }
                        else if (file.Checksum == $"{crc32:X8}" && file.FilePath == filepath)
                        {
                            MessageBox.Show("Выбранный файл со схожими путем и именем уже есть в файле-списке",
                                "Одинаковый путь и имя файла",
                                MessageBoxButton.OK,
                                MessageBoxImage.Question);
                        }
                    }

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

                
                var check = Check_dublicates_Checksum($"{crc32:X8}"); // Проверка на совпадение контрольной суммы

                if (check.Item1) // Если есть дубликат и пользователь выбрал поменять контрольную сумму
                {
                    while (files.Select(x => x.Checksum).Contains($"{crc32:X8}")) // пока контрольная сумма добавляемого файла не станет уникальной
                    {
                        using (var stream = new FileStream(filepath, FileMode.Append))
                        {
                            stream.WriteByte(1); // Добавляем байт 
                        }

                        fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                        crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32
                    }
                    var file = new File_JSON { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    files.Add(file); // Добавление в список информации о файле
                }
                else // иначе если  нет дубликата или пользователь отказался менять контрольную сумму
                {
                    crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                    var file = new File_JSON { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    files.Add(file);
                }
            }
        }



        //Сохранение файла
        private void Save_File(object sender, RoutedEventArgs e)
        {
            File_JSON.Save_Json(files); // вызов метода сохранения файла в формат JSON

        }

        // Импорт файла
        private void Import_File(object sender, RoutedEventArgs e)
        {
            var files_json = File_JSON.Import_JSON(); // получаем список файлов, сохраненных в JSON
            if (files_json == null) { }
            else
            {
                //files.CollectionChanged += File_JSON_CollectionChanged; // Подписка на событие, если происходит добавление файла в список
                foreach (var file in files_json) // Импорт файла из сохраненного файла-списка JSON
                {
                    // Добавляем каждый файл в список
                    Check_Has_Or_Correct_CheckSum(file); // вызов метода для наличия или проверки на корректность контрольной суммы

                }
                //files.CollectionChanged -= File_JSON_CollectionChanged; // Отписка на соыбтие, когда закончили добавлять файлы
            }
        }

        // Вызов события при любых изменениях с коллекцией (файлом-списком)
        /*private void File_JSON_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
            }
        }*/

        //функция проверки наличия или соответветстия контрольной суммы
        private void Check_Has_Or_Correct_CheckSum(File_JSON json_file)
        {
            bool file_exists = File.Exists(json_file.FilePath);

            if (json_file.FilePath == string.Empty || !file_exists) // если путь к файлу пустой или неккоректен
            {
                var msg = MessageBox.Show($"отсутствует или неправильно введен путь к файлу {json_file.FilePath}. Добавить/изменить его и вставить в файл-список?", "Некоректный путь к файлу", MessageBoxButton.YesNo, MessageBoxImage.Question);

                switch (msg) // Выбор добавить/изменить файл или не выбрать
                {
                    case MessageBoxResult.Yes:

                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = " Все файлы *.*|*.*";


                        if (openFileDialog.ShowDialog() == true)
                        {
                            string filepath = openFileDialog.FileName;
                            var fileinfo = new FileInfo(filepath);

                            byte[] fileBytes = File.ReadAllBytes(filepath); // считываем байты файла

                            uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                            var file = new File_JSON { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                            files.Add(file);

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
                var fileinfo = new FileInfo(json_file.FilePath); // информация о файле

                byte[] fileBytes = File.ReadAllBytes(json_file.FilePath); // считываем байты файла

                uint crc32 = CRC32.CalculateCRC32(fileBytes); // рассчет контрольной суммы файла под CRC32

                if(json_file.Checksum != $"{crc32:X8}") // Если неккореткно рассчитана контрольная сумма
                {
                   var msg = MessageBox.Show($"для данного файла {json_file.FilePath} отсутствует или неккоректно рассчитана контрольная сумма. Пересчитать и вставить файл в список?",
                       "Контрольная сумма",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question);

                    switch (msg) // Выбор рассчитать контрольную сумму и вставить файл или не добавлять
                    {
                        case MessageBoxResult.Yes: // Добавляе файл
                            var file = new File_JSON { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = json_file.FilePath };
                            files.Add(file);
                            break;
                        case MessageBoxResult.No: // Не добавляем файл
                            break;
                    }
                }
                else // Иначе, если контролльная сумма рассчитана правильно
                {
                    var file = new File_JSON { File_Name = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = json_file.FilePath };
                    files.Add(file);
                }

            }
        }

        // функция проверки на дубликаты контрольные суммы
        (bool, int) Check_dublicates_Checksum(string cur_checksum)
        {
            files.Select(x => x.Checksum); // Выбираем только контрольные суммы
            if (files.Select(x => x.Checksum).Contains(cur_checksum)) // если файл-список содержит уже контрольную сумму
            {
                var msg = MessageBox.Show("Контрольная сумма файла уже существует в файле-списке\n Изменить ее и добавить файл (Да)\n Просто добавить файл без изменений (Нет)?",
                    "Совпадение контрольных сумм",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                switch (msg) // Выбор изменять не изменять контрольную сумму
                {
                    case MessageBoxResult.Yes: // Возвращаем истину
                        return (true, files.Select(x => x.Checksum).Where(x => x == cur_checksum).Count()); // вовзращаем истину и количество изменений
                    case MessageBoxResult.No: // возвращаем ложь
                        //MessageBox.Show("Добавлен файл без изменения контрольной суммы", "Добавление", MessageBoxButton.OK, MessageBoxImage.Information);
                        return (false, 0);
                }
            }
            return (false, 0);
        }


        private void FileData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction == DataGridEditAction.Commit) //Действие подтверждено(изменение ячейки произошло)
            {
                //files.CollectionChanged += File_JSON_CollectionChanged;
                if (e.Column is DataGridBoundColumn column)
                {
                    var bindingPath = (column.Binding as Binding).Path.Path;

                    if (bindingPath == "File_Name")
                    {
                        Commit_Change();
                    }
                    else if (bindingPath == "Checksum")
                    {
                        Commit_Change();
                    }
                    else if (bindingPath == "FilePath")
                    {
                        Commit_Change();
                    }
                }
                //files.CollectionChanged -= File_JSON_CollectionChanged;
            }
            void Commit_Change()
            {
                int rowIndex = e.Row.GetIndex();
                var textbox = e.EditingElement as TextBox;

                var text = textbox.Text;

                if (rowIndex >= 0 && rowIndex < files.Count)
                {
                    var item = files[rowIndex];

                    item.FilePath = text;
                    files[rowIndex] = item;
                }
            }
        }

        // Функция удаления строки/строк при нажатии на кнопку "Удалить "
        private void Delete_Row(object sender, RoutedEventArgs e)
        {
            var selectedFiles = FileData.SelectedItems.Cast<File_JSON>().ToList();

            if(selectedFiles.Count == 0)
            {

                MessageBox.Show("Не выбрана(ы) строка(и) для удаления", "Удаление строки", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //files.CollectionChanged += File_JSON_CollectionChanged;
            foreach (var file in selectedFiles) // выбираем каждую выделенную строку для удаления файла из списка
            {
                if (files.Contains(file))
                {
                    files.Remove(file);
                }
            }
            //files.CollectionChanged -= File_JSON_CollectionChanged;
        }
    }
}
