using Session.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace Session.Windows
{
    /// <summary>
    /// Логика взаимодействия для wEditMin.xaml
    /// </summary>
    public partial class wEditMin : Window
    {
        public wEditMin()
        {
            InitializeComponent();
        }

        // Получает ссылку на лист элементов и запоминает его для дальнейшей работы.
        private ListView lvList;
        public wEditMin(ListView lv) : this() => lvList = lv;

        // Находит самое больше "минимальное количество"
        // среди выбранных элементов и выводит в поле ввода.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double max = ((Models.Material)lvList.SelectedItem).MinCount;
            foreach (Models.Material i in lvList.SelectedItems)
                if (i.MinCount > max)
                    max = i.MinCount;
            tbValue.Text = max.ToString();
        }

        // Изменяет "минимальное количество" всех выбранных элементов
        // на значение указанное пользователем и закрывает окно.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Здесь может быть ошибка, поэтому ловим ее в try-catch
                double count = double.Parse(tbValue.Text);
                // Изменяем значения
                foreach (Models.Material i in lvList.SelectedItems)
                {
                    AppData.DB.Materials.Single(c => c.ID == i.ID).MinCount = count;
                    AppData.DB.SaveChanges();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            // Закрываем окно
            Close();
        }

        // Если был нажат Enter, вызывает Button_Click()
        private void tbValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Button_Click(sender, e);
        }
    }
}
