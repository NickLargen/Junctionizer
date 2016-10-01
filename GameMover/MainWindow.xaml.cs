using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameMover.Model;
using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace GameMover
{

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        //todo: Delete on a junction gives recycle bin prompt but it's just for the junction
        //BUG: double clicking column to resize introduces the empty column on the right

        //todo: remove code from code behind

        //todo save locations between runs

        //todo invalid input handling
        //todo check for permissions everywhere

        //performance: sorting by size on hdd hangs ui
        //performance: test opening giant folder

        //feature: select all corresponding elements
        //feature: support drag and drop


        public MainWindow()
        {
            BindingExceptionThrower.Attach();
            InitializeComponent();
        }

        private void DataGridRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dataGridRow = sender as DataGridRow;
            var folder = dataGridRow?.Item as GameFolder;
            if (folder != null) Process.Start(folder.DirectoryInfo.FullName);
        }

        private void HideStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(0);
        }

        private void ShowStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
        }

    }

}
