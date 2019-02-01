using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace MatchingCardGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Regex isNumberRegex = new Regex("^[0-9]*");
        public MainWindow()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (isTextAllowed(numberOfPairsBox.Text.ToString()))
            {
                startGame();
            }
        }

        // check if the number of pairs entered is valid
        private bool isTextAllowed(string text)
        {
            return isNumberRegex.IsMatch(text);
        }

        private void startGame()
        {

        }

        private void numberOfPairsBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //if (!isTextAllowed(e.Text.ToString()))
            //{
            //    e.Text = "";
            //}
        }
    }
}
