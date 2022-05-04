using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;
using Session.Utils;

namespace Session.Windows
{
    /// <summary>
    /// Логика взаимодействия для wEdit.xaml
    /// </summary>
    public partial class wEdit : Window
    {
        // Конструктор по умолчанию, он же конструктор для создания нового материала.
        public wEdit()
        {
            InitializeComponent();

            // Подгружает список поставщиков и материалов из БД в comboBox'ы
            cbPickSupplier.ItemsSource = AppData.DB.Suppliers.ToList();
            cbMaterial.ItemsSource = AppData.DB.MaterialTypes.ToList();
        }

        // Хранит создаваемый/редактируемый материал
        private Models.Material material;

        // Конструктор для редактирования выбранного материала.
        public wEdit(Models.Material mat) : this()
        {
            // Сохраняет материал переданный аргументом
            material = mat;

            Title = "Изменить материал";
            // Привязывает событие, которое внесет данные выбранного материала в поля окна
            Loaded += wEditLoaded_Edit;
            // Активирует и отображает кнопку удаления материала
            btnRemove.Visibility = Visibility.Visible;
            btnRemove.IsEnabled = true;
        }

        // Забивает данные выбранного материала в поля окна.
        private void wEditLoaded_Edit(object sender, RoutedEventArgs e)
        {
            tbName.Text = material.Title;
            tbCountInStock.Text = material.CountInStock.ToString();
            tbUnit.Text = material.Unit;
            tbCountInPack.Text = material.CountInPack.ToString();
            tbMinCount.Text = material.MinCount.ToString();
            tbCost.Text = material.Cost.ToString();
            tbImage.Text = material.Image;
            tbDescription.Text = material.Description;

            cbMaterial.SelectedItem = material.MaterialType;

            // Добавляет в список поставщиков выбранного товара, если таковые имеются
            foreach (var i in material.Suppliers)
                lvSuppliers.Items.Add(i);
        }

        // Добавляет выбранного поставщика в список.
        private void cbPickAdd(object sender, RoutedEventArgs e)
        {
            // Если какой-либо поставщик выбран и его нет в списке, добавляет его
            if (cbPickSupplier.SelectedIndex != -1 && lvSuppliers.Items.IndexOf(cbPickSupplier.SelectedItem) == -1)
                lvSuppliers.Items.Add(cbPickSupplier.SelectedItem);
            // А затем снимает выделение с только что добавленного поставщика
            cbPickSupplier.SelectedIndex = -1;
        }

        // Удаляет выбранного поставщика из списка.
        private void cbPickRemove(object sender, RoutedEventArgs e)
        {
            // Если какой-либо поставщик выбран и он есть в списке, удаляет его
            if (cbPickSupplier.SelectedIndex != -1 && lvSuppliers.Items.IndexOf(cbPickSupplier.SelectedItem) != -1)
                lvSuppliers.Items.Remove(cbPickSupplier.SelectedItem);
            // А затем снимает выделение с только что удаленного поставщика
            cbPickSupplier.SelectedIndex = -1;
        }

        // При двойном нажатии на поставщика в списке, выбирает его.
        private void lvSuppliers_MouseDoubleClick(object sender, MouseButtonEventArgs e) => cbPickSupplier.SelectedItem = lvSuppliers.SelectedItem;

        // Открывает диалог с пользователем для выбора изображения.
        private void SelectImage(object sender, RoutedEventArgs e)
        {
            // Создает диалоговое окно
            OpenFileDialog filePath = new OpenFileDialog();
            // Если указанный в TextBox'е путь существует, открывает его
            if (string.IsNullOrWhiteSpace(tbImage.Text) || !File.Exists(tbImage.Text))
                filePath.InitialDirectory = "C:\\";
            // Иначе открывает диск C:
            else
                filePath.InitialDirectory = tbImage.Text;
            // Устанавливает фильтр по файлам
            filePath.Filter = "JPEG files (*.jpeg)|*.jpeg|All files (*.*)|*.*";

            // Если пользователь выбрал файл, вводит выбранный путь в TextBox
            if (filePath.ShowDialog() == true)
                tbImage.Text = filePath.FileName;
        }

        // Вызывается при нажатии кнопки "Сохранить".
        // Добавляет новый материал или сохраняет внесенные изменения.
        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            // Отлавливаем неправильный ввод пользователя
            try
            {
                // Если цена или "минимальное количество" меньще нуля, выбрасывает исключение
                if (decimal.Parse(tbCost.Text) < 0 || double.Parse(tbMinCount.Text) < 0)
                    throw new Exception("Цена и/или минимальное количество не могут быть отрицательными!");
                // Если окно в режиме редактирования материала
                else if (material != null)
                {
                    // Отбрасываем путь к файлу и запоминаем только его оригинальное название
                    string name = tbImage.Text.Split('\\').Last();
                    // Если путь в TextBox'е существует и отличается от предыдущего
                    if (tbImage.Text != material.Image && File.Exists(tbImage.Text))
                    {
                        // Генерируем имя для нового файла
                        do
                            name = String.Format(@"{0}.jpeg", System.IO.Path.GetRandomFileName());
                        // И если такой файл уже есть, генериум еще раз, пока не найдем подходящее
                        while (File.Exists(Directory.GetCurrentDirectory() + "\\materials\\" + name));
                        // Сохраняем выбранный файл под новым именем в папку программы
                        File.Copy(tbImage.Text, Directory.GetCurrentDirectory() + "\\materials\\" + name);
                    }

                    // Сохраняем введенные пользователем значения в объект материала
                    material.Title = tbName.Text;
                    material.CountInPack = int.Parse(tbCountInPack.Text);
                    material.Unit = tbUnit.Text;
                    material.CountInStock = double.Parse(tbCountInStock.Text);
                    material.MinCount = double.Parse(tbMinCount.Text);
                    material.Description = tbDescription.Text;
                    material.Cost = decimal.Parse(tbCost.Text);
                    // Если строка не пустая и файл существует, тогда сохраняем путь к файлу, иначе просто пустую строку
                    material.Image = !String.IsNullOrEmpty(name) && File.Exists(Directory.GetCurrentDirectory() + "\\materials\\" + name) ? "\\materials\\" + name : String.Empty;
                    // Узнаем ID выбранного материала и сохраняем его
                    material.MaterialTypeID = ((Models.MaterialType)cbMaterial.SelectedItem).ID;

                    // Очищаем список поставщиков
                    material.Suppliers.Clear();
                    // Сохраняем в объект всех выбранных пользователем поставщиков
                    foreach (Models.Supplier i in lvSuppliers.Items)
                        material.Suppliers.Add(i);

                    // Сохраняем внесенные изменения в БД
                    AppData.DB.SaveChanges();
                }
                // Если окно в режиме добавления материала
                else
                {
                    // Здесь будет (или не будет) храниться оригинальное имя файла
                    string name = null;
                    // Если введенный пользователем путь существует
                    if (File.Exists(tbImage.Text))
                    {
                        // Генерируем имя для нового файла
                        do
                            name = String.Format(@"{0}.jpeg", System.IO.Path.GetRandomFileName());
                        // И если такой файл уже есть, генериум еще раз, пока не найдем подходящее
                        while (File.Exists(Directory.GetCurrentDirectory() + "\\materials\\" + name));
                        // Сохраняем выбранный файл под новым именем в папку программы
                        File.Copy(tbImage.Text, Directory.GetCurrentDirectory() + "\\materials\\" + name);
                    }

                    // Создаем новый материал с данными, введенными пользователем
                    material = new Models.Material()
                    {
                        Title = tbName.Text,
                        CountInPack = int.Parse(tbCountInPack.Text),
                        Unit = tbUnit.Text,
                        CountInStock = double.Parse(tbCountInStock.Text),
                        MinCount = double.Parse(tbMinCount.Text),
                        Description = tbDescription.Text,
                        Cost = decimal.Parse(tbCost.Text),
                        // Если имя файла пустое, тогда сохраняем пустую строку, иначе путь к файлу
                        Image = String.IsNullOrEmpty(name) ? String.Empty : "\\materials\\" + name,
                        // Узнаем ID выбранного материала и сохраняем его
                        MaterialTypeID = ((Models.MaterialType)cbMaterial.SelectedItem).ID
                    };

                    // Сохраняем в объект всех выбранных пользователем поставщиков
                    foreach (Models.Supplier i in lvSuppliers.Items)
                        material.Suppliers.Add(i);

                    // Вносим наш только что созданный материал в БД
                    AppData.DB.Materials.Add(material);
                    // Сохраняем внесенные изменения в БД
                    AppData.DB.SaveChanges();
                }

                Close();
            }
            // Выводим пользователю все пойманные ранее ошибки
            catch (Exception ex) { MessageBox.Show(ex.Message + "\n" + ex.InnerException?.InnerException.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // Удаляет выбранный материал.
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            // Если из данного материала производят какой-либо продукт, выводит ошибку и ничего не делает
            if (material.ProductMaterials.Count != 0)
            {
                MessageBox.Show("Данный материал используется для производства одного или нескольких продуктов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Очищает поставщиков данного материала
            material.Suppliers.Clear();
            // Очищает историю изменения кол-ва данного материала
            material.MaterialCountHistories.Clear();
            // Удаляет выбранный материал
            AppData.DB.Materials.Remove(material);
            // Сохраняет изменения в БД
            AppData.DB.SaveChanges();

            Close();
        }
    }
}
