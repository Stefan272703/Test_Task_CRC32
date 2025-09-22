using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Test_Task
{
    public class File_JSON: INotifyPropertyChanged
    {

        public string File_Name { get; set; } // имя файла

        public string Checksum { get; set; } // контрольная сумма

        public string FilePath { get; set; } // Путь к Файлу

        public File_JSON() // конструктор по умолчанию
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // Сохранение файла JSON
        public static void Save_Json(ObservableCollection<File_JSON> files)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog(); // Открываем диалоговое окно для сохранения данных
            saveFileDialog.Filter = "*.json|*.json";
            string filename;
            if (saveFileDialog.ShowDialog() == true)
            {
                filename = saveFileDialog.FileName;
                string jsonstring = JsonConvert.SerializeObject(files, Formatting.Indented);
                File.WriteAllText(filename, jsonstring);
                MessageBox.Show("Файл сохранен");
            }
            else
            {
                MessageBox.Show("Операция сохранения файла отменена");
            }
        }

        // Импортировать существующий файл JSON
        public static ObservableCollection<File_JSON> Import_JSON()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "*.json|*.json";
                string filepath = "";
                if (openFileDialog.ShowDialog() == true)
                {
                    filepath = openFileDialog.FileName; // получаем путь к файлу
                    string json_read = File.ReadAllText(filepath); // считываем файл json

                    // получаем данные из файла json
                    ObservableCollection<File_JSON> file_JSON = JsonConvert.DeserializeObject<ObservableCollection<File_JSON>>(json_read);

                    MessageBox.Show("Файл импортирован в таблицу");

                    return file_JSON;
                }
            }
            catch
            {
                MessageBox.Show("Выбран не то файл JSON, поддерживающий данный файл-список, или данные были модцифицрованы", "Неверный файл JSON", MessageBoxButton.OK,MessageBoxImage.Warning);
                return null;
            }
            return null;
        }


    }
}
