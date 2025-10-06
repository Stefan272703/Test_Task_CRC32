using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Design;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Test_Task;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace TestTask
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
        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = " Все файлы *.*|*.*";
            //IsEnabled = false;

            FileInfo fileinfo = null;

            if (openFileDialog.ShowDialog() == true)
            {
                //ProgressBar progressBar = new ProgressBar();
                //ProgressBarFiles progressBarFiles = new ProgressBarFiles();


                //progressBarFiles.Show();
                //IsEnabled = false;
                foreach (var filepath in openFileDialog.FileNames) // перебираем каждый файл, выбранный пользователем в диалоговом окне
                {
                    fileinfo = new FileInfo(filepath);

                    if (Files.Select(x => x.FilePath).Contains(filepath)) // если список путей к файлу содержит выьранный пользователем путь к файлу 
                    {
                        uint crc32 = CRC32.CalculateCRC32(filepath); // рассчитываем CRC32 файла

                        foreach (var file in Files)
                        {
                            if (file.Checksum != $"{crc32:X8}" && file.FilePath == filepath)
                            {
                                var message = MessageBox.Show("Выбранный файл со схожим путем и именем есть в файле-списке, но отличаются контрольной суммой. Обновить контрольную сумму существующего файла?",
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
                            else if(file.Checksum == $"{crc32:X8}" && file.FilePath == filepath)
                            {
                                var msgbox = MessageBox.Show($"Выбранный файл {filepath} со схожими путем и именем уже есть в файле-списке, как и контрольная сумма",
                                    "Одинаковый путь и имя файла",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                        
                    }
                    else // иначе если список путей к файлу не содержит путь к файлу, выбранный пользователем, то просто добавляем файл в список
                    {
                        await AddFile(filepath);
                    }
                }
            }

            // добавление файла в список
            async Task AddFile(string filepath)
            {
                ButtonAdd.IsEnabled = false;
                ButtonDelete.IsEnabled = false;
                ButtonImport.IsEnabled = false;
                ButtonSave.IsEnabled = false;
                // порисходит рассчет CRC32 ассинхронно, чтобы приложение не зависло и пользователь по желанию, мог редактировать таблицу
                var crc32 = await Task.Run(() => CRC32.CalculateCRC32(filepath)); // рассчитываем CRC32 для определенного файла


                var check = CheckDublicatesChecksum($"{crc32:X8}"); // Проверка на совпадение контрольной суммы

                if (check.Item1) // Если есть дубликат и пользователь выбрал поменять контрольную сумму
                {
                    while (Files.Select(x => x.Checksum).Contains($"{crc32:X8}")) // пока контрольная сумма добавляемого файла не станет уникальной
                    {
                        using (var stream = new FileStream(filepath, FileMode.Append))
                        {
                            stream.WriteByte(1); // Добавляем байт 
                        }

                        crc32 = CRC32.CalculateCRC32(filepath);
                    }
                    var file = new FileJson { FileName = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    Files.Add(file); // Добавление в список информации о файле
                }
                else // иначе если нет дубликата или пользователь отказался менять контрольную сумму
                {

                    var file = new FileJson { FileName = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filepath };

                    Files.Add(file);

                }
                ButtonAdd.IsEnabled = true;
                ButtonDelete.IsEnabled = true;
                ButtonImport.IsEnabled = true;
                ButtonSave.IsEnabled = true;
            }
        }



        //Сохранить как файл JSON
        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            FileJson.SaveAsJson(Files); // вызов метода сохранения файла в формат JSON

        }

        // Импорт файла
        private async void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            ButtonAdd.IsEnabled = false;
            ButtonDelete.IsEnabled = false;
            ButtonImport.IsEnabled = false;
            ButtonSave.IsEnabled = false;
            
            var files_json = FileJson.Import_JSON(); // получаем список файлов, сохраненных в JSON
            //MessageBox.Show("KEK");
            if (files_json == null) { }
            else
            {
                foreach (var file in files_json) // Импорт файла из сохраненного файла-списка JSON
                {
                    // Добавляем каждый файл в список
                    //Thread.Sleep(10000);
                    await CheckHasOrCorrectCheckSum(file); // вызов метода для наличия или проверки на корректность контрольной суммы

                }
                MessageBox.Show("Файл импортирован в таблицу");
            }

            ButtonAdd.IsEnabled = true;
            ButtonDelete.IsEnabled = true;
            ButtonImport.IsEnabled = true;
            ButtonSave.IsEnabled = true;
        }

        //функция проверки наличия или соответветстия контрольной суммы
        private async Task CheckHasOrCorrectCheckSum(FileJson jsonfile)
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

                            //byte[] fileBytes = File.ReadAllBytes(filePath); // считываем байты файла

                            uint crc32 = await Task.Run(() => CRC32.CalculateCRC32(filePath)); // рассчет контрольной суммы файла под CRC32

                            var file = new FileJson { FileName = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = filePath };

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

                uint crc32 = await Task.Run(() => CRC32.CalculateCRC32(jsonfile.FilePath)); // рассчет контрольной суммы файла под CRC32

                if(jsonfile.Checksum != $"{crc32:X8}") // Если неккореткно рассчитана контрольная сумма
                {
                   var message = MessageBox.Show($"для данного файла {jsonfile.FilePath} отсутствует или неккоректно рассчитана контрольная сумма. Пересчитать и вставить файл в список?",
                       "Контрольная сумма",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question);

                    switch (message) // Выбор рассчитать контрольную сумму и вставить файл или не добавлять
                    {
                        case MessageBoxResult.Yes: // Добавляе файл
                            var file = new FileJson { FileName = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = jsonfile.FilePath };
                            Files.Add(file);
                            break;
                        case MessageBoxResult.No: // Не добавляем файл
                            break;
                    }
                }
                else // Иначе, если контролльная сумма рассчитана правильно
                {
                    var file = new FileJson { FileName = fileinfo.Name, Checksum = $"{crc32:X8}", FilePath = jsonfile.FilePath };
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

        // Событие завершения редактирования ячейки 
        private void FileData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) //Действие подтверждено(изменение ячейки произошло)
            {
                if (e.Column is DataGridBoundColumn column)
                {
                    var bindingPath = (column.Binding as Binding).Path.Path;

                    if (bindingPath == "FileName")
                    {
                        int rowIndex = e.Row.GetIndex(); // получаем индекст строки, в которм произошло изменение ячйеки
                        var textbox = e.EditingElement as TextBox; // получаем textbox ячейки, которая редактируется

                        string[] FileSplit = Files[rowIndex].FilePath.Split('\\').Last().Split('.');// получаем список значений разделенных точкой файла
                        string FileAndFormat = textbox.Text.Split('.').First(); // Заполняем файл с расширением

                        if (FileAndFormat.Count() == 0) 
                        { 
                            MessageBox.Show("Файл не может быть пустым");
                            textbox.Text = Files[rowIndex].FilePath.Split('\\').Last();
                        }
                        else
                        {
                            for (int i = 1; i < FileSplit.Count(); i++) // считываем каждое значение файла, кромер первого
                            {
                                FileAndFormat += '.' + FileSplit[i];
                            }

                            int indexLastSlech = Files[rowIndex].FilePath.LastIndexOf("\\"); // Получаем индекс в строке, где встречается "\"

                            if (Files[rowIndex].OldFilePath == null) // если старый путь к файлу пустой
                            {
                                Files[rowIndex].OldFilePath = Files[rowIndex].FilePath;
                            }


                            Files[rowIndex].FilePath = Files[rowIndex].FilePath.Substring(0, indexLastSlech) + "\\" + FileAndFormat;// Перезаписываем путь к файлу

                            if (Files[rowIndex].FilePath == Files[rowIndex].OldFilePath) // если совпадают путь к файлу и старый путь к файлу
                            {
                                Files[rowIndex].OldFilePath = null;
                            }

                            textbox.Text = FileAndFormat; // новое название файла с его форматом файла
                        }
                    }
                }
            }
            else if (e.EditAction == DataGridEditAction.Cancel)
            {
                int rowIndex = e.Row.GetIndex(); // получаем индекст строки, в которм произошло изменение ячйеки
                var textbox = e.EditingElement as TextBox; // получаем textbox ячейки, которая редактировалась
                textbox.Text = Files[rowIndex].FilePath.Split('\\').Last(); // получаем имя файла из пути и вставляем в FileName
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

        // функция сохранения редактированных в таблице файлов (имени и пути)
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files) // смотри каждый файл
            {
                if (file.OldFilePath != null 
                    && File.Exists(file.OldFilePath) 
                    && !File.Exists(file.FilePath)) // Если путь к файлу изменился, файл еще есть по старому пути и файл не находится по новому пути
                {
                    File.Move(file.OldFilePath, file.FilePath); // перемещаем файл в новый путь или с новым именем
                    file.OldFilePath = null; // делаем старый путь к файлу пустым
                }
            }
        }

        // событие нажатия клавишой мыши на ячейку "Путь к файлу"
        private void FileData_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = sender as DataGridCell; // ячейка
            if (cell != null) // если выбрана ячейка
            {
                // Проверяем, что это столбец "Путь файла"
                if (cell.Column is DataGridTextColumn textColumn &&
                    textColumn.Header?.ToString() == "Путь файла")
                {
                    var filePath = (cell.DataContext as FileJson)?.FilePath; // получаем путь 

                    using (var dialog = new CommonOpenFileDialog()) // Диалоговое окно выбора папки (директории)
                    {
                        dialog.IsFolderPicker = true;
                        dialog.Title = "Выберите папку";

                        if (dialog.ShowDialog() == CommonFileDialogResult.Ok) // Если пользователь выбрал новую папку для сохранения
                        {
                            string selectedPath = dialog.FileName;

                            // Получаем данные строки, к которой принадлежит ячейка
                            var rowData = cell.DataContext;
                            // Получаем индекс строки в Items коллекции DataGrid
                            var currentRowIndex = FileData.Items.IndexOf(rowData);

                            // проверка, что путь к старому файлу еще не определен
                            if (Files[currentRowIndex].OldFilePath == null) 
                            {
                                Files[currentRowIndex].OldFilePath = Files[currentRowIndex].FilePath; // устанавливаем старый путь к файлу
                            }
                            
                            // задаем новый путь к файлу
                            Files[currentRowIndex].FilePath = selectedPath + "\\" +$"{Files[currentRowIndex].FileName}";
                            
                            // Если Новый путь к файлу совпадает со старым
                            if (Files[currentRowIndex].FilePath == Files[currentRowIndex].OldFilePath)
                            {
                                Files[currentRowIndex].OldFilePath = null;
                            }

                            FileData.Items.Refresh(); // обновляем таблицу, для отображения новых данных

                        }
                    } 
                }
            }
        }

        private void FileData_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            //MessageBox.Show("Начало");
        }
    }
}
