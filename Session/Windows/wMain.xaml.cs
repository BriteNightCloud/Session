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
using Session.Utils;
using Session.Models;
using lvDataDLL;

namespace Session.Windows
{
    /// <summary>
    /// Логика взаимодействия для wMain.xaml
    /// </summary>
    public partial class wMain : Window
    {
        public wMain()
        {
            InitializeComponent();
        }

        // Глобальные константы, переменные, свойства, перечисления и т.п.
        #region Global Variables

        // Задает кол-во материалов отображаемых на одной странице
        private const int ITEMS_ON_PAGE = 15;

        // Количество страниц слева и справа от текущей
        // в блоке перехода по страницам
        private const int PAGES_RANGE = 2;

        // Сколько всего страниц на данный момент
        private int totalPages;

        // Текущая страница. Не меньше 0 и не больше текущего кол-ва страниц.
        private int _page;
        private int currentPage
        {
            get
            {
                return _page;
            }
            set
            {
                if (value < 0)
                    _page = 0;
                else if (value >= totalPages)
                    _page = totalPages > 1 ? totalPages - 1 : 0;
                else
                    _page = value;
            }
        }

        // Индексы сортировок
        enum SortBy
        {
            A = 2,
            Z = 3,
            StockMax = 4,
            StockMin = 5,
            CostMax = 6,
            CostMin = 7
        }
        #endregion

        private void wLoaded(object sender, RoutedEventArgs e)
        {
            // Инициализация крайне важной фигни
            lvData.Init(this, ref lvCount);
            tbSearch.Text = "Введите для поиска";

            // Добавляет типы материалов из БД в ComboBox
            foreach (var i in AppData.DB.MaterialTypes)
                cbFilter.Items.Add(i.Title);
        }

        // Обновляет кол-во выделенных элементов в списке материалов.
        private void lvMain_SelectionChanged(object sender, SelectionChangedEventArgs e) => lvSelCount.Text = $"| Выделенно: {lvMain.SelectedItems.Count}";
        // Снимает все выделения в списке материалов.
        private void DeselectAll(object sender, RoutedEventArgs e) => lvMain.UnselectAll();
        // Поиск по материалам при вводе текста в строку поиска.
        private void tbSearch_Search(object sender, TextChangedEventArgs e) => GetData();

        // Убирает плейсхолдер при нажатии на строку поиска.
        private void tbSearch_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (tbSearch.Text == "Введите для поиска")
                tbSearch.Text = String.Empty;
        }

        // Добавляет плейсхолдер при потере фокуса на строке поиска.
        private void tbSearch_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (tbSearch.Text.Trim() == String.Empty)
                tbSearch.Text = "Введите для поиска";
        }

        // Добавляет плейсхолдер если ничего не выбрано и обновление списка материалов
        private void cbSortFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 1)
                ((ComboBox)sender).SelectedIndex = 0;
            GetData();
        }

        // Верхнее меню
        #region Top Menu

        // Вызывается при раскрытии меню "Редактирование".
        // Активирует пункты меню по необходимости.
        private void MenuOpening(object sender, RoutedEventArgs e)
        {
            // По стандарту отключает оба пункта меню
            miEditMin.IsEnabled = miEditSelected.IsEnabled = false;

            // Если выбран только один единственный материал в списке,
            // активирует пункт "Редактировать выбранное"
            if (lvMain.Items.Count > 0 && lvMain.SelectedItems.Count == 1)
                miEditSelected.IsEnabled = true;

            // Если выбран хотя бы один материал в списке,
            // активирует пункт "Изменить минимальное кол-во"
            if (lvMain.Items.Count > 0 && lvMain.SelectedItem != null)
                miEditMin.IsEnabled = true;
        }

        // Вызывается при нажатии на любой элемент меню, далее проверяет какой именно элемент меню был нажат.
        private void MenuClick(object sender, RoutedEventArgs e)
        {
            Window window;
            switch (((MenuItem)sender).Name)
            {
                case "miEditMin":
                    window = new wEditMin(lvMain);
                    break;
                case "miEditSelected":
                    window = new wEdit((Models.Material)lvMain.SelectedItem);
                    break;
                case "miAddNew":
                    window = new wEdit();
                    break;
                case "miExit":
                    Close();
                    return;
                default:
                    return;
            }
            // Если созданное окно будет закрыто, обновит данные в списке
            window.Closed += (object a, EventArgs b) => { GetData(); };
            window.Owner = this;
            window.ShowDialog();
        }
        #endregion



        // Обрабатывает нажатия в блоке выбора страниц
        private void tbPage_Click(object sender, MouseButtonEventArgs e)
        {
            switch (((TextBlock)sender).Text)
            {
                case "<":
                    currentPage -= 1;
                    break;
                case ">":
                    currentPage += 1;
                    break;
                case "...":
                    break;
                default:
                    currentPage = int.Parse(((TextBlock)sender).Text) - 1;
                    break;
            }
            GetData();
        }

        // Обновляет данные в списке.
        private void GetData()
        {
            // Если какой-либо объект не инициализирован, выходить из функции, чтобы предотвратить ошибки
            if (tbSearch == null || cbSort == null || cbFilter == null || lvMain == null)
                return;

            // Снимаем все выделения с элементов
            lvMain.UnselectAll();

            // Метод Where() ругается, если эту строку напрямую в него запихнуть,
            // поэтому создаю отдельную переменную для этого
            string filter = cbFilter.SelectedItem.ToString();

            // Получаем данные, соответствующие нашим критериям
            var data = AppData.DB.Materials.Where(c =>
                tbSearch.Text != "Введите для поиска" && tbSearch.Text != String.Empty ?
                    c.Title.ToLower().Contains(tbSearch.Text.ToLower()) ||
                    c.Description.ToLower().Contains(tbSearch.Text.ToLower()) : true &&
                cbFilter.SelectedIndex > 1 ?
                    c.MaterialType.Title == filter : true
            ).ToList();

            // Запоминаем кол-во полученных элементов
            int foundCount = data.Count;

            // Вычисляем необходимое кол-во страниц
            totalPages = (int)Math.Ceiling(foundCount / (double)ITEMS_ON_PAGE);

            // Сортируем полученные элементы
            if (cbSort.SelectedIndex > 1)
                switch ((SortBy)cbSort.SelectedIndex)
                {
                    case SortBy.StockMax:
                        data.Sort((x, y) => y.CountInStock.Value.CompareTo(x.CountInStock.Value));
                        break;
                    case SortBy.StockMin:
                        data.Sort((x, y) => x.CountInStock.Value.CompareTo(y.CountInStock.Value));
                        break;
                    case SortBy.CostMax:
                        data.Sort((x, y) => y.Cost.CompareTo(x.Cost));
                        break;
                    case SortBy.CostMin:
                        data.Sort((x, y) => x.Cost.CompareTo(y.Cost));
                        break;
                    case SortBy.A:
                        data.Sort((x, y) => x.Title.CompareTo(y.Title));
                        break;
                    case SortBy.Z:
                        data.Sort((x, y) => y.Title.CompareTo(x.Title));
                        break;
                    default:
                        break;
                }

            // Если выбранная страница вышла за рамки после фильтрации,
            // перекидывает на первую страницу
            if (currentPage > totalPages - 1)
                currentPage = 0;

            // Находим индекс элемента который будет первым на странице
            // и начиная с него выводим не более ITEMS_ON_PAGE элементов
            int firstElementIndex = currentPage * ITEMS_ON_PAGE;
            int onPageCount = foundCount - firstElementIndex >= ITEMS_ON_PAGE ? ITEMS_ON_PAGE : foundCount - firstElementIndex;
            data = data.GetRange(firstElementIndex, onPageCount);
            lvMain.ItemsSource = data;

            // Пролистывает список в самый верх
            // (используется в случаях если мы остались на той же странице после поиска)
            if (lvMain.Items.Count > 0)
                lvMain.ScrollIntoView(lvMain.Items[0]);

            // Выводим стату типа "{первый элемент} - {последний элемент} из {сколько найдено}. Всего: {сколько в БД всего записей}"
            lvCount.Text = lvData.lvCountUpdate(firstElementIndex, onPageCount, foundCount, AppData.DB.Materials.Count());

            // Очищаем блок страниц
            spPages.Children.Clear();

            // Вставляет страницы в блок перехода по страницам
            for (int i = 1; i <= totalPages; i++)
                // Добавляет страницу в список, если i на PAGES_RANGE больше
                // или меньше текущей страницы, является первой или последней страницей.
                // Если i - текущая страница, элементу добавляется подчеркивание
                if ((currentPage + 1 - PAGES_RANGE <= i && currentPage + 1 + PAGES_RANGE >= i) || i == 1 || i == totalPages)
                    spPages.Children.Add(new TextBlock()
                    {
                        Text = i.ToString(),
                        Padding = new Thickness(3),
                        TextDecorations = i == currentPage + 1 ? TextDecorations.Underline : null
                    });
                // Вместо остальных страниц вставляется троеточие
                else if (i == 2 || i == totalPages - 1)
                    spPages.Children.Add(new TextBlock() { Text = "...", Padding = new Thickness(3) });

            // Вставляет элементы для перехода на одну страницу вперед и назад
            spPages.Children.Insert(0, new TextBlock() { Text = "<", Padding = new Thickness(3) });
            spPages.Children.Add(new TextBlock() { Text = ">", Padding = new Thickness(3) });

            // Добавляем обработчик события клика на все элементы блока страниц
            foreach (var i in spPages.Children)
                ((TextBlock)i).MouseLeftButtonDown += tbPage_Click;
        }
    }
}
