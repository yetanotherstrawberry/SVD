using Numpy;
using SVD.ViewModels;
using System;
using System.Windows;

namespace SVD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Test()
        {
            var matrix = new int[,]
            {
                {1,0,0,0,2,},
                {0,0,3,0,0,},
                {0,0,0,0,0,},
                {0,2,0,0,0,},
            };

            (var u, var s, var vh) = np.linalg.svd(np.array(matrix), full_matrices: false);
            var ret = np.dot(u, np.dot(np.diag(s), vh));
            np.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Test();
        }
    }
}
