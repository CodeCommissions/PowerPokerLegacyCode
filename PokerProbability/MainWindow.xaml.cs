using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Collections.Concurrent;

namespace PokerProbability
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// We store the cards as their raw integer representation (0-51), this makes sorting easier
        /// </summary>
        List<int> handCards = new List<int>{-1,-1,-1,-1,-1};

        Dictionary<string, double> payoffTable = new Dictionary<string, double>() { { "High Card", 0 }, { "Pair", 0 }, { "Jacks or Better", 1 },  { "Two Pair", 2 }, { "Three of a Kind", 3 }, { "Straight", 4 }, { "Flush", 6 }, { "Full House", 9 }, { "Four of a Kind", 25 }, { "Straight Flush", 50 }, { "Royal Flush", 800 } };



        //could have used a dictionary here, only dictionaries are not conducive to order dependent algorithms, making combinatoric calculations harder
        List<string> deckContent;


        //we store references to the generated labels so that we can alter them after creation just in case
        List<Label> generatedLabels;
        List<Label> handLabels;
        List<TextBox> payoffTableBoxes;
        Dictionary<string,Label> handTypeLabels;
        Dictionary<string, Label> handTypeCountLabels;

        public MainWindow()
        {
            InitializeComponent();
           
            resetDeckContent();
            initialiseHandLabels();

            payoffTableBoxes = new List<TextBox> {tb_PayoffHighCard,tb_PayoffPair,tb_PayoffJacks,tb_PayoffTwoPair,tb_PayoffTriple,tb_PayoffStraight,tb_PayoffFlush,tb_PayoffFullHouse,tb_PayoffFourOfAKind,tb_PayoffStraightFlush,tb_PayoffRoyalFlush };

            lbl_HandType.Content = "";
            updateHandDisplay();
            initialiseHandTypeLabels();

            if (theMainWindow.Title.Contains("Demo"))
            {
                foreach (var tb in payoffTableBoxes)
                {
                    tb.ToolTip = "Payoff table editing disabled in demo version";
                    tb.IsReadOnly = true;
                }
            }
            else
            {
                img_Donate.IsEnabled = false;
                img_Donate.Visibility = System.Windows.Visibility.Hidden;
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            createDeckLabels();
        }

        /// <summary>
        /// Only to be called once, this method dynamically creates WPF label components and adds them to the form
        /// </summary>
        private void createDeckLabels()
        {
            const int WIDTH_SPACER = 59;

            if (generatedLabels != null)
                return;
            generatedLabels = new List<Label>();
            string suites = "♥♣♦♠";
            string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 13; col++)
                {
                    //The addLabel function returns the created label, which we want to add an event handler to for the mouseup event
                    //The in-line conditionals here adjust the arguments based on the row and coolumn values so that we don't have to stack several ifs
                    
                    

                    //correct for the offset in the columns from Jack onwards
                    if(col%13 == 9)
                        generatedLabels.Add(addLabelToCanvas(Canvas_Deck, values[col] + suites[row], 4 + col * WIDTH_SPACER + (col >= 9 ? 15 : 0), 60 + row * 40, row % 2 == 0 ? Colors.Red : Colors.Black, 36, true));
                    else
                        generatedLabels.Add(addLabelToCanvas(Canvas_Deck, values[col] + suites[row], col * WIDTH_SPACER + (col >= 9 ? 15 : 0), 60 + row * 40, row % 2 == 0 ? Colors.Red : Colors.Black, 36, true));
                    //                                                     //add offset for 10X correction            check if even row and select colour
                    //generatedLabels.Add(addLabelToCanvas(Canvas_Deck, values[col] + suites[row], 10 + col * 75 + (col >= 9 ? 30 : 0), 60 + row * 40, row % 2 == 0 ? Colors.Red : Colors.Black, 36, true));
                    generatedLabels[generatedLabels.Count - 1].MouseUp += cardLabelClicked;
                    //we originally used a lamba function for this event but it became too large to be worth it's clutter

                }
            }
        }

        /// <summary>
        /// This helper method is used to create new labels, and populate a given canvas with them. 
        /// A reference for the newly created label is returned in case you want to store it for later use.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="labelsText"></param>
        /// <param name="relativeXPos"></param>
        /// <param name="relativeYPos"></param>
        /// <param name="fontColour"></param>
        /// <param name="fontSize"></param>
        /// <param name="isHitTestVisible"></param>
        /// <returns></returns>
        public Label addLabelToCanvas(Canvas canvas, string labelsText, int relativeXPos, int relativeYPos, Color fontColour, int fontSize = 36, bool isHitTestVisible = true)
        {
            Label newLabel = new Label();
            // newLabel.Width = width;
            // newLabel.Margin = new Thickness(5);

            canvas.Children.Add(newLabel);
            newLabel.Content = labelsText;
            newLabel.IsHitTestVisible = isHitTestVisible;
            newLabel.Foreground = new SolidColorBrush(fontColour);
            newLabel.FontSize = fontSize;

            Canvas.SetLeft(newLabel, relativeXPos);
            Canvas.SetTop(newLabel, relativeYPos);
            return newLabel;
        }



        

        public Color getCardColour(int cardNum)
        {
            if (cardNum / 13 % 2 == 0)
                return Colors.Red;
            else
                return Colors.Black;
        }

        public Color getCardColour(string card)
        {
            if (card[card.Length - 1] == '♥' || card[card.Length - 1] == '♦')
                return Colors.Red;
            else
                return Colors.Black;
        }

        private void resetDeckContent(int totalDecks = 1)
        {
            deckContent = new List<string>();
            for (int deck = 0; deck < totalDecks; deck++)
			{
                for (int card = 0; card < 52; card++)
                {
                    deckContent.Add(CardUtilities.convertToStringCard(card));
                }
			}
        }

        private Label getLabel(string expectedContent)
        {
            for (int i = 0; i < generatedLabels.Count; i++)
                if (generatedLabels[i].Content.ToString() == expectedContent)
                    return generatedLabels[i];
            return null;
        }

        private void initialiseHandTypeLabels()
        {
            //if one is null they both should be
            if (handTypeLabels == null)
            {
                handTypeLabels = new Dictionary<string, Label>() { { CardUtilities.getHandValueName(0), lbl_HighCards }, { CardUtilities.getHandValueName(1), lbl_Pairs }, { CardUtilities.getHandValueName(2), lbl_jacksOrBetter }, { CardUtilities.getHandValueName(3), lbl_TwoPairs }, { CardUtilities.getHandValueName(4), lbl_Triple }, { CardUtilities.getHandValueName(5), lbl_Straight }, { CardUtilities.getHandValueName(6), lbl_Flush }, { CardUtilities.getHandValueName(7), lbl_FullHouse }, { CardUtilities.getHandValueName(8), lbl_FourOfAKind }, { CardUtilities.getHandValueName(9), lbl_StraightFlush }, { CardUtilities.getHandValueName(10), lbl_RoyalFlush } };
                handTypeCountLabels = new Dictionary<string, Label>() { { CardUtilities.getHandValueName(0), lbl_HighCardsCount }, { CardUtilities.getHandValueName(1), lbl_PairsCount },{ CardUtilities.getHandValueName(2), lbl_jacksOrBetterCount },  { CardUtilities.getHandValueName(3), lbl_TwoPairsCount }, { CardUtilities.getHandValueName(4), lbl_TripleCount }, { CardUtilities.getHandValueName(5), lbl_StraightCount }, { CardUtilities.getHandValueName(6), lbl_FlushCount }, { CardUtilities.getHandValueName(7), lbl_FullHouseCount }, { CardUtilities.getHandValueName(8), lbl_FourOfAKindCount }, { CardUtilities.getHandValueName(9), lbl_StraightFlushCount }, { CardUtilities.getHandValueName(10), lbl_RoyalFlushCount } };
            }
                                                //high card,lowpair,jack+ pair,2pair
           int[] initialProbabilities = new int[] { 1302540, 760320 , 337920,  123552, 54912, 10200, 5108, 3744, 624, 36, 4};
           double total = initialProbabilities.Sum();

           int i = 0;
           foreach (var pair in handTypeLabels)
           {
               handTypeLabels[pair.Key].Content = CardUtilities.getHandValueName(i);// +(CardUtilities.getHandValueName(i).Contains("Flush") ? "es" : "s"); //If you want to pluralise
               handTypeCountLabels[pair.Key].Content = handTypeCountLabels[pair.Key].Content = string.Format("%{0:0.00}", ((initialProbabilities[i]) / total * 100));
               handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Black);
               handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Black);
               i++;
           }


        }

        private void initialiseHandLabels()
        {
            handLabels = new List<Label>() { lbl_Hand1, lbl_Hand2, lbl_Hand3, lbl_Hand4, lbl_Hand5 };
            foreach (Label lbl in handLabels)
            {
                lbl.MouseUp += new MouseButtonEventHandler((object sender, MouseButtonEventArgs e)=>
                {
                    if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
                    {
                        int x = CardUtilities.convertFromStringCard(((Label)sender).Content.ToString());
                        if (handCards.Contains(x))
                        {
                            Label deckLabel = getLabel(((Label)sender).Content.ToString());
                            if (deckLabel == null)
                                return;

                           // if (deckLabel.Content.ToString()[1] == '♥' || deckLabel.Content.ToString()[1] == '♦')
                           //     deckLabel.Foreground = new SolidColorBrush(Colors.Red);
                           // else
                           //     deckLabel.Foreground = new SolidColorBrush(Colors.Black);

                            handCards.Remove(x);
                            handCards.Add(-1);
                            //deckContent.Add(((Label)sender).Content.ToString());
                            
                            updateHandDisplay();

                            setForHandState(false);

                        }
                        return;
                    }
                });
            }
        }

   
        /// <summary>
        /// 
        /// </summary>
        /// <param name="originalHand"></param>
        /// <param name="newUnalteredHand"></param>
        /// <returns>int[] Mask, List<int> hand</returns>
        private IEnumerable<Tuple<List<int>,string>> enumerateDiscardCombos(int[] originalHand, int[] newUnalteredHand)
        {
            //An alternative is do this recursively, not unlike enumerateUniqueIntegerCombinations, that is probably the more efficient choice and we'll 
            //experiment with it efficiency later (once the general game logic is sorted and we can focus on optimisation of modular components)

            //Get all length 5 permuataitons of 1 and 0 (ie count up in binary...this is faster than counting to 32 in decimal and converting to binary every time)

            //for(byte i = 0; i < 32; i++)
            //{
            //    string temp = Convert.ToString(i,2).PadLeft(5,'0');
            //    yield return new Tuple<string, List<int>>(temp, 
            //                                    new List<int>()
            //                                    {
            //                                        temp[0] == '1' ? originalHand[0] : newUnalteredHand[0],
            //                                        temp[1] == '1' ? originalHand[1] : newUnalteredHand[1],
            //                                        temp[2] == '1' ? originalHand[2] : newUnalteredHand[2],
            //                                        temp[3] == '1' ? originalHand[3] : newUnalteredHand[3],
            //                                        temp[4] == '1' ? originalHand[4] : newUnalteredHand[4],
            //                                    });
            //}


            for (int i1 = 0; i1 < 2; i1++)
                for (int i2 = 0; i2 < 2; i2++)
                    for (int i3 = 0; i3 < 2; i3++)
                        for (int i4 = 0; i4 < 2; i4++)
                            for (int i5 = 0; i5 < 2; i5++)
                            {
                                yield return new Tuple<List<int>, string>(new List<int>()
                                        {
                                            i1 == 0 ? originalHand[0] : newUnalteredHand[0],
                                            i2 == 0 ? originalHand[1] : newUnalteredHand[1],
                                            i3 == 0 ? originalHand[2] : newUnalteredHand[2],
                                            i4 == 0 ? originalHand[3] : newUnalteredHand[3],
                                            i5 == 0 ? originalHand[4] : newUnalteredHand[4],
                                        }, i1 +""+ i2 +""+  i3 +""+  i4 +""+  i5);
                            }

        }

        //private void anon(object target)
        //{
        //    Dictionary<string, int> handTypeCount = new Dictionary<string, int>() { { "High Card", 0 }, { "Pair", 0 }, { "Two Pair", 0 }, { "Three of a Kind", 0 }, { "Straight", 0 }, { "Flush", 0 }, { "Full House", 0 }, { "Four of a Kind", 0 }, { "Straight Flush", 0 }, { "Royal Flush", 0 } };

        //    Tuple<List<int>, List<int>, int[], Hashtable> param = (Tuple<List<int>, List<int>, int[], Hashtable>)target;

        //    foreach (string hand in CardUtilities.enumeratePossibleHands(param.Item1,param.Item2,param.Item3))
        //        {
        //            handTypeCount[hand]++;
        //        }
        //    lock (param.Item4)
        //    {
        //        param.Item4.Add(param.Item3, handTypeCount);
        //    }
        //}

        private void anon(object target)
        {
            Dictionary<string, int> handTypeCount;
            Tuple<List<int>, List<int>, int[], Hashtable> param;

            //Loop until the concurrent stack has been removed
            while (workerThreadQueue != null)
            {
                //try to pop from the stack
                if (workerThreadQueue.TryDequeue(out param))
                {
                    handTypeCount = new Dictionary<string, int>() { { "High Card", 0 }, { "Pair", 0 }, {"Jacks or Better",0}, { "Two Pair", 0 }, { "Three of a Kind", 0 }, { "Straight", 0 }, { "Flush", 0 }, { "Full House", 0 }, { "Four of a Kind", 0 }, { "Straight Flush", 0 }, { "Royal Flush", 0 } };

                    if (param == null)
                        break;

                    //if you suceed in poping then calculate all the hand details
                    foreach (string hand in CardUtilities.enumeratePossibleHands(param.Item1, param.Item2, param.Item3))
                    {
                        handTypeCount[hand]++;
                    }
                    lock (param.Item4)
                    {
                        param.Item4.Add(param.Item3, handTypeCount);
                    }
                }
            }
        }

        //ConcurrentStack<Tuple<List<int>, List<int>, int[], Hashtable>> workerThreadStack;
        ConcurrentQueue<Tuple<List<int>, List<int>, int[], Hashtable>> workerThreadQueue;

        /// <summary>
        /// Calculates all the odds for the current state. If you leave chosenMask as null the optimal strategy will be calculated and used
        /// </summary>
        /// <param name="chosenMask"></param>
        private void calculateAndDisplayProbabilities(int[] chosenMask = null)
        {
            //http://www.durangobill.com/VideoPoker.html

            //calculate the value of the hand ONCE so that we don't have to redo it for each comparison;
            string currentHandType = CardUtilities.getHandName(handCards);

            //Go through (and count the types of) every set of drawable cards, given the current discard values

            DateTime start = DateTime.Now;

            List<int> sortedDeckClone = CardUtilities.createSortedCloneOfDeck(deckContent);
            Hashtable hashedResults = new Hashtable();
            Thread[] workers = new Thread[4];

            if (chosenMask == null)
            {
                workerThreadQueue = new ConcurrentQueue<Tuple<List<int>, List<int>, int[], Hashtable>>();

                workers[0] = new Thread(new ParameterizedThreadStart(anon));
                workers[1] = new Thread(new ParameterizedThreadStart(anon));
                workers[2] = new Thread(new ParameterizedThreadStart(anon));
                workers[3] = new Thread(new ParameterizedThreadStart(anon));

                foreach (var maskQuad in CardUtilities.enumerateHandMasks_ThreadVersion())
                {
                    try
                    {
                        workerThreadQueue.Enqueue(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item1, hashedResults));
                        workerThreadQueue.Enqueue(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item2, hashedResults));
                        workerThreadQueue.Enqueue(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item3, hashedResults));
                        workerThreadQueue.Enqueue(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item4, hashedResults));

                        if (workers[0].ThreadState == ThreadState.Unstarted)
                        {                                                            //hand,    deck  , mask, Storage_hashTable
                            workers[0].Start(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item1, hashedResults));
                            workers[1].Start(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item2, hashedResults));
                            workers[2].Start(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item3, hashedResults));
                            workers[3].Start(new Tuple<List<int>, List<int>, int[], Hashtable>(handCards, sortedDeckClone, maskQuad.Item4, hashedResults));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }

                workerThreadQueue.Enqueue(null);
                workerThreadQueue.Enqueue(null);
                workerThreadQueue.Enqueue(null);
                workerThreadQueue.Enqueue(null);

                workers[0].Join();
                workers[1].Join();
                workers[2].Join();
                workers[3].Join();
                Console.WriteLine(DateTime.Now.Subtract(start).ToString());

                if (workerThreadQueue.Count != 0)
                    throw new Exception();
                else
                    workerThreadQueue = null;

            }
            else //This else occurs when the user wants to know about a SPECIFIC mask, not neccessarily the optimal one
            {

                //Do the chosen mask of the user
                Dictionary<string, int> handTypeCount = new Dictionary<string, int>() { { "High Card", 0 }, { "Pair", 0 }, { "Jacks or Better", 0 }, { "Two Pair", 0 }, { "Three of a Kind", 0 }, { "Straight", 0 }, { "Flush", 0 }, { "Full House", 0 }, { "Four of a Kind", 0 }, { "Straight Flush", 0 }, { "Royal Flush", 0 } };

                foreach (string hand in CardUtilities.enumeratePossibleHands(handCards, sortedDeckClone,chosenMask))
                {
                    handTypeCount[hand]++;
                }
                
                hashedResults.Add(chosenMask, handTypeCount);

                //Compute the draw all mask, that we use for opponent draw odds
                if (!(chosenMask[0] == 0 && chosenMask[0] == chosenMask[1] && chosenMask[1] == chosenMask[2] && chosenMask[2] == chosenMask[3] && chosenMask[3] == chosenMask[4]))
                {
                    handTypeCount = new Dictionary<string, int>() { { "High Card", 0 }, { "Pair", 0 }, { "Jacks or Better", 0 }, { "Two Pair", 0 }, { "Three of a Kind", 0 }, { "Straight", 0 }, { "Flush", 0 }, { "Full House", 0 }, { "Four of a Kind", 0 }, { "Straight Flush", 0 }, { "Royal Flush", 0 } };

                    int[] drawAll = new int[] { 0, 0, 0, 0, 0 };
                    foreach (string hand in CardUtilities.enumeratePossibleHands(handCards, sortedDeckClone, drawAll))
                    {
                        handTypeCount[hand]++;
                    }

                    hashedResults.Add(drawAll, handTypeCount);
                }
            }

            //By this stage we have the raw count data.
            //Next thing is to use that data to find optimal strageies and caluclate the odds
            
            List<int[]> bestMasks = new List<int[]>(){new int[]{1,1,1,1,1}};
            int[] factorials = {1,2,6,24,120};

            double highestExpected = 0;

            ///Keeps track of how many tied hand masks there are
            int tiedMasks = 0;
            
            
            foreach (var singleResult in hashedResults)
            {
                double runningTotal = 0;
                double top = sortedDeckClone.Count;
                int draws = 5-((int[])((DictionaryEntry)singleResult).Key).Sum();

                for (int i = 1; i < draws; i++)
                    top = top * (sortedDeckClone.Count - i);
                
                if (draws == 0)
                {
                    if(highestExpected < payoffTable[currentHandType])
                    {
                        highestExpected = payoffTable[currentHandType];
                        bestMasks = new List<int[]>(){(int[])((DictionaryEntry)singleResult).Key}; //= (int[])((DictionaryEntry)singleResult).Key;
                    }
                    continue;
                }

                foreach (var pair in ((Dictionary<string, int>)((DictionaryEntry)singleResult).Value))
                {
                    runningTotal += (payoffTable[pair.Key] * pair.Value);
                }



                if (highestExpected == runningTotal / (top / factorials[draws - 1]))
                {
                    tiedMasks++;
                    bestMasks.Add((int[])((DictionaryEntry)singleResult).Key);
                }

                if (draws > 0 && highestExpected < runningTotal/(top / factorials[draws-1]))
                {
                    tiedMasks = 0;
                    highestExpected = runningTotal/(top / factorials[draws-1]);
                    bestMasks = new List<int[]>() { (int[])((DictionaryEntry)singleResult).Key }; 
                }
            }

            //Once here, we have an optimal strategy selected (or the users selected mask)

            double weaker = 0, tied = -1, better = 0;
            int[] drawAllMask = null;
            //We need to find the reference to the actual list on the hashtable ([0,0,0,0,0])
            foreach(int[] key in hashedResults.Keys)
            {
                if (key[0] == 0 && key[0] == key[1] && key[1] == key[2] && key[2] == key[3] && key[3] == key[4])
                {
                    drawAllMask = key;
                    break;
                }
            }

            foreach(var type in (Dictionary<string,int>)hashedResults[drawAllMask])
            {
                //are we still adding values to weaker and tied?
                if (tied == -1)
                {
                    if (type.Key == currentHandType)
                        tied = type.Value;
                    else
                        weaker += type.Value;
                    continue;
                }

                //Getting here means that tied is greater than -1 (so we must be adding to better from here on)
                better += type.Value;
            }

            Console.WriteLine("Opponents have a %{0:0.000} of starting with a BETTER hand than what you have", (better/(tied+weaker))*100);

            ///Alter this to find the highest held card various masks and display that
            foreach (var mask in bestMasks)
            {
                foreach (var elem in mask)
                    Console.Write(elem);
                Console.WriteLine();
            }


            btn_Card1Holding.Content = bestMasks[0][0] == 1 ? "Hold" : "Draw";
            btn_Card2Holding.Content = bestMasks[0][1] == 1 ? "Hold" : "Draw";
            btn_Card3Holding.Content = bestMasks[0][2] == 1 ? "Hold" : "Draw";
            btn_Card4Holding.Content = bestMasks[0][3] == 1 ? "Hold" : "Draw";
            btn_Card5Holding.Content = bestMasks[0][4] == 1 ? "Hold" : "Draw";

            Console.WriteLine("Optimal return rate- "+highestExpected);
            Console.WriteLine("ties identical masks"+tiedMasks);
 
            Console.WriteLine(DateTime.Now.Subtract(start).ToString());

            int betterHands = 0;
            int tiedHands = -1;
            int worseHands = 0;
            foreach (var pair in (Dictionary<string,int>)hashedResults[bestMasks[0]])
            {
                if (pair.Key == currentHandType)
                {
                    tiedHands = pair.Value;
                    handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Blue);
                    handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Blue);
                }
                else
                    if (tiedHands == -1)
                    {
                        worseHands += pair.Value;
                        handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Red);
                        handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        betterHands += pair.Value;
                        handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Green);
                        handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Green);
                    }
            }

            double totalHands = betterHands + tiedHands + worseHands;
            foreach (var pair in (Dictionary<string, int>)hashedResults[bestMasks[0]])
            {
                //The flow direction property of labels causes weird positioning of special charachters: special symols seem to go to the opposite side you place them O_o when using "RightToLeft"
                if (totalHands == 0)
                    if (pair.Key != currentHandType)
                        handTypeCountLabels[pair.Key].Content = "%0.00";
                    else
                        handTypeCountLabels[pair.Key].Content = "%100.00";
                else
                {
                    double chance = (pair.Value / (totalHands) * 100);
                    if (chance != 0 && chance < 0.01)
                        handTypeCountLabels[pair.Key].Content = "%0.01>";
                    else
                        handTypeCountLabels[pair.Key].Content = string.Format("%{0:0.00}", (pair.Value / (totalHands) * 100));
                        
                    
                }
            }

            lbl_decrease.Content = string.Format("{0:0.0%} chance of value decreasing", worseHands / totalHands);
            lbl_increase.Content = string.Format("{0:0.0%} chance of value increasing", betterHands / totalHands);
            lbl_returnRate.Content = string.Format("{0:0.0%} estimated return rate", highestExpected);
            lbl_betterStart.Content = string.Format("{0:0.0%} chance of better starting hand", (better / (tied + weaker))); 



        }
        

        /// <summary>
        /// Does exactly what the name implies: it updates the apperance of the 5 hand card labels to reflect what the user is holding
        /// </summary>
        private void updateHandDisplay()
        {
            //a safety check
            if (handCards == null)
                handCards = new List<int> { -1, -1, -1, -1, -1 };

            for (int i = 0; i < handLabels.Count; i++)
            {
                handLabels[i].Content = CardUtilities.convertToStringCard(handCards[i]);
                handLabels[i].Foreground = new SolidColorBrush(getCardColour(handCards[i]));
            }
            
        }

        /// <summary>
        /// Adds a new card to the hand, sorts the hand, and updates the display to reflects the hands new state
        /// </summary>
        /// <param name="card"></param>
        private void passCardToHand(string card)
        {
            if (handCards == null)
                handCards = new List<int>{-1,-1,-1,-1,-1}; //fill it will illegal card values

           
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i] == -1)
                {
                    handCards[i] = CardUtilities.convertFromStringCard(card);
                    break;
                }
            }

            handCards.Sort(CardUtilities.NumberCardComparer);

            updateHandDisplay();
        }


        private void cardLabelClicked(object sender, MouseButtonEventArgs e) 
        { 
            //This code will be executed when the new label is clicked - we want it to remove a card from the deck upon left cliking, right click will replace removed ones
            if(e.ChangedButton == MouseButton.Left)
            {               
                //check if there are any still to be removed
                if (deckContent.Contains(((Label)sender).Content.ToString()))
                {
                    deckContent.Remove(((Label)sender).Content.ToString()); //and remove one if there is
                    passCardToHand(((Label)sender).Content.ToString());
                }

                //check if there none left and grey out if there are
                if (!deckContent.Contains(((Label)sender).Content.ToString()))
                    ((Label)sender).Foreground = new SolidColorBrush(Colors.Gray);

                if (handCards[4] != -1)
                {
                    lbl_HandType.Content = CardUtilities.getHandValueName(CardUtilities.getSortedHandValue(handCards));
                    setForHandState(true);
                }
                else
                    setForHandState(false);

                    return;
            }

            //Put the card back in the deck if it was missing
            if(e.ChangedButton == MouseButton.Right)
            {
                //change the colour back to normal
                if (((Label)sender).Content.ToString()[1] == '♥' || ((Label)sender).Content.ToString()[1] == '♦')
                    ((Label)sender).Foreground = new SolidColorBrush(Colors.Red);
                else
                    ((Label)sender).Foreground = new SolidColorBrush(Colors.Black);

                //Check if the hand is holding a given card, and remove it from the hand if it is
                int x = CardUtilities.convertFromStringCard(((Label)sender).Content.ToString());
                if (handCards.Contains(x))
                {
                    handCards.Remove(x);
                    handCards.Add(-1);
                    deckContent.Add(((Label)sender).Content.ToString());
                    updateHandDisplay();
                }

                //if the card is missing from the deck replace it
                if (!deckContent.Contains(((Label)sender).Content.ToString()))
                {
                    deckContent.Add(((Label)sender).Content.ToString());
                }
                btn_Calculate.IsEnabled = false;
                btn_SpecificMask.IsEnabled = false;
                lbl_HandType.Content = "";
                
            }
        }

        
        
        /// <summary>
        /// Sets the states of the various GUI components based on whether the hand is full or not.
        /// For example it disabled all the buttons until the hand is full.
        /// </summary>
        /// <param name="isCompleteHand"></param>
        private void setForHandState(bool isCompleteHand)
        {
            Button[] buttons = new Button[] { btn_Calculate, btn_SpecificMask,  btn_Card1Holding, btn_Card2Holding, btn_Card3Holding, btn_Card4Holding, btn_Card5Holding };
            foreach (var btn in buttons)
                btn.IsEnabled = isCompleteHand;

            lbl_HandType.Content = isCompleteHand ? CardUtilities.getHandValueName(CardUtilities.getSortedHandValue(handCards)) : "";

            lbl_Instructions.Content = isCompleteHand ? "Click other cards you want removed from the deck." : "Click the cards you want to draw.";

            lbl_decrease.Content = "Uncalulated chance of value decreasing";
            lbl_increase.Content = "Uncalulated chance of value increasing";
            lbl_betterStart.Content = "Uncalulated chance of better starting hand";
            lbl_returnRate.Content = "Uncalulated estimated return rate";


            if (!isCompleteHand)
                initialiseHandTypeLabels();


        }

        /// <summary>
        /// Looks at the 5 bold/draw buttons, and creates a card draw/hold mask based on their state.
        /// 1 -> Hold current card
        /// 0 -> Draw new card
        /// </summary>
        /// <returns></returns>
        private int[] getReplacementMask()
        {
            return new int[] 
                {
                    btn_Card1Holding.Content.ToString() == "Hold" ? 1 : 0,
                    btn_Card2Holding.Content.ToString() == "Hold" ? 1 : 0,
                    btn_Card3Holding.Content.ToString() == "Hold" ? 1 : 0,
                    btn_Card4Holding.Content.ToString() == "Hold" ? 1 : 0,
                    btn_Card5Holding.Content.ToString() == "Hold" ? 1 : 0,
                };
        }

        private void initializePayoffTable()
        {
            for (int i = 0; i < payoffTableBoxes.Count; i++)
            {
                if (payoffTableBoxes[i].Text == "")
                    payoffTableBoxes[i].Text = "0";

                double temp;
                if (double.TryParse(payoffTableBoxes[i].Text, out temp))
                {
                    payoffTable[CardUtilities.getHandValueName(i)] = temp;
                }
                else
                {
                    payoffTableBoxes[i].Text = "0";
                    payoffTable[CardUtilities.getHandValueName(i)] = 0;
                }
            }
        }

        private bool checkAssendingPayoffs()
        {

            for (int i = 1; i < payoffTable.Count; i++)
            {
                if (payoffTable[CardUtilities.getHandValueName(i)] < payoffTable[CardUtilities.getHandValueName(i-1)])
                {                                                                                                                                 
                    MessageBoxResult output = MessageBox.Show("Your payoff table is not in ascending order, \nyou can continue and potentially get some \nbizzarre results, or stop now and fix it.\n\nContinue?", "Non-ascending payoffs", MessageBoxButton.YesNo);
                    return output == MessageBoxResult.Yes;
                }
            }
            return true;
        }

        private void btn_Calculate_Click(object sender, RoutedEventArgs e)
        {
            initializePayoffTable();
            if (!checkAssendingPayoffs())
                return;

            var x = DateTime.Now;
            var mask = getReplacementMask();
            calculateAndDisplayProbabilities();
            Console.WriteLine(DateTime.Now - x);
        }

        private void btn_SpecificMask_Click(object sender, RoutedEventArgs e)
        {
            initializePayoffTable();
            if (!checkAssendingPayoffs())
                return;

            var x = DateTime.Now;
            var mask = getReplacementMask();
            calculateAndDisplayProbabilities(mask);
            Console.WriteLine(DateTime.Now - x);
        }

        

        private void btn_Reset_Click(object sender, RoutedEventArgs e)
        {

            btn_Calculate.IsEnabled = false;
            lbl_ComparisonResults.Content = "Probabilites will display here:";
            handCards = null;
            for (int i = 0; i < generatedLabels.Count; i++)
            {
                generatedLabels[i].Foreground = new SolidColorBrush(getCardColour(i));
            }
            

            initialiseHandTypeLabels();
            resetDeckContent();
            updateHandDisplay();

            setForHandState(false);
        }

        /// <summary>
        /// Opens up a browser, allowing users to donate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void img_Donate_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //got help from here:
            //https://stackoverflow.com/questions/18401786/c-sharp-add-a-paypal-donate-button-in-application
            if (e.ChangedButton == MouseButton.Left)
            {
                string url = "";

                string business = "youremail@gmail.ca";  // your paypal email
                string description = "Donation";            // '%20' represents a space. remember HTML!
                string country = "US";                  // AU, US, etc.
                string currency = "USD";                 // AUD, USD, etc.

                url += "https://www.paypal.com/cgi-bin/webscr" +
                    "?cmd=" + "_donations" +
                    "&business=" + business +
                    "&lc=" + country +
                    "&item_name=" + description +
                    "&currency_code=" + currency +
                    "&bn=" + "PP%2dDonationsBF";

                System.Diagnostics.Process.Start(url);
            }
        }

        private void img_Donate_MouseEnter(object sender, MouseEventArgs e)
        {
            img_Donate.OpacityMask = new SolidColorBrush(Color.FromArgb(200,0,0,0));
        }

        private void img_Donate_MouseLeave(object sender, MouseEventArgs e)
        {
            img_Donate.OpacityMask = new SolidColorBrush(Colors.White);
        }

        private void btn_CardHolding_Toggle(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Content = ((Button)sender).Content.ToString() == "Draw" ? "Hold" : "Draw";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            AboutBox box = new AboutBox();
            box.Show();
        }

        private void btn_PayoffExplanation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
@"The payoff table dictates the amount of money paid out for getting
a certain hand type in video poker. This table works on a per currency
unit basis - so for every 1 dollar you put in, you get paid the amount
in the table for getting a certain hand.

It only truly applies to video poker, though it is still a useful gauge
in live games.

Our algorithm is sophisticated enough to cater for negative payoffs, these
essentially mean that getting a certain hand type results in some sort of
penalty. No real-stakes games are likely to use this sort of thing, but it
can make games with friends much more interesting.", "Payoff table help" );
        }

       

        /*
          private void calculateAndDisplayProbabilities()
        {
            //We're going to want a count for every hand type...so why bother performing the same "does it exist" check for every hand we go through (my validation for this explicit dictionary declaration)
            Dictionary<string, int> handTypeCount = new Dictionary<string, int>() { { "High Card", 0 }, { "Pair", 0 }, { "Two Pair", 0 }, { "Three of a Kind", 0 }, { "Straight", 0 }, { "Flush", 0 }, { "Full House", 0 }, { "Four of a Kind", 0 }, { "Straight Flush", 0 }, { "Royal Flush", 0 } };

            //calculate the value of the hand ONCE so that we don't have to redo it for each comparison;

            string currentHandType = CardUtilities.getHandName(handCards);

            //Go through (and count the types of) every set of drawable cards, given the current discard values


            foreach (string hand in CardUtilities.enumeratePossibleHands(handCards, CardUtilities.createSortedCloneOfDeck(deckContent), getReplacementMask()))
            {
                handTypeCount[hand]++;
            }


            
            int betterHands = 0;
            int tiedHands = -1;
            int worseHands = 0;

            foreach (var pair in handTypeCount)
            {
                if (pair.Key == currentHandType)
                {
                    tiedHands = pair.Value;
                    handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Blue);
                    handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Blue);
                }
                else
                    if (tiedHands == -1)
                    {
                        worseHands += pair.Value;
                        handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Red);
                        handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        betterHands += pair.Value;
                        handTypeCountLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Green);
                        handTypeLabels[pair.Key].Foreground = new SolidColorBrush(Colors.Green);
                    }
            }

            double totalHands = betterHands + tiedHands + worseHands;
            foreach (var pair in handTypeCount)
            {
                //The flow direction property of labels causes weird positioning of special charachters: special symols seem to go to the opposite side you place them O_o when using "RightToLeft"
                if (totalHands == 0)
                    if(pair.Key != currentHandType)
                        handTypeCountLabels[pair.Key].Content = "%0.00";
                    else
                        handTypeCountLabels[pair.Key].Content = "%100.00";
                else
                {
                    handTypeCountLabels[pair.Key].Content = string.Format("%{0:0.00}", (pair.Value / (totalHands) * 100));
                }
            }

            lbl_decrease.Content =  string.Format("{0:0.0%} chance of value decreasing",worseHands/totalHands);
            lbl_increase.Content = string.Format("{0:0.0%} chance of value increasing", betterHands / totalHands); 

        }
        

         */

        ////////////////

        /*
         *  private void calculateAndDisplayProbabilities()
        {

            foreach (string q in CardUtilities.enumeratePossibleHands(handCards,
                CardUtilities.createSortedCloneOfDeck(deckContent), getReplacementMask())) ;


            //We're going to want a count for every hand type...so why bother performing the same "does it exist" check for every hand we go through (my validation for this explicit dictionary declaration)
            Dictionary<string, int> handTypeCount = new Dictionary<string, int>() {{"High Card", 0},{ "Pair", 0},{"Two Pair", 0},{"Three of a Kind", 0},{"Straight", 0},{"Flush", 0},{"Full House", 0},{"Four of a Kind", 0},{"Straight Flush", 0},{"Royal Flush",0}};
            
            List<int> sortedDeckAsNums = CardUtilities.createSortedCloneOfDeck(deckContent);

            //calculate the value of the hand ONCE so that we don't have to redo it for each comparison;
            double handValue = CardUtilities.getSortedHandValue(handCards);
 
            int betterHands = 0;
            int tiedHands = 0;
            int worseHands = 0;
            double currentValue;

            foreach (List<int> possibleHandIndexes in CardUtilities.enumerateUniqueIntegerCombinations(0, sortedDeckAsNums.Count, 5))
            {
                //Becuase enumerateUniqueIntegerCombinations returns INDEX POSITIONS from 0 to deck-size, we have to get the matching values for each index
                currentValue = CardUtilities.getSortedHandValue(new List<int>(){sortedDeckAsNums[possibleHandIndexes[0]],sortedDeckAsNums[possibleHandIndexes[1]],sortedDeckAsNums[possibleHandIndexes[2]],
                                                    sortedDeckAsNums[possibleHandIndexes[3]],sortedDeckAsNums[possibleHandIndexes[4]]});

                handTypeCount[CardUtilities.getHandValueName(currentValue)]++;

                if (handValue > currentValue)
                    worseHands++;
                else if (handValue < currentValue)
                    betterHands++;
                else
                    tiedHands++;
            }

            double totalHands = worseHands + betterHands + tiedHands;

            string displayString = string.Format("Of the Remaining\n{0} Possible Combinations:\n\n{1} ({2}{3:0.0000%}) are stronger than your hand.\n{4} ({5}{6:0.0000%}) are tied with your hand.\n{7} ({8}{9:0.0000%}) are weaker than your hand.\n\n",
                                totalHands, betterHands, betterHands / totalHands < 0.000001 ? "<" : "", betterHands / totalHands < 0.000001 ? 0.000001 : betterHands / totalHands,
                                tiedHands, tiedHands / totalHands < 0.000001 ? "<" : "", tiedHands / totalHands < 0.000001 ? 0.000001 : tiedHands / totalHands,
                                worseHands, worseHands / totalHands < 0.000001 ? "<" : "", worseHands / totalHands < 0.000001 ? 0.000001 : worseHands / totalHands);

            foreach (var pair in handTypeCount)
            {
                displayString += string.Format("There are {0} {1} hands still possible.\n", pair.Value, pair.Key);
                handTypeCountLabels[pair.Key].Content = String.Format("{0:n0}", pair.Value);
            }

            lbl_ComparisonResults.Content = displayString;
            
            //foreach(var pair in handTypeCount)
            //{
            //    Console.WriteLine("We found {0} {1} hands",pair.Value,pair.Key);
            //}
        }
        */
    }
}
