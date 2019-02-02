using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Resources;
using MatchingCardGame.Properties;
using System.Globalization;
using System.Collections;
using System.Windows.Interop;

namespace MatchingCardGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Regex isNumberRegex = new Regex("^[0-9]+$");
        private int numberOfPairs;
        private int score = 0;
        private int mistakes = 0;
        private Random random = new Random();
        BitmapImage cardBackImageSource;
        System.Windows.Controls.Image firstSelection = null;
        
        private Dictionary<int, Tuple<System.Windows.Controls.Image, BitmapImage>> imageMapping = new Dictionary<int, Tuple<System.Windows.Controls.Image, BitmapImage>>();
        private List<BitmapImage> solvedPairs = new List<BitmapImage>();
        public MainWindow()
        {
            InitializeComponent();
            StarterPanel.Visibility = Visibility.Visible;
            GamePanel.Visibility = Visibility.Collapsed;
        }

        private void updateNumberOfMistakes()
        {
            MistakesText.Visibility = Visibility.Visible;
            MistakesText.Text = "Number of mistakes: " + mistakes.ToString();
        }

        #region Initialization

        private void startGame()
        {
            string entry = numberOfPairsBox.Text.ToString();
            if (isTextAllowed(entry))
            {
                errorMessage.Visibility = Visibility.Collapsed;
                numberOfPairs = int.Parse(entry);
            }
            else
            {
                errorMessage.Visibility = Visibility.Visible;
            }

            StarterPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;

            updateNumberOfMistakes();

            // the following call puts us in bin/Debug or bin/Release depending on configuration. We need to go back to the directory of where the card
            // images are stored.
            string binPath = Environment.CurrentDirectory;
            var pathList = binPath.Split('\\').ToList();
            
            pathList.RemoveAt(pathList.Count - 1);
            pathList.RemoveAt(pathList.Count - 1);

            string path = "";
            foreach (var item in pathList)
            {
                path += item + "\\";
            }
            path += "CardImages\\";

            var files = new DirectoryInfo(path).GetFiles();
            ResourceManager myResources = new ResourceManager(typeof(Resources));

            ResourceSet resourceSet = myResources.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            cardBackImageSource = convertBitmapToBitmapImage(MatchingCardGame.Properties.Resources.red_back);
            
            int index = 0;
            // we will populate this panel list with all of the cards that will be placed, and they will be randomized afterwards
            var panelList = new List<StackPanel>();
            // using a foreach loop to easily access the images in the resourceSet
            foreach (DictionaryEntry cardImage in resourceSet)
            {
                // just in case, we don't want the back of a card to be entered as the card's front
                if (cardImage.Key.ToString() == "red_back")
                {
                    continue;
                }
                // We've reached the end at this point, so break out
                if (index == numberOfPairs * 2)
                {
                    break;
                }
                
                var imageSource = convertBitmapToBitmapImage((Bitmap) cardImage.Value);
                
                panelList.Add(createCardStackPanel(imageSource, index));

                index++;
                
                panelList.Add(createCardStackPanel(imageSource, index));

                index++;
            }
            // randomize the cards that were placed in the panelList and place them in the GameGrid for display.
            for (int i = 0; i < numberOfPairs*2; i++)
            {
                int randomIndex = random.Next(0, panelList.Count-1);
                GameGrid.Children.Add(panelList[randomIndex]);
                panelList.RemoveAt(randomIndex);
            }
        }

        private void resetGame()
        {
            GameGrid.Children.Clear();
            mistakes = 0;
            score = 0;
            solvedPairs.Clear();
            imageMapping.Clear();
            firstSelection = null;
            cardBackImageSource = null;
            numberOfPairs = 2;
            StarterPanel.Visibility = Visibility.Visible;
            GamePanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Validation

        // check if the number of pairs entered is valid
        private bool isTextAllowed(string text)
        {
            return isNumberRegex.IsMatch(text);
        }

        #endregion

        #region Event Handlers

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            resetGame();
        }

        public void selectCardButton_Click(object sender, RoutedEventArgs e)
        {
            string name = (sender as Button).Name.ToString();
            // index allows us to get the proper mapping
            int index = parseNameToIndex(name);
            // we are calling the data we saved in mapImageToCard earlier
            var imageTuple = imageMapping[index];
            // make sure they can't pick an image that's already been selected
            if (!imageTuple.Item1.Source.Equals(cardBackImageSource))
            {
                return;
            }
            // don't do anything if we already solved this pair
            if (solvedPairs.Contains(imageTuple.Item2))
            {
                return;
            }
            imageTuple.Item1.Source = imageTuple.Item2;
            // if this is the first selection
            if (firstSelection == null)
            {
                firstSelection = imageTuple.Item1;
            }
            else // if this is the second selection
            {
                // if the user is correct, give them a congrats and allow them to keep going
                if (firstSelection.Source.Equals(imageTuple.Item2))
                {
                    score++;
                    MessageBoxResult rsltMessageBox = MessageBox.Show("Nice match!");
                    solvedPairs.Add(imageTuple.Item2);
                    firstSelection = null;
                    if (score == numberOfPairs)
                    {
                        if (MessageBox.Show("Play again?", "You did it!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            resetGame();
                        }
                        else
                        {
                            System.Windows.Application.Current.Shutdown();
                        }
                    }
                }
                else // if the user is incorrect, give them an error statement and flip the cards back over
                {
                    mistakes++;
                    updateNumberOfMistakes();
                    MessageBoxResult rsltMessageBox = MessageBox.Show("Oh man! Not a match.");
                    firstSelection.Source = cardBackImageSource;
                    imageTuple.Item1.Source = cardBackImageSource;
                    firstSelection = null;
                }
            }

        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            startGame();
        }

        private void numberOfPairsBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            startGame();
        }

        #endregion

        #region Data Processing Functions

        // Create one of the StackPanels making up the game board
        private StackPanel createCardStackPanel(BitmapImage imageSource, int index)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Vertical;

            //create the button and assign the Click behaviour
            Button button = new Button();
            button.Name = "Name" + index.ToString();
            button.Content = "Select";
            button.Click += selectCardButton_Click;

            // create the image "face down" with cardBackImage
            System.Windows.Controls.Image cardBackImage = new System.Windows.Controls.Image();
            cardBackImage.Name = "Image" + index.ToString();
            cardBackImage.Source = cardBackImageSource;

            // map the image that will be on the front of this card in the mapping dict so that it can be referenced easily later
            mapImageToCard(cardBackImage, imageSource, index);

            panel.Children.Add(cardBackImage);
            panel.Children.Add(button);
            return panel;
        }

        // map the input image to the imageMapping dict for ease of access later on when a card button is selected.
        private void mapImageToCard(System.Windows.Controls.Image cardBackImage, BitmapImage imageSource, int index)
        {
            imageMapping[index] = new Tuple<System.Windows.Controls.Image, BitmapImage>(cardBackImage, imageSource);
        }
        
        private int parseNameToIndex(string name)
        {
            string stringIndex = new string(name.Where(c => char.IsDigit(c)).ToArray());
            return int.Parse(stringIndex);
        }

        private BitmapImage convertBitmapToBitmapImage(Bitmap bmp)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bmp.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }


        #endregion

        
    }
}
