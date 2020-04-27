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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TodoViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var list = TodoList.EntryList.LoadFile("todo.txt", false);
            foreach (var item in list.Root.Children.Where(e => e.Status == "-"))
            {
                var child = new TreeViewItem();
                child.Header = item.Description;
                MainTree.Items.Add(child);

                if (item.Children.Count != 0)
                    BuildSubTree(item, child);
            }
        }

        private static void BuildSubTree(TodoList.Entry Entry, TreeViewItem Tree)
        {
            foreach (var child in Entry.Children.Where(e => e.Status == "-"))
            {
                var node = new TreeViewItem();
                node.Header = child.Description;
                Tree.Items.Add(node);
                if (child.Children.Count != 0)
                    BuildSubTree(child, node);
            }
        }
    }
}
